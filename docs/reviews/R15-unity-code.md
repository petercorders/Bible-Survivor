# R15 Unity 독립 코드 리뷰

- 검토일: 2026-11-18 KST
- 판정: **PASS — 지정된 Unity 소스·최종 인계 결합 범위에서 막는 HIGH/MEDIUM 발견 없음.**
- 소유 변경: 이 보고서만. 제품·타인 문서·사용자 저장을 수정하지 않았다.

## 범위와 판단

R15 계획·Core 독립 PASS, Unity 진행 문서와 `evidence/r15-unity-integration-20261118/handoff.md`, binding/verification-index/XML/trace를 읽었다. 현재 Host/catalog/UI/World diff와 신규 loader/importer/시험, 관련 기존 기대 변경을 검토했다. Core211/관련8·원 endpoint 충돌 변이·대표 좌표 CLI는 기존 독립 검토 결과를 재사용했다. 전체 suite·CLI·변이·Editor·앱·GUI·전체 해시 행렬을 다시 실행하지 않았다.

| 경계 | 판단 |
| --- | --- |
| 일반 진입·과거 의미 | AwaitingR15는 R14 between15→R15 일반 전환에만 연결된다. `ExodusComplete`는 Core의 명시 완료 상태를 공개하고, 설치 끝 ContentComplete와 창세기 ChapterComplete/CanEnterExodus 의미를 분리한다. R09→R10의 독립 후보·고정 operation/revision·Commit/Load/Resume/World 복원 후 게시하는 코드는 변경하지 않았다. 기존 일반 진입 실패 시 같은 arena/payload를 정지해 retry하는 경로를 재사용한다. |
| 완료 입력·명시 R01 | EnterCommand는 Error/suspend/같은 frame 중복을 막고 완료 R15에는 Resume/출애굽기 진입을 수행하지 않는다. 창세기 완료에서는 기존 명시 Exodus 진입만 수행한다. SelectSlot과 commandFrame을 공유해 같은 frame의 새 도전/Enter 중복도 소비한다. 완료 화면은 출애굽기 완료와 레위기 미구현을 표시하고 다음 책 버튼을 만들지 않는다. 명시 세 새 도전 선택만 R01/schema2로 전환하며 프로필을 보존한다. |
| 동적 catalog·저장 | R12/R15 보스 집합을 각각 canonical Rules로 검증하며 R15 피해18·projectile speed/radius/lifetime/cap/spread, 지원병을 확인한다. 기존 R09 피해15·R10~R14 fingerprint 경로를 유지한다. R15 normal/intro/boss/reward/settlement/between16을 수용하고 R15 round_settlement/R16 combat은 거절한다. 현재 Boss3/World4와 과거 R12 Boss4를 혼동하지 않고 현재 Boss+네 history/RunId/forms를 유지한다. |
| 보상·후보·retry | 고정500·선택/고갈100·성장25·완료280을 별도 처리한다. 동결 후보 무지급 Load, 중복 선택 거절, 보상/완료 저장 실패의 같은 payload 정지·retry와 재지급 방지를 기존 Host/Core 경로에 연결한다. 남은 성장 중 완료를 막고 완료에서만 회복/null 해금/between16을 적용한다. Host 신규 시험은 만석 자동100과 실제 카드 선택 분기를 구분한다. |
| 필수 예고·실탄 | UsesChainProjectiles에 따라 저장 ChainDirections와 실제 snapshot boss 좌표를 읽는다. 예고 길이/폭은 Rules의 ProjectileSpeed×Lifetime/Radius이며 실제 WorldProjection을 사용한다. 배우·무기·선택 입자 이후, HUD 이전에 예고/실탄을 그리며 FeedbackEnabled/lowFX로 숨기지 않는다. 실탄 위치는 실제 X/Y, 애니메이션 시간은 snapshot.Time−BornAt을 사용한다. PNG 크기로 Core 판정이나 독립 공격 시계를 추가하지 않는다. |
| 자세·first-shot·cleanup | windup3프레임은 저장 BossElapsed와 NextAttackAt/TelegraphDuration으로 선택한다. 공격2프레임은 emission>0인 실제 첫 발사 이후에만 재생하므로 시작 직후 가짜 공격 자세가 없다. pause/재로드 시 같은 Core 시계가 유지된다. 죽음4프레임/8fps는 별도 표시 수명 .5초다. stage가 boss를 벗어나면 경고/실탄을 즉시 표시하지 않고 World Advance의 기존 cleanup이 collection을 정리한다. World 교체/null 시 Clear가 반응·effects·boss/dash 상태를 지운다. |
| loader·그림자·Dispose | 승인8시트/27프레임과 index/manifest hash, 이름/rect/pivot/PPU32·Point/Clamp·readable Texture2D·무 mipmap 계약을 검증한다. importer는 고정 spriteID/name-fileID를 등록한다. ex16 small, ex17/ex18 wide, boss boss-shadow를 기존 발 기준 깊이 경로로 표시한다. 피격 마스크는 원본 alpha를 복사한 별도 Texture2D이며 원본을 바꾸지 않는다. 준비는 캐시를 재사용하고 멱등 Dispose가 runtime Destroy/editor DestroyImmediate로 파생 마스크를 해제한다. AdamArtWorld.Dispose와 AdamGame.OnDestroy 연결을 확인했다. |

## 기존 실행 결과와 실패 교정 구분

