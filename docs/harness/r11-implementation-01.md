# R11 요게벳 — 출애굽기 두 번째 라운드

2026-11-17. 전체 R10~R15 지속 승인 범위의 다음 단위다. `exodus-balance.md`와 기존 전환/저장·아트 계약을 재사용한다. R10 Unity 통합과 병행 가능한 작업은 외부 아트 준비뿐이며, Core 소스 수정은 R10 최종 소유 반환 뒤 시작해 DLL/provenance 검사와 충돌하지 않는다.

## 이야기·기술 초기값

출2:1~10의 숨김→갈대상자→구출/양육을 세 구간에 압축한다. 요게벳 이름은 출6:20에 근거한다. 미리암·바로의 딸은 자기 역할의 조연이며 플레이어는 사용자 자신이다. 아기/바구니 호위 HP·익사·탐지/속박 디버프·물 위 이동은 추가하지 않는다. 적은 수색/추격을 형상화한 창작 군세이고 실제 강물·이집트인 자체를 사탄으로 설정하지 않는다.

ID `exodus_02`, local2/global11, 일반180초/마감170초, 동시110/총960/원시XP4000/완료200gold/해금없음/보스없음. 수치는 자연밸런스가 아닌 초기 조정값이다. XP는 기존 정수배분으로 총합을 맞춘다.

| ID | 적 | HP | 접촉 | player속도비 | 표현 |
| --- | --- | --- | --- | --- | --- |
| ex4 | 갈대 매복마 | 180 | 10 | .60 | 갈대몸체/지면추적, 등장후계속보임·은신없음 |
| ex5 | 수색등불 | 140 | 10 | .75 | 눈과등불형부유몸체, 광선피해/탐지없음 |
| ex6 | 나일 추격수 | 240 | 12 | .90 | 갈대/진흙장식의빠른추적, 돌진/물피해없음 |

| 생성구간 | ex4 | ex5 | ex6 | 합계 |
| --- | --- | --- | --- | --- |
| 0~60 | 192 | 48 | 0 | 240 |
| 60~120 | 192 | 128 | 0 | 320 |
| 120~170 | 200 | 120 | 80 | 400 |

## 실행 순서

1. 외부 `art-exodus-r02/`만 선행 제작: 32×4frame 3PNG/12frames, stable IDs `enm_ex4-reed-stalker_move_32`, `enm_ex5-search-lantern_move_32`, `enm_ex6-nile-hunter_move_32`. PPU32/pivot16,1/rightparentflip/binaryalpha, fps8/8/10 권고, art03 wide/small/wide 그림자 재사용. 독립 QA 전 제품복사 없음. 숨김은 이야기 배경이며 스프라이트 무적/깜빡임 없음.
2. R10 검토·GC·업로드·Unity 소유반환 뒤 Core의 installed R11·schema3 normal/round_settlement/between12만 좁게 추가. R10 normal 완료→between11→R11 시작은 일반 round 전환이며 새로운 chapter 진입이 아니다. 세 보스receipt·RunId/장비/진화/성장/영구스냅샷/프로필 보존, R10완료180 재지급 없음. R11완료200once/해금없음/회복·기존고갈25 정책, 죽으면 R01.
3. 관련 Core 검사·중간Load/정산 retry/미지원R12전투원문보호·원구조 최소변이→독립 리뷰. 기존 root3 구조와 R10 fingerprint는 그대로 보존하고 무조건 버전 올리지 않는다.
4. 승인아트와 UI/월드/Host를 파일별로 연결. Editor/빌드/GUI 통합자1명, 관련검사+한앱·대표3화면/새프로세스Load. 독립 코드/UI→GC→commitpush/CI 후 R12 보스 단위로 이어간다. 기술fixture와 자연완주·native·모바일·성능미검증은 구분한다.

Claude 제외·Sol 우선·사용량start/end/실청구미계산·사용자앱/저장불조회·각영역소유·새Run없음은 R10과 같다. 이 파일 작성은 R11 Core/Unity 구현 완료를 뜻하지 않는다.

## 현재 배정

R10 SHA409327e의 원격 Core/art CI 두 개 success 확인 후 R11 Core181개/한seed 좌표전투·Load/원구조 정산변이 검증이 반환됐다. 독립 Core 리뷰를 기존 Run에 배정했으며 R11 아트는 독립 정적 PASS다. UI는 기존4개표시파일·새R11UiTests, 월드는 AdamArtWorld·새ExodusR02 loader/importer·새R11WorldTests·승인화이트리스트, Host는 AdamHost/AdamCatalog·새R11HostTests·관련기존경계시험·sync/QA/progressretro를 각각 단독 소유한다. 다른 영역 파일은 소유 반환 전 편집하지 않는다.

월드 작성자의 실제 시작 보고를 접수했다. Host/UI 배정과 실제 편집 시작은 별도로 보고한다. Core 독립 PASS와 UI/월드 반환 뒤 Host만 Editor/sync/최종앱을 실행한다. Root는 Core 반환 때 보고된 Resume상한 변경에 맞춰 기존 completion_resume_refused 변이의 anchor10→11만 갱신하고, 같은 원래 거절 주입의 의미는 유지한다. 최종 관련 도구 검증은 R11 종료 때 한 번 확인한다.

2026-11-18 배정 복구: R11 Core 독립 PASS와 월드 반환을 접수했다. 이전 UI의 완료된 R10 뒤 New messages 대기열은 실제 liveness exited·R11 UI편집0을 한 번 확인하고 해당 assignment를 abandon으로 정리했다(프로세스 종료/삭제 없음). 같은 Run의 `ctx_8455384ee1f4`/`task_5fcd56a5e4c2`가 R11 UI의 유일한 현 작성 소유자다. 오래된 큐 메시지는 이 제품 파일의 편집 권한을 주지 않는다. 새 배정 ready를 실제 편집 시작·시험 PASS로 세지 않으며 반환 뒤 통합자가 실행한다.
