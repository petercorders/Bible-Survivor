# Visual quality 01 통합

승인 `docs/harness/visual-quality-01.md`, U1의5파일 반환 후 U2가 최종 표시·Editor·PC를 단독 통합했다. 상세 API/실패·한계는 ignored `evidence/visual-quality-20261118/full-report.md`, 인계는 `handoff.md`다.

## 표시와 계약

- 실제 Fable5.2/Claude Design 방향·tokens/contract·로컬3보드를 읽고 직접 열었다. Design 실렌더 미확인과 생성기 PNG를 구별하며 U2는 Claude/Design/추가 worker를 실행하지 않았다.
- 독립3묶음 PASS 뒤 정확31PNG+3manifest만 `Resources/VisualQuality01/UI|World|Effects`에 복사했다. private 자료·기존 Art03/Bible/Jawbone/Rxx·index/hash·Core/Host/DLL/save 정책은 변경하지 않았다.
- static `AdamVisualQualityArt.GetClip/TryGetClip/Sprite`는 AdamArtClip을 쓴다. Art03→Bible→Jawbone→visual 최종 overlay이며 기존 등록·fps·loop·attach를 검증한다. keycap L,B,R,T는3,4,3,3이다. 상세 SHA/34원본 binding은 `art-binding.json`에 있다.
- 지형에 meadow/새 풀·파인 길·두 톤 그림자를 실제 사용하고 수관은4corner XYflip/centre60%clear/lowFX off다. 실제 명중/처치 FX는 XP/배우/필수예고 아래다. 선택 붉은 띠 뒤 기존 Core darkrim/whitecore/arrow·실적탄을 보존한다.
- 턱뼈 반픽셀 grip/손 follow/facing freeze/6layer 반전과 기존 Core clock을 유지한다. P3 trail/edge는 기존 attach를 현재 손/head 방향에 투영해 실제 사용한다.
- 물맷돌은 새 실제 projectile ID/velocity에서만 손끈을 표시하고 실제 current position에20px cap trail을 그린다. 실제 명중 방향 조각·세 무기 PCM을 연결했다. 성경은 공전 접선/page-contact이며 손 무기 회전을 강제하지 않는다. 빗나감 가짜명중·새 판정·hitstop은 없다.
- 실화면의 밝아진 잉크를 좁은 swatch로 발견해 Linear PC 표시 복사/solid 색곱을 교정하고 글자 색 경로는 별도로 유지했다.31float texture/74sprite raw budget1,126,496 bytes, Release/Reload/quit 수명이며 PNG/alpha/등록은 보존한다. 현재 PC 관측이며 exact 색도학·전체 메모리·모바일 지원 PASS가 아니다.

## 검증·반환

- 관련36/36, 색/UI delta13/13, 최종 관련2/2 PASS를 기록했다. 중복 시험을 고유51건으로 합산하지 않는다. 원 Art03 alpha membership은 원본 import 객체로 유지하고 overlay는 별도 membership으로 검증한다.
- 최종 통합 build/noHost probe/Player child exit0, capture complete PASS. `source-app-binding.json`의 제품 C#115파일이 빌드 사본과 bytes 동일하며 실앱 binary/세manifest를 결속한다. 기본 실행은 정상 Adam이며 QA flag에서만 기술 제어기다.
- 실제 숫자3/F6/F7/Esc/Enter/성장카드·pause/resume 포인터와 pause World 동결을 확인했다. function key 미지원은 정확 자기 PID native CGEvent로 대체하고 실제 app phase로 수신을 확인했다. 후속 snapshot 실패와 실제 전이를 구별하며 최종 색 변경이 건드리지 않은 입력 경로만 재사용했다.
- 실제 hit/played는 jawbone1/1, sling5/5, bible3/1(기존35ms 제한)이다. sling checkpoint7발사/5hit/1live로 무명중 소멸1개 이상이다. 최종3무기 `video/`는각60PNG/10fps/6초 무음, engine WAV는48k stereo, 생성 PCM은22.05k mono다.6 WAV 모두 비무음/peak<1이다. `video-with-audio/`는 편의 결합이며 청각 취향/latency/정밀 동기화 PASS가 아니다.
- 660/1100 전투·성장·보상·ExodusComplete, retry·death 화면을 수집했다. lowFX05/06에는 필수3방향/rim/core/arrow/실탄이 남는다. pause World/자기 저장·mute0voices·매 draw World/전체 저장 bytes 무변경을 확인했다. 기술 seed/위치/clock 재현이며 자연 밸런스/전체15라운드/실기기/성능 검증은 아니다.
- QA OnDisable null/미지원 probe API/정상 CanStartNew 거절에 따른 잘못된 무기 조건/Bible 무명중 조건을 실제 실패로 보존하고 필요한 부분만 교정했다. 제품·QA 실패와 의도된 retry IOException을 구별하며 과거 전체 suite/픽셀 행렬을 반복하지 않았다.
- 새 독점 identity/noHost/realpath/정확 자기 child exit·자기 저장 absent 복원 PASS다. 사용자 PID98774/window244879/r1580111c7154f5·iOS·unknownparticlesqa·pivot9534f7594886 및 저장은 불조회·불조작·불종료다. 사용자 최신 앱을 교체하거나 실행하지 않았다.
- 자기 actual model `gpt-6.1-sol`, phase start/end total delta21,317,050이며 root/Fable/QA 사용량을 합산하지 않는다. 비용/절감률 미계산이다. 소유를 root에 반환하며 초기 독립 코드 PASS 뒤 최종4runtime/색 시험 delta·시청각 리뷰와 commit/push/CI는 root가 맡는다.

## 독립 최종 리뷰

`docs/reviews/visual-quality-code.md`는 최종 네 Runtime/색 시험 delta와 source/app 결합까지 PASS로 마감했다. `docs/reviews/visual-quality-ui.md`는 지정 660/1100 화면과 세 무기 영상의 관측 표본 범위 PASS이며 차단 HIGH/MEDIUM은 없다. 활성 반전·돌 release부터 trail까지 연속 궤적·모든 성경 명중별은 독립 표본에서 직접 관측하지 않아 생산자 실행 근거와 구분한다. 주관적 thud/crack/page 청취는 도구 부재로 NOT_RUN이다. 원본 engine WAV와 무음 영상·편의 AV 결합을 구분해 전달하며 음향 취향·정밀 동기화·자연 밸런스·모바일·성능 PASS를 주장하지 않는다.
