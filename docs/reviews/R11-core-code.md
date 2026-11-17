# R11 Core 독립 코드 리뷰

2026-11-17. **PASS — 지정된 Core/Sim/카탈로그/시험 변경에서 막는 HIGH/MEDIUM 결함 없음.** 기준은 `AGENTS.md`, `docs/harness/r11-implementation-01.md`, `docs/progress/R11-core.md`와 현재 working-tree diff다. 구현자와 별도 세션에서 소스 및 기존 실행 증거를 대조했다. Unity 통합 완료 판정은 포함하지 않는다.

## 고위험 경계 검토

- **schema3 범위와 원문 보호:** `SaveFormat.cs:157`의 Probe와 `SaveState.cs:70`의 typed 검증은 R10/R11의 normal·round_settlement 및 between11/12만 허용한다. between의 fingerprint는 직전 라운드, 전투·정산은 현재 라운드로 검증한다. 따라서 between12는 R11 완료 체크포인트이며 R12 전투 허용이 아니다. R12 normal, 잘못된 fingerprint, 미지원 필드/버전은 `UnsupportedSaveVersionException`으로 거절된다. `SaveStore.LoadCore`는 이 예외를 backup 복구로 바꾸지 않고 Commit도 기존 primary를 먼저 읽으므로 미래 저장의 primary/backup을 덮어쓰지 않는다. 신규 시험 `RoundElevenTests.cs:60`과 기존 R10 미래 receipt 시험을 대조했다.
- **R11 진입 전 정의 검증:** `AdamSession.cs:125`는 완료 R10/between11·pending0·순차 번호를 요구하며 `ExodusContent.Validate(next)`를 Capture와 상태 변경보다 먼저 호출한다. invalid gold999 정의가 예외를 내고 전체 snapshot JSON이 그대로인 시험(`RoundElevenTests.cs:42`)이 있다. R09의 `PrepareExodus`는 여전히 R10 전용이다. R12 StartNextRound는 거절한다.
- **세 receipt와 R10 180 중복 방지:** 진입 시 RunId·프로필·장비·진화3종·성장·영구 snapshot·세 receipt를 유지한다. R10의 완료 flag를 초기화하면서 PreviousNormalRound=10으로 기록하고 World/drop/피격 시계를 초기화한다. 진입 경로에는 지급/회복 호출이 없다. `SaveState.cs:114` 이후는 세 receipt의 3/6/9 순서·CompletionApplied와 각 EncounterId의 RunId 결합을 검증한다. 신규 진입 시험은 gold/HP 및 보존 대상 JSON 동등성, 반복 진입 거절을 확인한다. R10 정산 시험은 완료180 once를 계속 검사한다.
- **R11 성장·완료200 once:** `AdamSession.cs:267`은 남은 drop XP를 성장으로 옮긴 뒤 round_settlement로 진입한다. pending이 있으면 지급을 미루고, 마지막 Choose 이후에만 완료한다. `CompleteNormalSettlement`는 현재 세션·라운드·단계·pending0·미적용 flag를 요구하고 200gold/최대HP회복/between12를 한 번 적용한다. `RoundElevenTests.cs:51`은 고갈25gold의 serial별 순차 선택, 이전 선택 거절, 중간 Load, 같은 operation Commit 재시도, replace 전 실패 후 재시도, 완료 후 재정산 거절 및 해금 불변을 검사한다.
- **콘텐츠·전투 계약:** `ExodusContent`/카탈로그의 R11은 exodus_02, cap110, 960마리, XP4000, 200gold, 해금 없음이다. 세 구간 240/320/400과 ex4/5/6의 HP180/140/240·접촉10/10/12·속도.60/.75/.90가 승인 계획과 일치한다. R11에만 별도 speed fingerprint 계약을 추가했고 기존 R10 fingerprint `IunkZmlC50vfCA3mEtdpX5iiLjkDjk2gkFVsEXpZF9A=`는 고정 시험으로 보존한다. World는 기존 추적·접촉/내부schema3·빈 Charges·적탄 없음 경로를 사용한다. ContentComplete는 R11 완료로 이동하고 ChapterComplete는 창세기 완료 의미를 유지한다.
- **Sim/시험 연결:** `CatalogReader.Exodus(file, number=10)`과 기존 R10 API/명령을 유지하면서 R11 선택을 추가했다. `--r11-series`는 기존 R01→R09 좌표 전투 뒤 R10/R11과 각 60초 disk Load를 실행한다. 추가 R11 시험5개가 runner에 등록되어 있고 `check_all.py`의 대표 후속 CLI가 R11로 이동했다.

