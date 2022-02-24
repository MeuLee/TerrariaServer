using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text.Json;

namespace TerrariaServer.Application.Messaging;

internal interface IMessage { }

internal interface IRabbitClientMessageConsumer
{
	void StartConsumingMessages();
	void StopConsumingMessages();
}

internal interface IRabbitClientMessageProducer
{
	void ProduceMessage(IMessage message);
}

// can share ioptions between publisher and listener for hostname, port etc, all of which would have default values
internal class RabbitClientWorker : IHostedService
{
	private readonly IRabbitClientMessageConsumer _messageConsumer;

	public RabbitClientWorker(IRabbitClientMessageConsumer messageConsumer)
	{
		_messageConsumer = messageConsumer;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_messageConsumer.StartConsumingMessages();
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_messageConsumer.StopConsumingMessages();
		return Task.CompletedTask;
	}
}

internal partial class RabbitClientMessageManager : IRabbitClientMessageConsumer
{
	private readonly Dictionary<Type, AsyncEventingBasicConsumer> _supportedChannels;
	private readonly IConnection _connection;

	internal RabbitClientMessageManager(params Assembly[] assemblies)
	{
		_connection = new ConnectionFactory { HostName = "localhost" }.CreateConnection();
		_supportedChannels = GenerateRabbitQueues(_connection, assemblies);
	}

	public void StartConsumingMessages()
	{
		foreach (var channel in _supportedChannels.Values)
		{
			channel.Received += ConsumeMessagesAsync;
		}
	}

	public void StopConsumingMessages()
	{
		foreach (var channel in _supportedChannels.Values)
		{
			channel.Received -= ConsumeMessagesAsync;
		}
	}

	private Task ConsumeMessagesAsync(object sender, BasicDeliverEventArgs serializedMessage)
	{
		var message = JsonSerializer.Deserialize<IMessage>(serializedMessage.Body.Span);
		// dispatch the message to mediatr handler
		// ack the message once mediator send completes
		return Task.CompletedTask;
	}

	private static Dictionary<Type, AsyncEventingBasicConsumer> GenerateRabbitQueues(IConnection connection, params Assembly[] assemblies)
		=> assemblies
			.Distinct()
			.SelectMany(x => x.GetTypes())
			.Where(x => typeof(IMessage).IsAssignableFrom(x) && x.IsClass && !x.IsAbstract)
			.ToDictionary(keySelector: x => x, elementSelector: x =>
			{
				var channel = connection.CreateModel();
				channel.QueueDeclare(queue: x.Name);
				return new AsyncEventingBasicConsumer(channel);
			});
}

internal partial class RabbitClientMessageManager : IRabbitClientMessageProducer
{ 
	public void ProduceMessage(IMessage message)
	{
		using var channel = _connection.CreateModel();
		var queueName = message.GetType().Name;
		channel.QueueDeclare(queue: queueName);
		var body = JsonSerializer.SerializeToUtf8Bytes(message);
		channel.BasicPublish(string.Empty, string.Empty, null, body);
		// wait until message is acked by consumer
	}
}

internal partial class RabbitClientMessageManager : IDisposable
{
	public void Dispose() => _connection.Dispose();
}

internal static class ServiceCollectionExtensions
{
	internal static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services)
		=> services.AddHostedService<RabbitClientWorker>()
			.AddSingleton(new RabbitClientMessageManager(Assembly.GetExecutingAssembly()))
			.AddSingleton<IRabbitClientMessageConsumer>(serviceProvider => serviceProvider.GetRequiredService<RabbitClientMessageManager>())
			.AddSingleton<IRabbitClientMessageProducer>(serviceProvider => serviceProvider.GetRequiredService<RabbitClientMessageManager>());
}
