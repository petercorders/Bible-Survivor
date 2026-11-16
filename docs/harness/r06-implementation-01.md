# R06 이삭 — 두 번째 보스 수직 통합

2026-11-16. R05의 commit/원격 CI 종료 뒤 사용자 지속 개발 승인으로 시작한다. 다음 산출물은 R05완료→R06일반→보스→선택보상/진화→성장/정산→between7 PC 앱이다. R07과 새 무기 공격은 이번에 추가하지 않는다.

## 기획과 기술 초기값

catalog genesis_06: 180초/생성마감170초/동시90/총780/원시XP2400, wave200/260/320 및 기존 조합. F1 물 약탈자80HP/8피해/.70, F2 우물 메움마120/10/.50, F3 다툼의 갑주270/12/.35 모두 추적·접촉이며 전면무적/새 디버프는 없다. 일반 종료는 기존2초 보호 intro·남은 일반 적 정리/드롭만 회수 후 회복 없이 보스전으로 넘어간다.

싯나의 우물지기(창세기26장 우물 메움·다툼의 창작 마물): HP6500/접촉16, 고정골드200, 공격주기7초/예고1.2초/피해18, HP70%/35%에 F1 각6마리 한 번씩·동시상한12. 보스 생존 중 한 공격으로 두 문턱을 넘으면 두 번의 소환 기록을 남기고 최대12를 생성하며, 즉사한 보스는 지원군을 새로 만들지 않는다. 남은 지원군 제거는 처치 보상이 아니다. 지원군 처치는 기존5킬 골드 규칙, XP0은 R03와 동일한 기술 초기값으로 명시한다.

기술 초기값은 기존 느린 보스의 속도비.30/반경.60/introoffset(0,+3)/표시 반경1.20을 재사용한다. 보스 elapsed7초에 첫 피해,5.8초부터 예고하면서 플레이어 목표를 고정하고 이동 정지한다. catalog의 projectile_count0을 유지하며 신규 적 투사체 엔진 없이 예고 종료 시 해당 원에 흙더미 낙하 피해를 한 번 적용한다. 흙/포물선은 시각 표현이고 잔류 흙은 장식뿐이며 길막/지속피해를 만들지 않는다. 원본 투척의 비행시간 미정은 이 초기 지점공격 계약으로 명시한다. 실제 Core target/radius를 Unity가 투영하고 lowFX에서도 필수 고리를 유지한다. 주기/목표/소환mask/남은시간은 명시 저장·fingerprint에 포함하며 boss timeout은 없다. 피해·사망 우선순위/접촉무적/영구재생·첫3진화 효과를 유지한다.

## 상태와 보상 보존

R03 보스 구현을 라운드별 정의로 재사용하며 R01~R05 과거 저장·R03 encounter ID/내용/fingerprint를 조용히 변경하지 않는다. R06 active/보상/완료와 과거 R03 보상·완료 receipt를 동시에 보존한다. 가장 작은 명시 DTO/optional history를 선택하고 중복/순서/round·boss identity를 검증한다. R06 완료가 과거 R03 receipt를 덮거나 재활성화하면 안 된다. 모르는 future schema/encounter/policy/shape는 원문 보존 거절한다.

보스 고정200은 일반킬 remainder와 분리해1회 지급, 같은 고정 후보/receipt·최대3고유/가능한 첫3진화 최소1보장·부족시골드100fallback 기존 규칙을 재사용한다. 이미 진화한 무기를 다시 진화시키지 않는다. 보상선택→남은 성장→완료100gold/cross 영구해금/현재최대HP회복→between7를1회 처리한다. R03의 저장된 정책은 그대로 해석하고 R06용 정책/encounter ID를 명시해 재시도/Load가 재추첨·중복지급하지 않게 한다. 미구현 십자가 장착/공격과 R07 진입은 거절한다.

## 단일 작성자·수용 범위

### Unity 단일 통합 인계

