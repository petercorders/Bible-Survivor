# R02 Unity 기술 연결 — 독립 PC 코드 리뷰

2026-11-14. 기준4971277bc97c59b189c4be015b7c1701518615e6. **코드/대조한 실행 근거 범위 PASS, 추가 HIGH/MEDIUM 없음.** 최종 R02 아트·native active 입력·모바일/성능·밸런스 수용을 뜻하지 않는다. AGENTS·전용 하네스·R02 계획/작업 인계·진행 문서·현재 소스와 ignored 최종 근거를 읽었다.

검토자는 이 보고서만 작성했다. 제품/Core/Editor/시험/앱/GUI/저장·기기를 실행하거나 수정하지 않았고, iOS 및 옛 출처 미확인 QA 프로세스/저장은 조회·조작하지 않았다. JSON/XML/파일 SHA와 저장 **증거 사본**을 Python으로 읽어 비교했다. 실제 Editor/Player·native 입력·빌드·변이 재실행은 NOT_RUN이다.

## 전환·저장 경계

- `AdamCatalog`는 고정 원본에서 정상 R01/R02만 읽고 identity·보스 없음·웨이브/예산·몬스터 참조를 검증한다. 기존 Definition은 R01 계약을 유지한다. checkpoint는 normal이면 해당 round, between_rounds이면 완료한 round(number−1)를 선택한다. R03 정의를 카탈로그에 보유했다는 이유로 노출하지 않는다.
- Host의 AwaitingR02/ContentComplete/CanStartNew가 완료 대기와 새 도전을 분리한다. pending 선택이 남은 R01 완료는 시작 버튼으로 덮어쓰거나 R02로 넘어가지 않는다. 선택을 적용하고 성공적으로 checkpoint한 뒤에만 completedCheckpoint와 남은 선택 조건을 만족한다.
- 단일 EnterR02가 기존 Core StartNextRound를 호출하고 새 BattleWorld를 만든다. 전환 자체는 보상/회복을 추가하지 않는다. 기존 Core가 처리한 RunId·성장/잔여XP·장비/보조·영구 스냅샷·골드/해금·kill remainder를 유지하며 새 시간·위치·적/투사체/드롭/공격·피격 시계를 사용한다.
- 진입 checkpoint 전 전투를 진행하지 않고, 실패하면 Error/Paused로 같은 새 Session/World·payload·operation을 동결한다. Tick/Choose/Start/SetPaused로 실패 payload를 바꾸거나 두 번째 전환을 만드는 경로를 막는다. RetrySave는 같은 operation/revision을 사용하고 성공 후에도 명시 재개를 요구한다. 결과 불명 시험은 stage 파일을 primary로 복사해 원자적 Commit의 멱등 분기를 재현한 것이며 실제 OS 교체 callback이라고 보고하지 않는다.
- Reload는 normal1/2·between2/3를 올바른 정의로 Resume하고 기존 Core의 fingerprint/legacy/미래 schema 보호를 따른다. legacy는 R01/그 완료 대기만 허용한다. World 없는 저장의 새 World 구성은 좌표 정확 복구로 표시하지 않는다. 실패한 Reload가 새 도전을 자동 시작하거나 디스크를 덮어쓰지 않는다.
- R02 완료 후 between3과 완료한 R02 세션을 보존한다. `노아 라운드 준비 중 (R03 미구현)`은 콘텐츠 끝 안내이며 R03 보스 진입이 아니다. 그 상태에서 남은 선택을 해결한 명시 새 도전 또는 사망 후 새 시작만 R01로 돌아간다. 해금 기록을 신규 무기 공격 구현으로 확대하지 않는다.

## 표현·입력·아트 경계

Game은 시작/종료의 아담 고정 문자열을 round 기반 표시로 바꾸고 R01 완료 계속 화면을 별도 처리한다. 오류→pending 선택→사망 clip→R02 대기/콘텐츠 끝 순서와 사망 SelectSlot 공통 가드를 유지한다. 같은 frame의 선택/held 키가 여러 명령으로 번지지 않도록 기존 commandFrame/release gate를 사용한다. World identity 변경 전후에 movement/touch ownership을 해제하고 이동키는 release/repress 후 적용한다.

새 World에서 ArtWorld generation이 바뀌며 반응/사망/FX/cursor를 비운다. Audio/Particle는 같은 generation 변화를 통해 이전 발음/입자/사건을 비운다. P0/P1 소유 자원·Core 명중 로직을 복제하지 않고 새 카탈로그/Host 결과를 읽는다. 실제 R02 on/off 동등 시험은 단순 빈 장면이 아니라 명중이 있는 전투를 요구한다.

b1/b2/b3는 원본 a몹으로 조용히 매핑되지 않는다. 명시 개발용 기하 표식과 HUD 안내를 사용하며 미지 ID는 예외다. small/wide shadow는 기존 검수 자산을 공통 효과로 재사용한다. 별도 R02ClipId는 미래 sidecar의 안정 ID 경계일 뿐 지금 최종 시트를 로드하지 않는다. 원본03 고정 index/79PNG·font·GUID를 느슨하게 만들지 않는다. 기하 기술 화면 PASS는 최종 R02 아트 PASS가 아니다.

## 독립 읽기 전용 대조

