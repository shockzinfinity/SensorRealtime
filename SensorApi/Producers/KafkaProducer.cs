using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace SensorApi.Producers;

public interface IKafkaProducer
{
  Task ProduceAsync(string topic, string key, string json, CancellationToken cancellationToken);
}

public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
  private readonly IProducer<string, string> _producer;
  private readonly KafkaOptions _options;

  public KafkaProducer(IOptions<KafkaOptions> options)
  {
    _options = options.Value;
    var config = new ProducerConfig
    {
      BootstrapServers = _options.BootstrapServers,
      Acks = Acks.All,
      EnableIdempotence = true
    };
    _producer = new ProducerBuilder<string, string>(config).Build();
  }

  public async Task ProduceAsync(string topic, string key, string json, CancellationToken cancellationToken)
  {
    try
    {
      var result = await _producer.ProduceAsync(
          topic,
          new Message<string, string> { Key = key, Value = json },
          cancellationToken);

      Console.WriteLine($"[KAFKA] Delivered to {result.TopicPartitionOffset} (status={result.Status})");
    }
    catch (ProduceException<string, string> ex)
    {
      Console.WriteLine($"[KAFKA][ERROR] {ex.Error.Code}: {ex.Error.Reason}");
      throw; // 원하면 재시도 로직으로 바꿀 수 있음
    }
  }

  public void Dispose() => _producer.Flush(TimeSpan.FromSeconds(2));
}