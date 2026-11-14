# 아담 기본 ParticleSystem 제품 연결

**현재 인계: CPT-01 최소 수정본.** 최종 근거는 `evidence/combat-particles-cpt01-20261114/final-binding.json`이며 앞선 P1 앱/근거와 구분한다. 동일 독립 리뷰어의 CPT-01 재검토와 PC UI 리뷰는 root 대기다.

2026-11-14. P0 `96bab9217cd98a884b5cab1fef06eaeb696bddb8` 이후 승인된 Unity 표현 단계다. 단일 작성자로 제품 Runtime·관련 시험·QA fixture만 변경하며 Core/원본03·폰트/기존 CI·검사기·hook은 보존한다. 사용자 iOS 앱·기기·저장은 조회하거나 조작하지 않는다. 이 문서는 구현자 검증이며 독립 PC 리뷰/GC/commit/push는 root 소유다.

`AdamWorldProjection`은 실제 `AdamGame.Arena(20,116,width−40,height−194)`와 스프라이트 Point 계산/PixelScale을 공유한다. 홀수 크기는 RT를 정수 픽셀로 올림하고 정수 배율 그대로 그린 뒤 arena의 끝에서 잘라낸다. camera 중심은 반올림된 스프라이트 중심과 같은 격자로 보정한다. RT는 Point/AA1/Clamp, camera는 명시 aspect·orthographic/HDR·MSAA 끔이다. 실제 Update에서 준비하고 OnGUI는 합성만 하며, Update 후 viewport가 바뀌면 이전 투영의 한 프레임을 늘려 그리지 않고 생략한다.

`AdamImpactParticles`는 Unity 기본 ParticleSystem 16개를 prewarm해 사용한다. 각 시스템 상한16/전체256이며 원본 제작 사각 파편 프리셋은 성경의 빛 조각6개, 턱뼈 먼지8개, 물맷돌 조각4개, 처치 금빛12개다. 짧은 수명·속도/색만 표현에 적용한다. 처치 burst는 포화 시 낮은 우선순위 burst를 교체하고 일반 burst는 빈 슬롯이 없으면 생략한다. 실제 프리셋의 한 번 포화 입자 수는256보다 작으며256개를 항상 방출한다고 주장하지 않는다.

Core 사건을 다시 읽지 않고 기존 `AdamArtWorld.FrameImpacts`에서 새 순번만 처리한다. 사망/World 변경은 clear, pause/성장/앱 중단은 수명·위치 동결 및 늦은 batch 폐기, 저효과/끔은 clear해 재개 때 과거 효과를 재생하지 않는다. 기존 P0 피격/사망/오디오·플레이어 경고는 유지한다. F6 저효과는 장식 입자를 생략하고 F9는 입자만 켬/끔을 바꾼다. 실제 InputSystem F9 누름/held 회귀를 추가했다.

전용 layer30을 입자 RT camera만 렌더한다. 기존/나중 URP camera에서 해당 bit를 제외하고 Dispose 때 소유했던 bit만 복원한다. 입자는 terrain/적 위, 플레이어/무기 아래에 합성하고 기존 HUD rect는 고정하며 모달은 그 위에 그린다. 수동 paused simulation의 bounds0 회귀를 피하도록 방출 위치의 명시 world bounds를 준다. RT는 크기 변경 때만 교체하고 생성한 material/camera/풀/RT 및 rendering callback은 Disable/Destroy에서 해제한다. 입자가 없거나 꺼졌으면 RT camera도 렌더하지 않는다.

ignored 증거 경계는 `evidence/combat-particles-20261114/`이며 P0 앱/증거를 덮어쓰지 않았다. 실제 게임 QA는 새 `com.biblesurvivor.adam.particlesqa.<run>`/suffix 제품명과 첫 무저장 path gate를 사용한다. 기존 bundle 또는 제품명만으로 저장 격리를 단정하지 않고 실제 Application.persistentDataPath와 정규화 경로를 대조한다. QA fixture는 실제 AdamGame의 표현 갱신/전체 HUD·모달 DrawGameView를 호출하며 복사한 별도 renderer를 사용하지 않는다. 결정적 켬/끔 캡처는 MonoBehaviour 자동 Tick 대신 같은 Host/Core 입력을 직접 진행하는 QA 구동이며, 일반 실제 게임 입력 검토와 구별한다.

