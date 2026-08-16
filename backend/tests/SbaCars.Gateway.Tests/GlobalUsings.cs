global using AwesomeAssertions;
global using Xunit;

// A8's OpenTelemetry ASP.NET Core instrumentation listens to the process-wide
// "Microsoft.AspNetCore" ActivitySource — a listener registered by one test class's WebApplication
// receives activities from *every* WebApplication host running in this process, not just its own.
// TraceContinuityTests and GatewayHealthAggregationTests both start real gateway hosts and assert
// on exactly which activities their own in-memory exporter captured; running test classes in
// parallel (xUnit's default) would let an unrelated class's request pollute those assertions.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
