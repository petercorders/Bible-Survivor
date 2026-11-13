# 03 독립 재검토 — iteration 01

검토일 2026-11-14 · 독립 QA Codex · 제작/검사기 수정 없음.
**종합 FAIL: 아트의 기존 누락·가독성은 개선됐으나 HIGH 1건과 MEDIUM 1건의 검사기 결함을 수정한 뒤 다시 검토해야 한다.** Unity/Android/iOS는 이번 오프라인 QA에서 모두 NOT_RUN이다. 에디터 설치·프로젝트 존재를 오프라인 검사의 엔진 실행 근거로 전용하지 않는다.

## 검토 스냅샷과 실행 경계

- 읽은 입력: `art-revision-03-result.md`, `art-production-readiness.md`, `art-readiness-loop-task.md`, `initial-02.md`, 03의 rig/contract/palette/manifest/pixel-source/tiles/UI 데이터, build/check/player/tiles/UI/review/verify/regen 스크립트.
- 원본 manifest SHA-256: `f08ca05f7adf7755eee4197122fe1a75110973deb612c37f1e00a80467b46b1a`.
- Python 3.11.6 / NumPy 2.4.3 / Pillow 12.1.1. 설치·의존성 변경 없음.
- 원본 01/02/03은 읽기 전용으로 취급했다. `verify_all`은 출력을 덮어쓰므로 **03 전체를 임시 QA 디렉터리에 복사한 뒤** 실행했다(캐시·canvas 제외). 임시 디렉터리의 01/02 경로는 원본으로 향하는 읽기용 링크이며 변환 출력은 별도 임시 파일이다. 검사 변이는 임시 사본만 수정했다.
- 임시 QA 루트: `<local temporary evidence>`. 아래 `<qa03>`은 그 안의 `art-concept-03`, `<neg>`는 `independent-negatives`이다. 임시 파일은 영구 납품물로 간주하지 않으며, 핵심 결과·재현 절차는 이 문서에 보존했다.
- 런타임 PNG **79개 / 실제 197프레임**, export 총83파일. 독립 Python으로 pixel-source의 키→팔레트→RGBA를 복원하고 79개 PNG를 전부 픽셀 대조하여 **79/79 일치**를 확인했다. 완성됐다는 제작자 숫자를 그대로 인용한 결과가 아니다.
- 전체 검증으로 새로 만든 임시 export와 원본 export도 SHA-256으로 전부 비교: **83 대 83, 차이0**. 즉 QA 재생성이 현재 원본과 동일했다.

## 새 결함 — 제작자에게 반환

### A03-01 · HIGH · FAIL — 필수 인계 메타데이터를 바꾸거나 지워도 검사기가 PASS

대상: `scripts/check_art03.py:94` 이후 main, `src/contract_v3.json`, `tests/test_check_art03.py`.

정상 계약에는 PPU32·왼쪽 아래 원점·수평 띠가 있고 rig에는 레이어 순서가 있지만, 검사기는 manifest의 최상위 `ppu/pivotSpace/frameLayout/layerOrder/flip` 존재·계약 일치를 확인하지 않는다. 에셋별 REQ/CONTRACT 검사가 최상위 import 계약까지 보장하는 것은 아니다.

독립 변이 각각의 **실제 CLI 결과**:

| 한 번에 넣은 오류 | exit | 출력 |
| --- | --- | --- |
| manifest `ppu`: 32→48 | **0** | PASS: 79/79 assets, contract, player layers, pixel-source, tiles, ui |
| `pivotSpace`: `TOP-LEFT, Y DOWN` | **0** | 동일 PASS |
| `layerOrder`: 원래 순서 뒤집기 | **0** | 동일 PASS |
| `frameLayout`: `vertical` | **0** | 동일 PASS |
| 위 4필드와 `flip` 모두 삭제 | **0** | 동일 PASS |

현재 정상 산출물의 해당 값이 틀렸다는 뜻은 아니다. 잘못된 단위·좌표계·레이어 순서가 들어온 미래 인계물을 재사용 CLI가 정상이라고 판정한다. 잘못된 PPU는 월드 크기, 좌표계는 피벗 해석, 레이어 순서는 머리/망토의 가림을 바꿀 수 있어 인계 검사 거짓 통과(HIGH)로 분류한다.

