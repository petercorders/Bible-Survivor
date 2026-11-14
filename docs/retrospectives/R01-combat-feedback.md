# 아담 P0 타격감·Unity 제공 기능 시험 회고

2026-11-14. 단일 구현자, 추가 worker 없음. root가 독립 리뷰/GC와 commit/push를 소유한다. 전역 규칙·기존 CI/hook은 수정하지 않았다.

- 판단 교정: 살아 있는 적 HP 차분은 치명 명중을 지우고 같은 적의 복수 명중을 합친다. 실제 이전 HP차분 구조를 QA 사본에 재현해 Expected2/Actual0과 Expected2/Actual1을 확인했다. 이벤트 cursor 재소비도 Expected2/Actual4로 검출했다. 비슷한 변이를 같은 증거로 대신하지 않았다.
- 전투 경계: 사건 배열/난수/순번을 snapshot에 넣지 않았다. 동일 입력의 전후145개 전체 Core/저장 직렬화 기록은 같았지만 A/B Player의 저장 파일은 서로 다른 실제 operation ID를 사용한다. 후자는 World/Run/Profile 동등과 전체 파일 동등을 구별해야 한다.
- QA 설계 교정: 최초 Core 변이 사본에 content를 누락해 baseline이 실패했다. baseline0이 확인되지 않은 변이 결과는 폐기하고 필요한 디렉터리를 포함해 다시 실행했다. 새 빌드 코드의 컴파일 괄호 누락·fixture catalog health 제한 위반도 실제 실패 근거를 남기고 수정했다.
- Unity 실험: 실제 파티클을 Scene에 만드는 것과 IMGUI 전투 영역에 보이게 합성하는 것은 다르다. Point RenderTexture·명시 aspect/투영·culling bounds·클리핑·수동 pause를 별도 QA 코드로 시험했다. alive count만으로 렌더 성공을 세지 않는다. 초기 edge 입자 미표시와 QA 종료 정리 오류를 PASS로 숨기지 않았다.
- 저장 안전 교정: 새 제품명만으로 QA 저장 격리를 확정하지 않았다. 실행 전 Info.plist에서 기존 standalone bundle ID를 발견해 실제 Adam 씬 앱은 실행 보류했고, root에 최소 Editor 변경을 인계했다. QA 전용 bundle ID와 첫 무저장 path-probe 씬을 추가해 실제 persistentDataPath를 확인한 뒤 게임 씬을 로드하도록 했다. 옛 앱은 보존하고 실행 금지로 표시했다.
- 도구 관측: QA Editor CmdQ 후 Orca AX는 접근 오류를 반환했으나 실제 프로세스 exit0을 먼저 확인했다. 권한 재설정/restore-window/재실행 루프로 이미 종료한 앱을 되살리지 않았다. 사용자 iOS를 조회하거나 입력하지 않았다.
- 에셋 출처: 공식 무료 팩의 MyAssets 다운로드 원본/해시/Readme/의존을 ignored에 보존했다. 전체 Settings/샘플을 임포트하지 않았고, 공식 도구 사용과 새 픽셀 아트 제작/Claude Design 실행을 혼동하지 않는다.

후보는 QA 단계별 source·시험 XML·빌드 입력 SHA와 무저장 경로 probe를 묶는 재현 도구화다. 이번에는 제안과 근거만 남기며 전역 학습 상태를 자동 변경하지 않는다. 모델/effort·input/output/cache/reasoning 사용량과 동일 조건의 대안 비용은 미측정이다. 단일 worker의 실제 기여와 재시도만 기록하며 추정 절감률/성능 향상을 주장하지 않는다.

마감 근거: bounds 두 설정만 제거한 실 앱의 동일 edge PNG 검사는 resize 전/후 모두 exit1, 정상 복원 앱은 각각0이었다. 최종47Edit/16Play와 새 QA 앱/source manifest를 별도로 결속하고 자동 meta79개·빈 아이콘만 원복했으며 제품 파티클 미연결과 합성 inset/실물 safeArea 미검증을 구별했다.

통합 GC 진행: 구현자와 독립된 코드 리뷰는 P0 사건·전투/저장 불변·풀/오디오 경계와 원래 회귀 변이를 검토했다. 초기 소스/시험 결속의 LOW CFB-01은 자동 meta 정리·최신 Editor 안전 경계·새 빌드 후302파일 최종 manifest로 재검토한다. PC GUI 검토는 별도 담당이 실제 앱/무저장 probe/원본 저장 보존을 소유하며 동시에 다른 실행자를 두지 않는다. 공식 팩 다운로드와 제품 임포트·QA 파티클과 제품 연결을 구분해 기록한다. 사용자 iOS 플레이는 전체 과정에서 보존한다. 최종 독립 결과·GC/commit/해당 SHA CI 결과는 후속 기록으로 남긴다.

통합 GC 종료: 동일 코드 리뷰어가302소스·28개 Core/Runtime 고정본·최신4앱 각334파일·47Edit/16Play·기존18변이·QA 무저장 probe를 대조해 CFB-01을 닫았다. 잘못된 신규 변이 결과 참조는 실제 `impact-mutations/results.json`으로 진행 문서와 ignored ledger에서 정정했다. 별도 PC GUI 리뷰는3무기·자연 성장/사망·중복 선택·1100/660·정지 재개를 직접 확인했고 추가 HIGH/MEDIUM은 없었다. 종료 후 PC6/QA3 보존9/9와 소스/앱 해시 불변도 확인했다. 청감/정밀90ms/실제F6–F8/성능/모바일/제품 파티클 미실행은 그대로 유지한다. 모델별 총 token/cache/reasoning 비용은 미측정이며, 역할별 실제 실행과 읽기 전용 검토를 구분했다. 다음 제품 파티클 작업은 이 커밋 경계 뒤 단일 작성자에게 소유를 넘긴다.
