using Amazon.MediaConvert;
using Amazon.MediaConvert.Model;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Interfaces;
using NoiseVault.Infra.Data.Storage;

namespace NoiseVault.Infra.Data.Media
{
    public class MediaConvertVideoTranscoder : IRemoteVideoTranscoder
    {
        private const string PlaylistBaseName = "master";
        private const string PosterBaseName = "poster";
        private const string AudioSelectorName = "Audio Selector 1";
        private const int SegmentSeconds = 4;
        private const int MaxRenditions = 4;
        private const int MaxAbrBitrate = 5_000_000;
        private const int MinAbrBitrate = 600_000;
        private const int AudioBitrate = 128_000;
        private const int PosterCaptures = 2;
        private const int PosterQuality = 80;

        private readonly IAmazonMediaConvert _client;
        private readonly VideoSettings _video;
        private readonly StorageSettings _storage;
        private readonly IApplicationLogger _logger;

        public MediaConvertVideoTranscoder(IAmazonMediaConvert client, VideoSettings video, StorageSettings storage, IApplicationLoggerFactory loggerFactory)
        {
            _client = client;
            _video = video;
            _storage = storage;
            _logger = loggerFactory.Create(LogContext.Media);
        }

        public bool Enabled => true;

        public string PlaylistFile => $"{PlaylistBaseName}.m3u8";

        public IReadOnlyList<string> PosterFiles { get; } = [$"{PosterBaseName}.0000001.jpg", $"{PosterBaseName}.0000000.jpg"];

        public async Task<string> StartAsync(RemoteTranscodeRequest request, CancellationToken cancellationToken = default)
        {
            var job = new CreateJobRequest
            {
                Role = _video.MediaConvertRoleArn,
                Queue = string.IsNullOrWhiteSpace(_video.MediaConvertQueueArn) ? null : _video.MediaConvertQueueArn,
                UserMetadata = new Dictionary<string, string> { ["mediaId"] = request.MediaId.ToString() },
                Settings = new JobSettings
                {
                    Inputs =
                    [
                        new Input
                        {
                            FileInput = S3Uri(request.InputKey),
                            TimecodeSource = InputTimecodeSource.ZEROBASED,
                            VideoSelector = new VideoSelector { Rotate = InputRotate.AUTO },
                            AudioSelectors = new Dictionary<string, AudioSelector>
                            {
                                [AudioSelectorName] = new AudioSelector { DefaultSelection = AudioDefaultSelection.DEFAULT },
                            },
                        },
                    ],
                    OutputGroups = [HlsGroup(request.OutputFolder), PosterGroup(request.OutputFolder)],
                },
            };

            var response = await _client.CreateJobAsync(job, cancellationToken);

            _logger.Info("MEDIACONVERT_JOB_CREATED", "MediaConvert job created", extra: new { request.MediaId, JobId = response.Job.Id });

            return response.Job.Id;
        }

        public async Task<RemoteTranscodeResult> CheckAsync(string jobId, CancellationToken cancellationToken = default)
        {
            var job = (await _client.GetJobAsync(new GetJobRequest { Id = jobId }, cancellationToken)).Job;

            if (job.Status == JobStatus.COMPLETE)
            {
                var outputs = (job.OutputGroupDetails ?? [])
                    .SelectMany(group => group.OutputDetails ?? [])
                    .ToList();

                var videos = outputs.Select(o => o.VideoDetails).Where(v => v != null).ToList();
                var width = videos.Count == 0 ? 0 : videos.Max(v => v!.WidthInPx ?? 0);
                var height = videos.Count == 0 ? 0 : videos.Max(v => v!.HeightInPx ?? 0);
                var durationMs = outputs.Count == 0 ? 0 : outputs.Max(o => o.DurationInMs ?? 0);

                return new RemoteTranscodeResult(RemoteTranscodeState.Complete, width, height, (int)Math.Round(durationMs / 1000d));
            }

            if (job.Status == JobStatus.ERROR || job.Status == JobStatus.CANCELED)
                return new RemoteTranscodeResult(RemoteTranscodeState.Failed, Error: $"{job.Status.Value} {job.ErrorCode}: {job.ErrorMessage}");

            return new RemoteTranscodeResult(RemoteTranscodeState.Running);
        }

