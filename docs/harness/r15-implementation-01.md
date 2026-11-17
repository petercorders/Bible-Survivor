# R15 브살렐 — 출애굽기 완료

2026-11-18. 사용자 승인 범위 R10~R15의 마지막 단위다. R12~R14 제품 검증과 충돌하지 않는 계획·외부 아트만 선행하고, Core 구현은 이전 단위 검토·소유 반환·업로드 뒤 별도로 배정한다. R15 완료 후 레위기·새무기·스킬·모바일을 자동 개발하지 않는다.

## 이야기와 적

출31의 브살렐·오홀리압 부르심은 짧은 도입으로, 출35~40의 자원하는 예물→제작과 협력→성막 세움을 세 구간으로 둔다. 금송아지 사건을 다시 재현하지 않는다. 실제 성막 제작 중 이 마물과 싸웠다는 본문은 없으며 제작·협력을 방해하는 창작 군세임을 명시한다. 성막·언약궤·기구·제사장·하나님의 임재 자체를 적으로 만들지 않는다. 보스 처치가 하나님의 임재를 만들어 낸다는 인과도 넣지 않는다. 성막 완성과 임재는 안전한 완료 설명/연출이며 구조물 호위HP·실제 제작 재료 수집은 추가하지 않는다.

ID `exodus_06`, local6/global15, 일반180초/마감170초, cap120/총1200/원시XP5600/완료280gold/해금없음. 아래는 자연 실측이 아닌 기술 초기값이다.

| ID | 적 | HP | 접촉 | player속도비 | 행동 |
| --- | --- | --- | --- | --- | --- |
| ex16 | 탐욕의 갈고리 | 280 | 13 | .85 | 갈고리 팔의 빠른 추적, 실제장비/골드강탈 없음 |
| ex17 | 왜곡의 직조마 | 400 | 14 | .65 | 검은 실타래 군집, 거미줄/속박 없음 |
| ex18 | 훼손의 쇳조각마 | 850 | 16 | .35 | 부서진 금속 큰 몸체의 느린 추적, 성물 자체가 아님 |

| 생성구간 | ex16 | ex17 | ex18 | 합계 |
| --- | --- | --- | --- | --- |
| 0~60 | 256 | 64 | 0 | 320 |
| 60~120 | 280 | 120 | 0 | 400 |
| 120~170 | 240 | 144 | 96 | 480 |

기존 HP 우선 정수배분으로 XP합을 맞춘다. 일반 적은 추적·접촉만, 신규 돌진/투사체/환경피해가 없다.

## 보스 — 훼손의 직조군주

ID `defilement_weaver`, HP26000/contact22/고정500gold/이동비.30/반경.60/보호intro2초/offset(0,+3). R09의 검증된 세 방향 적탄 패턴을 재사용한다. 5초 주기·첫4초 정지 예고→5초 발사, 예고 시작에 고정한 방향과±20도, 겹침(1,0). 현재 실제 boss위치에서 쇳조각3발을 생성하며 속도1.35wu/s·반경.16·수명6초·상한12. 새탄은 tick끝에서 생성해 다음 tick부터 이동, 양쪽 이동 swept 충돌·공통무적·접촉시소멸·bosskill즉시정리를 유지한다. 피해는 R15 규칙18이며 R09의15를 잘못 복사하지 않는다. 별도 homing·회전 궤도·적탄 반사·거미줄·slam·돌진이 없다.

HP60%·30%에 ex17 각6한번·총12, 치명타에는 추가 소환 없음. lowFX에서도 비점멸 세 방향 예고와 실제 적탄을 배우/무기/입자 위·HUD 아래에 유지한다. 예고 길이는 Core 속도×수명, 원점·저장방향·반경을 실제 WorldProjection으로 읽고 PNG 크기로 판정을 결정하지 않는다. 시간/선택/정지는 기존 Core clock을 따른다.

## 상태·보상·챕터 완료

R14 between15→R15는 일반 라운드 전환이다. 이전 R03/R06/R09/R12 네 receipt·RunId·장비·진화·레벨/XP·영구스냅샷·프로필을 유지한다. 고정500/선택고갈100/일반성장고갈25/완료280은 각각 once다. 후보최대3·가능진화우선·이미진화제외·고정후보무지급Load·남은성장·완료회복 순서를 바꾸지 않는다. R15 완료현재Boss와 네과거history를 함께 남기며 다섯번째 과거필드는 다음책 제작 전 불필요하다.

root3과 R10~R14·창세기의 기존fingerprint/shape는 보존한다. R15 normal/intro/boss/reward/settlement/between16까지만 좁게 지원하고 R16 전투·미지원 필드는 원문보호로 거절한다. R09와 같은 데이터 형식이면 Boss3/World4의 세방향 적탄 shape를 재사용하는 것을 우선 검토한다. 라운드가 새롭다는 이유만으로 버전을 올리지 않고, 실제 필드가 더 필요할 때만 최소 named shape를 계획·독립검토한다. 기존 R12 Boss4/World5 history를 같은 버전으로 바꾸거나 R09의 정책/피해/식별을 바꾸지 않는다. 적탄 ID·좌표/방향·BornAt/수명·고정후보·지원mask의 저장/복구 검증은 유지한다.

