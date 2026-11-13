# 아트 인계 준비 — v3 계약 (art-concept-03)

상태: **production-draft (제작 초안) · 오프라인 인계 준비**. Unity 임포트 · 씬 · Animator · 실기기 재생은 하지 않았다. 이 문서의 어떤 값도 Unity 나 기기에서 통과(PASS)했다는 뜻이 아니다.
작업 기준: 32px 셀 · 현재 등신 · 오른쪽 그림이 원본이고 왼쪽은 flipX · 레이어 팔레트 / 픽셀 원본. 48px 로 바꾸지 않았고 휴대폰 방향은 정하지 않았다.
기존 문서와 부딪히는 값은 이 문서(v3)가 정하고, 바뀐 곳은 §13 에 모았다. `art-production-workflow.md` · `claude-design-brief.md` 의 해당 절에는 이 문서를 가리키는 짧은 메모만 붙였다.

## 0. 빠른 시작

```sh
cd art-concept-03
python3 -I -B scripts/verify_all.py --review                 # 아래 1~10 단계를 순서대로 (3 은 --catalog, 9 · 10 은 --review 일 때), 실제 종료 코드를 tests/results/verify_all.txt 에 기록
python3 -I -B scripts/verify_all.py --catalog <Bible-Survivor>/content/genesis/catalog.json   # 코어 id 까지 대조
```

`verify_all.py` 가 부르는 것 (각각 따로 돌려도 된다):

| 단계 | 명령 | 기대 종료 코드 |
| --- | --- | --- |
| 1 | `python3 -I -B scripts/build_export03.py` — `export/` 를 다시 만든다 | 0 |
| 2 | `python3 -I -B scripts/check_art03.py --quiet` — 인계 검사 | 0 |
| 3 | `… check_art03.py --catalog <catalog.json> --quiet` — 코어 id 대조 | 0 |
| 4 | `python3 -I -B tests/test_check_art03.py` — 일부러 망가뜨린 41가지를 검사기가 모두 거절하는지 (종료 1 + 해당 코드, 예외로 죽은 것은 거절로 안 셈) | 0 (망가진 것 하나라도 통과하면 1) |
| 4b | `python3 -I -B tests/checker_mutations_iter02.py` — 반복02 수정의 핵심 줄 5개를 임시 사본에서 하나씩 지우면 4 가 실패하는지 | 0 |
| 5 | `python3 -I -B scripts/regen_check.py` — 새 임시 폴더에 다시 만들어 바이트 비교 | 0 |
| 5b | `python3 -I -B scripts/build_no_pillow_check.py` — Pillow import 를 막고 다시 만들어 바이트 비교 | 0 |
| 6·7 | `python3 -I -B scripts/convert_source.py <01 또는 02 pixel-source> <out.json> --verify-png-dir <그 PNG 폴더>` | 0 |
| 8 | 변환된 옛 파일을 manifest 로 넣은 검사 — 반드시 거절 | **1** |
| 9 | `python3 -I -B scripts/build_review03.py` — 검토용 판 · GIF · APNG · `review/index.html` (인계 입력 아님) | 0 |
| 10 | `python3 -I -B scripts/anim_meta_check.py` — GIF · APNG 의 프레임 수 · 지연 · 반복 값을 계약과 비교 (`tests/results/anim_metadata.txt`) | 0 |

검사기 종료 코드: `0` 모두 통과 · `1` 하나 이상 실패 · `2` 사용법 오류.

## 1. 묶음 구성

| 경로 (art-concept-03/) | 무엇 | 인계 입력? |
| --- | --- | --- |
| `export/png/*.png` | 79개 PNG 띠 (197프레임) | 예 |
| `export/manifest_v3.json` | 에셋마다 크기 · 프레임 · fps · 피벗 · 알파 · 정렬 · 품질 그룹 · 참조 id · 해시 | 예 |
| `export/source/pixel-source_v3.json` | 모든 PNG 의 프레임 격자 (글자 하나 = 픽셀 하나). 검사기가 PNG 와 픽셀 단위로 대조 | 예 |
| `export/tiles/path-16mask_v3.json` | 흙길 이웃 규칙 · 마스크 → 프레임 · 검사 지도 | 예 |
| `export/ui/ui-pieces_v3.json` | UI 조각 9-slice 테두리 · 상태 · 글꼴 · 색 토큰 | 예 |
| `src/palette_v03.json` | 팔레트 master-v0.3 (이름 붙은 램프 + 꾸미기 색 램프) | 예 (만들 때 입력) |
| `src/player_rig_v3.json` | 플레이어 부품 23개 (손으로 고친 그림 격자) + 프레임 자세표 | 예 (만들 때 입력) |
| `src/contract_v3.json` | 손으로 쓴 기대값 (검사기는 manifest 가 아니라 이것으로 판정) | 예 |
| `scripts/` | 만들기 · 검사 · 변환 · 재생성 · 검토판 | 예 |
| `tests/test_check_art03.py`, `tests/checker_mutations_iter02.py`, `tests/results/` | 음성 입력 시험 · 검사기 변이 확인과 기록 | 예 |
| `third_party/galmuri/` | Galmuri 2.40.4 글꼴 4개 + `LICENSE.txt` (SIL OFL 1.1) | 예 (UI 글자용) |
| `requirements.txt`, `.gitignore` | 고정 버전 · 캐시 제외 | 예 |
| `review/` | 검토 판 · GIF · APNG · `index.html` · 측정값 | 아니오 (검토용) |
| `canvas/project/` | Design 캔버스 원본 사본 (아트보드 `.dc.html` + `canvas.json`) | 아니오 (검토용) |

