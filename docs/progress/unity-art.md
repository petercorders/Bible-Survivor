# 아담 아트03 Unity 연결

상태: 구현·개발자 실검증 PASS. 독립 코드·아트/UI 재검토와 GC/commit/push는 coordinator 게이트다.

승인 원본은 `art/art-concept-03/`의 검수03 반복02이며 manifest SHA256은 `0c7f6ff0b86d83217beedf62f298294cd6c01c036ab589e2abed383c1e429751`다. 원본03·Core/R02·CI/checker/hook은 변경하지 않는다. Outline 폰트 sidecar `art/art-unity-font-01/`는 독립 QA PASS 후 root의 `f0d4368` 인계로 연결했다. manifest SHA256은 `16308da615ad12445794a89ac8dfd612b7a4060223e4e657b18eef79c37579bd`다. 기존 Bitmap TTF는 임포트하지 않았다.

`tools/unity_prepare_art.py`가 관련 계약 해시와 PNG 바이트를 확인한 뒤 79개 PNG를 `UnityGame/Assets/Adam/Resources/Art03/`에 복사하고 Unity용 index를 만든다. 기존 `.meta`는 유지한다. `AdamArtImporter`는 index와 개별 PNG 해시를 거절 조건으로 사용하며 실제 Multiple Sprite를 PPU32, Point, 비압축, mip 없음, full rect, 지정 pivot/border로 임포트한다. 프레임 ID는 stable ID와 번호로 결정한다. 원본의 binary/blend alpha를 그대로 유지한다.

기존 IMGUI 전체 화면/입력 경로를 유지해 UI-01/02를 보호했다. `AdamArtWorld`는 BattleWorld snapshot과 세션을 읽으며 전투에 값을 쓰지 않는다. 플레이어 6레이어의 idle/walk/death, 적 A1/A2/A3, 무기3/궤적/피격·처치 효과, XP와 지형을 표시한다. 플레이어는 공통 피벗에서 부모 반전을 한 번 적용한다. 턱뼈 효과의 프레임별 top-left 부착점과 물맷돌 방향 회전을 적용한다. 연타는 소비된 Core burst 시각으로 판단한다. 무기 효과는 별도 명중·피해를 만들지 않는다.

Ground→GroundDecal/그림자→Pickups→Actors(플레이어 내부6레이어)→WeaponFX→FX→UI 순서다. 흙길은 N1/E2/S4/W8 마스크의 한 칸 폭 십자 길만 사용하며 2×2 광폭/대각 연결·타일 회전은 지원하지 않는다. 풀 타일4종과 장식2종을 좌표 기반으로 반복한다. 원본의 나머지 장식/메도우/UI 소품은 임포트됐으나 해당 게임 기능을 새로 구현한 것으로 세지 않는다. 보조7종 아이콘은 원본에 없으며 한국어 정보로 표시한다.

월드 화면은 정수 배율(1100×720에서2, 660×440에서1)과 정수 위치를 사용한다. 기존 Core 카메라보다 작은 영역을 잘라 보여줄 수 있으며 Core 스폰/충돌/밸런스는 바꾸지 않았다. UI 조각은2배로 고정된 모서리와 타일 반복 가장자리/가운데, focus 화살표, pressed 내용2px 이동, disabled 상태를 사용한다. 카드의 효과/레벨 텍스트와 모달 내부 재개 버튼은 그대로 연결한다.

애니메이션은 시뮬레이션 시각을 쓰므로 정지·선택·중단 중 멈춘다. 사망 후 마지막 clip만 표현 시각으로 재생해 마지막 프레임을 유지하며 Core 시간은 멈춘다. 저장 재개는 world의 위치/방향/시각으로 재구성하고 이동 입력은 초기화해 idle로 시작한다. 사망한 과거 도전의 위치를 새 실행에서 복원하는 기능은 없다. 피격·처치 슬롯64개를 재사용하고 슬롯 재할당/새 도전/불러오기/종료 때 상태를 초기화한다. 월드별 GameObject·Collider·이벤트를 추가하지 않는다. 성능 개선은 측정하지 않았다.

