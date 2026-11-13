# R02 엔진 독립 구현 계획

승인 근거: R01 이후 독립 코드 지속 및 R02 task_a291e6039b22의 구체 승인. 재승인 없이 진행한다. 소유는 관련 src/tests/tools 및 R02 문서이며 Unity·아트·전역 설정·commit/push는 건드리지 않는다.

## 규칙과 최소 변경
- AdamSession 호환 이름을 유지하고 R01 새 도전 → R01 완료/남은 선택 → 명시적인 StartNextRound(R02) → R02 완료 → R03 진입 대기만 연결한다. 진행 중·사망·선택 중·잘못된 번호·R03 진입은 거절한다.
- 완료는 현재 definition의 라운드 ID/해금/골드를 사용한다. R02 catalog는 B1/B2/B3 360, rawXP600, concurrency50, 180초/170초, 완료35골드다. 5킬당1골드 remainder는 도전 전체에서 유지한다.
- 새 session을 반환하는 전환으로 이전 session/world를 무효화한다. 성장·장비·보조·영구 스냅샷·잔액·해금은 유지하고 새 Wave 및 World(위치/공격/발사체/드롭/시계), LastHitTime을 초기화한다. 회복/골드/해금은 완료 시 한 번만 적용한다.
- 저장 schema1에 optional RoundFingerprint를 추가한다. 기존 R01 및 완료→R02 대기 저장은 허용하지만 fingerprint 없는 R02/R03 저장은 거절한다. 신규 저장은 전체 라운드 정의 fingerprint를 검사하여 카탈로그 변경을 명시적으로 거절한다. 옛 실행기는 R02 resume를 거절하므로 downgrade 호환은 제공하지 않는다.
- 선택은 저장한 목록을 복원한다. 저장/전환은 SaveStore.Transact의 revision/operation ID로 수행하며 실패 시 디스크 상태에서 같은 명령을 재시도한다. 결과를 모르면 재로드하며 같은 operation ID로 확인한다. 중단/뒤로는 완료된 체크포인트를 보존하고 다음 라운드의 자원을 미리 만들지 않는다.
- 공격은 기존 bible/jawbone/sling만 재사용한다. wilderness_voice/lightning은 영구 해금 상태이며 장착/공격은 미구현이다. R03 보스·무기10종·스킬420종은 범위 밖이다.

## 검증 및 인계
- R01 전체 회귀를 유지하고 R02 예산/타임아웃/상한, 중간 저장·선택·전환·중복·실패재시도·사망·숫자·카탈로그 변경·legacy 경계를 시험한다. 실제 coordinate world hit 검사와 injected budget CLI를 분리한다.
- python3 tools/check_all.py 및 python3 tools/check_mutations.py를 기존 로컬 SDK로 실행한다. 원래 회귀 구조의 변이를 별도 사본에서 재현하여 행동 시험 실패를 확인한다.
- 독립 리뷰/GC/commit/push/원격CI는 coordinator 소유다. Unity/모바일/실측 밸런스는 NOT_RUN이다.
- 단독 구현: 상속 모델/effort 유지, 호출별 실제 model/usage는 미측정. 위험은 저장/중복 전환 때문에 높으며 외부 동작 검사와 coordinator 독립 리뷰로 보완한다. 추가 모델/worker 호출 없음, 토큰 예산/절감률 미측정. 환경 반복 실패는 숨기지 않고 해당 게이트만 차단한다.
