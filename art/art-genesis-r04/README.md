# R04 actor sidecar

독립 아트 QA PASS인 외부 `bible-survivors-docs/art-genesis-r04`의 승인된 5 PNG/16프레임과 manifest·pixel-source·계약·재생성 스크립트만 보존한다. 기존 Art03/R02/R03/성경 sidecar는 교체하지 않는다. Unity는 `ArtR04/index.json`의 정확 5클립과 pinned manifest/PNG 해시를 검증한다.

`src/telegraph_r04.json`과 외부 보드는 제작 당시 참조 계약이다. 실제 통합 권위는 `docs/harness/r04-implementation-01.md`의 Unity 인계 절이다: 정적 lane/arena clamp/배우 아래 배치를 채택하지 않는다. 경고 캡슐은 같은 Core snapshot의 현재 actor 위치·고정 방향·실제 Tuning과 남은 돌진시간으로 계산하며, 넉백 뒤에도 경로를 따라간다. lowFX에서도 배우·무기·입자 위/HUD 아래에 남는다. 새 판정·피해는 없다.

외부 아트 QA는 오프라인 자산 범위이며, Unity 화면·저장/재개 근거와 한계는 `docs/progress/R04-unity.md`에 구분한다.
