# art-unity-font-01 · Galmuri 외곽선 폰트 sidecar (인계 초안)

상태: **handoff-draft** — 오프라인 검사 `VERIFY_ALL: PASS`. root 독립 QA 전이다. Unity 임포트 · 렌더 · 기기 확인은 **NOT_RUN**(Unity 담당 후속 검증).
QA 전에는 이 폴더의 파일을 최종 게임 에셋으로 임포트하지 않는다(AGENTS.md).

## 1. 왜 따로 두나

아트03 `third_party/galmuri/Galmuri*Bitmap*-2.40.4.ttf` 4개는 **bitmap-only** 이다. glyf 테이블이 0 바이트이고 EBDT/EBLC 비트맵 strike 만 있다. 외곽선이 있는 한글은 0/11172, ASCII 는 0/94 다.
그래서 Unity 동적 폰트(외곽선을 래스터화)로는 글자를 그리지 못하고, OS 폰트 대체로 넘어간다.

이 폴더는 같은 공식 배포본(Galmuri v2.40.4)에서 **외곽선 TrueType** 4개를 꺼내, role · 크기 · 해시 · 라이선스 계약과 함께 sidecar 로 둔다.
아트03 Bitmap 파일과 repo `art/art-concept-03` 스냅샷은 건드리지 않았다. 두 곳의 sha256 은 같다.

## 2. 구성

```
fonts/galmuri-outline/        공식 zip 의 멤버와 바이트 동일한 4 폰트 + LICENSE.txt
src/font_contract_v1.json     손으로 쓴 기대값: 원본 · role · 크기 규칙 · 커버리지 · 라이선스
export/font_manifest_v1.json  build 가 실측해서 쓴 값(결정적, 다시 만들면 바이트 동일)
export/text_metrics_v1.json   레벨업 카드 71장 · UI 줄의 폭/높이 실측(게임 px)
scripts/                      sfnt.py(표준 라이브러리 SFNT 리더) · build · check · inspect · measure · render_compare · verify_all
tests/                        음성 CLI 26건 · 검사기 변이 8개 · results/
review/roles_outline_s2.png   role 별 s=2 견본(스크립트가 그림, Claude Design 아님)
RESULT.md                     실행한 명령 · exit · 계획과 다르게 한 것 · 미확인
```

## 3. Role (stable ID)

| role | 파일 | sha256 | unitsPerEm | basePx | s=1 / 2 / 3 (px) | 용도 | 대응 03 Bitmap |
|---|---|---|---|---|---|---|---|
| `title` | `Galmuri11-Bold.ttf` | `45d901e379138dd91873af157640f60aa4cf6beaa885c3efd2a8c279e039a237` | 1200 | 12 | 12 / 24 / 36 | 제목, 숫자 | Galmuri11Bitmap-Bold @16 ppem |
| `label` | `Galmuri11.ttf` | `e24256f42e43713d2ea086a1e1669d78b968f5b3cc547e5c157f0606ffa5def1` | 1200 | 12 | 12 / 24 / 36 | 카드 이름 줄, 버튼 | Galmuri11Bitmap-Regular @16 |
| `body` | `Galmuri9.ttf` | `e84e821b18be15b9e3a907ceb83cfba25fabf51c80b7edf0d2921cf8f8e1a11d` | 1000 | 10 | 10 / 20 / 30 | 효과 줄, 종류, 안내 | Galmuri9Bitmap-Regular @12 |
| `small` | `Galmuri7.ttf` | `c845c6bf6f7236ed370aed4a8f8d9b4f25e6a18f63515777fe4153d287c32917` | 800 | 8 | 8 / 16 / 24 | 레벨 숫자, 태그 | Galmuri7Bitmap-Regular @10 |

- 경로는 이 폴더 기준 `fonts/galmuri-outline/<파일>`.
- 라이선스 파일 `fonts/galmuri-outline/LICENSE.txt` 의 sha256 은 `86a3ee9495f942f0243f18c103da9faca27adb88142613edb8bb852e56c892c1`.
- 실측 글리프 수는 title 12698, label 20968, body 20714, small 20151 이다.

## 4. 크기 · AA 규칙

- 엔진 폰트 크기는 **basePx × n**(정수 n)만 쓴다. 안티에일리어싱은 끄고, raster / point 샘플링을 쓴다.
- 이유: 커버되는 모든 글리프의 점과 advance 가 100 font-unit 격자 위에 있고, `100 × basePx = unitsPerEm` 이다. 그래서 basePx × n 에서 격자 한 칸이 정확히 n px 이 된다. 다른 크기(예: 17, 18, 26)는 칸이 갈라져 흐려진다.
- s 는 아트 1 px 당 창 px 이다. 현재 카드 612 / 736 창 px 를 306 / 368 게임 px 로 본 값이 **s = 2** 이고, 이때 **title 24 · label 24 · body 20 · small 16 px** 이다.
- 이 규칙의 근거 셋:
  1. 폰트 자체의 gcd 실측이 basePx 12/12/10/8 을 준다.
  2. 검사기의 GRID 검사를 통과한다.
  3. Pillow/FreeType 으로 AA 를 끄고 그리면 외곽선 basePx 가 03 Bitmap strike 와 픽셀까지 같다(role 마다 11/11). 2×basePx 는 basePx 를 ×2 nearest 로 키운 것과 같다(11/11).
  - 3 은 Unity 가 아닌 렌더다.
