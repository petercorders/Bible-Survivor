# R03 Core slice01 회고

2026-11-15. `r03-implementation-01.md` 승인 범위인 순수 Core 첫 보스→정식 보상 대기를 구현했다. 코드 작성자는1명, coordinator는 계획/인계와 독립 리뷰를 담당한다. Unity/DLL/아트·사용자 저장·전역 GC·commit/push는 실행하지 않았다.

기존 Wave를 보스로 재사용하면180초 자동 완료와 보상/회복이 섞이므로 R03 전용 stage/clock/target 경계를 추가했다. 기존 전투 순서를 유지한 일반 branch와 boss branch를 나누고, geometry/자동무기/투사체는 재사용했다. 정식 보상은 receipt와 처치 당시 자격을 보존하는 미구현 대기로 명시해 골드로 치환하거나 라운드 완료로 오인하지 않도록 했다.

검증은 보스 핵심 단위·대표 sling 좌표 CLI/중간 Load·4원래 구조 변이에 집중했다. 기존115/18/10/30 전체 검사는 한 번의 성공 증거를 유지했고, 전체 검사 중 실제 World-version 오류 분류 회귀가 드러난 경우에만 수정/재실행했다. phase 사건 경계 보강 후에는 최종126 Core와 보스 관련 검사만 확인했다. CLI의 HP10000/무기8 fixture는 실제 R03 피해 경로를 검증하며 밸런스 검증은 아니다.

실패/교정: header 콘텐츠 shape 검사를 앞당기면 missing World version이 손상 대신 미래 콘텐츠로 바뀐다. 기존 World version 우선 검사를 복구했다. 내부 `Array` helper와 `System.Array` 이름 충돌 컴파일 오류는 완전한 이름으로 수정했다. 상수false 변이는 unreachable 경고로 컴파일이 실패하므로 비상수 조건으로 바꿔 assertion 실패만 검출로 인정했다. 새 규칙을 전역 파일에 추가하지 않고 구체 회귀 시험/변이로 고정했다.

자체 검토로 tuning fingerprint가 실제 상수에서 생성되게 연결했고 phase 경계에서 과거 피격 사건을 비웠다. 최종 bosskill의 정확한 실제 피해·killed 사건이 남는 시험을 추가했다. 근거·경로·미검증과 최종 source hashes는 `evidence/r03-boss-core-01/final-binding.json`, 진행은 `docs/progress/R03-core.md`다. 독립 리뷰는 coordinator 인계 단계이며 완료로 표시하지 않는다.

시작/끝 관측 session `01a1dee8-3c44-7950-822e-fc377006107e`의 delta는 input4,146,369(캐시4,048,000 포함), output31,988(reasoning7,897 포함), total4,178,357이다. 기록 구간은11:12:16→11:25:36 UTC다. 청구 비용은 미측정이다.

모델 적합성: 일반 Core 구현은 실제 가용 Sol을 사용했고 Astra 상향은 없었다. 시작/끝 사용량은 `record_model_usage.py --phase r03-boss-core-01`의 자기 provider session 관측값을 기록한다. 세션별 귀속/청구 단가가 없는 비용과 절감률은 미계산이다. 다른 worker 사용량을0으로 합산하지 않는다. 위임 목적은 단일 Core 작성자 분리이며 추가 agent를 실행하지 않았다. 향후 개선은 하드코딩 계약/실행 상수의 중복을 줄이는 형태가 유효하며, 이번에는 fingerprint 생성과 해당 시험에 반영했다. 별도 전역 GC entries/index/overview 반영은 제안하거나 실행하지 않았다.

## 독립 리뷰 교정 — boss regen

독립 리뷰에서 normal의 영구 초당 재생을 boss 전용 tick이 우회한 MEDIUM을 확인했다. 이는 완료 전량 회복 금지와 별개인 기존 구매 효과 누락이다. normal의 계산을 공통 함수로 옮기고 boss 생존 tick에 한 번 연결했다. 피해로 죽은 tick의 재생/보상을 막고, 보호 intro·pause·성장 선택·보상 대기는 재생 시간을 만들지 않도록 기존 진행 경계를 유지했다.

