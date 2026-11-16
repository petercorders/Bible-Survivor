# R07 야곱 순수 Core checkpoint

2026-11-16 KST. 기준092bc4c, 실행 계약 `docs/harness/r07-implementation-01.md`. 관련 Core 검사 PASS, 독립 코드 리뷰 대기. R06완료→R07일반→성장/정산→between8 구현이며 Unity/앱 완료는 아니다.

catalog의900/XP3000/cap100·180초/생성마감170초, G1 HP120/접촉9/속도.65·G2 180/10/.55·G3 240/11/.65를 소비한다. 기존 추적/무기/첫3진화 전투를 재사용한다. G3는 actor ID까지 기존 wave 생성표에서 같은 species의 생성순서를 세어 홀수왼쪽/짝수오른쪽에 배치한다. 기존 RNG 두 draw와 화면 밖 가로거리·세로분포를 유지하며 새 RNG/저장 counter 없이 죽은 actor를 포함한 순서가 복원된다. HP·피해·처치는 actor별 독립이다. R07 fingerprint에 속도/좌우순서/독립HP 계약을 포함했다.

기존 `CompletedBoss` R03 receipt는 유지하고 R06 현재 `Boss`를 진입 때 optional `CompletedSecondBoss`로 이동한다. 두 slot의 round/identity/정식선택·완료 receipt를 각각 검증하며 과거 라운드에서는 두 번째 history를 거절한다. schema2/World3를 그대로 사용하고 optional field 부재인 과거 R06 저장은 Load만으로 다시 쓰지 않는다. 모르는 필드·정책·미래 라운드는 원문/backup 보호 거절한다. PreviousNormalRound5로 R04/R05 완료 연쇄와 진화/성장/킬remainder/영구snapshot을 유지한다.

기존 일반 정산으로 드롭 XP→남은 성장→120gold·elijah_chariot 영구해금·최대HP 회복·between8을 한 번 적용한다. 미구현 불수레 장착/공격과 R08진입은 거절한다. D3 charge/boss 전투상태는 다음 일반 라운드에 유출하지 않는다.

검증은 `evidence/r07-core-01/summary.json`과 로그에 보존했다.

- `bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests -- --r07-only`:3/3 exit0. 두 보스 receipt/과거R06 무쓰기Load·진화/연쇄, 좌우배치·독립HP·pause/디스크 replay, 성장 gate·120gold/해금/회복 once·동일저장 retry·미래원문 보호.
- `--r07-related`:27/27 exit0. 신규3·기존R06 7·schema 관련17. 전행렬/기존18·10·30 변이 driver를 반복하지 않았다.
- 격리 사본에서 정확한 옛 일반 임의 가장자리 배치, 옛 단일 CompletedBoss 덮어쓰기 변이 각각 실제 R07 assert FAIL(exit1), 원복3/3 exit0. 새 driver/helper 프로젝트 없음.
- `--r07-series sling 7`:exit0. 실제 R03~R07 좌표전투, R07 180초/899킬/5220hits, G3중간Load·두보스receipt불변·between8·sling_fivefold 유지. R1/R2 timeout·장비8·HP10000 준비 fixture이며 R03 이후 피해/처치를 주입하지 않았다. 마지막 HP10000/골드1468은 기술 검증 결과이고 자연 밸런스가 아니다.
- `git diff --check` exit0. 기존 mutation anchor 18+4 고유성 정적 확인만 수행했다.

Unity 인계: 실제 `BattleWorld.EnemyPositions`/저장 `World.Enemies`의 actor별 좌표를 그대로 표현한다. G3는 각각 하나의 독립 actor이며 아트에서 공유HP를 만들지 않는다. 두 보스 history는 Run.CompletedBoss(R03), Run.CompletedSecondBoss(R06)이며 R07 Run.Boss는 null이다. 진행 안내는 Run.RoundNumber7 normal/round_settlement와 between8을 사용한다. Unity/DLL/provenance/art/CIhooks/GUI/실사용 저장/commitpush를 변경하지 않았다. 독립 리뷰·Unity 실행·모바일/성능은 NOT_RUN, 자연밸런스 UNVALIDATED다. phase r07-core-01의 자기 usage 시작/끝을 기록하며 청구비용과 절감률은 미측정이다.
