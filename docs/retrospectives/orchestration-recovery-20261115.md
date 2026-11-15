# 작업 대기열 복구 회고

2026-11-15. R02 P2 뒤 footpivot 정정·schema2 최종 통합 인계가 실행되지 않은 상태를 확인했다. 메시지 전송 성공은 작업 시작 증거가 아니었다. 이전 상태 보고에서 이 둘을 구분하지 못한 점을 교정한다.

## 관측과 복구

기존 worker의 liveness는 exited이나 배정은 pending/start_unknown으로 남았고, 대기 카드는 채팅 queue에 있었다. 같은 배정 retry는 latest settled 조건을 충족하지 않아 거절됐다. 카드가 자동 실행되지 않은 Orca 내부 원인은 확인되지 않았다.

사용자가 중단 작업을 이어가라고 명시한 뒤 해당 배정만 guarded stop을 요청했으나 외부 terminal이어서 stop_unknown이었다. 후속 worker-show에서도 exited를 확인했다. 이후 해당 배정을 abandon으로 fence·settle했으며, 이 명령이 process를 종료했다는 주장은 하지 않는다. 파일/원본/사용자 앱/기기는 건드리지 않았다.

같은 Task를 retry해 새 Codex worker를 gpt-6.1-sol/medium 지정으로 배정했다. 기존 Task 제목이 New messages여서 새 worker가 작업 내용을 기다리는 문제가 드러났고, 같은 배정에 파일 소유·미완료 범위·검증·저장 보존을 담은 구체 지시를 전달했다. 새 worker의 실행 시작 status를 받았다. 이는 구현/검증 완료 증거가 아니며 최종 산출물·명령/exit·파일 해시는 후속 인계에서 확인한다.

## 다음 인계 기준

- send 성공과 실행 시작·제품 변경·검증 완료를 상태 보고에서 구분한다.
- 같은 task의 제목/본문이 일반 알림인 경우 실제 수행 범위를 짧은 task packet으로 전달한다.
- 명시된 unknown/실패의 next 절차만 따르고 무한 resend·polling·새 Task로 우회하지 않는다.
- source와 최신 진행 문서를 먼저 확인해 이미 끝난 P2를 재구현하지 않는다. Editor/GUI 단일 소유 및 사용자 저장 보존은 복구 중에도 유지한다.
- 단계 토큰 기준점은 record_model_usage 도구로 기록하며, 복구·조율 비용도 숨기지 않는다. 사용량이 없는 worker를0으로 합산하지 않는다.

이 기록은 프로젝트 GC 근거다. 전역 규칙/메모리 자동 승격과 Orca 상태 초기화는 하지 않았다. 복구 뒤 제품 구현/통합/독립 리뷰·commit/push·CI는 아직 완료로 판정하지 않는다.
