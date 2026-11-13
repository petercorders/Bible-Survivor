# art-unity-font-01 결과 (2026-11-14, task_cfdc6fe3aaf0 / ctx_f7f64b6adb60)

결론: Galmuri v2.40.4 공식 배포본의 **외곽선** TTF 4개(title · label · body · small)를 sidecar 로 묶었다.
role · 크기 · sha256 · 라이선스 · 원본 zip 계약과 표준 라이브러리 검사기를 만들었고, 오프라인 검사는 모두 기대한 exit 로 끝났다(`VERIFY_ALL: PASS`).
아트03 Bitmap 4개는 bitmap-only(glyf 0 B)로 판정되며, 외곽선 판으로 보고하지 않는다. Unity 임포트 · 렌더 · 기기는 NOT_RUN 이다. root 독립 QA 전이다.

## 1. 실행한 명령과 exit (`tests/results/verify_all.txt`)

`python3 -I -B scripts/verify_all.py --zip <Galmuri-v2.40.4.zip> --mutations --render`

| # | 단계 | 명령 | exit / 기대 | 결과 |
|---|---|---|---|---|
| 1 | manifest 재생성 | `python3 -I -S -B scripts/build_font_package.py --out <tmp>` | 0 / 0 | `export/font_manifest_v1.json` 바이트 동일 |
| 2 | text metrics 재생성 | `python3 -I -S -B scripts/measure_text.py --out <tmp>` | 0 / 0 | `export/text_metrics_v1.json` 바이트 동일 |
| 3 | 검사기 | `python3 -I -S -B scripts/check_fonts.py --quiet` | 0 / 0 | `PASS: 4/4 roles … 11271 covered code points …` |
| 4 | 검사기 + 원본 zip | `… check_fonts.py --zip <zip> --quiet` | 0 / 0 | 같은 PASS(멤버 바이트 대조 포함) |
| 5 | 음성 CLI 26건 + 양성 2건 | `python3 -I -S -B tests/test_check_fonts.py --bitmap-dir … --zip <zip>` | 0 / 0 | `NEGATIVE TEST: PASS` |
| 6 | 패키지 폰트 판정 | `… scripts/inspect_font.py fonts/galmuri-outline/*.ttf` | 0 / 0 | 4개 모두 `outline`, 한글 11172/11172 · ASCII 94/94 |
| 7 | 아트03 Bitmap 판정 | `… inspect_font.py ../art-concept-03/third_party/galmuri/*Bitmap*.ttf` | 1 / 1 | 4개 모두 `bitmap-only`, glyf 0 B, EBDT/EBLC, 0/11172 · 0/94 |
| 8 | 검사기 변이 8개 | `… tests/checker_mutations.py --bitmap-dir … --zip <zip>` | 0 / 0 | 8/8 CAUGHT |
| 9 | 렌더 대조(Pillow 12.1.1, FreeType) | `python3 -I -B scripts/render_compare.py --bitmap-dir …` | 0 / 0 | role 4개 모두 A 11/11 · B 11/11 · 잉크 11/11 · 대조군 11/11 |

- 1–8 단계는 `-S`(site-packages 없음)로 돌았다. 표준 라이브러리만 쓴다는 실측이다.
- 9 단계만 Pillow 가 필요하다. Pillow 가 없으면 exit 3 이고 SKIP 이며 PASS 로 세지 않는다.

추가로 따로 돌린 것:
- repo 스냅샷 판정: `python3 -I -S -B scripts/inspect_font.py <repo>/art/art-concept-03/third_party/galmuri/*Bitmap*.ttf` → exit 1, 4개 모두 `bitmap-only`.
- repo 스냅샷과 docs 03 사본의 sha256 비교: 4개 모두 같다.
  - Galmuri11Bitmap-Bold `0b929fe8…`
  - Galmuri11Bitmap-Regular `0d7e7e86…`
  - Galmuri7Bitmap-Regular `c093a028…`
  - Galmuri9Bitmap-Regular `897c621b…`