`__pycache__/` 는 이 기계의 경로가 들어가므로 묶음에 넣지 않는다 (`.gitignore`). `verify_all.py` 는 `-B` 로 돌려 새로 만들지 않는다.

## 2. 좌표 · 단위

- PPU 32 (32 픽셀 = 1 유닛). 화면은 정수 배율로만 키운다.
- 프레임 배치: 동작 하나 = 가로 띠 하나. 프레임 i 는 x = i×cell.w … (i+1)×cell.w, 높이 전체, 여백 없음.
- 피벗 좌표: **픽셀 경계 좌표, 셀 왼쪽 아래가 (0,0), x 오른쪽 · y 위**. manifest 에는 `pivot.px` 와 Unity 용 `pivot.normalized` (= px ÷ 셀 크기) 를 함께 적었다.
- 발 기준 줄: 서 있는 캐릭터 · 적은 피벗 (16,1), 가장 아래 불투명 줄이 30 (위에서 센 줄 번호) — 검사기가 **모든 프레임마다** 확인한다.
- 방향: 그림은 오른쪽을 본다. 왼쪽은 모든 레이어의 부모 하나에 flipX. 피벗 x = 16 이라 flip 때 위치가 튀지 않는다.

## 3. manifest v3 (`export/manifest_v3.json`)

맨 위: `schema` = `bible-survivors.art-manifest`, `schemaVersion` = 3, `status`, `statusNote`, `ppu`, `pivotSpace`, `frameLayout`, `layerOrder`, `flip`, `palette` (파일 · 이름 · sha256), `inputs` (contract · rig 의 sha256), `data` (pixel-source · tiles · ui 파일과 sha256), `build` (명령 · 만든 도구 버전), `license` (§12), `assets`.

임포터가 읽는 다섯 칸은 계약 `manifestTop` 이 형식을 정하고 검사기 `TOP` 이 있음 · 형식 · 값을 본다: `ppu` 정수 32, `pivotSpace` · `frameLayout` 문자열 (계약과 글자까지 같음), `layerOrder` 목록 = 계약 `player.layers` (뒤 → 앞), `flip` 객체 `{"artFaces": "right", "faceLeft": "flipX", "applyOn": "parent", "note": …}` (반복 01 까지는 문자열이었다). 플레이어 레이어 에셋의 `layerOrder` 는 그 레이어의 `player.layers` 순번이어야 한다 (`LAYER`).

에셋 칸 (모든 에셋에 있음 — 검사기 `FIELD`):

| 칸 | 뜻 |
| --- | --- |
| `id` | 파일 이름과 같은 안정 이름 |
| `file`, `sha256`, `size` | PNG 경로 · 해시 · 전체 크기 (= cell.w × frames, cell.h) |
| `cell`, `frames`, `frameRects` | 셀 크기 · 프레임 수 · 프레임마다 [x, y, w, h] (위 왼쪽 기준) |
| `fps`, `loop` | 재생 속도 · 반복. 정지 그림은 fps 0 |
| `pivot` | `{"px": [x, y], "normalized": [u, v]}` (§2 좌표) |
| `ground` | 발이 땅에 닿는 그림인지 (true 면 발 기준 줄 검사) |
| `alpha` | `{"policy": "binary" 또는 "blend", "values": [쓰인 알파 값]}` (§6) |
| `sorting` | 정렬 레이어 제안 (§6) |
| `quality` | `core` · `optional-fx` · `terrain` · `ui` (§6) |
| `ref` | 코어 안정 id 참조 `종류:id` (§4) |
| `source` | `authoring` (만든 파일 · 함수) · `method` (손 · 절차 생성 구분) · `pixelSource` (격자 위치) |
| `status` | `production-draft` |
| 플레이어 레이어만 | `layer`, `animation`, `layerOrder`, `recolorGroups`, `keysFromRig` |
| 턱뼈 휘두르기만 | `attachPx` — 프레임마다 턱뼈 그림을 붙일 위치 (셀 위 왼쪽 기준 px) |

## 4. 안정 id 지도 (코어 ↔ 아트)

출처: Bible-Survivor `feat/adam-core` 커밋 `9758403b8a063675c917a18c0ca7b847db17dd79` 의 `content/genesis/catalog.json` (sha256 `5215376c…2c88246`) · `src/BibleSurvivor.Core/Growth.cs` (sha256 `2c9c6478…ddf91`). 2026-11-14 에 HEAD `a0206c6` 에서 두 파일의 해시가 같음을 다시 확인했다.

