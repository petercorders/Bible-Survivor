# 아트 03 반복 02 결과 — QA A03-01 · A03-02 검사기 수정

작성: 아트 제작 담당 (Orca worker, task `task_edab8eb0a055`), 2026-11-14. 상태: **독립 QA 재검토 대기.** 그림 (PNG 79개 · 197프레임) 은 다시 그리지 않았다. 고친 것은 `scripts/` · `src/contract_v3.json` · `tests/` · 다시 만든 메타데이터 (`export/manifest_v3.json`, `export/ui/ui-pieces_v3.json`) 와 결과 문서다. QA 보고 (`art-readiness-reviews/**`) 는 읽기만 했다. Unity 임포트 · 기기 확인 · 커밋 · push 는 하지 않았다.

## 1. 요약

| ID | QA 판정 (반복 01) | 이번 수정 | 같은 변이의 종료 코드 (반복 01 → 02) |
| --- | --- | --- | --- |
| A03-01 HIGH | manifest 맨 위 `ppu` 48 · `pivotSpace` 반대 · `layerOrder` 뒤집기 · `frameLayout` vertical · 다섯 칸 삭제가 모두 종료 0 | 계약 `manifestTop` (형식) + `flip` 객체 추가, 검사기 `check_top` (`TOP`) 과 레이어별 `layerOrder` (`LAYER`) | 0 → **1** (QA 변이 5개 그대로 + 단독 삭제 5 + 형식 · 값 4) |
| A03-02 MEDIUM | `fonts.title.file` 을 없는 파일로 바꾸고 ui 해시를 맞춰도 종료 0, 글꼴 없이 복사한 사본도 0 | 계약 `uiFonts` (role · 파일 · 크기 · sha256 · 라이선스 · 출처) 추가, 검사기 `check_fonts` (`FONT`), 시험 사본에 `third_party/` 포함 | 0 → **1** (QA 변이 그대로 + 11가지) |

QA 문서의 최소 재현 코드를 글자 그대로 뽑아 돌렸다: 현재 검사기 `ppu exit= 1` · `font ref exit= 1`, 같은 코드에서 검사기 경로만 반복 01 파일 사본으로 바꾸면 `0` · `0` (`art-concept-03/tests/results/qa_iter01_repro_iter02.txt`).

## 2. 결함 → 근거 → 수정 파일 → 증거

### A03-01 — 맨 위 인계 메타데이터

- 원인: 검사기가 에셋 단위 칸 (`REQ`) 과 계약 에셋 표만 보고, 임포터가 읽는 manifest 맨 위 다섯 칸은 아무 데서도 읽지 않았다. 반복 01 의 음성 시험 15가지도 에셋 · 데이터 파일만 망가뜨려서 이 구멍을 드러내지 못했다.
- 수정:
  - `src/contract_v3.json`: `flip` 을 객체로 (`{"artFaces": "right", "faceLeft": "flipX", "applyOn": "parent", "note": …}`), `manifestTop.types` = `ppu` int · `pivotSpace` str · `frameLayout` str · `layerOrder` list · `flip` object. `ppu` · `pivotSpace` · `frameLayout` 기대값은 원래 계약에 있던 값, `layerOrder` 기대값은 계약 `player.layers`.
  - `scripts/check_art03.py` `check_top`: 다섯 칸 이름은 코드에 고정 (계약에서 칸을 지워도 검사가 약해지지 않고 `CONTRACT` 실패), 칸마다 있음 → 형식 (`ppu` 는 `bool` 을 정수로 치지 않음) → 계약값과 같음. `check_player`: 레이어 에셋의 `layerOrder` 가 정수이고 `player.layers` 순번과 같음.
  - `scripts/build_export03.py`: manifest `flip` 을 계약에서 가져온다 (`ppu` 등과 같은 방식). 반복 01 의 문자열 `"art faces right; left = flipX on the parent"` 는 이제 `TOP` 형식 오류다.
- 시험 (`tests/test_check_art03.py`, 14가지): `top_ppu48` · `top_pivotspace_topleft` (`TOP-LEFT, Y DOWN`) · `top_layerorder_reversed` · `top_framelayout_vertical` · `top_all_deleted` — 여기까지 QA 변이 그대로. `top_missing_{ppu, pivotSpace, frameLayout, layerOrder, flip}` 단독 삭제 5, `top_ppu_string` (`"32"`), `top_flip_old_string`, `top_flip_left`, `asset_layerorder_swap` (base idle 1 → 0). 모두 종료 1 + `[TOP]` (마지막만 `[LAYER]`).

