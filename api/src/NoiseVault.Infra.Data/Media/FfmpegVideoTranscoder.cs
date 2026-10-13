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
                "-show_entries", "format=duration:stream=codec_type,width,height",
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

                if (root.TryGetProperty("streams", out var streams))
                {
                    foreach (var stream in streams.EnumerateArray())
                    {
                        if (!stream.TryGetProperty("codec_type", out var type) || type.GetString() != "video")
                            continue;

                        var width = stream.TryGetProperty("width", out var w) ? w.GetInt32() : 0;
                        var height = stream.TryGetProperty("height", out var h) ? h.GetInt32() : 0;
                        return new VideoProbe(width > 0 && height > 0, duration, width, height);
                    }
                }

                return new VideoProbe(false, duration, 0, 0);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public async Task<TranscodedVideo> TranscodeAsync(string inputPath, string outputDirectory, CancellationToken cancellationToken = default)
        {
            var videoPath = Path.Combine(outputDirectory, "video.mp4");
            var posterPath = Path.Combine(outputDirectory, "poster.jpg");
            var side = _settings.ShortSideMax.ToString(CultureInfo.InvariantCulture);
            var scale = $"scale='if(gt(iw,ih),-2,min({side},iw))':'if(gt(iw,ih),min({side},ih),-2)'";

            var transcode = await RunAsync(_settings.FfmpegPath,
            [
                "-hide_banner", "-loglevel", "error", "-y",
                "-i", inputPath,
                "-map", "0:v:0", "-map", "0:a:0?",
                "-vf", scale,
                "-c:v", "libx264", "-preset", "veryfast", "-crf", _settings.Crf.ToString(CultureInfo.InvariantCulture),
                "-profile:v", "high", "-pix_fmt", "yuv420p",
                "-c:a", "aac", "-b:a", "128k", "-ac", "2",
                "-movflags", "+faststart",
                "-max_muxing_queue_size", "1024",
                videoPath,
            ], cancellationToken);

            if (transcode.ExitCode != 0 || !File.Exists(videoPath))
                throw new InvalidOperationException($"ffmpeg falhou ao converter o vídeo (código {transcode.ExitCode}): {transcode.StderrTail}");

            var output = await ProbeAsync(videoPath, cancellationToken)
                ?? throw new InvalidOperationException("ffprobe não leu o vídeo convertido.");

            var poster = await RunAsync(_settings.FfmpegPath,
            [
                "-hide_banner", "-loglevel", "error", "-y",
                "-ss", output.DurationSeconds >= 2 ? "1" : "0",
                "-i", videoPath,
                "-frames:v", "1",
                "-q:v", "3",
                posterPath,
            ], cancellationToken);

            return new TranscodedVideo(
                videoPath,
                poster.ExitCode == 0 && File.Exists(posterPath) ? posterPath : null,
                output.Width,
                output.Height,
                (int)Math.Round(output.DurationSeconds, MidpointRounding.AwayFromZero));
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
