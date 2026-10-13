using Amazon;
using Amazon.Runtime;
using Amazon.S3;
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

            return services;
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
