# Repository Guidelines

## 프로젝트 구조 및 모듈 구성
- `SensorRealtime.slnx`는 세 개의 .NET 10 프로젝트를 하나의 솔루션으로 묶습니다.
- `SensorApi/`는 HTTP 인입과 MQTT→Kafka 파이프라인을 담당하며, 도메인 레코드는 `Domain/`, 백그라운드 워커는 `Services/`, 프로듀서는 `Producers/`에 배치됩니다.
- `SensorProcessor/`는 Kafka 스트림을 소비해 가공 토픽으로 내보내는 워커입니다. 공유 계약과 옵션은 프로젝트 루트의 `Domain/`, `Options.cs`에 정리합니다. `SensorPublisher/`는 로컬 스모크 테스트용 MQTT 샘플 발행기입니다.
- 루트에는 공용 `CLUSTER_ID`를 담은 `.env`, Kafka 스택을 올리는 `docker-compose.yml`, 브로커 초기화를 담당하는 `broker-entrypoint.sh`가 위치합니다.

## 빌드·테스트·개발 명령어
- `dotnet restore SensorRealtime.slnx && dotnet build SensorRealtime.slnx` — 프리뷰 SDK로 패키지를 복원하고 컴파일합니다.
- `dotnet run --project SensorApi/SensorApi.csproj`와 `dotnet run --project SensorProcessor/SensorProcessor.csproj` — 컨테이너 없이 API와 워커를 기동합니다.
- `dotnet run --project SensorPublisher/SensorPublisher.csproj` — `Program.cs`에서 정의한 MQTT 페이로드를 발행합니다.
- `docker-compose up --build broker schema-registry control-center sensorapi sensorprocessor` — 전체 스택을 올립니다. 실행 전 `.env` 존재를 확인하세요.

## 코딩 스타일 및 네이밍
- 파일 범위 네임스페이스, 공백 두 칸 들여쓰기, 표현식 본문 헬퍼를 기본으로 합니다. 예시는 `SensorApi/Services/MqttIngestWorker.cs`를 참고하세요.
- 설정 객체는 각 프로젝트의 `Options.cs`에 `public sealed`로 정의하고 nullable 컨텍스트를 유지합니다.
- 타입·공용 멤버는 PascalCase, 지역·매개변수는 camelCase, 환경 변수는 UPPER_SNAKE 형태로 작성하며 로그 접두사는 `[MQTT]`, `[ERROR]` 패턴을 유지합니다.
- 커밋 전 `dotnet tool install -g dotnet-format`으로 도구를 설치한 뒤 `dotnet format`을 실행해 Roslyn 기본 스타일을 맞춥니다.

## 테스트 가이드라인
- 현재 자동화 테스트가 없으므로 서비스명과 일치하는 xUnit 프로젝트를 `tests/` 아래 생성하세요(예: `tests/SensorApi.Tests`).
- 테스트 클래스는 `{타입명}Tests`, 메서드는 `메서드_상황_기대결과` 형태로 명명합니다.
- Kafka/MQTT 의존성은 페이크를 활용하고, 솔루션 루트에서 `dotnet test`를 실행해 신규 코드 기준 80% 이상 커버리지를 목표로 합니다.

## 커밋 및 PR 규칙
- 커밋 제목은 명령형 현재형으로 72자 이하(예: `Add MQTT reconnect backoff`)로 작성하고 필요한 경우 본문에 불릿으로 상세를 남깁니다.
- `Fixes #123`, `Refs #123`와 같이 이슈를 연결하고 영향 받은 서비스 태그를 덧붙입니다.
- PR에는 변경 요약, 검증 결과(`dotnet test`, docker 실행 등), API 계약 변경 시 페이로드 예시를 포함하세요.
- 공유 DTO나 인프라 파일을 수정할 때는 도메인 담당자 리뷰를 요청합니다.

## 보안 및 설정 팁
- 민감 정보는 커밋하지 말고 `dotnet user-secrets`를 사용해 로컬에서만 유지합니다. `.env`에는 `CLUSTER_ID` 등 비민감 항목만 두세요.
- Docker 실행 전 1883/8080/9092/19092 포트 충돌을 확인하고, 원격 브로커에 접속할 때는 `appsettings.Development.json`의 `Kafka__BootstrapServers`를 조정하세요.
