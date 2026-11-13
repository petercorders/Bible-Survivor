# Unity art 01 독립 검토

- 검토일: 2026-11-14, 독립 아트 QA.
- 판정: **관측한 macOS Unity 통합 아트·폰트 인계 PASS**. 현재 HIGH 0 / MEDIUM 0 / 미해결 LOW 0. 증거 시점 LOW UAR-01은 아래 후속 대조로 CLOSED. 전체 게임 출시·기기·성능 판정은 아니다.
- 대상: `Bible-Survivor/docs/progress/unity-art.md`, 승인된 docs 원본03/font01, 저장소 art 사본, Resources 등록 파일·meta·index, importer/runtime 소스, `evidence/unity-art-20261114/`.
- 본인 실행: 읽기 전용 Python 표준 라이브러리 대조와 view_image. Editor·Player·GUI·엔진 테스트·빌드 실행 **NOT_RUN**. 아래 39/10은 개발자 실행 XML을 본인이 읽어 확인한 수치이며 본인의 실행 횟수에 더하지 않았다.
- 원본·게임 코드·저장 파일은 쓰지 않았다. 이번 산출물은 이 보고서와 `unity-art-01-gc.md`뿐이다. 기존 원본의 Design 제작과 Pillow 합성·오프라인 폰트 샘플 검사는 구분하며, 이번 엔진 통합을 원본 재제작으로 세지 않는다.

## 바이트·등록 계약 독립 대조

임포트 도구를 실행하지 않고 원본과 실제 파일을 직접 읽었다. Python assert 실패는 비0 종료하는 검사로 수행했으며 아래 검사 호출은 모두 실제 **exit 0**이었다.

| 검사 | 실제 결과 |
|---|---|
| docs 원본03 → repo art → Resources PNG SHA256 | 79/79 동일, manifest의 각 asset SHA와 동일 |
| manifest → index animation 필드 | id·frames·fps·loop·pivot 일치, six-layer order 일치 |
| 실제 PNG meta | 79개 전부 PPU32, filterMode0(Point), mipmap0 |
| 실제 Multiple Sprite metadata | 197/197 프레임 이름·rect(x=i×width,y=0,w,h)·정규화 pivot·LBRT border가 index와 일치 |
| 원본 UI → index → meta | 79개 전체의 border/state 대조, 정의 없는 asset은 border0/state빈값, 불일치0 |
| asset-registration.json | PNG79+TTF4 = 83개 실제 파일 SHA·meta SHA·GUID 모두 일치, GUID83 고유 |
| Sprite identity | 197 spriteID 모두 고유 |
| docs font01 → repo art → Resources outline | 4/4 동일; 기존 Bitmap 전용 원본을 대체 수입하지 않음 |
| 배포 Adam.app 고지 | `Contents/Resources/Galmuri-LICENSE.txt`, `Galmuri-font-manifest.json`가 Resources/승인 원본 계약과 동일 SHA |

해시 고정값:

- art manifest: `0c7f6ff0b86d83217beedf62f298294cd6c01c036ab589e2abed383c1e429751`
- 실제 index: `c6be027b8da2f2c543a25d6317493c0359452526b513fcc0cdb9d0293b486fc6`
- font manifest: `16308da615ad12445794a89ac8dfd612b7a4060223e4e657b18eef79c37579bd`
- LICENSE: `86a3ee9495f942f0243f18c103da9faca27adb88142613edb8bb852e56c892c1`
- title Galmuri11-Bold: `45d901e379138dd91873af157640f60aa4cf6beaa885c3efd2a8c279e039a237`
- label Galmuri11: `e24256f42e43713d2ea086a1e1669d78b968f5b3cc547e5c157f0606ffa5def1`
- body Galmuri9: `e84e821b18be15b9e3a907ceb83cfba25fabf51c80b7edf0d2921cf8f8e1a11d`
- small Galmuri7: `c845c6bf6f7236ed370aed4a8f8d9b4f25e6a18f63515777fe4153d287c32917`