| 종류 | 코어 id (contract_v3 `stableIds`) | 이번 아트가 쓰는 것 |
| --- | --- | --- |
| weapon | bible, jawbone, sling, moses_staff, daniel_lion, fire_pillar, lightning, cross, wilderness_voice, elijah_chariot | `weapon:bible` (책 · 빛 · 아이콘), `weapon:jawbone` (턱뼈 · 휘두르기 · 아이콘), `weapon:sling` (돌 · 꼬리 · 아이콘) |
| passive | scroll, hair, stones, east_wind, seal, cloud, thunder, crown, underwear, whirlwind | `passive:scroll` · `passive:hair` · `passive:stones` (아이콘) |
| monster | a1 … i3 (27개) | `monster:a1` 작은 뱀 · `monster:a2` 열매 나방 · `monster:a3` 가시 마물 (+ 그림자) |

아트 쪽에만 있는 id (코어에 대응 id 가 아직 없음 — 아트가 코어 id 를 지어내지 않는다. 연결은 코어 구현 담당이 정한다):
`pickup:xp_base`, `pickup:xp_bonus`, `currency:gold`, `player:player`, `fx:monster_death_common`, `fx:hit_common`, `terrain:eden_ground`, `terrain:eden_path`, `terrain:eden_decal`, `ui:hud`, `ui:dialog` (UI 조각 11개는 모두 `ui:dialog`).

코어에는 외형 · 꾸미기 칸이 없다. 두 방법으로 확인했다: 코어 C# 전체에서 skin · hair · beard · hood · cloak · appearance · cosmetic · outfit · custom 검색 → 보조 아이템 id `"hair"` (삼손의 머리카락) 만 나옴, 그리고 `content/` 의 모든 JSON 키를 돌며 같은 낱말 → 0건. 그래서 꾸미기 키 (피부 · 머리색 · 튜닉 · 망토 · 수염 · 두건) 는 아트 쪽 `recolorGroups` · 레이어 이름으로만 있다.

`check_art03.py --catalog <catalog.json>` 은 weapon · monster · passive 참조가 실제 catalog 에 있는지도 본다 (`CATALOG`).

## 5. 에셋 표 (manifest 에서 뽑음)

| id | 셀 | 프레임 | fps | 재생 | 피벗 px | 알파 | 정렬 | 품질 | ref |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `enm_a1-small-snake_move_32` | 32×32 | 4 | 8 | 반복 | (16, 1) | binary | Actors | core | `monster:a1` |
| `enm_a2-fruit-moth_fly_32` | 32×32 | 4 | 12 | 반복 | (16, 1) | binary | Actors | core | `monster:a2` |
| `enm_a3-thorn-beast_walk_32` | 32×32 | 4 | 6 | 반복 | (16, 1) | binary | Actors | core | `monster:a3` |
| `fx_enemy-death_burst_32` | 32×32 | 4 | 12 | 한 번 | (16, 1) | binary | FX | core | `fx:monster_death_common` |
| `wpn_bible-book_spin_16` | 16×16 | 2 | 6 | 반복 | (8, 8) | binary | WeaponFX | core | `weapon:bible` |
| `fx_bible-glow_static_20` | 20×20 | 1 | 0 | — | (10, 10) | blend 0/96/176/255 | WeaponFX | optional-fx | `weapon:bible` |
| `wpn_jawbone_held_16` | 16×16 | 1 | 0 | — | (8, 8) | binary | WeaponFX | core | `weapon:jawbone` |
| `fx_jawbone-swing-edge_hit_64` | 64×64 | 4 | 20 | 한 번 | (32, 32) | binary | WeaponFX | core | `weapon:jawbone` |
| `fx_jawbone-swing-trail_hit_64` | 64×64 | 4 | 20 | 한 번 | (32, 32) | blend 0/112/176 | WeaponFX | optional-fx | `weapon:jawbone` |
| `wpn_sling-stone_spin_8` | 8×8 | 2 | 12 | 반복 | (4, 4) | binary | WeaponFX | core | `weapon:sling` |
| `fx_sling-trail_static_20x8` | 20×8 | 1 | 0 | — | (19, 4) | blend 0/112/136 | WeaponFX | optional-fx | `weapon:sling` |
| `fx_hit-spark_hit_16` | 16×16 | 3 | 15 | 한 번 | (8, 8) | binary | FX | core | `fx:hit_common` |
| `pkp_xp-gem-base_idle_8` | 8×8 | 2 | 4 | 반복 | (4, 4) | binary | Pickups | core | `pickup:xp_base` |
| `pkp_xp-gem-bonus_idle_8` | 8×8 | 2 | 4 | 반복 | (4, 4) | binary | Pickups | core | `pickup:xp_bonus` |
| `fx_shadow_static_16x6` (플레이어) · `fx_shadow-snake_static_20x4` · `fx_shadow-small_static_12x4` · `fx_shadow-wide_static_24x6` | 각 크기 | 1 | 0 | — | 셀 중앙 | blend 0/51 또는 0/89 | GroundDecal | core | 주인 id |
| `til_eden_path-16mask_static_32` | 32×32 | 16 | 0 | 마스크별 정지 프레임 | (16, 16) | binary | Ground | terrain | `terrain:eden_path` |
| `til_eden_grass-a…d`, `til_eden_meadow-a/b` (6) | 32×32 | 1 | 0 | — | (16, 16) | binary | Ground | terrain | `terrain:eden_ground` |
| `dec_eden_*` (18) | 각 크기 | 1 | 0 | — | 아래 가운데 | binary | GroundDecal | terrain | `terrain:eden_decal` |
| `ico_*` (7) | 16×16 | 1 | 0 | — | (8, 8) | binary | UI | ui | 무기 · 보조 · 재화 id |
| `chr_player_layer-<레이어>_<동작>_32` (18) | 32×32 | idle 4 · walk 4 · death 6 | 6 · 8 · 8 | 반복 · 반복 · 한 번 | (16, 1) | binary | Actors | core | `player:player` |
| `ui_*` (11) | §9 | 상태 수 | 0 | — | 셀 중앙 (내림) | binary | UI | ui | `ui:dialog` |

