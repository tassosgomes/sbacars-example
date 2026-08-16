using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Messaging.Tests;

/// <summary>
/// Proves every rule <c>MessagingOptions</c>/<c>MessagingOptionsValidator</c> enforce (D6, §4.4,
/// §6.3.1): the DataAnnotations on <see cref="MessagingOptions"/> itself (required-ness, ranges) and
/// the connection-string/error-queue rules <see cref="MessagingOptionsValidator"/> adds on top,
/// exercised together through <see cref="Validate"/> — the same combination
/// <c>AddOptions&lt;MessagingOptions&gt;().ValidateDataAnnotations()</c> plus the registered
/// <c>IValidateOptions&lt;MessagingOptions&gt;</c> singleton produce for real at boot
/// (<c>MessagingServiceCollectionExtensions.AddSbaCarsMessaging</c>), without needing to build a full
/// host just to observe it.
/// </summary>
public sealed class MessagingOptionsValidatorTests
{
    [Theory]
    [InlineData("amqp://guest:guest@localhost:5672/")]
    [InlineData("amqps://guest:guest@localhost:5671/")]
    public void AcceptsAbsoluteAmqpAndAmqpsConnectionStrings(string connectionString)
    {
        var options = ValidOptions();
        options.ConnectionString = connectionString;

        var (succeeded, _) = Validate(options);

        succeeded.Should().BeTrue();
    }

    [Fact]
    public void RejectsAnEmptyConnectionString()
    {
        // §4.4: mandatory sections fail boot on a missing value — this is [Required], not
        // MessagingOptionsValidator's own connection-string rule (that rule only runs once the
        // string is non-empty; see the class remarks on why both are exercised together here).
        var options = ValidOptions();
        options.ConnectionString = string.Empty;

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error => error.Contains(nameof(MessagingOptions.ConnectionString), StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsAnEmptyInputQueueName()
    {
        var options = ValidOptions();
        options.InputQueueName = string.Empty;

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error => error.Contains(nameof(MessagingOptions.InputQueueName), StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsAConnectionStringThatIsNotAnAbsoluteUri_AndNamesTheSchemeItFound()
    {
        var options = ValidOptions();
        options.ConnectionString = "not-a-uri";

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        // The message is the product this validator exists to produce: it must say what was found,
        // not just that something was wrong.
        errors.Should().Contain(error =>
            error.Contains("not a valid absolute URI", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("http://localhost:5672/", "http")]
    [InlineData("postgres://localhost:5432/sbacars", "postgres")]
    public void RejectsAnUnsupportedScheme_AndNamesTheSchemeItFound(string connectionString, string scheme)
    {
        var options = ValidOptions();
        options.ConnectionString = connectionString;

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error => error.Contains($"Scheme found: '{scheme}'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("amqps://guest:guest@localhost:5671/?certValidationCallback=AcceptAny")]
    [InlineData("amqps://guest:guest@localhost:5671/?sslProtocol=none")]
    [InlineData("amqps://guest:guest@localhost:5671/?verify=verify_none")]
    [InlineData("amqps://guest:guest@localhost:5671/?verifyPeer=false")]
    // Case-insensitive match, as the D6 rule requires: a key spelled with different casing is the
    // same downgrade, not a different, unrecognized one.
    [InlineData("amqps://guest:guest@localhost:5671/?CertValidationCallback=AcceptAny")]
    public void RejectsAmqpsWithAQueryStringThatDisablesCertificateValidation(string connectionString)
    {
        var options = ValidOptions();
        options.ConnectionString = connectionString;

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error =>
            error.Contains("disables TLS certificate validation", StringComparison.Ordinal));
    }

    [Fact]
    public void DoesNotRejectTheSameQueryStringKeyOnPlainAmqp()
    {
        // §6.3.1's rule targets amqps:// specifically: it is what makes the TLS protection real to
        // defeat in the first place. The identical key on a local, already-unencrypted amqp:// has
        // nothing to downgrade — the validator, as implemented, only inspects amqps:// connection
        // strings, and this is the symmetric case the B1 test spec calls out explicitly.
        var options = ValidOptions();
        options.ConnectionString = "amqp://guest:guest@localhost:5672/?verify=verify_none";

        var (succeeded, _) = Validate(options);

        succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void RejectsMaxDeliveryAttemptsOutOfRange(int maxDeliveryAttempts)
    {
        var options = ValidOptions();
        options.MaxDeliveryAttempts = maxDeliveryAttempts;

        var (succeeded, _) = Validate(options);

        succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void RejectsPrefetchCountOutOfRange(int prefetchCount)
    {
        var options = ValidOptions();
        options.PrefetchCount = prefetchCount;

        var (succeeded, _) = Validate(options);

        succeeded.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void RejectsNumberOfWorkersOutOfRange(int numberOfWorkers)
    {
        var options = ValidOptions();
        options.NumberOfWorkers = numberOfWorkers;

        var (succeeded, _) = Validate(options);

        succeeded.Should().BeFalse();
    }

    [Fact]
    public void MissingErrorQueueName_ResolvesToInputQueueNamePlusDotError()
    {
        // §6.3's default: an environment that never sets Messaging:ErrorQueueName still gets a
        // working error queue, mechanically derived rather than requiring every deployment to spell
        // out a name that is fully determined by InputQueueName.
        var options = ValidOptions();
        options.InputQueueName = "inventory-service";
        options.ErrorQueueName = null;

        var (succeeded, _) = Validate(options);

        succeeded.Should().BeTrue();
        options.EffectiveErrorQueueName.Should().Be("inventory-service.error");
    }

    [Fact]
    public void EmptyErrorQueueNameWithEmptyInputQueueName_FailsNamingBothProperties()
    {
        var options = ValidOptions();
        options.InputQueueName = string.Empty;
        options.ErrorQueueName = string.Empty;

        var (succeeded, errors) = Validate(options);

        succeeded.Should().BeFalse();
        errors.Should().Contain(error =>
            error.Contains(nameof(MessagingOptions.ErrorQueueName), StringComparison.Ordinal) &&
            error.Contains(nameof(MessagingOptions.InputQueueName), StringComparison.Ordinal));
    }

    private static MessagingOptions ValidOptions() => new()
    {
        ConnectionString = "amqp://guest:guest@localhost:5672/",
        InputQueueName = "test-service",
    };

    /// <summary>
    /// Runs both validation stages a real boot runs — DataAnnotations first (what
    /// <c>ValidateDataAnnotations()</c> wires up), then <see cref="MessagingOptionsValidator"/> — and
    /// merges their failures into one list, mirroring how <c>Microsoft.Extensions.Options</c>
    /// aggregates every registered <c>IValidateOptions&lt;T&gt;</c> result for a single
    /// <c>IOptions&lt;T&gt;.Value</c> access.
    /// </summary>
    private static (bool Succeeded, IReadOnlyList<string> Errors) Validate(MessagingOptions options)
    {
        var dataAnnotationResults = new List<ValidationResult>();
        var dataAnnotationsSucceeded = Validator.TryValidateObject(
            options, new ValidationContext(options), dataAnnotationResults, validateAllProperties: true);

        var validatorResult = new MessagingOptionsValidator().Validate(name: null, options);

        var errors = dataAnnotationResults
            .Select(result => result.ErrorMessage ?? string.Empty)
            .Concat(validatorResult.Failed ? validatorResult.Failures : [])
            .ToArray();

        return (dataAnnotationsSucceeded && validatorResult.Succeeded, errors);
    }
}
