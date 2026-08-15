using Microsoft.Extensions.Options;

namespace SbaCars.BuildingBlocks.Web.Auth;

/// <summary>
/// Validates <see cref="JwtSettings"/> beyond what <see cref="System.ComponentModel.DataAnnotations.RequiredAttribute"/>
/// can express: both <see cref="JwtSettings.Authority"/> and, when present,
/// <see cref="JwtSettings.MetadataAddress"/> must actually be absolute URIs — a typo here should
/// fail the host at boot (§4.4), not surface as a confusing token-validation failure on the first
/// request.
/// </summary>
internal sealed class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    public ValidateOptionsResult Validate(string? name, JwtSettings options)
    {
        if (!Uri.TryCreate(options.Authority, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail(
                $"{JwtSettings.SectionName}:{nameof(JwtSettings.Authority)} must be an absolute URI.");
        }

        if (options.MetadataAddress is not null && !Uri.TryCreate(options.MetadataAddress, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail(
                $"{JwtSettings.SectionName}:{nameof(JwtSettings.MetadataAddress)} must be an absolute URI when set.");
        }

        return ValidateOptionsResult.Success;
    }
}
