# 아트 03 결과 — 02 독립 QA 결함 수정 (1회차)

작성: Claude Design 제작자 (Orca worker, task `task_1a4cb26347fb` · dispatch `ctx_8c79c67463aa`), 2026-11-14.
상태: **production-draft 오프라인 인계 후보 — 독립 QA 재검토 전.** Unity 임포트 · Animator · 씬 · Android/iOS 실기기 재생은 하지 않았다 (NOT_RUN). 이 보고서의 PASS 는 모두 이 Mac 에서 돈 오프라인 검사 결과다.
계약 · 사용법: [`art-production-readiness.md`](art-production-readiness.md) (v3). 묶음: `art-concept-03/`.

## 1. 요약

- 32px · 현재 등신 · 오른쪽 원본 + flipX 기준으로 플레이어 6레이어 × idle 4 / walk 4 / death 6, 적 3종 4프레임, 효과 · 무기 · 픽업 애니메이션, 흙길 16마스크, UI 조각 11종 + 9-slice 데이터, 레벨업 · 보스 보상 · 꾸미기 시안을 만들었다. export PNG 79개 · 197프레임.
- 인계 데이터: `export/manifest_v3.json` (schemaVersion 3), `export/source/pixel-source_v3.json`, `export/tiles/path-16mask_v3.json`, `export/ui/ui-pieces_v3.json`. 판정 기준은 손으로 쓴 `src/contract_v3.json`.
- `python3 -I -B scripts/verify_all.py --catalog <core catalog> --review` → **11단계 모두 기대 종료 코드, VERIFY_ALL: PASS** (`art-concept-03/tests/results/verify_all.txt`). 일부러 망가뜨린 입력 15가지는 모두 종료 코드 1이다.
- A02-01 … A02-10 (HIGH 7 · MEDIUM 3) 과 A02-11 (LOW) 에 대한 수정과 근거는 §4 표에 있다. 그림이 읽히는지는 제작자 자신의 판정이 아니라 독립 QA 재검토로 닫는다.

## 2. 볼 곳

