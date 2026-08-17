using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace SbaCars.BuildingBlocks.Application.Cqrs;

/// <summary>Registers CQRS handlers, validators, the validation decorator and dispatcher.</summary>
public static class CqrsServiceCollectionExtensions
{
    public static IServiceCollection AddCqrs(
        this IServiceCollection services,
        params Assembly[] handlerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(handlerAssemblies);

        services.AddScoped<IDispatcher, Dispatcher>();

        foreach (var assembly in handlerAssemblies.Distinct())
        {
            RegisterValidators(services, assembly);
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    private static void RegisterValidators(IServiceCollection services, Assembly assembly)
    {
        var validatorTypes = assembly
            .DefinedTypes
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .Select(type => new
            {
                Type = type.AsType(),
                Interfaces = type.ImplementedInterfaces
                    .Where(@interface => @interface.IsGenericType &&
                        @interface.GetGenericTypeDefinition() == typeof(IValidator<>))
                    .ToArray(),
            })
            .Where(item => item.Interfaces.Length > 0);

        foreach (var item in validatorTypes)
        {
            services.AddScoped(item.Type);
            foreach (var validatorInterface in item.Interfaces)
            {
                services.AddScoped(validatorInterface, provider =>
                    provider.GetRequiredService(item.Type));
            }
        }
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly
            .DefinedTypes
            .Where(type => !type.IsAbstract && !type.IsInterface)
            .Select(type => new
            {
                Type = type.AsType(),
                CommandInterfaces = type.ImplementedInterfaces
                    .Where(@interface => @interface.IsGenericType &&
                        @interface.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
                    .ToArray(),
                QueryInterfaces = type.ImplementedInterfaces
                    .Where(@interface => @interface.IsGenericType &&
                        @interface.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
                    .ToArray(),
            })
            .Where(item => item.CommandInterfaces.Length > 0 || item.QueryInterfaces.Length > 0);

        foreach (var item in handlerTypes)
        {
            services.AddScoped(item.Type);

            foreach (var commandInterface in item.CommandInterfaces)
            {
                var arguments = commandInterface.GetGenericArguments();
                var commandType = arguments[0];
                var resultType = arguments[1];
                var decoratorType = typeof(ValidationCommandHandlerDecorator<,>)
                    .MakeGenericType(commandType, resultType);

                services.AddScoped(commandInterface, provider =>
                {
                    var inner = provider.GetRequiredService(item.Type);
                    var validatorType = typeof(IValidator<>).MakeGenericType(commandType);
                    var validator = provider.GetService(validatorType);
                    return Activator.CreateInstance(decoratorType, inner, validator)!;
                });
            }

            foreach (var queryInterface in item.QueryInterfaces)
            {
                var arguments = queryInterface.GetGenericArguments();
                var queryType = arguments[0];
                var resultType = arguments[1];
                var decoratorType = typeof(ValidationQueryHandlerDecorator<,>)
                    .MakeGenericType(queryType, resultType);

                services.AddScoped(queryInterface, provider =>
                {
                    var inner = provider.GetRequiredService(item.Type);
                    var validatorType = typeof(IValidator<>).MakeGenericType(queryType);
                    var validator = provider.GetService(validatorType);
                    return Activator.CreateInstance(decoratorType, inner, validator)!;
                });
            }
        }
    }
}
