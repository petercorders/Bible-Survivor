# 턱뼈 몽둥이 표시·명중 피드백

승인: `docs/harness/jawbone-feedback-01.md`. Core·DLL·판정·피해·범위·저장 형식은 수정하지 않았다. 원본 Art03/Bible/Rxx 아트도 변경하지 않았다.

## 구현

- `AdamArtWorld`의 플레이어 왼쪽 parent reflection도 GUI 그룹 창 좌표의 발 pivot을 사용한다. 실제 좌측 이동에서 발견한 기존 40px 배우 이동 오차를 없애 그립과 같은 좌표에 둔다. 발y/레이어/아트/다른 무기 계산은 바꾸지 않았다.
- `AdamJawboneSwing`: 기존 `LatestJawboneBurst`와 snapshot.Time을 소비하는 일시적 표시 상태다. burst 관측 시 방향을 고정하고 손 위치는 이동하는 플레이어를 따른다. 발 기준 손은 좌/우 7px, 위 12px이며 GUI 그룹의 arena.position을 포함한 **창 좌표**로 그립 회전한다. 0.2초 안에서 준비 0.03초→빠른 타격→회수, Core pause에서 그대로 멈춘다. 새 저장 clock은 없다.
- 그립은 held32의 bottom-left pixel-center `(7.5,11.5)`이며 반 픽셀을 유지한다. Sprite 중심/기존 edge attach를 기준으로 놓지 않는다. 120도/실제 Core range의 짧은 보조 호는 선택 효과이며 타격을 판정하지 않는다. 무기는 lowFX에서도 남는다.
- `AdamJawboneArt` / `AdamJawboneArtImporter`: 승인 v2의 PNG 두 장과 manifest 한 장만 `Resources/JawboneFeedback01`에 복사했다. explicit Texture2D·Point·Clamp·PPU32·FullRect·무압축·단일 sprite를 검증한다. AdamArt는 별도 held Sprite와 jawbone 아이콘만 연결한다. Art03 index와 원본 held16은 보존한다.
- `AdamImpactAudio`: 실제 FrameImpacts를 재생하는 기존 cursor/voice cap/mute 경계를 유지하고 jawbone에만 160ms 저음 body+짧은 tap PCM과 볼륨을 조정했다. Bible/sling PCM은 원래 계산 그대로다.
- `AdamImpactParticles`: 실제 jawbone 명중의 짧은 뼈색 접촉 조각만 조정했다. 빗나감에는 명중음/조각이 없다. lowFX·pause·pool·Dispose 경계는 유지한다. 새 흰색 flash/화면흔들림/hitstop은 추가하지 않았다.

## 아트·디자인 근거

외부 `art-jawbone-feedback-01` v2는 Codex script pixels다. 독립 QA `art-readiness-reviews/jawbone-feedback-01-review.md` 및 root의 v2 PASS 후에만 정확 3개 public 파일을 복사했다. manifest 안의 제작 당시 PENDING 문자열은 원본 bytes 보존 때문에 그대로이며, 독립 QA 판정과 구별한다.

- manifest SHA256: `87333c69aa50fcb13aa1bddfdf0fd679be8ad70d10c29566015fed6a5b329ce0`
- held32: `51f4364ebe5c9b25fc6c8aba192acfa55fb4554958c01b5318851494e1304a6e`
- icon16: `847297b86a862e4279dd77434a2ef806342fc87cb54ffd3196d926a454720245`

root가 별도로 받은 `art-jawbone-direction-01/RESULT.md`는 실제 claude-fable-5-2/Claude Design 자문이며 제작 PNG와 다르다. 손잡이 비율·grip 회전·burst 방향 고정 제안을 대조했고, 캔버스 렌더/Unity 검증을 자문 자체의 PASS로 세지 않았다. 이 Unity 작성자는 Claude/Design을 호출하지 않았다.

## 관련 검증

근거: ignored `evidence/jawbone-feedback-20261118/`.

- `group-before.xml`: 원래 FacingMatrix 그대로인 시험 2건 실제 FAIL. 660×440,1100×720에서 GUI 그룹 내 회전 기준점 오차 각각 166.469px. `related-verified.xml`: 같은 검증 복원 PASS 및 신규 포함 총17/17 PASS (`AdamJawboneTests`, `AdamImpactTests`, `AdamHitFlashTests`, `AdamEnemyDepthTests`). 순수 수식 변이의 실패이며 실제 화면 관측과 별개다. 이후 실제 화면에서 발견한 왼쪽 actor parent reflection까지 교정하고 `jawbone-final.xml`의 턱뼈 관련 8/8을 다시 통과했다. 기존 무관 시험은 반복하지 않았다.
- 신규 시험: 그립/4방향/고정 방향/이동/clock pause/sidecar import·icon mapping/실제 명중의 audio·particles·중복 방지/lowFX·mute/World·저장 bytes 무변경.
- 중간 시험은 기존 runtime Dispose가 EditMode에서 내는 destroy 로그 때문에 1건 FAIL했다. 해당 기존 소유 객체의 정확 예상 로그만 시험에 등록한 뒤 통과했다. 다른 오류를 무시하지 않는다.
- Editor 첫 원래 회귀 시험이 만든 Art03 자동 플랫폼 meta/ProjectSettings 차이는 당시 clean baseline에 근거하여 해당 자동 변경만 복원했다(`automatic-import-restoration.json`). 이후 모든 Editor/빌드/Player는 새 QA 사본/identity만 사용한다.
- A/B fixture의 클래스 이름 변경이 shader resource 문자열까지 바꾼 첫 실행은 exit2였다. QA 전용 문자열을 원래 resource명으로 복구했다. 제품 shader와 particles resource 경로는 처음부터 바꾸지 않았다. 실패 원본 로그/실행 원장을 보존한다.

