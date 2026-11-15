# R03 schema2 독립 코드 리뷰

2026-11-14. **최종 재검토 PASS — R03S-01/02 CLOSED, 신규 HIGH/MEDIUM 없음.** 순수 저장 준비 slice의 독립 리뷰이며 Unity/DLL/앱 지원 판정이 아니다. 구현자와 다른 담당자가 현재 소스·시험·driver·최종 결속을 읽고, 복사한 Core를 별도 임시 프로젝트에서 빌드해 새 입력을 실행했다. 제품·공유 시험·실제 저장·Unity/GUI·아트는 변경하거나 실행하지 않았다.

## R03S-01 CLOSED: 중첩 DTO의 미지원 필드를 읽고 다시 쓰면서 삭제

위치: `src/BibleSurvivor.Core/SaveFormat.cs:51`과 `:58`의 필드 검사는 envelope와 Run의 직계 필드만 검사한다. Profile 및 Run.World 등 중첩된 저장 DTO의 새 필드는 `DataContractJsonSerializer`가 무시한다. 따라서 이 중간 schema2 reader가 미지원 콘텐츠를 보존 거절한다는 경계를 중첩 상태로 우회할 수 있다.

직접 재현한 두 입력:

1. 정상 schema2 저장의 `Profile` 객체에 `"BossTokens":7`을 추가한다. 정상 이전 저장을 `.bak`에 둔다.
2. 실제 R01 전투 저장의 `Run.World`에 `"BossPosition":{"X":3,"Y":2}`를 추가한다. 나머지 필드와 fingerprint는 그대로 둔다.

각 입력에서 `loaded = store.Load(); store.Commit(loaded, loaded.Revision, "after")`는 예외 없이 성공했다. 새 primary에서 추가 필드는 각각 사라졌다. 정상 저장의 typed 읽기가 성공하므로 backup fallback 여부와 관계없이 손실이 발생한다. 대조 입력 `Run.Boss={}`는 의도대로 `UnsupportedSaveVersionException`으로 거절됐다. 단순 hash/JSON 손상이나 버전3 누락 검사가 아니라, schema2의 미지원 중첩 필드에 대한 검사 범위 차이다.

독립 probe는 원본 Core를 임시 폴더로 복사해 .NET10에서 실행했으며 exit0과 각 입력의 `accepted_and_committed/lost:true`를 실제 확인했다. 임의 필드명 자체가 확정 게임 규칙이라는 뜻은 아니다. 후속 schema2 writer가 중첩 상태를 확장할 때 이 준비 writer가 그 상태를 조용히 버리는 경계를 재현한 것이다. 보스 단계가 아닌 normal 상태에도 새 영구 상태나 전투 상태가 포함될 수 있으므로 상위 Stage 검사로 보장되지 않는다.

수정 수용 조건: 지원하는 저장 DTO 객체의 필드 집합을 중첩까지 검증하거나, 미지원 데이터를 손실 없이 보존하는 명시 계약을 적용한다. dictionary의 실제 게임 키와 DTO 속성 이름을 혼동해 모든 key를 막지 않는다. schema2 미지원 콘텐츠는 손상으로 처리해 옛 backup으로 덮어쓰지 말고 보존 거절해야 한다. 최소 Profile·World 두 원래 입력에서 Load/Commit 거절과 primary/backup byte 불변을 시험하고, 기존 v1 입력 및 R01/R02 호환 시험을 유지한다. 같은 리뷰어 재검토가 필요하다.

### 같은 QA 재검토

수정된 명시 DTO shape와 재귀 검사를 읽고 새 임시 Core 프로젝트에서 원래 `Profile.BossTokens`·`Run.World.BossPosition`을 직접 재주입했다. 추가 Enemy 항목의 미지원 필드도 함께 확인했다. 세 입력 모두 `UnsupportedSaveVersionException`이며 primary/backup byte 불변이었다. 정상 dictionary Key/Value 게임 키·nullable World·schema1 중첩 확장은 직접 Load/Commit에 성공했다. 이6개 독립 확인은 구현자의112/18/9 숫자와 합산하지 않는다. R03S-01을 닫는다.

수정 결속 `evidence/r03-schema-nested-20261114/final-binding.json`의28파일 SHA도 현재 파일과 일치했다. 수정 전 실제3시험 실패·원래 shallow 검사 변이의 같은3시험 실패·원복112 PASS, 기존18/추가9변이 및 역사적 binary30쌍 근거를 확인했다. 이는 구현자 실행 근거다. reflection 없이 지원 DTO의 속성과 collection/dictionary entry를 명시하며, nullable/primitive의 실제 타입 검증은 기존 typed DTO에 남긴 범위가 수정 의도와 맞는다.

## R03S-02 CLOSED: 미래 BattleSnapshot 버전을 손상으로 판단해 옛 backup으로 대체

위치: `src/BibleSurvivor.Core/SaveFormat.cs`의 World shape는 `SchemaVersion`을 알려진 값 필드로만 통과시킨다. `src/BibleSurvivor.Core/BattleWorld.cs:120`의 기존 `BattleSnapshot.Validate()`는 지원하지 않는 World version에 `ArgumentException`을 던진다. 이어 `SaveState.cs`의 LoadCore가 이를 손상으로 취급해 backup을 읽으므로 schema2 미지원 중첩 콘텐츠의 보존 거절 경계가 아직 완전하지 않다.

