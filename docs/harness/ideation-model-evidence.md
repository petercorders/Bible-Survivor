# 아이디어 모델 선택의 연구 근거

확인: 2026-11-14. 사용자 지시는 Astra를 어려운 검증 또는 Sol로 해결하기 어려운 문제에만 사용하는 것이다. 아래 조사는 아이디어에 Astra를 자동 배정하는 변경 승인이 아니다.

## 직접 모델 비교: UI 디자인

[Kombai Design Creativity Bench](https://kombai.com/benchmark/)와 [preprint 원문](https://kombai.com/papers/design-creativity-bench.pdf)은80개 과제·도메인별2개로160 UI brief를 평가했다. 논문의 Astra/Sol은 모두 max effort다.

| 지표 | GPT-6 Astra | GPT-6.1 Sol |
| --- | --- | --- |
| 공개 leaderboard 종합 creativity |65.5|63.7|
| 타 모델과의 디자인 차이(originality) |0.646|0.642|
| 같은 모델의 도메인 변화 대응(creative range) |0.472|0.487|

Astra는 이 평가의 종합/독창성 점수가 조금 높지만 변화 범위는 Sol이 높다. 종합 점수는 benchmark 자체 조합 지표이며 범용 창의성 점수나 게임 재미가 아니다. 논문 독창성95% CI는 Astra[0.631,0.661],Sol[0.628,0.657]이며, 이 평균 순위만으로 Astra의 확실한 우월성을 단정하지 않는다. 제품 회사의 preprint/벤치마크이고 게임 기획·Sol medium과 Astra high의 실전 비교를 검증한 것은 아니다.

## 빅테크: 모델뿐 아니라 과정

[Microsoft Research: Multi-agent AI systems outperform human teams in creativity](https://www.microsoft.com/en-us/research/publication/multi-agent-ai-systems-outperform-human-teams-in-creativity/), [arXiv 원문](https://arxiv.org/abs/2605.17885),2026년5월. 여섯 문제 해결 과제에서 multi-agent 아이디어4,541개·사람팀341개를 비교해 새로움 중심의 이득을 보고했다. 모델 선택과 토론 구조를 별도 설계 축으로 설명한다. 사전공개 연구이며 Astra 대 Sol 직접 비교나 게임의 재미/총토큰 절감 증거가 아니다.

[Google Research AI co-scientist](https://research.google/blog/accelerating-scientific-breakthroughs-with-an-ai-co-scientist/),2025년2월. Gemini 기반 생성·반성·순위·발전 등 역할을 분리해 가설을 만들고 평가·개선한다. 과학적 가설 생성 시스템이며 특정 단일 모델의 일반 아이디어 우월성으로 환산하지 않는다. 우리에게는 생성과 비판/검증을 분리하는 과정 설계의 참고다.

## 주의할 연구 결과

[Doshi & Hauser, Science Advances,2024](https://discovery.ucl.ac.uk/id/eprint/10195027/). 단편소설 실험에서 AI 아이디어 접근은 개인 작품의 창의성 평가를 높였지만 작품 간 유사성도 높였다. Astra/Sol 비교가 아니며, AI를 반복 호출하는 것만으로 다양성이 보장되지 않는다는 참고다.

## 프로젝트에 적용할 판단

Sol로 서로 다른 제약/관점의 소수 후보를 먼저 만들고 재미·성경 소재 연결·모바일 가독성·제작 비용을 별도 비교한다. Astra는 사용자 지정대로 어려운 검증 또는 Sol이 해결하지 못한 설계 모순에만 올린다. 모든 아이디어를 두 모델로 중복 생성하거나 대규모 agent 토론을 기본으로 만들지 않는다. 위 연구 수치/비용을 우리 프로젝트의 재미 향상·비용 절감 실적으로 옮겨 쓰지 않는다.
