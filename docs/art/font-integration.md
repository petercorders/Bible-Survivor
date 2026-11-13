# 외곽선 폰트 sidecar 통합·GC

2026-11-14. [독립 폰트 QA](reviews/font-01-review.md)에서 새 HIGH/MEDIUM 없이 오프라인 인계 준비를 통과했다. 공식 Galmuri2.40.4 zip SHA256과 TTF4종/라이선스5파일 바이트 일치를 독립 확인했다. 패키지는 `art/art-unity-font-01/`이며 Bitmap을 포함한 기존03은 변경하지 않는다.

통합 시 source/export/fonts/scripts/tests/review와 인계 문서 총20파일을 원본과 SHA256 대조해 모두 같음을 확인했다. 결과 로그·캐시·공식zip은 공개 패키지에 넣지 않았다. QA/GC 문서는 선별 복사하고 개인 절대경로를 가렸다. 폰트의 OFL1.1 고지와 출처·해시를 보존했다.

`tools/check_fonts.py`는 임시 사본에서 manifest·metrics 재생성 비교, SFNT/글리프·role·해시·라이선스, 오류 입력과 Bitmap의 outline 오인 거절을 실행한다. standalone checker가 measured 설명 필드를 전부 검증하지 않으므로 반드시 재생성 비교를 포함한다. 기본 게이트는 네트워크/서드파티 패키지를 요구하지 않고 zip 관련 검사와 변이/Pillow를 미실행으로 명시한다. 로컬 `--zip`은 공식 아카이브 대조를, `--full`은 검사기8변이와 Pillow 렌더 비교를 추가한다. 원격 아트 CI와 커밋 훅에는 기본 게이트를 연결했다.

이동 후 기본 게이트 exit0을 확인했다(`evidence/font-integration-20261113T165221361439Z`). [독립 통합 리뷰](../reviews/font-integration.md)도 정상0·오류24거절·zip2미실행을 확인했고, measured 설명 필드 변조는 standalone checker0과 달리 재생성 비교/전체 wrapper1로 거절됨을 직접 재현했다. 추가 HIGH/MEDIUM 없음. 반복된 전체 검사기 변이/공식zip 검증을 통합 리뷰가 새로 실행한 것처럼 합산하지 않는다.

이번 GC는 Bitmap의 빈 glyf와 외곽선 지원을 분리하고, 예외 종료를 의도된 거절로 세지 않으며, C#을 Python으로 옮긴 카드 실측과 엔진 실문자열을 구분하는 기존 도구/보고를 유지한다. QA의1px 측정차이는 폭 여유를 바꾸지 않는 관찰로 보존한다. 같은 규칙 문장을 전역에 추가하지 않았다. 모델별 사용량·토큰/비용·절감률은 미측정이다.

역할별 basePx12/12/10/8과 정수배/AA-off는 오프라인 근거가 있는 엔진 적용 후보다. 실제 TrueType 임포트·한국어 렌더·OS fallback·줄바꿈·작은 화면·기기는 Unity 담당이 검증한다. 이 통합은 엔진 지원 PASS를 뜻하지 않는다.