증거 루트: `evidence/unity-art-20261114/`(ignored). raw 로그/개인 저장 경로·파일은 여기에만 둔다. 런타임 저장 위치는 `Application.persistentDataPath/adam-prototype.json`이다. 기존/새 제품 저장과 .bak/.lock 여섯 파일을 보존했다. 중간 실행에는 별도 fixture를 교체해 사용하고, 모든 시험 Player 종료 뒤 원본 바이트·존재 여부를 최종 원복했다.

최종 검증은 아래 기록을 따른다. `gallery/` 저장은 실제 Core wave/session API가 만든 적 ID/시간에 위치만 연출한 시각 QA fixture이며 자연 전투나 밸런스 측정이 아니다. Android/iOS·물리 터치 기기·성능/메모리 예산은 NOT_RUN이다.

폰트는 `tools/unity_prepare_fonts.py`가 검수 manifest/contract/4TTF/license 해시를 먼저 확인해 별도 `Resources/ArtFonts/`로 복사한다. 임포터는 Dynamic·Hinted Raster·includeFontData와 외부 fontReferences 없음으로 설정한다. 빈 fontNames는 Unity가 글꼴 자체 가족명(Galmuri11/9/7)으로 정규화하므로 빈 배열을 유지한다고 주장하지 않는다. 런타임은 OS font 생성 API를 제거하고 원본 Font 에셋을 직접 사용한다. title/label24, body20, small16px로 역할을 나누며 GUI.matrix 배율은1이다. PC client 최소660×440을 유지해 더 작은 창을 소수배로 축소하지 않는다. 이 최소 크기 규칙은 모바일 지원 판정이 아니다.

엔진 시험은 실제 C# 카드71장(무기3×8+보조47)의 문자열이 검수 데이터와 같은지,660/1100의 실제 역할별 폭/행 상자에 맞는지 확인한다. 네 원본 폰트의 TextCore FontEngine face를 직접 로드해 전체 한글11172자의 nonzero glyph index를 확인하고 HasCharacter도 대조한다. named OS 대체 글꼴/외부 fontReferences를 지정하지 않는다. 요청한 한글/숫자/기호 샘플 atlas를 GPU→CPU로 읽어 alpha가0/255인 실제 Hint Raster와 Point를 확인했다. 전체11172자×4역할의 픽셀 렌더 대조나 계약 밖 문자·다른 OS의 전역 fallback 동작을 검증한 것은 아니다. 동적 atlas 재생성 때 Point를 다시 적용하고 소유 컴포넌트 종료 때 이벤트를 해제한다.

빌드 전 PNG/Font/license SHA와 실제 imported Sprite 등록을 다시 검사한다. 배포 app의 `Contents/Resources/Galmuri-LICENSE.txt`와 `Galmuri-font-manifest.json`에도 원본 고지/출처를 제공한다.

## 최종 개발자 검증

환경: macOS, Unity6000.6.9f1, Development macOS Player, 클라이언트1100×720/660×440. 이 단계는 Editor 실제 실행과 macOS Player까지만 검증했다.

