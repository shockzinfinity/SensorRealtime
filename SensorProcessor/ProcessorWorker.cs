using Confluent.Kafka;
using Microsoft.Extensions.Options;
using SensorProcessor.Domain;
using System.Text.Json;

namespace SensorProcessor;

public class ProcessorWorker : BackgroundService
{
  private readonly ILogger<ProcessorWorker> _logger;
  private readonly KafkaOptions _options;
  private readonly IConsumer<string, string> _consumer;

  public ProcessorWorker(ILogger<ProcessorWorker> logger, IOptions<KafkaOptions> options)
  {
    _logger = logger;
    _options = options.Value;

    var config = new ConsumerConfig
    {
      BootstrapServers = _options.BootstrapServers,
      GroupId = _options.GroupId,
      AutoOffsetReset = AutoOffsetReset.Earliest,
      EnableAutoCommit = false,
      SessionTimeoutMs = 10_000,
      MaxPollIntervalMs = 300_000
    };

    _consumer = new ConsumerBuilder<string, string>(config)
      .SetErrorHandler((_, e) =>
      {
        _logger.LogError($"[KAFKA][ERROR]: {e.Reason}");
      })
      .Build();
  }

  protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(() =>
  {
    _consumer.Subscribe(_options.InputTopic);
    _logger.LogInformation($"[PROC] Subscribed to '{_options.InputTopic}' as group '{_options.GroupId}'");

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var consumerResult = _consumer.Consume(stoppingToken); // 블로킹
          if (consumerResult is null || consumerResult.IsPartitionEOF) continue;

          LogHelper.ConsumeResult(_logger, consumerResult);

          var reading = JsonSerializer.Deserialize<SensorReading>(consumerResult.Message.Value);
          if (reading is null || string.IsNullOrWhiteSpace(reading.Value.ToString()))
          {
            _logger.LogWarning($"[PROC][WARN] Invalid reading payload");
            _consumer.Commit(consumerResult);
            continue;
          }

          _consumer.Commit(consumerResult);
        }
        catch (ConsumeException e) when (e.Error.Code == ErrorCode.UnknownTopicOrPart)
        {
          _logger.LogWarning($"[PROC][WARN] Topic not available yet. Retry in 2s...");
          _consumer.Unsubscribe();
          Thread.Sleep(2000);
          _consumer.Subscribe(_options.InputTopic);
        }
      }
    }
    catch (OperationCanceledException)
    {
      /* 정상 종료 */
    }
    finally
    {
      _consumer.Close();
      _consumer.Dispose();
    }
  }, stoppingToken);
}