## importer·소비자 계약

`AdamArtImporter.cs`는 PNG/index SHA를 확인하고 Multiple/PPU32/Point/Uncompressed/no-mipmap/FullRect/Clamp/FromInput을 설정한다. full rect로 투명 등록 영역을 유지하며 frame마다 명시적 rect/pivot/border와 결정적인 ID를 생성한다. 실제 meta에서 위 핵심 등록값을 전수 대조했다. 플랫폼별 설정에 overridden0인 compression 값이 남아 있는 것은 활성 override로 해석하지 않았다.

`AdamArt.cs`는 index 고정 SHA와 79개 자산 수를 요구하며 실제 Resources Sprite의 프레임 수·rect·pixel pivot·PPU·filter·border를 검사한다. `Frame()`은 floor(time×fps), loop일 때 modulo, nonloop일 때 마지막 프레임에 고정한다.

- 6레이어: cloak_back → base → hair_short → beard → hood → cloak_front.
- 각 레이어 idle4@6fps loop, walk4@8fps loop, death6@8fps nonloop가 원본/index 모두 일치한다.
- `AdamArtWorld.Draw()`는 발 기준 동일 matrix에서 부모 X반사 한 번만 적용한 뒤 6레이어를 그린다. 개별 레이어 flip 중첩 없음. 무기는 FacingMatrix를 공유하고 GUI.matrix를 복원한다.
- tile은 회전·반사 없이 원본 N1/E2/S4/W8 mask를 사용한다. 현재 한 칸 폭 십자 배치는 승인된 제한 안에 있다. grass4와 decal2 사용, 모든 납품물을 화면에 노출한다는 주장은 하지 않는다.
- nine-slice는 원본 LBRT 모서리를 정수2배로 유지하고 edge/center를 반복하며 끝 조각을 자른다. 프레임과 내용이 서로 침범하는 화면 증거 없음.
- 평상시 Clock은 core snapshot time에 묶이고, 사망은 presentation clock으로 6/8초 표시 후 새 도전 UI를 열고 마지막 프레임 유지. lifecycle pause 시 presentation도 정지하도록 구현되어 있다. terminal에서 core 시간이 그대로여도 active/hits/kills/damage/위치 변화로 snapshot 갱신한다.

## 직접 본 화면과 움직임 증거의 범위

`view_image`로 원본 `chr_player_layer-base_death_32.png`와 `chr_player_layer-hood_walk_32.png`를 열어 실제 자세·흔들림 프레임이 존재함을 재확인했다. 엔진 증거는 다음을 원래 해상도로 열었다.

| 실제 이미지 | 관찰 |
|---|---|
| player/verified660-modal.png (660×472, titlebar32 포함 → client660×440) | 세 카드 heading/body와 하단 안내가 보이고 잘림 없음; 2행 카드의 여유와 모서리 보존 확인 |
| player/verified660-resume.png | 작은 client의 1배 world와 2배 글꼴이 분리되어 유지됨; 발·경로·적·XP 구분 가능 |
| player/verified1100-shot.png (1100×752 → client1100×720) | sling stone+trail, 세 적 타입, XP, 6레이어 주인공, 십자 path 접합이 보임; 원본 픽셀을 흐리게 늘인 흔적 없음 |
| player/jawbone-frame-06.png / -10.png | 다른 시각의 held jawbone/swing trail/hit 표현 확인; 접전에서도 플레이어·thorn beast를 구분 가능. 이 두 장의 과거 글꼴은 최종 폰트 판정에 사용하지 않음 |
| player/death-fixed-resume.png | 누운 사망 자세와 사망 안내가 보이며 새 도전 modal이 아직 덮지 않음 |
| player/death-fixed-frame0.png / frame2.png / frame3.png | 뒤 시점에는 새 도전 modal 표시, 배경과 경계 유지; frame0/2/3만으로 6장 사망 프레임이 모두 재생되었다고 주장하지 않음 |

