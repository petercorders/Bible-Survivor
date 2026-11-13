# art-unity-font-01 독립 QA

2026-11-14 · 담당 독립 QA Codex · 파일/계약/검사/재생성/오프라인 렌더 검토.

**PASS — 현재 스냅샷의 오프라인 폰트 sidecar 인계 준비 완료.** 이번 범위에서 새 HIGH/MEDIUM은 발견하지 않았다. **Unity 임포트·실렌더·fallback·IMGUI/TMP·실기기는 NOT_RUN**이다. coordinator의 통합과 Unity 담당 `ctx_07e314900a51`의 실행 검증은 별도 게이트이며 이 PASS에 합산하지 않는다.

## 원본·스냅샷·공식 zip

README/RESULT, `src/font_contract_v1.json`, export 두 JSON, scripts의 build/check/sfnt/measure/render/verify, tests/변이 및 폰트·고지를 직접 읽었다. 원본 폴더를 수정하지 않고 별도 임시 사본에서 실행했다.

| 파일 | SHA-256 |
| --- | --- |
| export/font_manifest_v1.json | `16308da615ad12445794a89ac8dfd612b7a4060223e4e657b18eef79c37579bd` |
| export/text_metrics_v1.json | `0e13b58badced09495f1eaad4ce556dee6fd8459d868fdbac2fe3759fc92c82b` |
| scripts/check_fonts.py | `e06a9af22a7c32dfd32fb0510dc7cff1dcac936c66b88d77f7c0d6059582af53` |

공식 계약 URL `[Galmuri v2.40.4](https://github.com/quiple/galmuri/releases/download/v2.40.4/Galmuri-v2.40.4.zip)`에서 읽기 다운로드했다. Python urllib은 로컬 CA 인증 오류로 실패했고, TLS 검증을 끄지 않은 시스템 curl로 성공했다. zip **19,938,873 bytes /40멤버**, SHA-256 **`c8b3d9861a62ae73c8b1178091401cd79994812437ef386413f6dd54856e60e7`**로 계약과 같았다. zip 루트의 TTF4개와 LICENSE.txt를 납품 파일과 직접 바이트 비교해 **5/5 동일**을 확인했다. 원격 출처가 미확인인 상태로 제작자 보고만 믿은 결과가 아니다.

| role | outline 파일 | basePx / s=2 | unitsPerEm | 실측 glyph 수 |
| --- | --- | --- | --- | --- |
| title | Galmuri11-Bold.ttf | 12 /24 | 1200 | 12698 |
| label | Galmuri11.ttf | 12 /24 | 1200 | 20968 |
| body | Galmuri9.ttf | 10 /20 | 1000 | 20714 |
| small | Galmuri7.ttf | 8 /16 | 800 | 20151 |

폴더 내 LICENSE는 저작권·OFL1.1 전문을 포함하고 계약 SHA와 일치한다. 공식 zip에서 같은 고지를 확인했다. 폰트의 `name` 버전은 Version2.404, SFNT00010000, glyf가 실제 비어 있지 않다. 새로운 그림/글꼴을 만든 작업이 아니라 공식 outline 파일을 보존한 sidecar다.

## 실제 실행과 재생성

임시 QA 루트 `<qa>`: `<local temporary evidence>`.
`<pkg>`은 그 안의 `art-unity-font-01`. 원본/리뷰 파일에 실행 로그를 덮어쓰지 않도록 전체 사본에서 다음 명령을 실행했다.

```sh
python3 -I -B scripts/verify_all.py --zip ../Galmuri-v2.40.4.zip --bitmap-dir <art-docs>/art-concept-03/third_party/galmuri --mutations --render
```

**최종 exit0 /VERIFY_ALL: PASS.** 실제 하위 결과:

| 단계 | exit / 결과 |
| --- | --- |
| manifest 재생성 | 0 / 원본과 바이트 동일 |
| 카드 text metrics 재생성 | 0 / 원본과 바이트 동일 |
| checker | 0 / 4/4 roles |
| checker + 공식 zip | 0 / 실제 멤버 바이트 비교 포함 |
| 제작자 음성 CLI 하네스 | 0 / 양성2건0, 오류26건 명시 거절1 |
| 납품 TTF4개 inspect | 0 / 모두 outline |
| 기존03 Bitmap4개 inspect | **1 /예상 실패**: 모두 bitmap-only, glyf0 B, EBDT/EBLC |
| 검사기 변이 | 0 /8개 모두 CAUGHT |
| Pillow 렌더 대조 | 0 /4role 각각 A11/11·B11/11·ink11/11·control11/11 |

1~8단계는 최상위 명령이 `-I -S -B`로 실행되며 검사/빌드/실측/변이 코드는 표준 라이브러리다. 렌더 대조만 Pillow12.1.1/FreeType2.14.1을 사용했다. Python3.11.6. 이번 환경에서 Pillow가 존재하여 렌더 단계는 실제 실행됐고 SKIP을 PASS로 세지 않았다.

