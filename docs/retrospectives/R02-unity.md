# R02 Unity 구현 회고

2026-11-14. 구현자 기록이며 독립 리뷰·GC 적용·커밋은 coordinator가 담당한다. 전역 학습 entries/index/overview는 수정하지 않았다.

- 판단/fixture 교정: 완료 between 상태의 HP를 임의로 낮춘 시험은 Core의 완료 시 최대HP 불변식 때문에 실패했다. 그 불변식을 유지하고 진입 후 실제 피해로 피격 시계 초기화를 확인했다. fingerprint 없는 R02는 Core schema 단계에서 거절되므로 정상 Commit 성공을 기대하지 않고 독립 raw-invalid fixture로 거절을 확인했다.
- 렌더러 수명: component를 disable하면 입자 자원이 해제된다. 결정적 UI 시험은 disable 후 Initialize로 자원을 다시 준비해야 했다. 실제 component 입력 시험에서는 enabled를 유지했다.
- 전투 fixture: 성경 궤도 안으로 들어온 적은 궤도 바깥 접촉을 재현하지 못해 처치가 없었다. 부상 적을 현재 궤도 접촉점에 명시 배치해 명중/처치/경험 회수를 시험하고, 전체 체력 자연 전투는 별도 봇으로 기록한다.
- QA 첫 앱의 staged 화면은 Core Session만 앞당긴 뒤 옛 World를 checkpoint해 거절됐다. 세션·월드·드롭을 동일 경계로 만드는 fixture를 구성했다. 실패 로그/exit2와 원래 앱은 보존한다. 새 출력·새 suffix·첫 무저장 probe 후 실행하며, QA3 원복 전에 실제 자식 종료와 같은 새 identity의 다른 writer 부재를 확인한다.
- 도구화 후보: 향후 라운드 fixture 생성 helper에 session/wave/world clock·적/드롭 집합 동일성 assertion을 함께 둔다. 현재 CI/checker/hook은 변경하지 않는다.
- 승인/외부 의존: 아트 제작자의 verify 성공은 독립 QA가 아니다. root 승인 전 신규 R02 시트를 임포트하지 않았고 개발용 기하 표식으로 기술 연결을 분리했다. pivot 후보16,4를 최종 계약16,1로 정정했다.
- 모델/토큰: 지정 구현 worker 1명, 추가 worker 없음. 실제 usage/effort·input/output/cache/reasoning 토큰 미측정이며 절감률은 추정하지 않는다. 재실행은 실패 fixture 수정과 새 회귀/최종 결속 때문에 수행한다. 비교 가능한 단일 기준이 없어 효율 향상을 주장하지 않는다.

최종 compile/66Edit/26Play, 다섯 원래 구조 변이 assertion FAIL→원복 PASS와 새 앱 자동 캡처/실제 프로세스 Load0을 확인했다. 보호 원본222개/PC6 보존·자체 QA3 원복 및 프로세스 종료를 확인했다. 남은 독립 PC 코드/UI 리뷰·최종 아트·실물/성능·R03는 PASS로 합산하지 않는다.


재빌드 실패의 원인은 재사용 QA 씬의 캡처 driver2개였다. 같은 PID 안의 자체 중복 writer였으며 unknown 옛 QA writer라고 추정하지 않았다. 새 씬 사용과 기존 driver 거절을 추가하고 새 identity/출력의 캡처·Load를 성공시켰다. 실패 앱/명령/exit2는 별도 경계에 보존했다.

네이티브 도구의 background synthetic 키·클릭은 화면/월드 시간 변화가 없어 입력 수용을 NOT_RUN으로 남겼다. 종료 명령의 screenshot:null을 QA parser가 처리하지 못해, 정확한 자기 자식에만 종료 fallback을 요청하고 실제 exit0을 기록했다. 도구화 후보는 screenshot 생략 결과의 nullable 필드와 종료 후 관측을 분리하는 parser다. 재실행/restore-window를 반복하지 않았고 사용자 iOS/옛 QA는 조회·조작하지 않았다. 이 제약을 제품 입력 PASS나 정상 native quit 수용으로 바꾸지 않는다.

R02 on/off 동등 비교는 Run/Profile/World를 비교하고 저장 operation ID 차이를 제외한다고 명시한다. 성경·턱뼈 봇의0처치는 숨기지 않으며, HP1 접촉 fixture의 처치/성장과 구분한다. 구현된 기술 연결과 미승인 최종 아트/자연 수동·밸런스·실물·성능/R03 게이트를 분리한다.

통합 검토 기록: 독립 코드 검토는 최종331소스/334앱·보호222파일·QA Runtime/시험27파일·현Host/Game의 변이 복원SHA와 실제 새 프로세스 Run/Profile 복구를 대조했다. 완료 선택/저장→단일 진입·실패 freeze·같은 operation retry·R03 대기를 Core와 분리된 Host 책임으로 확인한다. 독립 UI는 반복적인 native focus 루프 대신 실DrawGameView 캡처9장과66Edit/26Play·저장 복구 자료를 직접 검토하며 실제 OS 입력을 실행한 것으로 세지 않는다. 최종 아트는 QA 검사기 수정 루프 중이므로 명시 기하표식 기술 checkpoint와 시각 완성을 분리한다. 모델별 usage/효율 비교는 미측정이며 필요한 경로만 재실행하고 실제 확인 범위를 기록한다.

통합 마감: 독립 코드/제공 화면·자동 입력 근거의 UI 리뷰는 기술checkpoint 범위에서 추가 HIGH/MEDIUM 없이 종료했다. R01 미처리 선택 전 화면은 캡처에 없고, 봇3장 모두 R03 대기·geometric1100은 정지 모달이라는 실제 이미지 범위를 명시했다. P1의 도구 focus 실패를 반복하지 않고 native는 미실행으로 남겨 불필요한 재시도와 사용자 저장 접촉을 피했다. 최종 아트 검사기 결함은 별도 제작/QA 반복으로 해결하며 이 코드 checkpoint가 아트 FAIL을 닫지 않는다. 다음 보스 단계는 독립 Core 계획을 준비하되 R02 최종 시각 수용을 완료한 것으로 이월하지 않는다.
