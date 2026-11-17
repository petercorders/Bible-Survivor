# R10 Unity 회고

2026-11-17. Host/UI/World 파일 소유를 분리해 병행했고 독립 Core/art PASS와 두 작성자 반환을 받은 뒤 단일 통합자가 sync/Editor/Player를 실행했다. Core/수치/저장 형식/CI checker/기획/원본 아트와 타인 미반환 파일은 편집하지 않았다.

기존 Host의 선게시 경로를 R10에 복사하지 않았다. 독립 후보+고정 operation/옛 revision→Commit/readback/Resume 후 게시를 분리하고, prewrite 실패와 실제 staged 원문 publish 뒤 ack loss를 시험했다. Reload는 미커밋 취소와 커밋된 R10 재개를 구분한다. 옛 선게시 구조의 격리 변이는 실제 메모리 assertion2 FAIL→복원2 PASS다.

관련47 중1 실패는 기존 normal10 미지원 기대였다. 새 미지원 normal11로 옮겨 실패1만 다시 PASS했고 나머지46을 재사용했다. Play22에는 R10 Enter/suspend/동일 frame digit+Enter/완료 Enter 무진입을 넣었다. 반환 UI에 빠진 ex shadow를 필수 최소로 연결했다. 전 suite/픽셀/해시 행렬·새 검사기 프로젝트는 만들지 않았다.

실제 첫 앱의 모든 동작 assertion은 통과했지만 예상 IOException 주입이 Development Console을 열어 대표 화면을 가렸다. 복구 후 developerConsoleVisible=false만 넣은 두 번째 앱에서도 콘솔이 남았다. 최종 세 번째는 QA 시작부터 developerConsoleEnabled=false를 설정했다. 실제 Unity Mac managed API 존재를 확인했고 제품 로깅·raw exception evidence는 유지했다. 앞선 두 앱/화면/trace/exit와 교정 source를 보존한다. 소스/DLL 고정 중 QA-only 표시 교정이었고 정상 재빌드3회의 사유를 숨기지 않는다. 다음 QA는 예상 오류 주입 전에 QA console 표시 정책을 정해야 한다는 root GC 후보이며 전역 규칙은 수정하지 않는다.

R09 교훈을 재사용했다: 유효 phase/6슬롯/Reload pause 확인, optional pulse clock은 임의 주입하지 않고 normal cycle만 지연, 명시 Texture2D, 신선한 identity와 no-Host 실제 realpath, SaveStore3파일만 복구하고 Unity 부수 파일과 혼동하지 않음, QA 씬을 product 씬으로 복원한 뒤 새 출력에 빌드. 원본 metadata79개와 빈 플랫폼 아이콘 자동 직렬화는 Editor 종료 후 exact old bytes 일치 조건으로만 되돌렸다. 추가 빌드/검사는 이 설정 정리 때문에 실행하지 않았다.

앱 정상 성장 후보1/pending1/Choose1과 완료 pending0/Choose0을 따로 기록했다. 기술 HP10000/장비/timeout/직접 처치·drop 폐기/weapon delay를 자연 검증으로 확대하지 않는다. 설치 R10 끝과 출애굽기 전체 완료, R09 완료와 R10 명시 진입, root schema3과 provenance2/World3을 구분한다. 독립 최종 code/UI와 GC/commitpush는 root 소유다.

## Coordinator GC

독립 Core·Unity 코드·아트·최종 지정3화면 리뷰 모두 PASS다. 기존176/Core/원회귀·실앱 근거를 재사용하고 독립 담당은 전체 실행을 반복하지 않았다. 후속 메시지가 끝난 UI 작업자 대기열에 남아 생명주기 경고가 발생했으나 완료 파일 소유를 재배정하거나 중복 편집하지 않고 실제 반환을 통합자에게 전달했다. R11 외부 아트는 R10 제품 고정과 충돌하지 않아 병행했고 독립 QA도 완료했다. R10 종료는 중간 checkpoint이며 사용자 재승인 없이 R15까지 이어간다.

최종 binding/실제 Info.plist의 identity와 진행/인계 문서의 이전 identity가 달랐다. root가 실제 값을 확인해 두 문서 각1개만 바로잡았고 앱·원binding·게임 검증은 변경하지 않았다. README의 첫 편집 patch는 존재하지 않는 추가문맥 때문에 원자적으로 거절됐으며 실제 문맥만 사용해 문서 편집을 다시 적용했다. 이 문서 교정으로 앱을 재빌드하지 않았다.

CI 연결 사전 확인에서 completion resume/저장 schema 변이의 텍스트 anchor4개가 더 이상 현 소스와 일치하지 않는 것과 역사 Core에 최신 ExodusContent 의존 CatalogReader를 복사하는 문제를 발견했다. 변이 의미는 유지하면서 현 지원경계/GenesisVersion의 위치로 옮기고 역사 probe에는 같은 pinned commit의 reader를 사용하도록 도구3파일만 보완했다. 원래 회귀를 실제 검출하는지 각 도구1회 실행·독립 검토하며 제품/Core/DLL/Unity 앱은 그대로 둔다. 이것은 새 게임 실행이 아니라 커밋/원격CI를 막는 실제 검증도구 연결 보완이다.

코어 저장호환과 도입 원자성은 고위험이므로 Sol high 독립검토를 사용하고 아트·대표화면은 Sol medium으로 한정했다. Astra/Claude 호출 없음. 공급자 세션 구간 계수·지연/누락 한계를 유지하며 전체 비용/절감률을 추정하지 않는다. 프로젝트 회고만 작성하고 전역 GC 규칙/메모리를 직접 승격하지 않았다.

최종 도구 실행은 모두 exit0: 기존 회귀18/18(`evidence/r02-mutations-20261117T082004605497Z/summary.json`), schema 회귀10/10(`evidence/r03-schema-mutations-20261117T082005761534Z/summary.json`), 역사 저장30개 비교/이행/구reader거절(`evidence/r03-schema-compat-20261117T082003461715Z/summary.json`). 독립 도구 정적검토 PASS와 함께 확정했다. 커밋 훅의 Core176/CLI·portable·아트/폰트/R02 준비도 exit0였다. 이 종료 기록 문구만 amend할 때는 이미 완료한 훅을 반복하지 않는다.