| 검사 | 구현자 결과 |
| --- | --- |
| Core 전체/기존 변이 | 실제 `tools/check_all.py`, `tools/check_mutations.py` 각각 exit0; `core-regressions.json` |
| compile/EditMode | `compile-isolated-final` exit0, `EditMode-isolated-final` 54/54（새 QA suffix·marker·출력 거절3개 포함） |
| PlayMode | `PlayMode-isolated-final` 18/18; 실제 PS RT 렌더·실제 F9/hold·풀 처치 우선순위·사망 clear·reload·Disable/Enable 포함 |
| 원래 구조 변이 | bounds 설정 제거→멀리 있는 실제 입자 RT alpha0, 화면 aspect 사용→좌표32px 오차, batch 재소비→2회가4회로 중복; 각각 assertion exit2→복원0 |
| macOS 제품 QA 빌드 | 실제 `native-isolated-probe-build-command.json` exit0; run별 ID·제품명과 무저장 gate/선택적 QA driver 포함 |

실험 중 실패도 남겼다. batchmode UnityTest의 WaitForEndOfFrame은 지원되지 않아 첫 새 시험이 실패했으며 실제 RT가 렌더되는 player-loop 두 프레임 대기로 고쳤다. 새 키보드 회귀는 Editor의 입력 focus routing 설정을 명시하지 않아 실패했고 기존 시험과 동일하게 설정/복원한 뒤 통과했다. 첫 캡처의 잘못된 리소스 이름은 QA 프로세스만 종료하고 바로잡았다. 첫 resize 비교의 off/on 시작 창 크기 불일치는 fixture를 초기 해상도로 되돌리도록 수정했으며 실패 결과를 정상 비교에 합산하지 않는다.

성능 측정 전 `performance-plan.md`에 호스트/조건/임시 가드를 기록했다. 정확히40/120개의 적을 유지하는 **고정 Core 스냅샷의 표현 비용 비교**이며 실제 진행 중인 전체 게임 성능 검증은 아니다. QA 메모리 카탈로그에서120개 동시 존재 조건만 구성하고 원본 콘텐츠는 변경하지 않는다. 원본 프로세스/기기를 정지하지 않으므로 호스트의 다른 부하를 통제했다고 주장하지 않는다. 모바일 실물/GPU/출시 최소 사양/전체 전투 예산 판정은 NOT_RUN이다.

최종 화면 근거는 `capture{660,1100}-handoff`, `capture-odd-handoff`, `capture-resize-handoff`이며 각 세 무기×켜기/끄기6구간을 정상 exit0으로 완료했다. 세 무기 가장자리 및 resize 후 추가 가장자리의15개 PNG 비교에서 실제 전투 영역 밖은 F9 상태 문구를 제외하고 변경0이었다. Core World bytes와12쌍 Run/Profile은 모두 같으며 전체 저장 bytes는 operation ID가 달라 같은 것으로 세지 않는다. 살아 있는 입자의 위치·수명과 Core 시간을 고정한 pause102 검사도 통과했다. resize는 실제 창1100×720→883×587과 합성 viewport823×539/inset, 배율2→1이며 실물 safeArea/기기 회전 수용을 뜻하지 않는다. 상세 `pixel-checks-handoff.json`, `run-profile-equivalence.json`에 결속했다.

`video/{660,1100}-{bible,jawbone,sling}-{off,on}.mp4`12개는60 PNG를10fps로 변환한6초짜리 무음 비교본이다. 타이밍/프레임율 성능이나 P0 오디오 평가 근거를 대신하지 않는다. 일반 실제 게임 QA 실행에서는660×440 시작/숫자 선택·Esc 정지·마우스 재개를 확인했고 정상 exit0으로 종료했다. 후속 Enter 조작 화면은 `stale save revision` 중단이어서 native Enter 재개 PASS로 세지 않는다. 실행 원장에 없는 두 번째 동일 QA bundle 프로세스가 관측되어 동시 저장 가능성을 coordinator에 확인 요청했다. 해당 프로세스는 조작하지 않았으며 독점 실행 재검증 전 일반 QA 저장 원복의 최종 판정은 보류한다. 첫 GUI 클릭은 창 포커스가 달라 도구에서 거절됐고 Apple Events 활성화도 권한 거절됐다. 다른 앱을 조작하거나 권한을 바꾸지 않고 해당 QA PID만 AppKit으로 활성화해 새 화면을 확인한 뒤 실제 재개를 완료했다. 종료 뒤 도구의 app_not_found를 재실행/restore로 우회하지 않았다. F9 실제 입력은 PlayMode의 InputSystem 장치로 검증했으며 도구를 통한 native F9 키 전송은 NOT_RUN이다.

