# Unity 아담 실제 아트 연결 작업

이 작업은 `unity-art-integration-plan.md`와 사용자 결제 외 지속 진행·Claude Design 협업 승인에 따른 후속 단계다. 앞선 Unity 아담 UI-02 독립 리뷰와 완료 커밋 후 coordinator가 실행을 요청한다. 실행 요청 전에는 씬/Player 소유권을 가져가지 않는다.

## 소유와 원본

- Unity 담당이 `UnityGame/Assets/Adam`의 표현/임포터/관련 시험·기존 Adam 씬을 단독 소유한다. 문서는 `docs/progress/unity-art.md`, `docs/retrospectives/R01-unity-art.md`, 필요한 Unity 전용 새 도구만 소유한다.
- 검수 원본은 `art/art-concept-03/export`와 명시 계약이다. manifest SHA `0c7f6ff0b86d83217beedf62f298294cd6c01c036ab589e2abed383c1e429751`, 반복02 flip 객체/폰트 라이선스 객체 형식이다. 이 폴더를 직접 고치지 않는다. 원본 이미지를 다른 생성 방식으로 대체하지 않는다.
- Core/R02/저장 규칙/밸런스·기존 CI/hook/checker는 수정하지 않는다. 필요한 API가 없으면 coordinator에게 사실과 최소 연결 변경을 보고한다. 파일/ID 확인 없이 전투 공격이나 방향/애니메이션 상태를 꾸며내지 않는다.
- Claude Design 담당은 별도 `art-unity-font-01` sidecar를 준비 중이다. Bitmap TTF를 동적 Unity 폰트로 사용하지 않는다. 폰트 패치 QA 전에는 현재 OS 글꼴을 명시적으로 임시 사용하고 월드 아트 연결을 독립 진행한다. 새 패치는 coordinator가 검토 인계한다.

## 구현

검수된 플레이어6레이어·적3종·무기3종·XP·효과·배경/16마스크 흙길을 원본 manifest와 대응시켜 실제 화면에 표시한다. PPU32·Point필터·정수 frame rect/pivot·layer order·정확한 fps/loop·flipX/alpha/quality 계약을 적용하고 잘못된 계약을 명시 거절한다. 생성 파일은 `.meta`와 함께 관리하고 재임포트가 GUID를 바꾸지 않게 한다. 재빌드/원본 SHA 대응을 검사한다.

기존 BattleWorld가 단일 전투 원본이다. 표현 방식은 기존 화면 구조와 일관된 최소 변경으로 선택하고 선택 이유를 기록한다. 물리/표현 애니메이션으로 별도 명중/피해를 적용하지 않는다. 모달/정지/복귀·저장 재개 때 시간/방향/모션 상태를 일관되게 복원한다. 사용하지 않는 객체/텍스처/이벤트를 정리하고 표현 생성 상한·재사용/반납 시 초기화를 확인한다. 풀링만으로 성능 향상을 주장하지 않는다.

UI 조각은 9-slice/상태/한국어 선택 의미 정보를 유지한다. 기존 UI-01(효과/레벨 정보), UI-02(660창 재개 버튼) 회귀를 유지한다. Claude Design 담당과 부족한 그림·경계·카드·폰트 규격을 stableID/크기/상태/재현으로 직접 소통하고 실제 화면을 피드백한다. 초기 무기/성장에 없는 기능을 시안에 있다는 이유로 구현하지 않는다.

## 검증과 인계

실제 컴파일/EditMode/PlayMode/macOS 개발 빌드와 실제1100/660 화면·입력을 확인한다. 임포트(rect/pivot/fps/loop/레이어/flip)·6레이어 동작·적 구별·공격/XP 표현·정지/복귀·저장 복원·재임포트 GUID 안정성을 의미 있는 시험으로 검증한다. 관련 핵심 회귀는 별도 복사본 변이로 실제 행동 실패를 확인한다. 이전 25/7 수를 유지하는 것만으로 새 아트 검증을 대신하지 않는다.

새 증거 폴더에 명령·실제 exit·XML·PNG/재현을 남긴다. 원본/표현/검사된 엔진 지원 범위를 구분하고 물리기기·성능·모바일은 NOT_RUN이면 그대로 둔다. 게임 저장은 원본 보존/원복하며 Editor/Player 동시 소유를 피한다. 완료 시 root가 독립 코드·아트/UI 리뷰→필요 수정→GC→commit/push/CI를 진행한다. 이 worker는 commit/push나 추가 worker 호출을 하지 않는다. 비용 외 확인 없이 작업 범위 내 선택을 진행한다.
