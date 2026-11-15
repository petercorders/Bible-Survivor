# R02 아트 연결 구현 회고

## 최종 통합 GC — 2026-11-15

footpivot/schema2 최종 통합은 독립 코드·시각 리뷰 모두 신규 HIGH/MEDIUM 없이 한정 PASS다. 최신 결과는 진행 문서와 `docs/reviews/unity-r02-final-code.md`/`unity-r02-art-ui.md`를 따른다. 아래 준비 checkpoint와 당시 보류 항목은 이력이며 최신 상태로 해석하지 않는다.

사용자는 대조·캡처의 과잉 반복을 지적했다. 해상도×종×프레임×방향×모드의 넓은 조합과 반복 빌드는 이번 표시 변경에 비해 비용이 컸다. 실제 발 anchor 오류와 저장 원문 손실처럼 동작에 영향을 주는 검증은 유지하되, 다음부터 대표 사례·관련 회귀·원래 결함 변이1회로 좁힌다. 이미 검증한 원본/전체 픽셀·해시는 새 드리프트 근거가 없으면 재검증하지 않는다. 소스·빌드 경계 확인과 미세색 반복 대조를 구분하고 독립 리뷰도 근거를 재사용한다. 이 기준은 프로젝트 AGENTS에 반영했으며 전역 규칙은 수정하지 않았다.

최종 QA 로그의 driver 초기 host=null 예외와 fixture 종료의 stale 진단은 리뷰의 비차단 한계로 기록했다. 이것을 자연 플레이 오류나 제품 관리예외0이라고 바꿔 보고하지 않는다. native 연속 조작·자연 군집 가독성·모바일/IL2CPP·성능은 미검증이다. 추가 제품 변경 없이 최종 필수 커밋 검사·업로드·해당 SHA CI로 마무리한다.

2026-11-14. 준비 checkpoint 기록이며 최종 Core sync와 독립 리뷰/GC/commit/push는 coordinator가 담당한다. 전역 하네스 학습 상태·Core/공유 시험·기존 checker/CI/hook은 변경하지 않았다.

- 원본 승격: R02-04 CLOSED인 checker/manifest SHA와 같은 입력을 격리 검증한 뒤 public whitelist만 복사했다. 원본 README의 private Design URL·캔버스·raw QA·archives는 복사하지 않았다. 생성기 PNG와 Design 제품 출력을 구분했다.
- 격리 참조: 전체verify의 producer selftest는 전달된 CLI flag와 달리 sibling 참조를 자체 해석했다. 처음 격리 검사가 참조 누락으로 실패했고, 원본 수정 대신 격리 참조 사본을 보완해 verify0을 확인했다. 직접 checker0과 전체verify0은 다른 게이트다.
- 픽셀 판정: 원시 RGBA와 실제 프레임버퍼의 색 인코딩을 혼동한 첫 검사0일치/exit2를 보존했다. 원시 해시는 그대로 검증하고 실제 Linear 출력의 gamma 변환을 별도로 비교했다. 정확한 legacy blit 전체 원인은 단정하지 않았고 공용 렌더링 설정/Art03는 변경하지 않았다.
- 재현성: 정상 앱의 반복 첫 캡처는 RGB 최대4차로 기존 허용차3에서 실패했다. 실제 실패를 남기고 QA에3프레임 대기·화면 허용차5·98% 불투명 픽셀 일치를 사용한다. 구조/프레임/flip/원시 해시 검사는 유지하며, 기하 표식 옛 구조는 같은 픽셀 위치에서0일치로 검출된다. 반복을 foreground 입력 복구 루프로 사용하지 않는다.
- 변이 근거: 정확한 기술 checkpoint의 두 파일로 돌아간 앱 build0→실제 프레임버퍼 assertion exit2를 보존했다. 별도 pivot/fps/임포트 pivot 가드 제거는 assertion FAIL, 원복은 PASS였다. 컴파일 실패를 행동 검출로 세지 않는다.
- 회귀/범위: 세 무기의 실제 명중 후 표현 on/off Run/Profile/World 동등을 확인했다.24개 staged 시각 조합·입자 상한 관측을 성능/자연 수동·실물 PASS로 바꾸지 않는다. 진행 중인 Core schema2와 이전 Unity DLL의 검증 범위를 분리한다.
- 도구화 후보: 승인 입력 inventory와 read-only 검사 드라이버의 CI 연결은 root에 인계한다. private 파일 배제/누락·변경·대문자 PNG 및 중복source 음성 입력을 검사했다. 기존 checkers/hooks를 구현자가 수정하지 않았다.
- 모델/비용: 단독 구현자, 추가 worker/새 유료 도구 없음. 실제 토큰·cache·reasoning usage/effort 미측정이며 절감률은 추정하지 않는다. 추가 실행은 실제 실패 교정·옛 구조 검출·최종 소스/앱 결속 목적이며 무관한 회귀 확대를 피한다.

