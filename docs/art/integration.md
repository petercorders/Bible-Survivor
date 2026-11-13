# 아트03 저장소 통합·GC

2026-11-14. 독립 QA 반복02에서 A03-01/02 해결 및 오프라인 인계 준비 PASS를 확인했다. 검토된 패키지는 `art/art-concept-03/`, 이전 형식 검증에 필요한 선별 fixture는 `art/art-concept-01/02/`다. 원본 폴더를 변경하거나 이동하지 않고 복사했다.

## 통합 검증

export/src/scripts/third_party/review의 파일198개를 원본과 SHA256 대조해 모두 같음을 확인했다. canvas·캐시·비공개 blob·임시 실행 로그는 복사하지 않았다. 문서는 선별 복사 후 개인 절대 경로·비공개 캔버스 URL을 가렸다. 비공개 blob 경로 형식을 설명하는 문장만 남으며 실제 식별자는 없다. 폰트 고지·출처·라이선스는 원본 그대로 보존했다.

`tools/check_art.py`는 임시 사본에서 catalog/계약 검사·오류 입력41종·검사기 회귀5종을 실행한다. 기본 검사는 Python 표준 라이브러리만 사용한다. `--full`은 원본 export의 재생성 비교를 먼저 수행한 뒤 전체 검증·legacy 변환·리뷰 재빌드를 실행한다. build가 export를 덮어써 차이를 숨기지 않도록 비교 순서를 고정했다. 로그와 JSON은 ignored `evidence/art-integration-*`에 보존한다.

원격 `.github/workflows/art.yml`에 기본 게이트를 연결했다. 로컬 커밋 훅도 같은 기본 명령을 실행하도록 작업트리에 연결했으며, 소스 훅·Unity 체크의 커밋은 Unity 통합 단계에서 함께 관리한다. 실제 원격 실행 결과는 push 후 확인한다. 원격 검사는 오프라인 패키지 검증이며 Unity·모바일 실행 검증을 하지 않는다.

커밋 `b24881bd448d194e581fbebc2f1e95d59159509a`를 원격 브랜치에서 확인했고 [아트 CI](https://github.com/petercorders/Bible-Survivor/actions/runs/43460878597)와 [코어 CI](https://github.com/petercorders/Bible-Survivor/actions/runs/43460878230)는 모두 성공했다. 이 SHA의 CI는 아직 Unity Editor 실행이나 후속 Unity portable 게이트를 포함하지 않는다.

저장소에서 `python3 tools/check_art.py --full`을 실행해 exit0을 확인했다(`evidence/art-integration-20261113T161157458265Z`). 독립 통합 리뷰에서도 기본 게이트0 및 manifest 오류/하위 시험 exit9/재생성 exit7의 wrapper 실패 전파를 확인했다. 재생성 실패 때 후속 build가 실행되지 않아 원본 차이를 숨기지 않았다. 추가 HIGH/MEDIUM 없음. [통합 리뷰](../reviews/art-integration.md)를 함께 보존한다.

## GC 판단

잘못된 최상위 계약·없는 폰트를 놓친 문제는 명시 계약과 음성 CLI/검사기 변이로 해결됐다. 같은 QA가 옛 검사기 전체와 새 검사기에 동일 입력을 넣어 0→1을 확인했다. 새 규칙 문장이나 전역 GC 엔트리를 추가하지 않았다. 이전 QA의 픽셀/동작 검토는 변경되지 않은 그림에만 재사용하고, 이동 후 경로·누락·재생성은 저장소 통합 검사로 따로 확인한다.

담당 분리는 Claude 제작/디자인 협업 → 독립 아트 QA → coordinator 통합 → 별도 통합 코드 리뷰로 유지했다. 총 토큰·비용·모델별 usage와 절감률은 미측정이다. 반복01·02의 실패와 재검토는 선별 리뷰/GC 문서에 남겼다.

후속 outline 폰트·Unity 임포터·실제 게임 표현은 별도 작업이다. 현재 Bitmap TTF를 Unity 지원 완료로 보고하지 않는다. 최종 게임 그래픽·기기·성능은 NOT_RUN이다.
