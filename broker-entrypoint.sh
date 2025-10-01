#!/bin/bash
set -e

# 1) 환경변수 → /etc/kafka/kafka.properties 생성
if [ -x /etc/confluent/docker/configure ]; then
  /etc/confluent/docker/configure
fi

# 2) 최초 포맷(볼륨이 비어 있을 때만)
if [ ! -f /var/lib/kafka/data/meta.properties ]; then
  : "${CLUSTER_ID:=${KAFKA_CLUSTER_ID}}"
  : "${CLUSTER_ID:=$(/usr/bin/kafka-storage random-uuid)}"
  /usr/bin/kafka-storage format --ignore-formatted -t "$CLUSTER_ID" -c /etc/kafka/kafka.properties
fi

# 3) 최종 실행 (신호 처리/로그 등 Confluent 표준 엔트리포인트)
exec /etc/confluent/docker/run