| 고정 장면 측정 (M4 Pro/24GB, macOS26.5.1, Unity6000.6.9f1 Development, 1100×720) | 40적 끔→켬 | 120적 끔→켬 |
| --- | --- | --- |
| 프레임 간격 p95 | 1.371→1.579ms | 1.745→1.841ms |
| 표현 Update+Repaint CPU p95 | 0.632→0.992ms | 1.210→1.208ms |
| Unity 관리 할당 중앙값 | 77,550→77,586B/frame | 100,600→100,636B/frame |
| 실제 입자/RT | 0→64개 /530×263 | 0→64개 /530×263 |

각 조건120 warmup 뒤600샘플, vSync0/프레임 제한 없음, 같은 seed/고정 스냅샷이며 켬/끔 World bytes가 같다. CPU 차이는+0.360ms/−0.0025ms, 할당 차이는 두 조건+36B/frame으로 사전 임시 가드(켬 p95≤33.3ms, 추가 표현 CPU p95≤2ms, 추가 할당 중앙값≤1024B/frame)를 통과했다. 작은 음수 차이는 측정 잡음이며 성능 개선을 뜻하지 않는다. CPU는 명시한 표현 구간이고 프레임 간격은 전체 Player 스케줄링을 포함한다. GPU·실제 전투 진행 전체 비용·최악256입자 비용을 측정했다고 주장하지 않는다. 기존 GUI 전체는 상당한 할당이 있으므로 무할당 표현이 아니다.