**수용 조건:** 필수 최상위 필드의 존재·타입 및 계약 일치를 검사하고, PPU/좌표계/프레임 배치는 독립 계약과, 레이어 순서는 계약 또는 rig와 대조한다. 개별 레이어 `layerOrder` 값도 같은 기준과 일치시킨다. flip은 구조화된 방향 계약 또는 명시된 허용값으로 검증한다. 위 각각의 변이와 각 필드 단독 누락을 CLI 테스트에 추가하고 정상0/오류1을 보존한다. 현재 아트를 다시 그릴 필요는 없다.

### A03-02 · MEDIUM · FAIL — UI 폰트 참조가 실제로 없어도 PASS

대상: `scripts/check_art03.py:450–452`, `export/ui/ui-pieces_v3.json`, `third_party/galmuri/`.

`fonts.title.file`을 `third_party/galmuri/__qa_missing__.ttf`로 바꾸고 UI 데이터 SHA를 manifest와 맞췄다. **실제 종료0, PASS:79/79**였다. checker는 폰트 경로의 접두어만 확인하며 파일 존재를 확인하지 않는다. 실제 검토 사본에는 폰트 파일을 아예 복사하지 않았는데도 정상 baseline이 종료0이었다. 원본에는 사용 폰트4종과 라이선스가 실제 존재하므로 납품 파일의 현재 누락은 아니다.

잘못된 폰트 경로를 인계하면 UI 구성/글자 렌더 단계에서 실패한다. 이것은 사용자에게 새 글꼴 결정을 요구할 상황이 아니라 요구된 파일 참조 검사 누락이다.

**수용 조건:** `--root/--src-root`의 명시된 정책에 따라 fonts의 필수 role·경로·파일 존재를 확인하고, 폰트와 출처 고지를 함께 검증한다. 필요 시 폰트/고지 해시를 manifest에 포함한다. 존재하지 않는 폰트 참조(해시 정합), 실제 파일 누락, 라이선스 누락을 각각 거절하는 CLI 음성 테스트를 추가한다. 검사에 폰트 렌더링 라이브러리를 새로 요구할 필요는 없다.

## A02-01~11 재판정

