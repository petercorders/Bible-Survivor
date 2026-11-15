# R02 아트·P2·schema2 최종 독립 코드 리뷰

2026-11-15. **검토 범위 PASS, 신규 CRITICAL/HIGH/MEDIUM 없음.** 최신 macOS Mono QA 앱과 현재 제품 입력의 결속을 확인했다. 구현자 실행 근거를 독립적으로 읽고 해시·XML·변이 구조를 대조한 결론이며, reviewer가 Editor/Player를 재실행하거나 자연 플레이를 수행한 결과는 아니다.

## 범위와 독립성

`AGENTS.md`·전용 Unity 검증/모델 계약, `docs/progress/unity-r02-art.md`, `evidence/unity-footpivot-schema2-20261115/handoff.md`·`final-binding.json`을 읽었다. 기존 `unity-r02-art-code.md`의 R02 입력/임포터/로더/깊이 정렬과 `r03-schema-code.md`의 R03S-01/02 CLOSED를 재사용했다. Core115·기존18변이·추가10변이·역사 binary30쌍 및 독립25개를 이번 reviewer 실행 수로 합산하지 않았다.

이번 추가 검토는 `AdamArtWorld`의 grouped GUI 발 기준 변환, P2 파생 마스크와 수명, 최신 Core DLL/provenance, 실제 Player schema 검사와 최종 결속, 사용량 도구 및 CI/hook 연결이다. 제품/시험/Editor/GUI/앱/기기/실제 저장/외부 아트를 수정하거나 실행하지 않았다. 산출물은 이 리뷰 문서이며, 별도 임시 폴더의 사용량 fixture와 승인된 자기 세션 사용량 ledger만 기록했다. graphify 그래프가 없음을 확인했으며, 허용된 읽기 전용 범위에 맞춰 새 그래프 생성 대신 배정된 파일·근거만 읽었다. 추가 agent·commit·push는 수행하지 않았다.

## 표시 변경

- `AdamArtWorld.cs:220`의 `ReactionMatrix(anchor+arena.position,1)`는 arena local 발 위치를 window 좌표 변환의 기준점으로 옮긴다. 실제 본체와 `DrawHit`가 같은 matrix·anchor·frame·flip을 공유하며, 다음 적을 그리기 전에 이전 matrix/color를 복원한다. Core 전투/저장 상태를 쓰지 않는다.
- `AdamR02Art.cs:84`는 검증된 원본 alpha에서 흰 파생 텍스처 세 개를 만들고 동일 사전을 재사용한다. 원본 Resources texture를 수정하거나 해제하지 않는다. 프레임 UV와 단일 좌우 반전은 본체와 일치한다. 강조는 명중 대상의 기존 반응 중 처음 .04초에만 .65 opacity이며 pause/low/reload 경계가 시험에 포함돼 있다.
- `AdamR02Art.cs:110`의 중복 Dispose는 안전하며, 파생 텍스처만 Play 중 지연 Destroy/Edit 즉시 Destroy한다. `AdamArtWorld.Dispose`와 `AdamGame.OnDestroy`가 연결된다. 일반 Clear/reload에서는 파생 텍스처를 재사용한다. Edit의 원본/alpha/재사용 검사와 Play의 10회 native 수명 검사가 최종90/31 XML에 포함됐다. 첫 로딩 할당과 전체 프레임 성능은 이 결론에 포함하지 않는다.

정확한 원래 변이는 현재 소스의 해당 호출 한 번만 `ReactionMatrix(anchor,1)`로 바꾼 bytes의 SHA를 계산해 old-offset build-input SHA와 일치함을 직접 확인했다. 두 앱의 QA driver SHA·DLL SHA는 같다. build 입력 차이는 그 소스·고유 identity를 위한 ProjectSettings·path-gate 씬뿐이다. 원래 구조의 build는 exit0이고 실제 픽셀 단언은 첫342개 중0개 일치·max136으로 exit2다. 복원 앱 build와 path/schema/capture/reload 자식은 모두 exit0이다. 최종96개(2해상도×3종×4frame×2방향×본체/마스크) 기록은37,014/37,014 일치·최대 RGB 차이1이다.

픽셀 QA driver는 실제 제품 Draw를 호출하고, window 발 위치를 고정한 별도 GPU 기준 및 명시 좌표식으로 비교한다. 이 제어 fixture는 자연 군집 가독성이나 입력 검증이 아니다. final/old-offset 두 capture 로그에는 driver가 host를 배정하기 전 OnGUI에서 발생한 NullReferenceException이 각각2회 남아 있다. 이후 같은 driver의 최종96개 성공과 정확한 원래 구조의 픽셀 실패는 확인했지만, 이 로그를 예외 없는 일반 플레이 성공으로 표현하지 않는다. 초기 저장 예외가 화면 콘솔을 가렸던 실패도 이력으로 보존돼 있고 최종 schema/visual은 별도 프로세스다.