초기40적 켬 조건의 입자0 및 Mono 스레드 할당 API의 전체0 읽기는 유효한 비교가 아니어서 `performance-first-invalid.json`에 보존하고 제외했다. 보정 실행은 양쪽 모두 실제 Core 명중 입자64개를 요구하고 [Unity ProfilerRecorder](https://docs.unity.com/en-us/engine/6000.0/script-reference/unity/profiling/profilerrecorder)의 유효한 `GC Allocated In Frame` 카운터로 이전 완료 frame의 전체 관리 할당을 측정했다. 측정용 CSV 문자열 등 QA 구동 오버헤드도 포함되며 결과는 `performance-results.json`/`benchmark-profile/*/frames.csv`에 남겼다.

최종 원장은 `final-binding.json`, 파일별 소스/앱 SHA는 `final-source-sha.json`/`final-app-sha.json`이고 실제 게임 검토 앱은 **`native-isolated-run.json`이 지정한 새 suffixed 앱만** 사용한다. 기존 shared-ID `build-*` 앱은 추가 실행하지 않는다. `native-isolated-path-probe.json`의 무저장 첫 씬/실제 persistentDataPath·realpath와 새 bundle/product를 먼저 확인한 뒤 실행한다. 구체 실행 절차·QA 저장/원본 보존·종료 PID와 source binding은 ignored `handoff-private.md`에 있다. 기존 P0 앱/근거는 그대로다. 자동 Android/iOS 기본 meta 블록79개와 빈 아이콘 슬롯만 분류·원복했고201개 Unity 원본/설정 및335개 기존 Core·raw art·설정 해시를 보존했다. `native-qa-restoration.json`은 본인 실행 전 상태로 되돌린 시점의 결과이고 후속 동일 bundle 프로세스 관측 때문에 QA 최종 원복은 재확인 대기다. `pc-save-preservation.json`, `final-checks.json`의 PC 원본 보존과 local/portable/diff exit0은 별도로 확인했다.

남은 게이트는 root의 독립 PC 코드·실제 시각/조작 리뷰, GC/commit/push/해당 SHA CI다. 모바일 전체 재빌드/실물/실제 safeArea/배터리·발열/GPU/전체 전투 성능·공식 팩 임포트는 NOT_RUN이다. 공식 팩 원본은 ignored 미임포트로 유지했고, 사용자 iOS 앱·기기·저장은 조회하지 않았다. 본인 새 QA Editor/Player는 종료됐으며 제품 편집·PC GUI 소유를 반환한다. 다른 기존 shared-ID 프로세스는 소유를 단정하거나 종료하지 않았다.

동시 writer 발견 후 root 지시에 따라 Core/Runtime을 고정하고 QA Editor 헬퍼에 `-adamParticlesRun`을 추가했다. suffix는 소문자 시작 영숫자6–24자만 허용하며 marker/새 절대 .app 출력 검증을 먼저 통과해야 한다. gate는 suffix·bundle/company/product를 확인하고 기존 PC와 예전 QA 저장 경로를 거절한다. 새 QA의 실제 persistentDataPath는 기존 경로와 다름을 무저장 probe/realpath로 확인했다. Format/새 출력·기존 파일/상대 경로·marker 음성 시험을 포함한 최종54Edit/18Play가 통과했다. 이 변경은 QA 빌드/fixture뿐이며 표현 Runtime은 `runtime-freeze-before-suffix.json`과 같다.

새 격리 앱 두 번의 일반 게임 실행은 실제 exit0이며, 첫 Commit→반복 Commit→종료→재실행의 실제 Host Load에서 Run/Profile/World가 정확히 같았다. 재실행 후 Enter 입력으로 저장 Core 시간이0.200→0.483초, revision8→13으로 증가했고 같은 RunId/Profile을 유지했다. `native-isolated-roundtrip.json`에 기록했다. 입력 직후 도구 스크린샷은 포커스를 돌려줘 suspended 모달이 다시 보일 수 있으므로 화면만으로 재개 판정하지 않았다. 새 격리 실행의 마우스 이벤트 전달은 확인했으나 그 뒤 진행을 마우스에만 귀속하는 native 판정은 NOT_RUN으로 둔다. 기존 실제 포인터 PlayMode 회귀는 통과했다. 두 종료 뒤 새 ID를 조회해 다른 실행이 없는 상태에서 새 QA3파일만 부재 상태로 원복했다(`native-isolated-restoration.json`). 예전 shared QA 원복은 `native-qa-restoration-invalidated.json`에 INVALID로 보존하며 이후 그 컨테이너는 조회/원복하지 않았다. 재실행된 예전 앱의 주체는 확인하지 못했고 restore-window/LaunchServices 재실행 가능성은 시간 순서 근거만 있는 가설로 남긴다.

앞선 P1 새 격리 리뷰 앱 manifest SHA256은 `6ae5563a3011c09d3d1f8d9f258eebe2732ddbe165d13ab9149732753f30a085`, 해당 P1의320소스 manifest 파일 SHA256은 `f4142d799ac57dde30868616401f54210fbe68ff57e99bbc24ba03bc0f88721d`다. performance/PNG 앱과 새 리뷰 앱의 표현 Runtime은 동일하고 새 QA Editor/gate/상태 probe만 달라 각 앱을 따로 결속했다.

## CPT-01 Clear 카메라 상태 수정

독립 코드 리뷰의 MEDIUM CPT-01을 재현했다. 실제 명중으로 입자가 살아 있고 Prepare가 camera를 켠 상태에서 Clear 또는 Run 없는 저장 Reload를 실행하면 기존 코드는 count만0이고 camera.enabled는 true였다. Run 없는 Reload에서는 실제 AdamGame.UpdatePresentation→ArtWorld.Snapshot null→PrepareParticles early return 경계를 사용했다. 두 새 PlayMode 회귀 모두 수정 전 false/true assertion 실패(exit2)를 확인했다.

최소 수정은 Clear가 소유 camera를 즉시 비활성화하는 것뿐이다. 다음 유효 Prepare의 기존 `ParticleCount>0 && Enabled` 재활성화 조건은 그대로다. 같은 camera 인스턴스에서 새 저장 World의 실제 swept hit가 다시 입자를 방출하고 렌더 camera를 켜는 것까지 두 경로에서 확인했다. 격리 사본에서 원래 Clear 본문 그대로의 변이는 같은 두 assertion 실패(exit2), baseline/원복은0이다. `original-clear-command.json`, `clear-mutation-results.json`, `clear-mutation-assertions.json`에 명령·SHA·XML을 남겼다.

최종 실제 compile0, EditMode54/54, PlayMode20/20과 새 suffix macOS QA 빌드0을 통과했다(`engine-commands.json`, `build-command.json`). 기존 Core89/18변이는 앞선 P1의 동일 Core 근거이며 이번 Clear 수정에서는 재실행하지 않았다. Core/DLL/provenance는 그대로이고 local/portable 검사0, 기존335/201원본·PC6 보존을 다시 확인했다. 기존 P1 대비 소스 manifest 변경은 `AdamImpactParticles.cs`와 `AdamParticlePlayTests.cs` 두 파일뿐이다. P1 화면/성능 자료는 과거 해당 소스의 결과로 보존하며 CPT 빌드에서 재측정한 것으로 세지 않는다.

새 리뷰 앱은 **CPT 근거의 `native-verified-run.json`이 지정한 suffix 앱만** 사용한다. 무저장 gate/실제 persistentDataPath·realpath 격리 후 두 Native 실행이 실제 exit0이었고, Commit→재Commit→종료/재실행의 실제 Host Load에서 Run/Profile/World가 같았다. 새 ID의 다른 writer 부재 확인 뒤 새 QA3만 부재 상태로 원복했다. 기존 shared QA/원장 밖 프로세스와 사용자 iOS는 조회·조작·종료·추가 원복하지 않았다. 이번 Native 증거는 저장/로드·종료·원복 확인이며 입력 직후 화면의 재개 진행 귀속은 독립 PC UI 검토와 구별한다.

QA 수집 첫 시도는 상태 JSON 파일 생성 직후 쓰기가 끝나기 전에 읽어 중단됐다. 해당 본인 PID가 사라지고 새 ID의 다른 실행이 없는 것을 확인한 뒤 새 QA만 원복했다. 그 Player의 실제 exit code는 수집하지 못했으므로 성공으로 세지 않는다(`aborted-native-recovery.json`). 수집기가 JSON 파싱 완료까지 기다리도록 고친 별도 기록에서 두 실행의 exit0/로드/원복을 다시 검증했다. 준비 단계의 잘못된 증거 설정 파일명도 실행 전 오류로 따로 보존했다. 제품/Core/QA fixture에 이를 숨기는 변경은 하지 않았다.

현재 CPT 앱 manifest SHA256은 `7bfce240fe329ead4563d412686748bf0ce5d7a5c160288bc505af0efb820ed5`,320소스 manifest 파일 SHA256은 `c47eb8f52861e6f4726bdc873723ed951fd20f1da31374ef0a9c47142d74d338`이다. 파일별 map·현재 source/app·실명령/exit/XML·새 QA 원복·PC6·프로세스·미실행은 CPT `final-binding.json`과 `handoff-private.md`에서 연결한다. 구현자 수정/검증을 마쳤으며 CPT-01의 독립 종결·PC UI 검토·GC/commit/push/CI는 root 소유다.

## 통합 코드 리뷰·최신 native 한계

같은 독립 코드 리뷰어가 최신320소스/334앱·QA Runtime/DLL/shader 동등·54Edit/20Play·원래Clear의 두 assertion FAIL→수정/복원PASS·무저장probe/실제Host복원·두exit0를 확인해 CPT-01 CLOSED, 추가 HIGH/MEDIUM 없음으로 판정했다. `../reviews/combat-particles-code.md`를 따른다. 과거 P1 픽셀·성능과 최신 CPT 회귀를 각 시점의 근거로 유지한다.

최신 native PC 리뷰의 창 포커스가 도구 호출 뒤 다시 background-suspended로 돌아가 연속 전투/실제 파티클·성장의 입력 귀속은 확인하지 못했다. 살아 있는 정확한 본인 PID/창에만 전경 유지 한 번을 시도했으며 해결되지 않아 추가 루프·권한 변경·다른 앱 조작을 중단했다. 이를 제품 결함이나 최신 native 전투 PASS로 판정하지 않는다. 실제 확인한 시작/정지·저장 재실행·두 크기 모달과 원본 보존은 `../reviews/combat-particles-ui.md`의 범위를 따른다. 전체 PC 시각·조작 수용 완료를 주장하지 않으며 남은 native 활성 전투/파티클/성장 검증은 열린 상태다.

이번 통합은 제품 연결 코드·독립 코드 검토·엔진 회귀·제한된 PC 관측을 보존하는 checkpoint다. native 전체 수용/모바일/실물/GPU/최대256입자 실전 성능·공식 팩 임포트를 완료 처리하지 않는다. 다음 R02 기술 연결은 명시 개발용 표현과 저장/전환 검증을 독립 진행하며 이 미검증 상태를 이월해 공개한다.

업로드 결과: `4971277bc97c59b189c4be015b7c1701518615e6`을 `feat/adam-core`에 push했다. 같은 SHA의 [코어·portable Unity CI](https://github.com/petercorders/Bible-Survivor/actions/runs/43541621606)와 [아트·폰트 CI](https://github.com/petercorders/Bible-Survivor/actions/runs/43541621820)는 모두 success다. 원격 CI가 native 전체 수용·GPU/모바일 실물 검증을 대체하지 않는다.
