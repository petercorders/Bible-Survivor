# 턱뼈 피드백 독립 코드 리뷰

- 검토일: 2026-11-18 KST
- 판정: **PASS — 현재 턱뼈 표시·한정 audio/particle 변경 범위에서 막는 HIGH/MEDIUM 발견 없음.**
- 소유 변경: 이 보고서만. Core/DLL·원본 아트·제품·타인 파일·사용자 저장을 수정하지 않았다.

## 검토 기준과 판단

`evidence/jawbone-feedback-20261118/handoff.md`, `files-modified.json`의21개 소유 파일 목록, 관련 XML·A/B·audio 근거와 현재 Runtime diff, 신규 AdamJawboneSwing/Art/Importer/시험을 읽었다. **Fable 전체 디자인 통합 이전의 고정된 턱뼈 소스** 기준이다. 검토 중 제품 writer가 없는 반환 상태를 기준으로 하며, 외부 새 폴더에서 진행하는 Fable 디자인은 이 코드 판정에 포함하지 않는다. 아직 없는 새 HUD·타일·전체 디자인을 검토했다고 주장하지 않는다.

| 경계 | 판단 |
| --- | --- |
| 반 픽셀 grip·좌표 | held32의 bottom-left pivot(7.5,11.5)을 importer가 normalized custom pivot으로 보존하고 loader가 정확한 pixel pivot/rect/PPU32를 확인한다. GripRect는 sprite 중심 대신 grip을 손에 놓으며, 회전축은 `hand+arena.position`의 창 좌표다. BeginGroup 내부 GUI.matrix도 창 좌표를 변환하는 기존 회귀를 피한다. GUI matrix/color는 draw 후 복구한다. |
| 손 follow·방향 freeze·Core clock | Observe는 기존 LatestJawboneBurst/ActiveSince/PendingBursts와 snapshot.Time만 읽는다. 새 burst를 관측할 때 방향을 저장하고 같은 burst 중 회전 입력으로 덮어쓰지 않는다. 손은 매 draw의 현재 feet와 좌우 손 offset(±7,-12)을 따라가므로 이동을 반영한다. .2초 표시 수명·준비/타격/회수는 Core time을 사용하고 새 gameplay timer·RNG·판정·저장 필드를 만들지 않는다. Clear는 시작/방향을 초기화하고 World 교체/null/Dispose 경로에 연결된다. pause에서는 snapshot time이 고정된다. |
| 왼쪽 배우40px 교정·여섯 레이어 | 반환 근거의 실제 화면 발견은 기존 left 반전이 arena의 창 offset을 빼고 회전축을 잡던 문제다. 현재는 `FacingMatrix(feet+arena.position, left)`를 한 부모 행렬로 적용한 뒤 모든 player layer를 같은 feet에 그린다. 오른손 offset도 같은 행렬로 왼손에 대응하고 feet-y가 유지된다. 코드·관련 두 크기 시험으로 정합을 확인했으며 40px 화면 관측 자체는 작성자 근거를 재사용한다. 새 최종 시각 판정을 대신하지 않는다. |
| lowFX·가짜 명중 없음 | held club은 lowFX에서도 표시하고 짧은120도/range 호만 선택 feedback에 종속된다. 호는 hit를 판정하지 않는다. 소리/접촉 조각은 기존 실제 FrameImpacts만 소비하며 swing 관측이나 빗나감이 새 impact를 만들지 않는다. cursor/generation으로 같은 사건을 중복 재생하지 않는다. mute/Volume0/비활성 상태는 음성을 정지하고 늦은 batch를 소비하며, 입자는 lowFX에서 clear·pause에서는 기존 입자를 동결한다. |
| 한정 audio·particle·pool | 턱뼈 PCM만 .14→.16초 body/tap과 제한된 gain으로 조정한다. Bible/sling의 길이·난수열·샘플 계산과 볼륨 경로는 유지한다. 턱뼈 입자 count/color/speed/size/lifetime만 바꾸고 다른 무기 값은 보존한다. 기존 audio8voice·35ms throttle, 입자16 emitter×16/256cap·kill priority를 유지한다. AudioClip/voice/listener, particle RT/material/root 및 camera mask/event 해제는 기존 Dispose를 재사용한다. 신규 swing은 managed 상태만 가지며 sidecar Sprite/Texture는 Resources 소유라 임의 파괴하지 않는다. |
| sidecar 범위 | strict manifest/PNG 계약과 Texture2D·FullRect·Point/Clamp·무 mipmap·stable spriteID/name-fileID를 importer에서 검증한다. AdamArt에는 club 참조를 추가하고 jawbone icon mapping만 덮어쓴다. 기존 held16 등록과 원본 Art03는 유지한다. 기존 다른 무기 피드백 좌표 수식은 변경하지 않았다. |