Core 독립 PASS 후 최신 sync1회·R05완료→R06일반→intro/boss/reward/성장/정산→between7 Host/catalog·한국어 이삭/보스명/다음미구현대기·검수된7PNG/25프레임 sidecar를 한 PC 앱으로 연결한다. `Session.BossRules`/`Boss.Rules`의 동적HP/actorID/7초/contact16/공격18/보상정책을 사용하고 R03의1800HP/actor481/6초를 하드코딩한 표시를 R06에 재사용하지 않는다. 기존 Target/NextAttackAt/BossElapsed/실제SlamRadius로 낙하지점·경과 프레임을 투영한다. lowFX 필수 고리는 배우 위/HUD 아래를 유지한다. 흙 비행·잔류 장식은 이번 필수 범위가 아니며 새 피해/길막/투사체 엔진을 만들지 않는다. 사망 clip은 기존 짧은 연출 후 제거하고 마지막 물우물을 영구 지형으로 남기지 않는다.

R03 완료/보상 기록과 기존 진화·R04/R05 완료 사실을 보존하고, 후보/200고정골드/선택골드/100완료/해금/회복은 Core receipt로만 적용한다. 관련 Host검사는 R06 진입/동적 보스매핑·70/35두단계/저장재개·고정후보와 이미진화한무기 제외·남은성장/동일payload retry/정산once·between7를 기존 경로로 확인한다. 실제 앱은 한무기 기술준비와 하나의 연속기록으로 예고·threshold저장/후보재개·보상선택·완료Load를 묶고 최대3화면으로 남긴다. 실제확인하지못한 phase는 시험근거로 구분하고 모든조합/전프레임캡처를 늘리지 않는다.

Unity Runtime/Editor/관련tests·7클립whitelist/index/importer·DLL/provenance sync·progress/retro만 단독소유한다. Core제품/수치/저장형식·기존CIhooks/원본아트/사용자앱저장은 변경하지 않는다. 기존 보호 preview/unknownQA/iOS를 조회조작하지 않고 새suffix/no-saveprobe/정확자기PID·자기저장복구만 사용한다. 고정source 최종Unity검사1회·단일macbuild 후 소유를 반환한다. 독립코드/UI·GC·commitpush/CI는coordinator후속이다.

Core 소유: Core/관련tests·Sim/필요최소기존검사anchor·progress/retro. Unity/DLL/provenance/art/CIhooks/GUI/실사용저장/commitpush 금지. 범위가 실제 기획과 충돌하면 좁은재현으로 보고하며 새 시스템/콘텐츠를 늘리지 않는다. 관련시험은 단계전환·기존R03회귀·7초고정지점공격/정지·regen/치명우선·70/35각1회/12상한·threshold/boss reward 중간SaveLoad·과거receipt보존·진화/고정후보/성장/중복정산·미래보호를 묶는다. 대표실좌표CLI 한무기한seed, HP/장비 기술 준비를 명시하고 실제 히트/보스킬은 주입하지 않는다. 원래위험 최소변이만 확인, 기존전체행렬/새검사기프로젝트/helper는 추가하지 않는다. 저장보상 위험은 Sol high 독립리뷰로 확인하며 Sol로 해결 못 한 난해한 경우에만 Astra 상향을 검토한다.

지정 아트 담당은 외부 art-genesis-r06에 F1~F3 move32x4와 보스64 move4/windup3/impact2/death4·필요그림자만, 기존PPU32/pivot16,1 및32,1/right-parentflip계약을 재사용해 최소source/manifest/계약·정상regen1회·대표혼합/흑백/위험단계 보드최대3장을 만든다. 보스는 우물돌·흙·두레박사슬의 창작 마물이며 이삭이나 성경 인물을 악마로 표현하지 않는다. 지점경고는 실제Core로 그리고 장식흙은피해지형처럼그리지 않는다. Design 제품과 스크립트 픽셀을 구분하고 비공개자료는repo에복사하지 않는다.

Core/아트독립리뷰→Unity단일통합·관련검사/고정source최종1회/단일PC빌드·대표기록/3화면→독립코드/UI→프로젝트GC→commitpush/해당SHA CI. 사용자 preview/unknownQA/iOS 보존, 모델Sol기본·관측된자기사용량만 기록하고 비용/절감률은추정하지 않는다. 자연밸런스/native/mobile/performance 미검증을 기술 fixture PASS와 구분한다.
