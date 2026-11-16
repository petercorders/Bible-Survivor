# R07 Unity 통합

2026-11-16. R06 완료→R07 야곱/G1~3→성장/일반 정산→between8·한국어 R08 대기 PC 통합·관련 검증 및 독립 코드/UI 대표 범위 리뷰 PASS다. 열린 HIGH/MEDIUM은 없으며 [코드 리뷰](../reviews/R07-unity-code.md)와 [화면 리뷰](../reviews/R07-unity-ui.md)에 근거와 한계를 기록했다. Core sync1회 DLL SHA256은 `88890687accd380531c884a792ab7a6346d3a198a43963134973a2bcc23df5d7`이다. Core제품/수치/저장형식/정책은 수정하지 않았다. Core 관련27/원구조2변이/대표899 actual kills·180초 timeout CLI와 외부 아트QA PASS를 재사용하며900킬 완주로 세지 않는다.

Host/catalog는 R07 normal/round_settlement/between8를 연결하고120gold/elijah_chariot해금/최대HP회복을 Core 정산 경로로만 적용한다. R03 CompletedBoss·R06 CompletedSecondBoss와 진화/이력·저장 retry를 보존하며 불수레 장착/공격·R08은 미구현이다. G1~3 승인3PNG/12프레임을 별도 pinned ArtR07 index/loader/importer로 연결했고 기존 그림자를 사용한다. Unity는 G3 실제 Core EnemyPositions를 각 actor별로 투영하며 재배치/공유HP/새 공격을 추가하지 않는다. 기존 동적 BossRules·짧은 death·R06 이삭/싯나 보상명 교정은 유지했다.

관련22시험 PASS: 두 역사 receipt/진화·midWorld Load, G3 실제 좌우/독립HP·제거/pause 재개, 성장 전120gold·회복 보류와 선택 후 once/between8, 같은 payload commit retry, mapping/fps/그림자를 확인했다. 처음 geometry fixture가 직접 Kill 뒤 위치 없는 XP drop을 남겨 정상 저장 검증에 걸렸고, 기술 준비에서 해당 drop을 버리도록 고쳤다. Core 검증은 완화하지 않았다. 고정소스 최종 Unity Edit116/Play31을 한 번씩 PASS했고 project/portable/diff도 exit0다. asmdef/allowlist 확장은 필요하지 않았다.

앱 seed는 이전 실제 R06 앱 완료 evidence JSON으로, between7/6보조 슬롯과 유효 phase를 먼저 읽었으며 장비·보조를 새로 주입하지 않았다. HP10000/기존 장비·sling/bible 진화를 유지하고, 앞121초 예정 actor 직접 처치/XP drop 폐기·3초 Session Advance 후 새 World(seed7)를 구성하는 기술 준비를 썼다. 무기는1000초까지 억제했고 G3 좌표는 수정하지 않았다. 초기 G3 6명의 x는 Core가 생성한[-11,+11]이다. 실제 Core 추적3초 뒤 화면은 여전히 좌우 x 약[-5.58,+5.62]로 분리되어 있고, full sprite가 양쪽 화면 안에 들어오는 좌표·pause/중간 disk 동등을 확인했다. 중앙에 겹친 뒤 배치 PASS를 주장하지 않는다.

대표 화면 뒤 직접 Core Hit7/Kill로 한 G3의233HP/제거와 다른 actor240HP 불변을 assert했다. 이는 기술 피해/처치 주입이다. 이후 나머지 직접 처치·normal timeout 뒤 후보3개 중 Host.Choose로 성장1회를 해결했다(level15→16). 기존 보고의 “성장3회”는 PendingChoices.Count를 횟수로 오독한 오류이며 원래 trace는 보존한다. 정산 직전976→완료1096의120gold/해금/최대HP회복/between8, 두 receipt·form·PreviousNormalRound5 보존 및 새 process Run/Profile 정확Load를 확인했다. 앞 준비/직접 처치에서 생긴 골드는 완료120과 분리했다. 앱 자연 완주/899실킬·모바일·native 입력·성능 PASS로 확대하지 않는다.

첫 macbuild/실행 exit0 후 실제 view_image에서 pause 모달이 전투 화면을 가리는 UI fixture 실패를 발견했다. 지속 승인에 따라 제품 소스/DLL/provenance를 고정하고 QA driver에서 캡처 직전 SetPaused(false) 두 곳만 추가했다. game.enabled=false여서 추가 simulation tick은 없고 동일 좌표를 유지한다. 새 출력 정상 build1회로 대표3장을 교체했으며 총 macbuild2회 exit0다. 실패 이미지/trace/PID/복구와 교정 근거는 별도 보존하고 최종 근거로 섞지 않았다. 새 단위시험/전체검사를 반복하지 않았다.

최종 근거는 ignored `evidence/r07-unity-integration-20261116/`의 binding/handoff·XML/log/실명령 exit/PID·연속 trace·core-spawn/early-flanks JSON·정확3화면·보존/복구·usage다. 최종 앱 `build-review/R07Review.app`, identity `com.biblesurvivor.adam.r02artqa.r078e8078c93e45`의 probe38936/capture38937/reload38982는 exit0이며 자기 저장3종만 absent로 복구/no-own-writer를 기록했다. 보호 preview/unknownQA/iOS·저장은 조회조작하지 않았고 기존842파일·Core handoff·원본/폰트/다른 담당 변경을 보존했다.

상속 Sol/실제 provider gpt-6.1-sol, 추가worker/Astra 없음. usage 시작 event가 기록 시점보다 오래되어 정확 R07 단독 귀속·청구/절감률·effort는 미측정이다. 자연밸런스/native/mobile/device/performance·모든 프레임/조합은 NOT_RUN이다. 제품·Editor·Player 소유를 반환하며 독립 코드/UI·최종GC·commitpush·CI는 root 후속이다.