최종 결속 후 원본/PC6·자체 QA3 원복·정확 자식 종료와 앱/소스 해시를 progress 문서에 확정한다. unknown 옛 QA·사용자 iOS/실저장은 조회·조작하지 않는다.

최종 실제 compile/84Edit/30Play와 새 앱 build/capture/새 프로세스Load exit0을 확인했다. 최종30probe는 불투명 픽셀 전부 일치/max RGB delta1이며,24조합·12아레나/24HUD 비교도 통과했다. 이전205보호파일·외부/공개21입력·PC6개를 보존하고 자체 QA3만 원복했으며 자체 자식 종료/같은 identity writer 부재를 확인했다. 최종 소스/앱 결속은 progress와 ignored binding에 남겼다. Core schema2 sync·최종 조합 재검증·독립 PC 리뷰/GC/커밋은 남겨 두며 최신 Core PASS로 전용하지 않는다.


## 디자인 P1 깊이 정렬 후속

- 경계: 화면에 실제 투영·반올림한 발 y로 적 본체만 정렬하고 ID로 동률을 결정했다. 원본 PNG/색 보정·그림자/플레이어 정렬·Core 순서 변경으로 확대하지 않았다. 재사용 버퍼의120개×1,000회 warmup 후 할당0을 확인했으며 전체 프레임 최적화로 과장하지 않는다.
- 검출: 정확한 원래 정렬 제거 구조는 두 화면 크기에서 실제 순서 assertion FAIL, 원복3개 PASS였다. 이전/이후 같은24개 전투의 Run/Profile/World가 같고 B3 겹침은 바뀌었다. B2 고유색 면적은 오히려 줄어든 장면이 있어 가독성 전반 개선으로 보고하지 않는다.
- 관측 fixture 오류: 저장 후 ID 순으로 바뀐 컬렉션에서 '첫 적'을 다시 고르면 전후 다른 대상을 가리킨다. 첫 B2 촬영 exit2를 보존하고 선택 안정 ID를 배치·관측에 공유해 수정했다. 실제 명중/색 코드는 바꾸지 않았다.
- 실시간 검사: 앱 캡처 병행 중 터치 테스트의 .1초 realtime wait가 .2초 Core 진행으로 관측되어 제한 assertion이 실패했다. 동일 코드/시험을 캡처 종료 뒤 직렬 재실행해30/30 PASS였다. 첫 실패도 남겼으며 병행 부하가 원인이라고 단정하지 않는다. 향후 입력 의미와 벽시계 제한을 분리하는 시험 설계 후보만 인계한다.
- 정직한 결과: B2 .09초 반응의 tint/스쿼시/P1은 실제 staged 영상 프레임에서 확인했다. 소수 시점의 픽셀 수를 자연 군집 가독성·접근성·전체 성능/모바일 PASS로 전용하지 않는다. 추가 신호 변경은 root 판단에 맡긴다.
- 소유: 두 새 suffix QA 앱만 정확 자식 PID로 실행·종료하고 자체 저장3개씩 부재를 원복했다. 기존 PC6 byte 보존, unknown QA/iOS는 조회하지 않았다. Core schema2 소스·DLL/sync 금지를 유지해 최종 종합 해시는 별도 인계 게이트다.


## 후속 P2·모델 분리 적용

