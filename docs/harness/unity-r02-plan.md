# R02 가인 — Unity PC 연결 계획

2026-11-14. 기준84ccd9190dc1cfe650b32654f8c8c797ce1e28c7. **계획 작성 완료, 이 문서에서 제품 코드·Editor·Player·저장·아트는 변경/실행하지 않았다.** 사용자 결제 외 지속 개발·멀티에이전트·라운드별 GC/commit/push 승인을 유지한다. 최신 사용자 지시대로 독립 리뷰는 PC 범위로 진행한다. 모바일 실물/성능 등의 미검증은 PASS로 전용하지 않는다.

## 근거와 현재 경계

`docs/game-design.md`의 도전 진행/저장·종료 규칙, `docs/genesis-balance.md`의 몬스터/웨이브/해금, `docs/progress/R02.md`와 기존 구현을 읽었다. 기획 원본 폴더의 game-design/genesis-balance/mobile-platform-plan은 저장소 복사본과 byte 동일했다. 과거 설치 전 개발 계획의 환경 상태는 현재 환경으로 해석하지 않는다.

- `AdamSession.StartNextRound`는 이미 R01 완료→R02만 지원한다. R02 완료는 RoundNumber3/Stage between_rounds로 보존하며 R03 보스는 구현하지 않았다. Core·저장 schema를 다시 설계하지 않는다.
- `AdamCatalog`는 round1만 읽는다. `AdamHost.Reload`는 normal1/between2만 허용하고 `Start`는 완료 도전을 새 도전으로 교체한다. `AdamGame`의 제목·완료 화면도 아담 고정이다. 이 어댑터 경계가 이번 변경 대상이다.
- `AdamArtWorld.EnemyClip`은 a1~a3만 매핑하며 b1~b3는 예외다. `AdamArt`는 검수 Art03의 index/manifest SHA·79개 개수를 고정 검증한다. b몹을 a몹으로 조용히 매핑하거나 Art03 원본/고정 SHA를 무조건 느슨하게 만들지 않는다.
- R02 수치: 동시50, 생성360, 원시XP600, 일반180초/생성마감170초, 완료35골드, lightning 해금. 웨이브별 B1/B2/B3 수는72/0/18→84/36/0→75/45/30이다. 다른 전투 수치와 기존3무기 성장은 현재 Core를 그대로 쓴다. 해금 기록과 공격 구현은 별개이며 wilderness_voice/lightning은 계속 미장착·미공격 상태다.

## 소유와 시작 순서

현재 Android 담당이 Unity 표현·격리 빌드를 소유하고 사용자가 iOS 앱을 플레이 중이다. R02 구현자는 Android의 파일/Editor 소유 반환을 받은 뒤 canonical Runtime/Tests/Editor를 수정한다. 그 전에는 별도 아트 브리프·소스 읽기·검사 계획만 독립 진행할 수 있다. 사용자 Simulator·앱·진행 저장은 실행/종료/복원하지 않는다.

| 담당 | 파일/산출물 | 경계 |
| --- | --- | --- |
| Unity 구현 1명 | AdamCatalog/AdamHost/AdamGame/AdamArtWorld, 필요한 별도 R02 아트 로더, 관련 EditMode/PlayMode | 씬/프리팹/임포터도 실제 변경 시 같은 작성자에게 지정 |
| 지정 Claude Design | 별도 R02 시각 브리프·b1/b2/b3 시트와 manifest | 제품 연결 확인 후 실행. 미연결이면 브리프 인계 준비로만 기록 |
| 독립 코드 리뷰 | docs/reviews/unity-r02-code.md | 저장/전환/기존 Core 경계·최종 SHA·변이 검토 |
| 독립 PC UI 리뷰 | docs/reviews/unity-r02-ui.md | 사용자 Simulator를 건드리지 않는 PC QA 앱/저장·실제 입력 |
| coordinator | docs/progress/unity-r02.md, docs/retrospectives/R02-unity.md, 통합·commit/push/CI | 원본 보존·검사 결과·미검증 범위 구분 |

원본 Core/DLL/catalog는 변경 필요가 없으며 우선 그대로 사용한다. 어댑터 시험에 새 pureCore 요구가 드러나면 원인/최소 변경을 별도 제시하고 DLL provenance 재동기화까지 묶는다. 현재 .meta GUID와 Art03/font hash는 보존한다.

