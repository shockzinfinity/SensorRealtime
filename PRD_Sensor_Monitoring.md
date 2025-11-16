# Product Requirements Document (PRD)  
**Project:** Sensor Monitoring SPA with MQTT → Kafka → Processor → Dashboard  
**Author:** DSN Solution  
**Date:** 2025-10-01  

---

## 1. 목적 (Objective)  
센서 데이터를 **실시간 모니터링 및 알람 처리**하기 위해, MQTT를 통해 들어오는 메시지를 Kafka에 적재하고, Processor가 이를 처리하여 알람 및 가공된 뷰를 생성, Vue.js 기반 프론트엔드가 실시간 대시보드 형태로 표시한다.  

---

## 2. 배경 (Background)  
- **센서 데이터 특성:** 주기적, 대량, 실시간성이 중요  
- **요구사항:**  
  - 특정 임계치를 초과하는 센서 값에 대해 **실시간 알람** 발생  
  - 데이터는 단순 알람뿐만 아니라 **장기 저장, 분석, 재처리** 가능해야 함  
- **MQTT vs Kafka 논의:**  
  - MQTT만으로도 실시간 구독 가능하나, **여러 파이프라인/재처리/확장성** 확보를 위해 Kafka를 중간 계층으로 둠  
  - Kafka는 **내구성, 파티션 기반 확장, 오프셋 재처리, 다중 컨슈머 그룹 지원** 등의 장점을 제공  

---

## 3. 아키텍처 (Architecture)  

### 3.1 데이터 플로우
1. **센서 → EMQX(MQTT Broker)**  
2. **MqttIngestWorker (C#)**: MQTT 메시지를 구독 후 Kafka `sensor.raw` 토픽에 적재  
3. **ProcessorWorker (C#)**: `sensor.raw` → 가공 → `sensor.view` / `sensor.alarm` 토픽으로 분기  
4. **백엔드 API (ASP.NET Core)**:  
   - SSE/WebSocket으로 `sensor.alarm` 스트림 제공  
   - `sensor.view` 기반 조회 API 제공  
5. **프론트엔드 (Vue.js + RxJS)**:  
   - 실시간 알람 SSE 구독  
   - RxJS로 데이터 스트림 결합 및 대시보드 표시  

### 3.2 Docker Compose 서비스
- **broker**: Kafka (Confluent cp-kafka, 단일 KRaft 모드)  
- **schema-registry**: Kafka Schema Registry  
- **control-center**: Kafka Control Center  
- **sensorapi**: C# ASP.NET Core API + BackgroundServices (MqttIngestWorker, ProcessorWorker)  
- **emqx**: MQTT 브로커 (별도 로컬 컨테이너)  

---

## 4. 주요 기능 요구사항 (Functional Requirements)  

### 4.1 MQTT Ingest
- MQTT `sensor/{lineId}/reading` 토픽 구독  
- 메시지 구조(JSON):  
  ```json
  {
    "deviceId": "line-1",
    "sensorId": "temp-01",
    "value": 82.5,
    "ts": 1759132045328
  }
  ```
- Kafka `sensor.raw` 토픽으로 전달 (Key = sensorId, Value = JSON)

### 4.2 Processor
- Kafka `sensor.raw` 구독  
- 비즈니스 로직:  
  - Threshold(`Processor__Threshold`) 초과 시 `sensor.alarm` 발행  
  - 정상 데이터는 `sensor.view`에 발행  
- Kafka Consumer 로깅 유틸 정적 메서드(`LogHelper`) 활용, 성능 최적화  

### 4.3 API (ASP.NET Core)
- `/health`: 단순 헬스 체크  
- `/stream/alarm`: SSE로 `sensor.alarm` 이벤트 스트림 제공  
- `/api/sensors/view`: 최신 뷰 데이터 조회  

### 4.4 Frontend (Vue.js + RxJS)
- RxJS로 SSE 스트림 구독 및 대시보드 UI 업데이트  
- 알람 발생 시 실시간 알림 표시  
- 차트/통계는 `sensor.view` 기반  

---

## 5. 비기능 요구사항 (Non-Functional Requirements)  

- **성능**:  
  - MQTT → Kafka 인게스트 지연 < 50ms (동일 네트워크 기준)  
  - 알람 전송 end-to-end 지연 < 200ms  

- **확장성**:  
  - Kafka 파티션 확장 가능  
  - 소비자 그룹 추가 가능 (ML, Analytics 등)  

- **신뢰성**:  
  - Kafka Producer: `acks=all`, `enable.idempotence=true`  
  - Processor: 장애 발생 시 재처리 가능  

- **운영/모니터링**:  
  - Kafka Control Center로 토픽/컨슈머 모니터링  
  - Prometheus/Grafana 연동 고려  

---

## 6. 향후 확장 (Future Work)  
- Kafka Connect를 통한 장기 저장 (e.g. InfluxDB, PostgreSQL, S3)  
- 알람 Slack/Webhook 연동  
- ML 모델 기반 예측 알람 (Anomaly Detection)  

---

## 7. 성공 지표 (Success Metrics)  
- 알람 지연 시간 < 200ms  
- Kafka 메시지 손실률 0%  
- 대시보드 실시간 업데이트 정확도 > 99%  
- Processor 장애 시 재처리 성공률 100%  
