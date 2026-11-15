# R04 아브라함 — 다음 PC 수직 통합

2026-11-16. R03 독립 리뷰·GC·commit/push 및 최신40b8093의 두 CI PASS 뒤 사용자 지속 개발 지시로 진행한다. 다음 산출물은 R03 완료 저장에서 이어지는 R04 전투·완료·저장 가능한 PC 앱 하나다. R05는 미구현 대기다.

## 기존 확정 규칙

권위는 catalog genesis_04와 genesis-balance: 180초, 생성 마감170초, 동시70, 총600/원시XP1600, 3구간160/200/240 및 기존 D1/D2/D3 조합을 유지한다. D1 광야 추적자45HP/7피해/.65, D2 미혹의 등불60/7/.50 추적(탄막 없음), D3 약탈 기병마100/10/.60, 1.2초 예고 뒤 속도비1.30으로0.6초 돌진·재사용6초. 보스 없음. 완료65골드·불기둥 영구 해금·성장 선택 뒤 최대HP 회복, 장비/첫3진화/경험치/구매/해금 유지. 미구현 불기둥은 해금만 기록하고 가짜 공격/획득 후보는 추가하지 않는다.

## 구현 경계와 초기 기술 결정

Core 단일 작성자가 R03 between4→R04→between5, D3 상태/clock/목표·방향·재사용을 실제 좌표 전투와 저장에 연결한다. 첫 돌진은 생성6초 뒤 예고 시작, 목표는 예고 시작 때 플레이어 위치로 고정, 예고 중 이동 정지, .6초 직선 이동 후 기존 추적으로 돌아가고 종료부터6초 재사용한다. 겹친 목표는 안정 고정 방향을 사용하며 기존 아레나 경계/접촉 피해·무적 규칙을 따른다. 기술 초기 결정은 명시 상수와 fingerprint에 넣는다. pause/성장선택/사망/완료에는 돌진 시간이 진행하지 않는다. 죽은 적·재사용된 슬롯의 돌진 상태가 새 적으로 유출되지 않는다. 돌진은 별도 추가 피해를 만들지 않는다.

저장에는 필요한 명시 named shape/version을 사용하고, 예고·진행 중 방향/남은시간/재사용을 Load해 공격이 다시 조준되거나 반복되지 않게 한다. 옛 R01~R03 저장은 동작을 보존하고 모르는 미래 상태는 원문 보존 거절한다. 새 단계 때문에 기존 진화 효과·회복/정산 receipt가 사라지지 않아야 한다. 코드 사실/기획에 충돌이 발견되면 좁은 재현으로 보고하며 임의 스코프 확대는 하지 않는다.

## 병렬 및 통합

Core 작성자 소유: Core/관련시험·Sim CatalogReader와 대표 CLI/필요최소검사 anchor·진행/회고. Unity/DLL/provenance/art/CIhooks/GUI/실사용 저장/commitpush 금지. 지정 Claude Design 담당은 별도 외부 art-genesis-r04에서 D1~D3와 D3 예고/돌진 상태의 작은 계약·필수 PNG/대표 보드를 준비한다. 원본 시트/계약을 유지하며 실제 Design 사용과 스크립트 픽셀 제작을 구분한다. 아트 독립 검수 후 Unity 단일 통합자가 고정 Core sync·실제 D3 위험 신호·보상 없는 완료/다음대기를 한 앱으로 연결한다.

## Unity 통합 인계 — Core·아트 독립 PASS 이후

Core는 `docs/reviews/R04-core-code.md`, 아트는 외부 `art-readiness-reviews/r04-01-review.md`에서 열린 HIGH/MEDIUM 없이 PASS다. Unity 단일 작성자가 최신 Core sync와 D1~D3 move/D3 windup·dash 5클립 sidecar, R03→R04→between5 Host·한국어 안내를 한 PC 앱으로 연결한다. 기존 아트·첫3진화·사용자 앱/저장은 보존한다. 미구현 R05와 불기둥 공격은 추가하지 않는다.

통합 결정: 아트의 완전 정적 경고선은 넉백 이후 실제 경로와 어긋날 수 있으므로 그대로 채택하지 않는다. Core의 고정된 방향/목표는 유지하고 경고선의 시작은 현재 배우 위치를 따른다. 예고 endpoint는 현재 위치+direction×PlayerSpeed×ChargeSpeed×ChargeDuration, 돌진 중에는 현재 위치+direction×실제 남은 돌진시간×PlayerSpeed×ChargeSpeed다. 반폭은 실제 PlayerRadius+EnemyRadius다. Core `ChargeStates`/`EnemyPositions`/`SimulationTime`/Capture Tuning으로 계산하며 보드의2.34/.45는 하드코딩하지 않는다. 예고 중 넉백이면 선도 이동하고, 이를 판정과 일치시키는 기술 표시로 명시한다. Core에는 기존 아레나 clamp가 없으므로 새 clamp/판정을 도입하지 않는다. 필수 경고선은 lowFX에서도 배우 위/HUD 아래에 표시하고 번쩍임 없이 예고/진행 상태를 구분한다.

Unity 소유: Runtime/Editor/관련시험·5클립 whitelist/index/importer·DLL/provenance sync·필요 최소 정확 allowlist·progress/retro. Core제품/수치/저장형식/기존 CIhooks는 변경하지 않는다. 새 독점 QA identity/무저장 probe/정확 자기 PID·자기 저장 복구 경계를 따른다. 관련 Host 완료/저장재개와 실제 D3 예고→돌진/중간Load/넉백시 live projection을 최소 대표 검사로 확인한다. 강한 진화 sling 연속 CLI가 D3를 예고 전에 죽이는 기존 근거는 재사용하고, Unity 기술 fixture에서는 한 D3가 살아 있는 예고/돌진을 명시적으로 관찰한다. 이 fixture를 자연 난이도 검증으로 표기하지 않는다. 고정source 최종 Unity검사/단일 PC빌드/대표기록·핵심3화면 후 소유를 반환한다. 별도 전무기·전단계·해상도 행렬이나 모바일 검증은 늘리지 않는다.

## 좁은 수용 검사

Core는 대표 D3 예고 목표고정/돌진 거리·타이밍/정지와 재사용/피해중복 없음/죽음과 슬롯초기화/진화 유지/예고와돌진 SaveLoad/완료1회·between5를 관련 시험으로 묶는다. 대표 연속 CLI는 한무기·한seed, 실제 좌표 공격을 사용한다. 실제 위험 최소 원구조 변이만 검출하고 기존138/18/10/30 전체 행렬은 반복하지 않는다. 기존 mutation 문자열이 새 완료 함수와 중복되는 문제는 대상 guard가 보장하는지 관련 diff에서 확인한다.

독립 Core 리뷰→아트 QA→단일 Unity 통합→관련 Unity검사·고정source 최종1회·한 대표기록/핵심3화면→독립 코드/UI→프로젝트GC→commitpush/해당SHA CI. 자연 밸런스·모바일/성능을 기술 fixture PASS로 확대하지 않는다. 사용자 PC preview/unknownQA/iOS는 기존 보호경계를 유지한다. 모델 Sol 기본·어려운 실패만 Astra 검토, usage 시작/끝과 누락/비용 미측정 구분. 별도 작은 개선·검사기 프로젝트·추가 helper·인계용 재빌드는 늘리지 않는다.
