using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TerrariaServer.Application.Features.Terraria.Shared;
using TerrariaServer.Application.Messaging;

namespace TerrariaServer.Application.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
		=> services.AddSingleton<WorldService>()
			.AddMediatR(Assembly.GetExecutingAssembly())
			.AddRabbitMqMessaging();
}
