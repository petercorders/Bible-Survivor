# R10 Unity 독립 코드 리뷰

2026-11-17 KST. **PASS — 아래 소스 범위에서 열린 HIGH/MEDIUM 없음.** 승인된 `docs/harness/r10-implementation-01.md`, `docs/progress/R10-core.md`, 독립 Core 리뷰와 Unity 실제 diff·새 R10 시험을 대조했다. 소스 결론과 최종 앱 화면 수용은 분리한다.

## Host·저장 게시 경계

- `AdamHost.cs:37`, `:65`는 설치·오류·중단·진입 진행 중 상태를 검사한다. `:70`부터 `PrepareExodus` 복사본으로 후보 session/새 World를 준비·검증하며, 기존 R09 State/Session은 유지한다. 후보를 보관한 뒤 operation을 한 번 생성하고 정지한다. 진입 지급/회복이나 새 Run 생성 경로가 없다.
- `AdamHost.cs:180`은 동일 후보·이전 revision·고정 operation으로 Commit하고 Load의 revision/operation을 확인한 뒤 Resume/World 복원을 수행한다. **게시 지점은 `:190` 하나**다. 예외 시 기존 메모리·후보·operation/revision을 유지하고 Error/Paused/이동0으로 멈춘다. `Start`, `Checkpoint`, `Tick`, 재진입은 실패 중 후보를 변경하지 않는다. ack 유실 재시도는 기존 SaveStore의 operation payload 검사와 revision 충돌 검사를 사용한다.
- `AdamHost.cs:42`의 명시 Reload는 읽기·Resume·World 구성이 모두 끝난 뒤 게시/후보 해제를 한다. commit 전 실패는 R09 완료로 돌아가고, commit 후 ack 유실은 저장된 R10으로 조정된다. 재시도 성공은 Paused 상태이며, 직접 진입 성공만 중단 상태를 확인하고 재개한다. lifecycle 중단 후에는 기존 명시 재개 정책을 유지한다.
- `AwaitingNextRound`는 기존 R02~R09에 한정한다. R09 between10은 Load/Tick만으로 R10에 진입하지 않고, R10 between11은 다음 전투를 열지 않는다. 완료·사망 후 명시 새 도전은 기존 R01/root2 경로로 간다. 세 receipt·RunId·profile/gold·장비/진화·성장 보존과 새 World/피격시계 초기화는 Core 계약과 Host 시험의 실제 비교를 확인했다. Core 내부 저장 검증 결론은 별도 `R10-core-code.md`를 재사용한다.

## Catalog·입력·월드

- `AdamCatalog.cs:33`, `:71`은 R10 normal/round_settlement/between11만 새 정의로 연결한다. 창세기 parser/속도 사전은 유지하고 출애굽기의 3종·speed/chase·global10/local1/stable ID·무보스/무해금·예산/HP/피해/웨이브를 별도 parser와 `ExodusContent.Validate`로 확인한다. Host Load/Restore/진입 모두 `SpeedsFor`를 사용한다. Exodus 미설치 시 기존 R09 완료 Load는 가능하며 진입은 비활성이다.
- `AdamGame.cs:105`, `:155`는 키보드/버튼이 같은 Enter handler를 사용하고 새 도전 선택과 `commandFrame`을 공유한다. 숫자/Enter 동시 입력은 한 명령만 소비한다. Error/Suspended/사망 표시 중 진입을 차단하며 진입 후 이동·touch 해제를 요구한다. World 교체도 `:144`에서 입력을 해제한다. `:275`는 창세기 완료와 설치 R10 끝을 구분하고, Enter 재개 분기는 완료 화면에서 R11을 시작하지 않는다.
- `AdamExodusR01Art.cs:15`와 importer는 고정 index/manifest/PNG hash 및 3개 32px×4frame/PPU32/pivot16,1 계약을 적용한다. Sprite의 texture를 명시적 `Texture2D`로 읽고 별도 runtime hit mask를 만든다. 원본 RGB/alpha를 바꾸지 않고 Point/Clamp/no-mipmap을 유지하며, Dispose는 파생 mask만 해제한다. `AdamArtWorld.cs:181`에 소유자의 Dispose가 연결된다.
- `AdamArtWorld.cs:263`의 ex1/ex2/ex3 분기는 기존 rounded feet y 정렬·동일 y의 stable ID 정렬·shared shadow·부모 좌우 flip·피격 reaction transform 경로 안에 있다. LowFX는 actor/깊이를 제거하지 않고 반응과 flash만 끈다. 기존 R01~R09 분기와 좌표는 그대로이며 새 아트 분기는 Core RNG/전투 판정을 호출하지 않는다.

## 재사용한 실행 근거

모든 경로는 `evidence/r10-unity-integration-20261117/` 아래다. 리뷰어는 Unity/Editor/build/Player/GUI·suite·변이를 실행하지 않았다.

