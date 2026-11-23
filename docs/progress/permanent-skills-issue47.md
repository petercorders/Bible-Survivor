# 영구 스킬 #47 개발 상태

창세기부터 R180까지 설치된 캠페인과 공통 120종, 무기 전용 300종의 첫 출시 범위를 대상으로 한다. 설계는 Draft #43의 0d020b5fbce79a5f4112eca7cc2b3c87c085066e, 기존 개발 기준은 71800d9165244934260c0ea2cd6216a5faec7804다.

| 확인 | 상태 | 재현 |
|---|---|---|
| 카탈로그 | 420개 ID, 442 구매 단계, 22개 트리와 378,050골드. 선행 도달·R60/R180 투자 표본·레거시 가격 일치 | Core PermanentSkillCatalogTests |
| 저장 거래 | 스키마 1~3의 읽기와 원본 보관, 표시한 가격·랭크·환불 목록 검증, 원래 단가 환불, 동일 요청, 오류 후 주 파일 재확인 | Core PermanentSkillSaveTests |
| 초기 효과 | P01 0.74 거리 보석, P02 물맷돌 기본 연사 저장·재개, 진화형 8발 계산. 체력·방어 단계의 합산 조건 | Core PermanentSkillCombatTests |
| 공통 효과 | 회복·기동·경험치·수집을 실제 전투에 연결. 이동 방향·보석·원본 처치·즉시 회복 상태를 보존하고 다음 라운드에서 초기화 | Core PermanentSkillCombatTests 및 PermanentRecoveryRulesTests |
| 기본 치명타 | 확률과 배율의 첫 두 노드를 원본 타격에 연결. 무기별 난수 상태를 저장하고 기존 월드 생성 난수와 분리 | Core PermanentSkillCombatTests의 턱뼈 실타격 및 복원 |
| 신규 무기 | 7종의 기본·진화 상태와 공격 전략. R03 통제 시험에서 각 형태를 장착해 월드 저장 후 같은 입력으로 복원 | Core ExpandedWeaponProgressionTests와 무기별 시험 |
| 무기 기초 효과 | 10종 무기의 기초 1~5를 연결. 성경 첫 주기와 진화 파동, 턱뼈 외곽 타격, 투사체 치수·속력, 번개·외침·불기둥·병거의 예약 상태를 확인 | Core PermanentSkillCombatTests 및 무기별 시험 |
| 추가 무기 분기 | 성경 회전, 턱뼈 반각, 물맷돌 조준·치명, 번개 표적·연쇄, 외침과 불기둥 효과, 지팡이 진화 수로, 십자가 구간별 속력·예산, 사자 표적·첫 공격·도약 | Core PermanentSkillCombatTests 및 무기별 시험 |
| 공통 투사체 | 속력·충돌 반경·유한 관통. 물맷돌과 십자가에 일관되게 연결하고 물맷돌의 비행 거리와 저장된 관통 상태를 확인 | Core PermanentSkillCombatTests |
| 공통 공격 주기 | 최초 원본 공격, 피격, 경과 시간, 정예 처치, 무피격 누적, 보스 본체 활성, 연사 및 원본 반복 간격을 새 도전의 실제 공격 일정에 반영 | Core PermanentSkillCombatTests와 PermanentTempoRulesTests |
| 불수레 추가 효과 | 주행 거리·평행 간격·차체 밀침과 진화 회오리 반경·불길 수명을 저장하고 Unity 경계 표시가 해당 개체의 값을 읽음 | Core ExpandedElijahChariotTests 및 PermanentSkillCombatTests |
| 공통 공격 범위 | 타격 치수, 근접 반각, 편도 거리, 지정 위치 원형 반경, 회전 거리 및 발사체 충돌 반경을 구분. 물결·불수레에도 편도 거리를 적용하고 이동 중 생긴 잔불에는 지정 위치 보너스를 적용하지 않음 | Core 무기별 시험과 PermanentSkillCombatTests의 월드 재개 |
| 사자 추가 효과 | 개별 포효 일정·반경·밀침·타격 대기, 진화 마지막 도약 처치의 추가 도약과 진화 할퀴기 반경을 연결 | Core ExpandedDanielLionTests 및 PermanentSkillCombatTests |
| 전역 예약 | 원본 발사체 192칸과 보조 구역 16칸을 공유하는 경로의 일부를 상한에 맞추고, 병거 보조 슬롯 부족 시 차체를 유지 | Core ExpandedElijahChariotTests 및 ExpandedFirePillarTests |
| 설치 캠페인 | v2에서 180개 라운드와 보스 60개를 통제 입력으로 진행. 책 진입과 대표 보스 월드·보상·R180 정산을 저장 후 복원 | Core ExpandedCampaignReplayTests |
| Unity 상점 | 제목·일시정지 등 진입 화면, 22개 트리 탐색, 상세·선행 이동, 구매·전체 환불과 오류 재시도. 현재 구매 허용 178개 ID·200개 구매 단계 | Unity AdamPermanentSkillHostTests, Editor 포인터 및 macOS QA Player 키보드 증거 |
| 미완료 | 구매 미연결 242개 ID, 모든 노드의 효과별 재현, 7무기의 자연 해금·진화와 Player 전투 화면, 네이티브 포인터·모바일 | 고정 설계의 save-and-acceptance.md |

기준 Unity PlayMode는 38/40이며 #46의 두 시각 효과 단정을 같은 기준본에서 다시 확인했다. 변경 빌드의 전체 결과는 별도로 기록한다. macOS QA Player는 독립 저장 경로에서 실제 키보드로 구매·환불 확인과 각각의 취소를 시험했다. 테스트 잔액은 600 → 500 → 600이었다. 660×440 Editor 포인터 시험은 일시정지에서 진입해 선행 이동과 구매·환불의 확인과 취소를 재현했다. 기존 v1 도전은 시간과 ID를 유지하고 재개했다.

`python3 tools/report_permanent_skill_coverage.py`로 442개 구매 단계의 명세·가격·상점 연결·실행 경로를 TSV로 출력한다. 상점 연결과 단계별 인수 검증은 각각 기록한다.

Claude Design 담당은 UI·무기 시안 14개를 제작했다. Unity에서는 신규 무기 7종의 상태를 검증용 윤곽선으로 그린다. 실제 스프라이트 프레임 제작 방향은 #47 Q1에서 승인 대기 중이며, 픽셀 원본 제작·독립 검수·임포트가 남아 있다.

번개 `weapon.lightning.a.03`의 순환 배정은 고정된 후보와 수량 상한 4에서 기본 배정과 같은 결과를 낸다. `a.04`를 함께 쓰면 재조준 선택 순서의 차이를 시험할 수 있다. 단독 구매 효과의 정의가 정리될 때까지 두 노드는 상점에서 구매할 수 없다.
