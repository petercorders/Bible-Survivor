# R01 모바일 iOS 선행 회고

## 통합 GC 결과

독립 코드·UI 리뷰 모두 PASS, 추가 HIGH/MEDIUM 없음. 검토 문서는 `../reviews/mobile-ios-code.md`, `../reviews/mobile-ios-ui.md`다. 코드는277파일과 설치 앱36파일 해시, UI는 본인의 Simulator 실행 및 저장9개 보존/복원을 확인했다. 실제 IL2CPP 두 타입 제거 변이와 고DPI renderer 클릭 회귀를 유지하며 전역 학습 상태는 수정하지 않았다.

보고 정정: 사용자 전시 화면을 재개 모달로 예상했으나 실제 정상 저장에는 레벨업 선택이 남아 있었다. 실제 화면에 맞춰 전시 인계·진행 문서를 정정했다. 사용자 플레이가 시작된 이후에는 검수 전 저장으로 자동 원복하지 않는다. 기기/앱을 열 때 구현자와 root의 boot 요청이 겹쳐 root 요청은 already Booted(exit149)였으며 추가 재시작 없이 정상 실행을 확인했다. 소유자 단일 실행과 전시 인계를 후속 도구화 후보로 남긴다.

모델·토큰·위임 회고: 구현과 독립 코드/GUI 리뷰를 분리해 같은 소스의 검증·실제 UI 관측을 교차 확인했다. 실제 모델별 총 token/cache/reasoning·비용은 미측정이며 절감률을 주장하지 않는다. 전체 빌드를 리뷰어가 반복하지 않고 최종 SHA와 기존 실패/원복 증거를 대조했으며, UI는 직접 조작이 필요한 경로만 실행했다. 추가 모델 변경이나 전역 규칙 승격은 적용하지 않았다. 선택적 QA UUID/창/저장 소유를 묶는 실행 도구는 후보이고, 잘못된 기기 대상·중복 실행을 거절하는 시험을 마련한 뒤 별도로 구현한다.

2026-11-14, 구현 worker 초안. 전용 harness-run-bible-survivor의 실행·검증·retro 절차를 적용하며 전역 학습 entries/index/overview는 변경하지 않는다. 독립 코드/UI 리뷰·통합 GC·선별 commit/push는 coordinator 소유다.

## 관측과 교정

- Hub CLI 실제 help·동일 Editor metadata·공간을 먼저 확인하고 무료 iOS 모듈만 추가했다. 기존 SDK/계정·pairing/다른 Simulator는 수정하지 않았다. 설치/내보내기/네이티브 앱 시작을 저장 성공으로 합산하지 않았다.
- 처음 Simulator create가 device-set 디렉터리 부재로 실패했다. 디렉터리를 만든 뒤 같은 생성 명령이 성공했다. 새 Editor 코드의 잘못된 architecture enum도 최초 컴파일 실패로 기록하고 설치 API의 실제 enum으로 교정했다. 실패 명령/exit를 지우지 않았다.
- custom device set에서 build/install/render는 성공했지만 GUI 단일 프로세스가 새 custom set을 열지 않았다. 기존 Simulator를 종료하거나 비공개 HID API로 우회하지 않고 root가 승인한 default set 새 QA UUID와 전용 창으로 전환하자 기존 기기/창을 유지하며 native 터치가 가능했다. 프로세스 분리와 앱/기기/컨테이너 격리는 다른 조건이다.
- 실제 IL2CPP에서 최초 저장이 실패했다. 축약된 화면 문구만으로 AOT 원인을 단정하지 않고 개발 빌드 전용 예외 전체 stack을 남겼다. 첫 MissingMethodException은 serializer의 GenericDictionaryEnumerator 생성자, 다음 Decode 오류는 KeyValue의 reflection 멤버였다. 두 내부 타입만 linker로 보존하며 Core/저장 형식을 그대로 유지했다. 각각을 제거한 별도 실제 빌드는 컴파일 성공 후 실제 Commit이 쓰기/읽기에서 실패했다. 정상 원복은 최초/반복 저장·SIGKILL 후 장비/성장/좌표 재개로 검증했다.
- 새 iOS 모듈이 canonical PNG79개에 기본 overridden:0 플랫폼 블록과 빈 iPhone 아이콘 슬롯을 자동 직렬화했다. GUID/rect/pivot/border 변경으로 오인하지 않도록 원본 diff를 보존하고, 본인 자동 추가분만 제거했을 때 HEAD와 동일함을 검사한다. 의미 있는 모바일 설정은 QA export 진입점으로 분리한다.
- 실제 portrait의 PC24px/50px 버튼은 작았다. 기존 검수 PNG/font를 다시 제작하지 않고 정수 배율·safeArea·66logicalpx 버튼·실제71개 성장 문자열의 두 줄 카드로 최소 적응했다. 이 QA phone profile의3x 기준44point와 실기기 가독성·모든 작은폰 지원을 구분한다.
- synthetic native 도구 성공만으로 전달을 판정하지 않았다. 실제 QA 저장/새 화면/RunId·위치·레벨·골드와 연결했고, 자연 LV2 도중 이동이 멈춘 경우에는 카드 대기 정지가 원인이므로 이동 성공으로 세지 않았다. 회전 중간의 뒤집힌 자세는 upright portrait 수용 결과와 구분한다.

