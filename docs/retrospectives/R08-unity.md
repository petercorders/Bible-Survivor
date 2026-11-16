# R08 Unity 구현자 회고

2026-11-16. root 독립 리뷰/최종GC 전 작성자 회고이며 전역 학습 상태는 수정하지 않았다.

- Core10/2원구조변이/actual1008kill timeout CLI와 아트QA를 재사용했다. 관련25·고정소스Edit121/Play31·단일 정상 macbuild·한기록/3화면으로 끝냈다. R07에서 확인한 Reload pause와 유효 seed/6보조 슬롯을 먼저 대조해 이번 앱의 추가 교정 빌드는 없었다.
- null 해금은 Unity JsonUtility에서 빈 문자열이 되어 Core의 round identity 검증에 걸렸다. 관련검사의 실제 실패를 보존하고 어댑터에서 null로 정규화했으며 Core validation/저장형식은 건드리지 않았다. 일반 고갈은 Core prefix·전체 serial ID·기존 Host 선택만 사용하고 텍스트를 재화/영구골드25로 분리했다.
- 유효 장비 최대화와 직접 XP수집으로 Core가 만든 후보를 사용했다. 생성/Load 무지급·첫 일반25once, 정산PendingLevels2/실제선택2의50골드와 완료140을 따로 확인했다. 최대 장비/HP10000·직접kill/drop·timeout·새World 준비를 실제 자연 완주와 구분했다.
- R07 보고의 PendingChoices.Count3을 실제 성장3회로 오독했음을 이번 seed level 대조로 발견했다. root에 즉시 정정 보고했고 root가 후보3/Choose1·level15→16으로 문서/리뷰를 바로잡았다. raw 근거는 보존하고 재검사를 늘리지 않았다. 이번에는 후보수·PendingLevels·실제 loop횟수를 혼용하지 않았다.
- 자기 PID/저장3종 absent 복구·보호 identity 불조회, 기존871파일/원본/폰트/Core/다른 담당 문서 편집을 보존했다. 최신 사용자 범위 축소에 따라 R08만 완료하고 R09/연속 최종 검증/추가무기·모바일·후속책을 시작하지 않는다. root의 리뷰/GC·commitpush·CI 뒤 정지한다.
- Sol 기본·자기 start/end만 기록하고 비용/절감률·effort·자연밸런스/native/mobile/performance를 추정하지 않았다. helper/새 검사기/전행렬·새 전역 규칙은 추가하지 않았다.

## Coordinator GC와 종료 범위

R08을 막은 실제 일반 성장 고갈은 장비 후보0일 때만25gold 단일 선택을 제공하는 최소 정책으로 해결했다. boss100과 별개·serial ID/선택once/후보Load무지급을 확인했고 경제 초기값/추가 수입은 기획에 명시했다. Unity null해금 변환은 어댑터에서 수정하고 Core 검증은 약화하지 않았다. 관련 근거를 재사용해 단일 정상 앱·대표3화면으로 종료한다.

이전 R07의 후보수/실제선택횟수 오독을 공개 진행·리뷰·회고와 외부 보고에서 정정했다. raw로그/binding은 보존하고 정정문서로 인해 게임을 재실행하지 않았다. 정정 자료를 찾던 root의 첫 검색은 evidence 하위 Library까지 내려가 과도한 결과를 냈으며, 발견된 정확한 driver 경로로 좁혀 필요한 PendingChoices.Count 줄만 읽었다. 재발 방지는 기존 경로/검색 범위 제한을 적용하는 것으로 충분하며 새 도구나 전역 규칙을 추가하지 않는다.

모델은 일반 Sol 구현/독립 Sol high 코드리뷰/지정 Claude 아트 담당을 유지했고 추가 Astra/helper·전행렬은 없었다. 관측된 자기 usage/캐시·stale 기준점·공급자별 계수 차이와 미등록 사용량을 구분해 전체 청구/절감률은 계산하지 않는다. 동일 조건 속도 비교도 미측정이다. 최신 사용자 “일단08까지만”에 따라 R08 독립 리뷰·GC·commitpush·해당SHA CI 이후 정지하며 R09 계획/구현·R01~R09 최종 연속 확인은 시작하지 않는다.
