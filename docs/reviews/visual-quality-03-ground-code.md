# Visual quality 03 책별 전장 독립 코드 리뷰

- 판정: **소스 범위 PASS — 막는 HIGH/MEDIUM 발견 없음. 도착한 source/app binding은 일치하며 최종 handoff는 PENDING.**
- 기준: `efb09c80e2368b615f579f6e0d2192ef1368f36d` 대비 첫 지형 단위, 2026-11-18 06:17:57 UTC checkpoint.
- 소유는 이 보고서뿐이다. 제품·타인 파일·Editor·앱·사용자 저장을 수정하거나 재실행하지 않았으며 새 worker·commit/push를 수행하지 않았다. 게임 이미지/영상을 조회하거나 채팅·외부에 게시하지 않은 텍스트 리뷰다.

## 검토 범위와 버전

변경된 `AdamArtWorld`, `AdamVisualQualityArt`, `AdamVisualQualityArtImporter`와 신규 `AdamBookGround`·`AdamBookGroundTests`, Resources의11PNG+manifest 계약을 읽었다. 외부 `art-visual-quality-03/RESULT.md`, `tokens.json`, `asset-contract.json`, production manifest와 독립 `art-readiness-reviews/visual-quality-03-review.md`를 참고했다. 후속 머릿장/출전띠·보스 연출/음향·경로 변경·장식 군집·중앙 비움은 이 판정 범위가 아니다. 기존 VQ02 코드/앱·턱뼈 리뷰는 변경하지 않은 경계에 한해 재사용한다.

위5개 C# 경로를 정렬해 `경로 UTF-8 + NUL + 파일 bytes + NUL`을 이어 계산한 단일 checkpoint SHA256은 `6259749b6885e35eead3808b04648d9c3388f5f19c2976e09bd44fb951128181`이다. U2 `ctx_9ddd3d60bbfe`의 source freeze 보고를 기준으로 검토했고, 종료 전 도착한 binding의5개 source SHA 및 QA 사본 bytes가 현재 검토한 파일과 일치했다.

## 소스 판단

| 경계 | 확인 내용 |
| --- | --- |
| 실제 책 선택·fallback | AdamArtWorld.Draw는 현재 Host.Session.RoundNumber를 넘기며 AdamBookGround.Exodus는10–15이면서 새 Sand clip이 있을 때만 true다. 별도 선택 상태·저장 flag·예시 라운드를 쓰지 않는다. 창세기1–9와 지원 밖 라운드·bundle absent는 기존 Ground/Path/Decor/Canopy 분기로 돌아간다. 새 hollow와 edge는 출애굽기 분기에만 추가되어 창세기 기존 표시를 보존한다. |
| 11개 계약·등록 | whitelist는 sand4/flag2/path1/decor2/hollow1/crest1의 정확한11ID다. Resources의 PNG 집합11개와 각 SHA가 manifest에 일치하고 manifest bytes는 외부 승인 production과 같다. SHA는 `a9e7b7b8c1b8293bc69f23b56a9aa79831553f5f18416194eb8122f725bb568c`로 loader 상수와 일치한다. 기존31+polish9의 ID/manifest 상수는 유지한다. crest2프레임은 등록만 하며 기존 UI를 교체하지 않는다. |
| 16mask·pivot | 기존 PathCell/PathMask의 십자 경로·N1/E2/S4/W8 인접 비트를 그대로 사용하고 새32×32/16frame strip을 같은 frame index로 고른다. path pivot16,16, pebbles5,0·scrub8,0은 기존 decor와 일치한다. 독립 아트의16frame 경계 검증을 재사용하며 새 길 픽셀이 기존과 같다고 주장하지 않는다. 게임 맵·충돌/이동 경로 변경은 없다. |
| hollow·draw order | 타일 뒤→hollow→decor→edge→contact shadow/FX→XP/배우/무기→필수 경고의 순서다. hollow는2tile lattice의 정수 hash·%3 gate·hash offset/flip만 사용하고 Core clock/RNG/actor 좌표를 변경하지 않는다. hollow는 단일64×64/1frame 전체 texture라 full UV flip이 해당 clip 영역과 일치한다. tile 자체는 회전/반전하지 않는다. |
| edge·lowFX | edge는 arena group 안의 창 pixel4/6/8px, alpha .3/.2/.1의 분리된12 rect다. 배우·XP·경고 이후 위에 덮지 않는다. lowFX에서는 edge만 끄고 static ground/path/decor/hollow는 유지한다. 기존 priority2·rim/core/arrow와 실제 hostile projectile 경로는 새 지형 분기의 feedback flag에 묶지 않는다. |
| loader·수명 | optional Book03 manifest가 있을 때만 기존 loader/importer 목록에 추가한다. strict manifest SHA/schema/ID·PNG hash/개수, Point/Clamp/PPU32/FullRect·무 mipmap/압축, sprite cell/frames/pivot/border 검증을 유지하고 stable spriteID에 새 prefix만 쓴다. hollow만 승인된 blend alpha[0,36]이고 나머지는 binary 허용집합이다. 기존 bounded Linear 표시 texture/sprite 생성과 Release/Reload/quit 해제를 재사용하며 원본 texture를 수정·파괴하지 않는다. |
| 비용·제품 경계 | visible radius는 기존 rx≤10/ry≤5.625를 넘지 않고 hollow도 해당 범위의2tile lattice를 순회하며 edge는12 draw다. draw 중 새 native 자원·Host/World·타이머/콜백을 만들지 않는다. Ground의 문자열 치환과 추가 GUI draw 비용은 존재하며 allocation·p95·모바일 성능을 측정한 결과는 아니다. 현재 tracked diff는 위3제품 파일뿐이며 Core/Host/catalog/save/기존 UI·입력·피해/전투는 변경하지 않는다. |

