# R10 출애굽기 진입·십브라 구현 계획 1

2026-11-17. `exodus-development-plan.md`의 첫 개발 단위다. 6라운드는 콘텐츠 초안으로 유지하되 이번 제품 범위는 누적 R10 한 라운드와 기존 R09 완료에서의 진입이다. R11~R15/새 무기/상점/모바일은 이 단위에 섞지 않는다. 같은 사용자 요청은 기존 작업에 합치며 새 Run을 만들지 않는다.

## 초기 카탈로그

chapter `exodus`, local1/global10, stable round ID `exodus_01`, 초점 십브라(부아 포함). PC 전투는 기존3분/170초마감, cap110, 총900, rawXP3600(각4), 완료180gold, 해금없음, 보스없음. 이는 자연 플레이로 검증하지 않은 기술 초기값이다. R09보다 낮은 생성량·속도로 챕터 도입의 숨 돌리는 구간을 두며 장비를 초기화해 난이도를 맞추지 않는다.

| ID | 이름 | HP | 접촉피해 | 기본player속도비 | 습성 |
| --- | --- | --- | --- | --- | --- |
| ex1 | 벽돌 운반마 | 160 | 10 | .45 | 느린 추적/접촉, 벽 생성 없음 |
| ex2 | 감시의 눈 | 100 | 9 | .70 | 빠른 추적/접촉, 탐지 디버프 없음 |
| ex3 | 명령의 사슬갑주 | 450 | 13 | .35 | 느린 고체력 추적/접촉, 속박/면역 없음 |

| 생성구간 | ex1 | ex2 | ex3 | 합계 |
| --- | --- | --- | --- | --- |
| 0~60 | 180 | 60 | 0 | 240 |
| 60~120 | 180 | 120 | 0 | 300 |
| 120~170 | 180 | 126 | 54 | 360 |

既定추적을 그대로 사용한다. 새 환경피해/호위체력/적탄/돌진 없음. 코드 상수와 카탈로그 수치는 해당 라운드의 단일 정의에서 일치시킨다. 창세기 카탈로그는 변경하지 않는다.

## 챕터·저장 계약

출애굽기는 chapter별 stable ID와 global number를 함께 갖는다. 기존 `genesis_01`~`genesis_09`의 fingerprint/JSON/저장 바이트 의미는 보존한다. root schema2의 R09 `between10`은 여전히 창세기 완료 대기이며 새 콘텐츠가 설치돼도 Load만으로 전투를 시작하지 않는다. UI의 명시 `출애굽기 시작`을 통해 진입한다. 명시 새 도전은 별도이며 R01로 가는 기존 의미를 유지한다.

기존 `ChapterComplete`를 전체 설치 콘텐츠 완료로 재사용하지 않는다. Host는 `CanEnterExodus`와 `ContentComplete`를 구분할 수 있어야 한다. R10 완료는 `between11`에서 현재 설치 콘텐츠 끝을 안내하며 출애굽기 전체 완료라고 표시하지 않는다.

R09→R10에서 덮어쓰기 위험이 있는 `CompletedBoss`에 유다 receipt를 넣지 않는다. R03/R06 두 기록과 R09 완료 receipt를 독립적으로 보존할 세 번째 명시 필드를 추가한다. 임의 범용 목록/중복 저장 경로는 만들지 않는다. 새 필드·챕터에 필요한 root schema3을 도입하되 기존 schema1/2는 기존 검사로 읽고 내용 변경 없이 재개한다. 구버전은 schema3을 미지원으로 거절해 원문을 보존한다. schema3은 현재 구현된 exodus_01의 normal/round_settlement/between11만 허용하고 R11 전투는 미지원으로 거절한다. schema 번호만 올려 미래 임의콘텐츠를 허용하지 않는다.

새 R10 검증 분기는 기존 schema1/2의 지원범위 검사와 분리해 추가한다. 기존 모든9상한/모든schema2 비교를 일괄 치환하지 않는다. R10 World는 기존 normal schema3 형태(Charges 빈 목록/적탄없음)를 재사용할 수 있는지 구현자가 확인하고 필요한 최소 shape만 명시한다. R09 적탄·무기피격 ledger·적좌표·World clock은 새 전투로 넘기지 않는다. RunId·장비/보조/레벨/XP/진화·영구스냅샷·잔액·일반kill 나머지·완료history는 유지한다. R10 새 world와 타이머/피격시계는 초기화한다. 진입 때 추가 heal/gold/receipt 지급은 없다.

독립 검토 보완: 현재 Decode는 CurrentVersion으로 무조건 메모리 승격하므로 단순 CurrentVersion=3 변경은 금지한다. 창세기 새 도전과 기존 재개는 schema2를 유지한다(schema1의 기존1→2 메모리 승격 포함). schema3 전환은 명시 R09→R10 진입 복사본에서만 수행한다. 프로필만 있는 schema3 저장에서 새 도전을 시작할 때도 기존 창세기 schema2 계약으로 유효하게 생성하며 구매/해금/잔액은 유지한다. root schema와 World 내부 shape 버전은 서로 다른 개념이다.

