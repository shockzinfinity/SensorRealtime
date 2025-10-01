using System.Text.Json.Serialization;

namespace SensorApi.Domain;

// 장치 원본 이벤트 (ingest -> processor)
public record SensorReading (
  [property: JsonPropertyName("deviceId")] string DeviceId,
  [property: JsonPropertyName("sensorId")] string SensorId,
  [property: JsonPropertyName("value")] double Value,
  [property: JsonPropertyName("ts")] long Ts // unix timestamp in milliseconds
  );

// 대시보드 전송용 (가벼운 스냅샷)
public record SensorView(
  string SensorId,
  double Value,
  long Ts
  );

// 임계치 알람 이벤트
public record AlarmEvent(
  string SensorId,
  double Value,
  double Threshold,
  string Serverity, // "WARN" | "CRID" 등
  long Ts
  );
