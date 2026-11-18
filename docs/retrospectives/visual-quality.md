# Visual quality 01 회고

- 계획/API/static Sprite와 manifest handshake를 먼저 맞추고, 원본은 독립3묶음 PASS 뒤 정확31PNG+3manifest만 복사했다. UI 소유 반환 전 Editor를 열지 않았다. 최대 작성자 제한·다른 작성자 소유를 지켰고 새 helper worker/모델 호출은 없었다.
- 원본 Art03 membership과 overlay alpha를 혼용하면 새 trail/shadow가 기존 계약을 깨는 것으로 오인된다. 원본 검사는 원본 import 객체로 유지하고 overlay를 별도 실제 manifest로 검증했다. 원본을 느슨하게 만들지 않았다.
- GUI의 색 곱/글자/texture 경로가 같다고 가정하면 잉크가 회색이 되거나 글자가 지나치게 어두워졌다. 실제 한 swatch 진단과 대표 화면으로 source를 조정했다. 현재 PC의 bounded float 표시 복사·명시 수명은 확인했지만 RGB exact/모바일/메모리 성능으로 확대하지 않는다.
- QA에서 game.enabled=false가 OnDisable로 입자를 해제하는 사실과 재초기화 시 owned listener 수명을 놓쳤다. 제어기는 Initialize로 자원을 재생성하고 자기 stable mixer listener를 둔다. 제품 수명 정책을 무관하게 넓히지 않았다.
- 같은 Host.Start로 무기를 바꾸려 한 QA는 정상 CanStartNew 거절 때문에 잘못된 무기를 보고 있었다. 실제 무기 inventory assert와 각 무기 독립 자기 Store/Host로 교정했다. Bible 원형 배치도 실제 공전 반경/위상을 대조하지 않아 무명중을 검출했고 실제 Core 접촉 조건만 바꿨다. 가짜 hit나 검사 문서만으로 통과시키지 않았다.
- Orca function key 미지원·workspace 모호성·종료 뒤 snapshot 실패는 도구 문제와 앱 실제 상태를 구별한다. 자기 PID에 한정한 native 대체와 application phase를 근거로 쓰며 다른 앱/사용자 저장을 찾아 우회하지 않는다.
- 최초 관련36, 색/UI delta13, 최종 관련2만 실행했다. 실패 뒤 필요한 사본 빌드는 같은 통합 앱의 QA/실제 표시 결함 교정이며 작은 기능별 배포가 아니다. 이미 확인한 native와 턱뼈 회귀는 동일 경로에서 재사용했다. 불필요한 과거 전체 suite/픽셀 행렬/문서용 앱 재빌드는 하지 않았다.
- 이 문서는 프로젝트 회고이며 전역 학습 상태·훅·CI를 자동 변경하지 않는다. 자기 usage start/end는 기존 도구의 실제 Sol telemetry를 전달하고 root/Fable/독립QA의 미측정을0으로 합산하지 않는다. 비용/절감률은 미계산이다.

- Coordinator의 형제 dispatch 직접 통신 가정은 권한 거절로 드러나 root 중계로 고쳤다. static/instance API 불일치는 구현 전 static Sprite 계약으로 정리했다. 메시지 전달 성공을 실제 구현 시작으로 세지 않고 첫 편집과 인계를 구분했다.
- Coordinator의 문서 패치에서 실제로 없는 문맥을 사용한 두 시도는 차단되어 파일 변경이 없었다. 실제 문서를 읽고 존재 확인 뒤 append로 고쳤다. 후속 도구화 후보는 문맥 일치 검증이며 전역 규칙에는 자동 반영하지 않는다.
- Fable5.2는 실제 Design 방향과 세 픽셀 원본 제작, Sol은 UI/월드 통합과 독립 리뷰에 사용했다. root는 배정·계약 중계·최종 커밋/CI를 맡았으며 root의 작업 시작 사용량 기준점은 복구 문맥에서 확인되지 않아 미측정이다. 세션 구간을 작업별 청구로 합산하거나 절감률로 발표하지 않는다. 다음 비교는 같은 수용 조건에서 디자인/제작/통합별 소요와 실패 재작업을 함께 측정해야 한다.
