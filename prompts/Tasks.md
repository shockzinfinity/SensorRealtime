# 작업 브레이크다운 – Sensor Monitoring MVP
(기준 문서: `prompts/ProductRequirementsDocument.md` v0.4, 작성일 2025-10-01)

---

## Phase 1 – 도메인 기반 백엔드 골격 정비 (목표: 1~3주)
### 1.1 솔루션 구조 정리
- [ ] `SensorApi`, `SensorProcessor`, `SensorPublisher`를 각각 Domain/Application/Infrastructure 레이어 프로젝트로 분리.
- [ ] 공통 DTO/값 객체를 `Shared.Domain`(또는 유사 네임스페이스) 라이브러리로 이전하고, 기존 코드 의존성 재정렬.
- [ ] `SensorRealtime.slnx`에 신규 프로젝트 추가 및 빌드 파이프라인 맞춤.

### 1.2 설정·환경 구성 정비
- [ ] `.env`, `appsettings.Development.json` 키를 PRD 명세(`Kafka__*`, `Mqtt__*`, `Processor__Threshold` 등)와 일치하도록 재구성.
- [ ] 환경값 검증 로직 추가 (부족한 값 로깅 + 앱 기동 시 실패).
- [ ] `Dockerfile`, `docker-compose.yml`에서 환경 변수 매핑 확인 및 정리.

### 1.3 MQTT 인입 파이프라인 강화
- [ ] `MqttIngestWorker`에 MQTT 연결 재시도, QoS 설정, 구독 실패 핸들링 추가.
- [ ] 메시지 유효성 검증(필수 필드, 타입) 및 불량 데이터 Dead Letter 전략 초안 작성.
- [ ] `IKafkaProducer` 인터페이스 기반으로 토픽·키·값 검증, 예외 처리, 재시도 백오프 적용.
- [ ] 단위 테스트: MQTT 메시지 시뮬레이션, Producer mock을 이용해 Kafka 호출 검증.

### 1.4 Processor 로직 완성
- [ ] `ProcessorWorker`에서 `sensor.raw` 소비 후 임계치 비교 로직 구현.
- [ ] Severity 규칙 정의(예: WARN/CRIT) 및 메시지 구조 결정.
- [ ] `sensor.view` 업데이트 전략(메모리 캐시 vs 외부 스토리지) 선택.
- [ ] 커밋/오프셋 처리 및 장애 시 재처리 플로우 확립.
- [ ] 로깅 유틸(`LogHelper`) 확장: 처리 결과, 경고, 오류를 구조화 로그로 기록.
- [ ] 단위 테스트: 샘플 payload 기반 로직 검증, 경계값 테스트.

### 1.5 API 레이어 구축
- [ ] `/stream/alarm` SSE 엔드포인트 구현 (Response cache, Keep-Alive, Retry 설정 포함).
- [ ] `/api/sensors/view` GET 엔드포인트 구현 및 DTO/Response 포맷 정의.
- [ ] `/health` 엔드포인트 확장: Kafka/MQTT 연결 상태, Processor heartbeat 포함.
- [ ] Application 서비스에서 CQRS 패턴 적용(명령/조회 핸들러 분리).

### 1.6 초기 문서/디자인 준비
- [ ] PRD 반영 상태 점검 및 업데이트 로그 작성.
- [ ] 대시보드/임계치 관리 UI 목업을 위한 기능 리스트·필드 정의서 작성.
- [ ] 목업 제작 범위 정리(데스크톱 풀 뷰, 모바일 축약 뷰, 임계치 관리 화면) 후 디자인팀 브리핑 자료 작성.

---

## Phase 2 – 프론트엔드 대시보드 및 UI 구현 (백엔드 병행 가능)
### 2.1 프론트엔드 프로젝트 부트스트랩
- [ ] `dashboard/` 폴더에 Vue 3 + Vite 프로젝트 생성.
- [ ] ESLint, Prettier, Stylelint, TypeScript 설정 및 Git hook(예: Husky) 구성.
- [ ] 기본 라우팅 구조(대시보드, 임계치 관리, 설정 페이지)와 국제화 준비(i18n).

### 2.2 디자인 시스템 및 목업 확정
- [ ] UI/UX 디자인 시스템 후보(예: Tailwind+Headless UI, Vuetify 등) 조사.
- [ ] 컴포넌트 레벨 디자인 가이드(색상, 타이포, 그리드) 작성.
- [ ] Figma 목업 1차안 제작 → 내부 리뷰 → 승인 후 개발 To-do 추출.

### 2.3 데이터 연동 계층
- [ ] SSE 래퍼 서비스 구현 (자동 재연결, backoff, heartbeat 감지).
- [ ] View API 클라이언트 작성 및 DTO 매핑 함수 구현.
- [ ] RxJS 기반 데이터 스트림 구성(알람 스트림, 센서 뷰 스트림, 상태 스트림 분리).

