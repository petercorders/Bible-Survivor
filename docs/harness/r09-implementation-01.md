# R09 유다 — 창세기 마지막 보스

## 최대5명 작성과 통합 소유

2026-11-17 사용자 속도 개선 지시로 이 프로젝트 동시 제품 작성자 상한은5명이다. 상한을 채우기 위한 빈 작업은 만들지 않고 작성 중/배정만/완료를 구분한다. Core와 외부 아트가 고정된 뒤 Unity도 세 파일 영역으로 병행한다. 전역 앱/모델 설정은 변경하지 않는다.

| 역할 | 단독 편집 파일 | 공유 경계 |
|---|---|---|
| Core | Core/관련C#tests·Sim·기존anchor | Unity/아트 금지, 고정후독립리뷰 |
| Codex 아트 | 외부art-genesis-r09 | 현재 제작완료, 독립QA전임포트금지 |
| Unity UI | AdamGame.cs/AdamChoiceText.cs/AdamRoundPresentation.cs/AdamBossPresentation.cs 및 새 AdamR09UiTests | Host/월드/loader/기존공용시험 금지 |
| Unity 월드 | AdamArtWorld.cs·새 AdamR09Art.cs/AdamChainWarning.cs·AdamR09ArtImporter.cs·Resources/ArtR09/및 art/art-genesis-r09 whitelist·새 AdamR09WorldTests | UI/Host/Core/DLL 금지, QA승인아트만복사 |
| Unity Host·최종통합 | AdamHost.cs/AdamCatalog.cs·새 AdamR09HostTests 및 기존round Host시험·sync산출물·QAfixture/빌드·progressretro | 위작성자반환전UI/월드파일편집금지 |

Editor/빌드/Player/GUI는 Host·최종통합자 단독 실행이다. 다른 작성자는 코드/아트만 작성하고 Editor를 열거나 자기 빌드를 만들지 않는다. Host의 AwaitingR09/ContentComplete와 UI의 Name(9), R09Enemy(id), boss명·그림자 계약을 먼저 공유하며 새 이름을 임의로 바꾸지 않는다. 월드는 Core 공개 ChainDirections/HostileProjectiles·현재boss좌표/clock을 읽고 기존WorldProjection을 사용한다. 완성범위를 반환받고 단일 통합자가 관련검사/한PC앱/대표3화면을 묶는다. 독립 리뷰는 제품작성자로 세지 않으며 자신의 작성범위를 스스로 독립검토하지 않는다.

2026-11-17. 사용자 “09개발 진행해줘”로 R08 종료 경계를 재개한다. 이번 범위는 R09 구현·관련 검증·독립 코드/UI·GC·commitpush/해당SHA CI와 R01~R09 대표 연속 진행 최종 확인까지다. 출애굽기·추가무기/스킬·모바일/성능 확대는 하지 않는다. 사용자 최신 지시로 Claude를 제외하며 자동 재호출하지 않는다. 아트는 Codex가 기존 픽셀 소스 방식으로 제작하고 Claude Design 미사용을 명시한다.

## 기존 확정 수치

catalog genesis_09: 180초/생성마감170초/동시120/총1140/원시XP4200, wave300/380/460·기존I1/I2/I3조합. I1 은전포식자160HP/10/.85, I2 은폐가면260/11/.60, I3 형제결박마600/14/.35 모두 추적·접촉·통로봉쇄없음. 일반 종료→기존2초 보호intro/드롭회수/무조기회복→보스전.

은잔의 참소자(창세기44장 은잔·분리의 창작 방해 마물): HP16000/접촉20/고정350gold, 주기5초·예고1초·3방향 느린 사슬투사체/피해15,60%/30%에I2각6한번·총12상한. 유다/요셉/베냐민 자체를 악마로 그리지 않으며 새 호위체력/희생조건/속박디버프는 없다. 기존boss생존/치명우선/regen/소환직후좌표동기화/receipt·고정후보·첫3진화/일반고갈25정책을 유지한다.

## 사슬 공격 기술 초기값