- 줄 지표(게임 px, basePx 기준):

| role | ascender | descender | lineGap | 글자 상자(asc−desc) | lineHeight |
|---|---|---|---|---|---|
| title / label | 12 | −2 | 2 | 14 | 16 |
| body | 10 | −1 | 2 | 11 | 13 |
| small | 8 | −1 | 2 | 9 | 11 |

- 자간은 GPOS `kern` 쌍 조정만 있다. 값은 −100 / −200 unit, 즉 −1 / −2 px 로 격자 위다. `kern` 테이블은 없다. GSUB 기능은 tnum · zero · vert 이고 UI 에 필요하지 않다.

## 5. Unity 적용 권장 (모두 NOT_RUN — Unity 담당이 확인)

현재 UI 는 IMGUI(`GUIStyle`)다. 글꼴은 `AdamChoiceText.CreateKoreanFont()` 가 OS 폰트를 쓰고(`Font.CreateDynamicFontFromOSFont`), 크기는 17 · 18 · 20 · 26 이다. 프로젝트는 Unity 6000.6.9f1, `com.unity.ugui` 2.6.0 이다.

1. **IMGUI 그대로 쓸 때**: TTF 4개를 Font 에셋으로 임포트한다(TrueTypeFontImporter).
   - Character = Dynamic, Rendering Mode = Hinted Raster(AA 없음).
   - Font Names(대체 폰트 목록) 칸은 비운다.
   - 다만 Unity 는 목록이 비어도 전역 OS fallback 으로 넘어갈 수 있다(Unity 문서 기억 기준, 미확인). 그러니 빠진 글자가 OS 폰트로 대신 그려지지 않았는지는 4번 방법으로 따로 확인한다(계획 4항 "시스템 fallback 으로 아트 글꼴 성공을 대신하지 않는다").
   - `GUIStyle.font` 에 role 별 Font, `fontSize` = basePx × s.
2. **TextMeshPro 로 옮길 때**: role 별 Font Asset 을 만든다.
   - Render Mode = RASTER(또는 RASTER_HINTED, SDF 아님).
   - Sampling Point Size = basePx × s, 아틀라스 텍스처 Filter = Point.
   - Atlas Population = Dynamic, 또는 §6 문자 집합으로 Static.
3. **확인할 위험**: `ScreenScale()` 은 창이 660×440 보다 작을 때 `GUI.matrix` 를 1 미만의 소수 배율로 줄인다. 그러면 글자가 비정수 배율로 다시 샘플돼 흐려질 수 있다.
   - 작은 화면에서는 글자만 matrix 1 로 그리고 정수 s 를 고르는 방식을 검토한다.
   - 실제 결과는 Unity 화면으로 확인한다.
4. **확인 방법 제안**:
   - `font.HasCharacter` 로 UI 문자열 전체를 조회해 0 건 누락을 본다.
   - 660×440 · 1100×720 캡처에서 03 Bitmap 견본(`review/roles_outline_s2.png`)과 같은 획인지 비교한다.
   - OS 폰트를 끈 상태에서도 한글이 나오는지 본다.

## 6. 동적 문자 범위

- 외곽선이 확인된 범위는 11,271 code point 다. 네 폰트 모두 아래 글자마다 비어 있지 않고 격자 위인 글리프가 있다.
  - 한글 음절 U+AC00–U+D7A3 (11,172)
  - 출력 가능 ASCII U+0021–U+007E (94) + 공백
  - UI 기호 `°` `·` `–` `…` `→`
- 현재 UI 문자열이 쓰는 글자(한글 303자, ASCII, 위 기호)는 모두 이 안에 있다.
- 알려진 빈칸: `small`(Galmuri7) 에는 `►` U+25BA 가 없다. 현재 UI 는 쓰지 않는다.
- 이 범위 밖의 글자(한자, 이모지, 다른 기호)는 검사하지 않았다. 새 문자열이 생기면 `src/font_contract_v1.json` 의 `samples` / `uiSymbols` 에 넣고 검사를 다시 돌린다.

## 7. 카드 · UI 줄 실측 (`export/text_metrics_v1.json`, 게임 px, kern 포함)

