# R14 Unity 독립 코드 리뷰

- 검토일: 2026-11-18 KST
- 판정: **PASS — 지정된 Unity 변경과 최종 인계 결합 범위에서 막는 HIGH/MEDIUM 발견 없음.**
- 소유 변경: 이 보고서만. 제품·타인 파일·사용자 저장은 수정하지 않았다.

## 범위와 판단

AGENTS, R14 계획·Unity 진행 문서, `evidence/r14-unity-integration-20261118/handoff.md`, binding/verification-index, 관련 XML과 trace를 읽었다. 현재 Unity Host/catalog/UI/World diff, 신규 loader/importer/시험 및 직접 영향을 받는 기존 시험 변경을 검토했다. Core 203·관련 6·원 R07 전용 조건 좌표 변이·CLI와 `R14-core-code.md`의 독립 PASS는 재사용했다. 새 시험·CLI·Editor·앱·GUI·전체 해시 행렬을 실행하지 않았다.

| 검토 경계 | 판단 |
| --- | --- |
| R13→R14 진입과 retry | Host 제품 변경은 `AwaitingR14`와 대기 목록 추가다. 완료 R13을 읽은 뒤 입력 없이 쓰거나 자동 진입하지 않으며, 명시 Continue는 기존 일반 진입 경로에서 새 arena를 한 번 게시한다. 저장 실패 시 Error/Paused로 tick·선택을 막고 같은 arena/payload/revision/operation을 재시도한다. 시험과 최종 trace가 이전 durable 경계 유지 및 성공 후 gold 불변을 확인하여 R13 완료 240 재지급을 방지한다. |
| 네 receipt·World·과거 경계 | 진입/재개는 같은 RunId·프로필·장비·forms·네 과거 receipt를 유지한다. R14는 현재 Boss 없는 normal World 3, clock 0, LastHitTime -1, 빈 charge/hostile 상태다. Catalog는 Core의 연속 설치 정의와 속도를 검증하고 R14 settlement/between15만 추가 수용한다. R09→R10의 독립 candidate·고정 operation/revision·Commit/Load/Resume/World 복원 후 게시 코드는 변경하지 않았다. |
| 성장·완료·지원 끝 | full serial 성장 선택당 25와 완료 260은 별도다. 첫/마지막 성장 저장 실패 시험은 같은 payload 정지·retry·stale choice 거절·총 선택 수와 최종 금액을 확인한다. 완료 후 회복/null 해금/between15/ContentComplete를 유지하며 중복 완료·자동 R15 진입을 거절한다. 새 process Load도 동일하다. 명시 새 도전과 사망 후 새 도전은 프로필을 유지한 R01/schema2로 전환한다. |
| Core 좌표 표시·독립 HP | Unity는 Core가 제공한 EnemyPositions를 기존 projection으로 표시하며 좌우 생성·추가 피해·연동 HP를 구현하지 않는다. Host 시험은 실제 Core 초기 3초의 좌우 존재, Hit7 후 첫 actor213/이웃220 및 첫 actor kill 후 이웃220을 확인한다. strict 종별 홀짝·화면 밖 생성 좌표는 기존 Core 시험과 실제 원조건 변이를 재사용한다. 최종 앱의 왼쪽4/오른쪽4는 중앙 수렴 전 초기 표본이다. |
| UI·그림자·시간 | R14 아론/출애굽기5와 완료260·R15 브살렐 대기를 표시하고 출애굽기 전체 완료를 주장하지 않는다. R12 두 줄 HUD/forms footer와 입력 흐름은 유지한다. ex13 small, ex14/ex15 wide 그림자는 기존 shadow-before-actor/발 기준 깊이 정렬을 사용한다. 3개 시트 각4프레임, 32×32·pivot16/1·fps8/6/8·loop를 로더/시험에서 확인한다. 애니메이션은 World presentation clock을 사용하며 lowFX가 Core 좌표나 시간을 변경하지 않는다. |
| loader/importer·리소스 해제 | index/manifest 고정 계약과 frame 수·이름·rect·pivot·PPU32·Point/Clamp·mipmap 없음·readable Texture2D를 검증한다. importer는 PNG 계약을 확인하고 stable spriteID/name-fileID를 등록한다. 피격 마스크는 원본 alpha를 복사한 별도 Texture2D이며 원본 픽셀은 변경하지 않는다. 준비는 중복 생성하지 않고 Dispose는 멱등이며 runtime Destroy/editor DestroyImmediate를 사용한다. `AdamArtWorld.Dispose`→신규 loader 해제와 `AdamGame.OnDestroy` 연결을 확인했다. World 교체/사망의 기존 Clear는 반응·effect·boss/dash 표시 상태를 정리하고 loader 캐시는 view 수명 동안 재사용한다. |

