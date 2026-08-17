using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SbaCars.BuildingBlocks.Application.Cqrs;

namespace SbaCars.BuildingBlocks.UnitTests.Cqrs;

public sealed class DispatcherTests
{
    [Fact]
    public async Task SendAsync_ResolvesHandlerByConcreteCommandType()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var result = await dispatcher.SendAsync(new EchoCommand("hello"));

        result.Should().Be("handled:hello");
    }

    [Fact]
    public async Task SendAsync_ValidatesBeforeInvokingHandler()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
        var handler = scope.ServiceProvider.GetRequiredService<ValidatedCommandHandler>();

        var act = () => dispatcher.SendAsync(new ValidatedCommand(string.Empty));

        await act.Should().ThrowAsync<ValidationException>();
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task QueryAsync_ResolvesQueryHandlerByConcreteQueryType()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var result = await dispatcher.QueryAsync(new CurrentValueQuery());

        result.Should().Be("query-result");
    }

    [Fact]
    public async Task QueryAsync_ValidatesBeforeInvokingReadHandler()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
        var handler = scope.ServiceProvider.GetRequiredService<ValidatedQueryHandler>();

        var act = () => dispatcher.QueryAsync(new ValidatedQuery(string.Empty));

        await act.Should().ThrowAsync<ValidationException>();
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_WhenNoHandlerIsRegistered_ThrowsResolutionError()
    {
        await using var provider = BuildProvider();
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var act = () => dispatcher.SendAsync(new MissingHandlerCommand());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static ServiceProvider BuildProvider() => new ServiceCollection()
        .AddCqrs(typeof(DispatcherTests).Assembly)
        .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
}

public sealed record EchoCommand(string Value) : ICommand<string>;

public sealed class EchoCommandHandler : ICommandHandler<EchoCommand, string>
{
    public Task<string> HandleAsync(EchoCommand command, CancellationToken cancellationToken) =>
        Task.FromResult($"handled:{command.Value}");
}

public sealed record ValidatedCommand(string Value) : ICommand<string>;

public sealed class ValidatedCommandValidator : AbstractValidator<ValidatedCommand>
{
    public ValidatedCommandValidator()
    {
        RuleFor(command => command.Value).NotEmpty();
    }
}

public sealed class ValidatedCommandHandler : ICommandHandler<ValidatedCommand, string>
{
    public int Calls { get; private set; }

    public Task<string> HandleAsync(ValidatedCommand command, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult("validated");
    }
}

public sealed record CurrentValueQuery : IQuery<string>;

public sealed class CurrentValueQueryHandler : IQueryHandler<CurrentValueQuery, string>
{
    public Task<string> HandleAsync(CurrentValueQuery query, CancellationToken cancellationToken) =>
        Task.FromResult("query-result");
}

public sealed record ValidatedQuery(string Value) : IQuery<string>;

public sealed class ValidatedQueryValidator : AbstractValidator<ValidatedQuery>
{
    public ValidatedQueryValidator()
    {
        RuleFor(query => query.Value).NotEmpty();
    }
}

public sealed class ValidatedQueryHandler : IQueryHandler<ValidatedQuery, string>
{
    public int Calls { get; private set; }

    public Task<string> HandleAsync(ValidatedQuery query, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult("validated-query");
    }
}

public sealed record MissingHandlerCommand : ICommand<string>;