| 검사 | 실제 결과 | 명령/증거(위 evidence 루트 기준) |
| --- | --- | --- |
| compile | Unity exit0, wrapper0 | `unity_check.py run --mode compile`, `final-validation-compile/summary.json`, `unity.log` |
| EditMode | exit0,39/39, skipped0 | `--mode EditMode`, `final-validation-EditMode/results.xml` |
| PlayMode | exit0,10/10, skipped0 | `--mode PlayMode`, `final-validation-PlayMode/results.xml` |
| macOS 개발 빌드 | exit0, batchmode successfully | `-executeMethod BibleSurvivor.Unity.Editor.AdamProject.BuildMac`, `final-validation-commands.json`, `final-validation-build.log` |
| GUID/meta/ForceText | PASS | `unity_check.py static`, `handoff-static.json` |
| 실제 빌드 입력/DLL 일치 | exit0 | `python3 tools/unity_check_project.py`, `handoff-project-check.json` |
| Core/R02 별도 게이트 | exit0 | `python3 tools/check_all.py`, `core-all-final.log`, `source-checks-final.json` |
| 아트/폰트 오프라인 계약 | 각각 exit0 | `python3 tools/check_art.py`, `python3 tools/check_fonts.py`, `art-source-final.log`, `font-source-final.log` |
| 새 준비 도구 격리 검사 | exit0,7/7 | `python3 tools/unity_art_tool_tests.py`, `handoff-tool-tests.log` |
| 정적 포즈 회귀 변이 | Unity exit2,1개 행동 실패 | `mutation-static/results.xml`: expected1/actual0, compile 오류 없음 |
| 같은 변이 복사본 원복 | exit0,1/1 | `mutation-static/restored-results.xml`, `restored-command.json` |
| 원본PNG/Font·GUID 등록 |79PNG+4TTF | `asset-registration.json`, 실제 강제 재임포트는 EditMode에서 GUID/local ID 비교 |
| 배포 고지/manifest | 원본 바이트 동일 | app의 Galmuri LICENSE/font manifest, `asset-registration.json` |
| 원본 저장/종료 | 여섯 파일 existence/bytes 모두true | `player/restoration-final.json`, 마지막 실행 `verified660-exit.json`, `verified1100-exit.json`, `death-fixed-exit.json` 모두0 |

`final-validation-commands.json`은 마지막 코드의 정확한 argv/프로세스 exit를, 각 모드 summary는 timeout/로그/XML/wrapper exit를 보존한다. 최종 색상은03 `text.0`이며 신규 프레임 등록/최소창/폰트 변경을 모두 포함한 동일 코드에서 마지막39/10와 빌드를 실행했다.

기존25/7 회귀를 유지하면서(최종39/10) 등록/재임포트/세 적의 서로 다른 fps/6레이어/경로 마스크/정수 피벗/부착점/방향 변환/정지·저장 복귀·사망의 마지막 프레임/고정 효과 슬롯을 추가했다. 세 시작 무기 각각500step에서, 표현을 반복 샘플한 전투와 표현 없는 같은 입력 전투의 전체 BattleSnapshot을 매 step 직렬화해 같음을 대조했다. Core 파일/기존 DLL·카탈로그·원본03/폰트 패키지를 이 worker가 바꾸지 않았다.

## 실제 화면과 입력

- `player/verified660-modal.png`(최종), `handoff1100-modal.png`(같은 카드 layout/글꼴의 이전 실행):4역할 글꼴과 실제 한국어 두 줄 카드, 효과/0→1/5,9slice. HUD 제목 조각이 패널 밖에 남지 않는다.660 실행 argv는640×400 요청이며 실제 창은660×472(타이틀32 포함)로 최소 client660×440을 확인했다.
- `handoff660-key1.png`와 `handoff660-after-save.json`: 숫자1로 crown1만 적용, choices 비움.1100은 `handoff1100-choice2.png`/`handoff1100-after-save.json`에서 클릭2로 underwear1만 적용했다. 최대3 연속 선택/같은 frame 중복 명령은 PlayMode 기존 시험으로 유지했다.
- `handoff660-resume.png`/`handoff660-enter.png`, `handoff1100-pause.png`/`handoff1100-resume.png`/`handoff1100-enter.png`: 모달 안 보이는 버튼과 실제 클릭/Enter 뒤 전투 화면·진행 시각. Esc raw 입력도 보존했지만 native focus 전환의 자동 정지가 섞이고 즉시 캡처가 이전 frame일 수 있어 모든 Esc PNG를 단독 성공 근거로 세지 않는다. 키보드 Esc의 정확한 정지 상태는 PlayMode 실제 Keyboard 입력 시험에서 assert했다. 독립 UI 담당이 동일 최종 빌드를 다시 조작하는 게이트가 남는다.
- `bible-verified-world.png`: 연출 gallery에서 세 적·반전 플레이어·실제 성경 궤도. `jawbone-frame-06.png`/`-10.png`: frame별 부착점의 턱뼈. 이후 종료 상태 갱신/가드를 보강했지만 활성 전투의 성경/턱뼈 계산·부착점은 유지했다. 최종 전체 코드의 glyph/UI/세 적/돌 궤적은 `verified660-*`와 `verified1100-*`로 다시 확인했다.
- `verified1100-shot.png`(최종), `sling-final-shot.png`(이전 실행): 실제 Player/글꼴에서 세 적과 실제 방향으로 회전하는 돌/궤적. 가까운 적에 바로 닿아 보이지 않는 경우를 피하려고 gallery 위치를 멀리 연출했다. 공격 피해/스폰/보상 수치를 바꾸지 않았으며 자연 밸런스 시험으로 세지 않는다.