출애굽기 전체 완료는 설치끝과 구분 가능한 `ExodusComplete` 같은 명시 상태로 제공한다. 기존 Genesis `ChapterComplete`/`CanEnterExodus`의 R09→R10 의미를 깨지 않는다. UI는 R15 완료를 출애굽기 완료/성막 완성·다음책 레위기 준비중으로 표시한다. Load는 장비·성장·진행을 보존하며 레위기 자동진입이나 강제 새도전이 없다. 새도전은 별도 명시버튼/입력일 때만 R01/schema2로 가고 영구프로필·잔액은 유지한다. 미설치 R16 시작 버튼은 활성화하지 않는다.

## 외부 아트

외부 `art-exodus-r06`의 8PNG·27frames 제작과 독립 정적 QA는 PASS이며 막는 HIGH/MEDIUM은 없다. 정확20파일 whitelist와 manifest `e765ce7bdf06e0ad717d88915ecec837e788294203b39ced04d5a78b3c8ff141`를 인계한다. QA 근거는 외부 `art-readiness-reviews/exodus-r06-review.md` 및 GC 문서다. 실제 Core 시계·투영·Unity·혼전·기기는 미검증이며 제품 임포트 승격과 구분한다.

최종 UI는 R12 실제660px에서 발견한 보스명/HP/phase 한줄 잘림 교훈을 재사용한다. 긴 보스명·HP와 상태 설명을 두 줄로 분리하고 장비/진화 행과 겹치지 않게 배치한다. R15 이름/상태 실제 문자열로 대표660px 확인을 하되 전투·저장 전체검사나 배율별 전행렬을 추가하지 않는다. 최종 Player를 만들기 전에 QA seed의 정확한 DataMember명과 보상 가능/만석 자동100 분기를 기존 Core 준비 경로로 확인해 준비 오타/정책 오해로 재빌드하지 않는다.

새 `art-exodus-r06/`만 선행 제작. 일반 stable IDs `enm_ex16-greedy-hook_move_32`, `enm_ex17-twisted-weave_move_32`, `enm_ex18-ruin-shard_move_32`: 각4frame, fps10/8/6, PPU32/pivot16,1/shadow small/wide/wide. 보스 `bos_defilement-weaver` move4/windup3/attack2/death4의64px4PNG, pivot32,1/기존boss40x8shadow. 쇳조각 `prj_defilement-shard_spin_16` 16px2frame/8fps/pivot8,8. 총8PNG27frames, right-parentflip/binaryalpha. 보스 move6fps·attack12fps·death8fps, windup은 Core1초의 0초·1/3초·2/3초 시점 세 자세다. 실제 공격뒤 짧은 attack clip은 저장boss시계에서 유도하고 첫발사 전 attack으로 보이지 않게 한다. 적탄 그림 회전은 표현일 뿐 고정 이동방향을 바꾸지 않는다.

보스는 검은 실과 부서진 금속의 창작 몸체로, 성막/언약궤/실제사람을 괴물로 그리지 않는다. lane PNG 없음. 원본격자/manifest/상대경로최소계약/정확public whitelist·정상regen1회·혼합/흑백/예고·적탄 최대3보드, Codex 스크립트픽셀/Claude Design미사용을 명시한다. 독립 QA 뒤 제품복사·runtime 임포트를 별도로 진행한다.

## 종료 게이트

관련 Core 검사는 실제3방향·피해18/소멸/무적·정지/수명·예고/실탄Load·지원6/12·playerdeath우선/killclear·네history·고정500/100/25/280·완료Load/NewRun·future원문보호를 묶는다. 원래구조 최소변이·한무기한seed 실제좌표 연속 CLI를 기존시리즈로 확인하고 최종게이트1회만 실행한다. 마지막 시리즈는 R09→R15를 포함하되 기존 R01 prefix를 재사용할 수 있고 기술HP/장비를 자연밸런스로 주장하지 않는다.

UI/월드/Host 파일별 소유·Editor1명·관련검사/한정상PC앱/한trace·대표3화면/새processLoad→독립code/UI→프로젝트GC→commitpush/해당SHA CI 후 이 승인범위를 종료한다. 실앱 기술 직접hit/timeout과 실제좌표 CLI·자연완주/native/mobile/performance 미검증을 구분한다. 보호 사용자앱/저장 불조회, 전역 GC 자동승격·새무기·스킬·다음책·모바일 작업은 없다.

## Unity 최종 소유 배정

Core 반환 후 Host/catalog·Host/Play 기대·최종Editor/빌드는 ctx_29961211b111 단독소유다. UI 표시네파일·새R15UiTests·과거UI시험의현재설치끝 기대는 UI담당, AdamArtWorld/AdamChainWarning의Rules연결·새ExodusR06Art/Importer·WorldTests·승인20복사는월드담당 단독소유다. Host.ExodusComplete는Session.ExodusComplete를읽는공개계약으로정하고 R09 ChapterComplete/CanEnterExodus 의미를유지한다. Core독립PASS와 UI/월드 고정반환 뒤 통합자만 sync/import/관련묶음·정상앱1개/3화면/새processLoad를실행한다. root는Resume15 선택변이와최종리뷰/GC/업로드를맡는다. R15완료후다음책/무기/모바일자동착수없음.