턱뼈 휘두르기는 판정에 쓰는 **앞날 (core, 불투명)** 과 꺼도 되는 **잔상 (optional-fx, 반투명)** 두 띠로 나눴다. 앞날은 프레임 0–2 에만 있고 프레임 3 은 일부러 비어 있다 (휘두르기가 끝나고 잔상 디더만 남는 프레임). 턱뼈 그림 자체는 `wpn_jawbone_held_16` 을 `attachPx` 위치에 따로 그린다 (64 셀 밖으로 나갈 수 있다).

## 6. 알파 · 품질 그룹 · 정렬

- 알파 `binary`: 0 또는 255 만, 알파 0 인 곳의 RGB 는 0. `blend`: 그 스프라이트가 쓰는 팔레트 항목의 알파 값만 (manifest `alpha.values`), 알파 0 인 곳 RGB 0. 검사기 `ALPHA`.
- 품질 그룹: `core` 항상 그림 · `optional-fx` "FX 낮춤" 옵션에서 꺼도 됨 (꺼도 게임이 읽혀야 함 — `review/views/view_pc_fx-low_x2.png`) · `terrain` · `ui`.
- 정렬 레이어 제안 (뒤 → 앞): Ground → GroundDecal → Pickups → Actors → WeaponFX → FX → UI. 같은 레이어 안에서는 피벗 y 로 정렬 (아래쪽이 앞). Unity 담당이 실제 이름 · 순서를 정한다.
- 색: 스프라이트 하나 16색 이하 (흙길 띠는 프레임마다), 모든 색은 팔레트에 있어야 함 — 검사기 `COLOUR`.

## 7. 플레이어 레이어 · 꾸미기

- 레이어 6개, 그리는 순서 (뒤 → 앞): `cloak_back` → `base` (피부 · 튜닉 · 허리띠 · 얼굴 · 샌들) → `hair_short` → `beard` → `hood` → `cloak_front`. 파일 이름에서는 `cloak-back` · `hair-short` · `cloak-front`.
- 레이어마다 idle · walk · death 띠 3개 = 18개. 모두 32×32 · 같은 프레임 수 · fps · 피벗 · 시간표 — 검사기 `LAYER` 가 하나라도 다르면 실패.
- 합성 예 (`player_rig_v3.json` `variants`): `a-tunic` (cloak_back 없음 · 수염 없음), `b-cloak-beard`, `c-hood`, `d-hood-cloak-beard`. **두건을 쓰면 `hair_short` 를 끈다** (눌린 머리 변형은 없다).
- 색 바꾸기: 부위마다 **4단계 키** (`recolorGroups`: skin `1234` · hair `AabB` · tunic `5678` · cloak `90oO`, 각 글자 = 팔레트 이름 하나). 실제 색은 `palette_v03.json` `recolor` 의 skin s1–s6 · hair black/darkbrown/brown/auburn/gray/white · dye linen/madder/indigo/ochre/olive/purple (두건은 망토 염료를 따른다). 런타임 팔레트 교체 · 미리 굽기 중 무엇으로 구현해도 같은 원본을 쓴다.
- 머리 · 수염과 얼굴 사이에 1px 어두운 이음선 (`outline.1`) 을 넣어 가장 어두운 피부 s6 에서도 머리 경계가 이어지지 않게 했다. 피부 6 × 머리 6 = 36조합 × 14프레임 전부를 잰 값: `review/metrics/hair_boundary_36.json`.
- 동작: idle 6fps 반복 (숨 쉬며 몸 1px 내려감 · 손 앞뒤 1px), walk 8fps 반복 (다리가 엇갈리는 4자세 · 팔은 반대로), death 8fps 한 번 (기울기 → 무너짐 → 45° → 누움 2프레임). 누운 프레임은 픽셀을 그대로 옮기는 90° 회전이라 빛 방향도 같이 돈다 (남은 문제).
- 바뀐 부품 · 자세는 `src/player_rig_v3.json` 만 고치면 된다. 만들기 단계가 레이어 띠 · 격자 · 해시를 다시 만든다.