- `sync-command.json` exit0, Core DLL SHA256 `517ae61998456273aaf510d5201d5cc03777880059e5bdcf570d24ee7561adfe`. `compile-command.json`은 실제 `AdamExodusR01ArtImporter.Reimport/ValidateBuild` exit0이다.
- `related.xml`은 47개 중46 PASS/1 FAIL(exit2)이었고 실패는 기존 R09 경계 시험의 normal10 거절 기대였다. 제품 계약에 맞춰 normal11 거절로 교정한 **그 시험 하나**가 `related-corrected.xml`에서 PASS/exit0이다. 합계 **47개 distinct PASS**, skipped/inconclusive0이며 두 번째 전체 suite 실행으로 보고하지 않는다. R10 Host10/UI5/World3개와 관련 기존 회귀를 실제 XML·시험 코드에 대조했다.
- `play.xml` **22/22 PASS, exit0, skipped/inconclusive0**. R10 실제 InputSystem Enter의 중단 중 차단→1회 진입→설치 끝 Enter 무진입, 숫자+Enter 동일 프레임의 명시 새 도전 우선 시험을 포함한다.
- `entry-mutation.json`, `old-entry-prepublish.xml`, `restored-entry-postcommit.xml`: 기존 선게시 구조를 재현한 변이에서 진입 실패 두 경우가 **old State same-instance assertion으로2/2 FAIL(exit2)**, 복원은 **2/2 PASS(exit0)**다. 컴파일 실패를 검출로 세지 않았다. 기준 PASS는 앞선 관련 XML에 있다.
- `fixed-product-source.json`의 리뷰 대상 Host/UI/World/Catalog/새 시험·importer·DLL·catalog 파일 hash와 현재 제품 파일을 읽기 대조했고 불일치0이다. `verification-index.json`의 project/portable/diff 검사는 통합자 exit0 기록을 재사용했다.

## CI 도구 최소 보완 독립 검토

Root가 후처리한 `tools/check_mutations.py`, `tools/check_schema_mutations.py`, `tools/check_schema_compat.py`의 diff만 추가 검토했다. **정적 PASS, 변이·호환 비교 의미의 약화 없음.** `completion_resume_refused`는 지원 상한 anchor9→10만 따라가며 `run.Stage != "normal"` 거절을 주입하는 원래 결함을 유지한다. schema 변이의 `GenesisVersion`/`>=` anchor는 현재 schema2 지원 검사 위치를 가리키며 Unsupported를 corruption으로 오분류하여 backup을 복구하는 결함과 중첩필드 guard 제거 결함을 그대로 주입한다. schema3도 해당 제거의 영향을 받지만 기존 schema2 표적 시험을 없애거나 예외를 통과로 바꾸지 않는다. compat는 old Core와 같은 pinned commit의 CatalogReader만 old probe에 쓰고, new probe는 현재 adapter를 유지한다. 두 probe의 같은 Genesis 입력·30 fixture·schema 번호 외 상태/fingerprint 동등 비교·30 migration·30 old-reader 거절 조건은 그대로다. 구현 귀속은 root이며 제품 Core/DLL/app 변경은 없다.

리뷰어는 추가 실행하지 않았다. Root의 `.local/r10-schema-compat.log`는 `evidence/r03-schema-compat-20261117T082003461715Z/summary.json` PASS를 반환했고, mutations/schema-mutations는 리뷰 시 실행 중이다. 실행 완료와 실제 exit·변이 검출은 root 인계로 별도 확정한다.

## 실행 한계·인계

최종 근거까지 **소스·관련 실행 근거 범위 PASS**다. `handoff.md`와 `final-clean/binding.json`을 좁게 대조했고, 최종 앱은 `final-clean/build/R10Qa.app`, 실제 identity는 run/path-probe와 일치하는 `com.biblesurvivor.adam.r02artqa.r1072f41b4f4136`이다. binding은 제품82파일 고정·앱 Core DLL의 위 SHA 일치를 기록한다. 정상 mac 빌드는 총3회이며 앞선 `build/`·`final-review/` 두 앱은 콘솔 가림 화면 FAIL 근거로 보존하고 최종3화면과 섞지 않는다. 최종 build exit0, probe/capture/reload의 실제 PID64252/64253/64265 모두 exit0, 통합자 runner exit0 인계를 확인했다. 최종 trace는 실패 후 같은 재시도·게시, 세 receipt/World 초기화·EX3종 실제3초 추적/lowFX·중간 Load, gold25 후보1/pending1/Choose1, 완료180/pending0/Choose0·between11·새 프로세스 정확 Load·Enter 무진입·명시 R01 새 도전을 기록한다. 최종 own3 저장 복구는 `final-clean/restoration.json` success이며 보호 identity 불조회 기록을 유지한다. 기존 Art03 meta79 자동삽입 복구 기록과 현재 Art03/ProjectSettings diff 없음도 확인했다. 기존 소스/시험/전체 hash 행렬은 재검사하지 않았고 화면 가독성 수용은 독립 화면 리뷰의 범위다.

시험/앱은 기술 HP·성장 fixture, 직접 boss hit/처치·시간 전진을 사용한다. 자연 플레이 완주·난이도/경제 밸런스·모바일/실기기·성능·보호 사용자 앱/저장 검증을 주장하지 않는다. readback 누락/변경 분기의 개별 fault injection은 위 XML에 없으므로 정적 검토 근거만 있으며, 추가 suite/변이 행렬을 요구할 새 결함은 찾지 못했다.

제품 파일은 읽기만 했고 리뷰 문서만 작성했다. 사용량은 `r10-unity-code-review` start/end로 자기 세션을 기록하며 청구액·비용 절감률은 미계산이다. 기존 graphify 산출물이 없어 읽기 전용 source/diff 검토를 사용했다. 별도 worker·commit/push는 수행하지 않았다.