## 원자성·재시도·중단

- 진입 전: 기존 완료 세이브의 복사본에서 다음 정의/상태를 구성·검증한다. 잘못된 catalog나 지원하지 않는 상태는 기존 session/메모리/파일을 변경하지 않고 실패한다.
- 진입 commit 성공: 새 session/world를 게시한다. 작업 ID는 같은 시도에서 고정해 중복 클릭/재시도에 별도 도전·재화 지급을 만들지 않는다.
- commit 실패/결과 불명: 기존 SaveStore revision/operation 확인·잠금·복구 경로를 재사용한다. 새 run을 임의로 덮어쓰거나 이전 완료 단계에서 보상을 다시 지급하지 않는다. 화면은 저장 오류와 재시도를 표시하며 정상 전투를 진행하지 않는다.
- 취소/뒤로가기: commit 전에는 창세기 완료 화면에 남는다. commit 후에는 R10 진행을 일시정지하는 기존 흐름을 사용하며 자동으로 R09로 롤백하지 않는다.
- 완료: 드롭회수→남은성장 처리→180gold once·해금없음·최대HP회복→between11 저장. 만석 성장25gold 정책과 실제 PendingLevels/Choose횟수는 기존 경로를 그대로 사용한다.
- 사망: 도전 종료→명시 새 도전 R01. 챕터2 시작점 부활을 추가하지 않는다.

## 소유·완료 조건

Core 담당: `content/exodus/catalog.json`, Core의 챕터전환/저장/카탈로그 연결 및 새 R10 tests·Sim 관련 경로, R10 progress/retro. Unity/DLL/아트/CI/GUI/사용자저장 금지. 기존호환 golden 대표1개·R09완료Load무쓰기·진입실패/성공/재시도·세receipt/성장유지·중간Load·완료한번·future원문보호를 묶어 검증한다. 회귀변이는 receipt덮어쓰기/진입중복 중 실제 변경이 다루는 원래 구조를 최소1회 검출한다. 관련검사 후 필요한 최종 게이트1회, 기술seed를 자연플레이로 보고하지 않는다.

아트 담당: 별도 외부 `art-exodus-r01/`만. ex1/ex2/ex3 32px×4frame, pivot16,1/PPU32/right-parentflip/binary·기존shadow 재사용. 원본스크립트/3PNG/manifest/필수계약/기존 방식의 정상regen1회·대표 최대3보드. Claude 제외. 게임 픽셀은 Codex 스크립트 출력임을 명시하며 독립 아트 PASS 전에 제품임포트 금지.

UI 연결지점 읽기 검토에서 시작해 Core 계약 반환 뒤 아래 파일allowlist로 구현을 인계했다. Editor/sync/build/Player는 통합자1명만. 독립 code/UI→라운드GC→commitpush/해당SHA CI 후 R10 단위 완료를 보고한다. 보호 사용자앱/저장·preview·unknown QA는 이전과 같이 불조회다.

## 현재 배정 갱신

Core176PASS/아트초안 제작 반환 후 독립 code/art 검토를 진행한다. UI 읽기검토8줄은 반환됐고 다음 세 작성영역을 배정했다. 배정 receipt/ready는 실제 편집 시작과 구분하며 첫 변경 보고를 받아 상태를 갱신한다. 전체 목표는 R15까지이며 이 단위 완료 때 사용자 재승인을 기다리지 않는다.

| 영역 | 단독 소유 | 실행 경계 |
| --- | --- | --- |
| UI | AdamGame/AdamChoiceText/AdamRoundPresentation/AdamBossPresentation·새R10UiTests | Host/World/기존공용시험·Editor금지 |
| 월드 | AdamArtWorld·새AdamExodusR01Art/Importer·새R10WorldTests·승인아트복사 | 독립artPASS전복사보류, Editor금지 |
| Host·최종통합 | AdamHost/AdamCatalog·새R10HostTests·필요최소기존Host시험·카탈로그복사/sync산출물/QAfixture/progressretro | Core/artPASS와UI/월드소유반환후Editor/build/Player단독 |

공유Host API는 `CanEnterExodus`, `ContentComplete`, `TryEnterExodus()`를 기준으로 한다. Core `CanEnterExodus`는 source 준비 가능상태이며 Host는 설치콘텐츠/오류/중단 상태도 검사한다. API 변경이 필요하면 다른파일을 임의 편집하지 않고 통합자·root에 전달한다. Root는 계획·배정·독립리뷰·통합후commitpush를 소유한다.

월드 담당의 실제 소스 작성 시작 보고를 접수했다. 아트 임포트는 독립PASS까지 보류한다. UI/Host의 배정만으로 실행검증 완료를 주장하지 않는다.
