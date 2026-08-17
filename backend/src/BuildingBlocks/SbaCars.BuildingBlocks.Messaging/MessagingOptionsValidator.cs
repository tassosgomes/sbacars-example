using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Messaging;

/// <summary>
/// Validates <see cref="MessagingOptions"/> beyond what a data annotation can express (§4.4, §6.3.1).
/// Runs alongside <c>ValidateDataAnnotations()</c> on the same <c>AddOptions&lt;MessagingOptions&gt;()</c>
/// call — the required-ness of the individual properties is a DataAnnotations concern; everything
/// here is a rule about how those properties relate to each other or to the shape of a connection
/// string, which a single <c>[Attribute]</c> cannot express.
/// </summary>
internal sealed class MessagingOptionsValidator : IValidateOptions<MessagingOptions>
{
    /// <summary>
    /// Query-string keys that, on an <c>amqps://</c> connection string, disable certificate
    /// validation — §6.3.1 calls this "o erro clássico aqui": someone copies a snippet that
    /// disables validation to get past a self-signed cert in a sandbox, and it survives into a
    /// connection string that is otherwise a real, TLS-protected broker. Matched case-insensitively
    /// against both the key and, where the value itself is what disables validation (e.g.
    /// <c>verify=verify_none</c>), the value.
    /// </summary>
    private static readonly string[] TlsDowngradeQueryKeys =
    [
        "certvalidationcallback",
        "sslprotocol",
        "verify",
        "verifypeer",
    ];

    public ValidateOptionsResult Validate(string? name, MessagingOptions options)
    {
        var errors = new List<string>();

        Uri? connectionUri = null;
        if (!string.IsNullOrEmpty(options.ConnectionString))
        {
            if (!Uri.TryCreate(options.ConnectionString, UriKind.Absolute, out connectionUri) ||
                (connectionUri.Scheme != "amqp" && connectionUri.Scheme != "amqps"))
            {
                var foundScheme = connectionUri?.Scheme ?? "(not a valid absolute URI)";
                errors.Add(
                    $"{MessagingOptions.SectionName}:{nameof(MessagingOptions.ConnectionString)} must be an " +
                    $"absolute 'amqp://' or 'amqps://' URI. Scheme found: '{foundScheme}'.");
                connectionUri = null;
            }
        }

        // §6.3.1: amqps:// is what makes CloudAMQP's TLS protection real. A query string that
        // quietly turns certificate validation back off defeats it while still looking secure at a
        // glance — this is the boot-time check that turns that into a loud failure instead of an
        // incident.
        if (connectionUri is { Scheme: "amqps" })
        {
            var offendingKey = GetQueryStringKeys(connectionUri)
                .FirstOrDefault(key => TlsDowngradeQueryKeys.Contains(key, StringComparer.OrdinalIgnoreCase));

            if (offendingKey is not null)
            {
                errors.Add(
                    $"{MessagingOptions.SectionName}:{nameof(MessagingOptions.ConnectionString)} uses 'amqps://' " +
                    $"but its query string contains '{offendingKey}', which disables TLS certificate validation. " +
                    "This defeats the purpose of amqps:// — remove it.");
            }
        }

        // Rule 3: an explicitly-empty ErrorQueueName only has a fallback to derive from
        // (InputQueueName) when InputQueueName is itself set. If both are empty, name both in the
        // message so whoever reads it does not have to guess which one to fix.
        if (options.ErrorQueueName is { Length: 0 } && string.IsNullOrEmpty(options.InputQueueName))
        {
            errors.Add(
                $"{MessagingOptions.SectionName}:{nameof(MessagingOptions.ErrorQueueName)} was set to an empty " +
                $"string and {MessagingOptions.SectionName}:{nameof(MessagingOptions.InputQueueName)} is also " +
                "empty, so no error queue name can be derived. Set InputQueueName, or provide an explicit " +
                "ErrorQueueName.");
        }

        if (options.PurgeInterval <= TimeSpan.Zero)
        {
            errors.Add(
                $"{MessagingOptions.SectionName}:{nameof(MessagingOptions.PurgeInterval)} must be greater than zero.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    /// <summary>
    /// Hand-rolled query-string key extraction: this project intentionally does not reference
    /// <c>Microsoft.AspNetCore.App</c> (it is a plain library, not a web project — see the
    /// <c>.csproj</c>), so the usual <c>QueryHelpers</c>/<c>HttpUtility</c> helpers are not on the
    /// class path here. The parsing only needs to be good enough to catch the query keys in
    /// <see cref="TlsDowngradeQueryKeys"/>; it does not need to be a general-purpose URI parser.
    /// </summary>
    private static IEnumerable<string> GetQueryStringKeys(Uri uri)
    {
        var query = uri.Query.TrimStart('?');

        if (query.Length == 0)
        {
            yield break;
        }

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=');
            var key = separatorIndex < 0 ? pair : pair[..separatorIndex];

            yield return Uri.UnescapeDataString(key);
        }
    }
}