## 최신 Core·Player 저장 연결

현재 Core 관련 이전 독립 리뷰 입력24개에서 SHA drift0, provenance의 evaluation/build 각각 repo 입력12개에서 drift0(기록된 부재/null 포함)이다. DLL SHA는 `bece4324eb8d722205fecc0870f23320f99c07ccba0799fe6b7ec29beab5f54c`, provenance SHA는 `ff84e4d07f45aebb9399f729a7a5b584d4c24214d5a78d481dc3dcaa460cbb9e`이며 최종 앱 embedded DLL과 같다. QA 빌드의 runtime/DLL/provenance 입력도 현재와 일치한다.

해시가 결속된 실제 QA driver와 schema/reload 로그·성공 marker·명령/PID/exit를 읽었다. 이전 실제 binary의 schema1 R02 저장을 Host로 Load했을 때 primary/backup 무쓰기·archive 부재, 첫 Checkpoint의 두 원문 archive, 정상 legacy backup이 있어도 envelope3/World2/Profile·World 미지원 필드의 Host Load/Checkpoint/RetrySave/Reload와 직접 Commit 보존 거절을 단언한다. archive 경로를 자체 파일로 막은 실패 후 RetrySave와 같은 operation 재전송도 확인한다. 재시도 payload/operation 동결의 제품 구현은 기존 독립 Core 리뷰 결론을 재사용했다.

재프로세스 Host Load의 State/World 직렬화 동등은 Load 직후 검사다. 기존 `OnApplicationQuit` checkpoint가 새 revision/operation을 쓰므로 종료 후 primary byte 동일로 확대하지 않는다. schema 프로세스 종료의 stale-save 진단은 최초 Host 외부에서 같은 QA primary를 갱신한 fixture 경계이며, 최종 reload assertion의 성공과 구분한다. 사용자 저장 접근이나 모바일/IL2CPP 지원을 확인한 근거는 없다.

## 직접 확인한 결속과 도구

`final-binding.json`의 artifact32개, source387개, 보호448개의 현재 SHA가 전부 일치했다. 최종 앱334개도 각 SHA와 정확한 파일 집합(extra/missing0)이 일치한다. source manifest SHA는 `f72a25add0be9c48b6d2f800441669a65e5866e1446ded24670baf7f54a55079`, app manifest SHA는 `7c9306aaa5f22b518bf9883cb3ce6d98dbaa3dea4fe6be906ca34882dbc22a10`이다. 최종 XML을 직접 파싱해 Edit90/90·Play31/31, failed/inconclusive/skipped0 및 XML SHA 일치를 확인했다. 엔진 wrapper와 실제 명령 exit도0이다. 문서 후속 편집은 그 편집의 별도 diff로 관리해야 하며 이 실행 당시 제품 결속으로 덮어 쓰지 않는다.

`record_model_usage.py`는 선택된 세션 파일에서 token telemetry만 추출하고 원문 대화·파일 경로를 ledger로 복사하지 않는다. 이전 같은 세션 checkpoint와의 **인접 누적 차이**이며 phase 전체/프로젝트 총량이 아니다. 임시 JSONL/ledger로 정상 delta, 추론을 더하지 않은 provider total, 누락 counter의 null, counter 감소 시 delta 거절, telemetry 부재/음수 시 exit1·ledger 무쓰기, 미완성 JSON 끝줄, 개인 원문 sentinel 제외를 직접 확인했다(exit0). reasoning/cache는 부분집합일 수 있으므로 합산하지 않는다. 실제 청구액과 API 환산액은 null이며 단가·요청 귀속/전체 worker 근거 없이 비용·절감률을 계산하지 않는다. 자기 세션 start/end는 `evidence/model-usage/checkpoints.jsonl`에 기록했다. end 이후 문서 작성·최종 응답 구간은 해당 간격에 포함되지 않는다.

CI core의 full history checkout·두 schema 검사, art의 read-only R02 gate, hook의 같은 gate 및 실패 전파가 연결돼 있다. 설치 hook과 versioned hook SHA도 같다. 새 commit의 원격 CI 성공은 이 리뷰에서 NOT_RUN이다.

독립 이미지/시각 리뷰·GC·선별 commit/push·그 SHA의 CI는 별도 게이트다. native active 입력, 자연 수동 전투/밸런스, 모바일·IL2CPP·실기기, 전체 성능, 미지원 R03 콘텐츠는 NOT_RUN/미확인으로 유지한다.
