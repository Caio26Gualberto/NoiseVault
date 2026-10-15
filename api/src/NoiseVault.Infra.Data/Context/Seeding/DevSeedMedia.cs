using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using NoiseVault.Application.Abstractions.Logging;
using NoiseVault.Application.Common.Media;
using NoiseVault.Application.Interfaces;
using NoiseVault.Infra.Data.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NoiseVault.Infra.Data.Context.Seeding
{
    public class DevSeedMedia
    {
        private const int VideoSeconds = 6;
        private const int UploadConcurrency = 4;
        private const string AvatarsPool = "avatars";
        private const string PhotosPool = "photos";
        private const string VideosPool = "videos";

        private readonly IFileStorage _fileStorage;
        private readonly IImageProcessor _imageProcessor;
        private readonly IVideoTranscoder _videoTranscoder;
        private readonly VideoSettings _videoSettings;
        private readonly IApplicationLogger _logger;
        private readonly string _mediaRoot;
        private readonly HashSet<string> _missingPools = [];

        public DevSeedMedia(
            IFileStorage fileStorage,
            IImageProcessor imageProcessor,
            IVideoTranscoder videoTranscoder,
            VideoSettings videoSettings,
            IConfiguration configuration,
            IApplicationLoggerFactory loggerFactory)
        {
            _fileStorage = fileStorage;
            _imageProcessor = imageProcessor;
            _videoTranscoder = videoTranscoder;
            _videoSettings = videoSettings;
            _logger = loggerFactory.Create(LogContext.Storage);
            _mediaRoot = configuration["Seed:MediaPath"] is { Length: > 0 } configured
                ? configured
                : DefaultMediaRoot();
        }

        public static string DefaultMediaRoot() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NoiseVault", "seed-media");

        public static string ImageKey(string folder, string name) => $"{folder}/seed-{name}.webp";

        public static string VideoFolder(int ownerUserId, string name) => $"users/{ownerUserId}/videos/seed-{name}";

        public static string VideoKey(string folder) => $"{folder}/master.m3u8";

        public static string PosterKey(string folder) => $"{folder}/poster.webp";

        public static (int Width, int Height) ImageSize(string key) => SizeOf(PresetOf(key), key);

        public async Task<(int Width, int Height)?> EnsureImageAsync(string key, CancellationToken cancellationToken = default)
        {
            if (await _fileStorage.SizeAsync(key, cancellationToken) is > 0)
                return null;

            var preset = PresetOf(key);
            await using var source = await OpenImageSourceAsync(key, preset, cancellationToken);
            var processed = await _imageProcessor.ProcessAsync(source, preset, cancellationToken);

            if (!processed.Succeeded)
                throw new InvalidOperationException($"O seed não conseguiu gerar a imagem '{key}' ({processed.Rejection}).");

            var image = processed.Image!;

            if (!await _fileStorage.PutAsync(key, image.Content, image.ContentType, cancellationToken))
                throw new InvalidOperationException($"O seed não conseguiu subir a imagem '{key}' para o storage.");

            return (image.Width, image.Height);
        }

        public async Task<(int Width, int Height, int DurationSeconds, long SizeBytes)?> EnsureVideoAsync(string folder, CancellationToken cancellationToken = default)
        {
            var masterKey = VideoKey(folder);

            if (await _fileStorage.SizeAsync(masterKey, cancellationToken) is > 0)
                return null;

            var workDirectory = Path.Combine(Path.GetTempPath(), "noisevault-seed", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workDirectory);

            try
            {
                var sourcePath = PickFromPool(VideosPool, folder, "*.mp4");

                if (sourcePath == null)
                {
                    sourcePath = Path.Combine(workDirectory, "source.mp4");
                    await GenerateSourceVideoAsync(folder, sourcePath, cancellationToken);
                }

                var video = await _videoTranscoder.TranscodeAsync(sourcePath, workDirectory, cancellationToken);
                var size = await UploadDirectoryAsync(video.OutputDirectory, folder, cancellationToken);

                if (video.PosterPath != null)
                {
                    await using var posterStream = File.OpenRead(video.PosterPath);
                    var poster = await _imageProcessor.ProcessAsync(posterStream, EImagePreset.Post, cancellationToken);

                    if (poster.Succeeded)
                        await _fileStorage.PutAsync(PosterKey(folder), poster.Image!.Content, poster.Image.ContentType, cancellationToken);
                }

                return (video.Width, video.Height, video.DurationSeconds, size);
            }
            finally
            {
                try
                {
                    if (Directory.Exists(workDirectory))
                        Directory.Delete(workDirectory, recursive: true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.Warning("SEED_TEMP_CLEANUP_FAILED", "Temporary seed folder was left behind", extra: new { Path = workDirectory, ex.Message });
                }
            }
        }

        private async Task<Stream> OpenImageSourceAsync(string key, EImagePreset preset, CancellationToken cancellationToken)
        {
            var pool = preset == EImagePreset.Avatar && key.StartsWith("users/", StringComparison.Ordinal) ? AvatarsPool : PhotosPool;
            var path = PickFromPool(pool, key, "*.jpg", file => FitsPreset(file, preset));

            if (path != null)
                return File.OpenRead(path);

            var (width, height) = SizeOf(preset, key);
            using var rendered = Render(key, width, height);
            var png = new MemoryStream();
            await rendered.SaveAsPngAsync(png, cancellationToken);
            png.Position = 0;
            return png;
        }

        private static bool FitsPreset(string path, EImagePreset preset)
        {
            var (minWidth, minHeight) = preset switch
            {
                EImagePreset.Avatar => (128, 128),
                EImagePreset.Banner => (800, 1),
                EImagePreset.Flyer => (300, 300),
                _ => (64, 64),
            };

            try
            {
                var info = Image.Identify(path);
                return info.Width >= minWidth && info.Height >= minHeight;
            }
            catch (Exception ex) when (ex is IOException or UnknownImageFormatException or InvalidImageContentException)
            {
                return false;
            }
        }

        private string? PickFromPool(string pool, string key, string pattern, Func<string, bool>? accept = null)
        {
            var directory = Path.Combine(_mediaRoot, pool);
            var files = Directory.Exists(directory)
                ? Directory.GetFiles(directory, pattern).Where(f => accept == null || accept(f)).OrderBy(f => f, StringComparer.Ordinal).ToArray()
                : [];

            if (files.Length == 0)
            {
                if (_missingPools.Add(pool))
                    _logger.Warning("DEV_SEED_MEDIA_POOL_MISSING", "Seed media pool is empty; falling back to generated placeholders. Run api/scripts/baixar-midia-seed.ps1",
                        extra: new { Pool = pool, Path = directory });

                return null;
            }

            var named = files.FirstOrDefault(f => string.Equals(Path.GetFileNameWithoutExtension(f), SeedNameOf(key), StringComparison.OrdinalIgnoreCase));

            if (named != null)
                return named;

            var hash = Hash(key);
            var index = BitConverter.ToUInt32(hash, 0) % (uint)files.Length;
            return files[index];
        }

        private async Task GenerateSourceVideoAsync(string folder, string outputPath, CancellationToken cancellationToken)
        {
            var hash = Hash(folder);
            var hue = hash[0] * 360 / 256;
            var (r, g, b) = Hsl(hue, 0.7, 0.35);
            var background = $"0x{(int)r:X2}{(int)g:X2}{(int)b:X2}";
            var tone = 110 + hash[1] % 220;

            var arguments = new[]
            {
                "-hide_banner", "-loglevel", "error", "-y",
                "-f", "lavfi", "-i", $"testsrc2=size=1280x720:rate=30:duration={VideoSeconds}",
                "-f", "lavfi", "-i", $"sine=frequency={tone}:duration={VideoSeconds}",
                "-filter_complex", $"[0:v]hue=h={hue}:s=1.4,drawbox=x=0:y=0:w=iw:h=ih:color={background}@0.35:t=fill[v]",
                "-map", "[v]", "-map", "1:a",
                "-c:v", "libx264", "-preset", "veryfast", "-pix_fmt", "yuv420p",
                "-c:a", "aac", "-b:a", "96k",
                "-shortest",
                outputPath,
            };

            var startInfo = new ProcessStartInfo(_videoSettings.FfmpegPath)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Não foi possível iniciar o ffmpeg para gerar o vídeo do seed.");

            var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || !File.Exists(outputPath))
                throw new InvalidOperationException($"ffmpeg falhou ao gerar o vídeo do seed (código {process.ExitCode}): {(await stderr).Trim()}");

            await stdout;
        }

        private async Task<long> UploadDirectoryAsync(string directory, string folder, CancellationToken cancellationToken)
        {
            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
            var total = 0L;

            foreach (var batch in files.Chunk(UploadConcurrency))
            {
                var uploads = batch.Select(async path =>
                {
                    var relative = Path.GetRelativePath(directory, path).Replace(Path.DirectorySeparatorChar, '/');
                    var uploaded = await _fileStorage.PutFileAsync($"{folder}/{relative}", path, ContentTypeOf(path), cancellationToken);
                    return (uploaded, new FileInfo(path).Length);
                });

                foreach (var (uploaded, length) in await Task.WhenAll(uploads))
                {
                    if (!uploaded)
                        throw new InvalidOperationException($"O seed não conseguiu subir o vídeo '{folder}' para o storage.");

                    total += length;
                }
            }

            return total;
        }

        private static string ContentTypeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".m3u8" => "application/vnd.apple.mpegurl",
            ".ts" => "video/mp2t",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream",
        };

        private static EImagePreset PresetOf(string key)
        {
            if (key.Contains("/avatar/", StringComparison.Ordinal))
                return EImagePreset.Avatar;
            if (key.Contains("/banner/", StringComparison.Ordinal))
                return EImagePreset.Banner;
            if (key.Contains("/musician/", StringComparison.Ordinal))
                return EImagePreset.Flyer;
            if (key.Contains("/seed-archive-", StringComparison.Ordinal))
                return EImagePreset.Archive;
            return EImagePreset.Post;
        }

        private static (int Width, int Height) SizeOf(EImagePreset preset, string key)
        {
            var hash = Hash(key);

            return preset switch
            {
                EImagePreset.Avatar => (512, 512),
                EImagePreset.Banner => (1600, 600),
                EImagePreset.Flyer => (900, 1200),
                _ => (hash[2] % 3) switch
                {
                    0 => (1600, 1200),
                    1 => (1200, 1600),
                    _ => (1600, 900),
                },
            };
        }

        private static Image<Rgba32> Render(string key, int width, int height)
        {
            var hash = Hash(key);
            var hueA = hash[0] * 360 / 256;
            var hueB = (hueA + 40 + hash[1] % 120) % 360;
            var (ra, ga, ba) = Hsl(hueA, 0.55, 0.22);
            var (rb, gb, bb) = Hsl(hueB, 0.65, 0.48);
            var angle = (hash[3] % 180) * Math.PI / 180;
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var stripe = 24 + hash[4] % 40;
            var circles = Enumerable.Range(0, 3 + hash[5] % 4)
                .Select(i => (
                    X: hash[(6 + i * 3) % hash.Length] * width / 256,
                    Y: hash[(7 + i * 3) % hash.Length] * height / 256,
                    R: Math.Max(width, height) * (8 + hash[(8 + i * 3) % hash.Length] % 20) / 100))
                .ToList();
            var noise = new Random(BitConverter.ToInt32(hash, 12));

            var image = new Image<Rgba32>(width, height);

            image.ProcessPixelRows(accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);

                    for (var x = 0; x < row.Length; x++)
                    {
                        var t = ((x * cos + y * sin) / (width * Math.Abs(cos) + height * Math.Abs(sin)));
                        t = Math.Clamp(t, 0, 1);

                        var r = ra + (rb - ra) * t;
                        var g = ga + (gb - ga) * t;
                        var b = ba + (bb - ba) * t;

                        foreach (var circle in circles)
                        {
                            var dx = x - circle.X;
                            var dy = y - circle.Y;
                            var d = Math.Sqrt(dx * dx + dy * dy);

                            if (d < circle.R)
                            {
                                var k = 0.35 * (1 - d / circle.R);
                                r += (255 - r) * k;
                                g += (255 - g) * k;
                                b += (255 - b) * k;
                            }
                        }

                        if (((x + y) / stripe) % 2 == 0)
                        {
                            r *= 0.88;
                            g *= 0.88;
                            b *= 0.88;
                        }

                        var grain = noise.Next(-14, 15);

                        row[x] = new Rgba32(
                            (byte)Math.Clamp(r + grain, 0, 255),
                            (byte)Math.Clamp(g + grain, 0, 255),
                            (byte)Math.Clamp(b + grain, 0, 255),
                            255);
                    }
                }
            });

            return image;
        }

        private static string SeedNameOf(string key)
        {
            var segment = key.Split('/').LastOrDefault(s => s.StartsWith("seed-", StringComparison.Ordinal));
            return segment == null ? string.Empty : Path.GetFileNameWithoutExtension(segment)["seed-".Length..];
        }

        private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

        private static (double R, double G, double B) Hsl(double hue, double saturation, double lightness)
        {
            var c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
            var h = hue / 60;
            var x = c * (1 - Math.Abs(h % 2 - 1));
            var m = lightness - c / 2;

            var (r, g, b) = (int)Math.Floor(h) switch
            {
                0 => (c, x, 0.0),
                1 => (x, c, 0.0),
                2 => (0.0, c, x),
                3 => (0.0, x, c),
                4 => (x, 0.0, c),
                _ => (c, 0.0, x),
            };

            return ((r + m) * 255, (g + m) * 255, (b + m) * 255);
        }
    }
}
