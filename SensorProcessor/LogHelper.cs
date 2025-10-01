using Confluent.Kafka;

namespace SensorProcessor;

public static class LogHelper
{
  public static void ConsumeResult<TKey, TValue>(ILogger logger, ConsumeResult<TKey, TValue> consumerResult)
  {
    logger.LogInformation(
        "[PROC] topic={Topic} partition={Partition} offset={Offset} key={Key} value={Value}",
        consumerResult.Topic,
        consumerResult.Partition.Value,
        consumerResult.Offset.Value,
        consumerResult.Message.Key,
        consumerResult.Message.Value
    );
  }

  public static void DeliveryResult<TKey, TValue>(ILogger logger, DeliveryResult<TKey, TValue> deliveryResult)
  {
    logger.LogInformation(
        "[PROC] delivered  topic='{Topic}' [{Partition}] at offset {Offset}: status={Status}",
        deliveryResult.Topic,
        deliveryResult.Partition.Value,
        deliveryResult.Offset.Value,
        deliveryResult.Status
    );
  }
}