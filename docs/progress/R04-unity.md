# R04 Unity 통합

2026-11-16. `r04-implementation-01.md` Unity 인계에 따른 PC 통합 구현·검증 및 독립 코드/UI 리뷰 PASS다. 열린 HIGH/MEDIUM은 없으며 판정 범위는 [코드 리뷰](../reviews/R04-unity-code.md)와 [대표 화면 리뷰](../reviews/R04-unity-ui.md)에 기록했다. Core 소스·수치·저장형식은 변경하지 않았다. 최신 Core를 한 번 sync했고 DLL SHA256은 `0ac82dfcf83a6a4dda8557ad70b5a76ee6b8e11449cda2b5d56942aa19e6d6e6`이다. 기존 Core 독립 PASS(관련7/최소2변이/대표 실제600kill CLI)와 외부 R04 아트 QA PASS를 재사용했다.

R03 완료 저장에서 R04 아브라함 진입, 첫3진화/장비·경험치·보스 보상 receipt 유지, 일반 전투·성장 선택·정산·65골드·불기둥 영구 해금·최대HP 회복·between5/한국어 R05 대기와 저장 재개를 연결했다. 불기둥 공격/장착·R05 진입은 지원하지 않는다. Host는 Core 정산과 기존 frozen 저장/retry 경로만 사용한다.

D1/D2/D3 move와 D3 windup/dash 총5PNG/16프레임은 별도 `ArtR04`로 연결했다. pinned index/manifest/PNG와 importer를 검증하고 기존 원본·GUID/폰트를 보존했다. windup은 Core 예고 경과 절반으로 두 프레임을 선택하고 dash는 돌진 경과로12fps 반복한다. 경고 캡슐은 같은 World snapshot의 live actor·고정 방향·실제 PlayerSpeed/ChargeSpeed/남은 시간·접촉 반폭을 사용한다. 넉백 이후도 선이 이동하고 lowFX에서도 배우/무기/입자 위·HUD 아래에 유지한다. 아트 보드의 정적 lane·경계 clamp·배우 아래 배치는 채택하지 않았다. 새 판정/피해는 없다.

관련 Host/round21개를 먼저 검증한 뒤 실제 Core 턱뼈 넉백 회귀를 포함한 고정 소스 최종 Unity Edit102/Play31이 한 번씩 PASS했다. 예고/돌진 pause·정확 disk Load, custom Tuning geometry/남은 시간, 실제 넉백의 고정 방향과 live endpoint, 성장 전 정산 보류·완료 once·쓰기 실패/retry·R05/가짜 불기둥 거절을 포함한다. 실제 compile와 단일 mac Development 빌드 exit0, project/portable/diff 종료 검사 exit0다. 새 asmdef/allowlist 확장은 필요하지 않았다.

격리 앱 `com.biblesurvivor.adam.r02artqa.r04abc58e01a090`에서 Host 이전 무저장 path probe/realpath를 확인했다. 기존 R03 앱의 완료 JSON을 새 identity에만 심고 HP10000·기존 진화 sling, backlog 직접 정리·무기 타이머 억제로 한 D3를 생존시켰다. 실제 Core 예고→돌진→cooldown을 관찰했고 두 단계 World/geometry disk replay와 완료 후 새 프로세스 Run/Profile 정확 동등을 확인했다. 완료 구간은 기술 timeout/성장 해결이다. 실제600kill 좌표 전투 근거는 Core CLI를 재사용하며 이 앱 fixture를 자연 전투/난이도 검증으로 세지 않는다.

ignored 근거는 `evidence/r04-unity-integration-20261116/`: `binding.json`, `handoff.md`, XML/log/실명령 exit·PID, 연속 trace와 660 lowFX 예고/돌진 및1100 완료의 정확3화면, selected DLL/provenance, 보존/복구 근거를 묶었다. probe/capture/reload 자기 child는 모두 exit0이고 자기 QA 저장3종만 absent로 복구했다. 보호 preview/unknownQA/iOS·해당 저장은 조회/조작하지 않았다. 기존734파일과 Core handoff 원문 보존을 확인했다.

상속 Sol/provider gpt-6.1-sol, 추가 Astra/helper 없음. 자기 phase start/end를 기록했으나 시작 provider event가 오래되어 interval은 정확한 R04 단독 귀속으로 해석할 수 없다. 비용/절감률/effort는 미측정이다. 자연밸런스·native focus/입력·모바일·기기·성능은 NOT_RUN이다. Editor/Player/제품 작성 소유를 root에 반환하며 독립 코드/UI 리뷰·최종 GC·commit/push·CI는 root 후속이다.