        private OutputGroup HlsGroup(string folder) => new()
        {
            Name = "HLS",
            OutputGroupSettings = new OutputGroupSettings
            {
                Type = OutputGroupType.HLS_GROUP_SETTINGS,
                HlsGroupSettings = new HlsGroupSettings
                {
                    Destination = S3Uri($"{folder}/{PlaylistBaseName}"),
                    SegmentLength = SegmentSeconds,
                    MinSegmentLength = 0,
                    SegmentControl = HlsSegmentControl.SEGMENTED_FILES,
                    DirectoryStructure = HlsDirectoryStructure.SINGLE_DIRECTORY,
                    ManifestDurationFormat = HlsManifestDurationFormat.INTEGER,
                    OutputSelection = HlsOutputSelection.MANIFESTS_AND_SEGMENTS,
                    StreamInfResolution = HlsStreamInfResolution.INCLUDE,
                    CodecSpecification = HlsCodecSpecification.RFC_4281,
                    ManifestCompression = HlsManifestCompression.NONE,
                },
            },
            AutomatedEncodingSettings = new AutomatedEncodingSettings
            {
                AbrSettings = new AutomatedAbrSettings
                {
                    MaxRenditions = MaxRenditions,
                    MaxAbrBitrate = MaxAbrBitrate,
                    MinAbrBitrate = MinAbrBitrate,
                },
            },
            Outputs =
            [
                new Output
                {
                    ContainerSettings = new ContainerSettings { Container = ContainerType.M3U8, M3u8Settings = new M3u8Settings() },
                    VideoDescription = new VideoDescription
                    {
                        CodecSettings = new VideoCodecSettings
                        {
                            Codec = VideoCodec.H_264,
                            H264Settings = new H264Settings
                            {
                                RateControlMode = H264RateControlMode.QVBR,
                                CodecProfile = H264CodecProfile.HIGH,
                                CodecLevel = H264CodecLevel.AUTO,
                                FramerateControl = H264FramerateControl.INITIALIZE_FROM_SOURCE,
                                GopSizeUnits = H264GopSizeUnits.SECONDS,
                                GopSize = 2,
                                GopClosedCadence = 1,
                                SceneChangeDetect = H264SceneChangeDetect.TRANSITION_DETECTION,
                            },
                        },
                        VideoPreprocessors = new VideoPreprocessor
                        {
                            Deinterlacer = new Deinterlacer
                            {
                                Algorithm = DeinterlaceAlgorithm.INTERPOLATE,
                                Control = DeinterlacerControl.NORMAL,
                                Mode = DeinterlacerMode.ADAPTIVE,
                            },
                        },
                    },
                    AudioDescriptions =
                    [
                        new AudioDescription
                        {
                            AudioSourceName = AudioSelectorName,
                            CodecSettings = new AudioCodecSettings
                            {
                                Codec = AudioCodec.AAC,
                                AacSettings = new AacSettings
                                {
                                    Bitrate = AudioBitrate,
                                    CodingMode = AacCodingMode.CODING_MODE_2_0,
                                    SampleRate = 48000,
                                    RateControlMode = AacRateControlMode.CBR,
                                    CodecProfile = AacCodecProfile.LC,
                                },
                            },
                        },
                    ],
                },
            ],
        };

        private OutputGroup PosterGroup(string folder) => new()
        {
            Name = "Poster",
            OutputGroupSettings = new OutputGroupSettings
            {
                Type = OutputGroupType.FILE_GROUP_SETTINGS,
                FileGroupSettings = new FileGroupSettings { Destination = S3Uri($"{folder}/{PosterBaseName}") },
            },
            Outputs =
            [
                new Output
                {
                    Extension = "jpg",
                    ContainerSettings = new ContainerSettings { Container = ContainerType.RAW },
                    VideoDescription = new VideoDescription
                    {
                        CodecSettings = new VideoCodecSettings
                        {
                            Codec = VideoCodec.FRAME_CAPTURE,
                            FrameCaptureSettings = new FrameCaptureSettings
                            {
                                FramerateNumerator = 1,
                                FramerateDenominator = 1,
                                MaxCaptures = PosterCaptures,
                                Quality = PosterQuality,
                            },
                        },
                    },
                },
            ],
        };

        private string S3Uri(string key) => $"s3://{_storage.Bucket}/{key}";
    }
}