### 2.4 대시보드 위젯 구현
- [ ] 알람 리스트 컴포넌트(필터링, 정렬, 세부정보 패널).
- [ ] 실시간 차트(라인/바) 컴포넌트와 데이터 스무딩 옵션.
- [ ] 라인별 요약 카드(상태, 평균, 임계치 대비).
- [ ] 장비 위치 지도: mock 데이터로 시작, 실제 좌표 스키마 결정.
- [ ] 유지보수 작업 큐: 알람→작업 생성→처리 상태 추적 UI.
- [ ] 모바일 축약 대시보드: 핵심 KPI/알람 중심으로 레이아웃 설계.

### 2.5 임계치 관리 UI
- [ ] 임계치 목록/검색 화면, 사용자별 변경 이력 조회.
- [ ] 변경 폼 및 유효성 검증(임계치 범위, 단위 등).
- [ ] 변경 이벤트 API 호출 후 실시간 반영(알람 스트림 업데이트).

### 2.6 상태 관리 및 라우팅
- [ ] Pinia 혹은 Composition Store로 전역 상태 설계(CQRS 기반으로 command/query action 분리).
- [ ] 라우팅 가드(권한, 초기 데이터 로딩) 및 레이지 로딩.
- [ ] 다국어/테마 토글 등 향후 확장 포인트 정의.

---

## Phase 3 – 테스트, 품질, 자동화
### 3.1 프론트엔드 테스트 전략
- [ ] Jest/Vitest 기반 유닛 테스트 세팅(컴포넌트, 유틸).
- [ ] Playwright E2E 시나리오 정의: 알람 수신, 임계치 조정, 모바일 레이아웃 검증, 유지보수 작업 흐름.
- [ ] 테스트 데이터 픽스처/Mock 서버 셋업.

### 3.2 백엔드 테스트 전략
- [ ] xUnit 프로젝트 생성(Domain, Application 별도) 및 핵심 도메인 서비스 테스트.
- [ ] Kafka/MQTT Testcontainers를 활용한 통합 테스트 작성(ingest→processor→알람 흐름).
- [ ] 로깅/메트릭에 대한 회귀 테스트(구조화 로그 형태 검증).

### 3.3 CI/CD 대비 작업
- [ ] 로컬용 빌드/테스트 스크립트 작성(`scripts/build.sh`, `scripts/test.sh`).
- [ ] GitHub Actions 초안 워크플로우(yaml) 준비(향후 실제 도입 시 사용).
- [ ] 코드 분석 도구(예: SonarLint, dotnet format, ESLint) 자동 실행 스크립트.

---

## Phase 4 – 관측성, 운영 준비, 인프라 보강
### 4.1 로그·모니터링 체계
- [ ] Serilog(또는 유사) 도입해 구조화 로그와 Correlation ID 관리.
- [ ] 알람 발생/확인/조치 타임스탬프 기록을 위한 로깅 스키마 설계.
- [ ] Prometheus 지표 설계(Event 처리량, 지연, 에러율) 및 Exporter 기본 뼈대 작성.

### 4.2 Docker Compose & 환경 정비
- [ ] EMQX 컨테이너 설정 추가(포트, 인증 옵션 기본값).
- [ ] 프론트엔드 서비스(Nginx/Node) 추가 및 리버스 프록시 구성.
- [ ] 개발/테스트 환경 분리(.env 파일 프로필, docker compose override).

### 4.3 운영 프로세스 초안
- [ ] 알람 SLA 모니터링 대시보드(알람 처리 시간 히트맵, SLA 위반 리스트) 설계.
- [ ] 운영 핸드북 초안(알람 대응 절차, 임계치 변경 가이드, 재처리 메뉴얼).
- [ ] 변경 이력 접근 권한 모델 정의(감사 대응 대비).

---

## Phase 5 – 후속 연구, 결정 필요사항
### 5.1 실시간 채널 전환 검토
- [ ] SSE vs WebSocket/SignalR 벤치마크 계획 수립(동시 접속, p95 지연, 리소스 사용량).
- [ ] 보안/브라우저 호환성/프록시 제약 분석.
- [ ] PoC 결과 보고서 및 전환 조건 정의.

### 5.2 디자인 시스템 및 컴포넌트 선정
- [ ] 후보 라이브러리 비교표(구성 요소, 접근성, 성능, 커뮤니티).
- [ ] 디자인 토큰/테마 전략 수립.
- [ ] 컴포넌트 래퍼 설계(예: 조직 내 재사용성을 위한 UI Kit).

### 5.3 외부 통합 및 확장성
- [ ] Slack/Webhook 알림 파이프라인 설계, 알람 중요도에 따른 라우팅 규칙.
- [ ] Kafka Connect 대상 시스템 비교(InfluxDB, PostgreSQL, S3 등) 및 PoC 일정.
- [ ] 보안 로드맵: MQTT TLS, Kafka ACL, API 인증/권한 도입 순서와 필요 라이브러리 조사.

### 5.4 문서화
- [ ] PRD 변경 로그 및 버전 관리 체계 마련.
- [ ] 아키텍처 다이어그램(데이터 플로우, 서비스 컴포넌트) 최신화.
- [ ] 개발자 온보딩 가이드(로컬 환경 세팅, 주요 명령, 테스트 절차) 작성.

> 각 Phase 완료 시점마다 PRD/Tasks 문서를 재검토하고, 사용자 지시에 따라 업데이트를 진행할 것.
