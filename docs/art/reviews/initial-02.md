# art-concept-02 독립 아트 QA — 초기 결함표

검토일: 2026-11-13 · 담당: 독립 QA Codex · 대상: `art-concept-02/` 읽기 전용.
판정: **FAIL — 시각 방향은 유지 가능하지만 오프라인 production-draft 인계 조건 미충족.**
이 판정은 02가 최종 에셋이라고 주장했다는 뜻이 아니다. 02는 자체적으로 concept 상태를 명시한다. 이번 검토는 승인된 `art-readiness-loop-task.md`의 개발 인계 조건에 대한 부족분을 03 제작자에게 넘기는 초기 결함표다. 03 제작·기획·게임 코드·전역 스킬은 수정하지 않았다.

## 직접 확인한 근거와 범위

- 원본 직접 읽음: `src/player_layers_32.json`, `src/palette_v02.json`, `scripts/{player02,px02,enemies02,terrain02,hud02,build_art02}.py`, `out/pixel-source_art-b02.json`, `canvas/project/canvas.json`. `art-production-workflow.md`, `claude-design-brief.md`, 작업 지시서 및 02 결과 문서를 대조했다. 결과 문서의 PASS를 검토 결론으로 전용하지 않았다.
- `view_image` 직접 열람: `out/boards/board_{player,enemies,weapons,background}_zoom.png`; `out/views/view_pc_{native,x2,gray_x2}.png`, `view_phone-{l,p}_native.png`; `out/sheets/scene_adam-late_fx-low_800.png`; `out/preview/enm_a3-thorn-beast_walk_32_x8.png`; `out/png/chr_player_layer-base_idle_32.png`, `ui_hud-pc_static_640x360.png`.
- Python `/usr/local/bin/python3`, Pillow **12.1.1**, NumPy **2.4.3** 사용 가능. 새 설치 없음. 검사 Python은 `-B`로 실행했고 이미지를 편집·저장하지 않았다.
- 격자 JSON 53개 스프라이트를 메모리에서 RGBA로 직접 복원하여 각각의 실제 PNG와 픽셀 단위 대조: **53/53 일치**, 행 수 불일치 0. 잘린 문자열 행은 원본의 명시된 우측 투명 패딩 규칙으로 읽었다. PNG 54개 중 나머지 1개는 HUD 통짜 이미지이며 격자 원본 목록에 없다.
- 검토 스냅샷 `out/pixel-source_art-b02.json` SHA-256: `6bfa20867577d4f60736c673886291e6bad32acf8cafeda28d83b7db34e5b055`.
- 원본 빌드는 출력 파일을 덮어쓰므로 실행하지 않았다. 대신 실제 검증 루프를 AST로 추출해 메모리 데이터에 실행했다. 아래 음성 입력 결과와 실행 범위를 구분한다.

## 우선 결함 및 수용 조건

