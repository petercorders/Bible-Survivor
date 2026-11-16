# R08 요셉 — 시기·감옥·기근

## 최신 사용자 종료 범위

2026-11-16 사용자 “일단08까지만진행해줘”. 현재 R08 구현·관련 검증·독립 코드/UI 리뷰·프로젝트GC·commit/push 및 해당SHA CI까지 완료한 뒤 정지한다. R09 개발/아트와 R01~R09 최종 연속 검증은 보류하며 별도 재개 지시 없이 시작하지 않는다. R08 완료 저장은 between9/R09 미구현 대기로 보존한다.

## Unity 단일 통합 인계

고갈 교정 포함 Core/art 독립 PASS 뒤 sync1회·R07→R08→between9 Host/catalog·요셉/R09 한국어 안내·승인3PNG12frames sidecar를 한PC앱으로 연결한다. 기존두보스receipt·G3좌우·진화·보상명/짧은death는보존하고H에추가위험선을만들지않는다. 일반고갈의Core후보ID를읽어 `골드 주머니`/`재화`/`영구 골드 +25`처럼 실제효과를표시하며 가짜무기레벨/최대단계로보이지않게한다. 선택은기존Host/Core경로만사용하고Unity가골드를별도지급하지않는다. 보스의골드100선택표시와혼동하지않는다.

관련Host검사는R08이력/진화·해금null/성장140gold회복·between9/commit실패sameop재시도·고갈후보SaveLoad와선택25gold once·클립/fps/shadow를다룬다. 기술앱대표기록은세구간Core근거재사용·중간추적/Load·유효최대장비에서Core가생성한gold25후보의실제선택/재개·완료/새process동등을묶고 최대3화면(일반·고갈카드·완료)을남긴다. 후보/보상판정은주입으로우회하지않고유효phase/seed·Reload pause를먼저확인한다. 직접처치/drop/timeout등준비는명시하고실제1020kill앱완주로확대하지않는다.

Unity소유는Runtime/Editor/관련tests·3클립whitelist/index/importer·DLL/provenance sync·progressretro, Core제품/수치/저장형식/CIhooks/기존원본/사용자앱저장은변경하지않는다. 새suffix/no-saveprobe/정확자기PID·자기저장복구/보호preview·unknownQA·iOS불조회유지. 고정source최종Unity검사1회/단일macbuild/대표3화면·독립코드/UI→GC→commitpush/CI. 소유내실제결함최소교정은지속승인으로진행하고행렬/새toolhelper/부수기능은추가하지않는다.

## 진행 차단 교정 — 일반 레벨업 선택 고갈

2026-11-16 coordinator 결정. 실제 R07완료→R08 연속전투에서 장비가 모두 최대레벨이 되어 `EnsureChoices`가 예외를 던졌다. 사용자 지속 개발/세부 결정 위임에 따라 일반 성장 후보가0개일 때만 `골드25` 단일 선택지를 제공한다. 보스 골드100 fallback과 별도 정책/표시이며 기술 초기 경제값이다. 가능한 장비 후보가1개라도 있으면 이 fallback을 넣지 않는다. 선택해야 pending level1개와 ChoiceSerial1개를 소비하고25gold를1회 지급하며, 후보 생성/Load만으로 지급하지 않는다. 여러 pending은 기존처럼 하나씩 처리하고 XP/레벨을 잃거나 무한 선택을 만들지 않는다.

기존 명시 DTO/ID 검증을 최소 확장해 고갈후보·선택상태를 저장/재개하고, 알 수 없는 미래 fallback을 원문 보존 거절한다. 이미 최대장비가 된 과거저장도 정당하게 재개할 수 있어야 하며 기존 유효 장비후보 저장의 의미를 바꾸지 않는다. 보스 후보의 고갈 사전검사도 일반 레벨업 fallback을 이유로 막지 않게 확인하되 보스 보상 자체를 일반25gold로 바꾸지 않는다. 관련 고갈/후보Load·동일op retry/복수pending·정산 진입 회귀1개와 정확옛throw변이, 기존R08관련9검사·같은실패CLI만 재실행한다. 새별도경제시스템/무기/전행렬/도구프로젝트는추가하지않는다. Unity는후속에서한국어명·실제25gold효과를표시한다.

2026-11-16. R07 commit/해당SHA 두CI 종료 후 지속 개발 승인으로 진행한다. 다음 산출물은 R07완료→R08→성장/정산→between9 PC 앱이며 마지막 보스 R09는 후속이다.

catalog genesis_08의180초/생성마감170초/동시110/총1020/원시XP3500, wave270/340/410과 조합을 유지한다. H1 시기의 그림자160HP/10피해/.65, H2 감옥 사슬령340/12/.40, H3 기근의 마수400/13/.55 모두 추적·접촉이다. 첫 구간H1만, 다음H1/H2, 마지막H1/H2/H3가 이야기 흐름이다. 사슬회전은 표현뿐·속박없음, 마른몸/곡식포식은 표현뿐·섭취무한성장없음. 새디버프/탄막/돌진/공유HP/피해지형은 없다.

완료140gold·성장 해결 뒤 최대HP회복·between9 once를 기존 일반 정산으로 연결하며 이번 라운드 해금은 null이므로 새 해금/장비 지급을 만들지 않는다. R03/R06 보상/완료receipt·과거일반완료 사실·첫3진화/레벨XP/킬remainder/구매snapshot을 유지하고 R07좌우특화가H에유출되지않게한다. 과거R07저장의무쓰기읽기·기존미래본문보호유지. R09진입은거절하고 새버전/저장필드는필요없으면추가하지않는다.

Core작성자소유는 Core/관련tests·Sim/최소기존검사anchor·progressretro, Unity/DLL/provenance/art/CIhooks/GUI/사용자저장/commitpush금지. 기존경로재사용·관련전환/세구간조합·추적/접촉·이력/진화/정산중복/해금null·midLoad/future거절과한무기한seed실좌표CLI만. 최소실제원구조변이/기존근거재사용,전행렬·새toolhelper·넓은최적화프로젝트는추가하지않는다. 동시110은기능상한으로확인하되실기기성능PASS를주장하지않는다.

아트담당은외부art-genesis-r08에H1~3move32x4각1/총3PNG12frames·최소source/manifest/계약·normalregen1회·혼합/흑백/실배율최대3board를준비한다. 기존PPU32/pivot16,1/rightparentflip/binary/shadow재사용. H2회전사슬은clip내안전여백·H3곡식표현은피해지형/성장기믹처럼그리지않는다. 요셉/감옥수/굶주린사람자체악마화가아닌창작방해몬스터로구분한다. 실제Design제품과스크립트픽셀제작구분/비공개자료repo복사금지.

Core/art독립PASS후Unity단일작성자가sync1회·R07→R08→between9 Host/catalog/한국어안내·3클립sidecar/세구간·저장재개/성장140gold회복을한PC앱으로묶는다. 기존두보스/좌우G3/첫3진화표현과원본을보존한다. 준비seed/유효phase/Reload pause를먼저확인하고소유내필수결함은지속승인으로최소교정. 고정source최종Unity검사1회/단일macbuild/한기록·최대3화면,독립코드/UI→프로젝트GC→commitpush/CI. Core제품/수치/저장형식/CIhooks는Unity소유밖. 보호preview/unknownQA/iOS불조회·새suffix/no-saveprobe/자기PID와자기QA저장복구유지. 자연완주·밸런스/native/mobile/performance미검증과기술fixture를구분한다. Sol기본/자기관측usage만기록·실제비용절감률추정금지.