보스 속도비.30/반경.60/introoffset(0,+3)은 기존값을 재사용한다. 첫 발사는boss elapsed5초,4초부터 정지 예고하며 당시 플레이어 방향을 고정한다. 기본방향과±20도 세 방향을 저장해5초 시점의 현재 보스 위치에서 발사한다. 겹친목표는(1,0),발사뒤유도/회전없음. 속도1.35worldunit/초(기본player3보다느림),반경.16,수명6초·각투사체접촉1회소멸이며접촉이무적으로막혀도소멸한다. 방어/무적·피해우선은기존플레이어경로로적용한다. 시작방향/좌표/잔여수명/식별자/후보·소환mask/다음발사시간은실제상수/fingerprint와명시저장에포함한다. 이는초기조정값이며자연난이도확정이아니다.

적투사체는기존플레이어투사체와별도bounded목록/ID로관리하고상한12를넘는저장은거절한다(정상5초주기/6초수명에서는최대6). 기존탄을먼저적분·swept circle충돌처리하고예고만료의새탄은그tick끝좌표에서생성해다음tick부터이동한다. 동일tick플레이어사망은무기공격/보스처치보다우선하며보스처치즉시잔여적탄을보상없이제거한다. pause/성장선택/intro/reward에는시간진행없음. 원래아레나clamp를추가하지않고수명으로회수한다. 필요최소named적탄shape/version으로세계저장을확장하되R01~R08/R03·R06기존shape/fingerprint/무쓰기Load를보존하고미래본문은원문보존거절한다.

Unity예고는실제현재boss위치와저장된세방향을Core속도·수명의예상구간으로투영한다. 저효과에서도배우위/HUD아래에비점멸3방향표식을유지하고발사뒤에는실제Core탄좌표를그린다. 예고선자체는피해판정이아니다. 경고와탄을원형slam으로대체하지않는다.

## 보상·창세기 완료

고정350/선택fallback100/일반levelup25/완료160gold는별도식별·once로유지한다. 보상최대3고유/가능진화최소1·이미진화제외·동일후보/정책저장·선택→남은성장→완료160/해금null/최대HP회복을적용한다. R03/R06두완료history와R09현재완료receipt를동시에보존하고덮거나재지급하지않는다. 마지막상태는창세기완료·다음책미구현대기(기존between10또는최소명시chapter경계)로저장하며장비/레벨/XP/진화/잔액·구매를초기화하지않는다. 이어하기는같은완료상태,새도전은명시버튼일때만R01. 자동출애굽기진입/엔딩후강제리셋없음.

## 배정·좁은 종료 게이트

Core작성자: Core/tests·Sim/필요최소기존검사anchor·progressretro만,Unity/DLL/provenance/art/CIhooks/GUI/사용자저장/commitpush금지. 관련시험은세발방향·저속실제피해/소멸/무적·정지/수명/적탄SaveLoad·60/30지원/12상한·同tick사망/보스kill탄정리·과거두receipt·보상고갈/중복·창세기완료Load/future보호를묶는다. 기존전행렬/새toolhelper없이최소실제원구조변이,한무기한seed대표연속CLI로확인한다. 최종한번의R01~R09연속확인은기술초기HP/장비를명시할수있으나좌표공격/보스kill을주입하지않고실제피해/처치·timeout·선택·중간Load를구분한다. 자연난이도/완주가능성PASS로확대하지않는다.

별도Codex아트담당: 외부art-genesis-r09폴더만,I1~3move32x4/보스64move4·windup3·attack2·death4와16px사슬탄2frames·기존shadow재사용. PPU32/pivot16,1·32,1/탄8,8/rightparentflip,binary최소source/manifest/계약·normalregen1회·혼합/흑백/예고·탄대표최대3board. 은잔/곡식자루/분리사슬의창작마물,죽음후사슬해제표현은짧은clip뿐. Claude·ClaudeDesign·비공개blob/캔버스호출없음,제작모델/스크립트픽셀출처명시. 독립아트검수후Unity임포트한다.

Core/art독립PASS→Unity단일작성자의sync1회/Host·세방향공격/실제적탄·보상/창세기완료/저장재개·관련검사/최종고정source1회/단일PCbuild·한기록/대표3화면→독립코드/UI→GC→commitpush/CI후정지. 유효seed/phase/슬롯/Reloadpause부터읽어지난실패를반복하지않고소유내실제필수교정은지속승인으로진행한다. 보호preview pivot9534f7594886(PID14039/window217455)/unknownparticlesqa(PID97399/window198185)/사용자iOS·저장은불조회,새suffix/no-saveprobe/정확자기PID·자기QA만원복. Sol기본·어려운미해결검증만Astra검토,자기관측usage기록·비용절감률추정금지.