## 8. 흙길 타일 규칙 (`export/tiles/path-16mask_v3.json`)

- 마스크 = 길인 이웃 N 1 + E 2 + S 4 + W 8. 프레임 번호 = 마스크 (0 홀로 · 1/2/4/8 끝 · 5/10 곧은 길 · 3/6/9/12 모서리 · 7/11/13/14 T · 15 네거리).
- 각 마스크 칸마다 `rectTopLeft` 와 `rectUnityBottomLeft` (띠 높이가 셀 하나라 둘이 같다).
- 대각선은 보지 않는다. 대각선으로만 닿은 칸은 끝 두 개로, 2×2 이상 넓은 길은 안쪽 모서리에 풀이 보인다 → **길은 한 칸 폭으로만** 그린다 (넓은 길이 필요하면 47장 세트가 따로 필요). 회전 · 뒤집기 없음 (빛이 왼쪽 위).
- 검사 지도 (`testMap`) 는 16마스크를 모두 쓴다. 경계 검사: 칸 테두리 픽셀이 이웃 칸이 기대하는 길 / 풀과 같은지 → 0건. 일부러 곧은 길 칸을 끝 칸으로 바꾸면 2건으로 잡힌다 (`review/metrics/path_seam_check.json`). 검사기 `TILES` 는 마스크 → 프레임 표를 본다.
- Unity RuleTile 패키지 · 씬은 만들지 않았다. 규칙은 위 표 그대로 옮기면 된다.

## 9. UI 조각 (`export/ui/ui-pieces_v3.json`)

| id | 크기 | 상태 | 9-slice L,B,R,T | 내용 칸 x,y,w,h | 쓰임 |
| --- | --- | --- | --- | --- | --- |
| `ui_panel_9slice_16` | 16×16 | default | 4,4,4,4 | — | 창 · 패널 바탕 |
| `ui_card_9slice_24` | 24×24 | normal / focus / pressed / disabled | 6,6,6,6 | — | 레벨업 · 보상 카드 (눌리면 내용 1px 아래) |
| `ui_button_9slice_16` | 16×16 | normal / focus / pressed / disabled | 4,4,4,4 | — | 글자 버튼 · 선택 버튼 |
| `ui_slot_18` | 18×18 | empty / filled / new / max | — | 1,1,16,16 | 무기 · 보조 칸, 아이콘 16×16 |
| `ui_pip_2` | 2×2 | off / on | — | — | 무기 레벨 점 |
| `ui_bar-frame_9slice_8x6` | 8×6 | default | 1,1,1,1 | 1,1,6,4 | 막대 테두리 (가운데 투명) |
| `ui_bar-xp_tile_1x4` | 1×4 | back / fill | — | — | 경험치 막대, 가로 반복 |
| `ui_bar-hp_tile_1x2` | 1×2 | back / fill | — | — | 발 밑 체력 막대 |
| `ui_swatch_12` | 12×12 | normal / selected | — | 2,2,8,8 | 꾸미기 색 칸 (내용 칸을 선택 색으로 채움) |
| `ui_arrow_6x9` | 6×9 | default | — | — | 오른쪽 화살표, 왼쪽은 flipX |
| `ui_badge-evolve_8` | 8×8 | default | — | — | 진화 표시 |