| ID | 심각도 / 상태 | 구체 근거·재현 및 영향 | 03 수용 조건 |
| --- | --- | --- | --- |
| A02-01 | HIGH / FAIL | `src/player_layers_32.json`의 6레이어는 각각 정지 격자 한 장이다. `out/png/chr_player_layer-*_idle_32.png`도 **32×32 = 실제 1프레임**이며 `frames_planned:4`가 이를 채우지 않는다. walk/death 파일은 없다. 기본 3합성도 idle 1장뿐이라 걷기·사망·부위 동기화를 인계할 수 없다. | 6레이어 각각 idle4/6fps, walk4/8fps, death6/8fps를 같은 셀·피벗·프레임 순서·타임라인으로 납품. walk는 팔·발의 교차 동작이 프레임별로 보여야 함. 오른쪽 원본/왼쪽 flipX 조합 contact sheet와 실제 재생 파일로 바운스만 반복·레이어 이탈·루프 점프를 재검토. |
| A02-02 | HIGH / FAIL | `out/pixel-source_art-b02.json` 및 실제 PNG에서 턱뼈 잔상 `fx_jawbone-smear_hit_64`는 **1/4**, 돌 `wpn_sling-stone_spin_8` **1/2**, 명중 `fx_hit-spark_static_16` **1/3**, 기본/추가 XP는 각각 **1/2**프레임. 공통 적 사망 효과 자체가 없다. 명중은 fps0/loop true라 3프레임 일회성 효과의 시간 계약도 없다. 성경은 실제2장/6fps로 이번 지시 조건을 충족하지만 `frames_planned:4`는 불일치한다. | 공통 사망4/12fps, 턱뼈4/20fps, 돌2/12fps·명중3/15fps, XP 각2/4fps, 성경2/6fps. 반복/일회성 구분, 타격 방향·회전 중심·alpha 정책을 실제 프레임과 일치시킬 것. 무기 ID와 효과 파일 이름이 달라도 명시 매핑으로 연결. |
| A02-03 | HIGH / FAIL | `scripts/terrain02.py:world()`의 길은 `PATH` 곡선까지의 픽셀별 거리로 800×800 장면에 직접 칠한다. 납품된 `til_*`는 grass4+meadow2의 **6개뿐**, soil/path 타일·인접 매핑·연결 검사 지도가 없다. 배경 보드의 2×2 반복은 풀 표면 확인일 뿐 길 코너/교차/끝 검사가 아니다. | 4방향 인접 비트 정의(N/E/S/W와 비트 순서 포함), 16마스크 타일 및 ID↔rect 매핑, 모든 마스크를 쓰는 검사 지도. 끝·직선·회전 코너·T·십자·타일 경계·flip 여부를 확대/1배로 확인. 대각선 지원/미지원과 처리 규칙 명시. Unity RuleTile 설치 없이 오프라인 검증 가능해야 함. |
| A02-04 | HIGH / FAIL | `out/png/ui_hud-pc_static_640x360.png` 1개가 HUD 전체다. `hud02.py:frame()`의 주석은 “9-slice style”이지만 실제 9-slice 에셋·border 값이 없다. 독립 바/슬롯/상태 패널·폰트 데이터 납품 없음. 레벨업 3택1·보스 보상·기본 꾸미기 화면도 없다. | 바·슬롯·아이콘·패널을 재사용 PNG/정수 rect로 분리하고 border L/B/R/T·폰트 파일/크기·색 토큰·선택/포커스/비활성 상태 데이터 제공. 3종 요구 화면을 같은 조각으로 구성한 시안 제공. 모바일 가로/세로는 후보로 유지하며 안전영역·터치영역 좌표/가정 표기. 시안은 클릭 구현 PASS로 쓰지 않음. |
| A02-05 | HIGH / FAIL | 원본 JSON 최상위는 `status,batch,created,palette,note,player_layers_source,sprites`이며 schemaVersion 없음. 개별 cell/pivot/fps는 있으나 frame rect·pivot 좌표계/PPU·sorting·quality_group·source/export 경로/해시 계약이 없다. 6개 layer 항목에는 `loop`, `alpha_ok`도 모두 빠진다. layer_order는 다른 원본 JSON에만 있다. `bible/jawbone/sling`, `a1/a2/a3` stable ID와 core/content의 대응표도 납품물에 없다. | 버전 있는 manifest에 모든 필수 필드, 경로 존재·파일 해시·프레임 범위·동일 레이어 계약 검사 제공. 코드 쪽 실제 계약을 인용한 stable ID 대응표와 32px 채택/48px 비교 후보 구분 제공. 값 누락 시 가져오기 담당자가 추측하지 않도록 검사 실패 처리. |
| A02-06 | HIGH / FAIL | `build_art02.py:65–96` 검증은 프레임 간 크기·팔레트·alpha·일부 발줄만 본다. **실제 검증 루프**에 넣은 음성 입력에서 뱀4→1프레임, 나방 pivot `[999,-3]`, hood 레이어 삭제가 모두 ACCEPT였다. `max(bottom)`만 확인해서 개별 프레임 발줄 차이도 일반적으로 빠질 수 있다. 현 데이터에 그 차이가 있다는 뜻은 아니다. JSON/PNG 참조·필수 ID 검증도 없다. | 프레임 기대수·pivot 범위/좌표계·필수 레이어·동일 타임라인·개별 발줄·alpha·경로/ID 참조를 검사. 실제 잘못된 프레임수/pivot/누락레이어/alpha/참조 각각을 독립 음성 입력으로 검사기에 넣고 **프로세스 비0 종료** 로그 보존. 정상 입력은 0. 계획 필드로 실제 누락을 통과시키지 말 것. |
| A02-07 | HIGH / FAIL | 01의 팔레트≤48/단일 문자→색/고정 폭 행 규격과 02의 층별 named ramp/스프라이트별 keys/우측 생략 행이 충돌한다. 02는 legacy 설명과 다른데 schemaVersion·명시 변환기·호환성 경계가 없다. `build_art02.py`는 01 비교 PNG와 macOS `/System/Library/Fonts/AppleSDGothicNeo.ttc`에도 의존한다. 원본이 영구 폴더에 있는 점은 양호하나 깨끗한 재생성은 미검증이다. | legacy v1 보존, v2→v3 의미/투명 패딩/색 치환 형식을 문서화하고 새 schema로 구분. runtime PNG 재생성을 비교 보드/시스템 글꼴 의존과 분리하거나 의존을 정확히 선언. 원본→새 임시 **출력 디렉터리** 재생성 로그·PNG/manifest 해시 비교 제공. 02를 덮어쓰지 말 것. 48색으로 압축하거나 48px로 전면 변경할 필요 없음. |
| A02-08 | MEDIUM / FAIL | `board_player_zoom.png`의 마지막 s6 피부 행에서 darkbrown/brown 조합은 이마·옆머리·얼굴의 경계가 이어져 보인다. 1배 PC에서는 얼굴 자체가 작아 명도 차이에 의존하기 어렵다. 원본 recolor 키/6종 램프/18개 정지 조합 증거는 있으므로 치환 기능 전체 누락은 아니다. | 현재 스타일·피부 톤을 유지하면서 머리 경계/밝은 면을 보강. s6×모든 머리색, 밝은 피부, 수염/두건/망토 대표 조합을 잔디·흙길·그늘/흑백에서 1배와 정수 확대 비교. idle/walk/death의 같은 조합이 프레임 전체에서 유지되어야 함. |
| A02-09 | MEDIUM / FAIL | PC 흑백/1배에서 A3의 갈색 몸통은 바닥·마른 풀에 가깝고 가시 끝·눈·검은 발에 주로 의존한다. 현 배치 A3는 주로 잔디 위이며, 흙길 위 A3·낮춤 FX와 직접 겹친 A3를 통제 비교한 증거가 없다. `scene()`은 플레이어 주변 빈 공간을 둔 고정 배치라 적40+FX 화면이 있다고 근접 중첩까지 검증된 것은 아니다. | A3 실루엣/몸통 밝기 또는 테두리를 보강하고 잔디·흙길·그늘 각각에서 컬러/흑백/FX 낮춤 비교. 동일 카메라 적40 예산, 플레이어 근처 실제 효과·적 중첩 장면을 제공하여 플레이어/적/XP/공격의 구분을 독립 확인. 평균 밝기 수치를 단독 합격 근거로 삼지 않음. |
| A02-10 | MEDIUM / FAIL | 애니메이션 검토 파일 GIF/APNG/영상이 없다. 적 시트에는 A1 4개 고유 프레임, A2·A3는 각각 4칸/3개 고유 프레임(1번=3번)이 있다. 반복 중간 포즈의 재사용은 정상 왕복 동작일 수 있어 결함으로 단정하지 않지만 정지 보드로 속도·보행·루프 연결을 판정할 수 없다. | 각 실제 fps로 재생 가능한 영구 검토 파일과 contact sheet 제공. 레이어 합성·flipX·사망 비반복 종료·적 루프·FX 종료를 재생 확인. 실제 재생을 못 봤다면 그대로 NOT_RUN 표기하고 검토 가능 상태로 인계. |
| A02-11 | LOW / 제안 | `board_player_zoom.png`의 “피부6 × 머리6 × 염색6” 제목은 실제 전체 Cartesian 조합을 보여주는 것으로 읽힐 수 있으나 코드는 zip6 + 망토6 + s6 머리6 = **18개 표본**만 그린다. `metrics_art-b02.json`의 바닥 블록 다양도 또한 시각 품질 점수가 아니다. | “18개 대표 조합”으로 범위를 명시하고 극단·가림 위험 조합 선정 근거를 함께 제공. 모든 조합을 검사했다는 주장은 실제 검사 범위가 있을 때만 사용. 낮은 취향 차이는 종료 방해 조건으로 확대하지 않음. |