## 재사용한 시험·기술 실행 근거

`alpha-test-assumption-failure.xml`은 고유5케이스 중4 PASS/1 FAIL이다. 실패는 완전 불투명 타일의 관측 alpha `{255}`에도 `{0,255}`가 모두 있어야 한다고 요구한 시험 준비 오류다. 최종 시험은 binary에 허용집합 subset을 적용하고, 명시된 hollow alphaValues[0,36]는 동등성을 유지한다. `alpha-corrected.xml`의 해당1케이스 PASS와 앞선4를 합쳐 **고유5케이스 PASS**이며6개 고유 실행으로 합산하지 않는다. 책 선택/Genesis 동일 반환·두 크기 edge 비중첩·11clip pivot/alpha/16frame·정적 lattice를 관련 범위에서 뒷받침한다. 새 전체 suite나 원래 회귀 변이 검출을 주장하지 않는다.

`fallback.txt`는 optional Book03 폴더를 실제 제거해11ID absent, prior40 유지, 출애굽기의 기존 Genesis ground/path fallback을 확인하고 finally 복원한 근거다. 재실행하지 않았다.

현재 도착한 `build.log`의 최종 build exit0와 `restoration.json`의 probe26917/capture26918 각 exit0, `capture/complete.txt`를 읽었다. 자기 child 종료·자기 QA store absent→absent 복구 기록을 재사용했으며 사용자 identity/store를 직접 조회하지 않았다. 초기 `growth-fixture-failure`는 lifecycle 불일치, `drop-sync-fixture-failure`는 clock/enemy checkpoint 불일치와 후속 null 오류가 보존되어 있다. 이를 제품 저장 검증 완화나 모든 시도 성공으로 바꾸지 않는다.

capture fixture는 소유 완료 snapshot에서 R09/R15로 진입하고 HP10000·공격 일정·적 위치를 준비한다. XP는 실제 Session.Hit으로 발생한 Core drop ID를 retained World의 enemy/drop 상태와 맞춰 저장한다. 보스는 기술 timeout 뒤 실제 Tick으로 telegraph/저장된3방향을 얻는다. 정상 플레이로 라운드를 완주하거나 spawn/balance를 검증한 결과가 아니다. OnGUI 전후 World·disk bytes 불변 assertion과 controlled render를 실제 realtime 성능/자연 플레이로 확대하지 않는다. 기록된 대표 visible monster는 R09 i1/i2, R15 ex16/ex17이며 XP 각1·boss directions 각3이다.

## 수치·디자인·남은 결합

`runtime-ground-probe.json`은 창세기 대표RGB92/126/64, 출애굽기164/140/102와 edge/lowFX 차이를 기록한다. R15 warning의 white-core pixel 수1372/low1378과 `warning-live-probe.json`의 실제 saved direction 위치에서 흰 core·어두운 rim 표본은 경고가 lowFX에도 남는 제한 수치다. 전체 흰 pixel 수만으로3개 방향 각각의 선명도·가장자리/군집/색각 가독성을 인증하지 않는다. 독립 아트의 warm enemy/XP 정적 수치 및 디자인 대비비는 실제 게임 합성 화면의 직접 심미 관찰과 구분한다.

최초 checkpoint 뒤 `source-app-binding.json`과32파일의 `files-modified.json`이 도착했다. 검토한5개 C#와 QA copies, 앱 `BookQa.app`의 Adam.Unity.dll SHA `50760fb5975389eb7adae2981aaf8e95d93a7ec0b3a9598a8bdf4083e0a89946` 및 Core DLL SHA `e40985359fe9d367c14f8e19e990e20613e80e6e21b6815ed1a4743ca791eeaf`의 binding 일치를 직접 확인했다. baseline/product Core 동등·보호 제품 diff 없음은 U2 기록을 재사용한다. 전체 앱 파일 hash 행렬은 다시 계산하지 않았다.

**최종 handoff.md는 종료 전 확인 시점에도 미도착이므로 최종 인계 종합 판정은 PENDING**이다. 도착한 fixed-source binding 일치와 실제 build/probe/capture exit0를 무시하지 않되, 미도착 handoff를 읽었다고 주장하지 않는다. 최종 반환 후 필요한 인계 차이만 추가 확인하면 된다. 창세기/출애굽기 실제 합성의 주관적 가독성·모바일·성능·자연 플레이는 별도 NOT_RUN이다. 이 리뷰는 이미지 도구·Editor·Player·앱·저장·전체 hash/시험 행렬을 실행하지 않았다.

## 자기 사용량

기존 `record_model_usage.py`의 자기 phase `visual-quality-03-ground-code-review` start/end로 관측 세션 구간을 기록했다. 기록 시각06:16:39→06:19:50 UTC, provider event06:06:49→06:19:24 UTC의 delta는 input1,321,084/cache1,282,304/output6,315/total1,327,399이며 reasoning1,576은 output에 포함된다. start의 지연 tail을 포함하므로 요청별 정확한 귀속으로 사용하지 않는다. 실제 청구액·단가·절감률은 미계산이다.