## 라운드 상태와 저장 흐름

1. 카탈로그 어댑터에 R01/R02 정의 조회를 추가한다. 기존 Definition(R01) 접근은 호환 유지하거나 사용처를 한 번에 교체한다. 유효 번호1/2·고정 genesis_01/02·보스 없음·예산/그룹·몬스터 참조를 검증한다. R03 카탈로그가 있다는 이유로 진행을 허용하지 않는다.
2. 새 도전은 기존3무기 중 하나로 R01에서만 시작한다. 살아 있는 R01/R02나 전환 대기를 시작 버튼이 덮어쓰지 않도록 Host와 화면 둘 다 제한한다.
3. R01의 완료 판정·보석 정산·완료25골드/해금·현재 최대HP 회복은 기존 Core가 수행한다. Core는 Finish에서 이미 회복하고 pending levels를 보존하므로 UI에서 회복/완료 보상/XP를 다시 적용하지 않는다. 미처리 레벨업이 있으면 기존 카드 선택을 모두 해결하고 매 선택을 저장한다. 그동안 World는 진행하지 않고 다음 라운드로 넘어가지 않는다.
4. 완료 상태를 성공적으로 checkpoint한 뒤 단일 Host 전환 경로에서 `Session.StartNextRound(r02)`를 **한 번** 호출한다. 새 세션/새 BattleWorld를 생성하여 round time0·플레이어 원점·이전 적/투사체/드롭/공격 타이머·피격 시계를 초기화한다. ArtWorld는 source identity 변경을 통해 FX/이전 적 캐시를 비운다. RunId·레벨/잔여XP·장비/보조·영구 스냅샷·골드/해금·KillRemainder는 유지한다. 전환 자체는 추가 회복·보상을 하지 않는다.
5. R02 진입 상태를 즉시 checkpoint하고 **성공한 뒤에만** 전투를 진행한다. checkpoint가 실패하면 새 arena를 재생성하거나 전환을 다시 하지 않고 현재 payload/revision/operation을 freeze한다. 같은 저장 재시도는 같은 operation을 사용한다. disk 결과를 모르는 오류에서도 추가 골드/초기화 없이 Commit의 멱등 경계를 유지한다.
6. 정상 실행 중 마지막 완료 선택의 저장 성공 후에는 R02로 자동 연결한다. 앱 시작/포커스 복귀·저장 다시 불러오기는 기존 정책대로 명시 재개가 필요하다. 로드한 완료 체크포인트의 pending choices는 먼저 표시하고, 해결 후 같은 단일 전환 경로를 거친다. 재개 버튼을 누른 손가락/키가 이동·다음 선택으로 전파되지 않게 release gate와 frame command 가드를 유지한다.
7. R02도 예정 전부 처치 또는180초 종료로 완료하며 완료35골드/lightning 해금/회복은 Core가 한 번 처리한다. 남은 선택을 해결하고 RoundNumber3/Stage between_rounds를 저장한다. 화면에는 “가인 완료 · 노아 라운드 준비 중 · 진행 저장됨”을 표시하고 R03 전투 버튼/가짜 보스 완료를 제공하지 않는다. 새 도전은 별도의 명시 동작으로만 기존 정책대로 R01을 시작한다.
8. 사망 시 Run은 null로 저장하고 영구 골드/해금/구매는 유지한다. 기존 사망 연출0.75초 공통 SelectSlot 가드와 다음 도전 release gate를 유지한다. R02에서 죽어도 새 시작은 R01이다.

UI 상태의 우선순위는 저장 오류→미처리 선택→사망 연출→유효 완료/콘텐츠 끝→정지/재개→전투다. R01 완료를 사망/새 시작 화면과 혼동해 자동으로 Run을 지우지 않는다. 전환 전후 HUD 제목은 세션의 라운드 정의에서 결정하며 아담 고정 문자열을 제거한다.

## 기존 저장·중단·취소

schema1을 유지한다. 로드 시 normal이면 RoundNumber의 정의, between_rounds이면 RoundNumber-1의 정의로 Resume한다. 허용 조합은 normal1/2, between2/3뿐이다. fingerprint 없는 legacy는 기존 Core와 동일하게 normal1/between2만 허용한다. R02 fingerprint를 R01로 덮거나 between3을 R03 정의로 Resume하지 않는다. fingerprint 불일치·손상 원본/백업·미래 schema의 기존 보호를 유지한다. World 없는 호환 저장은 새 World로 시작할 수 있으나 기존 좌표의 정확 복구로 보고하지 않는다.

