namespace SensorProcessor;

public sealed class KafkaOptions
{
  public string BootstrapServers { get; set; } = "broker:9092";
  public string GroupId { get; set; } = "sensor-processor";
  public string InputTopic { get; set; } = "sensor.raw";
  public string TopicView { get; set; } = "sensor.view";
  public string TopicAlarm { get; set; } = "sensor.alarm";
}