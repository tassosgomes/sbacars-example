using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Observability;

/// <summary>
/// Validates <see cref="ObservabilitySettings"/> beyond what a data-annotation attribute can
/// express: when present, <see cref="ObservabilitySettings.OtlpEndpoint"/> must actually be an
/// absolute URI. A typo here should fail the host at boot (§4.4), not surface as telemetry that
/// silently never leaves the process.
/// </summary>
internal sealed class ObservabilitySettingsValidator : IValidateOptions<ObservabilitySettings>
{
    public ValidateOptionsResult Validate(string? name, ObservabilitySettings options)
    {
        if (options.OtlpEndpoint is not null && !Uri.TryCreate(options.OtlpEndpoint, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail(
                $"{ObservabilitySettings.SectionName}:{nameof(ObservabilitySettings.OtlpEndpoint)} must be an absolute URI when set.");
        }

        return ValidateOptionsResult.Success;
    }
}
