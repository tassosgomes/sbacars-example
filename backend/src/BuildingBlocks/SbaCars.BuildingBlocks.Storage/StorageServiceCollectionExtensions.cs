using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbaCars.BuildingBlocks.Application;

namespace SbaCars.BuildingBlocks.Storage;

/// <summary>
/// The single entry point every service will use to wire object storage (§7, C3) — registers
/// <see cref="StorageOptions"/>, <see cref="IAmazonS3"/>, and <see cref="IObjectStorage"/>.
/// </summary>
public static class StorageServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="StorageOptions"/> (bound and validated, <c>ValidateOnStart</c>), a
    /// singleton <see cref="IAmazonS3"/> configured for S3 or MinIO, and
    /// <see cref="IObjectStorage"/> → <see cref="S3ObjectStorage"/>.
    /// </summary>
    public static IServiceCollection AddSbaCarsStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            var serviceUri = new Uri(options.ServiceUrl);
            var config = new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = options.ForcePathStyle,
                AuthenticationRegion = options.Region,
                UseHttp = serviceUri.Scheme == Uri.UriSchemeHttp,
            };

            return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        });

        services.AddSingleton<IObjectStorage, S3ObjectStorage>();

        return services;
    }
}