독립 입력은 실제 정상 schema2 R01 저장에서 `Run.World.SchemaVersion=2`로만 바꾸고, 진행 롤백을 눈으로 구분하기 위해 `Profile.Gold=19`로 두었다. `.bak`에는 정상 schema1 R01 저장(Gold0, World version1)을 뒀다. 현재 지원 World version은1이다. 직접 `Load()`하면 예외 대신 Gold0/World version1의 backup을 반환한다. 그 객체를 `Commit(loaded, loaded.Revision, "after")`하면 성공하고 미래 World primary를 옛 상태로 교체한다. 별도 임시 Core 실행 exit0의 관측값은 `accepted_gold_0_worldv_1`이었다. 사용자 저장에는 접근하지 않았다.

수용 조건은 World version2 지원 구현이 아니다. schema2에 포함된 미래 BattleSnapshot version을 typed validation/fallback 전에 `UnsupportedSaveVersionException`으로 보존 거절해야 한다. 원래 version2 입력과 정상 옛 backup을 직접 재주입해 Load/Commit 거절·두 원문 byte 불변을 확인하고, 현재 version1/nullable/기존 schema1 입력의 복구·호환 경계를 유지한다. 이 분기를 제거한 정확한 원래 fallback 구조의 회귀도 확인한다. 같은 리뷰어 재검토 전 최종 저장 slice PASS로 승격하지 않는다.

### 같은 QA 최종 재검토

`CheckWorldVersion`은 World 객체의 정확한 단일 양의 정수 버전을 typed validation 전에 읽는다. 지원하는 envelope schema1/2 양쪽에 적용하며, 현재 World1보다 큰 정수·Int32 overflow는 미래 버전으로 보존 거절한다. 누락/중복/문자열/bool/null/0/음수/소수/지수는 명시 손상 정책을 따른다. absent/null World는 그대로 지원한다. 이 변경은 미래 World 형식을 읽는 기능이 아니다.

새 복사 Core 프로젝트에서 직접25개 입력을 실행했다. schema1/2×World2/정수overflow×정상 primary·backup-only·손상 primary+미래 backup의12개에서 Load/Commit의 `UnsupportedSaveVersionException`, primary/backup byte 불변과 archive 미생성을 확인했다. primary 입력은 원래 Gold19/World2와 legacy Gold0 backup을 사용했으며, backup 사례는 incompatible Tuning 형식도 포함했다. 잘못된 World version7개는 손상으로 분류해 정상 backup을 읽고 Load 자체는 원문을 바꾸지 않았다. 정상 World1·null·absent×schema1/2의6개는 정상 읽기/쓰기와 schema1 원문 archive를 확인했다. 최종 독립 probe exit0이었다.

같은 임시 Core에서 World 선행 검사 호출을 제거한 정확한 옛 분기는 컴파일 후 `future protection 1 2 primary` assertion으로 실제 실패(exit134)했고, 원복한 소스에서25개 전체가 다시 exit0이었다. 첫 probe 수집 시 잘못된 예상 revision에서 발생한 stale-save 예외는 검출 근거로 세지 않았다. probe가 예외 유형을 수집하도록 고친 뒤 위 명시 assertion 실패를 확인했다. 제품 소스나 공유 시험은 변경하지 않았다.

`evidence/r03-schema-world-version-20261114/final-binding.json`의 최신28파일 SHA는 현재 파일과 모두 일치했다. 구현자의 수정 전115개 중3개 XML 실패, 정확한 World guard 제거 변이의 같은3개 실패 및 원복115 PASS·기존18/추가10변이·실제 역사적 binary30쌍 근거를 확인했다. 이 구현자 숫자를 독립25개에 합산하지 않는다. 원래 두 이슈를 닫으며 이번 범위에서 신규 HIGH/MEDIUM은 확인하지 않았다.

## 확인된 구현과 근거

- 최종 결속 `evidence/r03-schema-20261114T023436314332Z/final-binding.json`의 소스/시험/driver/진행·회고 문서28파일 SHA를 현재 파일과 직접 대조했다. drift0이다. 이 숫자는 독립 새 시험 횟수가 아니다.
- 버전은 typed Run 읽기 전 확인하며 양의 큰 정수 overflow도 미래 버전으로 거절한다. 미래 primary에서 옛 backup 복구를 금지하는 예외 경계를 확인했다. schema1 메모리 이행은 Load 자체를 쓰기로 바꾸지 않는다.
- legacy 보존본은 sibling 임시 디렉터리의 원문·manifest를 검증하고 rename으로 게시한다. 게시 실패는 교체 전에 중단하며, 기존 archive는 이후 손상된 primary와 비교하지 않고 자기 원문/hash를 검사한다. backup-only와 후속 손상 primary/legacy backup 복구 시험이 있다. 이 경로에서 추가 HIGH/MEDIUM은 확인하지 않았다.
- 구현자 근거의 Core106/106·14 CLI, 기존18변이 및 새8변이, 역사적 실제 binary30쌍/30migration/30old-reader거절을 읽었다. 새8변이는 실제 XML 실패 목록과 exit1·원복106 PASS를 포함하며 컴파일 실패를 검출로 세지 않는다. 이를 리뷰어 본인의106/18/8 실행 결과로 합산하지 않는다.
- `tests/.../Program.cs`의 새 시험 등록과 기존 schema assertion 변경은 새 writer2에 맞는다. `check_schema_compat.py`는 pinned 역사 ref를 실제 빌드하며 자동 fetch하지 않는다. `.github/workflows/core.yml`의 `fetch-depth: 0` 및 두 schema driver 실행 추가는 이 요구와 정합한다. 원격 CI는 아직 이 리뷰에서 실행하지 않았다.

## 범위·남은 게이트

Core DLL/provenance sync와 최신 Unity 검증, GC 통합·선별 commit/push·해당 SHA CI는 후속이다. 새 XML/header/archive 코드의 Unity/IL2CPP·모바일 실행, 성능·실제 사용자 저장은 NOT_RUN이다. 기존 DLL 기반 R02 아트 검증을 schema2 앱 검증으로 전용하지 않는다.