| 대상 | 직접 확인 결과 |
| --- | --- |
| binding-final → final-source-v2-sha | **331/331 현재 파일 SHA 일치**, manifest SHA도 일치 |
| 최종 앱 | **334/334 파일 SHA 일치**, file-map manifest SHA도 재계산 일치 |
| QA Runtime/시험 | 최종 binding의 **27/27 파일 byte 동일** |
| 보호 원본 | **222/222 초기 source/baseline bytes·SHA 동일**, Core/DLL/catalog/schema·아트/font/설정 경계 포함 |
| 현재 Host/Game 변이 결속 | 각각 최종 변이 원복 source SHA와 동일 |
| 최종 엔진 XML | **66 Edit/26 Play PASS**, failed/skipped/inconclusive0 |
| 실제 새 프로세스 Load | persisted-r02와 reload-state의 Run/Profile 직접 일치; World도 저장 안의 World와 대응 |
| 표현 on/off | 실제 R02 명중을 포함하는 전체 suite의 새 시험 PASS. Run/Profile/World serializer 결과 동등 요구 |

최종 소스 manifest SHA는 `34d208d9fe6c598fd540d10c38758accc8f30a580fc735eb77a307d2f5789202`, 앱 file-map manifest SHA는 `82c2c38ed7fd697199ea591f6cb58349027e305804d4643827c1247b5bf45a95`다. 파일 동등은 현재 디스크에서 직접 계산했고 raw 증거 위치는 `evidence/unity-r02-20261114/`다.

마지막 변경은 R02 표현 on/off PlayMode 시험1개이며 제품 Runtime/Editor/QA fixture는 앱 빌드 소스와 같다. 앱에 Adam.PlayMode/Adam.EditMode 시험 어셈블리가 없음을 확인했다. Unity 내부 Collections 테스트 보조 DLL의 이름까지 모두 없다고 주장하지 않는다. 현재26Play는 격리 QA에서 최신 시험을 전체 재실행한 기록이고, 이전25개 기록을26개로 재명명한 것이 아니다.

## 원래 구조 변이의 적합성

`mutation-results.json`의 baseline0·복원0과 다음 실제 assertion 실패를 확인했다.

- R01 전용 Reload: R02 저장/완료 정의 복원 경계 실패.
- pending 선택 guard 제거: 선택이 끝나기 전 전환/덮어쓰기 방지 실패.
- old World 유지: R02 초기화·저장 정합/새 arena 요구 실패.
- retry에서 새 operation 생성: 진입 저장의 동일 payload/멱등 재시도 요구 실패.
- 옛 generic terminal UI: 실제 전체 renderer의 작은/큰 계속 버튼 클릭이 R02에 진입하지 못함.

모두 컴파일 오류가 아닌 assertion을 포함한 exit2다. 현재 최종 Game source에서 옛 완료 UI 변이를 다시 적용한 `mutation-uifinal-results.json`도 작은/큰 클릭2FAIL→복원5Play PASS다. Host source는 첫 변이 원본과 동일하고 Game source는 마지막 UI 변이 원복과 동일함을 SHA로 확인했다. 최종 추가 on/off 시험이 변이 실행 후의 독립 시험 추가라는 시간 관계도 보존했다.

## 실행·격리 근거의 한계

QA Editor 진입점은 marker·새 절대 출력·suffix identity를 요구하고 기존 capture driver가 있는 씬을 거절한다. 실제 첫 두 capture 실패는 같은 World 시계가 아닌 fixture와 중복 driver로 인한 것이며 정상 증거로 합산하지 않는다. 첫 무저장 gate는 Host 이전에 identity/경로를 확인하고 종료한다. 새 process Load/capture는 실제 명령·자식 exit0으로 남는다. root가 인계한 최종 path probe/실행자료를 읽었으며 본인 앱을 실행하지 않았다.

PC 원본6개 보존·새 QA3개 부재 원복·새 QA identity writer 부재는 최종 구현자 기록으로 확인했다. 사용자 원본 저장을 직접 읽지 않았다. `final2/roundtrip.json`의 원복 문구는 생성 당시 pending이며, 최종 원복 판단은 뒤의 `final2/restoration.json`을 기준으로 구분했다. native quit screenshot parser/fallback 뒤 자식 exit0은 정상 native quit 수용 시험과 다르다.

native synthetic 키/클릭 후 world time이 변하지 않았으므로 active 입력·자연 수동 연속 플레이는 NOT_RUN을 유지한다. 실제 PlayMode InputSystem/IMGUI 결과가 이를 대신하지 않는다. 봇 R02에서 근접 두 무기는2명중/0킬 시간 완료, 물맷돌은521명중/360킬의 전멸 완료였으며 자동 선택/고정 입력의 기술 결과다. 부상 HP1 fixture의 세 무기 처치/성장 시험도 전체 체력 밸런스나 자연 수동 플레이로 합산하지 않는다.

추가 HIGH/MEDIUM은 발견하지 않았다. 실제 PC UI 독립 검토와 최종 R02 아트 검수/승격·GC·commit/push/해당 SHA CI는 coordinator 후속 게이트다. 최종 아트·R03 보스·새 무기/진화·모바일/실물/성능은 본 기술 연결 PASS에 포함되지 않는다.
