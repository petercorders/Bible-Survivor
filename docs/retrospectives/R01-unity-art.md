# R01 Unity 아트 연결 GC 인계

전역 학습/규칙 파일은 변경하지 않는다. 아래는 root의 독립 리뷰·GC를 위한 근거와 제안이다.

- 원본 보존:03 원본과 outline sidecar는 검수된 별도 패키지 그대로 두고, Unity용 복사·정규화 index와 `.meta`만 소유했다. 검수 hash pin/실제 imported frame 검사를 준비 도구·임포터·런타임·빌드 전 검사로 연결했다. source PNG/Font 바이트와 Sprite/Font GUID·local ID 재임포트 안정성도 실제 Editor에서 대조했다.
- 구현 판단: 기존 IMGUI 경로를 유지해 UI-01/02 실제 클릭 회귀를 보존했다. Core snapshot 읽기 전용 표현으로 전투 원본을 하나로 유지했다. 월드 정수 배율 때문에 보이는 카메라 영역이 작아지는 점은 문서에 명시했고 밸런스 개선/성능 개선으로 세지 않았다.
- 엔진 관측: 빈 fontNames는 importer가 자체 가족명으로 정규화한다. 초기 시험의 빈 배열 가정은 실패했고 자체 이름만 존재·외부 fontReferences 없음·FontEngine 실제 glyph face·binary atlas 검사로 수정했다. 단순 HasCharacter만으로 OS fallback 미사용을 단정하지 않는다.
- 시험 설계 교정: 자연 전투 fixture가 슬롯64회 순환 전에 사망해 강화 시험이 실패했다. 실제 피격·처치 연결과 고정 EffectPool의 순환/재할당/초기화 시험을 나누었으며 실패 로그를 보존했다. 카드 순회 초기 시험의 passive 인자 오용도 실제 실패 후 수정했다.
- GUI 도구 교정: 일부 백그라운드 키는 synthetic success여도 전달되지 않았다. raw JSON/화면/저장으로 판정했고 실패를 PASS에 합치지 않았다. 운영체제 로컬 네트워크/자동화 대화상자로 가려진 경우도 분리했다. 종료 hotkey에 restore-window를 붙이면 후속 상태 확인이 앱을 다시 여는 경우가 있어 이후 PID/직접 실행 wait와 restore 없는 종료를 사용했다. 추가 Player가 생겼던 재시도는 전부 종료했으며 최종 저장 여섯 파일 원복을 확인했다.
- 변이: ignored 별도 복사본에서 manifest fps를 무시하고 첫 포즈에 고정한 원래 정적 표현 회귀를 재현했다. 컴파일 오류 없이 AllSixLayers 시험이 expected1/actual0으로 실패(exit2)했고, 같은 복사본을 원복한 재실행은 PASS(exit0)다. 주 프로젝트는 변이에 사용하지 않았다.

도구화 제안(미적용): GUI 실행을 Popen PID/명령/exit와 원본 저장 보존·원복으로 묶은 좁은 probe 도구를 만들고, 종료 시 restore-window를 금지하는 회귀를 추가할 수 있다. 범용 GUI 권한 자동 승인은 제안하지 않는다. 프로세스 확인은 전체 인자 출력 대신 PID/comm과 대상 경로 pgrep으로 좁힌다. 폰트 검증에서는 자체 face와 fallback 참조를 분리하는 현재 시험을 유지한다.

모델/effort는 현재 worker 설정을 사용했고 추가 worker는 생성하지 않았다. 모델별 input/output/cache/reasoning usage와 비교 비용은 미측정(null)이다. 효율/토큰 절감률을 추정하지 않는다. 반복 실행은 실제 실패·새 폰트/등록 검사·최소 창 크기 변경에 따른 재검증이며 통과 후 추가 범위를 무조건 넓히지 않는다.