| 기존 ID | 이번 상태 | 독립 확인과 남은 경계 |
| --- | --- | --- |
| A02-01 플레이어 | **PASS / 해결** | 6레이어×idle4/walk4/death6=18띠, 6/8/8fps·반복/반복/1회. 원본 rig·실제 PNG·브라우저 합성을 대조. base 모든 프레임 발 마지막 행30. walk 팔·발이 교차하고 몸 바운스만 반복하지 않음. flipX 합성에서도 두건·수염·망토가 함께 움직임. |
| A02-02 효과 | **PASS / 해결** | 공통 사망4/12, 성경2/6, 턱뼈 앞날·잔상 각각4/20, 돌2/12, 명중3/15, XP 각2/4. 실제 PNG/격자/fps/loop 대조. 앞날 마지막 프레임이 비는 것은 명시된 종료 표현이며 결손으로 세지 않음. attachPx 포함 턱뼈 contact sheet 확인. |
| A02-03 타일 | **PASS / 해결** | N1 E2 S4 W8의 16프레임·양쪽 rect·전체 마스크 검사 지도 존재. 끝·모서리·T·십자·고립 타일과 이음매를 직접 열람. 한 칸 폭 경로만 지원하고 2×2 넓은 길/대각선은 미지원 예까지 명시. RuleTile 실행 PASS 아님. |
| A02-04 UI | **PASS / 에셋 해결** | 11조각·7아이콘·9-slice border·색·폰트 role·상태 데이터 존재. 조각/늘림 예와 레벨업·보상·꾸미기의 PC/가로/세로 PNG를 직접 확인. 폰트 참조 검사의 별도 결함 A03-02는 미해결. 실제 클릭/키보드/터치 구현 PASS 아님. |
| A02-05 계약/매핑 | **PASS / 정상 납품값 해결** | v3 schema, cell/rect/fps/loop/pivot/PPU/알파/sorting/quality/source/hash, layerOrder 확인. 코어 catalog 검사는 exit0. catalog·Growth.cs 실제 파일 해시는 contract의 두 해시와 일치. 잘못된 최상위 값을 거절하는 문제는 A03-01로 추적. |
| A02-06 검사 거짓 통과 | **PARTIAL / FAIL 유지** | 초기02의 프레임수·범위밖 피벗·누락레이어는 독립 CLI에서 전부1로 거절. alpha/ref도1. 그러나 최상위 import 메타데이터와 폰트 참조 오류가 여전히0이므로 A03-01/02 해결 전 전체 검사 게이트는 닫지 않음. |
| A02-07 재생성/형식 | **PASS / 해결** | 임시 사본 전체11단계0(기대 실패 단계1), 재생성83/83, Pillow 차단 재생성83/83, v1 17/17·v2 53/53 픽셀 일치. 원본01/02 보존. 123+59 풍부한 팔레트는 승인 기준으로 허용; 색수 자체로 BLOCKED 하지 않음. |
| A02-08 피부/머리 | **PASS / 오프라인 해결** | 36조합 보드·12대표×14프레임×3배경×컬러/흑백 보드 확인. s6+갈색/진갈색의 이마선이 전보다 분리됨. 별도 rig 렌더36×14=504프레임이 셀/발/transform assert 통과. 수치만으로 시각 품질을 판정하지 않음. |
| A02-09 적 가독성 | **PASS / 오프라인 해결** | A3 자두색 몸과 밝은 테두리가 잔디·흙길·그늘에서 분리됨. 흑백에서는 가시·얼굴·다리의 실루엣이 남음. PC 40마리, 64px 내7마리, FX 기본/낮춤·흑백·모바일 정지안 직접 확인. 플레이 중 1초 식별·기기 밝기 검증 아님. |
| A02-10 움직임 | **PASS / 검토 수단·오프라인 동작 확인** | GIF/APNG23쌍·local HTML 존재, 메타 검사 exit0. Orca 브라우저에서 실제 canvas를 열고 RAF 재생/프레임 진행·정지 단계/flipX를 확인. 짧은 FX의 모든 시간표를 연속 실시간으로 육안 확인했다는 뜻은 아님(아래 제한). |
| A02-11 조합 범위 | **PASS / 해결** | “대표 조합12(전체 조합 아님)”와 행별 선정 이유 명시. 전체 곱검사는 피부×머리36이며 모든 외형·모든 염색 전체 곱과 구분됨. |

## 명령과 실제 종료코드

### 전체 검증·독립 픽셀 확인

작업 디렉터리를 `<qa03>`으로 둔 실제 명령:

```sh
python3 -I -B scripts/verify_all.py --catalog <repo>/content/genesis/catalog.json --review
```

**최종 exit0.** build0 / checker0 / catalog0 / 제작자 음성15종 하네스0(각 잘못된 입력1) / regen0(83동일) / no-Pillow0(83동일) / legacy-v1변환0(17동일) / legacy-v2변환0(53동일) / 변환 파일 manifest로 거절1(기대1) / review build0 / GIF·APNG 메타0. 임시 사본 `tests/results/verify_all.txt`·`regen_results.txt`·`check_negative_results.*`에 실제 결과가 있다. 원본의 기존 로그를 다시 생성하지 않았다.

독립 Python 픽셀 확인: `json`으로 v3 source·palette를 읽고 각 glyph를 RGBA로 복원한 후 프레임을 가로 연결, `numpy.array_equal`로 실제 PNG와 비교. **exit0, 79/79 일치**. base 인접 프레임의 달라진 픽셀 수:

| 동작 | 인접 프레임 차이 픽셀 수 | 발 마지막 행 |
| --- | --- | --- |
| idle | 173,19,185,16 (마지막→처음 포함) | 30,30,30,30 |
| walk | 199,223,226,198 (마지막→처음 포함) | 30,30,30,30 |
| death | 245,232,325,361,38 (일회성 인접만) | 30,30,30,30,30,30 |

이는 단순히 다른 픽셀이 있다는 구조 확인이다. 별도로 contact sheet에서 다리 자세/팔 위치를 보았으며 숫자가 보행 품질 점수는 아니다. 36피부/머리×14프레임에서 source compose의 transform/no-clipping assert, 32×32 셀, foot30, flip을 두 번 하면 원상 복구됨을 확인했다(**exit0,504프레임**).

