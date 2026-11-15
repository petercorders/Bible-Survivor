# R04 아브라함 순수 Core checkpoint

2026-11-16 KST. 기준40b8093과 `docs/harness/r04-implementation-01.md`에 따라 R03 완료→R04 normal→성장 선택→between5를 구현했다. **관련 Core 검증 PASS, 독립 코드 리뷰 대기. Unity/PC 앱 통합 완료는 아니다.**

catalog의600/원시XP1600/동시70/180초·170초 생성마감, D1/D2/D3와 완료65골드/fire_pillar 해금을 사용한다. R03 보스의 완료/보상 receipt 전체는 `CompletedBoss`로 보존하고 R04에는 새 Wave/피격 시계/World를 시작한다. 장비·첫3진화 form·경험치·영구 snapshot·킬 remainder는 유지한다. R04 정상 종료는 남은 적을 보상 없이 정리하고 떨어진 XP만 회수한 뒤 `round_settlement`에서 성장을 해결한다. 이후 최대HP 회복·65골드·해금을 한 번 적용하고 between5를 저장한다. R05 진입 및 미구현 불기둥 장착/공격은 거절한다.

D3는 실제 생성시각+6초에 예고한다. 예고 시작의 플레이어 위치/방향을 고정하고1.2초 동안 정지한 뒤 속도비1.30으로0.6초 직선 이동한다. 겹친 목표의 방향은(1,0), 재사용은 돌진 종료+6초다. 기존 추적비0.60 및 접촉10/공통 무적을 사용하며 별도 돌진 피해를 추가하지 않는다. fixed step 안의 단계 경계는 실제 남은 구간만 적분해 돌진 거리가 기본속도3에서2.34가 된다. pause/선택/사망/완료에는 시간이 진행하지 않고 죽은 ID의 상태를 지운다.

D3 birth clock을 Wave의 해당 actor에만 추가하고, 새 World3 `Charges`에 named version1/ID/phase/NextAt/Until/locked target·direction을 저장한다. R04 active D3 저장은 좌표와 charge 상태를 함께 요구한다. 예고나 돌진 도중 Load가 timer/방향을 재초기화하지 않는다. 원본 timing·speed와 초기 기술 결정을 R04 fingerprint에만 결합했다. R01~R03 기본 serializer 필드는 바꾸지 않으며 새 선택 필드는 해당 이전 저장에서 쓰지 않는다. 미래 World/charge version·phase/필드 및 과거 보스 receipt의 미래 형태는 typed 읽기 전에 원문 보존 거절한다.

검증은 `bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests -- --r04-only` **exit0, 7/7**이다. catalog/진입·진화/receipt 유지, D3 목표고정·예고 정지·실제 거리/재사용·접촉 무적, pause/성장/죽음·새 ID, 예고/돌진 disk replay, pending 성장 후 정산/65골드/해금/회복 once·between5, 미래 원문 보호를 묶었다. D3 패턴 시험은 주입된 초기 웨이브 정리와 무기 타이머 억제를 명시한 geometry fixture이며 실제 이동·피해·저장은 World 경로를 사용한다.

기존 도구를 격리 사본에서 호출해 원래 일반 chase 분기로 돌아가는 변이와 Restore에서 charge 상태를 읽지 않는 변이 **2개를 실제 assertion 실패(exit1)로 검출→원복7/7(exit0)**했다. 새 도구/helper 프로젝트는 만들지 않았다. 근거/최종 Core source hashes: `evidence/r04-core-01/summary.json`, mutant2로그와 `restored.log`.

대표 `--r04-series sling 7`은 R1/R2 timeout·R03 장비8/보조1/HP10000 fixture에서 실제 R03 보스→진화·정산→R04 실제 좌표 전투600킬·1653hits→D3 cooldown 상태 저장/재개→between5를 통과했다(exit0). R04 elapsed177.1667, 골드492, 체력10000, sling_fivefold와 R03 receipt를 유지했다. 이 진화 무기는 D3를6초 전에 처치하므로 CLI의 `observed_charge=false`를 사실대로 남겼다. 실제 예고/돌진·해당 중간 Load는 위 별도 시험에서 확인했다. 자연 난이도/밸런스 PASS는 아니다. CLI 관측 요약은 `evidence/r04-core-01/cli-result.json`이다.

최초 fixture는 D3 생성 전61초에 정리를 끝내 ID를 찾지 못해63초의 실제 생성 구간으로 수정했고, 타이밍 fixture의 .01초 이동을 fixed step으로 맞췄다. 죽음 fixture는 직전 접촉 무적 때문에 주입 피해가 거절된 것을 확인해 무적 시계를 명시 초기화했다. 첫 CLI의 “반드시 예고 관측” 조건은 정상 선처치를 실패로 세어, cooldown checkpoint 재개와600실제 처치/완료를 수용 조건으로 고쳤다. 제품 피해 규칙/돌진 타이밍을 시험에 맞춰 바꾸지 않았다.

기존138/18/10/30 및 전무기 CLI 행렬은 반복하지 않았다. 범위 확대에 해당하던 옛 future fixture를 다음 미지원 round/World로 옮기고 resume 변이의 지원 상한 anchor만 갱신했다. 기존18 mutation anchor가 각각 한 곳을 고유하게 선택함을 정적으로 확인했으며 전 변이를 실행하지 않았다. `git diff --check`와 변경 Python compile은 exit0다. check_all의 대표 CLI는 R04 연속 경로로 교체했다.

Unity/DLL/provenance·아트·CI/hooks·GUI·실사용 저장·commit/push는 이 작성자가 변경하지 않았다. 모바일/성능/실플레이/아트 검수 및 독립 코드 리뷰는 NOT_RUN/대기이며 다음 단일 Unity 통합자와 coordinator에게 인계한다. 모델 Sol, usage phase `r04-core-01` start/end를 기록하며 청구 비용/절감률은 미측정이다.