남은 게이트: root의 독립 코드·아트/UI 재검토, 필요 수정·동일 담당 재검토, 통합 GC, 선별 commit/push와 해당 SHA CI. 모바일·물리기기·성능/메모리 예산은 NOT_RUN이다.

## 실제 접촉 사망에서 추가로 찾은 것

직접 `Session.Damage`와 빈 arena만으로 마지막 frame을 확인한 첫 시험은 실제 접촉 사망을 대신하지 못했다. Core의 해당 경로는 Wave를null로 하고 arena를 비운 뒤 시각 증가 전에 반환했다. 이전 Player의 exit0/39·10 통과만으로 화면 성공을 단정하지 않고 실제 새 fixture 화면을 열어 NullReferenceException을 발견했다. `death-contact-failing.xml`과 `player/death-visible.log`를 FAIL 근거로 보존했다.

시각만으로 cache를 갱신하던 코드를 종료/전투 counter/위치 변화까지 갱신하도록 고치고 null Wave 및 종료 객체 렌더를 방어했다. 실제 World.Tick 접촉, 같은 시각의 최종 방향/빈 actor snapshot, 전체 UI의 새 도전 rect 클릭을 회귀로 묶었다. 즉시 모달로 사망 pose를 가리던 표시도0.75초 연출 뒤 모달로 바꾸었다. 별도 복사본의 즉시 모달/clock-only 조건 두 변이가 각각 실제 행동 assert에서 실패하고, 각각 원복하면 통과했다. 최종 실제 Player에서도 쓰러진 pose→새 도전 UI를 확인했다. Core 변경은 없었다.

| 분류 | 근거·이미 반영한 것 | 후속 제안(자동 적용 안 함) |
| --- | --- | --- |
| 판단/시험 범위 | 직접Damage/빈 arena가 실제 Step 접촉과 다름; same-time snapshot FAIL | 종료·선택 UI smoke에 실제 Core 전환 fixture를 먼저 사용; 현재10개 PlayMode와 변이를 유지 |
| 도구 동작 | restore를 붙인 종료 재실행, synthetic 입력/포커스 실패 raw JSON | PID/명령/exit/fixture 보존·원복을 묶은 좁은 probe; 상태와 실행 성공을 따로 기록 |
| 반복/효율 | release/handoff/complete/final-validation 여러 실행, 후반 GUI에서 실제 오류 발견 | 첫 build 직후 접촉 사망/중단·재개 smoke를 먼저 실행하고 폰트/표시 변경을 모아 full suite/build를 한 번 검증 |
| 보고 정정 | 초기39/9 및 이후39/10은 당시 범위만 PASS, 첫 실제 접촉 Player는 FAIL | progress 최종 근거를 마지막 final-validation39/10과 verified660/1100/death-fixed로 갱신함 |

모델 적합성: nullable 수명/전투 시각/실제 GUI가 결합돼 현재 worker의 추론이 필요했지만 다른 모델과 같은 조건 비교는 NOT_RUN이다. 전체 토큰/비용/input/output/cache/reasoning/부모 총사용량은 null이며 반복 때문에 절감됐다고 쓰지 않는다. 위임은 사용자 금지에 따라0이고 독립 리뷰는 coordinator가 소유한다. 개선안은 단순 경고 문장 추가보다 기존 실제 전환 회귀와 native probe 도구의 범위를 명확히 하는 쪽이다. 전역/프로젝트 학습 entries·index·overview를 변경하지 않았다.

## 독립 리뷰의 키보드 경계 누락

마우스의 보이는 새 도전 버튼만 검사한 사망 모달 회귀는 digit1–3의 별도 Update 경로를 보호하지 못했다. 입력 경계를 SelectSlot에서 공통 적용하고, InputSystem 실제 상태 이벤트를 보내 Session/Run/골드와 clip 종료 후 새 선택을 검증하는 회귀를 준비했다. UI 담당 실행 소유를 기다리는 동안 소스·오프라인만 진행했으며 새 실행을 통과로 미리 기록하지 않는다. 문장 규칙보다 동일 명령의 마우스/키보드 경계 회귀를 유지하는 개선이며 전역 학습 상태는 수정하지 않았다.


