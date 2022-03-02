using Microsoft.Extensions.DependencyInjection;
using Paramore.Brighter;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.MessagingGateway.RMQ;
using Paramore.Darker.AspNetCore;
using System.Reflection;
using TerrariaServer.Application.Features.Terraria.Shared;
using TerrariaServer.Application.Features.Terraria.Vanilla;

namespace TerrariaServer.Application.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		var executingAssembly = Assembly.GetExecutingAssembly();
		services.AddSingleton<WorldService>();
		services.AddBrighterAndRmqMessaging(executingAssembly);
		services.AddDarker().AddHandlersFromAssemblies(executingAssembly);
		return services;
	}

	private static IServiceCollection AddBrighterAndRmqMessaging(this IServiceCollection services, Assembly assembly)
	{
		var rmqConnection = new RmqMessagingGatewayConnection
		{
			AmpqUri = new AmqpUriSpecification(new Uri("amqp://guest:guest@localhost:5672")), // credentials?
			Exchange = new Exchange(assembly.GetName().Name, supportDelay: true)
		};
		var supportedRequestHandlerTypes = new Type[] { typeof(RequestHandler<>), typeof(RequestHandlerAsync<>) };
		var requestHandlerTypes = assembly.GetTypes().Where(type => supportedRequestHandlerTypes
			.Any(requestHandlerType => type.InheritsFromGenericClass(requestHandlerType)));
		var rmqPublications = requestHandlerTypes.Select(ti => new RmqPublication
		{
			MaxOutStandingMessages = 5,
			MaxOutStandingCheckIntervalMilliSeconds = 500,
			WaitForConfirmsTimeOutInMilliseconds = 1000,
			MakeChannels = OnMissingChannel.Create,
			Topic = new RoutingKey(ti.Name)
		}).ToList();
		var producerRegistryFactory = new RmqProducerRegistryFactory(rmqConnection, rmqPublications);
		var producerRegistry = producerRegistryFactory.Create();

		services.AddBrighter().AutoFromAssemblies(assembly)
			.UseExternalBus(producerRegistry);
		return services;
	}

	private static bool InheritsFromGenericClass(this Type type, Type baseGenericClass)
	{
		if (type.BaseType is null || !type.BaseType.IsGenericType) return false;
		var baseGenericTypeDefinition = type.BaseType.GetGenericTypeDefinition();
		return !type.IsAbstract && !type.IsInterface&& type.BaseType.IsGenericType
			&& baseGenericTypeDefinition == baseGenericClass;
	}
}