- 패키지 개인 경로 검색: 0 건. `__pycache__` 없음.

## 2. 음성 입력 (`tests/results/check_font_negative_results.{json,txt}`)

- 통과 기준: exit 1 · 첫 줄 `FAIL:` · traceback 없음 · 기대한 `[CODE]` 줄이 있어야 한 건으로 센다(충돌은 거절이 아니다).

| 코드 | 건 |
|---|---|
| GLYF | `bitmap_as_outline`(실제 03 Galmuri11Bitmap-Regular 를 label 자리에 넣고 계약 · manifest 를 그 폰트에 맞게 고쳐 GLYF 만 걸리게 함), `empty_glyf_synthetic`, `sample_glyph_empty`('가' 하나만 빈 글리프) |
| SFNT | `not_truetype`, `cff_version`, `truncated`, `loca_out_of_range` |
| FILE | `file_missing`, `license_missing` |
| HASH | `hash_bad` |
| ROLE | `role_missing`, `role_swapped`, `role_extra` |
| PATH | `path_unsafe`(`../`) |
| SIZE | `size_not_integer`(10.5), `scale_px_wrong`(s=2 에 23) |
| GRID | `grid_claim_6px`(basePx 를 6 으로 주장) |
| CMAP | `cmap_symbol_missing`(small 에 ►) |
| NAME | `name_wrong` |
| LICENSE | `license_text`, `license_rfn` |
| SOURCE | `source_mismatch`, `zip_wrong`, `zip_member_differs` |
| STALE | `stale_contract` |
| build | `build_refuses_overwrite`: `EXISTS:` exit 1, 파일이 바뀌지 않았는지도 확인 |

## 3. 변이 (`tests/results/checker_mutations.txt`)

- 8개 모두 CAUGHT 다. 각 변이는 임시 사본에서 핵심 줄 하나를 깨고 음성 시험을 돌려, 그 줄이 지키던 경우가 통과돼 버리는지 본다.
- 1번 "glyf 테이블 존재만 보는 옛 구조"가 이 패키지가 막으려는 회귀다.
  - 0 바이트 glyf 검사와 빈 글리프 검사를 함께 지웠다.
  - 그러자 `empty_glyf_synthetic` · `sample_glyph_empty` 가 **통과(exit 0)** 됐다.
  - 실제 03 Bitmap 은 그 변이에서도 거절됐다. 다만 GRID(03 Bitmap 의 advance 가 100 unit 격자 밖) 때문에 우연히 걸린 것이고, GLYF 로 걸린 것이 아니다.
  - 그래서 회귀를 잡는 근거는 두 합성 사례다. 실제 Bitmap 사례는 "다른 검사도 막는다"는 보조 근거로만 센다.
- 나머지 7개: cmap · 격자 · 라이선스 문구 · RFN · 모르는 role · zip 바이트 대조 · build 덮어쓰기. 각 변이는 이름 붙은 사례만 통과시켰다.

## 4. 렌더 대조 (`tests/results/render_compare.txt`, Unity 아님)

- role 마다 문자열 11개를 그렸다: 계약 샘플 8 + 출력 가능 ASCII + 한글 97자 간격 + UI 기호.
- A 외곽선 @basePx(AA 끔) = 03 Bitmap strike(16/16/12/10 ppem): 픽셀 동일 11/11.
- B 외곽선 @2×basePx = A 를 ×2 nearest 로 키운 것: 동일 11/11.
- 빈 그림끼리 같다고 세지 않으려고 잉크 확인을 넣었다. 대조군(basePx+1 은 달라야 함)도 넣었다.
- 견본 `review/roles_outline_s2.png` 는 스크립트 합성이다.

## 5. 카드 실측 요약 (`export/text_metrics_v1.json`)

