# R05 롯 순수 Core checkpoint

2026-11-16 KST. 기준3ca6712와 `docs/harness/r05-implementation-01.md`. **관련 Core 검증 PASS·독립 코드 리뷰 대기**다. R04 완료→R05 normal→성장 정산→between6를 기존 경로로 연결했다. Unity/PC 앱 완료는 아니다.

catalog의690/원시XP1900/동시80/180초·170초 생성마감,180/230/280 웨이브를 사용한다. E1은60HP/8/.60, E2는80/9/.45, E3는90/9/.95의 기존 추적/접촉이다. 새 돌진·탄막·디버프·강제이동/지형 피해는 없다. 실제 speed 상수와 추적 계약을 R05 fingerprint에만 결합했다.

R04의 `round_settlement`/성장 선택 후 정산 함수를 재사용해 현재 최대HP 회복·80gold·daniel_lion 해금을 한 번 적용한다. R06과 미구현 사자의 장착/공격은 거절한다. 장비/진화 form·레벨/XP·킬 remainder/영구 snapshot/R03 CompletedBoss receipt를 유지한다. 새 R05 진입 시 현재 완료 flag와 전투 시계를 초기화하면서 R04 완료 사실은 선택 int `PreviousNormalRound=4`에 남긴다. 기존 R01~R04 저장은 이 default0 필드를 쓰지 않으므로 그 작성 형태를 유지한다. 새 저장/World version·전투상태 시스템은 추가하지 않고 schema2/World3를 재사용했다. R05의 charge 목록은 비어 있어야 하며 D3 actor/spawn/charge 누출을 거절한다.

`bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests -- --r05-only`는 **exit0,5/5**다. 진입/carry·receipt, 세 종의 실제 추적 속도/접촉과 플레이어 강제이동 없음, 진화 좌표 명중·중간 disk 동일 replay, pending 성장 뒤80gold/해금/회복·중복 정산/교체 실패·동일 payload retry·between6, charge 누출/잘못된 receipt·미래 round/필드/World 원문 보호를 묶었다.

원래 R04에만 있던 deferred settlement 분기로 되돌린 변이1개는 R05가 조기 일반 Finish를 쓰게 해 실제 pending성장/회복/골드 assertion 실패(exit1)를 검출했다. 복원은5/5 PASS(exit0)다. 근거는 `evidence/r05-core-01/summary.json`, `old_r04_only_settlement.log`, `restored.log`이며 최종 Core source hash를 포함한다. 기존 다른 변이는 재실행하지 않았다.

대표 `--r05-series sling 7`은 R1/R2 timeout·장비8/HP10000 기술 fixture로 R03 보스·진화·R04를 거쳐 R05 실제 좌표690킬/2095hits, 중간 Load,176.5167초 완료→between6를 통과했다(exit0). 골드710=R04 후492+690/5+완료80, HP10000, sling_fivefold·R03 완료 receipt·이전 R04 완료4를 유지한다. R05 charge 목록은 전체 전투 동안0이다. `evidence/r05-core-01/cli-result.json`은 실제 tool stdout 관측 요약이며 추가 CLI 실행을 만들지 않았다. 자연 밸런스/실플레이 수용은 아니다.

기존 전체 시험·18/10/30변이/호환 및 전무기/seed 행렬은 반복하지 않았다. 범위 확장에 맞춰 옛 future-round 입력과 resume mutation anchor만 다음 미지원 경계로 이동했고 기존18 anchor 고유성만 정적으로 확인했다. 대표 check_all CLI는 새 연속 경로로 교체했다. `git diff --check`와 변경 Python compile은 exit0다. 새 도구/helper 프로젝트는 없다.

이 작성자는 Unity/DLL/provenance·아트·CI/hooks·GUI·실사용 저장·commit/push를 변경하지 않았다. 독립 Core 리뷰 후 단일 Unity 통합자로 인계한다. R06/모바일/실플레이·성능·밸런스는 미구현 또는 NOT_RUN이다. 모델 Sol, phase `r05-core-01`의 자기 usage 시작/끝 기록을 남기고 청구액/비용 절감률은 미측정으로 둔다.