- 9-slice 순서는 Unity Sprite Editor 의 Border 순서 (L, B, R, T). 모서리 고정, 가장자리 · 가운데는 반복 (Tiled). 검사기 `UI` 는 테두리가 조각 안에 가운데를 남기는지 본다.
- 상태 규칙: focus = 키보드 · 패드 초점 또는 터치 눌림 강조, pressed = 내용 1px 아래, disabled = 글자 `text.2` · 선택 불가, new = 이번 레벨업에 새로 얻음, max = 최대 레벨.
- 글꼴 (안티에일리어싱 없음, 이 크기에서만): 제목 · 숫자 Galmuri11 Bold 16px, 카드 이름 Galmuri11 Regular 16px, 설명 Galmuri9 12px, 레벨 · 꼬리표 Galmuri7 10px. 게임 화면 글자에는 1px `outline.0` 테두리.
- `fonts` 의 role 4개 (`title` · `label` · `body` · `small`) 는 `file` · `pixelSize` · `sha256` 을 갖고, `license` 는 `{file, name, copyright}`, `source` 는 공식 배포 zip (이름 · 버전 · URL · sha256). 기대값은 계약 `uiFonts` 에 손으로 적혀 있고 검사기 `FONT` 가 role 마다 계약과 같은지, 경로가 `third_party/galmuri/` 아래의 안전한 상대 경로인지 (`..` · 절대 경로 · `\` 금지), 실제 파일 (링크 아님) 이 있고 바이트 해시가 같은지, `LICENSE.txt` 가 있고 해시가 같은지, 출처가 같은지 본다. 경로는 검사기 `--root` 기준.
- 색 토큰: `colors` (text.0–2 · panel.0–3 · hp · xp · gold · dim …).
- 화면 시안 (레벨업 3중 1 · 보스 보상 · 꾸미기 × PC · 휴대폰 가로 · 세로) 은 **조각과 글꼴만으로** 그렸다 (`review/mock/`). 구현된 UI 가 아니다. 휴대폰 안전 영역 · 방향 · 배율은 정하지 않았고, 휴대폰 후보에서는 누르는 곳을 44 게임 px 이상으로 맞췄다 (1 게임 px ≥ 약 1.09dp 일 때만 48dp · 44pt 를 만족 — 기기 배율은 미정). 얼굴은 아트에 1종뿐이라 꾸미기 시안에서 꺼진 선택지로 그렸다.

## 10. 픽셀 원본 v1 · v2 · v3 와 변환

| | v1 (01) | v2 (02) | v3 (03) |
| --- | --- | --- | --- |
| 표시 | 없음 (맨 위 `palette` 가 글자 → hex) | 맨 위 `palette` 가 master-v0.2 객체 전체 | `schema` = `bible-survivors.pixel-source`, `schemaVersion` = 3 |
| 팔레트 | 전역 한 글자 = 색 하나 | 이름 붙은 램프 (`ramp.i`) | 파일 + sha256 (`src/palette_v03.json`) |
| 스프라이트 키 | 없음 (전역 글자) | 스프라이트마다 `keys` | 스프라이트마다 `keys` (플레이어는 rig 의 키) |
| 줄 길이 | 셀 폭 | **오른쪽 공백을 자름** | 정확히 셀 폭 |
| 반투명 | — | `alpha_ok` | 에셋의 `alpha` 정책 |

`scripts/convert_source.py` 가 v1 · v2 를 v3 모양으로 바꾸고 (`status: legacy-converted`, 팔레트는 파일 안에 넣음), `--verify-png-dir` 로 원래 PNG 와 픽셀 단위로 비교한다: v1 17/17 · v2 53/53 같음. 변환된 파일은 검토 · 기록용이지 인계 입력이 아니다 — 검사기는 그것을 manifest 로 받으면 `SCHEMA` 로, 옛 v2 파일을 받으면 `LEGACY` 로 거절한다 (종료 코드 1).

## 11. 검사기 (`scripts/check_art03.py`, 표준 라이브러리만)

판정 기준은 manifest 의 자기 주장이 아니라 손으로 쓴 `src/contract_v3.json` 이다 (manifest 를 스스로 맞춰 고쳐도 계약과 다르면 실패).

| 코드 | 보는 것 |
| --- | --- |
| `MANIFEST` · `SCHEMA` · `LEGACY` | 파일 읽기 · schema/schemaVersion · 옛 pixel-source 를 manifest 로 준 경우 |
| `TOP` | manifest 맨 위 `ppu` · `pivotSpace` · `frameLayout` · `layerOrder` · `flip` 이 있고 계약 `manifestTop` 형식이며 계약값과 같음 (§3) |
| `CONTRACT` · `INPUT` · `STALE` · `DATA` · `PALETTE` | 계약 파일 · 입력 해시 · 데이터 파일 해시 · 팔레트 파일 읽기 |
| `FIELD` · `DUP` | 필수 칸 · id 중복 |
| `FILE` · `HASH` · `PNG` · `SIZE` · `RECT` | 파일 있음 · sha256 · 8비트 RGBA · 크기 = 셀 × 프레임 · 프레임 칸 |
| `FRAMES` · `FPS` · `PIVOT` · `SORT` · `QUALITY` · `REF` | 계약값과 같은지 · 피벗이 셀 안 · 정렬 · 품질 · 참조 id |
| `ALPHA` · `COLOUR` | 알파 정책 · 알파 0 아래 RGB 0 · 팔레트 안의 색 · 16색 이하 |
| `HOLD` | 움직이는 띠 (fps > 0, 2프레임 이상) 에서 바로 앞 프레임과 같은 프레임 (반복 띠는 마지막 → 처음도). 떨어진 프레임 재사용 (왕복 1 = 3) 은 허용, 플레이어 장식 레이어 (망토 · 머리 · 수염 · 두건) 는 몸이 움직이는 동안 멈춰 있어도 됨 — 계약 `frameChange` |
| `FOOT` | 땅에 서는 그림의 **모든 프레임** 가장 아래 불투명 줄 = 30, 플레이어는 base 가 30 이고 다른 레이어가 그 아래로 내려가지 않음 |
| `LAYER` | 플레이어 6레이어 × 3동작이 모두 있고 셀 · 프레임 · fps · 피벗 · 시간표 · ref 가 같음, 레이어별 `layerOrder` = 계약 순번 |
| `MISSING` | 계약에 있는 에셋이 manifest 에 없음 · 묶음 (접두어) 별 최소 개수 미달 |
| `SOURCE` | pixel-source 격자를 그리면 PNG 와 픽셀 단위로 같음 |
| `TILES` · `UI` | 마스크 표 · 9-slice 테두리 |
| `FONT` | UI 글꼴 role · 안전한 경로 · 실제 파일과 해시 · 라이선스 고지 파일과 해시 · 출처 · manifest `license.fonts` (§9) |
| `CATALOG` | (`--catalog` 일 때) 코어 catalog 에 id 가 있음 |

음성 입력 41가지 (`tests/test_check_art03.py`, 결과 `tests/results/check_negative_results.txt`). 시험은 `export/` · `src/` · `third_party/` 를 임시 폴더에 복사해 하나만 망가뜨린다.

- 기존 15가지: 프레임 줄임 (manifest 까지 맞춤) · 레이어 프레임 수 · 피벗 · 레이어 빠짐 · 반투명 픽셀 · 없는 id · 파일 없음 · 해시 · 한 프레임만 발 위로 · 팔레트 밖 색 · pixel-source 한 픽셀 · 옛 v2 파일 · 타일 마스크 칸 · 9-slice 테두리 · base idle 프레임 2 를 프레임 1 로 덮음 (PNG · pixel-source · 해시까지 맞춤).
- 반복02 A03-01 14가지 (`TOP` 13 · `LAYER` 1): ppu 48 · pivotSpace `TOP-LEFT, Y DOWN` · layerOrder 뒤집기 · frameLayout `vertical` · 다섯 칸 모두 지움 (여기까지 QA 변이 그대로) · 다섯 칸 각각 하나만 지움 · ppu `"32"` (문자열) · flip 옛 문자열 · flip `artFaces: left` · 레이어 에셋 하나의 layerOrder 1 → 0.
- 반복02 `FONT` 12가지 (QA A03-02, UI 데이터를 고친 경우 manifest 의 ui 해시까지 맞춤): title 경로 → 없는 `__qa_missing__.ttf` (QA 변이 그대로) · `..` 를 거쳐 실제 파일을 가리키는 경로 · role 밖 항목이 `..` 로 폴더 밖 실제 파일을 가리킴 · role 하나 지움 · title ↔ label 바꿈 · license 경로 · source 버전 · 글꼴 파일 지움 · 글꼴 파일 바이트 바꿈 · `LICENSE.txt` 지움 · `third_party/` 없음 · manifest `license.fonts.notice`.

→ 모두 종료 코드 1 과 해당 코드 (새 26가지는 `[TOP]` · `[LAYER]` · `[FONT]` 줄이 있어야 통과로 셈). 손대지 않은 사본은 0.

`tests/checker_mutations_iter02.py` 는 반복02 의 핵심 줄 (맨 위 검사 호출 · 레이어별 layerOrder · 글꼴 파일 있음 · 안전 경로 · 글꼴 검사 호출) 을 임시 사본에서 하나씩 지우고 음성 시험이 실패하는지 본다 — 5개 모두 잡힘 (`tests/results/checker_mutations_iter02.txt`). 반복01 검사기 파일을 그대로 넣으면 새 26가지가 모두 종료 0 으로 통과해 버림을 확인했다 (`checker_mutations_iter02_with_iter01.txt`), QA 문서의 최소 재현 코드를 글자 그대로 돌린 결과는 `tests/results/qa_iter01_repro_iter02.txt` (현재 검사기 1 · 1, 반복01 검사기 0 · 0).

`HOLD` 는 이번에 만든 옛 idle 자세표 (몸이 프레임 1 = 2, 3 = 0 으로 멈춰 있던 것) 를 그대로 다시 만들어 넣었을 때 종료 코드 1 로 거절함을 한 번 확인했다 (`tests/results/hold_old-idle_repro.txt`).

검사기가 보지 않는 것: 그림이 보기 좋은지 · 읽히는지 (색 개수 · 무늬 다양도 같은 수치를 품질 점수로 쓰지 않는다). 그건 검토 판 · 재생 · 독립 QA 가 본다.

## 12. 의존성 · 라이선스

- `requirements.txt`: Python 3.11.6 · numpy 2.4.3 (만들기) · Pillow 12.1.1 (검토 판 · GIF · APNG · 글자만). 검사기 · 음성 시험 · 재생성 비교 · 변환 · 애니메이션 메타 검사는 표준 라이브러리만 — numpy · Pillow import 를 막고 돌려도 통과함을 확인했다 (`tests/results/stdlib_only.txt`, verify_all 은 import 목록으로 확인). 만들기는 `build_no_pillow_check.py` 가 Pillow import 를 막고 다시 만들어 83개 파일이 바이트까지 같은지 매번 본다 (`tests/results/build_no_pillow.txt`). PNG 는 표준 라이브러리 쓰기 (`scripts/pngio.py`, 필터 0 · zlib 9, zlib 1.2.12) 로 써서 같은 입력이면 같은 바이트가 나온다.
- 시스템 글꼴을 쓰지 않는다. 글자는 `third_party/galmuri/` 의 Galmuri 2.40.4 (SIL OFL 1.1, Copyright 2019–2025 Lee Minseo) — `LICENSE.txt` 를 글꼴과 함께 둔다. 글꼴 4개 · `LICENSE.txt` 는 공식 배포 zip (`https://github.com/quiple/galmuri/releases/download/v2.40.4/Galmuri-v2.40.4.zip`, sha256 `c8b3d986…56e60e7`) 안의 파일과 바이트까지 같음을 2026-11-14 에 다시 확인했고, 그 해시를 계약 `uiFonts` 에 적었다. export PNG 에는 글자가 없다 (manifest `license.fonts.use` = 검토 판 · UI 시안 글자). 게임 UI 에 같은 글꼴을 넣을 때도 `LICENSE.txt` 를 함께 둔다.
- 그림 출처: manifest `license` — 모두 이 프로젝트용으로 만든 것 (플레이어 부품은 프로젝트 02 시안 레이어를 잘라 손으로 고침, 나머지는 이 폴더 스크립트가 팔레트로 그림). 외부 그림 · 따라 그리기 · 참고 그림 없음.

## 13. 기존 문서와 다른 점 (v3 가 정함)

| 기존 (`art-production-workflow.md` / `claude-design-brief.md`) | v3 | 이유 |
| --- | --- | --- |
| §4 · 브리프: 마스터 팔레트 48색 이하 | master-v0.3 은 이름 붙은 램프 123색 + 꾸미기 램프 59색. 스프라이트 하나 16색 이하는 지킴 (검사기) | 02 (111색) 부터 램프 묶음으로 커졌다. 48색 제한을 다시 맞출지는 **사용자 결정** — 이번에 그림을 줄이지 않았다 |
| §5.3-2 · 브리프: 9단계 순서 (… 얼굴 → 수염 → 머리카락 → 두건 …) | 6레이어, 몸 레이어에 피부 · 튜닉 · 허리띠 · 얼굴 포함, **머리카락 위에 수염** | 32px 에서 튜닉 · 허리띠 · 얼굴을 따로 두면 겹침만 늘었다. 수염이 머리카락 아래면 옆머리에 가려 턱선이 사라졌다 |
| §5.3-3 · 브리프: 색 램프 키 3단계 | 4단계 (`1234` · `AabB` · `5678` · `90oO`) | 02 시안이 이미 4단계로 그려짐. 어두운 피부의 경계에 4번째 단계가 필요 |
| §5.3-4 · 브리프: 두건 때 `hooded` 머리카락 변형 | 두건을 쓰면 `hair_short` 레이어를 끈다 | 32px 에서 두건이 머리를 거의 다 덮는다 |
| §6.1: 반투명은 그림자와 사이드카 `alpha_ok` 효과만 | 에셋마다 `alpha.policy` (binary/blend) + 쓰인 값 목록 | 사이드카를 manifest 로 합침 |
| §6.2: `<분류>_<대상>[_<부위-변형>]_<동작>_<셀px>` | 장식 (`dec_eden_tuft-1_11x8`) · UI 조각 (`ui_panel_9slice_16`) 은 동작 칸 대신 크기 바로 앞에 늘림 방식 (`9slice` · `tile`) 또는 생략 | 동작이 없는 정지 조각. 이름은 검사기 계약의 접두어로 묶인다 |
| §6.3: PNG 마다 사이드카 JSON | **manifest_v3.json 하나** (칸 대응: `pivot_px` → `pivot.px`, `alpha_ok` → `alpha.policy`, `recolor_keys` → `recolorGroups`, `palette` → 맨 위 `palette`, `source.canvas` → `source.authoring` + `pixelSource`, `license` → 맨 위 `license`). `hitbox_hint_px` 는 넣지 않음 (Unity 담당이 정함) | 해시 · 계약 대조를 한 곳에서 |
| §6.4 · 브리프: 픽셀 원본 v1 (`글자 → hex`) | v3 (§10), 옛 파일은 `convert_source.py` | 램프 이름 · 해시 · 정확한 줄 길이 |
| §7.1: Pivot = 사이드카 `pivot_px` / 셀 | manifest `pivot.normalized` | 같은 값, 위치만 바뀜 |
| §8.1 관문 B: 사이드카 · `Art/provenance.csv` | `check_art03.py` + manifest `license` · `source`. provenance.csv 는 Unity 프로젝트 폴더가 승인된 뒤 만든다 (§6.5 와 같음) | Unity 프로젝트 없음 |

## 14. 하지 않은 것 · 미확인

- Unity 임포트 (Sprite 설정 · 9-slice Border · Sprite Atlas · Pixel Perfect Camera) · Animator 클립 · 씬 · RuleTile: 하지 않음. Unity Editor 가 설치돼 있어도 프로젝트가 없어 임포트 PASS 를 주장하지 않는다.
- Android · iOS 실기기 화면 · 터치 · 성능: 하지 않음.
- 휴대폰 방향 · 안전 영역 · 배율: 정하지 않음 (시안의 값은 예시).
- 재생 확인 범위: `review/index.html` 을 Orca 내장 브라우저 (Chrome 150) 에서 3초씩 재서 manifest fps 와 맞음 (`review/metrics/playback_observed.json`). GIF · APNG 23쌍은 `anim_meta_check.py` 로 프레임 수 · 지연 · 반복 값만 계약과 맞춰 봤고 (GIF 는 1/100초 단위라 8fps 가 120ms = 8.33fps 로 저장된다), 하나씩 눈으로 보지는 않았다. Design 캔버스 화면은 열어 보지 않았다.
