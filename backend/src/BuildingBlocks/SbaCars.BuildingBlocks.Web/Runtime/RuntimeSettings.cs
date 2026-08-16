using System.ComponentModel.DataAnnotations;

namespace SbaCars.BuildingBlocks.Web.Runtime;

/// <summary>
/// The <c>Runtime</c> configuration section (§8, "Runtime" row): the default request timeout every
/// endpoint gets unless it opts into something more specific, and how long the host waits for
/// in-flight work to finish on shutdown before forcing it. Both ship with sane defaults, validated
/// with <c>ValidateOnStart</c> like every other section in this codebase (§4.4).
/// </summary>
public sealed class RuntimeSettings
{
    public const string SectionName = "Runtime";

    /// <summary>
    /// Default timeout applied to every request that does not carry a more specific
    /// <c>[RequestTimeout]</c> policy. 30s matches the timeout YARP's clusters already set on the
    /// gateway side of the same call (§8) — a request that is not done in 30s at the gateway is not
    /// going to be waited on longer once it reaches the service behind it either.
    /// </summary>
    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// How long <see cref="Microsoft.Extensions.Hosting.HostOptions.ShutdownTimeout"/> waits for a
    /// request in flight or a graceful drain to finish before the host forces a shutdown — the
    /// <c>stop_grace_period</c> concern §11.6 assigns to the deploy manifest has a counterpart here
    /// in-process, so a `docker stop`/SIGTERM does not cut off a request that was seconds from
    /// completing.
    /// </summary>
    [Range(1, 120)]
    public int GracefulShutdownSeconds { get; set; } = 30;
}
