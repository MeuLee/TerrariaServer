using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text.Json;

namespace TerrariaServer.Application.Messaging;

// can share ioptions between publisher and listener for hostname, port etc, all of which would have default values
internal class RabbitClientWorker : IHostedService
{
	private readonly RabbitClientMessageConsumer _messageConsumer;

	public RabbitClientWorker(RabbitClientMessageConsumer messageConsumer)
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

internal interface IMessage { }

internal partial class RabbitClientMessageConsumer
{
	private readonly Dictionary<Type, AsyncEventingBasicConsumer> _supportedChannels;
	private readonly IConnection _connection;

	internal RabbitClientMessageConsumer(params Assembly[] assemblies)
	{
		_connection = new ConnectionFactory { HostName = "localhost" }.CreateConnection();
		_supportedChannels = GenerateRabbitQueues(_connection, assemblies);
	}

	internal void StartConsumingMessages()
	{
		foreach (var channel in _supportedChannels.Values)
		{
			channel.Received += ConsumeMessagesAsync;
		}
	}

	internal void StopConsumingMessages()
	{
		foreach (var channel in _supportedChannels.Values)
		{
			channel.Received -= ConsumeMessagesAsync;
		}
	}

	private async Task ConsumeMessagesAsync(object sender, BasicDeliverEventArgs serializedMessage)
	{
		var message = JsonSerializer.Deserialize<IMessage>(serializedMessage.Body.Span);
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

internal partial class RabbitClientMessageConsumer : IDisposable
{
	public void Dispose() => _connection.Dispose();
}

internal partial class RabbitClientMessageProducer
{
	private readonly IConnection _connection;

	public RabbitClientMessageProducer()
	{
		_connection = new ConnectionFactory { HostName = "localhost" }.CreateConnection();
	}

	internal void ProduceMessage(IMessage message)
	{
		using var channel = _connection.CreateModel();
		var queueName = message.GetType().Name;
		channel.QueueDeclare(queue: queueName);
		var body = JsonSerializer.SerializeToUtf8Bytes(message);
		channel.BasicPublish(string.Empty, string.Empty, null, body);
	}
}

internal partial class RabbitClientMessageProducer : IDisposable
{
	public void Dispose() => _connection.Dispose();
}

internal static class ServiceCollectionExtensions
{
	internal static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services)
		=> services.AddHostedService<RabbitClientWorker>()
			.AddSingleton(new RabbitClientMessageConsumer(Assembly.GetExecutingAssembly()))
			.AddSingleton<RabbitClientMessageProducer>();
}