- 71장. 최대 이름 줄 250 · 효과 줄 256 게임 px. 글자 폭 280(660 창) / 342(1100 창) 안에 모두 한 줄로 들어간다.
- 빠진 글리프 0. 카드 높이 43 ≤ 개발 카드 44.
- 개발의 1100×720 Player 캡처(`evidence/unity-art-20261114/player/1100-modal.png`, OS 임시 폰트)와 대조했다:
  - 패널 760 · 카드 88 창 px, 아트 2배 표시가 s = 2 가정과 맞다.

## 6. 계획과 다르게 한 것

- 공유 대상: 디스패치는 `ctx_80bfdc71404e` 에 규격을 공유하라고 했다. 코디네이터가 최신 Unity 담당을 `ctx_07e314900a51` 로 바꿨으므로 그 담당의 요청(msg_240fea547345)에 답했다.
- "가능하면 stdlib": 검사 · 빌드 · 실측 · 변이는 stdlib 만 쓴다. 렌더 대조만 Pillow(선택 단계, 없으면 SKIP)를 쓴다. stdlib 래스터라이저를 새로 짜는 것보다 FreeType 결과가 근거로 강하다고 판단했다.
- Claude Design: 쓰지 않았다. 폰트 sidecar 에 새 시안이 필요 없었다. 견본 PNG 는 스크립트 합성이라고 표시했다.
- 카드 문자열: Unity 실행에서 뽑지 않고 C# `Describe` / `Card` 를 Python 으로 옮겨 만들었다. 개발 캡처 3장과 눈으로만 대조했다.

## 7. 작업 중 고친 실수 (GC 메모)

- 계약 샘플에 수치를 지어 넣은 줄이 있었다("전방 120° 타격 · 기본 피해 10 · 수 1 · 대기 1.2초"). 코드 리터럴 "전방 120° 타격" 으로 바꾸고, 샘플은 AdamGame.cs 리터럴과 캡처의 카드 두 줄만 쓴다고 계약 note 에 적었다.
  - 교훈: 계약 샘플 · 기대값은 출처(코드 줄 · 캡처)가 있는 문자열만 쓴다.
- 렌더 대조가 처음엔 빈 그림끼리도 "같음"으로 셀 수 있었다. 잉크 확인과 대조군을 더했다.
  - 교훈: 같음 비교에는 항상 "달라야 하는 대조군"을 함께 둔다.
- 변이 1번의 실제 Bitmap 사례가 다른 코드(GRID)로 걸린다는 것을 결과에서 확인했다. 회귀 근거를 합성 사례로 좁혀 적었다(학습 규칙 r89439e).
- 도구화 후보: 없음. `verify_all.py` 가 이미 9 단계를 한 번에 돌리고 exit 를 기록한다.

## 8. NOT_RUN · 미확인

- Unity TrueTypeFontImporter / TMP Font Asset 임포트, Unity 안 한국어 렌더, AA · 필터 결과
- 실제 화면의 카드 줄바꿈 · 높이
- `GUI.matrix` 배율 < 1(660×440 미만)에서의 선명도
- OS fallback 이 실제로 쓰이지 않는지
- 모바일 기기 · 터치 가독성
- 계약 범위(한글 음절 · ASCII · ° · – … →) 밖 글자
- Unity 의 Font Names / 전역 fallback 동작은 문서 기억 기준이다. 이 Mac 에서 확인하지 않았다.

## 9. root 독립 QA 에 넘길 것

- 대상은 이 폴더 전체다(README.md · RESULT.md · fonts · src · export · scripts · tests · review).
- 재현: 위 1 의 명령. zip 은 공식 URL 에서 받아 sha256 `c8b3d986…60e7` 을 확인한다.
- repo 로 옮길 때 03 스냅샷 · Bitmap 파일은 그대로 두고 sidecar 를 별도 폴더로 둔다. 이 패키지의 build 는 다른 파일을 덮어쓰지 않는다.
