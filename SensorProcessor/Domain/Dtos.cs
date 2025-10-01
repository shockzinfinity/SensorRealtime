using System.Text.Json.Serialization;

namespace SensorProcessor.Domain;

public record SensorReading(
  [property: JsonPropertyName("deviceId")] string DeviceId,
  [property: JsonPropertyName("sensorId")] string SensorId,
  [property: JsonPropertyName("value")] double Value,
  [property: JsonPropertyName("ts")] long Timestamp
  );