재생성은 source/fonts를 읽어 별도 출력 위치에 manifest/metrics를 만든다. `build_font_package.py --zip`은 기존 다른 폰트/고지의 덮어쓰기를 거절한다. **manifest JSON은 재생성 출력이므로 덮어쓰며, 폰트 파일과 같은 불변 정책이 아니다.** `--out`은 manifest 출력 위치만 바꾸고 font 추출 위치는 실행하는 패키지 root이므로 QA에서는 반드시 스크립트도 포함한 사본에서 실행했다.

## QA가 독립 생성한 잘못된 입력

제작자의 테스트 입력 생성 함수는 호출하지 않았다. 케이스별 새 `src/export/fonts` 사본을 만들고 실제 checker를 별도 프로세스로 실행했다. 폰트 바이트를 변조한 케이스는 **contract·manifest의 폰트 SHA 및 contract SHA도 맞춰** HASH/STALE만 우연히 잡히지 않도록 했다. zip 멤버 바이트 비교 케이스도 고지 해시를 맞췄다.

```sh
python3 -I -S -B <pkg>/scripts/check_fonts.py --root <qa>/independent/<case> --quiet
# zip 관련 케이스는 --zip <qa>/Galmuri-v2.40.4.zip 추가
```

정상2건 exit0, 오류15건 exit1을 관찰했다(아래 그룹 행에는 여러 건이 들어 있음). checker의 거절은 첫 줄 `FAIL:`, 기대 `[CODE]`, traceback 없음까지 확인했다. build의 의도된 거절은 `EXISTS:`와 파일 불변을 확인했다. 테스트 수집 하네스 자체의 exit0을 잘못된 입력의 exit로 쓰지 않았다.

| 독립 입력 | 실제 exit /명시 근거 |
| --- | --- |
| baseline /baseline+공식zip(2건) | 각각0 /PASS4 |
| small glyf 길이를0으로 하고 loca 전체0(해시 정합) | 1 /GLYF: glyf0 B 및11271개 empty |
| 실제03 Galmuri11Bitmap-Regular를 label로 대체, family/units/base/해시도 맞춤 | 1 /GLYF: glyf0 B,11271 empty. 이 QA 입력에서는 GLYF로 거절됨 |
| small 실제 폰트 삭제 | 1 /FILE |
| 실제 LICENSE 삭제 | 1 /FILE |
| manifest body role 삭제 | 1 /ROLE |
| small cmap에서 A를 없애는 첫 광범위 변이 | 1 /CMAP와SFNT. 원치 않는 다른 매핑 영향도 있어 단독 원인 증거로는 아래 정밀 변이를 사용 |
| **다른 매핑을 보존하고 A만 제거한 cmap** | **1 /CMAP만**: U+0041 A 하나 누락 |
| body base10.5, contract/manifest 모두 변경 | 1 /SIZE: 양의 정수 아님 |
| title uiScalePx[2]=23 | 1 /SIZE |
| `fonts/galmuri-outline/../galmuri-outline/Galmuri9.ttf` | 1 /PATH(+ROLE) |
| body 파일을 절대경로로 표기 | 1 /PATH(+ROLE) |
| manifest body zipMember를 없는 이름으로 변경 | 1 /SOURCE |
| contract source 멤버와 manifest를 없는 archive 멤버 이름에 맞춤 | **1 /SOURCE**: zip has no member __qa_absent__.ttf |
| LICENSE 끝 개행 추가·계약/manifest SHA도 갱신, 실제zip과 대조 | **1 /SOURCE만**: zip member와 바이트 다름 |
| body 마지막 바이트를 바꾸고 build --zip | **1 /EXISTS:**, 변경된 파일 SHA가 실행 전후 동일하여 덮어쓰지 않음 |

정밀 cmap 변이는 원본 Unicode format4 매핑을 별도 코드로 읽고 **A만 제외한 format12 표를 새로 구성**, 해당 table offset/length를 갱신했다. 기존 checker의 coverage 목록만 바꾸는 방식과 구분되며 실제 파일의 cmap 손상을 검사했다. 원본 글리프 데이터·나머지 cmap 매핑은 보존했다.

기록은 임시 `<qa>/independent-results.json`, `cmap_A_only_removed.log`, `zip_actual_member_missing.log`; 영구 재현 정의와 실제 결과는 위 표에 보존했다. 원본 그림·폰트를 직접 고치지 않았다.

## SFNT·한글·렌더·크기 판정의 범위

