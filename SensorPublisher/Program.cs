using MQTTnet;
using System.Text.Json;

Console.CancelKeyPress += (_, e) => e.Cancel = true;

var brokerHost = Environment.GetEnvironmentVariable("MQTT_HOST") ?? "localhost";
var brokerPort = int.TryParse(Environment.GetEnvironmentVariable("MQTT_PORT"), out var p) ? p : 1883;
var topic = Environment.GetEnvironmentVariable("MQTT_TOPIC") ?? "sensor/+/reading";
var baseIntervalMs = int.TryParse(Environment.GetEnvironmentVariable("PUBLISH_INTERVAL_MS"), out var ms) ? ms : 1000;

var factory = new MqttClientFactory();
var client = factory.CreateMqttClient();

var options = new MqttClientOptionsBuilder()
    .WithTcpServer(brokerHost, brokerPort)
    .WithClientId("sensor-publisher-" + Guid.NewGuid().ToString("N"))
    .Build();

await client.ConnectAsync(options);
Console.WriteLine($"Connected to MQTT broker {brokerHost}:{brokerPort}");

var random = new Random();
var running = true;

Console.CancelKeyPress += async (_, e) =>
{
  e.Cancel = true;
  running = false;
  await client.DisconnectAsync();
  Console.WriteLine("Disconnected from MQTT broker");
};

while (running)
{
  var payload = new
  {
    deviceId = $"line-{random.Next(1, 4)}",
    sensorId = $"temp-{random.Next(1, 5)}",
    value = Math.Round(60 + random.NextDouble() * 50, 2),
    ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
  };

  var json = JsonSerializer.Serialize(payload);

  var message = new MqttApplicationMessageBuilder()
    .WithTopic($"sensor/{payload.deviceId}/reading")
    .WithPayload(json)
    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
    .Build();

  await client.PublishAsync(message);
  Console.WriteLine($"[PUBLISH]: {json}");

  // 랜덤 딜레이: baseInterval ±50% 범위
  var jitter = random.Next((int)(baseIntervalMs * 0.5), (int)(baseIntervalMs * 1.5));

  await Task.Delay(jitter);
}