## 재사용한 실행 근거

| 근거 | 확인한 결과 |
| --- | --- |
| `evidence/r02-20261117T083824205984Z/core-tests.xml`, `checks.json` | Core **181/181, failures0**, Core 명령 exit0 및 standalone gate 모든 명령 exit0. 신규 R11 5개와 R10 5개의 testcase 및 시험 본문을 대조했다. |
| `evidence/r11-core/related-initial.log`, `r10-boundary-fixed.log` | 최초 관련 검사 9/10·exit1은 R10 시험의 옛 미지원 R11 기대가 ArgumentException을 받은 실패다. R11 5개는 PASS였고 미지원 경계를 R12로 교정한 R10 재검사 **5/5·exit0**, 최종 XML도 전부 PASS다. 최초 실패를 숨기거나 최종 실패로 혼동하지 않았다. |
| `evidence/r11-core-mutation/summary.json`, `baseline.log`, `mutant.log`, `restored.log` | **baseline0 / mutant1 / restored0**. R11을 later-normal 분기에서 제외해 이전 generic Finish로 돌아간 원구조 변이가 `Expected round_settlement, actual between_rounds` assertion으로 실패했다. mutant4/5, 복원5/5이며 compiler error가 아니다. |
| `evidence/r02-20261117T083824205984Z/14.log`, `checks.json` | 대표 `--r11-series sling 7` **PASS/exit0**. R10 4517hits/894kills/180초, R11 **5394hits/960kills/176.4166666666992초**, 두 midpoint disk Restore, 세 receipt·RunId 보존, between12·ContentComplete를 확인한다. 모든 생존 적 처치로 R11은 180초 전에 종료했다. |
| 리뷰어 `git diff --check` | **exit0**. 제품 변경이나 증거 불일치가 없어 suite/CLI/변이를 새로 실행하지 않았다. |

## 별도 후속 작업과 미검증

`tools/check_mutations.py:19`의 `completion_resume_refused` old/new literal은 Resume 상한10을 여전히 참조한다. root가 두 literal을11로 동기화할 예정인 도구 후속 작업이며, 이 Core 기능 판정과 분리한다. 이번 리뷰는 해당 도구를 수정하거나 전체 mutation suite가 통과했다고 주장하지 않는다.

좌표 CLI는 시작 전 장비8/보조1/HP10000을 넣는 기존 기술 fixture이며 hit/kill 주입은 없다. 출애굽기 진입 시에는 bible/jawbone/sling8·sling진화·stones2·crown/seal/cloud/underwear/whirlwind5로 성장한 실제 fixture 상태가 로그에 기록되어 있다. 단위시험의 이전 보스 직접 hit 및 성장 drop/pending fixture는 자연 전투 증거와 구분한다. 자연 완주·밸런스·Unity/DLL/provenance/Editor/GUI/native/모바일/실기기·성능·사용자 저장은 **NOT_RUN/UNVALIDATED**이며 이번에 실행·조회하지 않았다. 후속 Host의 저장 성공 후 게시와 저장 실패/결과 불명/중복 입력 처리는 Unity 통합 리뷰 범위다.

소유 산출물은 이 문서뿐이다. 제품/타 문서 편집·원복, 새 worker/Astra/Claude 호출, commit/push는 없다. 기존 graphify 산출물이 없어 읽기 전용 소스/diff와 증거를 사용했다. 자기 세션의 `record_model_usage.py --phase r11-core-review --checkpoint start|end`를 기록하며 실제 청구액과 비용 절감률은 미계산이다.
