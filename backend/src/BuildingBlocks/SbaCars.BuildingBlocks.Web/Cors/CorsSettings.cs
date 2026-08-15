namespace SbaCars.BuildingBlocks.Web.Cors;

/// <summary>
/// The <c>Cors</c> configuration section. Bound and validated with <c>ValidateOnStart</c>
/// (§4.4 of the architecture plan): a gateway that boots without a valid allow-list is a
/// misconfiguration, not something that should surface as a confusing browser error on the
/// first request.
/// </summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Absolute origins allowed to call this gateway (e.g. <c>http://localhost:5173</c>).
    /// Never hard-coded in application code — always sourced from configuration, so each
    /// environment lists its own SPA origins.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