사망 digit 후속 검증은 대기 소유 반환 뒤 실제 기존 코드10/13, 공통 경계 수정13/13을 확인했다. 최종 소스 별도 복사본의 가드 제거도3/3 행동 실패, 원복3/3 통과했다. 마우스 회귀 통과를 키보드 경계 통과로 확장해 기록했던 시험 범위 누락이며, 공통 명령 경계와 실제 각 입력 경로를 함께 검사하는 쪽으로 보강했다. 독립 검토에서 지적된 시험파일 이후 해시 불일치도 새 전체 소스/시험/빌드 해시로 교체했다. 추가 전역 규칙/entries는 만들지 않았다. native 단일 호출 성공과0.75초 clip 내 입력 타이밍 입증은 구별하고, 후자는 InputSystem 시험/변이로 한정했다. 최종 compile39Edit/13Play/macOS 빌드·660/1100 사망 화면·두 Player exit0·여섯 저장 원복 근거를 progress에 연결했으며 이후 독립 재검토/통합 GC/커밋은 root 소유다.

## Coordinator 최종 통합 GC

- 동일 코드/UI 리뷰에서 UA01/ART-UI-01 해결을 확인했다. 새 실제 엔진 입력13PASS와 동일 우회 변이3FAIL→원복3PASS, 추가 시험/가드를 포함한231SHA를 대조했다. 아트 QA도 UAR-01을 닫고 기존 원본83등록/관측 화면 PASS를 유지했다. 추가 HIGH/MEDIUM 없음.
- UI 후속의 restore-window 종료 재실행이 다시 발생했다. 원본 저장을 추가 보존하고 명시PID와 시작시각/인자를 대조해 QA 재실행으로 식별한 뒤 종료했다. 오염된1100 결과는 NOT_RUN으로 제외했다.660 exit0·명시1100 TERM143·재실행 프로세스 exit null/소멸은 서로 구분했고 여섯 원본 저장을 모두 원복했다. 툴 success나 앱 exit0을 표시/검증 성공과 혼동하지 않았다.
- 현재 재사용 가능한 자동 강제는 실제 InputSystem 경계·same-time death·정적 애니메이션 회귀와 준비 도구 손상/기존meta보존 시험이다. 원격 CI에는 준비 도구7개 검사를 연결했다. native 종료/소유권 probe는 여전히 후보이며 범용 GUI 도구나 전역 GC 상태를 수정하지 않았다. 다음 native QA는 PID가 살아 있는 동안만 조작하고 종료 복구 옵션 없이 회수하는 인계 근거를 함께 제공해야 한다.
- coordinator는 소스·복사 에셋·고지·메타/GUID와 개인 경로/비공개 자료 제외를 확인한다. README/시연 PNG는 검토용 연출임을 명시한다. 게임 소스/시험/빌드의 검수 후 변경은 없으며 문서/검사 CI 연결만 정리했다. 실제 토큰/모델별 usage·절감률은 미측정이다.
- 완료 커밋의 범위는 아담 macOS 시제품 아트 연결이다. 후속 라운드 엔진 연결·외형 편집·모바일/기기·성능·전체 수동 생존 검증은 별도 미완료다. 사용자 승인에 따라 GC 뒤 작업 브랜치에 push하고 새 SHA의 CI를 확인한다.

완료 확인: `48910998158eb6e791cf48a5349bd1eac070e4ac` 원격 SHA 일치, 코어/동기화/선언 경계/준비 도구 CI43485410634와 아트/폰트 CI43485410596 모두 성공했다. source/SDK 로컬 게이트와 원격 오프라인 게이트는 별도 범위를 유지했다. 다음 모바일 환경 감사는 게임 소스를 바꾸지 않는 읽기 전용 작업으로 시작했다.
