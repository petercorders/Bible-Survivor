# Unity 설치 전 코어 하네스

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

스키마 1만 지원한다. 초기 이전 저장이 없으므로 마이그레이션 체인은 아직 없고 구버전/손상 저장은 거절 또는 유효 백업으로 복구한다. 미래 버전은 백업으로 덮지 않고 거절한다. 백업은 이전 스냅샷이라 마지막 거래가 복구되지 않을 수 있으며, 실제 앱의 저장 주기·중단/복귀 정책은 Unity/모바일 단계에서 확정한다.

`AdamSession`은 순수 규칙 세션이다. 피해는 `Hit`를 통해 처리하며 `Wave`는 조회용이다. 처치로 생긴 드롭을 `Collect`해야 경험치를 받는다. 미처리 레벨업 선택은 전투 시간을 멈추고 저장에 후보 목록을 보존한다. 라운드 종료 시 남은 드롭만 자동 회수하고, 선택이 남아도 다음 라운드 대기 체크포인트에서 재개한다. 다음 라운드 전투 자체는 아직 구현하지 않았다.

입력 어댑터는 한 프레임의 치명적 피해를 `Advance`에 의한 종료 판정 전에 전달하는 순서를 사용한다. 이 순서면 사망이 우선이며, 이미 완료된 라운드에 뒤늦게 피해를 적용하지 않는다. 실제 Unity 프레임·물리 호출 순서는 통합 단계에서 검증한다.

공통 영구 강화 8종의 구매·선행 조건·무료 전체 환불을 구현했다. 가격표는 스키마 1에서 고정이며 향후 가격 변경 전 실제 지불 가격 이력의 마이그레이션이 필요하다. 무기별 영구 노드와 전체 420종은 미구현이다. 보조 효과는 세션의 공격 프로필·이동/흡수 배율·방어·XP·반격 수치로 제공하며 실제 이동·충돌·반격 연출은 어댑터 작업이다.

공격 시계와 피격 기록은 독립 프리미티브다. 실제 세션 전투 스케줄러·타깃 탐색·이동/물리·시계 저장/복원은 Unity 통합 시 연결한다. 이 단계의 시뮬레이션은 처치를 주입하므로 공격 DPS·난이도·밸런스 검증이 아니다.

## 공식 출처

- [Microsoft macOS 설치](https://learn.microsoft.com/dotnet/core/install/macos), [설치 스크립트](https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script): 로컬 SDK 준비.
- [Unity API compatibility](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html): 호환 API 대상 선택. 실제 Unity 컴파일은 아직 NOT_RUN.
- [공식 checkout](https://github.com/actions/checkout), [공식 setup-dotnet](https://github.com/actions/setup-dotnet): CI 액션의 v4 참조 SHA를 추가 시 확인해 고정했다.
