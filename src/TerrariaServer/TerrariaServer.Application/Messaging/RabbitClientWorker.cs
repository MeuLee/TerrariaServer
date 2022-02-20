using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TerrariaServer.Application.Messaging;

// start rabbitmq locally here

// as for publisher, should be a separate class than listener.
// recreate the queue each time to submit a message. queues are created if they don't already exist anyway. queue name would be message type name
// can share ioptions between publisher and listener for hostname, port etc, all of which would have default values
internal class RabbitClientWorker : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		// other class.startlisteningonmessages
		return Task.CompletedTask;
	}
	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal class RabbitClientMessageConsumer { }
internal class RabbitClientMessageProducer { }

internal static class ServiceCollectionExtensions
{
	internal static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services)
		=> services.AddHostedService<RabbitClientWorker>()
			.AddSingleton<RabbitClientMessageConsumer>()
			.AddTransient<RabbitClientMessageProducer>();
}
