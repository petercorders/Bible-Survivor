# R08 Unity 독립 코드 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r08-implementation-01.md`와 `evidence/r08-unity-integration-20261116/handoff.md` 기준 Unity 관련 diff·최종 근거를 검토했다. 본인의 R08 Core PASS(관련10/10·정확2변이·actual1008kill/180초 CLI)와 앞선 Unity PASS를 재사용하며 Core 제품을 다시 리뷰하거나 실행하지 않았다. 범위는 R08 종료까지다.

## 연결 확인

- Host는 R07 between8을 `AwaitingR08`로 넘기고 R08 between9만 `ContentComplete`로 처리한다. catalog는 R08 normal/round_settlement/between9를 정의8로 복원하며 R09와 잘못된 boss 라운드를 거절한다. 요셉 완료/R09 미구현 대기로 표시하며 R09 기능을 추가하지 않았다.
- JsonUtility에서 null 문자열이 empty로 읽히는 경계는 adapter에서 `string.IsNullOrEmpty(unlock_weapon_id)?null:...`로 정상화한다. 이를 그대로 Core RoundDefinition에 전달해 R08 null 해금 계약/동일 fingerprint를 지킨다. 기존 비어 있지 않은 R01~R07 해금 ID는 유지하며 Unity가 임의 해금/장비를 만들지 않는다.
- `AdamChoiceText.Describe`는 Core `GrowthGoldChoicePrefix` 후보의 전체 ID를 보존하면서 골드 주머니/재화/영구 골드+25로 표시한다. 재화 Card는 장비 신규/강화·가짜 레벨/최대단계 화살표를 출력하지 않는다. 보스gold100은 기존 별도 BossRewardCandidate 표시/선택 경로다.
- 카드/키보드 선택은 기존 `SelectSlot`→현재 `PendingChoices[slot]` 전체 문자열→`Host.Choose`→Core Choose 경로를 사용한다. 표시용 ID 절단/재작성이나 Unity의 별도 골드 지급은 없다. Error/suspend/동일 frame gate·후보 membership과 기존 저장 operation/revision/payload retry를 유지한다. 금액 적용 후 commit 실패 시 새 선택/tick을 막고 동일 payload를 저장하여 재지급하지 않는다.
- 남은 성장 처리 후140gold·최대HP 회복·완료 flag/between9을 기존 Core 일반 정산으로만 적용한다. R03/R06 history·첫3진화·PreviousNormalRound7과 null 해금 목록을 보존한다. 기존 보스 동적 규칙/고리·짧은 death·보상명 및 G3 좌우 특화는 변경하지 않았다.
- H1/H2/H3는 실제 World actor별 발 좌표에 승인 clip/그림자를 매핑한다. Unity는 좌우 재배치·속박/새 피해선을 추가하지 않는다. ArtR08 loader/importer는 고정 index/manifest·3PNG/12프레임·PPU32/pivot16,1·프레임 rect/이름·Point/Clamp/비mipmap·PNG whitelist/hash/무압축·고정 sprite ID/build 검증을 재사용한다. fps는9/5/7이며 미등록 actor는 대체 아트로 넘어가지 않는다.
- 기록된 provenance의 Core 소스 항목은 본인 R08 Core summary와 모두 일치했다. sync/binding/provenance의 DLL 기록은 `4104a400…db748`로 연결된다. 기록 비교이며 새 해시/빌드 검사를 실행하지 않았다.

## 재사용 검증과 실제 선택 횟수

- `related.xml` **25/25**, 최종 Edit **121/121**·Play **31/31**, project/portable PASS를 확인했다. Host 시험은 Core 생성 fallback의 전체 serial ID·Load 무지급·선택 후 commit 실패/동일 retry/옛 ID 거절, 재화 카드/보스100 구분, 두 history/진화·midWorld Load, 성장 전 지급 보류/140gold·null 해금·회복 once와 완료 commit retry,3클립 mapping/fps/그림자를 다룬다.
- `related-null-unlock-failure.xml`의 초기 **4/25 PASS·21실패**는 Invalid round identity의 실제 통합 실패다. 위 empty→null adapter 교정 후25/25로 복원됐으며 실패 근거를 삭제하거나 Core 계약을 완화하지 않았다.
- `capture/growth-choice-state.json`은 **level17/PendingLevels1/ChoiceSerial16/Choices=[growth_gold_25:16]/gold1227**이다. QA는 유효 장비를 Core API로 최대화하고 실제 XP 회수로 후보를 생성했다. Choices/PendingLevels/serial 직접 주입은 없다. Load는1227로 무지급, 실제 `SelectSlot(0)` 뒤1252(+25), 옛 ID 거절과 선택 후 Load도1252를 유지했다.
- 정산 구간은 별도로 **Run.PendingLevels2**, 실제 Choose loop **2회**다. 후보 수를 성장 횟수로 세지 않았다. `continuous-trace.txt`와 QA driver의 pending/paid counter·완료 snapshot을 대조하여1266+2×25+140=1456, level19/pending0/serial19·최대HP 회복/between9을 확인했다. 초기 일반25 한 번, 남은 성장25 두 번, 완료140을 분리한다.
- 앱 기록은 R07 완료 seed→R08 H혼합 실제 추적3초·pause/midWorld 정확Load→위 고갈 선택→성장/정산→새 process Run/Profile 정확동등을 연결한다. 두 역사 receipt·폼·PreviousNormalRound7·해금 목록은 유지된다. 앞 R07 보고의 후보 수/실제 성장 횟수 정정은 `seed-source.json`으로 구분하며 기존 raw 근거를 재작성하거나 재실행하지 않았다.
- 정상 compile/macbuild는 **1회 exit0**, 최종 앱은 `build/R08Qa.app`·identity `r0827eef7512bf4`이며 수동 앱 패치가 아니다. 대표는 일반/골드25 카드/완료의3장이다. 화면 독립 UI 판정은 별도 담당 범위다.

## 한계와 종료 범위

앱은 HP10000·기존 장비/진화 seed·앞 direct Kill/drop 폐기·무기 cycle1000·이후 장비 max/직접 KillCollect XP·normal timeout의 기술 fixture다. 실제 확인한 H추적/저장·Core 생성 후보/Host 선택·정산 연속성을 앱1008/1020 실킬·자연 완주로 확대하지 않는다. 기존 Core CLI도1008킬/180초 종료다. 자연밸런스/native input/mobile/device/performance·전프레임은 미검증이다.

자기 child75338/75352/75356 exit0·no-Host/no-save/realpath probe·자기3저장 absent 복구·보호 대상 불조회는 인계 기록을 재사용했다. 리뷰어는 제품/시험/Core/아트/DLL/저장을 수정하거나 새 suite·CLI·해시행렬·Editor/GUI/build를 실행하지 않았다. 새 재현이 필요한 실제 의심이 없어 probe도 추가하지 않았다. 본 문서만 소유하며 R09 개발/계획·R01~09 최종 연속검증은 시작하지 않았다. usage `r08-unity-review` start/end는 세션 기준점이며 stale 시작 event와 개별 worker 비용/effort 미측정을 구분하고 비용·절감률을 추정하지 않는다.
