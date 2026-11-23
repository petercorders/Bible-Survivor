# 영구 스킬 #47 개발 상태

창세기부터 R180까지 설치된 캠페인과 공통 120종, 무기 전용 300종의 첫 출시 범위를 대상으로 한다. 설계는 Draft #43의 0d020b5fbce79a5f4112eca7cc2b3c87c085066e, 기존 개발 기준은 71800d9165244934260c0ea2cd6216a5faec7804다.

| 확인 | 상태 | 재현 |
|---|---|---|
| 카탈로그 | 420개 ID, 442 구매 단계, 22개 트리와 378,050골드. 선행 도달·R60/R180 투자 표본·레거시 가격 일치 | Core PermanentSkillCatalogTests |
| 저장 거래 | 스키마 1~3의 읽기와 원본 보관, 표시한 가격·랭크·환불 목록 검증, 원래 단가 환불, 동일 요청, 오류 후 주 파일 재확인 | Core PermanentSkillSaveTests |
| 초기 효과 | P01 0.74 거리 보석, P02 물맷돌 기본 연사 저장·재개, 진화형 8발 계산. 체력·방어 단계의 합산 조건 | Core PermanentSkillCombatTests |
| 공통 효과 | 회복·기동·경험치·수집을 실제 전투에 연결. 이동 방향·보석·원본 처치·즉시 회복 상태를 보존하고 다음 라운드에서 초기화 | Core PermanentSkillCombatTests 및 PermanentRecoveryRulesTests |
| 신규 무기 | 7종의 기본·진화 상태와 공격 전략. R03 통제 시험에서 각 형태를 장착해 월드 저장 후 같은 입력으로 복원 | Core ExpandedWeaponProgressionTests와 무기별 시험 |
| Unity 상점 | 제목·일시정지 등 진입 화면, 22개 트리 탐색, 상세·선행 이동, 구매·전체 환불과 오류 재시도. 현재 구매 허용 92개 ID | Unity AdamPermanentSkillHostTests, macOS QA Player 키보드 증거 |
| 미완료 | 구매 미연결 328개 ID, 모든 노드의 효과별 재현, 7무기의 자연 해금·진화와 Player 전투 화면, R180 설치 캠페인 비교, 포인터·모바일 | 고정 설계의 save-and-acceptance.md |

기준 Unity PlayMode는 38/40이며 #46의 두 시각 효과 단정을 같은 기준본에서 다시 확인했다. 변경 빌드의 전체 결과는 별도로 기록한다. macOS QA Player는 독립 저장 경로로 실행했고, 실제 키보드로 구매 취소와 구매, 환불 취소와 환불을 확인했다. 테스트 저장의 잔액은 600 → 500 → 600이었다. 구매 전후 효력은 Core와 Unity Host에서 재현했다.

Claude Design 담당은 UI·무기 시안 14개를 제작했다. Unity에서는 신규 무기 7종의 상태를 검증용 윤곽선으로 그린다. 디자인 에셋의 실제 스프라이트 프레임 제작, 독립 검수, 임포트가 남아 있다.
