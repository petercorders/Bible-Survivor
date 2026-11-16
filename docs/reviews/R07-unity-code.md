# R07 Unity 독립 코드 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r07-implementation-01.md`와 `evidence/r07-unity-integration-20261116/handoff.md` 기준 Unity 관련 diff·최종 근거를 검토했다. 본인의 R07 Core PASS(관련27/27·정확2변이·899 actual kills/180초 CLI)와 R06 Unity PASS는 재사용했으며 Core 제품을 다시 리뷰하거나 실행하지 않았다.

## 확인 사항

- Host는 R06 between7을 `AwaitingR07`로 넘기며 R07 between8만 `ContentComplete`로 처리한다. 기존 pending 성장/완료 checkpoint gate·다음 라운드 진입 저장을 재사용한다. catalog는 R07 normal/round_settlement/between8을 정의7로 복원하고 R08과 잘못된 boss 라운드를 거절한다. 한국어 다음 라운드/완료 안내는 야곱/R08 대기에 대응한다.
- Unity의 보상 경로는 기존 Core 정산 호출뿐이다. 두 보스 역사 receipt/진화·PreviousNormalRound5는 새 session/Reload에 보존하고, pending 성장 선택 뒤120gold·elijah_chariot 해금·최대HP 회복/between8을 적용한다. 기존 frozen state/revision/operation retry와 Error 입력·tick 차단을 재사용하므로 실패 중 두 번째 정산이나 새 payload 생성으로 진행하지 않는다. 해금 이후 불수레 공격/장착은 허용하지 않는다.
- G1/G2/G3는 각각 승인 clip과 그림자로 명시 매핑된다. G3도 기존 actor별 실제 `EnemyPositions`/snapshot 발 좌표를 projection으로 그리며 Unity가 좌우에 재배치하거나 공유HP를 만들지 않는다. 방향 parent flip과 normal/lowFX가 같은 actor 좌표를 사용한다. 기존 R03/R06 동적 BossRules·보스 이름/고리/짧은 death 경로는 변경되지 않았다.
- 별도 ArtR07 loader는 고정 index/manifest와3개 clip·총12프레임, 프레임 이름/rect·pivot16,1·PPU32·Point/Clamp/비mipmap을 검증한다. importer는 PNG whitelist/hash·32격자·무압축/FullRect·고정 sprite ID를 적용하고 build 전에3장과 loader를 검증한다. fps는 G1/G3 9, G2 7이며 미등록 actor는 다른 아트로 대체하지 않는다.
- 기록된 `core-provenance.json` 소스 항목을 본인 R07 Core handoff summary와 대조하여 모두 일치함을 확인했다. sync/binding/provenance의 DLL 기록은 `88890687…df5d7`로 연결된다. 기존 기록 비교이며 새 해시 계산/빌드 검사는 아니다.

## 재사용 근거와 QA 교정

- `related.xml` **22/22**, 최종 `final-EditMode/results.xml` **116/116**, `final-PlayMode/results.xml` **31/31**, project/portable PASS를 확인했다. 신규 Host 시험은 R06→R07 두 receipt/진화·midWorld Load, 실제 Core G3 좌우·독립HP/제거·pause replay, 성장 전 정산 보류/선택 후 once, 완료 commit 실패 payload 고정·동일 retry, mapping/fps/그림자를 다룬다. 기존 시험의 콘텐츠 상한/완료 기대값은 R07 지원에 맞게 이동했다.
- `capture/core-spawn-world.json` 및 actors는 실제 Core가 생성한 G3 초기6명 x±11 근거다. `early-flanks-world.json`과 연속 기록은 실제 Core 추적3초 후 x약±5.6에서 양쪽 sprite가 보이는 좌표·pause/중간 disk 정확동등 근거다. 중앙에 합쳐진 화면을 초기 좌우 생성 근거로 사용하지 않는다.
- `capture/continuous-trace.txt`와 QA driver를 대조했다. 기존 R06 완료 seed→R07의 두 receipt/form 유지→기술 앞121초 직접 Kill/drop 폐기·Session3초/새 World→실제 생성/추적과 중간 Load→직접 Hit7/Kill 한 G3 및 peer240 유지→나머지 기술 Kill/timeout·pending 성장3회 **Host.Choose**→976+120=1096/elijah 해금/최대HP 회복/between8→새 process Run/Profile 정확동등을 확인한다. 주입 피해/처치를 실제 무기 처치로 세지 않는다.
- 초기 관련 geometry fixture의 direct Kill 뒤 위치 없는 drop 불일치는 실패 근거로 보존됐고 해당 기술 drop 제거로 수정했다. 제품 저장 검증은 완화하지 않았다.
- 첫 앱은 exit0이어도 pause overlay가 arena를 가린 화면은 UI **FAIL**로 격리했다. `qa-pause-overlay-correction.json`의 QA 전용 unpause 두 곳을 확인했으며 제품/Core/DLL/provenance는 고정이었다. game.enabled=false 상태로 캡처 전 추가 Tick이 없어서 같은 좌표를 유지한다. 정상 추가 build1회로 최종3장을 교체했으며 총 정상 macbuild는 **2회 exit0**다. 앞 실패 화면은 최종 대표에 포함하지 않는다. 최종 앱은 `build-review/R07Review.app`, identity `r078e8078c93e45`이며 수동 앱 패치가 아니다. 제품 불변이므로 관련/최종 시험은 재사용한다.

## 한계와 인계

앱은 HP10000/기존 장비·진화 seed·무기 cycle1000·direct Kill/Hit7/drop 폐기·normal timeout의 기술 fixture다. 실제 확인한 것은 Core 생성 좌표/초기 추적·독립HP와 Host 성장/정산/저장 연속성이다. 앱899/900 실킬·자연 완주 근거가 아니며 Core CLI도899킬/180초 timeout으로 구분한다. 화면 독립 UI 판정은 별도 담당 범위다. 자연밸런스/native input/mobile/device/performance·전프레임은 미검증이다.

최종 자기 child38936/38937/38982 exit0·no-Host/no-save/realpath probe·자기3저장 absent 복구·보호 대상 불조회는 인계 기록을 재사용했다. 리뷰어는 제품/시험/Core/아트/DLL/저장을 수정하거나 새 suite·CLI·해시행렬·Editor/GUI/build를 실행하지 않았다. 실제 의심 재현이 필요한 새 결함이 없어 probe도 추가하지 않았다. 본 문서만 소유한다. usage `r07-unity-review` start/end는 세션 기준점이며 stale 시작 event와 개별 worker 비용/effort 미측정을 구분하고 비용·절감률을 추정하지 않는다.
