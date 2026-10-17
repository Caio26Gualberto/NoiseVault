using Amazon;
using Amazon.MediaConvert;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleEmailV2;
using NoiseVault.Application.Interfaces;
using NoiseVault.Infra.Data.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NoiseVault.Infra.Data.Storage
{
    public static class StorageRegistration
    {
        public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration config)
        {
            var settings = StorageSettings.FromConfiguration(config);
            services.AddSingleton(settings);

            services.AddSingleton<IAmazonS3>(_ => CreateClient(settings));
            services.AddScoped<IFileStorage, S3FileStorage>();
            services.AddScoped<IImageProcessor, ImageSharpProcessor>();

            var video = VideoSettings.FromConfiguration(config);
            services.AddSingleton(video);
            services.AddScoped<IVideoTranscoder, FfmpegVideoTranscoder>();

            if (video.UsesMediaConvert)
            {
                services.AddSingleton<IAmazonMediaConvert>(_ => CreateMediaConvertClient(settings, video));
                services.AddScoped<IRemoteVideoTranscoder, MediaConvertVideoTranscoder>();
            }
            else
            {
                services.AddSingleton<IRemoteVideoTranscoder, DisabledRemoteVideoTranscoder>();
            }

            return services;
        }

        public static AmazonSimpleEmailServiceV2Client CreateSesClient(StorageSettings settings)
        {
            var clientConfig = new AmazonSimpleEmailServiceV2Config { RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region) };

            return settings.HasStaticCredentials
                ? new AmazonSimpleEmailServiceV2Client(new BasicAWSCredentials(settings.AccessKey, settings.SecretKey), clientConfig)
                : new AmazonSimpleEmailServiceV2Client(clientConfig);
        }

        private static AmazonMediaConvertClient CreateMediaConvertClient(StorageSettings settings, VideoSettings video)
        {
            var clientConfig = new AmazonMediaConvertConfig { RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region) };

            if (!string.IsNullOrWhiteSpace(video.MediaConvertEndpoint))
                clientConfig.ServiceURL = video.MediaConvertEndpoint;

            return settings.HasStaticCredentials
                ? new AmazonMediaConvertClient(new BasicAWSCredentials(settings.AccessKey, settings.SecretKey), clientConfig)
                : new AmazonMediaConvertClient(clientConfig);
        }

        private static AmazonS3Client CreateClient(StorageSettings settings)
        {
            var clientConfig = new AmazonS3Config();

            if (settings.UsesCustomEndpoint)
            {
                clientConfig.ServiceURL = settings.ServiceUrl;
                clientConfig.ForcePathStyle = true;
                clientConfig.AuthenticationRegion = settings.Region;
            }
            else
            {
                clientConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region);
            }

            return settings.HasStaticCredentials
                ? new AmazonS3Client(new BasicAWSCredentials(settings.AccessKey, settings.SecretKey), clientConfig)
                : new AmazonS3Client(clientConfig);
        }
    }
}
