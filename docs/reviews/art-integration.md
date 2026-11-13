# 아트03 저장소 통합 독립 검토

2026-11-14 KST. 판정: **PASS — 이번 통합 범위에서 추가 HIGH/MEDIUM 없음**. 원본 그림의 품질 QA는 반복02 독립 리뷰의 별도 결과다. 이번 검토는 저장소 배치, 검사 래퍼, 실패 전파, 공개 자료 범위와 라이선스 보존을 대상으로 한다. 원본·검사 도구·Unity 프로젝트는 수정하지 않았으며 이 보고서만 작성했다.

## 검토 결과

- `tools/check_art.py`는 아트 트리를 임시 폴더로 복사하고 실제 subprocess 종료 코드를 검사한다. 첫 실패에서 중단하고 JSON/log를 남기며 최종 실패를 exit1로 전파한다. 기본 경로는 catalog/계약 → 오류 입력41종 → 검사기 변이5종이다.
- `--full`은 committed export의 재생성 비교를 `verify_all.py`보다 먼저 실행한다. 따라서 verify_all의 export 재빌드가 원래 파일의 차이를 숨기지 않는다. 원본 아트는 검사 중 덮어쓰지 않는다.
- 최종 원격 아트 게이트는 `.github/workflows/art.yml`로 분리되어 Python 기본 검사만 실행한다. Unity 시제품의 별도 상태에 종속되지 않으며 Unity/모바일 PASS를 주장하지 않는다. 로컬 `tools/hooks/pre-commit`은 각 실패 코드를 전파하고, 해당 통합 자료가 있을 때 아트/portable Unity 검사를 실행한다. 훅 실제 설치와 원격 job 결과는 coordinator의 별도 통합 확인 대상이다.
- 현재 아트 파일278개를 조사해 canvas/cache/results/pyc 파일이 없음을 확인했다. 텍스트 파일에서 실제 개인 절대 경로, 비공개 캔버스 URL 또는 blob 식별자는 발견하지 않았다. 문서의 `/_blob/<id>`는 제외 대상의 형식 설명이다.
- manifest/checker/contract의 SHA256이 `docs/art/reviews/iteration-02-review.md`의 세 값과 모두 일치한다. 기존 QA의 승인 대상과 현재 핵심 인계 파일이 같다.
- `art/README.md`와 `docs/art/integration.md`는 원래 문서의 경로를 `art/` 아래로 옮긴 사실, 이전 리뷰 폴더의 대응, 선별 legacy fixture, 제외된 원본 실행 로그/캔버스를 설명한다. 변환·리뷰 스크립트의 sibling `art-concept-01/02` 참조는 현재 배치와 일치한다. 과거 결과 문서의 results/canvas 경로는 역사적 근거이며 현재 저장소에 그 자료가 있다고 판정하지 않았다.
- Galmuri 2.40.4의 TTF4개와 원저작권/SIL OFL1.1 전체 고지가 보존되어 있고, UI/manifest에 배포판 출처와 라이선스 참조가 남아 있다. Bitmap 폰트의 Unity 지원은 명시적으로 미검증 상태다.

## 실제 독립 실행

새 격리 복사본에 `art/`, canonical catalog와 래퍼만 복사했다. 저장소 원본, Editor, Player는 실행·변경하지 않았다. 기본 명령은 `python3 tools/check_art.py`이며 실패 입력은 이 복사본에만 적용하고 복원했다.

| 실행 | 실제 결과 |
|---|---|
| 변경 없는 기본 게이트 | wrapper0; catalog0, 41 negative0, 5 mutants0 |
| manifest만 `{}`로 변경 | 첫 checker1 → wrapper1; 후속 검사 미실행 |
| 후속 negative-test 스크립트만 exit9로 교체 | checker0 → test9 → wrapper1; mutation 미실행 |
| regen 스크립트만 exit7로 교체하고 `--full` 실행 | checker0 → regen7 → wrapper1; verify_all 미실행 |

격리 근거: `<temporary>/bible-art-review-iuuvo9pe/`의 `baseline.log`, `bad-manifest.log`, `bad-test.log`, `bad-regen.log` 및 각 `evidence/art-integration-*/summary.json`. 실제 임시 경로는 coordinator에게 인계했다. 의도적 후속 실패도 성공으로 흡수되지 않고, full 재생성 비교 실패 이후 export 빌드가 실행되지 않음을 확인했다.

coordinator의 전체 `--full` exit0 및 원본198파일 일치 근거는 `evidence/art-integration-20261113T161157458265Z`의 별도 결과다. 이 리뷰어가 전체 재생성/legacy 변환/리뷰 미디어 재빌드를 중복 실행했다고 주장하지 않는다.

## 검증 경계

실제 Ubuntu GitHub job은 push 후 별도 확인해야 한다. 원본 그림의 직접 시각 QA, 브라우저 움직임, Unity 임포트/폰트/Editor/Player, Android/iOS·실기기·성능은 이번 리뷰에서 **NOT_RUN**이다. 토큰·모델 사용량은 미측정이다.
