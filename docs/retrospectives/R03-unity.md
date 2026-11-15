# R03 Unity 통합 회고

2026-11-16 · 구현자 회고, root 최종 리뷰/GC 대기. 전역 학습 상태는 수정하지 않았다.

- 단일 수직 slice: 최신 sync부터 R03/보상/진화/정산/재개까지 한 PC 앱으로 묶었다. 기존 Core138/변이/대표 series와 성경 UI PASS를 재사용하고 새 작은 개선·helper·별도 단계 빌드·전조합 검증을 추가하지 않았다. 고정 소스 최종 Unity Edit97/Play31을 한 번씩 실행했다.
- 소유: Core는 지급/후보/진화/성장/회복/정산의 유일한 작성자다. Unity는 ID를 전달하고 저장 실패 시 기존 frozen operation을 보존한다. 보상/완료 실패·중복·남은 성장과 boss World 재개를 관련 시험으로 확인했다. catalog는 동일 Core 계약만 받아들이고 R04는 명시 대기다.
- 위험 정보: 배우와 같은 projection으로 Core target/radius를 사용하고, 장식이 아닌 필수 경계를 배우·입자·무기 위/HUD 아래에 뒀다. lowFX와 무음에도 유지하며 optional 금 데칼과 경고 참조 PNG는 통합하지 않았다. 원본 RGBA/색공간은 바꾸지 않았다.
- QA 재사용: 앞 작업의 EnhancedTouch/중복 driver 교정 경험을 반영해 이번에는 키보드 fixture를 추가하지 않고 실제 Host·SelectSlot 경로를 사용했다. 원본 씬부터 시작하고 duplicate guard가 있는 새 QA 프로젝트로 단일 빌드/연속 trace/3화면/재실행을 통과했다. HP10000·장비8·R1/R2 timeout은 명시 기술 준비이고 R03 피해/처치 주입은 없었다. 자연 밸런스/native/device/performance를 PASS로 세지 않는다.
- 모델/비용: 상속 Sol 기본, provider gpt-6.1-sol 자기 start/end만 기록했다. 추가 Astra/worker/유료 도구 없이 진행했다. 실제 비용/절감률/effort는 계측이 없으므로 미측정이다. 더 큰 모델을 쓰지 않은 사실을 품질/비용 절감률로 환산하지 않는다.
- 보존: 기존 preview/unknown/iOS는 조회하지 않고 독점 새 identity와 자신의 정확 child만 관리했다. Unity가 추가한 기존 import 비override platform block/빈 icon만 원문 baseline으로 복구했다. Core 소스·기존 아트·폰트·패키지와 다른 담당자 변경을 보존했다. 최종 독립 리뷰/GC/커밋·업로드는 root가 맡는다.

## Coordinator GC 정리

사용자의 “개발 속도가 너무 느린 것 같다”와 반복 대조 지적을 근거로 기존 프로젝트 검증 효율 기준을 적용했다. Core138/최소3변이·성경 관련2시험/3화면을 재사용하고, Unity 단일 작성자가 sync부터 보스/보상/진화/정산/재개를 단일 앱·대표3화면으로 인계했다. UI 독립 리뷰는 지정3화면 범위 PASS이며 전체 픽셀/해시/Editor 실행을 반복하지 않았다. 인계 send 성공을 실제 시작과 혼동하지 않고 작성자의 sync/파일 변경/실행 보고를 시작 근거로 구분했다. 다만 상태 전달 전용 후속 메시지가 이미 끝난 보조 worker의 큐 알림을 만들었으므로, 제품 진행에 필요 없는 전달·재개는 중단한다.

모델 적합성은 일반 구현 Sol·독립 코드 리뷰 Sol high·지정 아트 담당의 별도 제작으로 구분했다. 추가 Astra 호출이나 경량 helper를 늘리지 않았다. 관측 가능한 provider별 usage는 ignored 근거에 보존하고, 캐시 포함 총량·reasoning 중복·Claude/Codex 계수 차이를 고려해 프로젝트 전체 합산/비용·절감률을 추정하지 않는다. 개선 여부의 동일 조건 비교는 아직 없다. 향후 개선은 이미 채택한 단일 수직 통합·직접 실행 근거·변경 관련 검사 재사용을 유지하는 것으로 한정한다. 새 전역 학습 엔트리·규칙 승격·도구 설치는 하지 않았다.
