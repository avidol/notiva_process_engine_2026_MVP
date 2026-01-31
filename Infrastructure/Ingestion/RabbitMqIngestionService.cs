using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ProcessEngine.Worker.Domain;
using ProcessEngine.Worker.Infrastructure.Persistence;

namespace ProcessEngine.Worker.Infrastructure.Ingestion;

public class RabbitMqIngestionService
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<RabbitMqIngestionService> _logger;
    private readonly IConfiguration _config;

    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqIngestionService(
        INotificationRepository repository,
        IConfiguration config,
        ILogger<RabbitMqIngestionService> logger)
    {
        _repository = repository;
        _config = config;
        _logger = logger;
    }

    public void Start()
    {
        _logger.LogInformation("RabbitMqIngestionService.Start() invoked");

        if (!_config.GetValue<bool>("RabbitMqIngestion:Enabled"))
        {
            _logger.LogWarning("RabbitMQ ingestion disabled via config");
            return;
        }

        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMqIngestion:HostName"],
            Port = int.Parse(_config["RabbitMqIngestion:Port"]!),
            UserName = _config["RabbitMqIngestion:UserName"],
            Password = _config["RabbitMqIngestion:Password"],
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.QueueDeclare(
            queue: _config["RabbitMqIngestion:QueueName"],
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: ushort.Parse(_config["RabbitMqIngestion:PrefetchCount"]!),
            global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceived;

        _channel.BasicConsume(
            queue: _config["RabbitMqIngestion:QueueName"],
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("RabbitMQ ingestion started");
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var rawBody = Encoding.UTF8.GetString(ea.Body.ToArray());

            var envelope = new
            {
                source = "RabbitMQ",
                queue = _config["RabbitMqIngestion:QueueName"],
                receivedAt = DateTime.UtcNow,
                raw = rawBody
            };

            var notification = new NotificationItem
            {
                Id = Guid.NewGuid(),
                Channel = NotificationChannel.File,
                State = NotificationState.New,
                RetryCount = 0,
                MaxRetry = 5,
                PayloadJson = JsonSerializer.Serialize(envelope)
            };

            await _repository.InsertAsync(notification);

            _channel!.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ ingestion failed");

            // DO NOT requeue ingestion failures
            _channel!.BasicNack(ea.DeliveryTag, false, requeue: false);
        }
    }







//    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
//    {
//        try
//        {
//            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

//            // CRITICAL FIX: Parse body into JSON object
//            var parsedContent = JsonSerializer.Deserialize<object>(body);

//            var payload = new
//            {
//                Source = "RabbitMQ",
//                Queue = _config["RabbitMqIngestion:QueueName"],
//                DeliveredAt = DateTime.UtcNow,
//                Content = parsedContent
//            };

//            var notification = new NotificationItem
//            {
//                Id = Guid.NewGuid(),
//                Channel = NotificationChannel.File,
//                State = NotificationState.New,
//                RetryCount = 0,
//                MaxRetry = 5,
//                PayloadJson = JsonSerializer.Serialize(payload)
//            };

//            await _repository.InsertAsync(notification);

//            _channel!.BasicAck(ea.DeliveryTag, false);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "RabbitMQ message processing failed");
//            _channel!.BasicNack(ea.DeliveryTag, false, true);
//        }
//    }
}
