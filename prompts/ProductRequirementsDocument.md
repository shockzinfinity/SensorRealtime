# 제품 요구사항 문서 – Sensor Monitoring MVP

**메타데이터**
- 프로젝트: Sensor Monitoring SPA (MQTT → Kafka → Processor → Dashboard)
- 작성자: Codex (DSN Solution 협업)
- 작성일: 2025-10-01
- 상태: 검토용 초안 v0.4

---

## 1. 목적 (Objective)
센서에서 발생하는 실시간 데이터를 MQTT로 수집하고 Kafka를 거쳐 Processor가 임계치 기반 알람과 대시보드용 뷰를 생성, 프론트엔드 SPA가 실시간으로 노출하는 MVP를 구축한다. 알람 전달은 200ms 이내로 유지하여 운영자가 즉각 대응할 수 있게 한다.

## 2. 배경 (Background)
- 센서 데이터는 주기·대량·실시간 특성을 지니며, 단순 모니터링을 넘어 장기 저장·분석·재처리가 필요하다.
- MQTT만으로도 구독이 가능하지만, 다중 파이프라인/재처리/확장성을 위해 Kafka를 중간 계층으로 사용한다.
- Kafka는 내구성, 파티션 확장, 오프셋 기반 재처리, 다중 컨슈머 그룹 지원 등으로 향후 ML/Analytics 팀 확장에 유리하다.

## 3. 대상 사용자 (Target Users)
- **운영 엔지니어**: 실시간 상태, 알람 발생 즉시 확인, 시스템 헬스 점검.
- **생산 관리자**: 라인별 KPI, 추세 차트, 임계치 정책 정합성 확인.
- **데이터/ML 엔지니어**: 재처리 가능한 원시 스트림, 추가 컨슈머 그룹 연결.

## 4. 아키텍처 개요 (Architecture Overview)
### 4.1 데이터 플로우
1. 센서 → EMQX MQTT 브로커 (`sensor/{lineId}/reading`).
   - 현재 단계에서는 `SensorPublisher`로 로컬 테스트용 샘플 발행으로 대체.
2. `MqttIngestWorker`(SensorApi): MQTT 메시지를 Kafka `sensor.raw` 토픽에 적재(키: sensorId, 값: JSON).
3. `ProcessorWorker`(SensorProcessor): CQRS 기반 로직으로 임계치 초과 시 `sensor.alarm`, 정상치는 `sensor.view` 발행.
4. ASP.NET Core API: `/stream/alarm`(SSE)으로 알람 스트림, `/api/sensors/view`로 가공 데이터 제공, `/health` 모니터링.
5. Vue.js + RxJS SPA: SSE 구독, RxJS로 스트림 결합, 대시보드 UI(알람 티커, 센서 카드, 추세 차트) 렌더링.

### 4.2 Docker Compose 서비스 (MVP)
- `broker`: Confluent cp-kafka KRaft 단일 노드.
- `schema-registry`: Kafka Schema Registry.
- `control-center`: Kafka Control Center.
- `sensorapi`: ASP.NET Core API + BackgroundService(MqttIngestWorker 포함).
- `sensorprocessor`: ProcessorWorker 전용 서비스 (필요 시 API와 분리).
- `emqx`: MQTT 브로커.

### 4.3 DDD / Clean Architecture / CQRS 원칙
- 도메인 계층: 센서 리딩, 알람, 뷰 엔티티 및 값 객체 정의.
- 애플리케이션 계층: 명령(Command) 핸들러와 조회(Query) 핸들러 분리.
- 인프라 계층: Kafka, MQTT, 향후 스토리지 어댑터. 인터페이스 기반 주입.
- 공유 계약은 별도 도메인 어셈블리로 관리하여 서비스 간 결합 최소화.

---

## 5. 범위 및 릴리스 전략 (Scope & Release)
- **MVP**: Docker Compose 기반 온프레미스 배포, 임계치 알람, SSE 스트림, 기본 대시보드, 로컬 테스트 우선.
- **Phase 2 이후**: Kubernetes 확장, CI/CD, 고급 알람 정책, Webhook/Slack 연동, Prometheus/Grafana 모듈, 인증/권한, Kafka Connect 장기 저장, ML 이상 탐지.

---

