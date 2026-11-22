# 공통 영구 노드 120종

가격 칸의 항목 수가 최대 랭크다. 첫 랭크부터 표시 순서대로 구매한다. `id@N`은 랭크 N 이상, Rnn은 해당 라운드 영구 정산 완료를 뜻한다. ‘—’는 조건 없음이다. 도전 사본에 있는 효과는 해당 능력치를 쓰는 모든 무기·형태 또는 플레이어에 적용한다. 행의 예외 조건과 [계산·발동 계약](../skill-tree-mvp-design.md#합산과-발동)을 함께 적용한다.

## 위력

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `attack` | 공격력 | 100 / 200 / 350 / 550 / 800 | — | — | 공격 피해 +3%p/단계 |
| `common.power.02` | 큰 상대 | 220 | attack@1 | — | 정예·보스 원본 피해 +5%p |
| `common.power.03` | 첫 공격 | 320 | common.power.02@1 | R03 | 주기 첫 원본 타격 피해 +6%p |
| `common.power.04` | 마무리 | 450 | common.power.03@1 | R09 | 대상 HP ≤25% 때 원본 피해 +6%p |
| `common.power.05` | 온전한 힘 | 600 | common.power.04@1 | R15 | 내 HP ≥90% 때 피해 +5%p |
| `common.power.06` | 다수 상대 | 780 | common.power.05@1 | R30 | 같은 원본 타격에서 세 번째 이후 대상 피해 +6%p |
| `common.power.07` | 맞서는 힘 | 980 | common.power.06@1 | R60 | 피격 후 3초간 원본 피해 +7%p |
| `common.power.08` | 신중한 일격 | 1200 | common.power.07@1 | R90 | 대상과 거리 ≥4 때 원본 피해 +5%p |
| `common.power.09` | 집중 | 1450 | common.power.08@1 | R120 | 같은 적 원본 연속 3회 적중 뒤 3초간 대상 피해 +7%p |
| `common.power.10` | 결전 | 1750 | common.power.09@1 | R150 | 보스 본체 HP ≤25% 때 원본 피해 +10%p |

## 치명타

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `common.crit.01` | 날카로운 눈 | 150 | attack@1 | — | 원본 치명 확률 +3%p |
| `common.crit.02` | 깊은 타격 | 220 | common.crit.01@1 | — | 치명 배율 +10%p |
| `common.crit.03` | 첫 기회 | 320 | common.crit.02@1 | R03 | 주기 첫 원본 치명 확률 +5%p |
| `common.crit.04` | 큰 표적 | 450 | common.crit.03@1 | R09 | 정예·보스 치명 확률 +4%p |
| `common.crit.05` | 정확한 마무리 | 600 | common.crit.04@1 | R15 | 대상 HP ≤25% 때 치명 배율 +15%p |
| `common.crit.06` | 평정 | 780 | common.crit.05@1 | R30 | 내 HP ≥90% 때 치명 확률 +4%p |
| `common.crit.07` | 측면의 기회 | 980 | common.crit.06@1 | R60 | 대상 거리 ≥4 때 치명 확률 +4%p |
| `common.crit.08` | 신중한 조준 | 1200 | common.crit.07@1 | R90 | 무이동 1초 뒤 이동 전까지 치명 확률 +5%p |
| `common.crit.09` | 흔들린 적 | 1450 | common.crit.08@1 | R120 | 같은 적 원본 연속 3회 적중 뒤 3초간 치명 배율 +15%p |
| `common.crit.10` | 결정타 | 1750 | common.crit.09@1 | R150 | 보스 HP ≤25% 때 치명 확률 +8%p |

## 연사

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `cooldown` | 재사용 대기시간 | 500 / 1000 / 1800 | attack@3 | — | 공격 주기 대기부 -2%p/단계 |
| `common.tempo.02` | 빠른 준비 | 220 | cooldown@1 | — | 라운드의 첫 공격 직후 다음 개시를 기다리는 대기부 -15%p |
| `common.tempo.03` | 재정비 | 320 | common.tempo.02@1 | R03 | 접촉 아닌 실제 피격 포함 모든 피해 후 3초간 주기 대기부 -3%p |
| `common.tempo.04` | 첫 호흡 | 450 | common.tempo.03@1 | R09 | 라운드 전투 시작부터 10초간 주기 대기부 -4%p |
| `common.tempo.05` | 꾸준한 공격 | 600 | common.tempo.04@1 | R15 | 고정 타격 간격 -4%p |
| `common.tempo.06` | 전투 적응 | 780 | common.tempo.05@1 | R30 | 같은 라운드 전투 60초부터 주기 대기부 -4%p |
| `common.tempo.07` | 안정된 손 | 980 | common.tempo.06@1 | R60 | 같은 도전 전투 10초 무피격 때 주기 대기부 -4%p |
| `common.tempo.08` | 돌아온 기회 | 1200 | common.tempo.07@1 | R90 | 정예 처치 후 3초간 주기 대기부 -5%p |
| `common.tempo.09` | 연속 공격 | 1450 | common.tempo.08@1 | R120 | 연사 내 간격 -8%p |
| `common.tempo.10` | 결전의 속도 | 1750 | common.tempo.09@1 | R150 | 보스 본체 활성 중 주기 대기부 -5%p |

## 범위

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `common.area.01` | 넓은 공격 | 150 | attack@1 | — | 타격 반경·폭·도달 +4%p |
| `common.area.02` | 가까운 방어선 | 220 | common.area.01@1 | — | 근접 휘두르기·외침 반각 +4° |
| `common.area.03` | 긴 사거리 | 320 | common.area.02@1 | R03 | 이동 발사체 최대 편도 이동 거리 +5%p |
| `common.area.04` | 넓은 낙점 | 450 | common.area.03@1 | R09 | 지정 위치 원형 타격 반경 +5%p |
| `common.area.05` | 단단한 궤도 | 600 | common.area.04@1 | R15 | 회전 궤도 반경 +5%p |
| `common.area.06` | 바깥 타격 | 780 | common.area.05@1 | R30 | 발사체 충돌 반경 +5%p |
| `common.area.07` | 생존 거리 | 980 | common.area.06@1 | R60 | 내 HP ≤30% 때 타격 반경·폭·도달 +6%p |
| `common.area.08` | 포위 대응 | 1200 | common.area.07@1 | R90 | 반경 3에 적 ≥6일 때 타격 반경·폭·도달 +6%p |
| `common.area.09` | 넓은 여운 | 1450 | common.area.08@1 | R120 | 지역 피해 반경·폭 +6%p |
| `common.area.10` | 넓은 진화 | 1750 | common.area.09@1 | R150 | 진화형 타격 반경·폭·도달 +7%p |

## 지속

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `common.duration.01` | 긴 유지 | 150 | cooldown@1 | — | 기본 지속 +5%p |
| `common.duration.02` | 잔존 | 220 | common.duration.01@1 | — | 잔류 구역 지속 +5%p |
| `common.duration.03` | 버티는 효과 | 320 | common.duration.02@1 | R03 | 내 HP ≤30% 때 생성한 원본 지속 +6%p |
| `common.duration.04` | 넓게 남은 흔적 | 450 | common.duration.03@1 | R09 | 진화 잔류 구역 지속 +6%p |
| `common.duration.05` | 첫 유지 | 600 | common.duration.04@1 | R15 | 라운드 최초 지속 공격 지속 +8%p |
| `common.duration.06` | 유지 연장 | 780 | common.duration.05@1 | R30 | 해당 원본 개체가 일반 적 8명 처치 시 0.25초 연장, 개체당 1회 |
| `common.duration.07` | 정예의 여운 | 980 | common.duration.06@1 | R60 | 해당 원본 개체의 첫 정예 처치 시 0.30초 연장, 개체당 1회 |
| `common.duration.08` | 재생되는 자리 | 1200 | common.duration.07@1 | R90 | 반경 3의 적 ≥6인 지점에 생성한 원본 지역 지속 +6%p |
| `common.duration.09` | 긴 이동 | 1450 | common.duration.08@1 | R120 | 이동 발사체의 최대 생존 시간 +8%p, 고정 도달 거리 우선 |
| `common.duration.10` | 오래 가는 진화 | 1750 | common.duration.09@1 | R150 | 진화 원본 지속 +8%p |

## 투사체

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `common.shot.01` | 빠른 비행 | 150 | attack@1 | — | 이동 발사체 속력 +5%p |
| `common.shot.02` | 단단한 발사체 | 220 | common.shot.01@1 | — | 이동 발사체 충돌 반경 +4%p |
| `common.shot.03` | 깊은 관통 | 320 | common.shot.02@1 | R03 | 유한 관통 발사체 적중 예산 +1 |
| `common.shot.04` | 곧은 궤적 | 450 | common.shot.03@1 | R09 | 플레이어 출발 이동 발사체 목표 방향과 조준 방향의 각도 오차 ×0.90 |
| `common.shot.05` | 첫 비행 | 600 | common.shot.04@1 | R15 | 주기 첫 이동 발사체 속력 +8%p |
| `common.shot.06` | 연사 배열 | 780 | common.shot.05@1 | R30 | 같은 주기의 후속 발사체 충돌 반경 +6%p |
| `common.shot.07` | 막힌 길 | 980 | common.shot.06@1 | R60 | 유한 관통 발사체 마지막 적중의 피해 +7%p |
| `common.shot.08` | 먼 적중 | 1200 | common.shot.07@1 | R90 | 이동 거리 ≥4인 원본 발사체 피해 +6%p |
| `common.shot.09` | 이어지는 궤적 | 1450 | common.shot.08@1 | R120 | 진화 유한 관통 발사체 적중 예산 +1 |
| `common.shot.10` | 빠른 귀로 | 1750 | common.shot.09@1 | R150 | 귀환 발사체 속력 +10%p |

## 체력

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `health` | 최대 체력 | 80 / 160 / 280 / 440 / 640 | — | — | 최대 HP +10%p/단계 |
| `common.health.02` | 튼튼한 시작 | 220 | health@1 | — | 최대 HP +4%p |
| `common.health.03` | 생존의 힘 | 320 | common.health.02@1 | R03 | 최대 HP +4%p |
| `common.health.04` | 든든한 기반 | 450 | common.health.03@1 | R09 | 최대 HP +5%p |
| `common.health.05` | 참을성 | 600 | common.health.04@1 | R15 | 최대 HP +5%p |
| `common.health.06` | 버틸 여력 | 780 | common.health.05@1 | R30 | 최대 HP +5%p |
| `common.health.07` | 강인한 몸 | 980 | common.health.06@1 | R60 | 최대 HP +6%p |
| `common.health.08` | 두터운 체력 | 1200 | common.health.07@1 | R90 | 최대 HP +6%p |
| `common.health.09` | 마지막 여력 | 1450 | common.health.08@1 | R120 | 최대 HP +7%p |
| `common.health.10` | 긴 여정 | 1750 | common.health.09@1 | R150 | 최대 HP +8%p |

## 방어

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `armor` | 방어력 | 250 / 600 / 1100 | health@2 | — | 방어 +1/단계 |
| `common.armor.02` | 단단한 걸음 | 220 | armor@1 | — | 접촉 피해에 방어 +1 |
| `common.armor.03` | 막아 내기 | 320 | common.armor.02@1 | R03 | 적 투사체에 방어 +1 |
| `common.armor.04` | 위기의 방패 | 450 | common.armor.03@1 | R09 | 피격 전 내 HP ≤30%면 방어 +1 |
| `common.armor.05` | 한숨 돌리기 | 600 | common.armor.04@1 | R15 | 8초 무피격 후 첫 피격 방어 +2 |
| `common.armor.06` | 큰 상대 방패 | 780 | common.armor.05@1 | R30 | 정예·보스 피해에 방어 +1 |
| `common.armor.07` | 포위 방비 | 980 | common.armor.06@1 | R60 | 반경 3에 적 ≥6이면 방어 +1 |
| `common.armor.08` | 두터운 보호 | 1200 | common.armor.07@1 | R90 | 방어 +1 |
| `common.armor.09` | 전열 유지 | 1450 | common.armor.08@1 | R120 | HP ≥90% 때 방어 +1 |
| `common.armor.10` | 마지막 방비 | 1750 | common.armor.09@1 | R150 | 보스 본체 활성 중 방어 +2 |

## 회복

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `regen` | 체력 회복 | 200 / 450 / 800 | armor@1 | — | 초당 HP +0.1/단계 |
| `common.regen.02` | 숨 돌리기 | 220 | regen@1 | — | 8초 무피격 상태에서 초당 HP +0.1 |
| `common.regen.03` | 상처 회복 | 320 | common.regen.02@1 | R03 | HP ≤30%에서 초당 HP +0.1 |
| `common.regen.04` | 차분한 휴식 | 450 | common.regen.03@1 | R09 | 1초 무이동부터 초당 HP +0.1 |
| `common.regen.05` | 첫 안도 | 600 | common.regen.04@1 | R15 | 라운드 첫 일반 적 10처치에 HP 1 즉시 회복, 라운드당 1회 |
| `common.regen.06` | 버틴 보상 | 780 | common.regen.05@1 | R30 | 정예 원본 처치 시 HP 1 회복, 5초 대기 |
| `common.regen.07` | 꾸준한 회복 | 980 | common.regen.06@1 | R60 | 초당 HP +0.1 |
| `common.regen.08` | 위기 탈출 | 1200 | common.regen.07@1 | R90 | 실제 피격으로 HP가 25% 이하로 처음 내려갈 때 생존 시 HP 2 회복, 12초 대기 |
| `common.regen.09` | 전투의 쉼 | 1450 | common.regen.08@1 | R120 | 보스 소환 적 원본 처치 시 HP 1 회복, 5초 대기 |
| `common.regen.10` | 되찾는 힘 | 1750 | common.regen.09@1 | R150 | 초당 HP +0.2 |

## 기동

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `move` | 이동속도 | 100 / 250 / 450 | pickup@2 | — | 이동 속도 +5%p/단계 |
| `common.move.02` | 가벼운 걸음 | 220 | move@1 | — | 이동 속도 +2%p |
| `common.move.03` | 안정된 걸음 | 320 | common.move.02@1 | R03 | 8초 무피격 상태에서 이동 속도 +3%p |
| `common.move.04` | 회피 보폭 | 450 | common.move.03@1 | R09 | 실제 피격 후 2초간 이동 속도 +4%p |
| `common.move.05` | 거리를 두는 발 | 600 | common.move.04@1 | R15 | 반경 3에 적 ≥6이면 이동 속도 +4%p |
| `common.move.06` | 위기의 걸음 | 780 | common.move.05@1 | R30 | HP ≤30%에서 이동 속도 +4%p |
| `common.move.07` | 먼 여정 | 980 | common.move.06@1 | R60 | 이동 속도 +2%p |
| `common.move.08` | 전진 | 1200 | common.move.07@1 | R90 | 같은 방향 ±15°로 1초 연속 이동 중 이동 속도 +3%p |
| `common.move.09` | 탈출로 | 1450 | common.move.08@1 | R120 | 접촉 피해 후 2초간 이동 속도 +5%p |
| `common.move.10` | 결전의 발 | 1750 | common.move.09@1 | R150 | 보스 본체 활성 중 이동 속도 +4%p |

## 경험치

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `growth` | 경험치 획득량 | 300 / 650 / 1100 | move@1 | — | 획득 XP +3%p/단계 |
| `common.xp.02` | 배움의 시작 | 220 | growth@1 | — | 라운드 첫 보석 5개 XP +5%p |
| `common.xp.03` | 빠른 배움 | 320 | common.xp.02@1 | R03 | 성장 Lv.5 이하 획득 XP +5%p |
| `common.xp.04` | 꾸준한 배움 | 450 | common.xp.03@1 | R09 | 획득 XP +3%p |
| `common.xp.05` | 굳건한 배움 | 600 | common.xp.04@1 | R15 | HP ≥90% 때 획득 XP +3%p |
| `common.xp.06` | 깊은 배움 | 780 | common.xp.05@1 | R30 | 성장 Lv.20 이상 획득 XP +4%p |
| `common.xp.07` | 이어지는 배움 | 980 | common.xp.06@1 | R60 | 2초 안에 두 번째 이후 보석 XP +4%p |
| `common.xp.08` | 포위 속 배움 | 1200 | common.xp.07@1 | R90 | 반경 3에 적 ≥6이면 획득 XP +5%p |
| `common.xp.09` | 오랜 배움 | 1450 | common.xp.08@1 | R120 | 획득 XP +4%p |
| `common.xp.10` | 결전의 배움 | 1750 | common.xp.09@1 | R150 | 보스 라운드의 일반 웨이브와 종료 보석 정산에서 획득 XP +6%p |

## 수집

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `pickup` | 경험치 획득 범위 | 60 / 120 / 220 / 360 / 540 | — | — | 획득 반경 +10%p/단계 |
| `common.pickup.02` | 넓은 손 | 220 | pickup@1 | — | 획득 반경 +5%p |
| `common.pickup.03` | 움직이는 수집 | 320 | common.pickup.02@1 | R03 | 이동 입력 중 획득 반경 +5%p |
| `common.pickup.04` | 조용한 수집 | 450 | common.pickup.03@1 | R09 | 1초 무이동부터 획득 반경 +7%p |
| `common.pickup.05` | 위기 수집 | 600 | common.pickup.04@1 | R15 | HP ≤30%에서 획득 반경 +8%p |
| `common.pickup.06` | 첫 보석 | 780 | common.pickup.05@1 | R30 | 라운드 첫 보석까지 획득 반경 +8%p |
| `common.pickup.07` | 여러 보석 | 980 | common.pickup.06@1 | R60 | 직전 습득 후 2초간 획득 반경 +5%p |
| `common.pickup.08` | 넓은 시야 | 1200 | common.pickup.07@1 | R90 | 획득 반경 +6%p |
| `common.pickup.09` | 포위 속 수집 | 1450 | common.pickup.08@1 | R120 | 반경 3에 적 ≥6이면 획득 반경 +8%p |
| `common.pickup.10` | 결전의 수집 | 1750 | common.pickup.09@1 | R150 | 보스 라운드의 일반 웨이브 중 획득 반경 +10%p |
