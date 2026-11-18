# Jawbone feedback 회고

- 원래 GUI 그룹 회귀를 다른 수식으로 대신하지 않고 기존 FacingMatrix에서 실제 FAIL(오차166.469px) 후 새 창 좌표 기준 회전으로 PASS했다. 기존 footpivot 유형의 재발이며 추가 전역 규칙 승격/수정은 하지 않는다.
- 새 Editor 시험이 Art03 자동 import 설정을 생성했다. 시작 clean baseline과 exact diff를 보존하고 해당 자동 변경만 복원한 뒤 격리 프로젝트로 옮겼다. 향후 같은 종류의 수치 회귀 시험도 처음부터 격리 Editor 사본을 사용한다.
- QA baseline 클래스의 전역 이름 치환이 shader resource 문자열까지 변경하여 Player exit2를 만들었다. resource 경로는 클래스 심벌이 아니므로 원래 문자열을 복원했다. 실패 로그를 버리지 않고 관련 실패만 재검증한다.
- 기존 Dispose는 runtime Destroy를 사용하므로 EditMode audio/particles 시험에 로그가 발생했다. 새 제품 수명 정책을 넓히지 않고 시험 소유 객체의 예상 로그만 명시했다. blanket ignore는 쓰지 않는다.
- 승인 v2 아트까지 runtime/source 시험을 병행했고 최종 binding/build는 독립 PASS 이후 진행했다. helper worker/새 모델 호출은 없었다. root의 Fable/Design 자문과 Codex PNG 제작·이 작성자의 Unity 실행 근거를 구별한다.
- 사용량은 기존 ledger에 자기 구간 start/end로 기록한다. root/외부 작업자 비용을 추정하거나 미측정을0으로 합산하지 않는다. 비용/절감률은 미계산이다. 이번 회고는 프로젝트 문서이며 harness 학습 상태를 자동 수정하지 않는다.

- 좌측 이동의 실제 화면에서 actor parent reflection 자체도 arena.x를 빠뜨려 배우가40px 옆으로 이동함을 관측했다. helper 수식 PASS만으로 손 연결을 완료로 보지 않고 같은 창 좌표 발 pivot으로 배우를 정렬했다. 이 근거 때문에 새 관련2시험과 최종 대표 장면만 재실행했다.
