# Bible Survivor 개발 지도

기획: `docs/game-design.md`, `docs/genesis-balance.md`, `docs/mobile-platform-plan.md`.
승인된 단계: `docs/genesis-development-plan.md`. 현재 범위는 Unity 설치 전 R01 순수 C# 코어다.

- 검사: `python3 tools/check_all.py`. 실제 .NET SDK 경로가 다르면 `BIBLE_SURVIVOR_DOTNET`을 지정한다.
- `src/BibleSurvivor.Core`는 netstandard2.1/C#9, Unity/외부 패키지에 의존하지 않는다. 콘텐츠 원본은 `content/genesis/catalog.json`이다.
- 코드·수치·저장 변경에는 실제 실패/복구 시험과 독립 코드 리뷰를 적용한다. 원래 회귀의 변이 검증은 별도 사본에서 실행한다.
- 라운드별 개발 사이클 끝에 `docs/retrospectives/Rxx.md`로 GC 회고를 남긴다. 학습 상태 자동 반영은 하지 않는다.
- 사용자가 라운드별 검증·리뷰·GC 완료 후 GitHub 업로드를 승인했다. 미실행 Unity/모바일 검증을 통과로 세지 않고 선행 코어 단계임을 커밋/진행 문서에 명시한다. merge/배포 승인은 별도다.
- Unity 프로젝트 생성·계정·라이선스 설정은 아직 범위 밖이다. 실제 Editor/기기 확인 없이 플레이·성능 완료를 주장하지 않는다.
- 사용자가 Unity 없이 가능한 코드 개발을 계속하라고 명시했다. 그래픽 생산/QA와 순수 C# 이동·전투·저장·자동 검증은 독립 작업이다. Editor/아트 부재는 해당 연결·실기기 게이트에만 적용하고 승인된 독립 코드 작업을 멈추는 이유로 사용하지 않는다.
- 기존 사용자 변경을 보존한다. 씬·프리팹은 향후 파일별 단일 작성자로 관리한다.