`verification-index.json`과 실제 XML의 관련 Edit는 **고유131건**이다. 최초 `related.xml`은130 PASS/1 FAIL(exit2), `corrected.xml`은 같은 보상 메서드의 False/True 두 케이스 PASS(exit0)다. 교정 실행을 새 고유2건으로 더하거나 단일131/131 XML로 표현하지 않는다. 신규 Host6/UI18/World5가 포함된다. 최초 compile exit1은 시험의 없는 Position 멤버 참조를 X/Y로 교정한 뒤 exit0이 되었다.

최초 보상 fixture의 실패는 예상3558/실제3658로, cloud4를 Session.Capture 전에 넣어 inventory 상태가 덮어쓰고 만석 자동100을 지급한 준비 오류다. 현재 준비는 Capture 뒤 commit 전에 cloud4를 적용한다. 두 보상 분기의 기존 금액·pending/serial·선택/완료 retry assertion을 유지하며 Runtime/Core 변경으로 회피하지 않았다. 이전 라운드 시험은 설치 끝16/표시문구를 갱신하고 R14의260 등 기존 경제 assertion을 유지한다. `play.xml` **14/14 PASS**는 실제 InputSystem Enter의 R15 진입·완료 Enter 무진입과 기존 입력/오류/suspend 경계를 포함한다. native OS 입력 시험으로 확대하지 않는다.

정상 mac 앱은 **총2개**, Editor build 호출은3개다. 첫 정상 앱은 direct Session.Hit 직후 World collection까지 즉시 empty여야 한다는 QA 기대에서 exit2였다. 제품은 stage 변경 즉시 경고/실탄을 숨기고 다음 World Advance에서 collection을 정리한다. 기존 World 시험과 같은 `Telegraphing false → World.Advance(0) → 빈 actor/적탄`으로 QA driver만 교정했다. 중간 prebuild는 재사용 QA 씬의 capture component 때문에 Fresh QA scene gate exit1이었고 BuildPipeline 전이라 앱을 만들지 않았다. 최종은 원 제품 씬 재복사 후 새 identity/절대 출력에 정상 빌드한 **review-build/R15Qa.app PASS**다. 첫 앱·driver·FAIL 화면·rawlog는 initial-failure에 보존되며 성공3장과 구분한다. 제품/binary 패치로 처리하지 않았다.

최종 bundle은 `com.biblesurvivor.adam.r02artqa.r1580111c7154f5`다. binding/fixed-product-source는108입력을 기록한다. 리뷰에서는 변경 Runtime8개·신규 importer·Core DLL/provenance/catalog의 필요한12개 입력만 현재/QA/기록 동일을 확인했다. 앱 Core DLL은 `e40985359fe9d367c14f8e19e990e20613e80e6e21b6815ed1a4743ca791eeaf`, 앱 Assembly-CSharp는 최종 binding과 같고 catalog는 canonical과 일치한다. provenance schema2는 root save3과 별개다. 기존 registration의27개 before/after가 동일하다. 전체 소스·아트 해시 행렬은 반복하지 않았다.

## 최종 trace와 검증 한계

`capture/continuous-trace.txt`, `reload-pass.txt` 및 필요한 QA assertion을 대조했다. R14 완료 무쓰기 Load와 명시 R15 진입에서260 재지급 없음, BossElapsed4.2의 실제3방향/lowFX·World4 disk Load,5.2의 실제3발·BornAt/다음 tick 이동, 직접 threshold Hit 뒤 지원6/12·mask1/3의 exact Load를 기록한다. final probe/기록/새 process는 각각 exit0이다.

금액은4488→kill500=4988, 실제 동결 후보2개(cloud4→5/골드100) 무지급 Load→선택100=5088, 기술 pending2의 실제 Choose2(cloud5 한 번/통화25 한 번)=5113, 별도 완료280=5393이다. 후보2개와 pending2·Choose2·currencyChoose1을 구분한다. 만석 자동100은 Host 시험의 별도 분기다. 두 저장 실패에서 frozen payload/retry와 무중복을 확인하며 완료 후 최대HP10000/null 해금/currentBoss3+네 history를 보존한다. 새 process exact Run/Profile Load와 Enter/Resume 무변경, 명시 R01/schema2·프로필5393 보존을 확인한다.

이 trace는 HP10000/무기8/3진화/6슬롯의 상속 기술 seed에서 cloud5→4를 준비했다. 자연 장비 유지나 새 진화 획득 검증이 아니다. timeout·요청 Damage20(방어 후9985)·bossX9/cycle1000·직접 threshold/lethal Hit·pending2도 기술 준비다. Core CLI1097/1200킬·관측 최대 적탄6을 전처치나 cap12 실측으로 확대하지 않는다. 자연 밸런스·앱 R01~R15 자연 연속 완주·native/device/mobile·성능은 **NOT_RUN/UNVALIDATED**다. 성공 대표3장과 화면 가독성은 별도 최종 UI 리뷰 범위이며 코드 판정으로 대신하지 않는다.

자기 저장 복구/프로세스 종료는 인계 restoration 근거를 재사용하고 보호 identity나 저장을 조회·조작하지 않았다. 자기 사용량은 기존 `record_model_usage.py`의 `r15-unity-code-review` start/end로 기록한다. 지연된 이전 tail을 포함할 수 있는 자기 세션 구간이며 청구액·단가·비용 절감률은 계산하지 않는다. 다음 책·무기·모바일 작업은 시작하지 않았다.
