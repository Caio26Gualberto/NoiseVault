using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Interfaces;

namespace NoiseVault.Infra.Data.Media
{
    public class FfmpegVideoTranscoder : IVideoTranscoder
    {
        private const int StderrTailLength = 2000;
        private const string PlaylistFile = "master.m3u8";
        private const int SegmentSeconds = 4;
        private const double HighFrameRate = 30.5;
        private const double MaxFrameRate = 60;

        private static readonly (int ShortSide, int BitrateKbps)[] Ladder =
        [
            (360, 800),
            (480, 1400),
            (720, 2800),
            (1080, 5000),
        ];

        private readonly VideoSettings _settings;

        public FfmpegVideoTranscoder(VideoSettings settings)
        {
            _settings = settings;
        }

        public async Task<VideoProbe?> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
        {
            var run = await RunAsync(_settings.FfprobePath,
            [
                "-v", "error",
                "-show_entries", "format=duration:stream=codec_type,width,height,avg_frame_rate:stream_side_data=rotation",
                "-of", "json",
                inputPath,
            ], cancellationToken);

            if (run.ExitCode != 0)
                return null;

            try
            {
                using var json = JsonDocument.Parse(run.Stdout);
                var root = json.RootElement;

                var duration = root.TryGetProperty("format", out var format)
                    && format.TryGetProperty("duration", out var durationText)
                    && double.TryParse(durationText.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                        ? seconds
                        : 0;

                if (!root.TryGetProperty("streams", out var streams))
                    return new VideoProbe(false, false, duration, 0, 0, 0);

                var hasAudio = streams.EnumerateArray().Any(s => s.TryGetProperty("codec_type", out var t) && t.GetString() == "audio");

                foreach (var stream in streams.EnumerateArray())
                {
                    if (!stream.TryGetProperty("codec_type", out var type) || type.GetString() != "video")
                        continue;

                    var width = stream.TryGetProperty("width", out var w) ? w.GetInt32() : 0;
                    var height = stream.TryGetProperty("height", out var h) ? h.GetInt32() : 0;

                    if (IsQuarterTurn(stream))
                        (width, height) = (height, width);

                    return new VideoProbe(width > 0 && height > 0, hasAudio, duration, width, height, FrameRateOf(stream));
                }

                return new VideoProbe(false, hasAudio, duration, 0, 0, 0);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public async Task<TranscodedVideo> TranscodeAsync(string inputPath, string outputDirectory, CancellationToken cancellationToken = default)
        {
            var source = await ProbeAsync(inputPath, cancellationToken)
                ?? throw new InvalidOperationException("ffprobe não leu o vídeo original.");

            var frameRate = source.FrameRate > 0 ? Math.Min(source.FrameRate, MaxFrameRate) : 30;
            var highFrameRate = frameRate > HighFrameRate;
            var steps = PlanLadder(Math.Min(source.Width, source.Height));
            var hlsDirectory = Path.Combine(outputDirectory, "hls");
            Directory.CreateDirectory(hlsDirectory);

            var filter = $"[0:v]split={steps.Count}" + string.Concat(steps.Select((_, i) => $"[s{i}]")) + ";"
                + string.Join(";", steps.Select((step, i) =>
                {
                    var side = step.ShortSide.ToString(CultureInfo.InvariantCulture);
                    return $"[s{i}]scale=w='if(gt(iw,ih),-2,{side})':h='if(gt(iw,ih),{side},-2)'[v{i}]";
                }));

            var arguments = new List<string>
            {
                "-hide_banner", "-loglevel", "error", "-y",
                "-i", inputPath,
                "-filter_complex", filter,
            };

            for (var i = 0; i < steps.Count; i++)
            {
                arguments.AddRange(["-map", $"[v{i}]"]);
                if (source.HasAudio)
                    arguments.AddRange(["-map", "0:a:0"]);
            }

            arguments.AddRange(
            [
                "-c:v", "libx264", "-preset", "veryfast", "-profile:v", "high", "-pix_fmt", "yuv420p",
                "-crf", _settings.Crf.ToString(CultureInfo.InvariantCulture),
                "-fpsmax", MaxFrameRate.ToString(CultureInfo.InvariantCulture),
                "-force_key_frames", $"expr:gte(t,n_forced*{SegmentSeconds / 2})",
            ]);

            for (var i = 0; i < steps.Count; i++)
            {
                var bitrate = (int)(steps[i].BitrateKbps * (highFrameRate ? 1.5 : 1));
                arguments.AddRange([$"-maxrate:v:{i}", $"{bitrate}k", $"-bufsize:v:{i}", $"{bitrate * 2}k"]);
            }

            if (source.HasAudio)
                arguments.AddRange(["-c:a", "aac", "-b:a", "128k", "-ac", "2"]);

            var streamMap = string.Join(" ", steps.Select((step, i) =>
                (source.HasAudio ? $"v:{i},a:{i}" : $"v:{i}") + $",name:{step.ShortSide}p"));

            arguments.AddRange(
            [
                "-f", "hls",
                "-hls_time", SegmentSeconds.ToString(CultureInfo.InvariantCulture),
                "-hls_playlist_type", "vod",
                "-hls_flags", "independent_segments",
                "-hls_segment_filename", Path.Combine(hlsDirectory, "%v", "seg_%03d.ts"),
                "-master_pl_name", PlaylistFile,
                "-var_stream_map", streamMap,
                Path.Combine(hlsDirectory, "%v", "index.m3u8"),
            ]);

            var transcode = await RunAsync(_settings.FfmpegPath, arguments, cancellationToken);
            var masterPath = Path.Combine(hlsDirectory, PlaylistFile);

            if (transcode.ExitCode != 0 || !File.Exists(masterPath))
                throw new InvalidOperationException($"ffmpeg falhou ao gerar o HLS (código {transcode.ExitCode}): {transcode.StderrTail}");

            var renditions = new List<VideoRendition>();

            foreach (var step in steps)
            {
                var playlist = Path.Combine(hlsDirectory, $"{step.ShortSide}p", "index.m3u8");
                var probe = await ProbeAsync(playlist, cancellationToken)
                    ?? throw new InvalidOperationException($"ffprobe não leu a rendição {step.ShortSide}p.");
                renditions.Add(new VideoRendition($"{step.ShortSide}p", probe.Width, probe.Height));
            }

            await NormalizeMasterAsync(masterPath, frameRate, cancellationToken);

            var posterPath = Path.Combine(outputDirectory, "poster.jpg");
            var posterSide = Math.Min(steps[^1].ShortSide, 720).ToString(CultureInfo.InvariantCulture);
            var poster = await RunAsync(_settings.FfmpegPath,
            [
                "-hide_banner", "-loglevel", "error", "-y",
                "-ss", source.DurationSeconds >= 2 ? "1" : "0",
                "-i", inputPath,
                "-frames:v", "1",
                "-vf", $"scale=w='if(gt(iw,ih),-2,{posterSide})':h='if(gt(iw,ih),{posterSide},-2)'",
                "-q:v", "3",
                posterPath,
            ], cancellationToken);

            var top = renditions[^1];

            return new TranscodedVideo(
                hlsDirectory,
                PlaylistFile,
                poster.ExitCode == 0 && File.Exists(posterPath) ? posterPath : null,
                top.Width,
                top.Height,
                (int)Math.Round(source.DurationSeconds, MidpointRounding.AwayFromZero),
                renditions);
        }

        private List<(int ShortSide, int BitrateKbps)> PlanLadder(int sourceShortSide)
        {
            var limit = Math.Min(sourceShortSide, _settings.MaxShortSide);
            var steps = Ladder.Where(step => step.ShortSide <= limit).ToList();

            if (steps.Count == 0)
                steps.Add((Math.Max(2, limit - limit % 2), Ladder[0].BitrateKbps));

            return steps;
        }

        private static async Task NormalizeMasterAsync(string masterPath, double frameRate, CancellationToken cancellationToken)
        {
            var lines = (await File.ReadAllLinesAsync(masterPath, cancellationToken))
                .Where(line => line.Length > 0)
                .ToArray();
            var attribute = $",FRAME-RATE={frameRate.ToString("0.000", CultureInfo.InvariantCulture)}";

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("#EXT-X-STREAM-INF:", StringComparison.Ordinal) && !lines[i].Contains("FRAME-RATE=", StringComparison.Ordinal))
                    lines[i] += attribute;
                else if (!lines[i].StartsWith('#'))
                    lines[i] = lines[i].Replace('\\', '/');
            }

            await File.WriteAllTextAsync(masterPath, string.Join('\n', lines) + '\n', cancellationToken);
        }

        private static bool IsQuarterTurn(JsonElement stream)
        {
            if (!stream.TryGetProperty("side_data_list", out var sideData))
                return false;

            foreach (var entry in sideData.EnumerateArray())
            {
                if (entry.TryGetProperty("rotation", out var rotation) && rotation.TryGetDouble(out var degrees))
                    return Math.Abs(Math.Abs(degrees) % 180 - 90) < 1;
            }

            return false;
        }

        private static double FrameRateOf(JsonElement stream)
        {
            if (!stream.TryGetProperty("avg_frame_rate", out var value) || value.GetString() is not { } text)
                return 0;

            var parts = text.Split('/');

            if (parts.Length == 2
                && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator)
                && denominator > 0)
                return numerator / denominator;

            return 0;
        }

        private async Task<ProcessRun> RunAsync(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = new Process { StartInfo = startInfo };

            try
            {
                process.Start();
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                throw new InvalidOperationException($"Não foi possível iniciar '{fileName}'. Instale o ffmpeg e confira Video:FfmpegPath e Video:FfprobePath.", ex);
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_settings.ProcessTimeout);

            var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var stderr = process.StandardError.ReadToEndAsync(timeout.Token);

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);

                if (cancellationToken.IsCancellationRequested)
                    throw;

                throw new TimeoutException($"'{Path.GetFileName(fileName)}' passou de {_settings.ProcessTimeout.TotalMinutes} minutos e foi encerrado.");
            }

            return new ProcessRun(process.ExitCode, await stdout, Tail(await stderr));
        }

        private static string Tail(string text)
        {
            var singleLine = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return singleLine.Length <= StderrTailLength ? singleLine : singleLine[^StderrTailLength..];
        }

        private sealed record ProcessRun(int ExitCode, string Stdout, string StderrTail);
    }
}
