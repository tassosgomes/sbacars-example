using SbaCars.Contracts;

namespace SbaCars.BuildingBlocks.Messaging.Tests;

/// <summary>
/// A well-formed test integration event — deliberately not a business event (those are B4 work; see
/// <c>B1-TEST-SPEC.md</c>). Only its wire name (<c>"test.probe"</c>) matters to the steps under test.
/// </summary>
[IntegrationEvent("test.probe")]
internal sealed class ProbeEvent;

/// <summary>
/// Carries the exact same <c>[IntegrationEvent("test.probe")]</c> attribute as <see cref="ProbeEvent"/>
/// under a deliberately different C# type name — used to prove
/// <c>IntegrationEventTopicConvention</c> resolves the topic from the attribute, never from the type
/// name, so renaming a class cannot silently change what a subscriber is bound to (D4).
/// </summary>
[IntegrationEvent("test.probe")]
internal sealed class RenamedProbeEventWithADifferentClassName;

/// <summary>A message type with no <see cref="IntegrationEventAttribute"/> at all — the case every "no attribute" test in this project exists to cover.</summary>
internal sealed class UnattributedEvent;