- 카드 텍스트는 `AdamChoiceText.Describe` / `Card` 를 Python 으로 다시 구현해 Core `WeaponRules` 수치로 만들었다(무기 3 × 8 단계 + 보조 47 = 71장). 개발 캡처의 카드 3장과 눈으로 대조했고, Unity 에서 돌린 것은 아니다.
- 카드 폭: 660 창 306, 1100 창 368. 테두리 6 · 안쪽 여백 2 · 포커스 표시 칸 10 을 빼면 글자 폭은 **280 / 342** 다.
- 이름 줄(label) 최대 **250**: `1. 매끄러운 다섯 돌  |  보조 · 신규 0 → 1/2`.
- 효과 줄(body) 최대 **256**: `피격 시 근처 적 반격 피해 20 → 25 (피해 보너스 반영)`.
- 71장 모두 한 줄에 들어가고 빠진 글리프는 0 이다.
- 카드 높이: 2×(6+2) + 이름 14 + 간격 2 + 효과 11 = **43**(효과 2줄이면 56). 개발 카드 88 창 px = 44 게임 px 안에 든다.
- UI 줄:

| role | 문자열 | 폭 |
|---|---|---|
| title | 레벨 상승 · 하나를 선택하세요 | 169 |
| title | 시작 무기를 선택하세요 | 130 |
| body | 전투 정지 중 · 숫자 1–3 / 클릭 · 선택 즉시 저장 | 222 |
| body | 무기 수치는 기본값 · 보조/영구 강화는 별도 합산 · 진화 미구현 | 292 |
| body | 저장 기록을 불러오는 중… | 122 |
| label | 재개 (Enter) | 71 |

  - 이 줄들의 IMGUI 영역 폭은 `panel.width − 24` 로 카드 폭과 같다(660 창 306, 1100 창 368). 가장 긴 body 292 도 들어간다. 실제 줄바꿈은 Unity 화면으로 확인한다.

## 8. 원본 · 라이선스

- 원본: Galmuri **v2.40.4** 공식 배포 zip `https://github.com/quiple/galmuri/releases/download/v2.40.4/Galmuri-v2.40.4.zip`
  - zip sha256 `c8b3d9861a62ae73c8b1178091401cd79994812437ef386413f6dd54856e60e7`, 40 멤버.
  - 4 폰트와 LICENSE.txt 는 zip 루트 멤버와 바이트 동일하다(`check_fonts.py --zip` 이 비교).
  - name 테이블의 버전은 `Version 2.404`.
- 라이선스: **SIL Open Font License 1.1**, `Copyright (c) 2019–2025 Lee Minseo`. 저작권 머리말에 Reserved Font Name 은 없다(검사함).
- OFL 의무:
  - 폰트를 게임에 넣어 배포할 수 있다. 폰트 파일과 함께 LICENSE.txt 를 같이 둔다.
  - 폰트 파일 단독 판매는 안 된다.
  - 수정본을 만들면 OFL 로 배포한다.
  - 크레딧 / 오픈소스 고지 화면에 Galmuri · 저작권 · OFL 을 적는 것을 권장한다.
- 유료 항목은 없다.

## 9. 검사 실행

```
# 표준 라이브러리만(1–8단계는 python3 -I -S -B 로 실행)
python3 -I -B scripts/verify_all.py
# 공식 zip 바이트 대조 · 변이 · Pillow 렌더 대조까지
python3 -I -B scripts/verify_all.py --zip Galmuri-v2.40.4.zip --mutations --render
# 개별
python3 -I -B scripts/check_fonts.py [--zip Galmuri-v2.40.4.zip]   # exit 0 PASS / 1 FAIL / 2 사용 오류
python3 -I -B scripts/inspect_font.py FONT.ttf ...                # outline 이 아니면 exit 1
```

- 03 Bitmap 폴더는 기본값 `../art-concept-03/third_party/galmuri` 다. repo 안에서 돌릴 때는 `--bitmap-dir` 로 `art/art-concept-03/third_party/galmuri` 를 준다.
- 검사기 오류 코드: CONTRACT · MANIFEST · ROLE · PATH · FILE · HASH · SFNT · GLYF · CMAP · GRID · SIZE · NAME · LICENSE · SOURCE · STALE.
- build 는 이미 있는 다른 파일을 덮어쓰지 않는다(`EXISTS:` · exit 1).

## 10. 제작 방식 구분

- 폰트는 공식 배포본을 그대로 복사했다. 수정 · 재래스터화는 하지 않았다.
- `review/roles_outline_s2.png` 는 `render_compare.py` 가 Pillow 로 그린 **스크립트 합성 견본**이다. 제품 화면도 Claude Design 산출물도 아니다.
- 이번 작업에서 Claude Design 제품은 쓰지 않았다(폰트 sidecar 에 새 시안이 필요하지 않았다).

## 11. NOT_RUN · 미확인

- Unity 임포트(TrueTypeFontImporter / TMP Font Asset), Unity 안의 한국어 렌더, AA · 필터 결과, `GUI.matrix` < 1 에서의 선명도
- 실제 게임 화면의 카드 줄바꿈 · 높이, OS 폰트 fallback 이 꺼졌는지
- 모바일 기기 · 다른 해상도 · 터치 가독성
- 카드 문자열은 C# 을 Python 으로 옮긴 재구현이다. Unity 실행 문자열과 1:1 대조는 하지 않았다.
