# R04 Unity 구현자 회고

2026-11-16. 독립 리뷰/최종 GC는 root 소유이며 전역 학습 상태는 수정하지 않았다.

- 승인된 한 수직 slice로 sync1회·고정소스 최종 Unity검사1회씩·macbuild1개·대표3화면을 완료했다. 기존 Core/아트 근거를 재사용하고 helper·새 검사기·추가 조합/모바일/유료작업을 늘리지 않았다.
- 아트 보드의 정적 경고선을 그대로 옮기지 않고 명시 통합 결정에 따라 실제 Core live 위치·고정 방향·남은 시계·Tuning을 사용했다. custom geometry와 실제 턱뼈 넉백 시험이 서로 다른 위험을 다룬다. lowFX에서 필수 경고를 유지한다.
- QA는 fresh scene/중복 driver guard·Host 이전 path gate와 독점 identity를 재사용했다. 첫 단일 빌드와 연속 fixture가 통과해 재빌드/새 fixture 교정은 없었다. 생존 준비의 backlog 직접 kill/장비 타이머 억제·HP10000·완료 timeout을 명시했고 실제 전투/자연 난이도와 섞지 않았다.
- Unity의 기존 import 자동 추가 block/빈 icon만 baseline과 대조해 복구했고, Core 작성자의 변경과 기존 아트·사용자 앱/저장은 보존했다. 실제 자기 child 종료와 자기 저장 absent 복구 뒤 GUI 소유를 반환한다.
- Sol 기본을 유지했다. provider start event의 시차 때문에 세션 counter interval을 R04만의 정확한 토큰 비용으로 보고할 수 없다. 비용/절감률·effort·자연밸런스/native/mobile/performance를 추정하지 않는다. 이번 회고는 새 전역 규칙 승격/도구화 제안을 추가하지 않는다.

## Coordinator GC

사용자의 속도·중복 대조 피드백을 이어 적용했다. Core와 아트만 독립 병렬로 만들고 Unity는 한 통합자가 소유했으며, 기존 근거를 재사용하고 대표3화면으로 리뷰를 제한했다. Core의 고정 목표/방향과 아트의 불변 시작좌표 가정이 다른 점은 그림 교정 루프 대신 실제 live geometry를 따르는 소비 계약으로 해결했다. 별도 제품 기능이나 전역 규칙을 추가하지 않는다.

모델은 일반 구현 Sol/독립 코드 Sol high 및 지정 Claude 아트 담당을 유지했고 단순 탐색은 직접 도구로 처리했다. 공급자별 자기 usage 근거가 있는 구간만 보존하며 누락을0으로 합치거나 세션 누계를 R04 청구 비용으로 환산하지 않는다. 비용·절감률과 동일 조건 개발속도 비교는 미측정이다. 추가 최적화 후보는 현재 단일 앱/최종검사/변경 관련 리뷰를 유지하는 것으로 한정하고 새 helper·검사 도구·전역 승격은 보류한다. 최종 리뷰와 commit CI 결과는 진행 보고 및 해당 실행 근거로 구분한다.
