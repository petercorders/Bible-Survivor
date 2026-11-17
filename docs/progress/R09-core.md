# R09 유다 순수 Core checkpoint

2026-11-17 KST. 기준9272a13, 실행 계약 `docs/harness/r09-implementation-01.md`. **관련 Core 검증 PASS·독립 코드 리뷰 대기**. R08완료→R09일반/보스→보상·성장→창세기완료 between10 저장을 구현했다. Unity/앱·자연난이도 완료가 아니다. Claude 호출과 아트 편집은 없다.

catalog의1140/XP4200/cap120, wave300/380/460·180초/170초 생성마감, I1 160/10/.85·I2 260/11/.60·I3 600/14/.35를 기존 추적·접촉으로 사용한다. 일반 종료는 드롭 XP만 회수하고 무조기회복·2초 보호intro를 거쳐 은잔의 참소자16000HP/접촉20으로 이어진다. actor ID1141, I2 지원1142부터이며60%/30%에각6개를한번생성한다. 같은tick지원동기화·12상한·즉사시소환없음·공유무적/regen/플레이어치명우선을 유지했다.

보스 주기5초,4초부터1초 정지예고다. 예고시 플레이어 방향을 고정하고 겹치면(1,0),기본방향±20도를 저장한다.5초 시점 현재boss위치에서3발을 생성하고 다음tick부터 각1.35worldunit/s·반경.16·TTL6초로 직선이동한다. 기존 적탄부터 swept circle충돌을 처리하며 플레이어 이동도 상대선분에 포함한다. 접촉1회 소멸이며 무적으로피해가막혀도소멸한다. 사망시반격/무기/보스처치보다먼저종료하고보스처치즉시남은적탄은무보상정리한다. 새탄은tick말보스가살아있을때만생성한다. pause/성장선택/intro/reward에서적탄clock은정지한다. arena clamp/속박/탄막확대는없다.

R09에만 BossVersion3의 optional ChainDirections, World4의 optional HostileProjectiles/NextHostileId, HostileVersion1을 사용한다. 명시 시작좌표/고정방향/출생시각/현재좌표/남은수명/ID를 저장하며 목록상한12·고유ID/단위방향·일정속도와수명의clock·발사주기·lifecycle을검증한다. 기존 두 보스/World1~3는 새필드를작성하지않고 기존fingerprint를유지한다. R09 fingerprint에는 실제Chain상수·예고·지원/문턱/정책·무기진화계약을결합했다. schema2를유지하며미래World5/Boss4/Hostile2·unknown필드는typedbody전에원문/backup보호거절한다. R09공격이시작된activeboss의저장에는Worldgeometry가필요하다.

고정350gold·선택fallback100·일반성장고갈25·완료160은각각기존receipt/선택serial/완료flag로once다. 최대3고유/가능진화보장·기존후보/첫3진화·성장/XP/remainder/구매snapshot을유지한다. CompletedBoss(R03)/CompletedSecondBoss(R06)와현재Boss(R09)를함께검증하며덮어쓰지않는다. 성장완료후160gold/해금null/최대HP회복→between10으로저장한다. `AdamSession.ChapterComplete`는이완료receipt상태에서true다. Resume는동일상태,StartNextRound10은거절하며자동출애굽기/장비초기화/새도전은없다.

근거는 `evidence/r09-core-01/summary.json`과로그에있다.

- `--r09-only`6/6 exit0:catalog/intro/두history,세방향고정·정지·다음tick발사/저속/TTL/pause,실제적탄damage·무적소멸/동tick치명/kill정리,60/30소환/저장,예고·live탄diskreplay/상한·미래원문,후보/350·160once/완료Load·세receipt.
- `--r09-related`20/20 exit0(신규6+R06 7+R08 5+schema2). 알려진round9/World4를지원하게되어기존future시험경계를10/5등으로이동했다. R08접촉fixture의임의보상seal등급의존기대값은실제armor기준으로교정했다. 마지막R09damage시험도같은원칙으로고정해6/6을확인했다.
- 격리사본의옛point-slam분기/옛hostile없는Restore구조는각각실제R09 assert FAIL(exit1)→복원6/6 exit0. 기존18+4+10+3 mutation anchor 고유성만정적으로확인했다. driver/전행렬/전무기·seed반복/새도구프로젝트는없다. `git diff --check`exit0.
- **한번의 `--r09-series sling 7` exit0**:R01이전에장비8/대응보조1/HP10000을준비한기술fixture로R01~R09실제좌표공격을연속실행했다. kill/피해주입없이R01 240킬/R02 360킬,R03~R09보스전·선택·각중간Load를거쳐완료했다. R09일반+지원1142킬/8607hits,보스kill1/40.8167초,mask3/12소환,사슬24발,예고/live탄/보상Load·두history유지true,ChapterComplete/between10저장. 실제gold100자동선택receipt도완료저장에있다. 최종level55/XP682/remainder4/골드3119/HP10000은기술결과이며자연완주/경제밸런스PASS가아니다. R04고기술장비로dash전처치되어CLI의observed_charge=false이며이번CLI를그전조상태검증으로전용하지않는다.

Unity읽기계약:BossRules.Number9/Id/speed등과`Boss.ChainDirections`를사용한다. 방향순서는-20도/기본/+20도이며현재`EnemyPositions[1141]`에서속도×수명8.1worldunit구간으로투영한다. `Boss.Telegraphing`때만선표시,예고선자체는피해가없다. 발사뒤에는읽기용방어복사목록`BattleWorld.HostileProjectiles`의각Id/X/Y/DirectionX/Y/Lifetime/BornAt/StartX/Y를그대로표시한다. 반경/속도/수명은BossDefinition.ChainRadius/ChainSpeed/ChainLifetime을읽는다. NextAttackAt/BossElapsed는보스clock,Hostile.BornAt/World.Time은`NormalEndedAt+IntroElapsed+BossElapsed`절대combatclock이다. 벽시각을쓰지않는다. 피해cause는`chain`,기존slam표현으로대체하지않는다. 완료안내는ChapterComplete/between10·다음책미구현대기를사용한다.

Unity/DLL/provenance/art/CIhooks/GUI/실사용저장/commitpush 미실행. 독립코드리뷰·Unity/실플레이·모바일/성능 NOT_RUN. phase r09-core-01 자기usage시작/끝을기록하며Sol관측값만보고하고청구비용/절감률은미측정이다.