정지 이미지와 연속 번호 이미지 대조는 실제 장면·상태 변화를 뒷받침한다. 일정한 간격의 6프레임 전체 capture나 본인의 연속 Player 관람을 수행한 것은 아니다. 재생 속도/loop와 pause/resume/사망 완료의 기능 근거는 별도로 소스와 아래 엔진 XML에 있다. gallery enemy·drop 위치는 `AdamArtEvidence.CreateGallerySaves()`에서 실제 core ID/시간을 얻은 뒤 연출한 배치다. 이를 자연 전투의 밀집도·밸런스 합격으로 세지 않는다.

## 폰트·본문 크기·글리프 근거

실제 `AdamFontImporter`와 TTF meta는 Dynamic/HintedRaster(includeFontData1, fallbackFontReferences빈값)이며 family는 각 자체 Galmuri family다. Bold의 family가 Galmuri11인 것은 동일 family 이름이며 잘못된 role 대체라는 뜻이 아니다. `AdamFonts`는 명시적 Resources Font를 직접 load하고 atlas rebuild에서도 Point를 설정한다. 이 경로에 OSFont 생성은 없다.

`AdamGame` 실제 스타일: title24/label24/body20/small16, 승인 base12/12/10/8의 정수2배. 기본 GUI.matrix 확대 없이 각 role의 Font를 사용한다. 660 최종 화면에서 제목·효과·안내를 직접 읽어 대조했으며 본문 clipping은 관찰하지 못했다.

개발자 Edit XML에서 다음 세 font test가 Passed인 것을 읽었고, 구현도 직접 읽었다.

1. `ActualRuntimeStringsMatchAll71ReviewedCardsAndFitTheirRealRoleBoxes`: 실제 C# Describe() 결과 71개의 heading/effect를 승인 metrics와 문자열로 1:1 대조하고, 실제 Font/GUIStyle CalcSize를 client660/1100 카드 폭·높이에 비교한다. 이전 Python C# 재구현만의 대조 한계는 이 추가 엔진 근거로 보완된다.
2. `AllRolesUseOriginalOutlineRasterWithoutFallbackReferencesAndWithCompleteHangul`: 4role 각각 TextCore FontEngine의 한글11172 glyph index가 모두 비0인지 검사하고 HasCharacter 확인, 샘플 텍스트 atlas를 GPU→CPU readback하여 잉크 존재와 alpha0/255만 허용한다.
3. `FontReimportKeepsGuidAndLocalFileIdentity`: reimport 전후 font GUID/localID 보존.

이 결과는 해당 macOS Unity6000.6.9f1 경로의 실제 엔진 글리프·샘플 래스터 증거다. 독립 offline font01 QA 횟수와 합산한 별도 Unity지원 PASS 수치로 만들지 않는다. 모든 한글11172×4 pixel 렌더, OS 전역 fallback 부재, 기기별 hinting, 화면 접근성 정량 측정까지 입증하지 않는다.

## 실행 증거 및 회귀

본인이 XML/summary를 읽어 확인한 개발자 최종 실행:

- `final-validation-compile/summary.json`: exit0, compile/import PASS.
- `final-validation-EditMode/results.xml`: Passed total39/pass39/fail0/skip0.
- `final-validation-PlayMode/results.xml`: Passed total10/pass10/fail0/skip0.
- `verified660-exit.json`, `verified1100-exit.json`, `death-fixed-exit.json`: 각각 exit0.
- `mutation-static/results.xml`: Failed1 (`AllSixLayersShareRegisteredFramesAndDeclaredPlayback`), restored Passed0fail.
- `mutation-death-modal/results.xml`: Failed1 (`DeathClipCannotBeCoveredByClickableRestartModal`), restored Passed0fail.
- `mutation-terminal-clock/mutant-results.xml`: 같은 death test Failed1, restored Passed0fail.

위 변이는 제작자가 실행한 실제 옛 동작 회귀 증거이며 이번 QA가 새 변이를 실행한 것이 아니다. 접촉 사망·정지 프레임·modal 가림을 일반 PASS 수치만으로 추론하지 않고 각각 실패/복구 XML을 확인했다.

