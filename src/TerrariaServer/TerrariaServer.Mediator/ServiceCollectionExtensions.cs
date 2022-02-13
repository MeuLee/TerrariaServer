using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace TerrariaServer.Application.Mediator;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
	{
		var requestHandlerTypes = assemblies.SelectMany(x => x.GetTypes())
			.Where(type => type.IsAssignableFrom(typeof(IAsyncRequestHandler<>))
				|| type.IsAssignableFrom(typeof(IAsyncRequestHandler<,>)));
		foreach (var requestHandlerType in requestHandlerTypes)
		{
			services.AddTransient(requestHandlerType);
		}
		services.AddSingleton<Mediator>();
		return services;
	}
}