## 실측 프레임 표

`out/pixel-source_art-b02.json`의 배열 길이와 PNG 폭÷cell 폭을 각각 대조했다.

| 대상 | 실제 프레임 / fps | 이번 인계 목표 | 판정 |
| --- | --- | --- | --- |
| 플레이어 6레이어 + 3합성 idle | 각각1 / 6 | idle4 | FAIL |
| 플레이어 6레이어 walk / death | 없음 | walk4/8, death6/8 | FAIL |
| A1 move32 | 4 / 8 | 4 / 8 | 정적 구조 PASS, 재생 NOT_RUN |
| A2 fly32 | 4 / 12 (고유3) | 4 / 12 | 정적 구조 PASS, 재생 NOT_RUN |
| A3 walk32 | 4 / 6 (고유3) | 4 / 6 | 정적 구조 PASS, 가독성 FAIL, 재생 NOT_RUN |
| 공통 적 사망 | 없음 | 4 / 12 | FAIL |
| 성경16 | 2 / 6 | 2 / 6 | 정적 구조 PASS, planned 메타데이터 수정 필요 |
| 턱뼈 잔상64 | 1 / 20 | 4 / 20 | FAIL |
| 돌8 | 1 / 12 | 2 / 12 | FAIL |
| 명중16 | 1 / 0 | 3 / 15 | FAIL |
| 기본/추가 XP8 | 각각1 / 4 | 각각2 / 4 | FAIL |