Orca의 일부 foreground 확보 실패, synthetic 입력, 종료 후 screenshot permission 오류는 raw JSON에 그대로 있다. 성공은 tool 호출 반환만으로 판단하지 않고 새 화면/저장 및 직접 Popen wait로 확인했다. 닫힌 앱의 후속 캡처가 실패한 종료도 실제 Player wait0과 종료 확인으로 구분했다. 중간 자동화 재시도의 Player는 모두 종료 후 원복했고 최종 인계 시 UnityGame 대상 Editor와 Player는 실행하지 않는다.

템플릿 URP/종료 ComputeBuffer 정리 경고는 raw 로그에 남지만 두 해상도의 실제 IMGUI 텍스처/한글 표시를 막는 현상은 관측하지 않았다. 렌더 파이프라인 교체는 하지 않았다. 메모리 누수 없음/목표 FPS/성능 향상을 입증한 것은 아니다.

## 협업과 남은 게이트

Design에 초기9slice/카드/폰트 계약을 대조하고 실제1100 PNG 피드백을 전달했다(`msg_240fea547345`, `msg_ac159a11375d`). 받은 outline 역할/크기/라이선스/정수 배율을 실제 엔진에서 확인했다. 최종660/1100·glyph 결과 피드백은 원 dispatch가 종료돼 send가거절되고 reply도 `terminal_not_found`였다. 위 실제 화면·폰트 관측을 최종 디자인 피드백으로 coordinator가 보관/새 담당에게 전달하면 된다. 신규 그림 부족/임포트 결함은 현재 관측하지 않았고 보조7 아이콘·미사용 시안 기능은 여전히 연결하지 않았다.

Sprite Editor 공식 API용 정확한 Editor 참조 추가로 기존 allowlist가 한 번 FAIL했다(`core-project-final.log`). root가 `d12311b`의 공유 경계 검사로 교정했고 local재실행0이다. root 인계 기준 해당 baseline의 core/portable/boundary CI43470940690과art/font CI43470940666은PASS다. 이 worker의 새 게임 아트 코드·도구는 아직 커밋 전이므로 이 CI를 새 게임 아트 완료 SHA의 CI로 대체하지 않는다. root checker/helper를 격리 복사하는 새 worker 도구는 없다.

남은 것은 coordinator의 독립 코드·실아트/UI 재검토→필요 수정·동일 담당 재검토→통합 GC→선별 commit/push와 해당 SHA CI다. Android/iOS 빌드/실물 기기/멀티터치·노치·다른 OS·장시간 성능/메모리·색각/흑백 접근성 실측은 NOT_RUN이다. 기존 진화/외형 편집/후속 라운드 기능을 구현하거나 검증한 것으로 세지 않는다.

## 최종 사망 경로 보강