실제 앱/영상·음향 최종 관측은 아래와 별도 인계 근거를 따른다. 흑백 보드·실기기·자연 밸런스·장시간 성능은 NOT_RUN이며 전체 과거 suite/행렬은 실행하지 않았다. 실제 청구액/비용·절감률은 미계산이다. 사용자 live 앱/저장과 보호 preview/unknown QA identity는 조회·조작하지 않았다.


## 최종 대표 실행과 소유 반환

최종 `build-final.log` exit0. 격리 앱 `build/JawboneQa.app`은 기본 실행 시 일반 Adam 화면이며 캡처 플래그가 있을 때만 QA scene으로 간다. 일반 Adam bootstrap/실제 jawbone Start 화면 `capture/normal-app.png`, `normal-pass.txt`, 정상 child exit0을 남겼다. 사용자 최신 앱은 바꾸거나 실행하지 않았다.

`video/{before660,after660,before1100,after1100}.mp4`는 각각60 PNG/10fps의6초 무음 A/B다. 같은 Core fixture에서 우→상→좌→하 이동하며 공격 중에 방향을 바꾸는 구간(예: frame013/023/033)을 넣었다. 대표 실제 열람: after1100/frame023의 좌측 이동 배우/손/위쪽으로 고정된 회수, after660/frame032의 왼쪽 타격, before1100/frame022의 몸에서 떨어진 옛 weapon과 after frame022의 손 연결. 실제 v2 별도 sprite를 표시하며 좌측 배우도 같은 발 pivot에 남는다. Core range의 선택 호와 actor 위치를 같이 볼 수 있다. 영화는 고정 tick 캡처를 인코딩했으므로 실시간 프레임율/90ms 체감 평가를 인증하지 않는다.

각 A/B 실행은 실제 jawbone 명중1회를 포함하며 빗나가는 후속 swing도 담았다. `world-comparison.json`: 두 크기 모두 최종 World 전체와 Profile이 A/B에서 같다(HP100 유지). 매 OnGUI에서 World/저장 bytes 무변경을 확인했고 pause 후 World/자기 저장도 동결됐다. lowFX/mute는 관련 시험과 기존 gate 유지로 확인했으며 별도의 모든 효과 조합 화면을 늘리지 않았다.

`capture/{before,after}{660,1100}-engine.wav`는 실제 Unity 믹서의 stereo48kHz 캡처다. `jawbone-{before,after}-pcm.wav`는 생성 원음 비교이며160ms 뒤 소리로 바뀐다. `audio-facts.json`은6개 WAV 모두 비무음/peak<1을 확인한다. Bible/sling PCM은 QA에서 이전 계산과 sample별 동일함을 확인했다. 청각적 선호·지각 지연·영상과 엔진 audio의 정밀 동기화는 이 작성자의 검증으로 주장하지 않으며 독립 시청각 리뷰에 전달한다. 무음 MP4와 WAV를 구별한다.

`path-probe.json`: noHost probe가 새 bundle/product/persistentDataPath를 확인한 후 종료했고 세 SaveStore 파일이 없었다. `player-commands.json`의 최종 probe/capture/normal child는 모두 exit0이다. `restoration.json`은 자기 QA의 세 SaveStore 파일만 다시 없는 상태로 복원했음을 기록한다. 캡처 내부 fixture 저장은 ignored 근거로 남긴다. 보호된 사용자/기존 QA identity는 조회하지 않았다. 표준 Unity 종료 ComputeBuffer 경고 등 원문 로그를 보존하며 장시간 leak/performance 완료를 주장하지 않는다.

자기 usage는 phase jawbone-feedback start/end, 실제 telemetry model `gpt-6.1-sol`이다. 관측 구간 total delta11,008,852(input10,971,093/cached input10,725,760/output37,759/reasoning output12,413; cached/reasoning은 별도 합산 항목이 아니다). root/외부 작업자 사용량은 이 구간에 합산하지 않으며 비용/절감률은 미계산이다. 제품과 Editor/Player 단독 소유를 root에 반환하며 독립 코드/시청각 검토가 남는다. 커밋/업로드/CI는 이 작성자의 범위 밖이다.