48px 적 비교 후보는 채택된 납품 수에 더하지 않았다. 플레이어 base에 피부·튜닉·허리띠가 함께 있는 현재 6레이어 체계는 이번 지시에서 허용된 기준으로 취급하며, 과거 9레이어 제안으로 다시 분리하도록 요구하지 않는다.

## 검사 결과와 재현 방법

검사 1: Python 표준 `json/pathlib/hashlib` + Pillow/NumPy로 모든 `sprites`를 순회했다. `palette.actor/ui/terrain`의 `램프.번호`를 RGBA로 풀고 `keys`를 거쳐 문자를 해석, 점/생략 픽셀은 투명 0으로 패딩했다. 각 프레임을 가로 연결하여 `out/png/<name>.png`와 `numpy.array_equal` 비교했다. **종료0, 불일치0**, 실제 `ground:true` 스프라이트의 개별 발 마지막 행 차이0. 이것은 프레임 수 완비·Unity 피벗 검증을 뜻하지 않는다.

검사 2: 실제 검증 코드를 바꾸지 않고 AST로 `for name, s in SPR.items():` 노드만 추출했다. PNG를 cell 단위로 잘라 메모리 `SPR`을 만든 다음 `fails=[]`, `rows=[]`, 실제 `px02`와 NumPy를 제공하여 동일 노드를 실행했다. 원본 파일·PNG·캐시 쓰기 없이 `python3 -B`로 실행했다.

