# Unity 기본 파티클 제품 연결 — 후속 실행 계획

사용자의 타격감 개선과 Unity 제공 기능 활용·지속 개발 승인에 따른 후속 작업이다. P0 명중/오디오 커밋·독립 PC 리뷰/GC 경계가 끝난 뒤 root가 단일 구현자에게 실행을 인계한다. 지금은 계획이며 제품 파일을 병렬 수정하지 않는다. 근거는 `../progress/combat-feedback.md`의 별도 Particle RT 실험과 `unity-effects-assessment.md`다.

## 목표·변경 경계

현재 QA 앱에서 검증한 Unity 기본 Particle System을 실제 Adam 게임 전투 영역에 연결한다. 적의 충돌 판정·좌표·피해/공격 간격·Core 사건·난수·저장 schema를 바꾸지 않는다. P0 기존 스파크/피격/오디오 위에 optional 작은 파편·금빛 burst를 추가하되 기본/저효과·끔 동작을 명시한다. 공식 팩 원본은 미임포트/ignored로 보존하고, 이번 단계는 기존 Unity 모듈과 원본 제작의 작은 프리셋만 사용한다. 성경 인물/종교 상징을 무관한 공격 연출로 바꾸지 않는다.

## 구현 수용 조건

- 실제 `AdamArtWorld.Draw`의 arena/Point 투영/PixelScale과 같은 계산을 공유한다. QA 전용 arena를 복사해 제품 좌표로 사용하지 않는다. Camera.aspect·RT 크기/정수배율/Point/AA1·GUI 합성 rect를 동일 viewport에서 계산하고 resize/홀수 크기/inset에서 일치시킨다.
- optional Runtime 파티클 풀·카메라·RT는 제품 소유 단일 작성자가 추가한다. 메인 카메라에 중복 렌더되지 않게 layer/culling과 수명 관리를 명시한다. UI/HUD/모달의 위치·입력 rect는 그대로이며 플레이어·적·무기·파편의 합성 순서와 가림을 실제 화면에서 확인한다.
- 기존 명중 cursor를 한 번 소비한 결과에서만 방출하며 두 번째 소비자를 넣어 오디오/기존FX를 다시 재생하지 않는다. 실제 명중/치명/관통을 무기별로 구분하고 풀 상한256과 우선순위, reload/새 World/terminal/pause/background clear/동결을 정한다. bounds0으로 입자가 cull되는 기존 실험 회귀를 실제 제품 경로에서도 방지한다.
- 적절한 양의 작은 스파크·돌가루/먼지·성경 빛 조각 세 프리셋으로 시작한다. 저효과는 장식 파티클을 생략해도 기존 P0 플레이어 피격 단서를 유지한다. 후처리/새 패키지/VFX Graph/Cinemachine/실제 hit-stop/상태 이상/물리 넉백은 범위 밖이다.
- RT/Material/GameObject/ParticleSystem의 생성은 초기화·크기 변경 때로 제한하고 종료/Disable에서 소유 자원을 해제한다. 재시작/창 크기 변경으로 자원이 누적되지 않아야 한다. 실제 측정 전 성능 개선을 주장하지 않는다.

## 검사·인계

같은 seed/입력의 Core World/Run/Profile 동등, 제품 세 무기·1100/660/홀수 해상도·실제 resize·합성 inset에서 가장자리/화면 밖·HUD/모달·성장·사망/재시작·일시정지·저효과를 확인한다. 실제 게임 경로의 renderer bounds/투영/중복소비 원래구조 변이→행동 실패→복원 PASS를 별도 QA 사본에서 보존한다. 기존 Core89·회귀18 및 관련 Unity47Edit/16Play를 후퇴시키지 말고 필요한 의미 있는 시험을 추가한다.

실제 적40/120 조건에서 파티클 켬/끔 동일 장면의 CPU frame time/관리 할당·RT/입자 수를 측정하고 장치/시간/시드/측정법을 남긴다. 예산은 사전 문서화하되 Editor/PC 측정을 모바일 실물 성능 PASS로 옮기지 않는다. 실패한 예산은 파티클 수/수명/품질 선택으로 줄이며 전투 규칙은 그대로 둔다.

소유는 Unity 표현/관련 시험·새 제품 파티클 코드와 검증 fixture·진행/회고 문서다. Core·원본03/font/GUID·CI/checker/hook·전역 규칙은 수정하지 않는다. 사용자가 플레이 중인 iOS/기기/저장·기존 PC 저장은 조회/조작하지 않는다. 별도 marker/앱ID/무저장 path probe와 새 QA 출력으로만 실제 앱을 실행한다.

독립 PC 코드/시각·조작 리뷰→수정/재리뷰→프로젝트 GC→commit/push/해당 SHA CI를 root가 맡는다. 구현자는 `docs/progress/combat-particles.md`, `docs/retrospectives/R01-combat-particles.md`와 ignored 새 evidence에 실제 명령/exit/XML/전후영상·새 소스/앱SHA·저장/자산 보존·미실행을 남기고 소유를 반환한다. 이 계획은 모바일 전체 검증이나 공식 팩 제품 임포트 완료를 뜻하지 않는다.
