namespace SbaCars.BuildingBlocks.Observability;

/// <summary>
/// The three health-check tags every process in the topology uses to sort its registered checks
/// into <c>/health/live</c>, <c>/health/ready</c> and <c>/health/startup</c> (§8 of the
/// architecture plan) — the "convenções de health check" this project owns per §3.3. A check is
/// wired into an endpoint purely by carrying the matching tag; <see cref="SbaCarsHealthChecksExtensions"/>
/// is the only place that reads them.
/// </summary>
public static class HealthCheckTags
{
    /// <summary>Self only, no external dependency — "is this process' request pipeline alive".</summary>
    public const string Live = "live";

    /// <summary>
    /// Real dependencies this process needs before it can usefully receive traffic — PostgreSQL
    /// and the Logto JWKS discovery document where <c>AddSbaCarsAuth</c> is wired, plus (for a
    /// gateway) the readiness of the services it routes to.
    /// </summary>
    public const string Ready = "ready";

    /// <summary>Whether the process has finished booting — same self check as <see cref="Live"/> today.</summary>
    public const string Startup = "startup";
}