| 무엇 | 위치 |
| --- | --- |
| 로컬 재생 페이지 (export PNG 띠를 manifest fps 로 재생, 레이어 · 색 · flipX · 한 프레임씩) | `art-concept-03/review/index.html` (file:// 로 열면 됨, 외부 요청 없음) |
| 검토 판 15장 + 1배 검사 지도 1장 · 장면 10장 · 시안 9장 · GIF/APNG 23쌍 · 측정값 6개 · 재생 페이지 화면 1장 | `art-concept-03/review/boards/`, `views/`, `mock/`, `anim/`, `metrics/` |
| 검사 기록 | `art-concept-03/tests/results/` (verify_all · 음성 입력 · 재생성 · Pillow 없이 만들기 · 변환 · 애니메이션 메타 · 표준 라이브러리 확인 · 도구 변이) |
| Design 캔버스 "Bible Survivors 아트 시안 03" (아트보드 18개: 결함 요약 · PC 장면 · 근접 · 02↔03 · 휴대폰 · 플레이어 동작/레이어/합성 · 조합 12 · 조합 바탕 · 적 · 효과 · 타일 · UI 조각 · 로컬 재생 · 시안 3종) | <private Design canvas; local only> — **비공개** (소유자만 열림. QA · 다른 사람은 소유자가 Share 해야 열 수 있음). 같은 아트보드 원본 사본: `art-concept-03/canvas/project/` |

캔버스의 그림은 위 `review/` PNG · GIF 를 그대로 올린 것이다. 캔버스 화면 렌더 자체는 제작자가 열어 확인하지 않았다 (NOT_RUN). 로컬 검토는 캔버스 없이 `review/` 만으로 된다.

## 3. 원본 01 · 02 보존

- 03 에서 가장 먼저 생긴 파일은 2026-11-13 23:27:11, 02 의 가장 늦은 파일은 같은 날 23:19:39. `find art-concept-01 art-concept-02 -type f -newer <03 첫 파일>` → **0건** (2026-11-14 다시 확인).
- 03 은 01/02 를 읽기만 한다: `convert_source.py` (01 · 02 pixel-source 와 PNG 대조), `build_review03.py` (02 A3 · 02 장면 비교 판), 음성 시험 `legacy_v2` (02 파일을 **임시 사본**으로 복사). 만들기 (`build_export03.py`) 는 01/02 를 읽지 않는다 — `regen_check.py` 가 `src/ scripts/ third_party/` 만 임시 폴더에 복사해 다시 만들어 83/83 바이트가 같다.
- 01 · 02 의 Design 캔버스에는 03 작업 동안 게시하지 않았다 (03 게시는 새 캔버스 VJxmGfDq9mCV15Wn1P5QGw 에만).

## 4. 결함 → 수정 파일 → 새 증거 → 명령 / 종료 코드

경로는 `art-concept-03/` 기준. "verify_all N" 은 `tests/results/verify_all.txt` 의 N번째 단계 (1 만들기 · 2 검사기 · 3 검사기+catalog · 4 음성 입력 · 5 재생성 · 5b Pillow 없이 만들기 · 6/7 v1/v2 변환 · 8 변환 파일 거절 · 9 검토 판 · 10 애니메이션 메타).

| 결함 | 수정한 파일 | 새 증거 | 명령 → 종료 코드 |
| --- | --- | --- | --- |
| **A02-01** HIGH 플레이어 레이어가 1프레임뿐 | `src/player_rig_v3.json` (부품 23개 · idle/walk/death 자세표), `scripts/player03.py`, `scripts/build_export03.py`, `src/contract_v3.json` (player 시간표 · `frameChange`) → `export/png/chr_player_layer-<6레이어>_<idle·walk·death>_32.png` 18개 | 6레이어 모두 idle 4 (6fps 반복) · walk 4 (8fps 반복) · death 6 (8fps 한 번), 셀 32×32 · 피벗 (16,1) · 같은 시간표. walk 는 다리가 엇갈리는 4자세 + 팔 반대. `review/boards/board_player_layers_x3.png` (레이어별 전 프레임), `board_player_variants_x3.png` ("레이어 합성 4종 × 3동작 — 오른쪽 원본 뒤에 같은 프레임의 flipX"), `review/anim/player_<a-tunic·b-cloak-beard·c-hood·d-hood-cloak-beard>_<idle·walk·death>` 12쌍 + `player_s6-brown_walk_flipx`, `review/index.html` | verify_all 2 → 0 · 4 → 0 (그 안의 `layer_frames` · `layer_missing` · `hold_frame` 각각 1) · `tests/results/hold_old-idle_repro.txt` → 1 (기대 1) · verify_all 10 → 0 |
| **A02-02** HIGH 효과 · 무기 · 픽업 프레임 부족, 사망 효과 없음 | `scripts/fx03.py`, `scripts/build_export03.py`, `src/contract_v3.json` → `fx_enemy-death_burst_32`, `fx_jawbone-swing-edge_hit_64` · `fx_jawbone-swing-trail_hit_64`, `wpn_sling-stone_spin_8`, `fx_hit-spark_hit_16`, `pkp_xp-gem-base/bonus_idle_8`, `wpn_bible-book_spin_16` | 사망 4/12fps/한 번 · 턱뼈 4/20fps/한 번 (앞날 불투명 core + 잔상 반투명 optional-fx, 회전 중심 (32,32), 프레임마다 턱뼈 붙일 위치 `attachPx`, 오른쪽 기준 · 왼쪽은 flipX) · 돌 2/12fps/반복 · 명중 3/15fps/한 번 · XP 각 2/4fps/반복 · 성경 2/6fps/반복. 효과 id ↔ 무기 id 는 manifest `ref` (`weapon:jawbone` 등, readiness §4 · §5). `review/boards/board_fx_x3.png`, `board_jawbone-swing_x4.png`, `review/anim/` 7쌍 | verify_all 2 → 0 (CONTRACT · FPS · ALPHA) · 4 안의 `frames_short` (manifest 까지 맞춘 1프레임) → 1 · `alpha_bad` → 1 · verify_all 10 → 0 |
| **A02-03** HIGH 흙길이 장면에 직접 칠해짐, 타일 · 규칙 없음 | `scripts/tiles03.py`, `scripts/build_export03.py` → `export/png/til_eden_path-16mask_static_32.png` (16프레임), `export/tiles/path-16mask_v3.json` | 비트 N1 E2 S4 W8, 마스크 = 프레임 번호, 칸마다 `rectTopLeft` · `rectUnityBottomLeft`, 대각선 미지원 (한 칸 폭 길만, 넓은 길은 47장 세트 필요), 회전 · 뒤집기 없음. `review/boards/board_path-16mask_x3.png`, `board_path-testmap_x2.png` + 1배 `path-testmap_native.png`, `review/metrics/path_seam_check.json` (16마스크를 모두 쓴 검사 지도 경계 문제 0 · 미지원 예시 지도 0 · 일부러 틀린 칸 하나 넣으면 2) | verify_all 2 → 0 (TILES) · 4 안의 `tile_mask_bad` → 1 |
| **A02-04** HIGH HUD 통짜 이미지, 9-slice · 시안 없음 | `scripts/ui03.py`, `scripts/build_export03.py`, `scripts/build_review03.py` (시안), `third_party/galmuri/` → `export/png/ui_*.png` 11개, `export/ui/ui-pieces_v3.json`, 아이콘 `ico_*` 7개 | 조각별 9-slice L,B,R,T · 내용 칸 · 상태 (normal/focus/pressed/disabled · empty/filled/new/max …) · 글꼴 파일/크기 · 색 토큰 (readiness §9). `review/boards/board_ui-pieces_x4.png` (조각 + 늘림 예), `review/mock/mock_<levelup·reward·custom>_<pc·phone-l·phone-p>_x2.png` 9장 — 같은 조각과 글꼴만으로 구성, 예시 안전 영역 · 누를 곳 표시, `review/metrics/touch_targets.json` (휴대폰 후보의 누를 곳 모두 ≥ 44 game px, PC 는 마우스라 해당 없음). 구현된 UI 가 아니고 휴대폰 방향 · 배율은 미정 | verify_all 2 → 0 (UI) · 4 안의 `nine_slice_bad` → 1 · verify_all 9 → 0 |
| **A02-05** HIGH manifest 칸 · stable ID 대응 없음 | `scripts/build_export03.py` → `export/manifest_v3.json`, `src/contract_v3.json` (`stableIds` + 출처 커밋 · 파일 해시), `art-production-readiness.md` §2–§6 | manifest 맨 위 schema/schemaVersion · PPU · pivotSpace · frameLayout · layerOrder · flip · palette/inputs/data 해시 · build · license, 에셋마다 file/sha256/size/cell/frames/frameRects/fps/loop/pivot(px+normalized)/ground/alpha/sorting/quality/ref/source/status. 코어 대응: Bible-Survivor `feat/adam-core` `9758403b` 의 catalog.json · Growth.cs 에서 읽음 (HEAD `a0206c6` 에서 두 파일 해시 같음), 코어에 없는 아트 id 는 `artOnly` 로 분리. 32px 만 계약 · 묶음에 있다 (48px 는 만들지 않음) | verify_all 2 → 0 · 3 (`--catalog`) → 0 · 4 안의 `ref_bad` · `file_missing` · `hash_bad` · `pivot_bad` → 각각 1 |
| **A02-06** HIGH 검사기가 잘못된 입력을 통과시킴 | `scripts/check_art03.py` (계약 기준 판정 · 프레임마다 FOOT · LAYER · HOLD …), `src/contract_v3.json`, `tests/test_check_art03.py` | `tests/results/check_negative_results.txt` / `.json`: 손대지 않은 사본 0, 망가뜨린 15가지 모두 1 — QA 가 든 뱀 4→1 (`frames_short` → CONTRACT), 나방 피벗 [999,-3] (`pivot_bad` → PIVOT), 두건 레이어 삭제 (`layer_missing` → LAYER), 한 프레임만 발 위로 (`foot_bad` → FOOT frame 2) 포함. `hold_old-idle_repro.txt`: 이번 회차에 실제로 있었던 idle 정지 프레임 구조를 다시 만들어 넣으면 HOLD 로 1 | `python3 -I -B tests/test_check_art03.py` → 0 (= 15건 모두 거절, 하나라도 통과하면 1) · 검사기 직접 실행 시 실패 1 · 사용법 오류 2 |
| **A02-07** HIGH 규격 충돌 · 변환기 없음 · 시스템 글꼴 · 재생성 미검증 | `scripts/convert_source.py`, `scripts/pngio.py`, `scripts/px03.py` (Pillow 를 쓸 때만 import), `scripts/regen_check.py`, `scripts/build_no_pillow_check.py`, `requirements.txt`, `export/source/pixel-source_v3.json`, `third_party/galmuri/` (시스템 글꼴 대신, OFL 고지 포함) | v1/v2/v3 차이표와 변환 (readiness §10, 충돌은 §13). `tests/results/regen_results.txt` (새 임시 폴더 83/83 바이트 같음), `build_no_pillow.txt` (Pillow import 를 막고 83/83 같음), `convert_results.txt` (v1 17/17 · v2 53/53 픽셀 같음), `stdlib_only.txt` (검사기 · 시험 · 변환은 numpy · Pillow 없이 0). 48색으로 줄이거나 48px 로 바꾸지 않음 | verify_all 5 → 0 · 5b → 0 · 6 → 0 · 7 → 0 · 8 (변환 파일을 manifest 로) → 1 (기대 1, SCHEMA) · 4 안의 `legacy_v2` → 1 (LEGACY) |
| **A02-08** MEDIUM 어두운 피부 × 갈색 머리 경계 | `src/player_rig_v3.json` (머리 · 수염과 얼굴 사이 1px `outline.1` 이음선) | `review/metrics/hair_boundary_36.json`: 피부 6 × 머리 6 = **36조합 전부**, 조합마다 14프레임 (idle 4 + walk 4 + death 6) 중 최악 프레임 기준, 약한 경계 (CIE76 ΔE < 12) 비율 > 0.10 인 조합 02: 6 → 03: 0 (가장 나빴던 s6 × brown 0.615 → 0.053). `review/boards/board_skin-hair-36_x4.png`, `board_player_combos-12_x3.png` (대표 12조합 × 14프레임), `board_player_combos-12_backgrounds_1x.png` · `_x2.png` (잔디 · 흙길 · 그늘 × 컬러 · 흑백, 1배 · 2배, 같은 조합이 세 동작 전 프레임에 유지). 수치는 경계 수정의 근거일 뿐 품질 점수가 아니다 | verify_all 9 → 0 (측정 · 판 생성). 읽히는지는 QA 판정 |
| **A02-09** MEDIUM A3 가 바닥에 묻힘 · 근접 겹침 미검증 | `scripts/enemies03.py` (A3 자두색 몸 + 밝은 테두리 · 가시), `scripts/build_review03.py` (장면: 적 40 · 근접 · 그늘) | `review/boards/board_a3_02-vs-03_x4.png` ("A3 02 (왼쪽 4칸) → 03 (오른쪽 4칸): 잔디 · 흙길 · 그늘, 아래 3줄은 흑백"), `board_enemies_x4.png` (적 3종 × 잔디 · 흙길 · 그늘 × 컬러 · 흑백), `review/views/view_pc_x2.png` · `view_pc_gray_x2.png` · `view_pc_fx-low_x2.png` · `view_pc_fx-low_gray_x2.png` · 1배 `view_pc_native.png` · `view_pc_fx-low_native.png` · 휴대폰 가로/세로 (+ FX 낮춤), `board_close-quarters_x4.png` (플레이어 64px 안 적 7 + 턱뼈 · 명중 · 사망 효과 겹침), `review/metrics/scene_counts.json` (같은 PC 카메라 안 살아 있는 적 40 = a1 24 · a2 11 · a3 5, 그중 그늘 속 A3 1, 64px 안 7, 사망 효과 1 은 40 과 별도) | verify_all 9 → 0. 평균 밝기 수치를 합격 근거로 쓰지 않았다 |
| **A02-10** MEDIUM 재생 파일 없음 | `scripts/build_review03.py` (GIF · APNG · `review/index.html`), `scripts/anim_meta_check.py` | `review/anim/` GIF 23 + APNG 23 (플레이어 합성 4 × 3동작 · flipX 걷기 · 적 3 · 효과/무기/픽업 7), `review/index.html`, `review/metrics/playback_observed.json` (Orca 내장 브라우저 Chrome 150 에서 file:// 로 열고 '다시 재생' 뒤 3초 동안 프레임 카운터를 5ms 마다 읽음: 14개 모두 manifest fps · 한 번/반복과 맞음), `review/screens/index_orca-browser.png`, `tests/results/anim_metadata.txt` (23쌍의 프레임 수 · 지연 · 반복 값 = 계약; GIF 는 1/100초 단위라 8fps 가 120ms = 8.33fps) | verify_all 9 → 0 · 10 → 0. 브라우저 측정은 수동 1회 (재실행 스크립트 없음). GIF · APNG 를 하나씩 눈으로 본 것은 아님 |
| **A02-11** LOW 조합 범위 표기 | `scripts/build_review03.py` (조합 12개 + 고른 이유), `review/metrics/combos_12.json` | 판 제목 "대표 조합 12개 (전체 조합 아님) × 14프레임 …— 고른 이유는 오른쪽", 행마다 이유 (s6 × 머리 6색 · 02 에서 약했던 조합 · 두건/망토/수염 가림 최대 · 밝은 피부 × 검은/흰 머리 반대 극단 · 중간 피부 기본색). `combos_12.json` scope = "representative sample, not all combinations", 포함 범위 (피부 s1·s3·s5·s6, 머리 6, 튜닉 6, 망토 6, 변형 4). 전체 곱은 피부 × 머리 36 만 경계 수치로 확인 (위 A02-08) | verify_all 9 → 0 |

## 5. 하네스 기록 — 이번 회차 반복 · GC 근거

`art-readiness-reviews/**` 는 QA 소유라 `iteration-01-gc.md` 를 직접 쓰지 않았다. 아래 5.1–5.4 를 그 파일의 원본으로 쓰면 된다.

### 5.1 반복 기록 (발생 순)

| # | 무엇이 틀렸나 | 어떻게 드러났나 | 고친 것 | 확인 |
| --- | --- | --- | --- | --- |
| 1 | 검토 판 만들기가 UI 조각 표를 비운 채 읽어 KeyError | 실행 오류 | `ui.build()` 뒤 export 프레임 · 테두리로 덮어씀 | review build 0 |
| 2 | 휴대폰 꾸미기 시안이 화면 밖으로 넘치고 색 칸 40px < 44 | 시안을 직접 봄 + `touch_targets.json` | 시안마다 고정 좌표로 다시 배치 | 30/30 ≥ 44 |
| 3 | 보상 시안 휴대폰 확인 버튼 30px, 세로 시안 줄 생략 | 시안 확인 | 120×44 · 모든 줄 표시 | `touch_targets.json` |
| 4 | UI 판 글자 겹침, 늘린 막대 테두리가 안 보임 | 판 확인 | 조각마다 한 줄, 잔디 바탕 | 판 다시 생성 |
| 5 | 턱뼈 판에서 턱뼈가 잘림, 효과 판 빈 공간 | 판 확인 | 80×72 칸, 줄마다 높이 | 판 다시 생성 |
| 6 | 조합 바탕 판이 2프레임뿐 · 줄 이름 잘림 | 판 확인 | 12조합 × 14프레임 판 2장, 이름 칸 320 | 판 다시 생성 |
| 7 | **idle 에서 몸 (base) 이 프레임 1=2, 3=0 으로 멈춤** — 4프레임이지만 실제 자세 2개. 검사기는 통과시킴 | 프레임 고유 수 측정 | idle 자세표 변경 (몸 1px · 손 ±1 · 망토 끝) + 검사기 `HOLD` 규칙 (아래 5.2) | 옛 표 재현 → 1, 새 표 → 0 |
| 8 | 종료 코드를 파이프 뒤 `tail` 의 것으로 기록 | 기록 검토 | `verify_all.py` 가 단계마다 실제 종료 코드를 씀 | verify_all.txt |
| 9 | verify 기록에 개인 절대 경로 | 기록 검토 | `<tmp>` · `<art03>` · `..` · `<catalog>` 로 가림, `-B` | 바이트 검색 0건 (§7) |
| 10 | `__pycache__` 에 이 기계 경로 | 파일 검토 | `.gitignore` + `-B` (지우지는 않음 — 아래 §9) | — |
| 11 | **"만들기는 NumPy 만 필요" 라고 썼는데 `px03.py` 가 맨 위에서 Pillow 를 import** | 의존성 주장을 실행으로 확인 | Pillow import 를 쓸 때만, `build_no_pillow_check.py` 를 verify_all 에 넣음 | 5b → 0, 옛 구조 변이 → 1 |
| 12 | 검토 폴더에 이전 판 2장이 남음 (제작자가 만든 것) | 목록 대조 | 그 2장만 삭제 | review 89개 |
| 13 | readiness 문서가 코드와 다름: 검사 코드 `PALETTE` 빠짐, `MISSING` 뜻 틀림, UI 조각 ref 를 "—" 로 씀 | 보고 전에 코드 · manifest 와 대조 | 문서 정정 | 문서 §5 · §11 |
| 14 | GIF/APNG 를 손으로만 메타데이터 확인 | 자기 점검 | `anim_meta_check.py` (계약과 비교) 를 verify_all 에 넣음 | 10 → 0, 반복 플래그 변이 → 1 |
| 15 | 캔버스 LocalReview 그림 설명이 실제 캡처와 다름 | 캡처를 직접 봄 | 설명을 사실대로 고쳐 다시 게시 | 캔버스 버전 21 |
| 16 | 03 모듈 5개 첫 설명 줄이 "art draft 02" | 설명 대조 | 문구만 수정 (코드 변화 없음) | verify_all 0 · 재생성 83/83 |
| 17 | zsh 에서 `$cmd` 가 단어로 안 나뉘어 확인 명령 3개가 잘못 돎 | 출력의 FileNotFoundError | `${=cmd}` 로 다시 실행 | `stdlib_only.txt` |
| 18 | A1/A2/A3 수가 계획 24/12/4 → 24/11/5 (근접 배치가 종류를 바꿈) | `scene_counts.json` | 계획 값을 고치지 않고 실제 값 기록 | §9 |

### 5.2 검사기가 놓친 것 → 재발 방지 도구 (모두 이번 회차에 반영)

| 놓친 것 | 도구 | 이빨이 있나 (옛 구조로 실패를 봄) |
| --- | --- | --- |
| 움직이는 띠에서 같은 프레임이 이어지는 것 (프레임 수만 보고 변화는 안 봄) | `src/contract_v3.json` `frameChange` + `check_art03.py` `HOLD` (반복 띠는 마지막→처음 포함, 떨어진 재사용과 플레이어 장식 레이어 정지는 허용) + 음성 입력 `hold_frame` | 이번 회차의 옛 idle 자세표를 그대로 다시 만들어 넣음 → `[HOLD] … frame 2 repeats frame 1` · `frame 0 repeats frame 3`, 종료 1 (`hold_old-idle_repro.txt`) |
| "만들기는 NumPy 만" 이라는 의존성 주장 | `scripts/build_no_pillow_check.py` (Pillow import 를 막고 다시 만들어 바이트 비교), verify_all 5b | `px03.py` 에 옛 맨 위 import 를 되살린 사본 → ImportError, 종료 1 (`tool_mutations.txt`) |
| 검토 애니메이션의 프레임 수 · 속도 · 반복 값 | `scripts/anim_meta_check.py` (손으로 쓴 계약과 비교), verify_all 10 | 한 번 재생할 death GIF 를 반복으로 다시 저장한 사본 → 종료 1 (`tool_mutations.txt`) |
| 종료 코드 · 경로가 섞인 기록 | `scripts/verify_all.py` (실제 종료 코드, 경로 가림, `-B`) | 경로 바이트 검색 230파일 0건 |

### 5.3 전역 GC 후보 (적용하지 않음 — "전역 GC 자동 apply 금지")

coordinator 가 골라 `/harness-gc` 로 기록할 수 있게 후보만 적는다.

1. 판단: 생성 스크립트 · 문서에 "X 만 필요 / 표준 라이브러리만" 같은 의존성 주장을 쓰기 전에, 그 패키지를 막고 실제로 돌린다 (위 #11). 기존 학습 규칙 "환경 사실 · 영향 단정은 확인한 뒤 쓴다" 의 재발로 볼 수 있다.
2. 판단: 애니메이션 계약을 프레임 **수**로만 검사하면 멈춘 프레임을 놓친다 → 프레임 **변화**도 검사 항목에 넣는다 (#7).
3. 도구 후보: zsh 에서 변수에 담은 명령을 `$cmd` 로 실행하면 단어로 안 나뉜다 → Bash 가드가 `for … in "…"; do … $var` 꼴을 경고 (#17).
4. 판단: 보고 직전에 내가 쓴 계약 문서의 표를 코드 · manifest 에서 다시 뽑아 대조한다 (#13). 기존 규칙 재발.

### 5.4 모델 · 사용량

모델: Claude Opus 5.5 (`claude-opus-5-5`, 이 세션 시스템 정보). 토큰 · 비용 사용량: **미측정** (세션이 수치를 제공하지 않음). QA 는 기존 독립 QA 한 명 (`ctx_200651da76af`) 을 유지하며 제작자는 서브 에이전트를 쓰지 않았다.

## 6. 누가 그렸나 · 어디서 확인했나

| 구분 | 무엇 | 근거 |
| --- | --- | --- |
| 손으로 고친 그림 | 플레이어 부품 23개 (02 시안 레이어에서 잘라 rig 격자에서 손으로 고침, 자세표로 배치) · 성경책 · XP 보석 (손 격자) · 아이콘 일부 | manifest `source.method` (`hand-placed parts …`, `hand grid`, `procedural/hand`) |
| 절차 생성 (스크립트가 팔레트 램프로 그림, 결정적) | 적 3종 · 효과 · 돌/턱뼈 · 바닥 · 장식 18 · 흙길 16 · UI 조각 11 | manifest `source.method` (`procedural generator …`, `procedural pixel boxes`) |
| Design 캔버스 | 위 PNG · GIF 를 올려 배치한 아트보드 18개. 캔버스에서 그린 그림은 없음. 캔버스 렌더는 열어 보지 않음 (NOT_RUN) | `art-concept-03/canvas/project/*.dc.html` |
| 브라우저 재생 | `review/index.html` 을 Orca 내장 브라우저에서 측정 (타이머 재생) | `playback_observed.json` |
| 엔진 · 기기 | 없음. Unity Editor 6000.6.9f1 은 설치돼 있으나 프로젝트가 없어 임포트하지 않음 | — |

## 7. 검증 상태

| 항목 | 상태 | 근거 |
| --- | --- | --- |
| 만들기 · 검사기 · catalog · 음성 15 · 재생성 · Pillow 없이 · 변환 v1/v2 · 변환 파일 거절 · 검토 판 · 애니메이션 메타 | **PASS** (11단계 모두 기대 종료 코드) | `tests/results/verify_all.txt` (python 3.11.6 · zlib 1.2.12 · numpy 2.4.3 · Pillow 12.1.1) |
| 표준 라이브러리 전용 주장 | PASS (numpy · Pillow import 를 막고 0, 만들기만 1) | `tests/results/stdlib_only.txt` |
| 개인 경로 · 이메일 · 비밀값 | 0건 — 230파일 바이트 검색 (HTML 안 base64 이미지 풀어서 포함) + 텍스트 검색. PNG 에 글자 청크 없음 (화면 캡처 1장에 일반 색 프로필 iCCP 만) | 이 보고서 작성 시 실행 |
| 브라우저 재생 속도 | 측정함 14/14 (수동 1회) | `review/metrics/playback_observed.json` |
| GIF · APNG 를 눈으로 하나씩 | 하지 않음 (메타데이터만 자동 비교) | `anim_metadata.txt` |
| 그림이 읽히는지 (적 · 플레이어 · XP · 공격 구분, A3, 머리 경계) | 제작자 판정만 — **독립 QA 재검토 대기** | `review/` 판 · 장면 |
| Design 캔버스 렌더 | NOT_RUN | — |
| Unity 임포트 · Animator · RuleTile · 9-slice Border 설정 | NOT_RUN | — |
| Android · iOS 실기기 화면 · 터치 · 성능 | NOT_RUN | — |

## 8. 남은 문제 (알고 있는 약점)

- 누운 death 프레임 (5 · 6) 은 픽셀을 그대로 90° 돌려서 빛 방향도 같이 돈다.
- 홀로 있는 흙길 칸 (마스크 0) 이 작아 보인다. 넓은 길 · 대각선은 이 세트로 안 된다 (규칙에 명시).
- 머리카락 (삼손) 아이콘이 다른 아이콘보다 약하다.
- A3 는 짙은 그늘 + 흑백에서 몸 밝기보다 테두리 · 가시에 기대어 읽힌다.
- 팔레트가 48색 제안보다 크다 (§9). 줄일지는 사용자 결정.
- 턱뼈 앞날 띠의 프레임 3 은 일부러 비어 있다 (잔상만 남는 프레임). 판정 프레임을 몇 개로 볼지는 코어 구현 담당이 정한다.

## 9. 계획과 다르게 한 것

- **A02-11 범위 표기:** QA 수용 조건 문구는 "18개 대표 조합" 이었으나 03 은 **12개 대표 조합** 을 보이고 "대표 조합 12개 (전체 조합 아님)" 으로 적었다. 12개는 어두운 피부 × 머리 6색, 가림 최대 (두건 · 망토 · 수염), 반대 극단 (밝은 피부 × 검은/흰 머리), 중간 피부로 골랐고 이유를 행마다 적었다. "전체를 검사했다" 는 말은 피부 × 머리 36조합 경계 수치에만 썼다.
- **팔레트 48색:** workflow §4 · 브리프의 "마스터 48색 이하" 를 지키지 않았다 (master-v0.3 = 램프 123색 + 꾸미기 59색; 스프라이트당 16색 이하는 검사). task 문서대로 줄이지 않고 v3 에 충돌로 적었다 — **사용자 결정 필요**.
- **레이어 순서 · 키:** 9단계 → 6레이어 (수염이 머리카락 위), 3단계 램프 키 → 4단계, 두건용 머리 변형 → 두건 때 머리 레이어 끔 (readiness §13).
- **사이드카 JSON → manifest 하나**, 장식 · UI 조각 이름은 동작 칸 없음 (readiness §13).
- **idle 자세표 변경:** 처음 만든 03 idle 표는 몸 (base) 이 두 자세뿐이라 (프레임 1=2, 3=0) 손 ±1 · 몸 1px 로 바꿨다 (02 의 idle 은 1프레임뿐이었다).
- **적 종류 수:** 장면 계획 a1 24 · a2 12 · a3 4 → 실제 24 · 11 · 5 (근접 배치로 옮긴 적의 종류). 총 40 은 같다.
- **검사 추가:** 원래 계획에 없던 `HOLD` 규칙 · `build_no_pillow_check.py` · `anim_meta_check.py` 를 이번 회차에 넣었다 (§5.2). 계약 파일이 바뀌어 manifest `inputs.contract.sha256` 이 `4406f4ea…` 로 바뀌었다.
- **iteration GC 위치:** `art-readiness-reviews/iteration-01-gc.md` 대신 이 보고서 §5 에 적었다 (그 폴더는 QA 소유).
- **정리하지 않은 것:** `scripts/__pycache__/` (이 기계 경로 포함, `.gitignore` 로 제외, 삭제 명령은 가드가 막고 다시 생기는 캐시라 둠), `art-concept-03/out/` (작업 시작 때 생긴 빈 폴더, 파일 0개 — git 에 안 올라감).

## 10. coordinator 인계 목록

공개 저장소 후보 (경로 · 비밀값 0건 확인):

- `art-concept-03/export/**` (PNG 79 · manifest · pixel-source · tiles · ui)
- `art-concept-03/src/**`, `scripts/*.py` (18개, `__pycache__` 제외), `tests/test_check_art03.py`
- `art-concept-03/third_party/galmuri/**` (글꼴 4 + `LICENSE.txt`, SIL OFL 1.1)
- `art-concept-03/requirements.txt`, `.gitignore`
- `art-concept-03/tests/results/**` (경로를 가린 검사 기록 10개 — 실행 로그를 저장소에 둘지는 coordinator 판단)
- `art-concept-03/review/**` (검토용 3.9 MB, 인계 입력 아님 — 넣을지는 coordinator 판단)
- 문서: `art-production-readiness.md`, `art-revision-03-result.md`, `art-production-workflow.md` · `claude-design-brief.md` (v3 충돌 메모만 추가)

로컬에만 두기를 권함:

- `art-concept-03/canvas/project/**` — 비공개 Design 캔버스의 `/_blob/<id>` 참조가 들어 있다 (그림 자체는 `review/` 에 같은 파일이 있음).
- `art-concept-03/scripts/__pycache__/`, 빈 `art-concept-03/out/`.

하지 않은 것: git commit · push, 공개 업로드, 전역 GC apply, 01/02 · QA 보고서 · 코어 저장소 수정.

## 11. QA 재현 명령

```sh
cd art-concept-03
python3 -I -B scripts/verify_all.py --catalog <Bible-Survivor>/content/genesis/catalog.json --review   # 11단계, 0 이면 모두 기대대로
python3 -I -B scripts/check_art03.py --quiet; echo $?                                                 # 0
python3 -I -B tests/test_check_art03.py; echo $?                                                      # 0 (15건 모두 1 로 거절됨)
open review/index.html                                                                                # 로컬 재생
```

검사기를 망가진 입력에 직접 돌려 보려면 `tests/test_check_art03.py` 의 함수 하나 (예: `pivot_bad`) 를 임시 사본에 적용한 뒤 `python3 -I scripts/check_art03.py --root <사본> --quiet` → 1.
