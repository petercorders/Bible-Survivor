# outline 폰트 sidecar 01 GC 근거

2026-11-14 · 독립 QA Codex · 관련 `font-01-review.md`.
**오프라인 인계 준비 PASS, Unity/실기기 NOT_RUN.** 원본·repo·폰트 편집, 전역 GC apply/promote, 커밋/push, Editor 실행, helper 생성은 하지 않았다.

## 발견·재현·해결 근거

| 분류 | 실제 사건/근거 | 처리와 다음 게이트 |
| --- | --- | --- |
| 구분 오류 방지: 테이블 존재≠outline | 기존 03 Bitmap 4개는 glyf 테이블이 있으나 길이 0, EBDT/EBLC 있음 | QA inspect 실제 exit1/bitmap-only. 독립 실제 Bitmap 대체·해시 정합 입력도 GLYF1. 새 outline 4개는 공식 zip과 동일하고 전체 계약 coverage nonempty로 exit0. 실제 Unity 렌더는 별도. |
| 도구화: 빈 glyf/개별 빈 glyph | 제작자 변이 “테이블 존재만 보는 옛 구조”에서 합성 empty_glyf와 sample_empty가 exit0으로 통과 | QA가 전체 8변이를 재실행해 CAUGHT 확인. 실제 Bitmap이 다른 GRID로 걸린 것을 같은 회귀 검출로 세지 않고, 원래 구조 자체를 드러낸 합성 입력 두 개를 근거로 삼음. |
| 독립 입력: 우연한 오류 거절 | 첫 cmap 변이가 A 외 매핑에도 영향을 주어 CMAP+SFNT로 거절 | format4 매핑을 읽어 A만 빠진 format12를 별도 구성. **CMAP 1건만** 나오는 정밀 입력으로 재실행. 포괄적인 비0과 표적 회귀 검출을 구분. |
| 도구화: 예외와 정상 거절 | 음성 입력은 Python crash도 exit1일 수 있음 | QA 오류 15건에 exit1+FAIL/기대 코드+traceback 없음을 요구. build는 EXISTS+파일 SHA 불변을 확인. 하네스 exit0과 자식 CLI exit1을 분리. |
| 출처 검증 | 계약의 공식 release URL/zip SHA를 믿기만 하면 다른 폰트가 섞일 수 있음 | 공식 URL 읽기 다운로드·zip SHA 일치·TTF4개+LICENSE의 5/5 바이트 동일 직접 확인. zip 실제 멤버 누락·고지 바이트 차이는 SOURCE1. |
| 원본 보존 | sidecar가 03 Bitmap을 덮어쓸 위험 | docs03/repo03 총 436파일 전후 해시 변경0. build 독립 음성에서 다른 폰트는 EXISTS1·파일 SHA 불변. 출력 manifest와 폰트 불변 정책을 구분. |
| 실행 환경 | Python urllib TLS CA 실패 | TLS 검증을 끄지 않고 시스템 curl로 같은 공식 URL 다운로드 성공·SHA 검증. 계정/결제/신규 설치 없음. |
| 생성 메타 검증 범위 | measured.lineHeight 변조는 standalone checker에서 exit0 | 같은 source 재생성은 정상 manifest와 같고 변조본과 다름. 재생성 byte compare를 포함하는 verify_all을 인계 진입점으로 유지. 현재 전체 게이트 결함으로 세지 않음. |
| 수치 해석 | coverage11271은 비공백, 공백은 별도 매핑; 렌더11샘플은 전체 한글 렌더가 아님 | 리뷰에서 범위를 11271+공백1로 명시. 모든 glyph loca 범위/계약 한글 전체 coverage/샘플 FreeType 렌더/Unity를 구분. LOW 서술 정밀도이며 폰트 누락 아님. |
| 측정 경로 차이 | Pillow 142줄 검사 중 이름줄 3개가 GPOS 포함 실측보다 1px 넓음 | RAQM=False 관측과 차이값을 기록. 폭280 여유는 유지되므로 HIGH/MEDIUM 아님. Unity 문자열/커닝/실폭은 실행 담당이 대조. |

제작자 RESULT의 샘플 숫자 교정·빈 그림끼리 동일 비교 방지·다른 검사 GRID의 우연한 Bitmap 거절 분류는 제작자 발견 기록이다. QA는 현재 코드와 잉크/대조군11/11·변이 결과를 확인했지만 수정 전의 모든 내부 오류를 직접 재현했다고 기록하지 않는다.

## 실제 확인 묶음

- QA 임시 사본 verify_all 9단계 최종0. 제작자 오류26·양성2, 검사기 변이8, 원본 Bitmap inspect 예상1, outline inspect0, manifest/metrics 바이트 재생성 동일을 실제 재실행했다.
- QA 독립은 정상2+오류15. 그 외 measured 설명 필드의 검사 범위 관찰1건은 비차단 정보로 구분했다. 실제 명령/버전/해시/입력 정의/코드는 review에 영구 기록했다.
- Pillow 4role×11문자열에서 outline@base=Bitmap strike, outline@2base=nearest×2, 잉크 존재, base+1 대조군이 전부 충족했다. 견본 PNG를 직접 열었다. 이 생성은 Pillow 스크립트이며 Claude Design/Unity 화면이 아니다.
- 프로젝트 도구는 이미 build/check/sfnt/measure/verify/음성/변이로 강제된다. 새 전역 문장 규칙을 추가할 구체 이유는 없으며 전역 apply/promote는 미실행이다.

## 다음 담당에게 남는 검증

root는 sidecar 별도 통합·CI·공개물 선별·커밋/push를 담당한다. Unity 담당 ctx_07e314900a51은 실제 폰트 import·AA/point/raster·IMGUI/TMP·소수 GUI.matrix·한글 glyph/fallback·카드 width/height·실제 C# 문자열을 검증한다. Python 카드 재구현71장과 엔진 실문자열1:1 대조는 이번에 하지 않았다. 오프라인 반복 PASS를 합쳐 엔진 지원 PASS로 기록하지 않는다.

## 모델·비용·위임 회고

| 영역 | 판정 | 근거/다음 행동 |
| --- | --- | --- |
| 모델 선택 | 판단 불가 | 현재 Codex 세션, provider 모델ID/effort별 usage 미제공. 대체 모델 동등 품질 근거 없음. |
| 토큰·도구 비용 | 관측상 적합/총량 미측정 | 전체 검증1회가 8변이까지 수행. 독립 입력은 필수 회귀와 측정 차이에 집중. 원본03 전체 화면 QA 재실행/Editor 실행 없음. 토큰 절감률 추정 안 함. |
| 위임 | 관측상 적합 | 같은 독립 QA1명, helper0. 제작자/QA/Unity 실렌더/root 통합 소유 분리. |
| 추가 개선 | 후속 엔진 증거 우선 | 현재 파일·출처·재생성 검사는 재사용 명령 유지. 엔진 지원 불확실성을 추가 정지 폰트 시안으로 덮지 않고 실제 Unity 입력/화면에서 검증. |
