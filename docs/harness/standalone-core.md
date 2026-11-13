# 엔진 독립 코어 하네스

현재 코어는 .NET Standard 2.1 / C# 9로 빌드한다. 검사·시뮬레이션 호스트는 .NET SDK 10.0.404다. 전역 PATH나 셸 설정을 바꾸지 않고 `tools/dotnet.sh`가 로컬 SDK 또는 기존 PATH의 dotnet을 사용한다. 다른 설치 위치는 `BIBLE_SURVIVOR_DOTNET`에 실행 파일 경로를 지정한다.

```sh
python3 tools/check_all.py
python3 tools/check_mutations.py
bash tools/dotnet.sh run --project tools/BibleSurvivor.Sim -- bible
```

첫 명령은 코어 의존 경계, 콘텐츠 무결성, C# 빌드/회귀 시험, 시작 무기별 규칙 시뮬레이션을 실행한다. 실행 로그·JUnit 형식 XML·JSON 결과는 매번 새 `evidence/` 폴더에 저장한다. 시험 호스트는 외부 패키지 없는 콘솔 검사기이며 `dotnet test`/Unity Test Framework 실행이 아니다. `check_mutations.py`는 임시 사본에서 실제 회귀를 재현하고 컴파일 오류가 아닌 시험 실패인지 확인한다.

로컬 저장소에는 `tools/hooks/pre-commit`을 `.git/hooks/pre-commit`으로 설치했다. 다른 clone에서는 기존 훅 유무를 확인한 뒤 설치하며 덮어쓰지 않는다. GitHub Actions는 커밋에 고정한 공식 checkout/setup-dotnet 액션으로 동일 검사를 실행한다. 프로젝트 편집/Stop 훅과 Unity CI·asmdef 경계 검사는 아직 미설치다. 단순 파일 검색 기반 코어 검사는 C# 의미론 분석을 대체하지 않는다.

## 저장·전환 계약

`SaveStore.Transact(expectedRevision, operationId, change)`는 마지막 커밋의 독립 사본을 바꾸고 임시 파일 검증·flush·원자적 교체 후 결과를 읽는다. 실패 시 기존 저장은 보존된다. 결과 불명 시 같은 revision/operation ID로 재시도하고 새 입력 전 상태를 재조회한다. 오래된 revision은 거절한다. 파일 잠금은 다중 저장 인스턴스의 덮어쓰기를 막는다. 실제 모바일 파일 시스템·강제 종료 내구성은 별도 시험 대상이다.

스키마 1을 유지하고 R02에서 optional `RoundFingerprint` 필드를 추가했다. 없는 저장은 이전 R01 진행/완료→R02 대기까지만 읽고 다음 Capture/전환 시 보강한다. fingerprint 없는 R02 진행/R03 대기는 거절한다. 신규 fingerprint는 라운드 ID·웨이브·적 수치·XP·동시상한·완료골드·해금을 묶으므로 원본 변경 시 Resume를 거절하고 저장을 보존한다. 이전 필드 없는 R01 저장은 미래 웨이브 변경을 검출할 수 없으며 이 경계에서 원본 R01 catalog를 유지해야 한다. World 없는 저장은 좌표 복원으로 간주하지 않는다. 구 실행기는 R02를 재개할 수 없으므로 downgrade 호환은 제공하지 않는다. 손상 저장은 거절 또는 유효 백업으로 복구하며 미래 schema는 백업으로 덮지 않고 거절한다. 백업은 이전 스냅샷이라 마지막 거래가 복구되지 않을 수 있으며, 실제 앱의 저장 주기·중단/복귀 정책은 Unity/모바일 단계에서 확정한다.