전환 도중 앱 종료는 마지막 성공 checkpoint가 완료 대기 또는 R02 time0 중 하나가 되게 한다. 메모리에서 새 arena가 만들어졌어도 쓰기 실패 상태에서 자동 재전환하지 않는다. Retry는 현재 고정 payload를 저장하고, Reload는 마지막 유효 disk 상태로 돌아간다. 미래 schema는 갱신 안내를 표시하고 새 도전으로 덮지 않는다.

자동 전환에 취소 가능한 비동기 작업/서버 자원은 없다. 선택 창 닫기/포커스 상실/앱 종료는 선택·전환을 취소하거나 새 후보를 뽑는 동작이 아니다. 기존 동일 후보/완료 대기를 저장해 재개한다. 새 도전이 기존 진행을 대체하는 동작임을 화면에서 명확히 표시한다. 현재 사용자 플레이 저장으로 migration fixture를 만들지 않고 별도 PC QA 저장에 이전 정상/완료 fixture를 복사해 검사한다.

## R02 시각 인계

별도 검수 R02 sidecar를 추가하고 Art03의79PNG/index/font 원본은 불변으로 유지한다. 세 적은 가인/아벨 사람 자체가 아니라 시기·분노·갈라짐을 형상화한 창작 적이다. 신규 아트 없이 구현을 먼저 시험해야 하면 세 종이 구분되는 개발용 기하 표식을 명시적으로 표시하고 결과를 임시 표현으로 기록한다. 기존 뱀/나방/가시 마물을 최종 R02 그래픽으로 위장하지 않는다. 최종 R02 시각 PASS는 지정 디자인 산출물의 독립 검수/임포트 후에만 가능하다.

Claude Design 요청 후보 계약(실행 전 기존 아트 담당과 확인; 아래 프레임/피벗은 제안값):

| 안정 ID | 표현·게임 습성 | 제안 납품 |
| --- | --- | --- |
| enm_b1-envy-eye_move_32 | 시기의 눈. 플레이어를 향하는 동공·서로 겹쳐도 보이는 외곽. HP12/피해5/속도0.55, 추적 군집 | 투명32×32/프레임4개 가로시트,8fps loop, 오른쪽 원본/부모 flipX, 발 기준 pivot(16,4) |
| enm_b2-rage-hound_run_32 | 분노의 사냥개. 낮은 몸·빠른 달리기 실루엣. HP18/피해6/속도0.90, 별도 돌진/탄막 없음 | 같은32px/4프레임/8fps/피벗 후보. 빠름은 이동 속도와 애니메이션으로 표현 |
| enm_b3-cracked-mask_move_32 | 갈라진 가면. 폭이 있는 둔중한 몸과 균열. HP30/피해6/속도0.45, 분열 없음 | 같은32px/4프레임/6fps/피벗 후보. B1/B2와 색 외 실루엣 차이 |

공통 PPU32/Point/no mip/투명 바깥/검수된 sorting·그림자 참조/색 외 종 구분을 요청한다. 아트의 몸 크기는 Core 충돌 반경을 자동으로 바꾸지 않는다. 최종 manifest에는 실측 시트 크기·각 rect·pivot·fps·loop·방향·레이어·alpha·hash·출처/라이선스를 고정하고 잘못된 참조/등록을 검사한다. ArtWorld에는 monsterID→검수 clipID를 명시하고 알 수 없는 적을 조용히 대체하지 않는다. 배경은 기존 지형을 공통 개발용 바닥으로 재사용 가능하나 가인 전용 새 배경이 제작됐다고 주장하지 않는다. 신규 배경은 이번 최소 연결의 필수 조건이 아니다.

## PC 수용·회귀·변이

구현자가 현재 실제 baseline을 다시 확인하고 새로운 출력 폴더에 실행한다. 현재 기록은42Edit/15Play PASS이며 이것을 새 실행 결과로 재사용하지 않는다.

