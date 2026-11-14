# R02 Unity 기술 연결 작업 인계

사용자의 창세기 개발·결제 외 지속 진행·멀티에이전트·PC 리뷰/라운드별 GC·GitHub 업로드 승인을 따른다. P1 파티클 독립 리뷰/GC/커밋 경계 뒤 root가 단일 구현자에게 실행을 넘긴다. 상세 상태/전환/저장·실패/수용은 `unity-r02-plan.md`를 기준으로 하며 과거 Unity42/15 기록은 현재 baseline으로 재사용하지 않는다. 직전 P1 최종 기준은54Edit/20Play이며 실제 시작 시 새 실행으로 확인한다.

## 구현 범위·보존

- 기존 R01/R02 Core와 저장 schema·DLL/provenance·catalog 원본을 우선 그대로 사용한다. AdamCatalog/Host/Game/ArtWorld의 R01 고정 경계를 R02까지 연결하고 관련 시험/필요한 QA 빌드·fixture만 수정한다. R03 진입/보스/새 무기는 추가하지 않는다.
- R01 완료의 남은 성장 선택을 먼저 처리하고 마지막 성공 checkpoint 뒤 한 번만 R02로 전환한다. R02 진입 저장 성공 전 전투를 진행하지 않는다. RunId/성장·장비/골드/잔여XP·킬 잔여/영구 스냅샷 유지, 회복·보상은 Core가 한 번만 적용한다. 새 World가 이전 적/드롭/공격·피격/시간·오디오/피격/입자 cursor를 비운다.
- 기존 normal1/2·between2/3·legacy 허용 범위/미래 schema·fingerprint 보호·저장 실패 freeze/동일 operation retry를 유지한다. R02 사망 후 새 도전은 R01이다. R02 완료는 R03 대기임을 정확히 표시하고 진행을 지우지 않는다.
- 원본03/font/GUID·Core와 기존CI/checker/hook은 수정하지 않는다. 사용자가 실행 중인 iOS/기기/저장과 출처 미확인 옛 Particle QA 프로세스/저장은 조회·조작·종료·원복하지 않는다. 일반 PC 저장에 fixture를 쓰지 않는다.

## 아트 의존 분리

`unity-r02-plan.md`의 세 적 b1/b2/b3 stable ID와 Claude Design 요청 계약을 따른다. 제품 연결이 실제로 확인된 Claude Design 산출물 없이 다른 worker/절차 그림을 Design 제작으로 표시하지 않는다. 원본03의79PNG·고정 SHA를 느슨하게 바꾸거나 b몹을 기존 a몹으로 조용히 매핑하지 않는다.

최종 시트가 없으면 세 종이 구분되는 **명시 개발용 기하 표식**으로 기술 연결·전투·저장 시험만 독립 진행할 수 있다. 적별 ID/종류/임시 표시를 구분하고 최종 시각 PASS·완성 라운드로 보고하지 않는다. 검수된 별도 R02 sidecar의 추후 로더/ID 매핑 경계를 준비한다. 최종 아트 검수 전 R02 시각 게이트는 NOT_RUN이다. 그림이 없다는 이유로 Core가 이미 제공하는 전환·복구·수치 검증을 중단하지 않는다.

## 실행·검증·인계

단일 Editor/Player/GUI 소유자로 marker·새 앱ID/product suffix·새 출력·무저장 persistentDataPath/realpath probe·정확 PID/창만 사용한다. generic bundle open/restore-window/앱 종료 후 재실행 루프 금지. 새 QA ID에 다른 writer가 있으면 원복하지 말고 보존한 채 root에 보고한다. 종료/원복은 자기 새 QA3만, 원본PC6과 iOS/옛QA는 건드리지 않는다.

새 round Host/catalog/기존저장·선택/전환/재시도·CoreWorld 초기화·HUD/사망/파티클/입력 회귀, 원래R01-only/선택전전환/oldWorld유지/실패중복operation/아담고정 UI 변이의 행동 실패→원복PASS를 별도사본에서 확인한다. 새 source/build/XML·실제명령exit·기존원본 보존과 개발용아트 경계를 결속한다. 실제3분 전후 연속·fixture/자동시뮬레이션/자연수동을 각각 구분한다. 기존Core89/변이18·P1 관련54Edit20Play를 후퇴시키지 않는다.

문서 소유: `docs/progress/unity-r02.md`, `docs/retrospectives/R02-unity.md`; ignored 별도 evidence를 사용하고 과거 증거를 덮어쓰지 않는다. root가 독립 PC 코드/UI 리뷰→수정/재검토→GC→선별 commit/push/해당 SHA CI를 맡는다. 구현자는 추가worker/commit/push 없이 종료·파일/GUI소유와 실제아트 미완료를 인계한다. 외부 제품 연결이 필요하면 구체 브리프/실화면/필요납품을 root에게 전달한다. 결제 작업만 중단해 확인하며 무료 독립 작업은 계속한다.