`AdamSession`은 R01/R02 순수 규칙 세션이다. 피해는 `Hit`를 통해 처리하며 `Wave`는 조회용이다. 처치로 생긴 드롭을 `Collect`해야 경험치를 받는다. 미처리 레벨업 선택은 전투 시간을 멈추고 저장에 후보 목록을 보존한다. 라운드 종료 시 남은 드롭만 자동 회수하고, 선택이 남아도 다음 라운드 대기 체크포인트에서 재개한다. `Resume`에는 진행 중이면 현재 라운드, 대기 중이면 마지막 완료 라운드의 정의를 전달한다. 선택을 모두 처리한 R01 완료 세션의 `StartNextRound(r02Definition)`이 새 세션을 반환한다. 이전 세션/월드의 진행·Capture는 더 이상 유효하지 않으며 새 `BattleWorld`를 만든다. 성장/장비/영구 스냅샷/킬 remainder는 유지하고 적·드롭·투사체·위치·공격/피격 시계는 초기화한다. 회복/보상은 완료 시에만 적용한다. 전환을 `SaveStore.Transact` 안에서 실행하면 실패/결과 불명 재시도를 같은 revision/operation ID로 처리할 수 있다. R02 완료 후 RoundNumber=3, Stage=between_rounds로 저장하며 R03 진입은 거절한다.

입력 어댑터는 한 프레임의 치명적 피해를 `Advance`에 의한 종료 판정 전에 전달하는 순서를 사용한다. 이 순서면 사망이 우선이며, 이미 완료된 라운드에 뒤늦게 피해를 적용하지 않는다. 실제 Unity 프레임·물리 호출 순서는 통합 단계에서 검증한다.

공통 영구 강화 8종의 구매·선행 조건·무료 전체 환불을 구현했다. 가격표는 스키마 1에서 고정이며 향후 가격 변경 전 실제 지불 가격 이력의 마이그레이션이 필요하다. 무기별 영구 노드와 전체 420종은 미구현이다. 보조 효과는 세션의 공격 프로필·이동/흡수 배율·방어·XP·반격 수치로 제공하며 실제 이동·충돌·반격 연출은 어댑터 작업이다.

공격 시계와 피격 기록의 프리미티브에 이어 `BattleWorld`가 60Hz 좌표 이동·타깃·자동 공격·상대 이동 스윕 충돌·관통·거리 회수·접촉 피해/반격을 세션에 연결한다. 월드 좌표·난수·공격/투사체·드롭·고정 step 나머지도 `RunState.World`에 선택적으로 저장한다. 이전 World 없는 저장은 기존 규칙 세션으로 읽으며 위치가 없는데 좌표 전투가 복원됐다고 표시하지 않는다. World가 있을 때는 라운드 시간/적/드롭 ID의 교차 정합을 저장 전에 검사한다.

`session.Capture()` 뒤 `state.Run.World=world.Capture()`로 동일 시점의 전체 체크포인트를 만들고 저장한다. 같은 시드·시간별 입력으로 복원 전후의 직렬화된 전체 월드가 같은지 시험한다. 플레이어 입력은 단위 벡터로 제한하고 이동이 없으면 마지막 방향을 유지한다. 튜닝과 몬스터 속도 계약이 다른 복원은 거절한다. 렌더링·입력 장치·Unity 충돌과 모바일 적용은 별도다.

```sh
bash tools/dotnet.sh run --project tools/BibleSurvivor.Sim -- --combat bible 7
bash tools/dotnet.sh run --project tools/BibleSurvivor.Sim -- --combat jawbone 7
bash tools/dotnet.sh run --project tools/BibleSurvivor.Sim -- --combat sling 7
```

CLI 입력은 가까운 경험치를 향하고 없으면 원형 경로를 도는 자동 정책이다. 특정 무기의 승리/처치 수를 보장하지 않는다. seed7에서 근접 무기는 봇이 적과 거리를 벌려 처치 수가 낮고 물맷돌은 멀리 있는 적을 공격한다. 이 결과를 무기 밸런스/사용자 조작감으로 일반화하지 않는다. 기존 처치 주입 모드와 실제 좌표 전투 모드는 각각 별도 검증이다.

## 공식 출처

- [Microsoft macOS 설치](https://learn.microsoft.com/dotnet/core/install/macos), [설치 스크립트](https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script): 로컬 SDK 준비.
- [Unity API compatibility](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html): 호환 API 대상 선택. 실제 Unity 컴파일은 아직 NOT_RUN.
- [공식 checkout](https://github.com/actions/checkout), [공식 setup-dotnet](https://github.com/actions/setup-dotnet): CI 액션의 v4 참조 SHA를 추가 시 확인해 고정했다.
