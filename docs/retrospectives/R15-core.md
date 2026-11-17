# R15 Core 회고

2026-11-18 KST. 승인된 마지막 R15 Core 범위를 수행하고 전체 제품 종료/Unity 게이트와 구분했다. 전역 학습 상태/스킬/검사기를 자동 수정하지 않았다.

- **재사용과 버전:** R09의 세 방향 적탄이 R15에 필요한 데이터를 이미 갖고 있어 Boss3/World4를 재사용했다. R12 history Boss4/World5와 현재 전투를 구별하고 새 필드/버전을 늘리지 않았다. 피해는 현재 Rules.AttackDamage18이며 R09의15는 관련 시험으로 유지했다.
- **원래 결함 검출:** endpoint-only 거리 조건으로 되돌린 격리 변이는 양 endpoint가 밖인 실제 양쪽 이동 교차에서 `Expected87/actual100`으로 검출됐다. baseline0/mutant1/restored0이며 비슷한 다른 변이나 compile 실패를 검출로 세지 않았다.
- **실패/복구:** 최초 R15 관련 실행은 시험 준비의 nullable Forms.Remove 경고가 오류로 처리된 CS8602/exit1이었다. 방어적 null 표기만 수정한 뒤 같은 관련 검사8/8을 통과했다. 원로그를 보존하고 제품 버그나 성공 변이로 바꾸지 않았다.
- **보상/상태:** 원시500/고갈100/성장25/완료280·회복을 분리하고 normalPending5/Choose5와 bossPending2/Choose2, 각각 후보1을 따로 기록했다. 기존 Genesis ChapterComplete/원자적 Exodus 진입을 유지하면서 ExodusComplete를 설치끝과 독립된 의미로 제공했다. 다음 책 자동 진입/reset은 추가하지 않았다.
- **모델/위임/효율:** requested Sol 기본/observed gpt-6.1-sol 단독 작성이며 Claude/Astra/helper를 새로 실행하지 않았다. 기존 fixture/시리즈와 R09/R12 근거를 재사용하고 관련 검사·최소 변이1건·필수 최종 check_all1회 안의 한 무기/seed CLI를 사용한다. 실제 실패나 불일치 없이 전체 조합/새 도구 체계를 늘리지 않는다. 독립 리뷰는 root가 배정하며 자체 검사로 대체하지 않는다.
- **추가 개선 제안:** 패턴 재사용 시 shape와 피해/정책/완료 의미를 별개로 대조하는 방식은 재사용할 수 있다. 이번에는 새 전역 규칙·hook·checker를 설치하지 않고 프로젝트 문서에 근거만 남긴다. 각 schema version의 숫자보다 실제 새 필드 필요 여부를 우선 확인했다.

자기 R15-core start/end provider interval을 기록하며 지연된 telemetry/CI 대기·보고가 섞일 수 있다. 시작 호출의 이전 phase delta와 최종 end 뒤 tail은 별도다. 실제 청구액/단가·전용 비용/절감률은 미계산이며 누락을0으로 대체하지 않는다. 기술 HP/장비/직접 Hit와 실제 좌표 CLI, 자연 밸런스/Unity/기기 미검증을 구분한다.

최종 check_all1회는 exit0/211개 모두 PASS/15명령 모두 exit0이다. 그 안의 R15 CLI sling/seed7은12891hits/일반·지원1097kills/boss1/335.6833333초·168좌표·예고/실탄 disk Restore·네history/현재Boss/between16/ExodusComplete를 확인했다. 자기 provider interval은 input8726492/cache8673664/output32567/reasoning11869/total8759059이며 청구미계산·end 뒤 tail 제외다. 전체 앱 완료와 독립 code/UI/Unity/native/mobile/performance·자연 밸런스는 root의 후속 게이트로 남긴다.