실제 Core 접촉 사망 fixture에서 최초 Player는 process exit0이어도 화면 FAIL이었다(`player/death-visible.log`/PNG). Core는 접촉 사망 때 Wave를null로 하고 적/드롭/투사체를 비운 뒤, 전투 시각을 늘리기 전에 반환한다. 시각만 비교하던 표현 cache가 이전 적 목록을 남겨 `AdamArtWorld.Draw`에서 NullReferenceException을 냈다. 초기 직접 Session.Damage/빈 arena 시험으로는 이 경로를 잡지 못했다.

수정은 Core 없이 표현 계층에 한정했다. 전투 시각 외에 활성/종료·hits/kills/collected/damage·위치 변화를 갱신 조건으로 사용하고, Wave는nullable로 읽는다. 종료된 arena는 남은 전투 객체/무기 효과를 그리지 않는다. 최종 사망 snapshot의 이동/좌우 방향도 같은 시각에서 새로 읽는다. 플레이어 사망6frame@8fps가0.75초 동안 보인 뒤 새 도전 모달을 표시한다. 연출 중 앱 중단은 기존 재개 모달로 멈추며 Core 시각/규칙은 추가로 진행하지 않는다.

실제 PlayMode 회귀는 정상 Core가 만든 적의 저장 위치와 체력1만 QA fixture로 연출해 `World.Tick`의 접촉 사망을 발생시킨다. 시각 불변·Wave null·빈 최종 enemy snapshot·마지막 이동의 좌향을 assert한 뒤 실제660 IMGUI 전체 renderer의 옛 새 도전 hit rect를 클릭해 도전이 바뀌지 않음을 확인한다. 단순 property 모사 시험으로 대신하지 않았다.

- `death-contact-failing.xml`: 원래 clock-only 코드에서 same-time snapshot이 비워지지 않아 FAIL(exit2); 수정 뒤10/10 PASS.
- `mutation-death-modal/results.xml`: 별도 복사본에서 즉시 새 도전 모달을 복구하면 실제 클릭이 새 session을 만들어 FAIL(exit2); 같은 복사본 원복1/1 PASS.
- `mutation-terminal-clock/mutant-results.xml`: 최종 코드에서 원래 clock-only 갱신 조건을 복구하면 최종 snapshot assert가 FAIL(exit2); 같은 복사본 원복1/1 PASS. 두 변이 모두 컴파일 오류를 실패 근거로 세지 않았다.
- `player/death-fixed-resume.png`: 실제1100 Player의 보이는 쓰러짐 포즈/종료 안내. `death-fixed-frame3.png`: clip 뒤 정상 새 도전 모달. `death-fixed.log`에는 이전 NullReferenceException/KeyNotFoundException/IndexOutOfRangeException이 없고 직접 wait exit0이다.
- 마지막 코드의 `final-validation-compile`, `final-validation-EditMode`39/39, `final-validation-PlayMode`10/10, `final-validation-build`는 모두 exit0이다. 그 뒤 `verified660-modal.png`/`verified660-choice2.png`/`verified660-pause.png`/`verified660-resume.png`과 `verified1100-shot.png`/`verified1100-pause.png`/`verified1100-enter.png`를 재실행해 실제 입력/화면을 확인했다. 최신660 클릭2 저장은 underwear1만 적용됐고 시간43.216…→45.066…이다.

최종 코드·배포 Adam.Unity/Core DLL SHA는 `final-source-build-sha.json`, PNG/TTF83개 원본 SHA와 GUID는 `asset-registration.json`, 최종 여섯 저장 존재/바이트 원복은 `player/restoration-final.json`으로 인계한다. 이후 scene/Player/Editor 소유는 coordinator에게 반환한다.

## 독립 리뷰 후속: 사망 연출의 숫자 선택 경계

coordinator의 `msg_7362d559685b`에서 Player 종료/여섯 저장 원복과 실행 소유 반환을 확인한 뒤 실검증을 재개했다. 대기 중에는 소스/오프라인만 진행했다. 기존 가드 없는 코드의 전체 PlayMode는10 PASS/3 FAIL(exit2)이었고, 세 실패 모두 실제 숫자 입력이 Session을 바꾼 행동 assert다. SelectSlot 공통 진입점에 DeathPresentationVisible 가드를 추가한 뒤13/13 PASS(exit0)다.