## 6. 기능 요구사항 (Functional Requirements)
- **FR1 MQTT 인입**: 와일드카드 토픽 구독, JSON 스키마 검증, Kafka `acks=all`, `enable.idempotence=true` 설정.
- **FR2 처리 로직**: `Processor__Threshold` 구성 가능, LogHelper 등 로깅 유틸 활용, 임계치 초과 시 severity 포함 알람 이벤트 발행.
- **FR3 알람 채널**: `/stream/alarm` SSE 구현. WebSocket/SignalR 대안 비교(지연, 확장성, 브라우저 지원) 리포트 작성 후 전환 여부 결정.
- **FR4 뷰 API**: `/api/sensors/view`에서 최신 스냅샷 제공, `lineId`, `sensorId` 필터 지원.
- **FR5 대시보드 UX**: 알람 리스트, 실시간 차트, 라인별 요약, 장비 위치 지도, 유지보수 작업 큐 등 핵심 위젯 포함. 반응형 구성으로 데스크톱은 전체 위젯, 모바일은 알람/요약 중심 최소 위젯 세트 제공.
- **FR6 UI 목업**: 대시보드 및 임계치 관리 UI를 Figma 등으로 설계(데스크톱/모바일 별도 버전). 최종 UI/UX 디자인 시스템은 미정 상태이므로 목업 단계에서 가이드라인을 정의하고 승인 후 개발 진행.
- **FR7 임계치 관리**: 사용자별 임계치 변경 이력 저장(변경 시간, 이전/이후 값, 사용자 식별). 초기에는 API 기반 관리, UI는 목업 검토 후 구현 범위 확정.
- **FR8 설정 관리**: `.env`, `appsettings.Development.json` 기반 환경 구성. `dotnet user-secrets` 연계를 대비.
- **FR9 관측성 초기화**: `/health` 엔드포인트, 구조화 로그, Prometheus 모듈 추가 용이한 Metric 추상화 설계.
- **FR10 확장 인터페이스**: Kafka 컨슈머 그룹 추가 가능하도록 구성(Analytics, ML 등).
- **FR11 대시보드 테스트**: 핵심 사용자 시나리오를 Playwright 스크립트로 자동화하여 릴리스 전 검증한다.

---

## 7. 비기능 요구사항 (Non-Functional Requirements)
- **성능**: MQTT→Kafka 지연 50ms 이하, ingest→알람 전달 200ms 이하(p95).
- **확장성**: Kafka 파티션 확장, 컨슈머 그룹 추가 시 코드 수정 최소화.
- **신뢰성**: Processor 장애 시 오프셋 기반 재처리 100% 보장, 메시지 손실률 0%. 대시보드 가용성 운영 시간 기준 99%.
- **보안(추후)**: MVP는 내부망 가정. 완성도 확보 후 MQTT TLS, Kafka ACL, API 인증/권한 검토.
- **유지보수성**: DDD/Clean Architecture 엄수, CQRS 구분, 테스트 용이한 모듈형 설계.
- **테스트성**: 로컬 페이크(Kafka/MQTT) 기반 통합 테스트 템플릿, 프론트엔드 RxJS 단위 테스트, Playwright 기반 E2E 대시보드 테스트.

---

## 8. SLA 및 운영 가이드라인
- 알람 대응 SLA: 알람 발생 후 1분 이내 운영자 확인, 5분 이내 1차 조치.
- 핵심 지표는 추후 정의하되, 일간 알람 수/미처리 알람/조치 시간 등을 대시보드에 추가할 수 있도록 데이터 수집 구조 설계.

---

## 9. 성공 지표 (Success Metrics)
- 알람 종단 지연 < 200ms.
- Kafka 메시지 손실률 0%.
- 대시보드 실시간 업데이트 정확도 99% 이상.
- Processor 장애 후 재처리 성공률 100%.
- SLA 준수율 95% 이상.

---

## 10. 향후 확장 (Future Work)
- Kafka Connect로 InfluxDB/PostgreSQL/S3 등 장기 저장.
- Slack/Webhook 알람 채널.
- ML 기반 이상 탐지 및 예측 알람.
- Prometheus/Grafana 통합을 위한 모듈화된 Exporter.
- Kubernetes Helm 차트, GitOps 기반 CI/CD.
- WebSocket/SignalR 전환 기준 수립 및 PoC.

---

## 11. 로드맵 (Roadmap & Milestones)
1. **MVP 기초 (1~3주)**: 도메인 모델, ingest/processor 파이프라인, SSE 알람, 기본 Vue 대시보드, UI 목업 작업 병행.
2. **관측성 보강 (4~5주)**: 구조화 로그, `/health` 확장, Prometheus 연계 설계.
3. **알람 채널 검토 (5~6주)**: SSE vs WebSocket/SignalR 벤치마크 및 의사결정 문서화.
4. **Phase 2 준비**: Kubernetes 매니페스트, CI/CD, 인증 전략, Slack/Webhook 연동.
5. **Phase 3**: Kafka Connect, ML 이상 탐지 PoC.

---

## 12. 미해결 과제 (Open Questions)
- 임계치 관리 UI 개발 일정(목업 승인 후 개발 우선순위 조정 필요).
- 대시보드 모바일 뷰에 포함할 최소 위젯 세트 구체화.
- 알람 대응 SLA 측정을 위한 구체적 로깅/모니터링 항목 정의.
- User별 임계치 변경 이력 저장소(관계형 vs NoSQL) 및 접근 정책.
- SSE→WebSocket/SignalR 전환 기준(동시 사용자 수, p95 지연 등) 수립.
- UI/UX 디자인 시스템 및 컴포넌트 라이브러리 선정 일정.

---

## 13. 부록 (Appendix)
- MQTT 메시지 예시:
  ```json
  {
    "deviceId": "line-1",
    "sensorId": "temp-01",
    "value": 82.5,
    "ts": 1759132045328
  }
  ```
- Docker Compose 서비스 요약: broker, schema-registry, control-center, sensorapi, sensorprocessor, emqx.
- 관련 스크립트: `docker-compose.yml`, `broker-entrypoint.sh`, `.env` (CLUSTER_ID 저장).