## UAR-01: 최종 증거 이후 테스트 파일 변화 — 후속 CLOSED

`final-source-build-sha.json`의 `files`를 현재 실파일 SHA와 비교한 본인 Python 명령은 exit0으로 실행되었고, 불일치 목록은 `UnityGame/Assets/Adam/Tests/PlayMode/AdamPlayTests.cs` 하나였다. 나머지 기록된 파일(아트·폰트 소비자와 해당 Editor/Play 아트 테스트 포함)은 일치했다. `git diff -- <위 파일>`에는 사망 중 Digit1/2/3이 clip을 건너뛰지 못하게 하는 테스트 3개 추가가 보인다.

재현: JSON의 각 path에 `hashlib.sha256(Path(path).read_bytes()).hexdigest()`를 계산하여 저장값과 비교한다. 39/10은 그 당시 버전의 증거이며 지금 추가된 테스트에 대한 실행 증거로 쓰면 안 된다. 현 아트 등록 결함은 아니므로 LOW이고 본 검토를 막지 않는다. 수용조건: root가 병행 코드/UI 검토 후 최종 통합 소스 기준 테스트를 다시 수행하고, source/build SHA·XML·빌드 증거를 함께 갱신하거나 테스트 추가 이후 미실행 범위를 명시한다.

## 남은 범위

새 HIGH/MEDIUM 아트 수정 요청은 없다. 엔진·GUI를 본인 실행하지 않았으므로 별도 UI 담당의 실제 조작 검증과 root의 통합 판단이 필요하다. 성능·device·자연 전투 밸런스·전역 fallback·전체 한글 pixel 렌더·접근성 실측은 **NOT_RUN**. 원본03/font01은 수정하지 않고 유지하며, 그림 추가나 코드 변경 요청이 생기면 각 담당에게 돌린다.


## 후속 재검토: death digit 최종 증거 / UAR-01 CLOSED

2026-11-14 같은 독립 아트 QA가 `docs/progress/unity-art.md`의 마지막 인계절과 `evidence/unity-death-digit-20261114/`를 좁게 검토했다. 본 절은 위 초기 39Edit/10Play 및 당시 LOW 상태를 최신 증거로 보완한다. **UAR-01 CLOSED, 기존 관측 아트 PASS 유지, 새 HIGH/MEDIUM/LOW 없음.** UA01 및 ART-UI-01의 코드·정밀 키입력 해결 판정은 동일 별도 담당 소유이며 본인의 실행·승인으로 합산하지 않는다.

### 최신 소스·시험·빌드 결속

읽기 전용 Python으로 각 JSON에 기록된 경로를 열고 SHA256을 직접 계산했다. 최신 `final-source-build-sha.json` 자체 SHA는 `4909dd04305c5f6c448e4262ae6b045b052841c36bcd1588f0eede6ba14790a1`이다.

- 최신 231/231 파일 SHA 일치, missing/drift 0. `UnityGame/Assets/Adam/`의 현재 파일 229개가 전부 기록되어 누락된 새 파일 0이며, 나머지 2개는 실제 app의 `BibleSurvivor.Core.dll`과 `Adam.Unity.dll`이다.
- `unchanged-inputs.json`의 원본 art03/font01/Core 등 229개도 실제 파일 SHA 일치, drift 0.
- 새 `asset-registration.json`의 83개 entry가 이전 unity-art 등록과 완전히 같다. 각 실제 PNG79/outlineTTF4의 bytes SHA, meta SHA, GUID를 다시 읽어 모두 일치했다. 임포터·index·197프레임 등록을 바꾸지 않았으므로 이전 전체 그림 검토는 반복하지 않았다.
- 옛 final-source-build-sha와 겹치는 경로 중 변경은 `Runtime/AdamGame.cs`, `Tests/PlayMode/AdamPlayTests.cs`, app의 `Adam.Unity.dll` 세 개다. Core DLL은 동일하고 아트·폰트 runtime/importer/전용 frame 테스트는 동일하다.
- 최신 AdamGame SHA: `6711044b572a26019293c71c4ac1c055ee40489ba61f07faaacf58ca084bd304`.
- 최신 AdamPlayTests SHA: `0c0303bc17906bfe14c8aa7034c35aed66b56fe54dce7130a4d2b6196211b5d3`.
- 최신 app Adam.Unity.dll SHA: `ab6e68cd89424f8f6a4341c89d6ec97f249fb82c12703087fa0e5a4f77b6c94b`.

