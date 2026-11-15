# R03 아트 초안 검수 상태

2026-11-15. 외부 `art-genesis-r03/`는 production draft이며 최종 Unity 임포트 전이다. C1/C2/C3·보스 move/windup/impact/death·그림자·예고 참조/금 데칼 PNG10개, 대표 보드5개·GIF2개를 제작자가 인계했다. idle은 새 시트 없이 move0번 고정으로 제안했다. 게임 픽셀은 스크립트 출력이며 Design Artifact 캔버스는 검토 호스팅 경로다. 원격 캔버스 렌더는 미확인이고 private URL/blob은 공개 통합하지 않는다.

root는 적3종과 slam 대표 보드를 보았다. 어두운 연기/밝은 사족/목재를 두른 수직 형체가 다르고 보스의 곤봉 자세 변화도 보인다. 정지 시안만으로 실제 움직임·Unity 색공간·군집 가독성 PASS를 판정하지 않는다.

독립 QA는 대표 혼합/흑백/동작과 필수 프레임·pivot·fps·alpha·참조/반경 계약만 검토한다. 정상 재생성1회·필요한 대표 거절만 허용하며, 전 픽셀/프레임/방향/해상도 행렬과 대량 검사기 변이는 추가하지 않는다. 보고서는 외부 `art-readiness-reviews/r03-01-review.md`/`r03-01-gc.md`다. 현재 독립 판정 대기다.

독립 결과: 대표 아트·필수 인계 PASS, 막는 HIGH/MEDIUM 없음. QA는 temp 정상 검사/재생성1회와 fps·source중복 대표2오류의 명시 거절만 확인했다. idle의 move0 고정 대체를 수용했고 optional 금 데칼·군집 고리 가림은 통합 판단으로 분리했다. 원격 Design·Unity 투영/HP/피격/P1·기기/성능은 아직 NOT_RUN이다. 기존 아트나 QA 원본을 바꾸지 않고, 다음 Unity 작성자에게 이 입력을 인계한다.

Unity 연결 시 예고 경계는 GroundDecal 장식과 별도 위험 신호로 취급한다. Core 고정 target/radius를 공유 WorldProjection으로 투영하고, 군집·입자에 경계가 가려지지 않는 표시 순서를 대표 실제 화면에서 정한다. 원본 PPU32의38.4px을 화면 픽셀로 고정하지 않는다. 그림 크기가 충돌·공격 반경을 바꾸지 않는다. 홍수는 사탄 공격으로 사용하지 않는다.

현재 Core 보스 slice도 별도 독립 리뷰 중이며 정식 보상/진화/정산과 Unity R03 연결은 후속이다. 기존 R02/Art03/font·사용자 앱/저장은 그대로 유지한다.