## 유지할 시험과 제안

좁은 linker 두 타입 제거의 실제 iOS 변이와 고DPI 시작/재개 hit rect의 실제 GUI 회귀를 버전 갱신 시 다시 실행한다. 내부 BCL 타입 이름에 의존하므로 Unity/Mono/IL2CPP 또는 stripping 변경 때는 현재 PASS를 이월하지 않는다. 같은 엔진/백엔드라도 실제 기기 파일 시스템·폰트/셰이더·멀티터치·전원 손실·장기 발열은 별도 게이트다.

도구화 후보는 .adam-mobile-qa marker/새 출력·동일 앱ID/명시 QA UUID/창ID/원본 hash를 묶은 재현 패킷이다. 판단 누락을 경고문만 늘리는 대신 실제 Commit→재Commit→Load·백업/미래 schema 거절·원래 입력 우회 행동 회귀로 검출한다. 새로운 전역 규칙 자동 승격은 제안하지 않았다. 추가 worker는 사용자 금지에 따라0이며 모델별 비교/전체 token·비용/cache/reasoning 사용량은 미측정이다.

최종 정상 iOS export/Xcode·PC compile/42Edit/15Play/macOS 빌드는 모두exit0이다. 최종 소스/설정/설치 앱36파일의 동일 SHA를 결속하고, 본인 자동 meta79/빈 아이콘 슬롯만 원복했으며 기존11 Simulator 상태/GUI와 PC 저장6개를 보존했다. native 사망 영상은 포즈→종료 모달을 확인했으나 클릭 왕복이1.8초여서0.75초 내 도착 가정의 probe가 실패했다. 이는 NOT_TIMED로 정정하고 정확 경계는 InputSystem/마우스 회귀 범위로 한정했다. arm64 codesign 조회가0이어서 unsigned 바이트 부재라는 검사 가정도 버렸다: 실제 링커 ad-hoc/Team 없음이며 계정·개발 서명을 쓰지 않은 Simulator 빌드라고 정확히 기록했다. 최종 근거/남은 게이트는 docs/progress/mobile-ios.md다.

후속 공식 click-count2 단일 요청은 사망 접촉 재개와 재시작 버튼이 겹치는 위치의 연속 탭 뒤 Run null/골드를 유지했고, 새 클릭은 정상 시작했다. 개별 touch 전달 시각은 여전히 NOT_TIMED로 두었다. 두 번째 확인 후에도 본인 QA 앱/기기만 다시 종료하고 원래 Simulator/PC 저장과 최종 소스·앱 해시를 재확인했다.