### QA가 직접 만든 음성 입력

각 케이스는 새 임시 `export/`+`src/` 사본에 독립 주입했다. 제작자의 테스트 생성 함수를 호출하지 않았다. 실제 checker CLI를 별도 프로세스로 실행했고 하네스 자체 exit0과 자식 프로세스 exit를 구분한다.

```sh
python3 -I -B <qa03>/scripts/check_art03.py --root <neg>/<case> --quiet
```

| case | 독립 입력 변화 | 실제 exit / 확인 메시지 |
| --- | --- | --- |
| baseline | 정상 PNG/manifest/source | 0 / PASS79 |
| frames_coherent | A1 PNG를32×32 1칸으로 자름, manifest frames/size/rect·PNG SHA·source frames/data SHA까지 일치시킴 | **1 / CONTRACT frames1 !=4** |
| pivot | A2 pivot[999,-3], normalized도 같은 수식으로 맞춤 | **1 / PIVOT, CONTRACT, SOURCE** |
| layer_missing | hood_walk manifest 항목 삭제 | **1 / LAYER** |
| alpha_coherent | A1 픽셀 하나를 팔레트 안 shadow.0(알파89)로 바꾸고 PNG SHA·source glyph/keys·source SHA·alpha.values까지 맞춤 | **1 / ALPHA binary 위반만** |
| ref | A1 ref를 monster:__qa_missing__로 변경 | **1 / REF, CONTRACT** |
| ppu | 최상위 ppu48 | **0 / 거짓 PASS** |
| pivotSpace | 최상위 원점/축을 반대로 표기 | **0 / 거짓 PASS** |
| layerOrder | 최상위 레이어 순서 반전 | **0 / 거짓 PASS** |
| frameLayout | 최상위 vertical | **0 / 거짓 PASS** |
| font_reference | title 폰트 파일을 없는 같은 접두어 파일로 변경, UI SHA 갱신 | **0 / 거짓 PASS** |
| metadata_missing | ppu/pivotSpace/frameLayout/layerOrder/flip 삭제 | **0 / 거짓 PASS** |

독립 전체 결과: 임시 루트 `independent-negative-results.json`(12케이스, 정상1+기존 결함5+신규 거짓 통과6). 이 표의 `alpha_coherent`·`frames_coherent`는 해시 불일치나 PNG 크기만 우연히 잡은 결과가 아니도록 source/manifest까지 정합하게 만들었다.

새 두 결함의 최소 재현(원본 대신 임시 사본에만 사용):

```python
import copy, hashlib, json, pathlib, shutil, subprocess, tempfile
original = pathlib.Path('<art-docs>/art-concept-03')
with tempfile.TemporaryDirectory(prefix='art03-qa-repro-') as td:
    root = pathlib.Path(td)
    for part in ('export', 'src', 'third_party'):
        shutil.copytree(original / part, root / part)
    mp = root / 'export/manifest_v3.json'
    normal = json.loads(mp.read_text())
    # A03-01: current checker wrongly returns 0.
    mutated = copy.deepcopy(normal)
    mutated['ppu'] = 48
    mp.write_text(json.dumps(mutated))
    cmd = ['python3', '-I', '-B', str(original / 'scripts/check_art03.py'),
           '--root', str(root), '--quiet']
    print('ppu exit=', subprocess.run(cmd).returncode)
    # A03-02: keep data hash coherent, current checker wrongly returns 0.
    up = root / 'export/ui/ui-pieces_v3.json'
    ui = json.loads(up.read_text())
    ui['fonts']['title']['file'] = 'third_party/galmuri/__qa_missing__.ttf'
    up.write_text(json.dumps(ui))
    normal['data']['ui']['sha256'] = hashlib.sha256(up.read_bytes()).hexdigest()
    mp.write_text(json.dumps(normal))
    print('font ref exit=', subprocess.run(cmd).returncode)
```

## 직접 화면/움직임 검토

`view_image`로 다음 원본 PNG를 직접 열었다(경로 `art-concept-03/review/` 기준).

