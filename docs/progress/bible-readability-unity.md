# 성경 책·후광 sidecar Unity 연결

2026-11-16 · 구현자 연결/관련 검사 PASS, 독립 실제 UI 리뷰 대기. 후보1의 독립 아트 검토는 막는 HIGH/MEDIUM 없이 PASS이며, 이번 단계는 새 게임 지원·자연 플레이 가독성 최종 합격을 뜻하지 않는다.

`art/art-bible-readability-01/`에 공개 계약·팔레트·편집 가능한 픽셀 원본·제작/검사 스크립트·manifest·PNG2만 복사했다. 원본 manifest의 출처/권리와 candidate 상태 문구는 역사적 입력으로 보존했다. 게임 PNG는 Claude 작성 스크립트 출력이며 Design 제품에서 내보낸 그림으로 보고하지 않는다. private canvas/RESULT/원문 세션/보드·GIF는 공개 복사 대상에서 제외했다.

`Resources/BibleReadability01`의 두 PNG만 기존 stable ID `wpn_bible-book_spin_16`, `fx_bible-glow_static_20`에 연결한다. `AdamArt`는 Art03 원본79개 등록을 그대로 읽고 검사한 후 Sprite 두 항목만 sidecar로 선택한다. 원래 index/clip이 계속 셀·중심pivot·fps·loop·품질·배치를 결정한다. 새 importer는 별도 경로에서 FullRect/PPU32/Point/Clamp/무압축/no mip/원본 alpha와 결정적 새 sprite ID를 설정한다. 두 ID와 승인 SHA/manifest만 허용하고 누락·변조·미지원 ID는 거절한다. Art03의 PNG79/manifest/index/source/기존 GUID는 바꾸지 않는다.

| 대상 | 기존 계약 유지 |
| --- | --- |
| 책 | 16×16,2frame,6fps loop,중심8,8,core,WeaponFX |
| 후광 | 20×20,1frame,0fps/nonloop,중심10,10,optional-fx,WeaponFX |
| 표시 | 후광→책,동일 위치/배율,기존 orbit/속도/책수/판정 유지 |

Core 작성자가 후속 보상/진화를 병행하므로 Core/DLL/provenance/sync/catalog/CI/hooks는 편집하거나 동기화하지 않았다. 이번 compile/앱은 현재 선택된 DLL `bece4324eb8d722205fecc0870f23320f99c07ccba0799fe6b7ec29beab5f54c` 기준이며 진행 중인 R03 소스 지원 판정이 아니다.

## 관련 검증과 제한

새 ignored 근거는 `evidence/bible-sidecar-unity-20261116/`다. 전체 suite·전프레임 픽셀/해시 조합·대규모 변이는 실행하지 않았다.

- 실제 Unity compile exit0. 관련 `AdamBibleArtSidecarTests`만 EditMode2/2 PASS,실패/skip0이다. 두 ID 선택·나머지 원본 선택,기존 재생/품질/pivot,raw PNG SHA와 실제 importer 설정을 확인했다. `compile/summary.json`, `sidecar-tests-command.json`, XML을 참조한다.
- 실제 macOS Development 앱의 대표 화면은 총3장이다: `initial-input-setup/capture/01-660-normal.png`, `capture/02-660-low-after-f6.png`, `capture/03-1100-normal-after-f6.png`. 첫 기본 화면을 재사용하고 후속 실행에서는 두 장만 촬영했다. 같은 제품 소스·선택 DLL이며 실제 게임의 `DrawGameView`를 사용했다.
- level6 성경3권·Core time0.1의 명시 staged fixture다. 660/1100 정수배율에서 책이 보이고,660 저효과에서도 책이 유지됐다. 실제 F6 처리 경로를 앱 내부 합성 키보드로 기본→낮음→기본 전환했다. 정지시킨 State/World의 직렬화가 토글 전후 정확히 같다. 장식만 optional-fx로 꺼지고 core 책은 남는 연결을 확인했으며, 자연 전투/OS native 입력·모바일·접근성·성능 합격으로 전용하지 않는다. 캡처의 책은 frame0 대표 시점이며 두 프레임 전체 렌더 검증은 추가하지 않았다.
- 최초 QA 입력 검사는 `game.enabled=false`가 EnhancedTouch를 끈 상태로 private Update를 호출해 실패했다. 기본 화면1장은 정상 저장되어 재사용했다. QA 입력 초기화만 보완한 재빌드에서 이전 QA 씬을 다시 열어 driver가 중복 추가되는 새 실패가 있었다. 두 번째 실패 실행은 화면을 추가하지 않았다. 원본 씬을 매 빌드 복사하고 중복 driver를 거절한 최종 빌드·캡처는 exit0이다. 제품 코드나 기대값을 완화하지 않았고 실패 앱·로그를 보존했다.
- 새 suffix/product/bundle/marker마다 Host 없는 첫 씬의 무저장 path probe와 realpath를 대조한 후 실행했다. 세 identity의 정확 Player/Editor child PID·exit,자기 app writer 부재·자기 persistent 파일만 원래 부재로 원복한 기록이 있다. 지정된 사용자 preview/old unknown 프로세스·윈도/저장과 iOS는 조회·조작하지 않았다.
- 보호565파일의 dispatch baseline byte를 보존했다. Unity가 자동 추가한 기존 Art03의 비override platform block/빈 icon 설정만 baseline byte와 대조해 원복했다. 기존 원본 GUID/PNG·Core DLL/provenance/catalog·패키지는 그대로다. 병행 Core 작성자의 수정은 되돌리지 않았다.

최종 `binding.json`에 제품 변경·선택 DLL/provenance·관련 XML·앱 주요 산출물·세 화면·명령/실제 exit/PID·보존/원복을 연결한다. 모델 요청은 상속 기본값(별도 override 없음), 실제 provider 모델은 자체 usage의 gpt-6.1-sol이다. own start/end를 기록했으며 청구액·절감률·effort는 미측정으로 둔다. 새 helper/유료 도구/패키지/커밋/push는 없다.

제품/Editor/Player 소유를 root에 반환한다. 세 화면의 같은 독립 UI 리뷰와 최종 채택·후속 커밋/업로드 판단은 root에 남는다. 공개 source checker 재생성은 기존 독립 아트 QA의 정상 검사를 재사용했으며 새로 반복하지 않았다.