### A03-02 — UI 글꼴 참조

- 원인: `check_ui` 는 글꼴 경로가 `third_party/galmuri/` 로 시작하는지만 보고 파일이 있는지는 보지 않았다. 음성 시험은 `export/` · `src/` 만 복사해서 글꼴이 하나도 없는 사본이 정상 기준선이었다 — 시험 자체가 "글꼴 없음" 을 정상으로 굳히고 있었다.
- 수정:
  - `src/contract_v3.json` `uiFonts`: `dir` = `third_party/galmuri/`, role 4개 (`title` · `label` · `body` · `small`) 마다 `file` · `pixelSize` · `sha256`, `license` = `{file: LICENSE.txt, name: "SIL OFL 1.1", sha256}`, `source` = Galmuri 2.40.4 공식 zip (URL · sha256 `c8b3d986…56e60e7`). 해시는 03 의 파일을 잰 값이고, 그 5개 파일이 공식 zip 안의 파일과 바이트까지 같음을 오늘 다시 대조했다.
  - `scripts/check_art03.py` `check_fonts` (`FONT`): role 마다 계약과 같은 `file` · `pixelSize` · `sha256`; 실제로 가리키는 경로가 안전한지 (상대 경로 · `/` 구분 · 빈 칸 · `.` · `..` · `\` · `:` · 절대 경로 금지 · `dir` 아래); `--root` 기준으로 링크가 아닌 실제 파일이 있고 바이트 해시가 계약과 같은지; role 밖 항목도 파일을 가리키면 안전 경로 + 있음; `license` 객체 · 고지 파일 있음 · 해시; `source` 가 계약과 같은지; manifest `license.fonts` 의 `notice` · `license` · `source`.
  - `scripts/build_export03.py`: ui-pieces `fonts` 각 role 에 실제 파일에서 잰 `sha256`, `license` 를 문자열 → `{file, name, copyright}` 객체로, `source` 추가, manifest `license.fonts.source` 추가.
  - `tests/test_check_art03.py`: 모든 사본에 `third_party/` 를 복사한다 (QA 지적의 기준선 문제).
- 시험 (12가지): `font_ref_missing` (QA 변이 그대로) · `font_path_unsafe` (`third_party/galmuri/../galmuri/<실제 파일>`) · `font_extra_unsafe` (role 밖 항목이 `..` 로 폴더 밖 실제 파일 — 안전 경로 규칙만 잡을 수 있음) · `font_role_missing` · `font_role_swapped` · `font_license_ref_bad` · `font_source_bad` · `font_file_missing` · `font_file_changed` · `font_license_missing` · `font_dir_missing` (QA 의 글꼴 없는 사본) · `manifest_font_notice_bad`. UI 데이터를 고친 경우 manifest 의 ui 해시까지 맞췄다. 모두 종료 1 + `[FONT]`.

### 시험이 "거절" 을 세는 방식도 바꿨다

Python 이 예외로 죽어도 종료 코드는 1 이다. 그래서 음성 시험은 이제 종료 1 **이고** 첫 줄이 `FAIL:` **이고** 트레이스백이 없으며, 새 26가지는 기대한 `[TOP]` · `[LAYER]` · `[FONT]` 줄이 있어야 통과로 센다. 실제로 "파일 있음 검사를 지운" 변이에서 5가지가 해시 계산 중 예외로 죽었는데, 옛 셈법이면 이것이 "거절" 로 보였을 것이다 (§4).

## 3. 바뀐 파일

반복 01 값은 수정 직전에 떠 둔 사본 (세션 임시 폴더, 묶음에 넣지 않음) 에서, 반복 02 값은 현재 파일에서 스크립트로 잰 것이다.

| 파일 | 반복 01 sha256 (수정 직전) | 반복 02 sha256 |
| --- | --- | --- |
| `scripts/check_art03.py` | `691a233ea9d56a18a8696522dbfbeeef8943e9776a502067bdec201414b52901` | `3654344e3a9e5c2dd69d320935cc39f0dd3f359db769e5261bc7fa102c4ccebb` |
| `scripts/build_export03.py` | `87af68c67f2e3892d2412cc69eac0ac2a1cc5bb7a2b0d809bf29b9e9b715827b` | `f492c4ee0632a4b7f0fd3c81adac2e284e3906368923bbc10b35975629a64752` |
| `scripts/verify_all.py` | `04be6ed9baceb9955e61773973d9c0cb1b9910708506a45ef16c211515b868f1` | `8e804ccffbe8c99fb53ea73d39514efd1cd2126701abfd32c4fcb5416c679903` |
| `src/contract_v3.json` | `4406f4ead86e6539ed483ce3adcc8429e27d845465d24f661638696a8c287234` | `80444372c948b1bb60cb03422c3eeab169e134f7acc50c21c4e317aac51c8b98` |
| `tests/test_check_art03.py` | `4872067b08df737b5f10b2b5a6572f6282d1f33183d52258fe49e159ac3808de` | `fe0c1c58db1d930cfce20835cf536a23c714ed0edefa1f4d890e6d382fe2d3b1` |
| `tests/checker_mutations_iter02.py` | (새 파일) | `79f8c151571754600f36f3ac9c504065953115b03fa9a7305126d6fee319e97f` |
| `export/manifest_v3.json` | `f08ca05f7adf7755eee4197122fe1a75110973deb612c37f1e00a80467b46b1a` | `0c7f6ff0b86d83217beedf62f298294cd6c01c036ab589e2abed383c1e429751` |
| `export/ui/ui-pieces_v3.json` | `1419d9d86b120e0f38df6661d5601a46df6526e2e8844f740518a4801f5528b4` | `7117b480b2f88d721e7bd9422a285da64bc7a5bb6f29a053dd43b18ebf9da77f` |

manifest 에서 바뀐 맨 위 칸은 `flip` · `inputs` (계약 해시) · `data` (ui 해시) · `license` 뿐이다. 에셋 79개의 PNG 해시 · pixel-source · tiles 해시는 반복 01 과 같다 (스크립트로 대조). 새 결과 파일: `tests/results/check_negative_results.{json,txt}` (갱신), `checker_mutations_iter02.txt`, `checker_mutations_iter02_with_iter01.txt`, `qa_iter01_repro_iter02.txt`, `verify_all.txt` (갱신).

## 4. 검증 — 명령과 실제 종료 코드

`art-concept-03` 에서, python 3.11.6 · numpy 2.4.3 · Pillow 12.1.1 · zlib 1.2.12:

| 명령 | 종료 | 결과 |
| --- | --- | --- |
| `python3 -I -B scripts/verify_all.py --catalog <Bible-Survivor>/content/genesis/catalog.json --review` | **0** | 12단계 모두 기대값 (`tests/results/verify_all.txt`) |
| └ 2 · 3 `check_art03.py --quiet` (+ `--catalog`) | 0 · 0 | `PASS: 79/79 assets, manifest top level, contract, player layers, pixel-source, tiles, ui, fonts` |
| └ 4 `tests/test_check_art03.py` | 0 | 손대지 않은 사본 0, 망가뜨린 41가지 모두 1 + 해당 코드 |
| └ 4b `tests/checker_mutations_iter02.py` | 0 | 변이 5개 모두 잡힘 |
| └ 5 `regen_check.py` | 0 | 83 / 83 바이트 같음 |
| └ 5b `build_no_pillow_check.py` | 0 | 83 / 83 바이트 같음 |
| └ 6 · 7 · 8 옛 v1 · v2 변환 / 거절 | 0 · 0 · 1 | 17 / 17, 53 / 53, `SCHEMA` 거절 |
| └ 9 · 10 검토 판 · 애니메이션 메타 | 0 · 0 | 23 애니메이션 |
| `python3 -I -B tests/checker_mutations_iter02.py --old-checker <반복 01 검사기 사본>` | 0 | 반복 01 검사기는 새 26가지를 모두 종료 0 으로 통과시킴 (`checker_mutations_iter02_with_iter01.txt`) |
| QA 최소 재현 코드 (글자 그대로) | — | 현재 1 · 1, 반복 01 검사기 0 · 0 |

변이 5개 (임시 사본에서 한 줄씩 지움 → 음성 시험이 실패해야 함):

| 지운 것 | 깨끗이 거절되지 않은 케이스 |
| --- | --- |
| `check_top(M, C, R)` 호출 | `top_*` 13가지 모두 통과 (종료 0) |
| 레이어별 `layerOrder` 비교 | `asset_layerorder_swap` 통과 |
| 글꼴 파일 있음 확인 | 5가지가 해시 계산에서 예외로 죽음 — 시험이 거절로 세지 않음 |
| 안전 경로의 `.` / `..` 규칙 | `font_extra_unsafe` 통과 |
| `check_fonts` 호출 | `font_*` · `manifest_font_*` 12가지 모두 통과 |

## 5. 설계 결정 (QA 가 볼 곳)

1. **`flip` 을 문자열에서 객체로.** QA 수용 조건 "구조화된 방향 계약 또는 명시된 허용값" 중 구조화를 골랐다. 문자열은 임포터가 읽을 수 없고 같은 뜻의 다른 문장을 막을 수도 없다. manifest `flip` · ui `fonts` 를 읽는 코드는 없다 (두 방법으로 확인: `grep` 과 파이썬 전체 순회 — 03 스크립트 · 시험 · UI draft · Bible-Survivor 저장소에서 `flip` · `fonts` 키 소비처 0, Bible-Survivor 는 `manifest_v3` · `ui-pieces_v3` 를 참조하지 않음. 03 안에서 manifest 를 읽는 `build_review03.py` 는 다른 칸만 읽고 검토 판 다시 만들기도 종료 0). 개발 담당에게 변경을 알렸다.
2. **글꼴 해시를 계약에 적었다.** 경로만 맞추면 다른 파일로 바꿔치기해도 통과하므로, role 마다 파일 해시까지 계약이 정한다. 글꼴 파일을 바꾸려면 계약을 함께 고쳐야 하고, 그러면 manifest `inputs.contract` 해시가 바뀌어 `STALE` 이 다시 만들기를 요구한다.
3. **글꼴 경로 기준은 `--root`.** `export/png/…` 와 같은 기준 (묶음 뿌리). `--src-root` 는 계약 · 팔레트에만 쓴다.
4. **ui-pieces `fonts.license` 를 객체로.** 문자열 안의 경로는 검사할 수 없어서. 반복 01 형식은 `FONT` 형식 오류로 거절된다.

## 6. GC 근거

| 분류 | 사건 · 근거 | 이번 처리 |
| --- | --- | --- |
| 도구 버그 (QA 발견) | A03-01 · A03-02 — 검사기가 인계 계약의 **소비 필드** (맨 위 메타데이터 · 참조 파일) 를 안 봄 | 계약 기대값 + 검사 + 음성 26가지 + 변이 확인을 `verify_all` 에 연결 (§2 · §4). 문구 규칙이 아니라 코드로 막음 |
| 시험 설계 실수 (제작자) | 반복 01 의 음성 시험은 내가 만든 결함 표만 변이했고, 시험 사본이 `third_party/` 를 빼서 "글꼴 없음" 이 정상 기준선이었다 | 시험 사본을 실제 인계 단위 (`export` · `src` · `third_party`) 로; 소비 필드마다 단독 삭제 · 형식 · 값 변이; 반복 01 검사기 전체를 넣으면 새 케이스가 모두 통과함을 실행으로 확인 |
| 셈법 결함 (이번에 발견) | 예외로 죽은 검사기도 종료 1 이라 "거절" 로 세질 수 있었다 — 변이 "파일 있음 확인 지움" 에서 실제로 5가지가 예외 | 음성 시험이 `FAIL:` 첫 줄 · 트레이스백 없음 · 기대 코드 줄을 요구; 변이 결과에 통과 / 예외 / 다른 코드 구분 |
| 도구 사용 (이번 회차) | ① 파이썬 here-doc 안 문자열 치환이 assert 에서 멈춤 (원인 미확인, 파일은 안 바뀜) → Edit 도구로 전환 ② `verify_all` 의 변이 단계가 `--old-checker` 결과 파일을 덮어씀 → 출력 파일을 둘로 나눔 ③ 첫 변이 결과에 예외를 "통과" 로 적음 → 구분 칸 추가 | 모두 이번 회차에 고침. 원본 · 결과 유실 없음 |
| 전역 GC 후보 (적용 안 함) | "검사기 음성 시험은 인계 계약이 **소비하는** 필드마다 단독 삭제 · 형식 · 값 변이를 두고, 시험 사본은 실제 인계 단위를 복사한다" · "음성 시험은 종료 코드만이 아니라 기대 오류 코드 줄을 확인한다 (예외 = 종료 1)" | 이 프로젝트에서는 코드 (`test_check_art03.py` · `checker_mutations_iter02.py`) 로 이미 강제. 전역 규칙 반영은 "전역 GC 자동 apply 금지" 에 따라 하지 않음 — root / 사용자 판단 |

모델 · 사용량: 이 세션 모델 Opus 5.5. 토큰 사용량 수치는 전달받지 못해 비용 효율을 수치로 판단하지 않았다. 추가 worker · 서브에이전트 0.

## 7. 디자인 협업 (UI-01) — 이번 회차에 오간 것

- 개발 담당 (dispatch `ctx_06b7040ad4e5`) 의 `ui.levelup.card` 요청 (msg_4547f1fee9b0) 에 03 규격 대조로 답했다 (msg_bffd9e7bdcb1): 개발 숫자 = 03 게임 px × 정수 배율 2, 9-slice Border 원본 기준 6 · Tiled, 여백 12 → 16 + 포커스 표식 칸, 색 외 상태 표식 (focus = `ui_arrow_6x9` · pressed = 내용 1 게임 px 아래 · disabled = 글자 표식), 03 의 `*Bitmap*.ttf` 는 외곽선 (glyf) 이 0바이트라 Unity 동적 글꼴 · TMP 에는 맞지 않으므로 (Unity 에서 직접 확인하지는 않음) 같은 배포본의 외곽선판 Galmuri11/9/7 을 12 · 10 · 8 의 정수배로. 근거 · 시안: `art-ui-draft-01/` (03 은 읽기만).
- 개발 담당 실제 화면 (msg_3f8f13f69c5b, `Bible-Survivor/evidence/unity-ui01-fix-20261114/player/final{1100,660}-modal.png`): 카드 2줄 잘림 없음 확인. 660×440 에서 HUD 제목 글자 윗부분이 패널 위 테두리 바로 위에 잘린 조각으로 보이는 결함 1건을 확대본 (`art-ui-draft-01/mock/feedback_ui01_660_top_x2.png`) 과 완료 조건 · 방법 (03 `dim.0` 배경 또는 HUD 제목 생략) 으로 보냄 (msg_14486158c81b). 03 의 `flip` · `fonts` 형식 변경도 같은 메시지로 알림. 보조 아이콘 7종은 개발 쪽이 보류 — 필요해지면 요청 형식대로 받기로 함.
- **Claude Design 구분:** `art-ui-draft-01` 의 시안 PNG 는 이 저장소 스크립트 (`build_card_mock.py`, Pillow) 가 03 조각과 Galmuri 로 합성한 것이다. Claude Design 제품의 생성 기능은 쓰지 않았고 캔버스에도 게시하지 않았다.

## 8. 계획과 다르게 한 것

- 결과 기록 위치: coordinator 지시 "결과 문서" 를 반복 01 결과 (`art-revision-03-result.md`) 에 덧붙이지 않고 이 새 문서로 냈다 — 반복별 근거를 섞지 않으려고.
- `tests/checker_mutations_iter02.py` 를 새로 넣고 `verify_all` 단계 4b 로 연결했다. 지시에는 없었지만 "테스트만 통과하고 독립 변이가 0 이면 미해결" (QA GC) 을 이 저장소에서 매번 확인하려는 것. 변이 단계가 약 40초를 더해 `verify_all` 전체가 약 50초 (이 Mac 에서 잰 값).
- 계약에 없던 검사도 넣었다: role 밖 글꼴 항목의 안전 경로, manifest `license.fonts`, `ppu` 의 `bool` 거절. 각각 음성 케이스가 있다.

## 9. 남은 것 · 미확인

- 독립 QA 의 같은 변이 재실행 (root 가 요청). 이 문서의 종료 코드는 제작자 실행 값이다.
- Unity 임포트 · 9-slice · 글꼴 렌더 · 휴대폰: 하지 않음. 03 을 게임에 임포트하지 않았다.
- `art-ui-draft-01` 의 03 패치 계획 (외곽선 글꼴 추가 · `stateRules` 의 색 외 표식) 은 아직 03 에 넣지 않았다 — 이번 반복은 검사기 수정만. 넣으면 계약 `uiFonts` 에 role 을 추가하고 같은 음성 케이스를 늘린다.
- `art-concept-03/scripts/__pycache__/` 가 있다 (최종 수정 00:33 — 이번 반복의 `-B` 실행 이전, 누가 만들었는지 미확인). `.gitignore` 대상이라 지우지 않았다. 공개 통합에서 빼야 한다.

## 10. coordinator 인계

- 커밋 · push · 공개 통합 · Unity 임포트 하지 않음. 통합할 때 포함: `art-concept-03/` 의 `scripts/` · `src/` · `tests/` (results 포함) · `export/` · `third_party/` · `review/` · 이 문서 · `art-production-readiness.md`. 제외: `__pycache__/`, 세션 임시 폴더.
- QA 재현 명령: `cd art-concept-03 && python3 -I -B scripts/verify_all.py --catalog <Bible-Survivor>/content/genesis/catalog.json --review` (기대 0), `python3 -I -B tests/test_check_art03.py` (기대 0, 41가지), QA 문서의 최소 재현 코드 (기대 `ppu exit= 1` · `font ref exit= 1`).
