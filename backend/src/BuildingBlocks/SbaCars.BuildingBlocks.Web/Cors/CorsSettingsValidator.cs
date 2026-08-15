using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Web.Cors;

/// <summary>
/// Validates <see cref="CorsSettings"/> beyond what a data-annotation attribute can express:
/// at least one origin, and every origin must actually be an absolute URI (a typo here would
/// otherwise silently produce a CORS policy that allows nothing, or fail confusingly at the
/// browser instead of at boot).
/// </summary>
internal sealed class CorsSettingsValidator : IValidateOptions<CorsSettings>
{
    public ValidateOptionsResult Validate(string? name, CorsSettings options)
    {
        if (options.AllowedOrigins.Length == 0)
        {
            return ValidateOptionsResult.Fail(
                $"{CorsSettings.SectionName}:{nameof(CorsSettings.AllowedOrigins)} must list at least one allowed origin.");
        }

        var invalidOrigins = options.AllowedOrigins
            .Where(origin => !Uri.TryCreate(origin, UriKind.Absolute, out _))
            .ToArray();

        return invalidOrigins.Length == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"{CorsSettings.SectionName}:{nameof(CorsSettings.AllowedOrigins)} contains invalid origin(s): " +
                string.Join(", ", invalidOrigins));
    }
}
