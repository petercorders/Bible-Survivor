# R11 Core 회고

2026-11-17. R11 단위 구현과 Core 검증에 한정한다. 전역 학습 상태는 변경하지 않았다.

- 판단/교정: 설치 콘텐츠 끝을 R11로 이동하면서 기존 R10의 미지원R11 예외 기대 하나를 놓쳤다. 최초 관련 검사9/10 exit1을 `evidence/r11-core/related-initial.log`에 보존했고 해당 경계를 R12로 옮긴 뒤 R10 관련5/5 exit0을 확인했다. 신규 R11 5개는 처음부터 통과했다. 기존 각 라운드의 끝/미지원 경계 기대를 설치 범위 확대 때 함께 확인하는 후보로 기록한다.
- 검증 효율: 실제 옛 generic Finish 경로 한 개만 변이해 정산전 완료 회귀를 assertion으로 잡았다. 기존 R10 독립 검증 조합을 반복하지 않았고 최종 전체 Core gate는1회(181/181, 대표 sling seed7 포함)만 실행했다. 문서 때문에 재빌드하지 않았다.
- 모델 적합성: 상속 Sol/provider `gpt-6.1-sol`로 구현했다. 새 worker·Astra·Claude 호출은 없다. root의 독립 리뷰와 Unity 통합은 별도 단계다.
- 토큰 근거: `record_model_usage.py --phase r11-core --checkpoint start|end`를 실행했다. runtime session ID가 root 세션을 가리키므로 공급자 로그의 지연된 같은 세션 interval이며 worker 요청별 독립 귀속값으로 더하지 않는다. 실제 청구·단가·effort 귀속과 동일 수용 조건의 대조군이 없으므로 비용·절감률은 미계산이다.
- 추가 개선 후보: Resume 상한은 설치 콘텐츠 확장 때 CI 변이 anchor와 함께 움직여야 한다. 이번 `completion_resume_refused` literal 변경 필요를 편집 전에 root에 전달했다. 상한을 상수로 바꾸거나 checker를 재설계하는 추가 작업은 이번 소유에 넣지 않았다.

증거: `evidence/r02-20261117T083824205984Z/checks.json`, `evidence/r11-core-mutation/summary.json`, `docs/progress/R11-core.md`. 자연 밸런스·Unity·모바일·성능은 미검증이다. 위 후보는 제안이며 자동 GC 승격이나 전역 규칙 추가를 수행하지 않았다.
