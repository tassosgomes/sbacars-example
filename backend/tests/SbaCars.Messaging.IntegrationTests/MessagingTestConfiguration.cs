using Microsoft.Extensions.Configuration;

namespace SbaCars.Messaging.IntegrationTests;

/// <summary>
/// Builds the <c>Messaging</c> configuration section every test in this assembly binds
/// <c>AddSbaCarsMessaging</c> against, pointed at <see cref="SbaCarsRabbitMqFixture"/>'s real
/// container.
/// </summary>
internal static class MessagingTestConfiguration
{
    public static IConfiguration Build(
        SbaCarsRabbitMqFixture fixture, string inputQueueName, IDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Messaging:ConnectionString"] = fixture.AmqpConnectionString,
            ["Messaging:InputQueueName"] = inputQueueName,
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                values[key] = value;
            }
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    /// <summary>
    /// A queue name unique to a single test run, so tests sharing the collection's one container
    /// (see <see cref="SbaCarsRabbitMqCollection"/>) never contend over the same input/error queue.
    /// </summary>
    public static string UniqueQueueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 13, 40)];
}