독립 코드 리뷰는 보이는0.75초 사망 clip 중 마우스 새 도전 버튼은 없지만 Update의 digit1–3→SelectSlot→Host.Start 경로가 열리는 차이를 지적했다. 선택 명령의 공통 경계에서 DeathPresentationVisible를 확인해 두 입력의 의미를 같게 할 범위다. Core·원본03·font/PNG는 수정하지 않는다.

AdamPlayTests에 digit1/2/3별 실제 InputSystem 회귀를 준비했다. 정상 Core 접촉 사망 fixture에서 연출 시계를 정지하고 component Update는 계속 실행해 각 키/동시 키 입력의 Session 참조·Run null·골드·전투 시각 불변을 검사한다. 앱 중단/복귀와 키를 연출 종료 경계까지 유지한 경우에도 지연 시작이 없어야 하며, 종료 뒤 키를 놓고 새로 누르면 해당 무기로 정상 시작해야 한다. 전체13건 실행이 PASS이며 기존 hold/중복/작은창 재개/마우스 사망 회귀도 포함된다. 증거 루트는 `evidence/unity-death-digit-20261114/`이며 실행 도구 소유 대기는 `ownership.json`에 기록했다.


### 독립 코드 UA01 / UI ART-UI-01 수정 최종 인계

`SelectSlot`은 `DeathPresentationVisible`일 때 commandFrame 기록/Host.Start/Host.Choose 전에 반환한다. 클릭과 Update 숫자키가 같은 경계를 사용하며 Core·저장·전투 수치의 의미는 바꾸지 않았다. 해당 연출 중 숫자 명령을 버리고, clip이 끝난 뒤에도 유지 중인 키를 예약 명령으로 재사용하지 않는다.

새 증거는 `evidence/unity-death-digit-20261114/`다. 이전 `unity-art-20261114/final-source-build-sha.json` 이후 추가된 시험과 가드는 **새** `final-source-build-sha.json`의 전체 Adam 파일231개(빌드 DLL 포함)로 대체했다. 이 해시 후 게임 소스·시험·빌드를 수정하지 않았다.

| 검증 | 실제 결과·증거 |
| --- | --- |
| 수정 전 실제 전체 PlayMode | `baseline-no-guard/results.xml`: 10 PASS/3 FAIL, Unity exit2; 세 digit 모두 Session 참조 변경 assert 실패 |
| 수정 후 실제 전체 PlayMode | `fixed-PlayMode/results.xml`: 13/13, Unity exit0; 기존 중복/hold/touch/앱 중단/660 모달 클릭·마우스 사망 회귀 유지 |
| 원래 우회 구조 변이 | 별도 캐시 프로젝트에 최종 소스 복사 후 공통 가드만 제거; `mutant-results.xml`: 0/3, exit2, 동일 Session assert 실패; `restored-results.xml`: 3/3, exit0 |
| 실제 compile / EditMode | `final-compile/summary.json` exit0; `final-EditMode/results.xml` 39/39, exit0, skipped0 |
| 실제 macOS 개발 빌드 | `final-build-command.json`의 BuildMac argv/exit0와 `final-build.log`; 새 Player의 두 실행 직접 wait exit0 |
| 도구/원본/등록 | project checker exit0; art 도구 격리7건 PASS; 원본03/font/Core 파일 해시 불변, PNG/TTF83개 bytes/GUID/meta 이전 증거와 동일 (`asset-registration.json`); 새 빌드 Core DLL도 이전 SHA와 동일 |

새 InputSystem 세 시험은 정상 Core 접촉 사망으로 Wave null·Run null·DeathPresentationVisible true를 만든다. 숫자별·동시 세 키·중단/복귀·종료까지 hold 중 Session/골드/전투 시각/Run 불변을 검사한다. clip 종료 후 release→새 press만 올바른 무기를 시작하고 같은 키 hold로 RunId가 다시 바뀌지 않는다. 컴파일 오류를 변이 실패로 세지 않았다.

