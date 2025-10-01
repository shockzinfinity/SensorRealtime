namespace SensorApi;

public sealed class MqttOptions
{
  public string Host { get; set; } = "";
  public int Port { get; set; }
  public string Topic { get; set; } = "sensor/+/reading";
}

public sealed class KafkaOptions
{
  public string BootstrapServers { get; set; } = "";
  public string TopicRaw { get; set; } = "sensor.raw";
  public string TopicView { get; set; } = "sensor.view";
  public string TopicAlarm { get; set; } = "sensor.alarm";
}

public sealed class ProcessorOptions
{
  public double Threshold { get; set; } = 80.0;
}