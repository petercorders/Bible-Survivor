# R03 PC Unity 통합

2026-11-16 · 구현자 통합 PASS, root 독립 코드/UI 리뷰 대기. 승인 계획은 `docs/harness/r03-unity-integration-01.md`다. 기존 Core138·최소3변이·대표 sling series와 성경 sidecar 독립 UI PASS를 재사용했다.

기존 schema2 sync 도구로 승인된 Core를 한 번 Release Rebuild했다. 선택/앱 내 DLL SHA256은 `409909ec918d0e3ca0a8357dddf8b76d74d2386617514ea65fd551af5754f040`이다. Core 제품·수치·저장형식·catalog·CI/hooks는 변경하지 않았다.

Host/Catalog는 R01→R02→R03 normal/보호 intro/boss→Core 보상 후보/진화→남은 성장→Core 완료 정산→between4를 연결한다. 보상은 reward/candidate ID로 Core를 호출하고, 중복·만료 후보를 거절하며 Unity에서 추첨/지급하지 않는다. 실패 시 기존 operation/payload와 진행 정지를 유지하고 Retry/Reload를 사용한다. 이전 보상 정책의 승격도 명시 버튼으로 Core에 요청하며 Load가 자동 지급하지 않는다. R04는 미구현 대기이며 새 도전만 R01로 시작한다.

QA 승인 R03 계약/manifest/편집 source와 필요한 배우·그림자8PNG를 `art/art-genesis-r03` 및 `Resources/ArtR03` sidecar로 연결했다. 기존 Art03/R02/성경/font 원본과 GUID를 보존한다. intro는 move0, windup은 Core1.2초 시계로0/1/2, impact2@10/death4@6를 표시한다. 보스 HP/phase, 한국어 보상과 첫3진화 이름/효과를 표시한다. 네피림은 게임 각색 보스다. 위험 경계는 실제 Core locked target/radius를 공유 WorldProjection으로 투영해 모든 배우·입자·무기 위/HUD 아래에서 항상 그린다. lowFX도 유지하며 PNG 크기를 판정으로 쓰지 않는다. 참조 warning PNG와 optional impact decal은 가져오지 않았다. 원격 Design 렌더를 확인했다고 주장하지 않는다.

## 실제 검증

근거는 새 ignored `evidence/r03-unity-integration-20261116/`의 `binding.json`과 `handoff.md`다.

- 실제 compile0, 관련 Host17/17 PASS. 보스 intro/예고 중 World·Boss 저장 재개, 보상/정산 쓰기 실패 후 frozen payload 재시도·중복 거절, 남은 성장의 정산 차단, Core 반경 투영/예고 frame을 검증했다. 고정 소스의 최종 Unity Edit97/Play31은 각각 실제 exit0, 실패/skip0이다. 종료 local project/portable/diff 검사도0이다. Core 전체 suite/기존 변이를 반복하지 않았다.
- 하나의 macOS Development 앱 build0. 한 sling/seed7 기술 fixture로 R01/R02 timeout 후 sling8+stones1/HP10000을 설정하고, R03부터는 실제 Core 좌표 전투만 사용했다. R03 피해/처치를 주입하지 않았다. normal180초→보호 intro→보스885HP 예고 중 disk restore→실제 투사체 처치→보상 후보 disk restore→실제 SelectSlot의 물맷돌 진화→남은 성장 해결→완료 정산/회복/receipt→R04 대기를 연속 trace로 기록했다. 최종 골드306은 fixture 실행값이며 자연 플레이 보상/밸런스 측정으로 일반화하지 않는다.
- 실제 핵심3화면만 촬영했다:660 lowFX 예고,660 한국어 보상3카드,1100 완료/R04 대기·진화 이름. 전프레임/phase/무기/해상도 픽셀·해시 행렬이나 별도 단계용 빌드는 추가하지 않았다. 새 프로세스 Load의 Run/Profile이 종료 전 기대값과 정확히 같고 완료 중복은 거절됐다. 정상 OnApplicationQuit의 기존 checkpoint가 revision/operation을 갱신하므로 전체 primary byte 불변을 주장하지 않는다.
- 새 독점 suffix/product/bundle/marker의 무저장 첫 씬 probe와 realpath를 확인한 뒤 자기 child PID만 실행/종료했다. writer 부재 후 자기 QA 저장만 부재로 원복했다. 지정 사용자 preview/unknown QA/해당 저장/iOS는 조회·입력·종료·원복하지 않았다. 기존 보호593파일(고정 Core 소스 포함)을 보존하고 Unity 자동 비override import/빈 icon80항목만 baseline byte와 대조해 복구했다. 기존 runtime 중 미수정 파일도 그대로이며 변경/새 소스는 결속에 명시한다.

모델은 상속 기본값이며 실제 provider는 gpt-6.1-sol, Astra/helper 추가 없음이다. own usage 시작/끝을 기록했고 비용·절감률·effort는 미측정이다. 커밋/push하지 않았다. 제품/Editor/Player 소유를 root에 반환하며 독립 코드/UI 리뷰·GC 최종·commit/push·해당 SHA CI는 root 후속이다. 자연 난이도/장시간 완주·native 정밀 입력·모바일/기기/IL2CPP·성능은 NOT_RUN이다.
