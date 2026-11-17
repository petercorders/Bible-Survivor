# R13 미리암 — 홍해 이후 공동체

2026-11-18. 전체 R10~R15 지속 승인 범위의 다음 일반 라운드다. R12 Core/Unity가 작성·검증되는 동안 이 계획과 외부 아트만 준비한다. R13 제품 Core 수정은 R12 소유 반환·검토·업로드 뒤 별도 배정한다.

## 이야기와 기존 규칙

출15~16장: 미리암의 승리 노래 → 마라와 엘림 → 만나를 받는 공동체를 세 구간에 압축한다. 마라·만나는 동행 공동체의 사건이며 미리암이 기적을 행하거나 만나를 내렸다고 말하지 않는다. R12에서 끝낸 실제 추격/홍해를 다시 전투로 재연하지 않는다. 패주의 잔영은 실제 살아남은 병사의 시신/영혼이 아니라 압제와 앙심의 창작 마물이다. 백성 자체·찬양·하나님의 공급을 악마로 만들지 않는다.

플레이어는 계속 사용자 자신, 3분의 혼합·유한 웨이브와 기존 추적/접촉만 사용한다. 물/배고픔 수치·식량 수집·호위 HP·원망 디버프·드롭 강탈·조작 반전은 없다. 첫 보스 뒤 숨 돌리는 구간으로 낮은 HP 적을 충분히 섞고 신규 공격 패턴을 추가하지 않는다.

## 기술 초기값

ID `exodus_04`, local4/global13, 180초/생성마감170초, 동시110/총1080/원시XP4800/완료240gold/해금없음/보스없음. 자연 플레이 실측이 아닌 초기 조정값이며 XP는 기존 정수배분으로 정확히 맞춘다.

| ID | 적 | HP | 접촉 | player속도비 | 행동·외형 |
| --- | --- | --- | --- | --- | --- |
| ex10 | 패주의 잔영 | 150 | 10 | .60 | 찢어진 깃발/깨진 장식의 가벼운 추적 마물, 실제 병사 아님 |
| ex11 | 광야원망령 | 280 | 12 | .60 | 모래와 빈 항아리/벌어진 입 실루엣의 군집, 혼란효과 없음 |
| ex12 | 탐식의 자루마 | 600 | 15 | .35 | 큰 곡식자루와 입의 느린 고체력형, 실제 보상/드롭을 먹지 않음 |

| 생성구간 | ex10 | ex11 | ex12 | 합계 |
| --- | --- | --- | --- | --- |
| 0~60 | 224 | 56 | 0 | 280 |
| 60~120 | 216 | 144 | 0 | 360 |
| 120~170 | 220 | 132 | 88 | 440 |

## 저장·완료 계약

R12 완료between13→R13은 기존 일반 라운드 전환이다. 새 chapter 진입이 아니며 R09→R10의 별도 원자적 진입 경로는 그대로 둔다. R12 완료 보스의 현재 receipt를 새 명시 `CompletedFourthBoss`(또는 독립 검토한 동등한 한 필드)에 보관해 R03/R06/R09 세 과거 기록을 덮지 않는다. RunId·장비·보조·XP/레벨·진화·영구 snapshot·프로필을 이어가고 R12 보스400/선택100/완료220을 재지급하지 않는다.

root3과 R10~R12 fingerprint/기존shape를 유지하고 R13 normal/round_settlement/between14만 좁게 지원한다. R13 새 World는 빈 돌진·적탄/새 clock·피격시계로 초기화하며 과거 R12 boss4/World5는 과거 receipt 검증과 구별한다. 현재 적은 전부 추적 접촉형이므로 R12의 dash ledger가 넘어오면 거절한다. 최소 World shape는 구현자가 기존 R10/R11과 대조해 정하며 새 version을 필요 없이 올리지 않는다. R14 전투·알 수 없는 필드는 미래형식 원문보호로 거절한다.

완료는 드롭회수→남은성장(고갈시25gold)→완료240once/해금없음/최대HP회복→between14. 콘텐츠끝은 R14 준비중이며 출애굽기 전체 완료라고 표시하지 않는다. 죽으면 창세기 R01 새 도전/schema2와 영구프로필 유지. 정확 PendingLevels/후보수/Choose횟수를 따로 기록한다.

## 외부 아트·실행 순서

새 외부 `art-exodus-r04/`만 선행 제작한다. stable IDs `enm_ex10-retreat-echo_move_32`, `enm_ex11-wilderness-grumble_move_32`, `enm_ex12-greedy-sack_move_32`, 각32px×4frame =3PNG12frames. fps8/8/6, PPU32/pivot16,1/right-parentflip/binaryalpha, 기존shadow small/wide/wide 권고를 계약으로 확정한다. 공개 상대경로·stable ID 재사용, PNG에 피해/오라/디버프 표시를 넣지 않는다. 원본격자/manifest/최소계약/normalregen1회·혼합/흑백/x1~3 최대3보드, Codex 스크립트 픽셀 출처·Claude/Design 미사용을 명시한다.

R12 종료 후 Core 관련검사·네receipt/진입Load·정산retry/240once·future원문보호·원래구조 최소변이·한무기한seed 좌표 CLI→독립 리뷰. 승인아트와 UI/월드/Host 소유 분리·Editor1명·관련검사/한 정상PC앱/대표3화면/새processLoad→독립code/UI→GC→commitpush/해당SHA CI 후 R14로 이어간다. 과거 전체조합·픽셀·빌드 반복을 추가하지 않는다. 자연 밸런스/native/mobile/performance 미검증을 구분하고 보호 사용자앱/저장은 불조회다.

## 아트 선행 결과

외부 `art-exodus-r04`의3PNG12frames·대표3보드 독립 정적 QA PASS, 막는 HIGH/MEDIUM과 필수 수정 없음. 임포트는 정확14 public whitelist만 사용하며 개인경로/부수파일이 없다. fps8/8/6·PPU32/pivot16,1·binary/right-parentflip·shadow small/wide/wide를 유지한다. 원본 manifest SHA256 `fd6b30a5cdca295b42d4d84c53f5e7b88573c3e8641e16119914ebc8ed949379`. 외부 `art-readiness-reviews/exodus-r04-review.md`와GC를 참조하며 실제 loop/혼전/Unity/모바일은 NOT_RUN이다. 이 PASS를 R13 Core/앱 완료로 세지 않는다.
