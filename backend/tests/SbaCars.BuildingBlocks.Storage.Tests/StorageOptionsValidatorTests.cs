using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SbaCars.BuildingBlocks.Storage;

namespace SbaCars.BuildingBlocks.Storage.Tests;

/// <summary>
/// Proves every rule <see cref="StorageOptions"/> / <see cref="StorageOptionsValidator"/> enforce
/// (§4.4, §7), including <c>ValidateOnStart</c> at boot.
/// </summary>
public sealed class StorageOptionsValidatorTests
{
    [Theory]
    [InlineData("http://localhost:9000")]
    [InlineData("https://s3.amazonaws.com")]
    public void AcceptsAbsoluteHttpAndHttpsServiceUrls(string serviceUrl)
    {
        var options = ValidOptions();
        options.ServiceUrl = serviceUrl;

        var (succeeded, _) = Validate(options);

        succeeded.Should().BeTrue();
    }

    [Fact]
    public void RejectsAnEmptyServiceUrl()
    {
        var options = ValidOptions();
        options.ServiceUrl = string.Empty;

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error => error.Contains(nameof(StorageOptions.ServiceUrl), StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsWhitespaceOnlyAccessKey()
    {
        var options = ValidOptions();
        options.AccessKey = "   ";

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error => error.Contains(nameof(StorageOptions.AccessKey), StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsWhitespaceOnlySecretKey()
    {
        var options = ValidOptions();
        options.SecretKey = "   ";

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error => error.Contains(nameof(StorageOptions.SecretKey), StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsAnEmptyBucketName()
    {
        var options = ValidOptions();
        options.BucketName = string.Empty;

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error => error.Contains(nameof(StorageOptions.BucketName), StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsWhitespaceOnlyBucketName()
    {
        var options = ValidOptions();
        options.BucketName = "   ";

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error => error.Contains(nameof(StorageOptions.BucketName), StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsAServiceUrlThatIsNotAnAbsoluteUri_AndNamesTheSchemeItFound()
    {
        var options = ValidOptions();
        options.ServiceUrl = "not-a-uri";

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error =>
            error.Contains("not a valid absolute URI", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("ftp://localhost:9000", "ftp")]
    [InlineData("postgres://localhost:5432/sbacars", "postgres")]
    public void RejectsAnUnsupportedScheme_AndNamesTheSchemeItFound(string serviceUrl, string scheme)
    {
        var options = ValidOptions();
        options.ServiceUrl = serviceUrl;

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error => error.Contains($"Scheme found: '{scheme}'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1441)]
    public void RejectsUploadUrlLifetimeOutOfRange(int uploadUrlLifetimeMinutes)
    {
        var options = ValidOptions();
        options.UploadUrlLifetimeMinutes = uploadUrlLifetimeMinutes;

        var (succeeded, _) = Validate(options);

        succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1441)]
    public void RejectsDownloadUrlLifetimeOutOfRange(int downloadUrlLifetimeMinutes)
    {
        var options = ValidOptions();
        options.DownloadUrlLifetimeMinutes = downloadUrlLifetimeMinutes;

        var (succeeded, _) = Validate(options);

        succeeded.Should().BeFalse();
    }

    [Fact]
    public void MissingStorageSection_FailsValidation()
    {
        var options = new StorageOptions();

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidConfiguration_LetsServiceProviderStart()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ServiceUrl"] = "http://localhost:9000",
                ["Storage:AccessKey"] = "minioadmin",
                ["Storage:SecretKey"] = "minioadmin",
                ["Storage:BucketName"] = "sbacars-catalog-media",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSbaCarsStorage(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        provider.GetRequiredService<IOptions<StorageOptions>>().Value.ServiceUrl
            .Should().Be("http://localhost:9000");
    }

    private static StorageOptions ValidOptions() => new()
    {
        ServiceUrl = "http://localhost:9000",
        AccessKey = "minioadmin",
        SecretKey = "minioadmin",
        BucketName = "sbacars-catalog-media",
    };

    private static (bool Succeeded, IReadOnlyList<string> Errors) Validate(StorageOptions options)
    {
        var dataAnnotationResults = new List<ValidationResult>();
        var dataAnnotationsSucceeded = Validator.TryValidateObject(
            options, new ValidationContext(options), dataAnnotationResults, validateAllProperties: true);

        var validatorResult = new StorageOptionsValidator().Validate(name: null, options);

        var errors = dataAnnotationResults
            .Select(result => result.ErrorMessage ?? string.Empty)
            .Concat(validatorResult.Failed ? validatorResult.Failures : [])
            .ToArray();

        return (dataAnnotationsSucceeded && validatorResult.Succeeded, errors);
    }
}
