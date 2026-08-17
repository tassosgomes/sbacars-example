using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Storage;

/// <summary>
/// Validates <see cref="StorageOptions"/> beyond what DataAnnotations can express (§4.4, §7).
/// </summary>
internal sealed class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            errors.Add(
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.ServiceUrl)} must not be empty or whitespace.");
        }
        else if (!Uri.TryCreate(options.ServiceUrl, UriKind.Absolute, out var serviceUri) ||
                 (serviceUri.Scheme != Uri.UriSchemeHttp && serviceUri.Scheme != Uri.UriSchemeHttps))
        {
            var foundScheme = serviceUri?.Scheme ?? "(not a valid absolute URI)";
            errors.Add(
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.ServiceUrl)} must be an " +
                $"absolute 'http://' or 'https://' URI. Scheme found: '{foundScheme}'.");
        }

        if (string.IsNullOrWhiteSpace(options.AccessKey))
        {
            errors.Add(
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.AccessKey)} must not be empty or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            errors.Add(
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.SecretKey)} must not be empty or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(options.BucketName))
        {
            errors.Add(
                $"{StorageOptions.SectionName}:{nameof(StorageOptions.BucketName)} must not be empty or whitespace.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
