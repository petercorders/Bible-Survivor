# R06 actor sidecar

외부 `bible-survivors-docs/art-genesis-r06`의 독립 아트 QA PASS인 F1/F2/F3 move와 싯나 보스 move/windup/impact/death7PNG·25프레임 및 manifest/pixel-source/계약/재생성 스크립트만 보존한 whitelist다. private Design/blob·리뷰 보드·새 패키지는 복사하지 않았다. 기존 Art03/R02/R03/R04/R05·성경 sidecar/폰트와 GUID를 교체하지 않는다.

Unity `ArtR06/index.json`은 정확7클립 및 manifest/PNG 해시를 pin한다. F1~3은32px/PPU32/pivot16,1·fps10/7/5이며 일반 추적/접촉 배우다. 보스64px/pivot32,1·move6fps/windup0fps3프레임/impact10fps2프레임/death6fps4프레임이다. 공통 Point/FullRect/right-parentflip 규칙과 기존 small/wide/R03 boss 그림자를 사용한다.

예고는 실제 Core target·clock·공통 SlamRadius의 필수 고리로 배우/입자/무기 위·HUD 아래, lowFX에서도 유지한다. windup은 Core 예고 경과로3프레임을 선택한다. 사망은4/6초의 짧은 clip 뒤 제거하며 물우물을 영구 지형으로 남기지 않는다. 새 흙 투사체·잔류 장식·길막·피해지형·부수FX는 추가하지 않았다. 오프라인 아트QA와 실제 Unity 기술 검증의 범위는 `docs/progress/R06-unity.md`에서 구분한다.
