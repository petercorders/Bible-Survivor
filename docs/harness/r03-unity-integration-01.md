# R03 플레이 가능 Unity 통합

2026-11-16. 사용자 지속 개발·PC 우선 리뷰·GC/업로드 승인으로 진행한다. Core 실행02 독립 코드리뷰 PASS와 성경 sidecar 실제 화면 리뷰 PASS를 선행 근거로 사용한다. 다음 플레이 가능 통합본이 목표이며 추가 별도 개선으로 범위를 넓히지 않는다.

## 연결할 행동

- 최신 승인 Core를 schema2 도구로 한 번 동기화한다. R01/R02에서 R03 일반전→보호 intro→노아 보스→보상 선택→남은 성장 선택→완료 회복/정산→R04 미구현 대기까지 Host를 연결한다. 보상과 일반 레벨업 선택은 Core가 제공하는 후보/효과/receipt를 사용하고 Unity에서 다시 추첨하거나 지급하지 않는다.
- 외부 `art-genesis-r03/`의 QA 승인 자산/계약 중 실제 필요한 파일만 별도 sidecar로 가져온다. 기존 Art03·R02·성경 개선 sidecar를 보존한다. C1/C2/C3·보스 move/windup/impact/death를 Core 상태에 맞춰 표시하며 intro idle은 move0 고정이다. 네피림은 게임 각색 보스라는 원본 문서 구분을 유지한다.
- 공격 예고는 실제 고정 Core target/radius를 기존 WorldProjection으로 그린다. 명목 PNG 크기를 피해 판정으로 사용하지 않는다. 중요한 경계는 배우/P1 위·HUD 아래에서, 저효과 모드에서도 읽히게 한다. optional 금 데칼은 꼭 필요하지 않으면 생략한다.
- 보스 체력/phase와 한국어 보상 카드·진화 형태를 표시한다. 최대3후보와 한 번의 선택, 저장 실패/재시도·Reload·일시정지/사망의 기존 Host 경계를 유지한다. 미구현 R04는 명시 대기로 남긴다.

## 단일 작성자와 보호 경계

담당자는 Unity Runtime/Editor/관련시험·R03 sidecar·최소 importer/index·DLL/provenance sync 산출물·필요한 정확한 검사 allowlist·진행/회고를 소유한다. Core 제품/수치/저장 형식은 독립 리뷰된 상태로 고정한다. 실제 막는 Core 결함이면 좁은 재현과 함께 coordinator에게 인계한다. CI/hooks/전역 하네스와 다른 담당자의 파일은 임의 변경하지 않는다. 커밋/push는 coordinator가 한다.

사용자 PC preview identity `com.biblesurvivor.adam.r02artqa.pivot9534f7594886` 및 PID14039/window217455, unknown QA `com.biblesurvivor.adam.particlesqa` PID97399/window198185와 해당 저장, 사용자 iOS는 조회·입력·종료·원복하지 않는다. 새 suffix QA identity와 무저장 path probe를 사용하고 정확한 자기 자식 PID만 관리한다. 자기 QA 저장만 종료 확인 뒤 원복한다.

## 완료와 필요한 검증

기존 Core138·3변이·대표 series 및 sidecar 관련Edit2/대표3화면·독립 리뷰는 재사용한다. 새 Host 전환/보상 선택·재개/정산과 실제 투영 경계의 관련 회귀를 추가하고 실제 compile·필요한 Unity 시험·macOS 빌드를 실행한다. 같은 고정 소스의 전체 검사는 종료 게이트로 한 번이면 충분하며 문서 정리 때문에 다시 빌드하지 않는다.

대표 PC 확인은 한 무기/한 seed의 명시 기술 checkpoint로 intro/보스 위험 경계·보상 적용·재개/완료 대기를 확인한다. 가능하면 한 연속 짧은 기록과 핵심3화면 이내로 묶는다. 전무기×모든phase×크기 픽셀/해시 행렬은 만들지 않는다. 자연 난이도/장시간 완주·native 정밀 타이밍·모바일·성능은 실제 확인하지 않으면 NOT_RUN이다. 필수 판정·저장 동등성은 관련 시험으로 확인하며 기술 fixture를 자연 밸런스 PASS로 표기하지 않는다.

최종 제품소스 고정→독립 코드 및 대표 화면 리뷰→막는 결함만 수정→프로젝트 GC→선별 commit/push와 해당 SHA CI 순서다. 모델은 Sol 우선이며 usage 시작/끝과 미측정 비용을 정확히 남긴다.