## 재사용한 검사와 A/B 근거

`group-before.xml`은 실제 원 GUI group 회귀의660/1100 두 케이스가 모두 **166.469223px 오차로 FAIL**한 기록이다. 복원된 `related-verified.xml`은 **17/17 PASS**, 실제 배우 left 교정 후 `jawbone-final.xml`은 **8/8 PASS**다. 최종8에는 기존 턱뼈6케이스와 신규 left reflection2케이스가 들어 있으므로 17+8을25개 고유 시험으로 더하지 않는다. 그립/방향·snapshot 무변경, 실제 hit의 audio/particle·중복 방지·lowFX/mute, 기존 depth/hitflash/PCM 계약을 관련 범위에서 확인한다. EditMode의 기존 runtime Destroy 오류는 예상 LogAssert로 처리된 제약이며 Editor 수명 처리 전반을 새로 개선한 결과는 아니다.

`world-comparison.json`은660/1100 모두 A/B World/Profile 동일·HP100을 기록한다. QA fixture는 매 draw의 World/저장 bytes 무변경과 pause 고정을 확인하고 실제 턱뼈 hit를 센다. 두 크기 모두 실제 hit1 뒤 후속 빗나감이 있는 기술 클립이다. 실제 Core 상태와 draw를 분리한 근거를 재사용했으며 게임 판정 개선이나 자연 밸런스 결과로 확대하지 않는다.

`audio-facts.json`의6 WAV는 비무음·peak<1이다. before/after PCM은22050Hz mono .14/.16초, engine capture는48000Hz stereo로 구분한다. 기존 QA의 Bible/sling sample 동일 assertion과 변경되지 않은 계산 분기를 확인했다. **이번 리뷰에서 WAV를 실제 청취하지 않았으므로 소리 취향·타격감 선호·지각 latency·정밀 영상 동기화 PASS를 주장하지 않는다.** MP4는 무음이며 WAV 존재를 영상에 동기 녹음됐다는 뜻으로 세지 않는다.

최종 build exit0과 자기 final child3개 exit0는 기존 인계/명령 근거를 재사용했다. QA 클래스 rename이 shader resource 문자열까지 바꿔 난 최초 exit2는 QA-only literal 복구로 해소됐고 제품 shader 경로 변경이 아니다. 초기 실패·자동 import 복구·자기 저장 absent 복원 근거를 보존한 인계를 읽었으며 리뷰가 앱이나 저장을 다시 실행·조회하지 않았다.

## 남은 범위와 사용량

최종 움직임 영상/실제 grip·실루엣 가독성과 청취는 별도 시청각 리뷰가 남는다. 고정 tick 60frame/10fps/6초 A/B는 realtime 성능 검증이 아니다. 전체 suite·픽셀/해시 행렬·CLI·Editor·앱·GUI를 재실행하지 않았다. 사용자 live PID98774/window244879/bundle과 그 저장, 다른 보호 identity는 조회·조작하지 않았다. 후속 Fable 전체 디자인 통합 후에는 필요한 변경 차이만 새로 검토해야 한다.

기존 도구의 자기 `jawbone-feedback-code-review` start/end로 사용량을 기록한다. 지연된 이전 tail을 포함할 수 있는 자기 세션 구간이며 실제 청구액·독립 작업 비용·절감률은 계산하지 않는다.
