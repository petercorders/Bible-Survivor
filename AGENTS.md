# Bible Survivor 개발 지도

- 2026-11-14 사용자 지속 승인: “결제만 아니면 나한테 물어보지 말고 쭉쭉 진행해줘.” 이 게임 개발에 필요한 계획·프로젝트 연결·구현·검증·독립 리뷰·수정·GC·커밋·GitHub 업로드는 재확인 없이 진행한다. 비용 발생 시에만 사용자에게 확인한다. 무관한 개인 파일 삭제나 자격 증명 공개는 개발 범위에 포함하지 않는다.
- 디자인 협업: 개발 중 필요한 UI·아이콘·애니메이션·배경·에셋 변경은 지정된 Claude Design 담당에게 요청하고 개발 담당과 상호 피드백한다. 요청에는 stable ID·용도·크기/프레임/피벗·상태·완료 조건을 붙인다. Design 제품 사용과 단순 코드/격자 생성·캔버스 게시를 구분하며 연결되지 않았으면 대체 실행을 Design 실행으로 보고하지 않는다. 독립 QA 전 원본을 최종 에셋으로 임포트하지 않는다.

기획: `docs/game-design.md`, `docs/genesis-balance.md`, `docs/mobile-platform-plan.md`.
개발 단계: `docs/genesis-development-plan.md`, `docs/harness/unity-adam-plan.md`. R01/R02 순수 코어와 Unity 아담 시제품을 구현했다. 검증 상태는 `docs/progress/`에서 확인한다. 아트03은 오프라인 인계 QA를 통과했고 실제 게임 적용은 `docs/harness/unity-art-integration-plan.md`로 이어간다.

- 검사: `python3 tools/check_all.py`. 실제 .NET SDK 경로가 다르면 `BIBLE_SURVIVOR_DOTNET`을 지정한다.
- 추가 게이트: `python3 tools/unity_check_project.py`(로컬 실제 동기화 입력), `python3 tools/unity_check_portable.py`(fresh checkout용 원본·산출물 일관성), `python3 tools/check_art.py`(오프라인 계약·음성 입력·검사기 회귀). 이 명령들은 Unity Editor/모바일 실행 시험을 대신하지 않는다.
- `src/BibleSurvivor.Core`는 netstandard2.1/C#9, Unity/외부 패키지에 의존하지 않는다. 콘텐츠 원본은 `content/genesis/catalog.json`이다.
- 코드·수치·저장 변경에는 실제 실패/복구 시험과 독립 코드 리뷰를 적용한다. 원래 회귀의 변이 검증은 별도 사본에서 실행한다.
- 라운드별 개발 사이클 끝에 `docs/retrospectives/Rxx.md`로 GC 회고를 남긴다. 학습 상태 자동 반영은 하지 않는다.
- 사용자가 라운드별 검증·리뷰·GC 완료 후 GitHub 업로드를 승인했다. 미실행 Unity/모바일 검증을 통과로 세지 않고 선행 코어 단계임을 커밋/진행 문서에 명시한다. merge/배포 승인은 별도다.
- Unity 프로젝트 생성·아담 연결은 2026-11-14 사용자가 `docs/harness/unity-adam-plan.md`대로 진행하도록 승인했다. 루트는 `UnityGame/`이며 계정·라이선스 변경은 포함하지 않는다. 실제 Editor/기기 확인 없이 플레이·성능 완료를 주장하지 않는다.
- 사용자가 Unity 없이 가능한 코드 개발을 계속하라고 명시했다. 그래픽 생산/QA와 순수 C# 이동·전투·저장·자동 검증은 독립 작업이다. Editor/아트 부재는 해당 연결·실기기 게이트에만 적용하고 승인된 독립 코드 작업을 멈추는 이유로 사용하지 않는다.
- 기존 사용자 변경을 보존한다. 씬·프리팹은 파일별 단일 작성자로 관리하며 동일 프로젝트에서 Editor/빌드/Player GUI 소유를 인계한 뒤 실행한다.