실제 소스 `AdamGame.cs:111`의 SelectSlot 공통 guard에 DeathPresentationVisible가 commandFrame·Start·Choose보다 앞에 있는 것을 읽었다. 이번 검토는 이 변경 범위를 식별한 것이며 별도 입력 QA의 세부 결과를 대체하지 않는다.

### 실행 기록 읽기와 실제 본인 명령

개발자 실행 자료를 XML/JSON 원문으로 확인했다.

| 증거 | 읽어 확인한 실제 결과 |
|---|---|
| final-compile/summary.json | Unity/wrapper exit0 PASS |
| final-EditMode/results.xml + summary | 39/39 PASS, fail0/skip0, exit0 |
| fixed-PlayMode/results.xml + summary | 13/13 PASS, fail0/skip0, exit0 |
| baseline-no-guard/results.xml | 10 PASS/3 FAIL/skip0; digit1/2/3 세 시험이 실패 |
| mutant-results.xml + mutant-command.json | 동일 digit 세 시험 0 PASS/3 FAIL, exit2 |
| restored-results.xml + restored-command.json | 3/3 PASS/skip0, exit0 |
| final-build-command.json + final-build.log | BuildMac argv, exit0, 로그에 `Exiting batchmode successfully now!` |
| player/death660-exit.json / death1100-exit.json | 각각 실제 process wait exit0 |
| player/restoration.json | 여섯 entry 모두 existenceMatches/bytesMatch true |
| player/log-check.json | 두 player log의 gameplayExceptions 빈 배열 |
| ownership.json | RELEASED |

본인 SHA/등록/XML reader의 첫 호출은 마지막 log-check가 dict가 아닌 list인 것을 잘못 처리하여 Python AttributeError로 exit1이었다. 이는 제품 검사 실패·엔진 오류가 아니며, list로 읽도록 고친 다음 호출이 실제 exit0으로 231/231·229/229 해시와 XML·6저장·빌드 결과를 확인했다. 추가 파일 집합 검사도 exit0으로 229 Adam 파일 누락0을 확인했다. 다른 read-only 검사에서 임포트 도구·validation.py·Unity를 실행하지 않았다.

최신 파일 결속 재현의 핵심은 아래와 같다(저장소 루트에서 실행, 쓰기 없음).

```python
import hashlib, json
from pathlib import Path
r = Path('evidence/unity-death-digit-20261114')
files = json.loads((r / 'final-source-build-sha.json').read_text())['files']
assert len(files) == 231
assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest() == h
           for p, h in files.items())
actual = {str(p) for p in Path('UnityGame/Assets/Adam').rglob('*') if p.is_file()}
assert not (actual - files.keys())
```

새 13건 결과가 추가된 세 시험을 실제로 포함하고 새 DLL 빌드가 현재 소스 snapshot에 결속되어, 초기 UAR-01의 미실행 범위 표시 조건은 해소되었다. 39/13과 변이3건은 **개발자 실행 증거**, 본인 Editor/Player/GUI 실행은 계속 **NOT_RUN**이다. 원본·시각 리소스 불변에 따른 기존 화면 관측 PASS만 유지하며, 자연 전투 밸런스·성능/device·전역 fallback·전체 한글 pixel 렌더·접근성 미실측 범위를 넓히지 않는다. 별도 코드/UI 재검토, root 통합·GC·commit/push·해당 SHA CI는 여전히 별도 게이트다.
