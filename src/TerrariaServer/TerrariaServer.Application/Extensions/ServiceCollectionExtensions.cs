using Microsoft.Extensions.DependencyInjection;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Darker.AspNetCore;
using System.Reflection;
using TerrariaServer.Application.Features.Terraria.Shared;

namespace TerrariaServer.Application.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		var executingAssembly = Assembly.GetExecutingAssembly();
		services.AddSingleton<WorldService>();
		services.AddBrighter().AutoFromAssemblies(executingAssembly);
		services.AddDarker().AddHandlersFromAssemblies(executingAssembly);
		return services;
	}
}