실제660 Player `player/death660-return.png`와1100 `death1100-return.png`에 보이는 사망 포즈/종료 안내를 확인했다. 포커스 중단으로 clip 시계가 멈춘 경우 재개했고,1100 `death1100-resume2.png`에서 정상 종료 모달, `death1100-select3-final.png`에서 물맷돌 새 도전이 보인다.660 `death660-digit2.png`에서는 종료 후 성경 새 도전을 확인했다. 저장에도 각각 bible/sling 새 RunId와 동일 골드39가 있다. 첫660 digit 시도는 focus 중단 모달로 바뀌었고 Run null이었다. native 도구 호출 사이0.75초 시점/전달 여부를 정확히 재지 못하므로 **실제 GUI에서 clip 중 숫자 차단의 정밀 타이밍 성공으로 세지 않는다**; 그 경계의 Session/Run/골드 증명은 실제 InputSystem PlayMode 회귀와 변이 결과다.

두 Player 모두 직접 process wait exit0(`death660-exit.json`, `death1100-exit.json`)이고 신규 로그에 관측 gameplay exception은 없다(`log-check.json`). 종료 명령 뒤 캡처 대상 창이 없어 CLI가 실패한 raw JSON도 보존했다. `player/restoration.json` 여섯 원본 저장의 존재/bytes 모두true, `ownership.json` RELEASED이며 대상 UnityGame Editor/Player를 남기지 않았다. 개인 경로/원본 bytes는 ignored evidence에만 있다.

남은 게이트는 동일 독립 코드/UI 담당의 UA01/ART-UI-01 재검토, 새 전체 소스 해시와 관측 증거 대조(UAR-01), coordinator GC/commit/push 및 해당 SHA CI다. 기존 실물 기기·모바일·장기 성능/메모리 NOT_RUN 범위는 그대로다.

## Coordinator 독립 재검토·통합

동일 [코드 리뷰](../reviews/unity-art-code.md)에서 UA01, [UI 리뷰](../reviews/unity-art-ui.md)에서 ART-UI-01을 해결 상태로 확인했다. 공통 입력 가드와 실제 InputSystem13개 통과, 원래 가드 없는 구조의 동일3개 행동 실패→복원3개 통과를 대조했다. 아트 QA는 새231개 소스/시험/빌드 SHA와83개 등록을 대조해 증거 시점 LOW UAR-01을 닫고 [관측 아트/폰트 PASS](../art/reviews/unity-art-01-review.md)를 유지했다. 검토 범위의 추가 HIGH/MEDIUM은 없다.

최종 native UI 후속은660 사망 포즈를 관측했지만 포커스 중단이 섞여0.75초 내 입력 차단을 정밀 측정한 것은 아니다.1100 후속은 닫힌 창 복구 조작이 Player를 재실행해 두 프로세스가 겹쳤으므로 NOT_RUN/오염으로 제외했다. 경계 해결 판정은 실제 엔진 입력/원회귀와 코드 가드를 근거로 하며 무제한 native 입력 인증을 뜻하지 않는다. 후속의 종료 상태는660 exit0, 명시1100 SIGTERM143, QA 재실행 프로세스의 exit는 미회수(null)·소멸 확인으로 기록했다. 모든 Player 소멸과 여섯 저장 존재/bytes 원복을 확인했다.

Coordinator는 prepared-art 도구7개와 strict/portable checker를 다시 실행해 exit0을 확인했다. 원격 코어 CI에 도구7개 검사를 추가했으며 이는 실제 Editor 실행이 아니다. [통합 GC](../retrospectives/R01-unity-art.md)를 남기고 허용 파일만 커밋·push·해당 SHA CI 확인을 진행한다. 공개 README 화면은 시각 검토용 연출 배치로 표시했으며 자연 밀도/밸런스 결과로 쓰지 않는다.