| 입력 변이 | 실제 원본 루프 결과 | 기대 |
| --- | --- | --- |
| 기존 53종 | fails=[] | 정상 데이터의 제한적 검사 통과 |
| A1 frame 배열 4→1 | fails=[] / ACCEPT | REJECT |
| A2 pivot `[16,1]`→`[999,-3]` | fails=[] / ACCEPT | REJECT |
| hood layer 항목 삭제 | fails=[] / ACCEPT | REJECT |
| A1 첫 픽셀 RGBA `[1,2,3,127]` | 색+alpha 두 결함으로 REJECT | REJECT |
| A1 첫 픽셀 RGBA `[1,2,3,255]` | 팔레트 밖 색으로 REJECT | REJECT |

음성 입력 캡처 하네스 자체 종료코드는 **0**(결과 관찰을 마쳤다는 의미)이다. 원래 전체 빌드의 종료코드를 측정했다고 쓰지 않는다. 원본은 `fails`가 생기면 `sys.exit(1)`하지만 거짓 정상인 위 3개 변이는 그 분기에 도달하지 않는다. 잘못된 파일/ID 참조 거절은 원본 검사에 항목이 없어 NOT_RUN이며 03에서 필수다. 원본 빌드 전체·출력 재생성·재생성 음성 테스트의 실제 종료코드 증거는 **NOT_RUN**이다.

## 유지할 장점과 미검증 경계

- 정지 PNG↔격자 일치, 고정 32px 셀, 현재 데이터의 발 정렬, opaque 배우/분리된 반투명 FX는 오프라인 구조 확인을 통과했다. FX 낮춤에서는 책 본체·돌·턱뼈 앞날이 남아 효과 옵션의 방향을 재사용할 수 있다.
- 정지 PC에서 플레이어의 밝은 튜닉, 보라 뱀, 장밋빛 나방, 청록 XP가 분리된다. 확대 얼굴·튜닉·망토의 재질 차이도 확인했다. 그 장점을 유지하고 누락·가독성·계약을 고치는 것이 03 범위다.
- 영구 source/scripts/canvas가 존재하며 `third_party/galmuri/LICENSE.txt`에 저작권 및 SIL OFL 1.1 전문이 있다. 폰트 4개가 함께 있다. 03 재배포물에도 이 고지와 사용 파일의 출처를 유지해야 한다. 법률 적합성 검토를 수행했다는 뜻은 아니다.
- **캔버스 라이브 렌더·계정 연결: NOT_RUN.** 로컬 canvas.json과 11개 아트보드 파일 존재는 확인했지만 외부 캔버스의 실제 로딩/Design 제작 실행을 독립 확인하지 않았다. 제작자의 기존 로그인 차단 보고는 참고 정보이며 이번 검토의 직접 관찰로 쓰지 않는다.
- **애니메이션 실제 시간 재생: NOT_RUN.** view_image로 프레임 줄/정지 보드만 봤다. 반복 프레임 수만으로 동작 품질을 판정하지 않았다.
- **Unity 임포트·SpriteRect·PPU·pivot·sorting·Tilemap·알파 블렌딩·게임플레이/1초 식별: NOT_RUN.** Unity를 실행하지 않았다. 오프라인 통과 후에도 별도 게이트다.
- **Android/iOS 실기기·물리 글자/터치 크기·엄지 가림·노치: NOT_RUN.** 가로/세로 native 정지안은 열람했지만 방향 선택·실기기 PASS로 전용하지 않는다. 가로/세로 카메라의 다른 적 밀도도 그대로 비교 점수로 쓰지 않았다.

## 03 재검토에 필요한 제출물

제작자는 A02-01~07의 누락/계약/검사 문제와 A02-08~10의 가독성/재생 문제를 수정하고, **결함 ID→수정 파일→새 증거→검사 명령/종료코드** 표를 제공한다. 새 03 manifest·실제 PNG·움직임·타일 검사 지도·UI 조각·대표 조합을 동일 독립 QA가 다시 직접 읽고 연다. HIGH/MEDIUM 미해결이면 인계 준비 완료로 닫지 않으며, Unity 미실행은 오프라인 합격과 별도 표시한다.
