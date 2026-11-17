# R15 마지막 독립 PC 정적 UI 리뷰

**PASS — 최종 review-build 성공 원본 3 PNG의 정적 관측 범위. 차단 HIGH/MEDIUM 없음.** 2026-11-18 KST, task `task_abcf7e48b58f` / dispatch `ctx_2eca4faab0ad`. 구현자와 별도 Codex worker가 단독 검토했다.

AGENTS.md 및 `evidence/r15-unity-integration-20261118/handoff.md`, `binding.json`을 읽었다. 대상은 최종 `review-build/R15Qa.app`, bundle `com.biblesurvivor.adam.r02artqa.r1580111c7154f5`에 귀속된 아래 성공 원본 3 PNG뿐이며 전부 `view_image`로 직접 열었다. 초기 QA exit2의 initial-failure 이미지/trace, 중간 prebuild 씬중복 exit1은 실패 이력이며 성공 UI 평가에 혼합하지 않는다. 정상 앱 build2회와 Editor build 호출3회(중간 호출은 BuildPipeline 전 중단)를 구분하며 이를 본인 실행으로 세지 않는다.

## 직접 이미지 관측

원본 경로 접두는 `/Users/gimgibong/Bible-Survivor/evidence/r15-unity-integration-20261118/capture/`다. 좌표는 대략값이다.

| 원본 | 관측 | 상태 |
|---|---|---|
| `01-660-bezalel-lowfx-three-directions.png` (660×440) | 출애굽기6/브살렐 및 상단 HUD가 읽힌다. 보스 이름과 **26000/26000**(약 y148), **3방향 예고·5초 주기·1초** phase(약 y168)가 두 줄로 분리되어 배우와 겹치지 않는다. forms는 검은 하단행(약 y389), F6 등 키 안내는 약 y417에 있어 경기장 배우와 분리된다. | PASS(정적) |
| 같은 660 원본 | 보스 쪽 약 x500/y237에서 왼쪽 위/중앙/아래로 뻗는 세 밝은 선과 왼쪽 화살표가 잔디·길·배우와 구별되어 **3방향 예고의 방향·범위**를 읽을 수 있다. 색 외 선/화살표 단서가 있다. | PASS(예고 정적 관측) |
| `02-1100-bezalel-frozen-reward.png` (1100×720) | 보스 보상 제목, **고정 골드 +500 적립됨·선택 즉시 저장**, 구름기둥 강화4→5/5 카드와 골드 **+100** 카드가 패널 안에 구분되어 읽힌다. | PASS(정적) |
| `03-1100-bezalel-exodus-complete.png` (1100×720) | **출애굽기 완료·성막 완성**, **완료 골드 +280·진행 유지**, **레위기 미구현·다음 책 준비 중**, **새 도전 선택 시 성장·장비 초기화**와 세 새 도전 버튼이 온전히 표시된다. | PASS(정적) |
| 세 성공 원본 공통 | 핵심 HUD/문구/버튼의 잘림이나 가리는 콘솔/오류 패널은 관측되지 않는다. 우하단 Development Build 표시는 핵심 문구/선택 버튼을 가리지 않는다. | PASS(관측 한정) |

보상500/선택100/완료280을 원본에서 직접 읽고 성공 `capture/continuous-trace.txt`의 4488→4988(+500)→5088(+100)→5113(+25)→5393(+280) 전달 기록과 대조했다. 보스 HP26000도 원본에서 읽힌다. 숫자 불일치가 없고 추가 코드 조사나 재빌드 권고는 필요하지 않았다.

## 관측 한계와 근거 재사용

이 PNG의 보스 상태는 **예고**다. 실제 세 실탄 생성·이동 순간, 지원군6/12, 경고/배우/탄 cleanup은 이미지에 없어 **미관측**이다. 이를 실탄 가독성 PASS나 움직이는 회피/인지 성능 PASS로 세지 않는다. 생산자의 trace/World4 상태/Play 근거는 별도로 재사용할 뿐 본인 실행이 아니다. frozen 보상의 시간정지·선택·저장 동작도 정적 PNG로 입증하지 않는다.

생산자 재사용: Core211, Edit131(최초130 PASS/1 fixture FAIL 후 교정 메서드2 PASS; 재실행2개를 새 고유사례로 더하지 않고 단일131/131 XML로 쓰지 않음), Play14. 500/100/25/280 각 지급과 실패저장 동일 retry/무중복, pending2/actualChoose2(구름5 및 fullserial25), 현재Boss3+네 과거receipt·World4·새프로세스 정확 Run/Profile Load, 명시 R01/schema2 새 도전은 재사용 근거다. 본인 시험 횟수에 합산하지 않는다.

HP10000/gear8/3forms/6slots/cloud5→4, normal timeout/injury20(방어 후9985), bossX9/cycle1000/직접 threshold·lethal Hit/pending2 기술조건을 자연 검증으로 확대하지 않는다. 자연1200kill/180초 밸런스·자연 장비유지/새진화·전체 앱 R01~R15 연속 완주·native/device/mobile/performance는 **NOT_RUN**. 콘솔 전체 로그 무오류도 본인 검증 범위가 아니다.

제품/이미지/앱/GUI/Editor/사용자저장/보호identity 접근·수정·실행, 추가worker, commit/push는 하지 않았다. 새 캡처/전조합/전체 픽셀/hash 계산 없이 소유 세 문서만 작성하고 타인 편집을 보존했다. 이번 단위에서 마감하며 **후속 책 개발을 시작하지 않는다**.

## 사용량·모델

세션 `01a1ea49-7c85-7cb0-854f-074ad240421e`, phase `R15-independent-static-UI`, 관측 gpt-6.1-sol, effort 미측정. Claude/추가 worker 없음. `tools/record_model_usage.py --checkpoint start|end --ledger /dev/null` 양 명령 exit0. 소유 외 ledger를 생성하지 않고 출력 카운터만 기록했다.

- start recorded_at `2026-11-17T18:36:34.485269+00:00`, event `2026-11-17T17:50:48.634Z`: input1905208/cached1822720/cache_write0/output17917/reasoning821/total1923125. 오래된 provider event이므로 정확한 R15 시작 사용량으로 전용하지 않는다.
- end recorded_at `2026-11-17T18:36:44.612645+00:00`, event `2026-11-17T18:36:39.302Z`: input2119968/cached1935616/cache_write0/output18428/reasoning821/total2138396, resultPASS. 이후 문서 작성·인계 및 telemetry 지연 구간은 미포함이다.
- 자동 delta_status BASELINE, 별도 총량 재계산 없음. reasoning은 output에 포함되어 중복 합산하지 않는다. 청구·가격·요청별 귀속 자료가 없어 실제 청구액/API equivalent cost/절감률 **미계산**, 미측정을0으로 합산하지 않는다.