- 사용자 승인으로 고정 캡처 측정만 경량 helper(requested gpt-6-luna/low, fork none)에 분리하고 제품·Unity·GUI 단일 소유를 유지했다. helper의 resolved 모델/effort·usage는 원문이 없어 null이며 절감률을 추정하지 않는다. 실제 helper가 settled한 뒤 결속하며 별도 하위 worker는 만들지 않았다.
- helper의 첫 ROI 시간 차이는 애니메이션/배경까지 포함했고, 이후 source 대응도 실제 frame·반전·픽셀 중심을 놓쳐 표본이 작았다. 부모가 원본/실제 화면 수치를 대조해 교정했다. 모델을 무조건 상향하기보다 결정적인 좌표/샘플 산술을 도구로 검증했다. 추가 교정 비용도 총효율 평가에 포함해야 한다.
- 그룹 GUI 변환: 실제 피격 화면에서 기존 localFeet 행렬의 전역 적용 때문에 발 위치가 이동함을 발견했다. 순수 행렬 단위검사가 실제 그룹 GUI 좌표를 충분히 검증하지 못했다. root에 근거와 최소 범위 결정을 인계했으며 별도 미해결 관측을 마스크 개선 완료에 섞지 않는다.
- 신호: 원본 팔레트를 바꾸지 않고 파생 alpha 텍스처를 한번 준비해 재사용했다. 짧은 대상별 강조를 기본 모드에만 적용하고 lowFX 정책/판정/저장/정렬을 유지했다. 원본 texture 검증과 파생 mask 수명/UV 검증은 다른 대상이다.
- 행동 변이: 정상 컴파일/실행된 원래 강조 없는 구조가 실제 픽셀 assertion에서 실패하고 정상 앱이 통과함을 보존했다. UI/전투 상태·저장 동등을 지우거나 기대값을 완화해 통과시키지 않았다.

- GPU 대조값: 첫 alpha probe는 투명 바탕을0으로 가정해 불투명344개만 맞았다. 실제 빈 출력 대조값을 읽어 비교하도록 교정하고 실패를 보존했다. 원본 alpha·프레임/flip 기대와 제품 코드는 약화하지 않았다.48개 제어 표면 probe 전부1024픽셀 일치이며 전체 게임 전후96시점 검사와 구분한다.
- 마감: 새 suffix6개 자체 자식 exit/writer 부재와 QA3 부재 원복·PC6 보존을 확인했다.355소스와 전체 게임 리뷰 앱, 별도 UV/변이 앱을 구분 결속했다. 기존 grouped-pivot 관측은 후속 결정으로 남겨 완료 범위를 과장하지 않는다.


## 2026-11-15 발 pivot·schema2 Unity 마감

- 표시 좌표: 순수 local pivot 불변 시험만으로 GUIGroup의 실제 window 변환을 증명할 수 없었다. 기존 원점 오프셋을 표시층 한 곳에서 정정하고 실제 그룹 Draw의 발 anchor·스쿼시·frame/flip·mask GPU 출력96개로 확인했다. 정확한 옛 오프셋 분기는 컴파일 후0/342 assertion FAIL, 정상 복원37,014/37,014 PASS/maxRGB1이었다.
- 검사 간섭: 예상된 미래 저장 거절이 Development Console을 열어 reference 화면을 가렸다. 최초 투명 바탕 추정과 콘솔 setter 교정만으로 충분하지 않아 실패 앱·로그를 보존하고 저장 거절과 픽셀 단언을 별도 새 프로세스로 분리했다. 반복 빌드 비용은 실패 교정 비용이며 효율 개선으로 세지 않는다. 렌더 검사 전 예상 로그 오버레이 여부를 확인하는 도구화 후보만 제안한다.
- 저장/경계: 최신 DLL/provenance를 실제 동기화하고 schema1 무쓰기 Load→원문 archive→첫 v2 commit→재실행 동등, 미래 envelope/World/중첩 보존 거절, legacy backup 보존·archive 실패 후 같은 operation retry를 macOS Development Player에서 확인했다. Core 독립25개와 이번 앱 횟수를 합산하지 않는다. Mono 실행을 IL2CPP/모바일 지원으로 전용하지 않는다.
- identity: macOS persistentDataPath를 product 문자열 경로로 단정한 첫 wrapper는 실패했다. 무저장 probe가 보고한 bundle 경로를 suffix/realpath/보호 경계와 대조해 고쳤으며 같은 app writer 부재·정확 자식 종료 뒤 자체 QA만 원래 부재로 돌렸다. PC6를 보존하고 unknown QA/iOS는 조회하지 않았다.
- 모델/측정: 단독 구현·검증이며 helper 없이 진행했다. Orca ID와 provider session ID를 혼동한 usage 시작 조회 실패를 남겼다. 실제 CODEX_THREAD_ID를 찾아 후반 start/end를 기록했지만 초반은 미측정이며 청구액·절감률은 계산하지 않는다. 일반 수정/QA에 사용한 실제 모델은 usage ledger의 gpt-6.1-sol이며 effort는 공급자 계측이 없어 미측정이다.
- 남은 소유: root의 동일 독립 코드/UI 리뷰·GC 최종·선별 commit/push/CI가 남는다. 전역 규칙 상태는 자동 반영하지 않았고 제품 변경은 표시 변환 한 곳과 승인된 DLL/provenance 산출물에 한정했다.
