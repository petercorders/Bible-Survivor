# R06 이삭 순수 Core checkpoint

2026-11-16 KST. 기준 e521382, 실행 계약 `docs/harness/r06-implementation-01.md`. **관련 Core 검증 PASS·독립 코드 리뷰 대기**다. R05→R06 일반→보스→보상/진화·성장→between7를 구현했으며 Unity/앱 완료는 아니다.

catalog의780/XP2400/cap90/180초·170초 생성마감과 F1/F2/F3를 사용한다. 일반 종료는 드롭만 회수하고 회복·완료 지급 없이2초 보호 intro로 간다. R03와 같은 전투 경로를 라운드 정의로 재사용해 싯나 HP6500/접촉16/처치200, 주기7초·5.8초 예고 시작·고정 위치 반경1.20·피해18의 단발 지점공격을 적용한다. 예고1.2초 동안 추적을 멈추며 투사체/잔류 피해 엔진은 추가하지 않았다. 흙 낙하는 표현이며 기계적 impact Cause는 기존 `slam`을 재사용한다. 보스 timeout은 없고 공유 무적/regen/치명타 우선순위를 유지한다.

생존 HP70%/35% 각각 F1×6을 한 번 소환한다. `SummonMask`로 두 문턱을 저장하며 한 hit가 두 문턱을 넘으면12개를 생성하고 즉사는 소환하지 않는다. 지원 처치는 일반5킬 remainder, XP0 기술 정책이며 사후 제거는 무보상이다. R06 보스 ID781/지원782부터, 원본 R03 ID481/482와 encounter ID·정책·fingerprint는 유지한다. 공통 초기 tuning(.30/.60/intro(0,+3)/공격반경1.20/지원반경1.5)은 기술값이다. 정의의 실제 stats/지원 종류·수량/문턱/정책·진화 계약을 R06 fingerprint에 결합한다.

R03 `CompletedBoss`는 기존 slot에 원문 의미를 보존하고 R06은 현재 `Boss`와 병존한다. 과거 slot은 완료한 R03만, 현재 R06은 round6/sitnah identity만 허용해 중복·역순·덮어쓰기를 거절한다. 기존 Boss version2에 default를 쓰지 않는 round6/mask 필드만 추가했다. R03의 해당 필드는0/미작성이다. schema2/World3를 재사용하고 모르는 encounter·정책·round/field/version은 typed body 읽기 전에 원문 보존 거절한다.

정식 정책 `r06-production-v1`은 기존 최대3고유·첫3진화 자격 보장·골드100 fallback과 안정 후보/선택 receipt를 재사용한다. 보상→남은 성장→완료100gold/cross 해금/최대HP 회복을 한 번 적용해 between7로 저장한다. 이미 진화한 무기와 미구현 십자가는 획득/공격 후보가 아니다. R07은 거절한다.

검증:

- `--r06-only` **7/7 exit0**: 일반/intro 무조기 지급·R03 receipt,7초 고정 공격/정지,70/35·12상한·즉사 우선, threshold/예고 replay·timeout 없음,200/완료100·진화/성장·중복/retry·두 receipt,regen/치명 우선, 미래 원문 보호.
- 관련 기존 `--boss-only`12/12·`--reward-only`11/11 exit0. 기존 전행렬·18/10/30·전무기/seed를 반복하지 않았다.
- 격리 사본의 최소3변이: 옛 mask 없는 저장 shape, 단일 완료 receipt로 R03 덮기, 옛 다음 tick 지원 좌표 동기화. 모두 실제 시험 FAIL(exit1)→복원7/7(exit0). 새 driver/helper 프로젝트는 없다. `evidence/r06-core-01/summary.json`, 개별 로그와 `restored.log`에 명령/exit·source hashes가 있다.
- 대표 `--r06-series sling 7` exit0: 실제 R03~R06 좌표 전투, R06 일반780+지원11킬·4080hits·보스킬1, 두 문턱12소환(mask3), threshold·같은 보상 Load, R03 receipt 불변, between7. 보스21.4초·골드1168·HP10000은 장비8/HP10000을 준비한 기술 fixture 결과다. 피해/보스킬을 주입하지 않았고 밸런스 수용이 아니다. `cli-result.json`/`boss-series.log`에 실제 출력을 보존했다.
- `git diff --check`/변경 Python compile exit0. 기존18+보스4 mutation anchor 고유성만 정적으로 확인했다.

실제 CLI가 지원 소환 hit와 같은 tick에 저장할 때, 새 Support가 Targets에는 있으나 좌표 추가는 다음 tick까지 늦는 기존 보스 경계를 발견했다. tick 끝에 살아 있는 targets를 동기화하는 최소 수정으로 고쳤고 실제 무기 hit 직후7개 actor/Validate 시험과 정확한 옛 구조 변이로 고정했다. 최초 nullable 시험 컴파일 경고도 수정했다. 이 실패 수정 때문에 관련검사/대표CLI만 재실행했으며 전체 검사는 늘리지 않았다.

Unity 인계: `Session.BossRules`/`Boss.Rules`의 Number/Id/MaxHealth/Contact/KillGold/AttackPeriod/AttackDamage/BossActorId/SupportMonster/Thresholds를 사용한다. R06에 R03 static HP/ID/주기 상수를 쓰지 않는다. 고정 ring은 `Boss.TargetX/Y`, `Telegraphing`, `NextAttackAt`, `BossElapsed`와 공통 `BossDefinition.SlamRadius`를 따른다. 이 작성자는 Unity/DLL/provenance·아트·CI/hooks·GUI·실사용 저장·commit/push를 변경하지 않았다. 독립 코드 리뷰·Unity/실플레이/모바일·성능은 대기/NOT_RUN이다. Sol 모델의 phase `r06-core-01` 자기 usage 시작/끝을 기록하며 청구 비용/절감률은 미측정이다.
