# 성경 sidecar Unity 연결 회고

2026-11-16 · 구현자 회고이며 최종 독립 UI 리뷰/GC는 root 후속이다. 전역 학습 상태를 수정하지 않았다.

- 경계: 승인 그림 두 개만 별도 sidecar로 연결하고 Art03 원본79/manifest/source/GUID와 기존 clip 계약을 보존했다. 제품 loader는 한 줄에서 override를 적용하고 importer/검증은 sidecar 경로에 한정했다. Core 병행 변경은 선택 DLL과 분리해 보고했으며 sync하지 않았다.
- 검증 선택: compile·새 관련2개 검사·대표3화면·실제 F6 처리 경로의 합성 입력만 실행했다. 모든 픽셀/프레임/해상도 조합이나 전체 suite를 반복하지 않았다. 저효과 책 유지와 토글 중 frozen State/World 동등을 확인했다. 실제 자연 플레이 가독성/OS 입력/모바일/성능은 남긴다.
- QA 초기화 실수: 비활성 AdamGame의 private Update를 시험하며 OnDisable이 EnhancedTouch를 끈 사실을 놓쳤다. 첫 실패의 정상 화면을 재사용하고 입력 초기화만 보완했다. 다음 재빌드가 이미 수정된 QA scene에 driver를 또 붙여 두 fixture writer가 stale revision을 일으켰다. 원본 scene부터 매번 복사하고 duplicate guard를 추가한 뒤 같은 제품 코드로 통과했다. 사용자 저장/제품 저장 알고리즘 실패로 오인하지 않는다.
- 효율/교정: 실제 새 실패 때문에 QA 앱 빌드만 추가했다. 최종 정상 화면은 기존1+신규2로 총3장이고 두 번째 실패 실행은0장이다. 준비 fixture의 lifecycle/scene freshness를 사전에 확인하는 도구화 후보이며 자동 전역 규칙 승격은 하지 않는다. 추가 빌드 비용을 효율 개선으로 포장하지 않는다.
- 모델: 별도 모델 요청/위임 없이 상속 기본값으로 작업했고 실제 모델은 gpt-6.1-sol이다. provider 세션 start/end만 합산하고 디자인/독립 아트 QA의 자기보고 usage와 섞지 않는다. effort·실제 청구액·절감률은 미측정이다.
- 보존: 자신의 세 새 QA identity만 정확 자식 exit/writer 부재 뒤 원복했다. 지정 사용자 preview/old unknown/iOS는 조회하지 않았다. Unity 자동 비override metadata/빈 icon만 baseline으로 복구하고 나머지 담당자의 Core/시험/도구 편집은 건드리지 않았다.
