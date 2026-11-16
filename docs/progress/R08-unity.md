# R08 Unity 통합

2026-11-16. R07 완료→R08 요셉/H1~3→일반 성장 고갈 카드/정산→between9 PC 통합·관련 검증 및 독립 코드/UI 대표 범위 리뷰 PASS다. 열린 HIGH/MEDIUM은 없으며 [코드 리뷰](../reviews/R08-unity-code.md)와 [화면 리뷰](../reviews/R08-unity-ui.md)에 후보·선택 횟수/금액과 재사용 근거·미검증을 구분했다. 최신 사용자 범위는 R08 종료까지이며 R09 개발·아트·계획·R01~09 최종 연속검증은 시작하지 않았다. Core sync1회 DLL SHA256은 `4104a40062549cdc557cfbc5e395ae171c57d3dd092aab91c8825ddf606db748`이다. Core제품/수치/저장형식/정책은 수정하지 않았다. Core 관련10/원구조2변이/actual1008kill·180초 timeout CLI 및 외부 아트QA를 재사용하며1020킬 완주로 세지 않는다.

Host/catalog는 R08 normal/round_settlement/between9와 요셉/R09 미구현 대기를 연결했다. JsonUtility가 null 해금을 빈 문자열로 읽어 Core가 거절하는 실제 어댑터 문제를 관련검사에서 잡아 null로 정규화했다.140gold·최대HP회복은 Core 정산만 사용하며 null 해금의 목록 불변을 확인했다. 두 보스 receipt·진화/이력·동적 BossRules·R06 보상명·짧은 death·G3 기존 표시를 보존했다. 승인3PNG/12프레임을 별도 pinned ArtR08 index/loader/importer로 연결하고 H1 small·H2/H3 wide 그림자를 재사용했다. 사슬/곡식은 표현이며 새 위험선·속박/탄막/피해지형은 없다.

일반 고갈은 Core `GrowthGoldChoicePrefix`로 분류하고 `골드 주머니 | 재화`/`영구 골드 +25`로 표시한다. 카드에 가짜 무기/최대레벨/강화 화살표를 붙이지 않는다. 기존 SelectSlot→Host.Choose가 원래 전체 serial ID를 전달하며 Unity의 별도 골드 지급은 없다. 보스의 gold100 후보 카드는 별도 기존 표시를 유지한다.

관련25시험 PASS: Core가 생성한 gold25 full-ID 후보의 무지급 Load·선택once·실패payload 동결/동일 retry·stale ID 거절·XP/레벨 보존, 재화/보스gold100 표시 구분, 두 receipt/form·midWorld Load, 성장 전140gold 보류·정산once/null해금·완료retry 및3클립 mapping/fps/shadow를 검증했다. 처음 null 해금 어댑터 실패(21/25 실패)는 로그/XML로 보존했다. 고정소스 최종 Unity Edit121/Play31을 한 번씩 PASS했고 compile·단일 mac Development build·project/portable/diff는 exit0다. 추가 빌드/검사 행렬/새 helper·도구는 없다.

단일 앱의 기술 seed는 이전 R07 완료 evidence JSON·HP10000·기존 장비/form·6보조 슬롯이다.125초 예정 actor 직접 처치/XP drop 폐기·Session5초 뒤 새 Core World/첫weapon1000으로 H혼합을 준비했다. 실제 추적3초·pause/midWorld 정확 disk Load와3종 일반 화면을 확인했다. 이후 유효한 현재 장비를 Core.TryChoose로 최대화하고 직접 Kill/Collect의 실제 XP로 Core가 `growth_gold_25:16`을 생성했다. Choices/PendingLevels/serial 후보를 주입하지 않았다. 생성과 Load 후1227gold 불변, 실제 SelectSlot 선택 뒤1252(+25)·stale ID 거절·선택 후 Load를 확인했다.

정산 준비의 직접 처치 후 기준1266gold에서 실제 PendingLevels2개를 각각 Host.Choose로 처리해 gold25 선택2회(+50), 완료140을 별도로 더한1456gold·최대HP회복/between9를 assert했다. 후보 수와 pending level·실제 선택 횟수를 구분했다. 첫 일반 재화 선택 때 level17/pending1/serial16, 완료는level19/pending0다. 해금 목록과 두 receipt/form·PreviousNormalRound7은 그대로이며 새 process Run/Profile 정확Load/중복 완료 거절도 PASS했다. 앱의 직접 처치/XP·최대 장비/timeout은 기술 준비이며 자연 완주나 app1008/1020실킬 근거가 아니다.

ignored 근거 `evidence/r08-unity-integration-20261116/`의 binding/handoff·실명령 exit/PID·XML/log·연속 trace·midWorld/growth-choice/expected state·정확3화면과 보존/복구/usage를 인계한다. 실제 view_image로 일반660/고갈660/완료1100 세 화면을 확인했다. 앱 identity `com.biblesurvivor.adam.r02artqa.r0827eef7512bf4`, probe75338/capture75352/reload75356은 모두exit0이며 자기 저장3종만 absent로 복구/no-own-writer를 기록했다. 보호 preview/unknownQA/iOS·저장은 조회조작하지 않았고 기존871파일·Core handoff·원본/폰트/다른 담당 변경을 보존했다.

R07 이전 보고의 후보3개를 성장3회로 오독한 사실은 seed 대조 중 발견해 root에 보고했고, root가 실제 Choose1회/level15→16으로 문서·리뷰를 정정했다. R07 raw 근거/앱은 재실행하지 않았고 root 문서 편집을 보존했다. 이번 R08은 PendingLevels와 실제 loop 횟수를 따로 기록했다.

상속 Sol/실제 provider gpt-6.1-sol, 추가worker/Astra 없음. 자기 usage 시작 event 시차 때문에 정확 R08 단독 귀속·청구/절감률·effort는 미측정이다. 자연밸런스/native/mobile/device/performance·전프레임/전조합은 NOT_RUN이다. 제품·Editor·Player 소유를 반환하며 root가 R08 독립 코드/UI·최종GC·commitpush·CI 후 정지한다.