| 게이트 | 구체 검사 |
| --- | --- |
| 카탈로그/복원 | R01/R02 원본 수치·fingerprint, normal1/2와 between2/3의 정의 선택, legacy R1/완료·R2 World 없는 호환·불일치/미래 버전 거절 |
| 연속 상태 | R1 완료에 pending choices가 있으면 전환0회→모두 선택/저장 성공 후 전환1회. RunId/XP/장비/골드/remainder/영구 스냅샷 유지, HP 최대/World 시계0·새 아레나/피격 초기화 |
| 실패/재시도 | 완료 대기 저장 실패·R2 진입 저장 실패·교체 후 결과 불명 각각 freeze. 같은 operation으로 retry·revision 단일 증가, Reload 기존 정상 상태/보상 중복 없음 |
| 실제 전투 | B1/B2/B3 등장·원본 수치/속도, 3무기 명중/처치/보석 회수·자연 레벨업. 순간 빈 화면은 완료 아님, 예정 전부 처치와180초 완료 구분 |
| 화면·입력 | PC660×440/1100×720에서 R1→R2 제목·남은 선택·정지/재개·가인 완료/R3 대기. 숫자/마우스1회 선택·hold/release/사망 연출 가드, R2 사망→R1 새 도전 |
| Renderer 격리 | R2 World 교체 시 FX/이전 적/죽음 clip 누수 없음. 표현을 켜고 꺼도 동일 Core snapshot, 기존 Art03/TTF/GUID/meta 불변 |

최소 구체 변이는 별도 사본에서 다음 **옛 구조 자체**를 재현한다: (1) Reload를 R01만 허용으로 되돌림→R02/완료 저장 복원 행동 FAIL, (2) pending choices 조건 제거→선택 전 전환 FAIL, (3) old World 유지→시계/좌표/적 초기화 FAIL, (4) 전환 실패에서 다시 전환/새 operation 생성→중복/재시도 FAIL, (5) 제목/완료를 아담으로 고정하거나 기존 새 도전 분기로 되돌림→실제 전체 renderer/클릭 흐름 FAIL. 실제 실패 assertion과 복원 PASS를 보존하고 컴파일 오류를 검출로 세지 않는다. 기존18 Core 변이는 유지하며 어댑터 변경만으로 불필요한 Core 변이를 추가하지 않는다.

실제 compile→관련 EditMode/PlayMode→macOS Development build→PC native 플레이·저장 재실행을 단일 실행 소유로 검증한다. 자동/fixture/R01강제180초 전환 시험과 자연 수동 플레이를 구분한다. CLI 세 무기의 R02 조기/시간완료는 밸런스 PASS가 아니며 근접 bot0킬 이력을 숨기지 않는다. 이번 단계에는 새 iOS/Android 독립 UI 리뷰를 추가하지 않는다. 변경된 소스를 기존 iOS 사용자 앱에 재설치하지 않는다.

## 완료와 후속

1. Android 담당 인계/구현 파일 소유 확보 → R02 카탈로그·Host/로드·원자적 전환/화면 연결.
2. 독립 아트 브리프/검수 sidecar 또는 명시 임시 표현으로 실행 경계를 분리 → 관련 시험/원래 회귀 변이 → 실제 PC 플레이.
3. 독립 PC 코드/UI 리뷰 → HIGH/MEDIUM 수정·같은 담당 재검토 → 최종 소스/빌드 해시·기존 에셋/저장 보존 확인.
4. R02-unity GC에 실제 실패·도구화 후보·보고 정정·모델/토큰 미측정과 남은 게이트 기록 → 선별 commit/push→해당 SHA CI. 전역 GC entries/index/overview는 수정하지 않는다.

지금 사용자에게 새로 물어야 하는 확정값은 없다. 새 무기/강화 수치/출시 방향을 정하는 단계가 아니다. 단, Claude Design 제품 연결/신규 R02 검수 아트는 아직 확인되지 않았으므로 시각 완성의 외부 의존으로 root에 인계한다. 기술 연결은 명시 임시 표현으로 독립 진행할 수 있다.

R03 노아/네피림 폭군의 boss stage·2초 무적 예고·지원군 상한12·보상 최대3택1·중복 보상 방지/진화 자격은 별도 Core 계획과 수용 조건으로 다음에 구현한다. 현재 between3 체크포인트를 보존하고 R03 진입이나 보스 완료로 표시하지 않는다.
