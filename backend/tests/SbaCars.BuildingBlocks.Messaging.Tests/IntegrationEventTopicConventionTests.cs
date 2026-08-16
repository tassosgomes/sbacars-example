using SbaCars.BuildingBlocks.Messaging.Topology;

namespace SbaCars.BuildingBlocks.Messaging.Tests;

/// <summary>
/// Proves D4: a Rebus topic name comes from a type's <c>[IntegrationEvent]</c> attribute, never from
/// its .NET type name. Without this, Rebus' own <c>DefaultTopicNameConvention</c> would fall back to
/// the type's short assembly-qualified name, and renaming a C# class would silently change what a
/// consumer's binding routes on.
/// </summary>
public sealed class IntegrationEventTopicConventionTests
{
    [Fact]
    public void ATypeDecoratedWithIntegrationEvent_ResolvesToExactlyTheAttributesName()
    {
        var convention = new IntegrationEventTopicConvention();

        var topic = convention.GetTopic(typeof(ProbeEvent));

        topic.Should().Be("test.probe");
    }

    [Fact]
    public void ATypeWithNoIntegrationEventAttribute_Throws_NamingTheTypeAndWhatToDoAboutIt()
    {
        // This is the test that matters most in this file (D4): without it, an unattributed type
        // would silently fall back to Rebus' DefaultTopicNameConvention and route by C# type name
        // instead of failing loudly at the point where the mistake was made.
        var convention = new IntegrationEventTopicConvention();

        var act = () => convention.GetTopic(typeof(UnattributedEvent));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{typeof(UnattributedEvent).FullName}*")
            .Which.Message.Should().Contain("[IntegrationEvent(");
    }

    [Fact]
    public void RenamingTheCSharpClass_DoesNotChangeTheTopic()
    {
        // ProbeEvent and RenamedProbeEventWithADifferentClassName carry the exact same
        // [IntegrationEvent("test.probe")] attribute under deliberately different type names — the
        // topic must be identical for both, proving the wire name tracks the attribute, not the
        // class identity.
        var convention = new IntegrationEventTopicConvention();

        var topicForOriginalName = convention.GetTopic(typeof(ProbeEvent));
        var topicForRenamedType = convention.GetTopic(typeof(RenamedProbeEventWithADifferentClassName));

        topicForRenamedType.Should().Be(topicForOriginalName);
    }

    [Fact]
    public void TheResolvedTopic_ContainsNeitherTheTypeNameNorTheAssemblyName()
    {
        var convention = new IntegrationEventTopicConvention();

        var topic = convention.GetTopic(typeof(ProbeEvent));

        topic.Should().NotContain(nameof(ProbeEvent));
        topic.Should().NotContain(typeof(ProbeEvent).Assembly.GetName().Name!);
    }
}