## 재사용한 실행·결합 근거

`verification-index.json`과 실제 XML을 대조했다. **Edit 고유 101건**은 최초 `related.xml` 100 PASS/1 FAIL(exit2)과 실패한 동일 1건의 `split-corrected.xml` PASS(exit0)를 합친 결과다. 102개 고유 시험이나 단일 101/101 실행으로 세지 않는다. 최초 실패는 직접 `Session.Kill`이 만든 기술 드롭에 World drop geometry가 없는 상태에서 pause checkpoint가 실패해 이전 World와 비교한 경우다. 현재 fixture는 그 기술 드롭을 폐기하고 checkpoint 성공을 명시적으로 확인하며, 좌우·독립 HP·정확 disk Load assertion을 유지한다. 실패 XML을 보존했고 제품 변경으로 회피하지 않았다. 신규 Host9/UI8/World3이 포함되며, 관련 외 과거 기대 변경은 설치 끝 경계 갱신을 컴파일한 범위다.

`play.xml` **14/14 PASS(exit0)**는 IMGUI Continue 클릭과 InputSystem Enter, 오류·suspend·같은 frame 명령·기존 창세기 진입 경계를 포함한다. R13 관련 시험은 기존 240/성장25 경제 assertion을 유지하고 새 설치 끝에 맞춰 AwaitingR14로 갱신했다. native OS 입력 시험으로 확대하지 않는다.

최종 앱은 `build/R14Qa.app`, bundle `com.biblesurvivor.adam.r02artqa.r1446fdab19d0bf`이며 **정상 mac build 1회**다. binding에는 고정 103개 입력과 QA 사본 일치를 기록했다. 리뷰에서는 변경 Runtime 6개·신규 importer·Core DLL/provenance/catalog의 필요한 10개 입력만 현재/QA/binding 해시가 같은지 확인했다. 앱 Core DLL도 binding의 `a7bf1a4cb4229a54f30cf219a23051212c6361d63405768919aa0d9bfc40530c`와 같고 Unity catalog는 canonical catalog와 같다. provenance schema2는 root save schema3과 별개다. 전체 소스 해시 행렬은 반복하지 않았다. 기존 registration의 12개 before/after name/GUID/localID/rect/pivot가 동일하며 build preflight는 완료 R13→R14/네 receipt/무추가지급/World3와 실제 Core 3초 좌우 가시성을 PASS로 기록한다.

`capture/continuous-trace.txt`, `reload-pass.txt`와 capture fixture의 금액·선택 수 assertion을 대조했다. R13 gold4020 진입 전후 불변, normal 성장 후보1/pending1/Choose1의25, 완료 성장1/1/1의25와 별도260, 완료 후 gold4488의 새 process exact Run/Profile Load 및 명시 R01을 기록한다. 중간 gold4161/4193에는 기술 준비 처치 금액이 섞이므로 총 차액 전체를 성장·완료 보상으로 계산하지 않는다. build/probe/capture/reload exit0는 기존 명령 근거를 재사용했다.

## 한계와 사용량

기술 fixture는 상속 HP10000/무기8/진화3/6슬롯, cycle1000 지연, timeout/direct kill/drop 폐기를 사용했다. 초기 실제 Core3초 좌우와 기술 준비 후 실제3초 혼합 추적은 서로 다른 관찰이다. Core CLI 973/1140킬은 전처치·자연180초 밸런스·앱 R01~R14 연속 완주 근거가 아니다. 최종 대표 화면3장은 인계에 결합되어 있지만 이 코드 리뷰는 별도 화면 가독성 리뷰를 대신하지 않는다.

자연 플레이·native/device/mobile·성능은 **NOT_RUN**이다. 저장 보존/자기 프로세스 종료는 기존 restoration 근거를 재사용했으며 리뷰가 앱·저장을 다시 조작하지 않았다. 기존 도구의 `r14-unity-code-review` start/end를 기록한다. 공급자 자료는 지연된 이전 tail을 포함할 수 있는 자기 세션 구간이며, 작업 독립 청구액·비용 절감률은 계산하지 않는다.
