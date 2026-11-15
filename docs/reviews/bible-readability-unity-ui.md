# 성경 sidecar Unity 독립 시각 리뷰

2026-11-16 · **지정 대표3화면 범위 PASS**, 막는 HIGH/MEDIUM 없음. 추가 필수 그림/제품 수정 요청은 없다.

`evidence/bible-sidecar-unity-20261116/handoff.md`와 `docs/progress/bible-readability-unity.md`를 읽고 아래3장을 view_image로 직접 열었다. 외부 후보1 독립 아트 PASS를 재사용했고 새 캡처·픽셀/해시 행렬·재빌드·전면 검사는 수행하지 않았다.

| 대표 실제 화면 | 직접 관측 |
|---|---|
| initial-input-setup/capture/01-660-normal.png | HUD 효과 기본. 표지 십자와 종이 가장자리가 보이는 책3권, 책 밖의 별도 사각 폐곡선 없음 |
| capture/02-660-low-after-f6.png | HUD 효과 낮음. core 책3권이 그대로 보이고 후광 없는 상태에서도 책으로 식별 가능 |
| capture/03-1100-normal-after-f6.png | HUD 효과 기본 복귀. 확대된 책의 표지·어두운 책등 띠·밝은 종이 단면이 구분됨; 후광이 별도 상자나 장판으로 보이지 않음 |

책 자체의 사각 표지/종이 테두리는 남지만, 원래 좁은 책등 밖에서 고정된 금색 박스가 도는 문제와 구분된다. 후광은 매우 옅어도 이번 장식 최소화 의도에 맞으며 밝기를 더 올릴 필요를 발견하지 못했다. 660에서도 책은 배경·플레이어와 구분된다.

세 화면은 같은 고정 시점의 staged 성경6/3권·time0.1/frame0 표본이다. 첫660 기본화면은 초기 실행에서 이미 저장된 것을 재사용했고, 이후 입력 준비 실패/QA driver 중복 실패는 구현자 인계에 보존돼 있다. 첫/최종 제품 Runtime·선택 DLL 동일이라는 구현자 결속 근거를 재사용했으며 QA가 첫화면을 최신앱에서 재촬영하거나 별도 전해시 대조한 것은 아니다.

compile0/Edit2/2/build0, 실제 F6 처리 경로의 **앱 내부 합성 키**, 토글 전후 State/World 정확 동등은 구현자 근거다. 본인 엔진 실행으로 합산하지 않으며 화면만 보고 저장 동등이나 OS native 키 전달 성공까지 추론하지 않는다. 화면에서 확인한 HUD 순서는 기본→낮음→기본이다.

기존 Art03 원본79/manifest를 유지하고 sidecar 두ID만 선택하는 통합 경계와 core 책/optional 후광 역할을 그대로 수용한다. 새 게임 지원·전투 수치 변경·원본 교체 승인은 아니다. 진행 중인 Core/R03 소스가 아니라 선택된 기존 DLL의 이 표시 연결 범위다.

**NOT_RUN:** 자연 플레이·밀집 전투 가독성, 두 프레임 전체 실제 렌더, OS native 조작, 모바일/실기기, 파티클/모든 배경 조합, 성능·접근성 실측. 앱/Editor/GUI/사용자PC·iOS·저장은 조회·조작하지 않았다. 원격 Design 실행/렌더는 독립 미확인이며 스크립트 PNG와 캔버스 호스팅을 구분한다.

root의 최종 채택·GC·후속 커밋/업로드 판단이 남는다. QA 자체 provider 모델 gpt-6.1-sol의 bible-sidecar-ui-review usage start/end는 temp ledger에 기록했으며 비용·절감률은 계산하지 않았다.