유효 health2/armor1/regen1 fixture의 normal/boss 비교·동결 경계·최대 cap을 시험1개로 묶었다. 수정 전 예상111.1/실제111 assertion FAIL(exit1), 수정 후 관련 boss12/12 PASS(exit0)다. 격리 사본에서 boss regen 호출만 지운 정확한 원래 구조 변이1개를 검출(exit1)하고 복원PASS(exit0)를 남겼다. 기존126/18/10/30 전체 행렬은 반복하지 않았다. 근거는 `evidence/r03-boss-regen-review/`다. 원인은 별도 phase 루프에서 기존 영구 효과 호출을 빠뜨린 구현 누락이며, 추가 전역 규칙 대신 구매 효과의 실제 phase 간 동등성 시험에 고정했다. 같은 독립 리뷰어의 변경 부분 재검토가 남아 있다. 후속 보상·진화나 Unity/DLL 연결은 진행하지 않았다.

## 실행 02 — cohesive checkpoint

정식 후보·첫3진화·완료 정산을 같은 순수 Core 작성자로 구현하고 between4 checkpoint를 만들었다. 장비 base ID/레벨과 form lineage를 분리해 최대 기본 능력을 다시 더하지 않고 같은 슬롯에서 진화했다. pending-v1은 자동 재해석 대신 명시 승격 receipt로 보존했다. 별도 reward selector를 써 전투 RNG/일반 선택 serial을 바꾸지 않았다.

실제 공격 검증은 R04가 없는 현재 경계를 지켜 각 진화의 R03 좌표 fixture와 정식 reward→between4 흐름을 구분했다. 보상/진화·저장·정산 관련11개 시험과 최종 Core138/138, 대표 sling1seed 연속 CLI, 최소3원래 구조 변이만 새로 검증했다. 기존18/10/30과 전무기/phase/seed 행렬은 반복하지 않았다. 신규 strongest-target 시험의 설정 단계에서 injected kill의 drop을 좌표 snapshot 없이 남겨 Restore가 거절한 fixture 실패1개를 수정했다. 제품 Restore 검증을 완화하지 않고 주입된 설정 drop만 명시적으로 제거한 뒤 실제 aim/shot/retarget을 검사했다.

사용자의 다음 플레이 가능 통합본 우선·속도 피드백에 따라 작동하는 checkpoint에서 추가 개선을 멈추고 독립 리뷰로 인계한다. 검사기 확장은 세 실제 위험의 좁은 mutation driver와 기존 check_all의 대표 CLI 교체뿐이다. 전역 GC/Unity/DLL/아트/사용자 저장/GUI/CI hooks/commit/push는 실행하지 않았다. final binding은 `evidence/r03-reward-evolution-02/final-binding.json`이다. 모델은 Sol이며 usage start/end phase `r03-reward-evolution-02`를 기록한다. 비용/절감률은 미측정·미계산이며 독립 리뷰와 Unity 통합은 coordinator의 다음 단계다.

## CI 수정 — 기존 R02 해금 변이 anchor

원격 Core CI43731139114(`fb8a2b1`)는 첫13변이 PASS 뒤 `round_completion_uses_r01_unlock`의 고유 위치 검사에서 중단했다. R03 완료 정산을 추가하면서 `state.Profile.UnlockedWeapons.Add(definition.UnlockWeaponId);`가 일반 Finish와 R03 정산 두 곳에 존재하게 된 것이 원인이다. 제품 결함으로 대체하거나 다른 회귀를 시험하지 않고 `tools/check_mutations.py`의 해당 anchor에 일반 Finish의 기존 들여쓰기를 포함해 그 위치만 고유하게 지정했다. R02가 R01 해금 wilderness_voice를 잘못 지급하는 원래 회귀 의미는 같다.

격리 사본에서 기존 `R01 R02 injected budgets preserve total kills XP gold and unlocks through R03 waiting` assertion 하나만 등록해 이 정확한 변이의 실제 실패(exit1)→복원1/1 PASS(exit0)를 확인했다. 근거는 `evidence/r03-ci-mutation-anchor/summary.json`과 mutant/restored 로그다. 제품 Core/Unity/DLL/provenance·새 도구는 변경하지 않았으며 `python3 tools/check_mutations.py` exit0·기존18/18 실제 assertion 실패 검출로 나머지 anchor도 확인했다. 전체 근거는 `evidence/r02-mutations-20261115T154219506734Z/summary.json`다. commit/push는 coordinator가 수행한다.
