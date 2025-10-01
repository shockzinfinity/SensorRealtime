using Microsoft.Extensions.Options;
using MQTTnet;
using SensorApi.Domain;
using SensorApi.Producers;
using System.Text;
using System.Text.Json;

namespace SensorApi.Services;

public class MqttIngestWorker : BackgroundService
{
  private readonly IKafkaProducer _kafkaProducer;
  private readonly KafkaOptions _kafkaOptions;
  private readonly MqttOptions _mqttOptions;

  public MqttIngestWorker(IKafkaProducer kafkaProducer, IOptions<KafkaOptions> kafkaOptions, IOptions<MqttOptions> mqttOptions)
  {
    _kafkaProducer = kafkaProducer;
    _kafkaOptions = kafkaOptions.Value;
    _mqttOptions = mqttOptions.Value;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var mqttClient = new MqttClientFactory().CreateMqttClient();

    mqttClient.ApplicationMessageReceivedAsync += async e =>
    {
      var payload = e.ApplicationMessage.Payload;
      var json = payload.IsEmpty ? string.Empty : Encoding.UTF8.GetString(payload);

      Console.WriteLine($"[MQTT] {e.ApplicationMessage.Topic} -> {json}");

      try
      {
        var reading = JsonSerializer.Deserialize<SensorReading>(json);
        Console.WriteLine($"[MQTT] Parsed: {reading}");

        if (reading is null || string.IsNullOrWhiteSpace(reading.SensorId)) return;

        Console.WriteLine($"[MQTT] forwarding to Kafka topic '{_kafkaOptions.TopicRaw}' key='{reading.SensorId}'");
        await _kafkaProducer.ProduceAsync(_kafkaOptions.TopicRaw, reading.SensorId, json, stoppingToken);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[ERROR] Failed to process message: {ex.Message}");
      }
    };

    var options = new MqttClientOptionsBuilder()
      .WithTcpServer(_mqttOptions.Host, _mqttOptions.Port)
      .WithClientId("sensor-ingest-" + Guid.NewGuid().ToString("N"))
      .Build();

    await mqttClient.ConnectAsync(options, stoppingToken);

    var filter = new MqttTopicFilterBuilder()
      .WithTopic(_mqttOptions.Topic)
      .WithAtLeastOnceQoS()
      .Build();

    await mqttClient.SubscribeAsync(filter, stoppingToken);

    Console.WriteLine($"[MQTT] Subscribed to {_mqttOptions.Topic}");

    while (!stoppingToken.IsCancellationRequested)
      await Task.Delay(1000, stoppingToken);

    await Task.CompletedTask;
  }
}