- checker 실제 실행은 각 role에서 한글음절11172 +출력 ASCII94 +UI기호5 = **11271개의 비공백 code point**에 대해 cmap·nonempty outline·grid를 확인했다. 공백 U+0020은 별도로 mapped인지 확인하며 빈 glyph를 허용한다. 따라서 공백까지 합한 확인 범위는11272이며 README의 “11271에 공백 포함”처럼 읽히는 서술은 수치 범위를 구분해서 인계해야 한다(LOW 설명 정밀도).
- loca는 모든 glyph ID의 범위를 확인하지만 **글꼴 전체의 모든 code point/모든 복합문자 조합을 렌더한 것은 아니다.** 한자·이모지 등 계약 밖 글자는 커버리지 PASS 대상이 아니다. small의 ► 누락은 계약의 knownGaps와 일치한다.
- grid100 ×basePx =unitsPerEm이며 정수배 크기가 권장된다. Pillow/FreeType에서 AA를 끄고 그린 11문자열/role은 outline@base와 Bitmap strike가 같고 outline@2base는 nearest×2와 같다. 잉크 존재와 base+1의 차이 대조군도 모두 확인했다. **4role×전체한글11172를 모두 픽셀 렌더 대조한 것은 아니다:** 렌더 샘플은 계약8문자열+ASCII문자열+97음절 간격 한글+UI기호이며 전체 한글 검사는 SFNT 구조/coverage 검사다.
- `review/roles_outline_s2.png`를 `view_image`로 직접 열어 제목·본문·숫자/기호의 정수배 견본을 확인했다. 이 PNG는 **Pillow 스크립트 합성**, Claude Design 생성·제품 화면·Unity 캡처가 아니다.
- base12/12/10/8, AA-off/point/raster 권장은 폰트 격자·FreeType 증거에 근거한 **Unity 적용 후보**다. Hinted Raster/TMP 설정·GUI.matrix 소수 배율·fallback 차단의 실제 동작은 여기서 확인하지 않았다. 빈 glyf인 Bitmap 판정으로 실제 엔진 fallback 동작까지 관찰했다고 주장하지 않는다.

## 카드 실측 검토

metrics를 실제 재생성해 71장, 최대 이름250/효과256 게임px, 가용 폭280/342, 결손0, 1효과줄 높이43≤가정 카드44를 확인했다. 이는 C# `AdamChoiceText.Describe/Card`를 Python으로 옮긴 데이터와 s=2 레이아웃 가정을 사용한다. **실제 Unity 런타임 카드 문자열을 뽑아1:1 비교한 검증이 아니다.** 보조/영구강화·damageMultiplier·새 UI 문자열은 생성 범위를 확인한 뒤 실제 엔진에서 다시 측정해야 한다.

독립 교차 확인으로 Pillow `ImageFont.getlength`를 71장×이름/효과=142줄에 실행했다. 139줄은 stdlib GPOS 포함 값과 같고 bible/jawbone/sling의6→7 이름줄3개는 **Pillow가1px 더 넓었다**(211vs210,223vs222,235vs234). 이 환경은 RAQM=False인 FreeType basic layout이므로 shaping/커닝 경로 차이의 가능성이 있으며 엔진 오류라고 단정하지 않는다. 이 차이를 포함해도 가장 좁은 가용280에 모두 들어가고 현재 인계의 폭 여유 결론은 유지된다. Unity 폭을 이 숫자로 확정하지 않는다.

## 비차단 관찰: standalone checker와 재생성 게이트

manifest의 `roles.title.measured.lineMetricsAtBasePx.lineHeight`만16.0→1.0으로 바꾸면 **standalone check_fonts exit0**이다. checker는 실제 font/contract를 검증하며 생성된 measured 설명 필드를 전부 대조하지 않는다. 같은 사본 source에서 build로 다시 만들면 exit0, **새 manifest는 원래 정상본과 완전히 같고 변조 manifest와 다르다**. `verify_all`의 manifest byte-compare가 이 종류의 변조를 검사한다. 따라서 인계 명령은 checker 단독이 아니라 **재생성을 포함한 verify_all**로 유지한다. 현재 정상 산출물 값이 틀리거나 전체 게이트가 통과시킨 결함이 아니므로 신규 HIGH/MEDIUM으로 세지 않았다.

## 기존03 보존·최종 범위

검토 전후 해시 비교: docs의 `art-concept-03`234파일 +repo의 `art/art-concept-03`202파일 = **436파일 변경0**(캐시 제외). docs03과 repo03의 Bitmap4개 및LICENSE도 서로 바이트 동일했다. 과거 어떤 작업도 변경한 적 없음을 전체 역사로 증명한 것은 아니며, 이번 QA 동안 원본과 저장소 스냅샷이 보존된 것을 확인했다.

이 sidecar는03을 교체하지 않는다. coordinator가 별도 폴더로 통합하고 원본/계약/공식 출처/고지/검사/재생성 명령을 함께 인계한다. 원격zip·캐시·임시로그·개인절대경로를 공개물에 무조건 포함하지 않으며 공개 선별과CI·커밋/push는 root 소유다. 이 보고서는 **오프라인 인계 준비 PASS**이며 반복 QA 횟수나 이전 아트 QA PASS를 Unity 폰트 지원 PASS로 합산하지 않는다.