- 플레이어: `boards/board_player_layers_x3.png`, `board_player_variants_x3.png`, `board_skin-hair-36_x4.png`, `board_player_combos-12_backgrounds_1x.png`. 레이어6종×3동작과 외형 합성4종×3동작의 오른쪽/왼쪽 전체 자세, 갈색 경계와 그림자 배경, 대표12행의 14프레임을 확인했다.
- 전투: `views/view_pc_native.png`, `view_pc_gray_x2.png`, `boards/board_close-quarters_x4.png`, `board_a3_02-vs-03_x4.png`, `board_jawbone-swing_x4.png`, `views/view_phone-l_fx-low_x2.png`, `view_phone-p_x2.png`. `scene()`의 근접 배치·PC 영역 집계 코드를 읽어 40예산에 별도 사망1과 64px내7이 포함되는 범위를 확인했다. A3의 잔디/흙길/그늘 각4프레임과 흑백을 직접 비교했다.
- 타일: `boards/board_path-16mask_x3.png`, `board_path-testmap_x2.png`. 끝·코너·교차·링·고립·미지원 폭 예를 보았다. 16마스크와 stored/recomputed adjacency는 checker 재실행으로도 확인했다.
- UI: `boards/board_ui-pieces_x4.png`, `mock/mock_{custom,levelup,reward}_{pc,phone-l,phone-p}_x2.png` 9종 전부. 글자/카드가 겹치거나 안전영역 밖으로 넘는 문제는 확인되지 않았다. disabled 얼굴 선택은 설명된 아트1종 범위와 맞는다. 터치 최소치는 게임px 후보이며 dp/pt 실측 아님.

Orca CLI로 `review/index.html`을 file URL로 열었다. page ID `2e97441c-6dcc-45da-a5e1-13900cef4190`, 제목 `Art 03 local review`, 실제 canvas14개·내장 PNG29개 로딩 완료를 확인했다. 외부 Design 캔버스는 방문하지 않았다.

- 브라우저 RAF 콜백 기준 재생: visible 상태에서 약1.6초, 실제23개 렌더 시점을 관찰했다. idle/walk/A1/A2/A3의4프레임, 성경/돌/XP의2프레임이 반복되고 death의1~6이 한 번 진행된 뒤 끝에 멈췄다.
- 일반 eval 시각으로 replay 직후 샘플링한 첫 시도는 0번 프레임 표기와 짧은 일회성 효과의 중간 프레임 누락이 있어 재생 품질 근거에서 제외했다. RAF 시각으로 다시 시작한 위 관찰도 20fps 효과의 모든 프레임을 연속 캡처하지는 못했다. 캡처 간격/브라우저 상태를 에셋 고장으로 판정하지 않았다.
- 별도로 정지 시각0/250ms에서 s6/brown·두건+망토+수염 합성·flipX를 canvas에 렌더하고 스크린샷2장을 `view_image`로 확인했다. walk의 발/팔 차이·death의 내려앉음·동일 레이어 위치를 확인했다. 원본 파일은 변경하지 않았고 브라우저의 검토 상태만 바꿨다.
- GIF/APNG23쌍의 frame/duration/loop는 전체 CLI 메타 검사로 확인했다. **23쌍을 모두 실시간 육안 재생해 본 것은 아니다.** 높은 fps 효과의 프레임별 모양은 contact sheet로 확인했고, Unity의 실제 공격 타이밍과 알파 합성은 NOT_RUN이다.

스크린샷은 Orca의 임시 screenshot 저장소에 있으며 영구 공개 파일로 넘기지 않는다. 재생 JSON은 임시 QA 루트의 `browser-raf.json`, `browser-playback.json`, `browser-step0.json`, `browser-step2.json`에 있다. 재생 경로/관찰 범위/결과는 위에 영구 기록했다.

## 다음 반복 범위

제작자는 A03-01/02 검사 코드·명시 계약·독립 음성 케이스를 수정하고 실제 실행 기록을 제공한다. QA는 수정 검사기에 위 **동일 변이**를 다시 주입해 0→1로 바뀌는지 확인하고 정상79개·재생성83개 회귀를 검토한다. 아트/동작을 바꾸지 않으면 이미 확인한 모든 화면을 무조건 다시 만드는 대신 바뀐 부분 중심으로 확인한다. canvas/비공개 blob·캐시·임시 실행로그는 공개 통합 대상에서 제외하며, 저장소 통합·CI·커밋·push는 coordinator 소유다.
