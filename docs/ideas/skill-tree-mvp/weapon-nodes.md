# 무기 전용 영구 노드 300종

가격 칸에는 1회 결제액을 표시한다. 모든 노드는 1랭크다. 숙련은 공통 공격력 랭크 1과 해당 무기 영구 해금을 요구한다. 진화 갈래의 표식 E는 해당 무기의 영구 진화 업적을 뜻한다. Rnn은 영구 라운드 정산 조건이다. 구매한 효과는 도전 사본에 들어가며 같은 무기를 보유하는 동안 기본형과 진화형에 적용된다. 진화 갈래는 실제 진화형에서만 적용한다. 종속 동작이 필요한 경우 선행 칸에 함께 표시했다. 발동과 비용 공유는 [계산·발동 계약](../skill-tree-mvp-design.md#합산과-발동)을 따른다. 행의 타격 계수(예를 들어 ‘35%’)는 해당 무기의 기본 공격 피해 대비 보조 타격 계수다.

## 회전 성경

### 기초

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.bible.base.01` | 성경 숙련 | 150 | attack@1 | 무기 해금 | 해당 무기 피해 +10%p |
| `weapon.bible.base.02` | 성경 신속 | 250 | weapon.bible.base.01@1 | — | 해당 무기 주기 대기부 -5%p |
| `weapon.bible.base.03` | 한 권 더 | 350 | weapon.bible.base.01@1 | — | 수량 +1권 |
| `weapon.bible.base.04` | 빠른 첫 장 | 420 | weapon.bible.base.03@1 | R03 | 라운드 첫 책 생성의 활성부 종료 후 첫 대기부 -25%p(대기부 0인 진화형은 라운드 첫 말씀 파동 반경 +10%p) |
| `weapon.bible.base.05` | 큰 책장 | 580 | weapon.bible.base.04@1 | R09 | 책 충돌 반경 +12%p |

### 회전 궤도

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.bible.a.01` | 속독 | 450 | weapon.bible.base.01@1 | R03 | 회전 속력 +12%p |
| `weapon.bible.a.02` | 바깥 궤도 | 650 | weapon.bible.a.01@1 | R09 | 반경 3에 적 ≥6이면 밀집 방향 궤도 반경 +15%p |
| `weapon.bible.a.03` | 안쪽 궤도 | 900 | weapon.bible.a.02@1, weapon.bible.base.03@1 | R30 | 특화 책 1권을 반경 ×0.65에 배치 |
| `weapon.bible.a.04` | 교차 회전 | 1200 | weapon.bible.a.03@1 | R60 | 안쪽 책 역회전 |
| `weapon.bible.a.05` | 왕복 궤도 | 1500 | weapon.bible.a.04@1 | R120 | 궤도 반경을 주기 중 +0→+0.20→+0 단위로 선형 보간 |

### 수호

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.bible.b.01` | 단단한 표지 | 450 | weapon.bible.base.01@1 | R03 | 책 밀침 +0.20 |
| `weapon.bible.b.02` | 접근 차단 | 650 | weapon.bible.b.01@1 | R09 | 거리 ≤1.2인 대상 밀침 +0.25 |
| `weapon.bible.b.03` | 펼친 방패 | 900 | weapon.bible.b.02@1 | R30 | 책 접촉 적탄 제거 20%, 대기 2초 |
| `weapon.bible.b.04` | 위기의 표지 | 1200 | weapon.bible.b.03@1 | R60 | 내 HP ≤30%에 진입 시 3초간 책 충돌 반경 +15%p, 대기 8초 |
| `weapon.bible.b.05` | 마지막 장벽 | 1500 | weapon.bible.b.04@1 | R120 | 실제 피격 후 반경 1.6에 35% 충격파, 대기 5초 |

### 페이지

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.bible.c.01` | 낱장 | 450 | weapon.bible.base.01@1 | R03 | 책 원본 적중 20%에 사거리 3·속력 7 페이지 35%, 대기 1초 |
| `weapon.bible.c.02` | 잉크 자국 | 650 | weapon.bible.c.01@1 | R09 | 페이지 경로에 폭 0.35·1초·15% 잉크 |
| `weapon.bible.c.03` | 다음 구절 | 900 | weapon.bible.c.02@1 | R30 | 페이지 적중 예산 2(첫 대상 포함) |
| `weapon.bible.c.04` | 책갈피 | 1200 | weapon.bible.c.03@1 | R60 | 책 타격 대상 2초 표식, 첫 페이지 피해 계수 +0.12 |
| `weapon.bible.c.05` | 흩날리는 장 | 1500 | weapon.bible.c.04@1 | R120 | 책 자연 종료 시 최대 2권에서 같은 규격 페이지 1개씩, 대기 4초 |

### 순환

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.bible.d.01` | 긴 독서 | 450 | weapon.bible.base.01@1 | R03 | 책 지속 +10%p |
| `weapon.bible.d.02` | 이어 읽기 | 650 | weapon.bible.d.01@1 | R09 | 책이 일반 적 6처치하면 현재 지속 +0.25초, 생성당 1회 |
| `weapon.bible.d.03` | 돌아오는 책 | 900 | weapon.bible.d.02@1 | R30 | 책 자연 종료 시 2권까지 반경 0.25·속력 6·사거리 3로 귀환, 각 30% 1회 |
| `weapon.bible.d.04` | 여백의 빛 | 1200 | weapon.bible.d.03@1 | R60 | 책 비활성 중 1초마다 반경 1.2에 15% 타격 |
| `weapon.bible.d.05` | 다시 펼치기 | 1500 | weapon.bible.d.04@1 | R120 | 책 활성 중 정예 원본 처치 시 다음 대기 -10%p, 대기 6초 |

### 진화 연계

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.bible.e.01` | 넓은 성역 | 850 | weapon.bible.base.01@1 | E | 말씀 파동 반경 +15%p |
| `weapon.bible.e.02` | 두 번째 울림 | 1050 | weapon.bible.e.01@1 | E + R15 | 네 번째 파동마다 0.5초 뒤 35% 추가 파동 |
| `weapon.bible.e.03` | 성역의 경계 | 1250 | weapon.bible.e.02@1 | E + R30 | 파동 외곽(반경의 ≥80%) 밀침 +0.30 |
| `weapon.bible.e.04` | 중심의 말씀 | 1500 | weapon.bible.e.03@1, weapon.bible.a.03@1 | E + R90 | 파동에 미적중한 거리 ≤1.4의 최근접 적 1명 쪽으로 안쪽 책 각도를 최대 30° 이동한 뒤 기존 충돌로 판정, 기본 회전 속력으로 복귀(파동당 1회) |
| `weapon.bible.e.05` | 겹친 성역 | 1800 | weapon.bible.e.04@1 | E + R150 | 정예 원본 처치 위치에 반경 1.2·1.5초·25% 성역, 대기 5초 |

## 삼손의 턱뼈

### 기초

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.jawbone.base.01` | 턱뼈 숙련 | 150 | attack@1 | 무기 해금 | 해당 무기 피해 +10%p |
| `weapon.jawbone.base.02` | 턱뼈 신속 | 250 | weapon.jawbone.base.01@1 | — | 해당 무기 주기 대기부 -5%p |
| `weapon.jawbone.base.03` | 연속 휘두르기 | 350 | weapon.jawbone.base.01@1 | — | 수량 +1타(0.15초 기본 간격) |
| `weapon.jawbone.base.04` | 긴 팔 | 420 | weapon.jawbone.base.03@1 | R03 | 휘두르기 도달 +10%p |
| `weapon.jawbone.base.05` | 묵직한 끝 | 580 | weapon.jawbone.base.04@1 | R09 | 사거리 외곽 ≥80%에서 피해 +8%p |

### 휘두르기

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.jawbone.a.01` | 넓은 호 | 450 | weapon.jawbone.base.01@1 | R03 | 호 반각 +10° |
| `weapon.jawbone.a.02` | 되치기 | 650 | weapon.jawbone.a.01@1 | R09 | 주기 두 번째 타격 각도 +180° |
| `weapon.jawbone.a.03` | 등 뒤의 일격 | 900 | weapon.jawbone.a.02@1 | R30 | 네 번째 주기 후방에 30% 추가 타격 |
| `weapon.jawbone.a.04` | 내려찍기 | 1200 | weapon.jawbone.a.03@1 | R60 | 주기 마지막 타격 반각 ×0.60, 피해 +12%p |
| `weapon.jawbone.a.05` | 사방의 턱뼈 | 1500 | weapon.jawbone.a.04@1 | R120 | 여섯 번째 주기, 추가로 90° 간격 네 방향 반각 25°·30% 타격(대상당 1회) |

### 괴력

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.jawbone.b.01` | 강한 손목 | 450 | weapon.jawbone.base.01@1 | R03 | 밀침 +0.25 |
| `weapon.jawbone.b.02` | 무거운 상대 | 650 | weapon.jawbone.b.01@1 | R09 | 대상 현재 HP ≥75%면 피해 +6%p |
| `weapon.jawbone.b.03` | 갑주 깨기 | 900 | weapon.jawbone.b.02@1 | R30 | 같은 정예 3연속 원본 적중 시 3초간 턱뼈 피해 +8%p 표식 |
| `weapon.jawbone.b.04` | 땅 울리기 | 1200 | weapon.jawbone.b.03@1, weapon.jawbone.a.04@1 | R60 | 내려찍기 뒤 길이 2·폭 0.6·30% 충격파 1개, 대기 2초 |
| `weapon.jawbone.b.05` | 벽 없는 돌파 | 1500 | weapon.jawbone.b.04@1 | R120 | 한 휘두름에 적 ≥4면 해당 밀침 +0.20 |

### 연전

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.jawbone.c.01` | 첫 쓰러짐 | 450 | weapon.jawbone.base.01@1 | R03 | 주기 첫 원본 처치 후 2초 안 다음 타격 피해 +8%p, 주기당 1회 |
| `weapon.jawbone.c.02` | 이어지는 힘 | 650 | weapon.jawbone.c.01@1 | R09 | 2초 내 일반 적 원본 4처치 시 3초간 주기 대기부 -5%p, 대기 6초 |
| `weapon.jawbone.c.03` | 끊기지 않는 팔 | 900 | weapon.jawbone.c.02@1 | R30 | 정예 원본 처치 시 다음 주기 개시를 0.15초 앞당김, 대기 5초 |
| `weapon.jawbone.c.04` | 군세 상대 | 1200 | weapon.jawbone.c.03@1 | R60 | 한 원본 타격에 적 ≥4면 다음 타격 도달 +8%p, 2초 안 소모 |
| `weapon.jawbone.c.05` | 천인 돌파 | 1500 | weapon.jawbone.c.04@1 | R120 | 일반 적 누적 원본 20처치 시 반경 2.2·45% 타격, 대기 5초 |

### 근접 생존

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.jawbone.d.01` | 거리 확보 | 450 | weapon.jawbone.base.01@1 | R03 | 대상 거리 ≤1이면 밀침 +0.25 |
| `weapon.jawbone.d.02` | 상처의 반격 | 650 | weapon.jawbone.d.01@1 | R09 | 실제 피격 후 3초 내 다음 원본 피해 +10%p, 대기 5초 |
| `weapon.jawbone.d.03` | 숨 고르기 | 900 | weapon.jawbone.d.02@1 | R30 | 정예 원본 처치 시 HP 2 회복, 대기 6초 |
| `weapon.jawbone.d.04` | 흔들리지 않음 | 1200 | weapon.jawbone.d.03@1 | R60 | 원본 타격 직후 0.12초 동안 접촉 방어 +1 |
| `weapon.jawbone.d.05` | 포위 돌파 | 1500 | weapon.jawbone.d.04@1 | R120 | 반경 2에 적 ≥6이면 다음 타격을 360°로, 대기 5초 |

### 진화 연계

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.jawbone.e.01` | 깊은 충격 | 850 | weapon.jawbone.base.01@1 | E | 진화 충격파 도달 +15%p |
| `weapon.jawbone.e.02` | 연쇄 붕괴 | 1050 | weapon.jawbone.e.01@1 | E + R15 | 턱뼈에 밀린 적의 0.3초 이내 첫 인접 충돌에 25% 타격, 대기 2초 |
| `weapon.jawbone.e.03` | 굳센 연격 | 1250 | weapon.jawbone.e.02@1 | E + R30 | 마지막 타격 정예 피해 +10%p |
| `weapon.jawbone.e.04` | 넓은 돌파로 | 1500 | weapon.jawbone.e.03@1, weapon.jawbone.a.04@1 | E + R90 | 진화 내려찍기 뒤 반각 45°·길이 2.4·35% 파동, 대기 3초 |
| `weapon.jawbone.e.05` | 끝나지 않는 괴력 | 1800 | weapon.jawbone.e.04@1 | E + R150 | 정예 원본 처치 뒤 3초 내 첫 주기 +1타, 대기 6초 |

## 다윗의 물맷돌

### 기초

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.sling.base.01` | 물맷돌 숙련 | 150 | attack@1 | 무기 해금 | 해당 무기 피해 +10%p |
| `weapon.sling.base.02` | 물맷돌 신속 | 250 | weapon.sling.base.01@1 | — | 해당 무기 주기 대기부 -5%p |
| `weapon.sling.base.03` | 추가 투석 | 350 | weapon.sling.base.01@1 | — | 수량 +1발 |
| `weapon.sling.base.04` | 단단한 돌 | 420 | weapon.sling.base.03@1 | R03 | 돌 충돌 반경 +10%p |
| `weapon.sling.base.05` | 빠른 돌팔매 | 580 | weapon.sling.base.04@1 | R09 | 돌 속력 +12%p |

### 정밀

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.sling.a.01` | 거인 조준 | 450 | weapon.sling.base.01@1 | R03 | 사거리 내 보스·정예 우선 |
| `weapon.sling.a.02` | 약점 노리기 | 650 | weapon.sling.a.01@1 | R09 | 원본 치명 확률 +6%p |
| `weapon.sling.a.03` | 깊은 상처 | 900 | weapon.sling.a.02@1 | R30 | 치명 배율 +20%p |
| `weapon.sling.a.04` | 같은 표적 | 1200 | weapon.sling.a.03@1 | R60 | 같은 연사에서 같은 적 원본 2회째부터 피해 +6%p |
| `weapon.sling.a.05` | 결전의 한 발 | 1500 | weapon.sling.a.04@1 | R120 | 네 번째 주기 정예·보스에 첫 돌 피해 +12%p |

### 연사

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.sling.b.01` | 빠른 손 | 450 | weapon.sling.base.01@1 | R03 | 연사 내부 간격 -12%p |
| `weapon.sling.b.02` | 나눠 쏘기 | 650 | weapon.sling.b.01@1 | R09 | 정예가 없으면 최근접 순으로 연사 대상을 순환 |
| `weapon.sling.b.03` | 빈틈 사격 | 900 | weapon.sling.b.02@1 | R30 | 직전 돌로 표적 처치 시 다음 돌 재조준(예정 발사 시각 유지) |
| `weapon.sling.b.04` | 추가 장전 | 1200 | weapon.sling.b.03@1 | R60 | 네 번째 주기 끝에 45% 복제 돌 1개 |
| `weapon.sling.b.05` | 돌비 | 1500 | weapon.sling.b.04@1 | R120 | 일반 적 원본 누적 15적중 시 0.15초 간격 30% 돌 2발, 대기 5초 |

### 도탄

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.sling.c.01` | 튀는 돌 | 450 | weapon.sling.base.01@1 | R03 | 첫 적중 후 반경 2.5 내 미적중 적으로 45% 도탄 1회 |
| `weapon.sling.c.02` | 넓은 도약 | 650 | weapon.sling.c.01@1 | R09 | 도탄 탐색 +1 |
| `weapon.sling.c.03` | 남는 힘 | 900 | weapon.sling.c.02@1 | R30 | 도탄 계수 +0.15 |
| `weapon.sling.c.04` | 갈라진 파편 | 1200 | weapon.sling.c.03@1 | R60 | 마지막 도탄 적중 시 반경 1에 20% 파편 타격, 대기 2초 |
| `weapon.sling.c.05` | 돌아오는 돌 | 1500 | weapon.sling.c.04@1 | R120 | 도탄 대상 없으면 플레이어 쪽 3단위 귀환하며 25% 1적중, 대기 2초 |

### 관통

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.sling.d.01` | 꿰뚫기 | 450 | weapon.sling.base.01@1 | R03 | 유한 적중 예산 +1 |
| `weapon.sling.d.02` | 일렬 조준 | 650 | weapon.sling.d.01@1 | R09 | 보스·정예 없음일 때 직선 폭 0.6·길이 6에 적 ≥3인 방향 우선 |
| `weapon.sling.d.03` | 깊이 파고들기 | 900 | weapon.sling.d.02@1, weapon.sling.d.01@1 | R30 | 같은 원본 돌의 두 번째 이후 관통 적중 피해 +8%p, 감쇠 없음 |
| `weapon.sling.d.04` | 끝의 파열 | 1200 | weapon.sling.d.03@1 | R60 | 마지막 유한 적중에서 반경 1에 25% 파열, 대기 1초 |
| `weapon.sling.d.05` | 길 뚫는 돌 | 1500 | weapon.sling.d.04@1 | R120 | 여섯 번째 주기 첫 돌의 유한 적중 예산을 8로 올림 |

### 진화 연계

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.sling.e.01` | 다섯 돌의 집중 | 850 | weapon.sling.base.01@1 | E | 진화 연사 첫 생존 표적을 유지하며 조준 오차 0° |
| `weapon.sling.e.02` | 다섯째의 무게 | 1050 | weapon.sling.e.01@1 | E + R15 | 기본 슬롯 5번 피해 +15%p |
| `weapon.sling.e.03` | 거인의 흔들림 | 1250 | weapon.sling.e.02@1 | E + R30 | 보스 원본 누적 12적중 시 다음 연사 피해 +8%p, 대기 5초 |
| `weapon.sling.e.04` | 흩어지는 승리 | 1500 | weapon.sling.e.03@1 | E + R90 | 정예 원본 처치 시 반경 3 내 일반 적 2명에 25% 파편, 대기 3초 |
| `weapon.sling.e.05` | 한 발의 결단 | 1800 | weapon.sling.e.04@1 | E + R150 | 다섯 번째 주기 첫 돌의 첫 실제 충돌 치명 확정 |

## 모세의 지팡이

### 기초

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.moses_staff.base.01` | 지팡이 숙련 | 150 | attack@1 | 무기 해금 | 해당 무기 피해 +10%p |
| `weapon.moses_staff.base.02` | 지팡이 신속 | 250 | weapon.moses_staff.base.01@1 | — | 해당 무기 주기 대기부 -5%p |
| `weapon.moses_staff.base.03` | 갈라지는 물결 | 350 | weapon.moses_staff.base.01@1 | — | 수량 +1물결 |
| `weapon.moses_staff.base.04` | 먼 물길 | 420 | weapon.moses_staff.base.03@1 | R03 | 물결 이동 거리 +12%p |
| `weapon.moses_staff.base.05` | 거센 흐름 | 580 | weapon.moses_staff.base.04@1 | R09 | 물결 속력 +12%p |

### 물길

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.moses_staff.a.01` | 넓은 수로 | 450 | weapon.moses_staff.base.01@1 | R03 | 물결 폭 +10%p |
| `weapon.moses_staff.a.02` | 물결 사이 | 650 | weapon.moses_staff.a.01@1 | R09 | 물결 간 각도 15→20° |
| `weapon.moses_staff.a.03` | 모이는 흐름 | 900 | weapon.moses_staff.a.02@1 | R30 | 반경 4 내 정예 우선 시 물결 간 각도 최대 5° |
| `weapon.moses_staff.a.04` | 돌아오는 파도 | 1200 | weapon.moses_staff.a.03@1 | R60 | 끝에서 반대 방향 사거리 3·속력 6·폭 0.6의 35% 파도, 대기 2초 |
| `weapon.moses_staff.a.05` | 굽은 수로 | 1500 | weapon.moses_staff.a.04@1 | R120 | 이동 중 첫 일반 적 방향으로 최대 12° 선회 1회 |

### 파도

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.moses_staff.b.01` | 밀어내는 물 | 450 | weapon.moses_staff.base.01@1 | R03 | 밀침 +0.25 |
| `weapon.moses_staff.b.02` | 솟는 물기둥 | 650 | weapon.moses_staff.b.01@1 | R09 | 끝 지점 반경 1에 35% 타격, 대기 2초 |
| `weapon.moses_staff.b.03` | 무거운 파도 | 900 | weapon.moses_staff.b.02@1 | R30 | 거리 ≤2인 대상 피해 +8%p |
| `weapon.moses_staff.b.04` | 부서지는 물보라 | 1200 | weapon.moses_staff.b.03@1 | R60 | 정예 원본 적중 시 반경 1.2에 25% 물보라, 대기 2초 |
| `weapon.moses_staff.b.05` | 넘실대는 힘 | 1500 | weapon.moses_staff.b.04@1 | R120 | 네 번째 주기 첫 파도 폭 +15%p·피해 +10%p |

### 잔류 수로

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.moses_staff.c.01` | 젖은 길 | 450 | weapon.moses_staff.base.01@1 | R03 | 경로에 폭 0.7·1.5초·25% 수로 |
| `weapon.moses_staff.c.02` | 긴 물길 | 650 | weapon.moses_staff.c.01@1 | R09 | 수로 지속 +15%p |
| `weapon.moses_staff.c.03` | 갈라진 지면 | 900 | weapon.moses_staff.c.02@1 | R30 | 수로 외곽 ≥80%에 처음 진입 시 대상당 20% 추가, 구역별 1회 |
| `weapon.moses_staff.c.04` | 흐름의 교차 | 1200 | weapon.moses_staff.c.03@1 | R60 | 서로 다른 수로 겹침 시 첫 교차점 반경 1에 30% 물기둥, 대기 3초 |
| `weapon.moses_staff.c.05` | 밀려오는 끝물 | 1500 | weapon.moses_staff.c.04@1 | R120 | 수로 자연 종료 시 중점 반경 1.2에 25%, 대기 3초 |

### 보호

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.moses_staff.d.01` | 가까운 물막이 | 450 | weapon.moses_staff.base.01@1 | R03 | 발사 위치 전방 반각 30°·도달 1.2에 25% 물막, 대기 2초 |
| `weapon.moses_staff.d.02` | 막아선 파도 | 650 | weapon.moses_staff.d.01@1 | R09 | 원본 물결 접촉 적탄 제거 20%, 대기 2초 |
| `weapon.moses_staff.d.03` | 빈틈의 수로 | 900 | weapon.moses_staff.d.02@1 | R30 | 실제 피격 시 다음 주기 개시를 0.25초 앞당김, 대기 5초 |
| `weapon.moses_staff.d.04` | 길 위의 발걸음 | 1200 | weapon.moses_staff.d.03@1, weapon.moses_staff.c.01@1 | R60 | 자기의 잔류 수로 또는 진화 기본 수로 안에서 이동 +5%p, 두 수로가 겹쳐도 한 번 적용 |
| `weapon.moses_staff.d.05` | 둘러싼 물벽 | 1500 | weapon.moses_staff.d.04@1 | R120 | 반경 2에 적 ≥6인 주기 시작에 반경 2·30% 원형 물결, 대기 4초 |

### 진화 연계

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.moses_staff.e.01` | 넓은 홍해의 길 | 850 | weapon.moses_staff.base.01@1 | E | 진화 수로 폭 +15%p |
| `weapon.moses_staff.e.02` | 양옆의 물벽 | 1050 | weapon.moses_staff.e.01@1 | E + R15 | 진화 수로 외곽 ≥75%의 피해 계수 +0.12 |
| `weapon.moses_staff.e.03` | 긴 통로 | 1250 | weapon.moses_staff.e.02@1 | E + R30 | 진화 수로 지속 +20%p |
| `weapon.moses_staff.e.04` | 밀려드는 귀환 | 1500 | weapon.moses_staff.e.03@1 | E + R90 | 진화 수로 종료 시 끝점→시점에 50% 역파도 1개, 대기 3초 |
| `weapon.moses_staff.e.05` | 열린 길 | 1800 | weapon.moses_staff.e.04@1 | E + R150 | 정예 원본 처치 위치에 진행 방향 길이 2·폭 0.8·1초·30% 수로, 대기 5초 |

## 다니엘의 사자

### 기초

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.daniel_lion.base.01` | 사자 숙련 | 150 | attack@1 | 무기 해금 | 해당 무기 피해 +10%p |
| `weapon.daniel_lion.base.02` | 사자 신속 | 250 | weapon.daniel_lion.base.01@1 | — | 해당 무기 주기 대기부 -5%p |
| `weapon.daniel_lion.base.03` | 사자의 동행 | 350 | weapon.daniel_lion.base.01@1 | — | 수량 +1사자 |
| `weapon.daniel_lion.base.04` | 빠른 추격 | 420 | weapon.daniel_lion.base.03@1 | R03 | 사자 속력 +12%p |
| `weapon.daniel_lion.base.05` | 긴 발톱 | 580 | weapon.daniel_lion.base.04@1 | R09 | 할퀴기 반경 +10%p |

### 추적

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.daniel_lion.a.01` | 큰 먹잇감 | 450 | weapon.daniel_lion.base.01@1 | R03 | 플레이어 반경 5 내 정예 추적 우선 |
| `weapon.daniel_lion.a.02` | 먼 사냥 | 650 | weapon.daniel_lion.a.01@1 | R09 | 복귀 거리 +15%p |
| `weapon.daniel_lion.a.03` | 뛰어들기 | 900 | weapon.daniel_lion.a.02@1 | R30 | 표적 거리 ≥2에서 0.25초 동안 최대 2.5 이동 도약, 사자별 대기 3초 |
| `weapon.daniel_lion.a.04` | 빈틈 없는 추격 | 1200 | weapon.daniel_lion.a.03@1 | R60 | 표적 사망 시 0.15초 안 재탐색(타격 대기 유지) |
| `weapon.daniel_lion.a.05` | 사냥의 마침표 | 1500 | weapon.daniel_lion.a.04@1 | R120 | 일반 적 HP ≤25%에서 사자 피해 +10%p |

### 협공

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.daniel_lion.b.01` | 표적 분담 | 450 | weapon.daniel_lion.base.01@1 | R03 | 일반 적 배정 시 2순위 표적이 1순위의 거리 +1 이내면 미배정 우선 |
| `weapon.daniel_lion.b.02` | 집중 포위 | 650 | weapon.daniel_lion.b.01@1 | R09 | 보스 단일 표적일 때 최초 타격 위상을 사자 인덱스 ×0.12초 분산 |
| `weapon.daniel_lion.b.03` | 맞물린 발톱 | 900 | weapon.daniel_lion.b.02@1 | R30 | 다른 사자의 원본 적중 후 1초간 같은 적에 피해 +8%p |
| `weapon.daniel_lion.b.04` | 사냥 표식 | 1200 | weapon.daniel_lion.b.03@1 | R60 | 정예 원본 적중 시 2초 표식, 사자 원본 피해 +6%p |
| `weapon.daniel_lion.b.05` | 동시 도약 | 1500 | weapon.daniel_lion.b.04@1 | R120 | 여섯 번째 무기 주기 사자 2마리까지 각 35% 추가 도약, 대기 4초 |

### 포효

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.daniel_lion.c.01` | 짧은 포효 | 450 | weapon.daniel_lion.base.01@1 | R03 | 반경 1.4에 포효 25%, 사자별 4초 |
| `weapon.daniel_lion.c.02` | 큰 울림 | 650 | weapon.daniel_lion.c.01@1 | R09 | 포효 반경 +15%p |
| `weapon.daniel_lion.c.03` | 위협의 소리 | 900 | weapon.daniel_lion.c.02@1 | R30 | 포효 밀침 +0.30 |
| `weapon.daniel_lion.c.04` | 사냥의 신호 | 1200 | weapon.daniel_lion.c.03@1 | R60 | 자기 포효 뒤 2초간 사자 타격 대기부 -6%p |
| `weapon.daniel_lion.c.05` | 겹치지 않는 울림 | 1500 | weapon.daniel_lion.c.04@1 | R120 | 동시 포효는 인덱스 ×0.18초 엇갈림 |

### 호위

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.daniel_lion.d.01` | 가까운 순찰 | 450 | weapon.daniel_lion.base.01@1 | R03 | 사자 1마리가 4초마다 1초간 내 반경 1.5 순찰 |
| `weapon.daniel_lion.d.02` | 위험 감지 | 650 | weapon.daniel_lion.d.01@1 | R09 | 순찰 때 반경 2에 적 ≥4면 최근접 무리 방향 최대 2 도약, 대기 4초 |
| `weapon.daniel_lion.d.03` | 귀환의 발톱 | 900 | weapon.daniel_lion.d.02@1 | R30 | 복귀 경로 폭 0.5에 30%, 대상당 복귀마다 1회 |
| `weapon.daniel_lion.d.04` | 보호의 포효 | 1200 | weapon.daniel_lion.d.03@1 | R60 | 실제 피격 시 순찰 사자 반경 1.6에 밀침 0.6, 대기 6초 |
| `weapon.daniel_lion.d.05` | 안전한 거리 | 1500 | weapon.daniel_lion.d.04@1 | R120 | 거리 ≤2인 정예 원본 처치 후 2초간 방어 +2, 대기 6초 |

### 진화 연계

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.daniel_lion.e.01` | 수호자의 도약 | 850 | weapon.daniel_lion.base.01@1 | E | 진화 도약 피해 계수 +0.15 |
| `weapon.daniel_lion.e.02` | 연속 덮치기 | 1050 | weapon.daniel_lion.e.01@1 | E + R15 | 진화 도약 처치 시 다음 표적으로 35% 도약 1회, 대기 5초 |
| `weapon.daniel_lion.e.03` | 큰 사냥터 | 1250 | weapon.daniel_lion.e.02@1 | E + R30 | 진화 사자 할퀴기 반경 +12%p |
| `weapon.daniel_lion.e.04` | 수호의 울림 | 1500 | weapon.daniel_lion.e.03@1 | E + R90 | 진화 포효 접촉 적탄 제거 25%, 대기 2초 |
| `weapon.daniel_lion.e.05` | 무리의 수호자 | 1800 | weapon.daniel_lion.e.04@1 | E + R150 | 협공의 사냥 표식을 구매했으면 +6→+10%p, 사냥 표식이 없으면 정예 원본 적중 시 2초 +4%p 표식 |

## 불기둥

### 기초

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.fire_pillar.base.01` | 불기둥 숙련 | 150 | attack@1 | 무기 해금 | 해당 무기 피해 +10%p |
| `weapon.fire_pillar.base.02` | 불기둥 신속 | 250 | weapon.fire_pillar.base.01@1 | — | 해당 무기 주기 대기부 -5%p |
| `weapon.fire_pillar.base.03` | 쌍불기둥 | 350 | weapon.fire_pillar.base.01@1 | — | 수량 +1기둥 |
| `weapon.fire_pillar.base.04` | 넓은 불자리 | 420 | weapon.fire_pillar.base.03@1 | R03 | 불기둥 반경 +10%p |
| `weapon.fire_pillar.base.05` | 빠른 첫 불꽃 | 580 | weapon.fire_pillar.base.04@1 | R09 | 점화 지연 -0.08초(최소 0.05초) |

### 점화 위치

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.fire_pillar.a.01` | 군집 점화 | 450 | weapon.fire_pillar.base.01@1 | R03 | 반경 1.5 내 적이 가장 많은 지점 우선 |
| `weapon.fire_pillar.a.02` | 정예 점화 | 650 | weapon.fire_pillar.a.01@1 | R09 | 거리 ≤4의 정예 우선 |
| `weapon.fire_pillar.a.03` | 진로 점화 | 900 | weapon.fire_pillar.a.02@1 | R30 | 해당 적의 속도와 방향으로 0.25초 앞을 예측, 이동량 최대 1 |
| `weapon.fire_pillar.a.04` | 겹침 줄이기 | 1200 | weapon.fire_pillar.a.03@1 | R60 | 첫 중심부터 최소 1.2 간격인 후보 우선 |
| `weapon.fire_pillar.a.05` | 둘러싼 불 | 1500 | weapon.fire_pillar.a.04@1 | R120 | 다섯 번째 주기 내 주변 반경 1의 최근접 적 쪽에 반경 0.7·1초·35% 불자리, 대기 4초 |

### 지속

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.fire_pillar.b.01` | 오래 타기 | 450 | weapon.fire_pillar.base.01@1 | R03 | 기둥 지속 +12%p |
| `weapon.fire_pillar.b.02` | 중심의 열 | 650 | weapon.fire_pillar.b.01@1 | R09 | 기둥 반경 ≤40% 영역 피해 +8%p |
| `weapon.fire_pillar.b.03` | 남은 불자리 | 900 | weapon.fire_pillar.b.02@1 | R30 | 기둥 종료 지점에 반경 0.7·1초·25% 잔불, 대기 2초 |
| `weapon.fire_pillar.b.04` | 끈질긴 열기 | 1200 | weapon.fire_pillar.b.03@1 | R60 | 동일 기둥 연속 3번째 타격부터 피해 +8%p |
| `weapon.fire_pillar.b.05` | 이어지는 불꽃 | 1500 | weapon.fire_pillar.b.04@1 | R120 | 기둥 정예 원본 처치 시 해당 기둥 +0.35초, 기둥당 1회·무기 대기 4초 |

### 확산

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.fire_pillar.c.01` | 날리는 불씨 | 450 | weapon.fire_pillar.base.01@1 | R03 | 여섯 번째 원본 적중에 반경 3의 다른 적으로 속력 6·25% 불씨, 대기 2초 |
| `weapon.fire_pillar.c.02` | 번지는 열 | 650 | weapon.fire_pillar.c.01@1 | R09 | 날리는 불씨 첫 적중 위치에 반경 0.6·1초·20% 불자리 1회 |
| `weapon.fire_pillar.c.03` | 뜨거운 전이 | 900 | weapon.fire_pillar.c.02@1 | R30 | 일반 적 원본 누적 8처치 시 반경 1·25% 화염, 대기 2초 |
| `weapon.fire_pillar.c.04` | 이어 붙은 불길 | 1200 | weapon.fire_pillar.c.03@1 | R60 | 거리 ≤2인 두 기둥 생성 시 최초 한 번 폭 0.6·30% 선형 타격, 대기 3초 |
| `weapon.fire_pillar.c.05` | 큰 점화 | 1500 | weapon.fire_pillar.c.04@1 | R120 | 일반 적 누적 원본 15처치 시 5초 안 다음 주기 기둥 반경 +15%p, 대기 5초 |

### 화염 주기

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.fire_pillar.d.01` | 상승하는 불 | 450 | weapon.fire_pillar.base.01@1 | R03 | 점화의 첫 타격 피해 +10%p |
| `weapon.fire_pillar.d.02` | 내려앉는 재 | 650 | weapon.fire_pillar.d.01@1 | R09 | 자연 종료 시 반경 1·25% 타격, 대기 2초 |
| `weapon.fire_pillar.d.03` | 열의 축적 | 900 | weapon.fire_pillar.d.02@1 | R30 | 같은 적이 해당 기둥 안에 1초 연속 머무르면 그 대상 전용 다음 타격 간격 -6%p(대상별 마지막 적중 시각 기준, 전체 틱과 중복 타격 금지) |
| `weapon.fire_pillar.d.04` | 재점화 | 1200 | weapon.fire_pillar.d.03@1 | R60 | 정예 원본 처치 위치에 반경 0.7·1초·35% 기둥, 대기 5초 |
| `weapon.fire_pillar.d.05` | 불의 맥박 | 1500 | weapon.fire_pillar.d.04@1 | R120 | 다섯 번째 주기 활성 원본 기둥 최대 3곳에서 즉시 25% 타격, 대상 1회, 대기 4초 |

### 진화 연계

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.fire_pillar.e.01` | 구름의 길 | 850 | weapon.fire_pillar.base.01@1 | E | 진화 잔불 지속 +20%p |
| `weapon.fire_pillar.e.02` | 느린 순찰 | 1050 | weapon.fire_pillar.e.01@1 | E + R15 | 반경 1.5에 적 ≥4일 때 진화 기둥 이동 속력 ×0.70 |
| `weapon.fire_pillar.e.03` | 옮겨 붙는 인도 | 1250 | weapon.fire_pillar.e.02@1 | E + R30 | 군집의 마지막 적이 죽으면 새 중심 이동 속력 ×1.25를 1초 적용, 대기 3초 |
| `weapon.fire_pillar.e.04` | 두른 열기 | 1500 | weapon.fire_pillar.e.03@1 | E + R90 | 진화 기둥 외곽 80~100%에 20% 추가 타격, 주기 0.75초 |
| `weapon.fire_pillar.e.05` | 광야의 화염길 | 1800 | weapon.fire_pillar.e.04@1 | E + R150 | 진화 기둥 2 이동마다 반경 0.7·1.5초·25% 불자리, 대기 1.5초 |

## 하늘의 번개

### 기초

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.lightning.base.01` | 번개 숙련 | 150 | attack@1 | 무기 해금 | 해당 무기 피해 +10%p |
| `weapon.lightning.base.02` | 번개 신속 | 250 | weapon.lightning.base.01@1 | — | 해당 무기 주기 대기부 -5%p |
| `weapon.lightning.base.03` | 추가 낙뢰 | 350 | weapon.lightning.base.01@1 | — | 수량 +1낙뢰 |
| `weapon.lightning.base.04` | 넓은 낙점 | 420 | weapon.lightning.base.03@1 | R03 | 낙뢰 반경 +10%p |
| `weapon.lightning.base.05` | 빠른 벼락 | 580 | weapon.lightning.base.04@1 | R09 | 낙뢰 지연 -0.08초(최소 0.05초) |

### 표적

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.lightning.a.01` | 군집 탐지 | 450 | weapon.lightning.base.01@1 | R03 | 반경 1.5의 적 밀집도가 높은 곳 우선 |
| `weapon.lightning.a.02` | 높은 위협 | 650 | weapon.lightning.a.01@1 | R09 | 보스·정예 우선 |
| `weapon.lightning.a.03` | 고른 낙뢰 | 900 | weapon.lightning.a.02@1 | R30 | 모든 미배정 적 소진 뒤 재사용, 재사용 시 직전 배정 표적 후순위 |
| `weapon.lightning.a.04` | 끝까지 추적 | 1200 | weapon.lightning.a.03@1 | R60 | 착탄 전 표적 사망 시 1회 재조준(지연 재시작 없음) |
| `weapon.lightning.a.05` | 고독한 적 | 1500 | weapon.lightning.a.04@1 | R120 | 낙점 반경 2 내 다른 적이 없으면 대상 피해 +10%p |

### 강타

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.lightning.b.01` | 깊은 벼락 | 450 | weapon.lightning.base.01@1 | R03 | 대상 HP ≥75% 피해 +6%p |
| `weapon.lightning.b.02` | 순간 치명 | 650 | weapon.lightning.b.01@1 | R09 | 원본 치명 확률 +5%p |
| `weapon.lightning.b.03` | 잔류 전하 | 900 | weapon.lightning.b.02@1 | R30 | 같은 적에 1.5초 내 낙뢰 재적중 피해 +7%p |
| `weapon.lightning.b.04` | 벼락의 끝 | 1200 | weapon.lightning.b.03@1 | R60 | 주기 마지막 낙뢰 반경 +14%p |
| `weapon.lightning.b.05` | 겹친 천둥 | 1500 | weapon.lightning.b.04@1 | R120 | 다섯 번째 주기에 정예 첫 원본 적중 지점에 0.5초 후 35%, 반경 0.8 |

### 전도

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.lightning.c.01` | 작은 불꽃 | 450 | weapon.lightning.base.01@1 | R03 | 반경 2.5 내 미적중 적 1명에 전류 35%, 대기 1초 |
| `weapon.lightning.c.02` | 먼 전도 | 650 | weapon.lightning.c.01@1 | R09 | 해당 전류 탐색 반경 +1 |
| `weapon.lightning.c.03` | 남은 전압 | 900 | weapon.lightning.c.02@1 | R30 | 해당 전류 계수 +0.15 |
| `weapon.lightning.c.04` | 전하 표식 | 1200 | weapon.lightning.c.03@1 | R60 | 전류 적중한 적에 3초 내 다음 원본 낙뢰 피해 +8%p |
| `weapon.lightning.c.05` | 끊긴 전류 | 1500 | weapon.lightning.c.04@1 | R120 | 전류 표적이 없으면 첫 적에게 20% 충격, 같은 대기 소비 |

### 지면 전류

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.lightning.d.01` | 낙뢰 자국 | 450 | weapon.lightning.base.01@1 | R03 | 원본 낙점에 반경 0.7·1.5초·25% 전류 |
| `weapon.lightning.d.02` | 넓은 균열 | 650 | weapon.lightning.d.01@1 | R09 | 전류 반경 +15%p |
| `weapon.lightning.d.03` | 남는 전기 | 900 | weapon.lightning.d.02@1 | R30 | 전류 지속 +20%p |
| `weapon.lightning.d.04` | 이어진 지면 | 1200 | weapon.lightning.d.03@1 | R60 | 거리 ≤2.5인 구역 생성 시 최초 교차에 폭 0.5·30% 직선 타격, 대기 3초 |
| `weapon.lightning.d.05` | 땅의 울림 | 1500 | weapon.lightning.d.04@1 | R120 | 매 8번째 원본 낙뢰 반경 1.6·35% 전류 타격, 대기 3초 |

### 진화 연계

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.lightning.e.01` | 깊은 연쇄 | 850 | weapon.lightning.base.01@1 | E | 진화 연쇄 전이 +1(최대 4) |
| `weapon.lightning.e.02` | 갈라진 낙뢰 | 1050 | weapon.lightning.e.01@1 | E + R15 | 첫 전이 때 반경 2.5의 별도 미적중 적 1명에 30%, 대기 1초 |
| `weapon.lightning.e.03` | 우레의 폭 | 1250 | weapon.lightning.e.02@1 | E + R30 | 천둥 충격파 반경 +15%p |
| `weapon.lightning.e.04` | 폭풍 표식 | 1500 | weapon.lightning.e.03@1 | E + R90 | 정예 전이 시 5초 안 다음 원본 피해 +10%p, 대기 4초 |
| `weapon.lightning.e.05` | 폭풍의 중심 | 1800 | weapon.lightning.e.04@1 | E + R150 | 매 5번째 주기 첫 낙뢰 군집 우선·피해 +12%p(단일 보스 우선 유지) |

## 십자가

### 기초

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.cross.base.01` | 십자가 숙련 | 150 | attack@1 | 무기 해금 | 해당 무기 피해 +10%p |
| `weapon.cross.base.02` | 십자가 신속 | 250 | weapon.cross.base.01@1 | — | 해당 무기 주기 대기부 -5%p |
| `weapon.cross.base.03` | 쌍십자가 | 350 | weapon.cross.base.01@1 | — | 수량 +1십자가 |
| `weapon.cross.base.04` | 긴 비행 | 420 | weapon.cross.base.03@1 | R03 | 편도 거리 +12%p |
| `weapon.cross.base.05` | 큰 날개 | 580 | weapon.cross.base.04@1 | R09 | 충돌 반경 +10%p |

### 나아가는 길

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.cross.a.01` | 빠른 전진 | 450 | weapon.cross.base.01@1 | R03 | 전진 비행 속력 +12%p |
| `weapon.cross.a.02` | 정예 조준 | 650 | weapon.cross.a.01@1 | R09 | 반경 5 내 정예 우선 |
| `weapon.cross.a.03` | 넓은 펼침 | 900 | weapon.cross.a.02@1 | R30 | 다중 발사 간 각도 +6° |
| `weapon.cross.a.04` | 첫 타격 | 1200 | weapon.cross.a.03@1 | R60 | 전진 첫 적중 피해 +8%p |
| `weapon.cross.a.05` | 끝의 회전 | 1500 | weapon.cross.a.04@1 | R120 | 반환 시 반경 1·30% 회전 타격, 대기 2초 |

### 돌아오는 길

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.cross.b.01` | 빠른 귀환 | 450 | weapon.cross.base.01@1 | R03 | 귀환 속력 +12%p |
| `weapon.cross.b.02` | 귀환의 무게 | 650 | weapon.cross.b.01@1 | R09 | 귀환 피해 +8%p |
| `weapon.cross.b.03` | 다른 귀로 | 900 | weapon.cross.b.02@1 | R30 | 귀환 시 시작 접선 각도 20°만큼 수정하고 0.4초 동안 0°로 선형 복귀 |
| `weapon.cross.b.04` | 지나온 표적 | 1200 | weapon.cross.b.03@1 | R60 | 같은 십자가 전진 적중 대상의 귀환 피해 +7%p |
| `weapon.cross.b.05` | 두 번째 순회 | 1500 | weapon.cross.b.04@1 | R120 | 다섯 번째 주기 35% 십자가 1개 추가 왕복 |

### 관통과 회전

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.cross.c.01` | 긴 관통 | 450 | weapon.cross.base.01@1 | R03 | 유한 적중 예산 +1/구간 |
| `weapon.cross.c.02` | 회전의 테두리 | 650 | weapon.cross.c.01@1 | R09 | 이동 충돌 반경 +8%p |
| `weapon.cross.c.03` | 다중 적중 | 900 | weapon.cross.c.02@1 | R30 | 같은 개체 4번째 이후 적중 피해 +8%p |
| `weapon.cross.c.04` | 꿰뚫는 귀환 | 1200 | weapon.cross.c.03@1 | R60 | 여섯 번째 주기 첫 십자가 귀환 예산 8 |
| `weapon.cross.c.05` | 교차점 | 1500 | weapon.cross.c.04@1 | R120 | 서로 다른 원본 궤적이 0.25초 내 교차 시 반경 0.8·30% 빛, 대기 3초 |

### 잔광

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.cross.d.01` | 빛의 흔적 | 450 | weapon.cross.base.01@1 | R03 | 경로에 폭 0.6·1초·25% 잔광 |
| `weapon.cross.d.02` | 오래 남는 빛 | 650 | weapon.cross.d.01@1 | R09 | 잔광 지속 +20%p |
| `weapon.cross.d.03` | 돌아온 빛 | 900 | weapon.cross.d.02@1 | R30 | 귀환 잔광 계수 +0.10 |
| `weapon.cross.d.04` | 가시의 예고 | 1200 | weapon.cross.d.03@1 | R60 | 정예 원본 적중 지점 반경 0.6·30% 가시, 대기 2초 |
| `weapon.cross.d.05` | 흩어진 광휘 | 1500 | weapon.cross.d.04@1 | R120 | 정예 원본 처치 시 반경 3 내 일반 적 2명에 25%, 대기 4초 |

### 진화 연계

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.cross.e.01` | 긴 수난의 길 | 850 | weapon.cross.base.01@1 | E | 진화 가시 지속 +20%p |
| `weapon.cross.e.02` | 겹친 가시 | 1050 | weapon.cross.e.01@1 | E + R15 | 전진·귀환 가시 겹침 구간의 가시 계수 +0.12, 한 구역만 피해 적용 |
| `weapon.cross.e.03` | 넓은 광휘 | 1250 | weapon.cross.e.02@1 | E + R30 | 진화 가시 반경 +15%p |
| `weapon.cross.e.04` | 부서지는 속박 | 1500 | weapon.cross.e.03@1 | E + R90 | 귀환 완료 시 반경 1.5·35% 파동, 대기 3초 |
| `weapon.cross.e.05` | 수난의 순회 | 1800 | weapon.cross.e.04@1 | E + R150 | 네 번째 주기 첫 진화 십자가 반환점에서 0.5초·반경 0.8·35% 회전 후 귀환 |

## 광야의 외침

### 기초

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.wilderness_voice.base.01` | 외침 숙련 | 150 | attack@1 | 무기 해금 | 해당 무기 피해 +10%p |
| `weapon.wilderness_voice.base.02` | 외침 신속 | 250 | weapon.wilderness_voice.base.01@1 | — | 해당 무기 주기 대기부 -5%p |
| `weapon.wilderness_voice.base.03` | 넓은 외침 | 350 | weapon.wilderness_voice.base.01@1 | — | 도달 +20%p |
| `weapon.wilderness_voice.base.04` | 먼 목소리 | 420 | weapon.wilderness_voice.base.03@1 | R03 | 도달 +10%p |
| `weapon.wilderness_voice.base.05` | 빠른 첫 음절 | 580 | weapon.wilderness_voice.base.04@1 | R09 | 첫 타격 지연 -0.08초(최소 0.05초) |

### 발성

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.wilderness_voice.a.01` | 긴 호흡 | 450 | weapon.wilderness_voice.base.01@1 | R03 | 외침 지속 +12%p |
| `weapon.wilderness_voice.a.02` | 강한 첫말 | 650 | weapon.wilderness_voice.a.01@1 | R09 | 첫 음파 피해 +8%p |
| `weapon.wilderness_voice.a.03` | 끝의 외침 | 900 | weapon.wilderness_voice.a.02@1 | R30 | 마지막 음파 밀침 +0.3 |
| `weapon.wilderness_voice.a.04` | 흔들리지 않는 발성 | 1200 | weapon.wilderness_voice.a.03@1 | R60 | 발성 도중 시작 조준각을 고정 |
| `weapon.wilderness_voice.a.05` | 마무리 음절 | 1500 | weapon.wilderness_voice.a.04@1 | R120 | 네 번째 주기 마지막 음파 피해 +12%p |

### 방향

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.wilderness_voice.b.01` | 넓은 입구 | 450 | weapon.wilderness_voice.base.01@1 | R03 | 반각 +8° |
| `weapon.wilderness_voice.b.02` | 중심의 울림 | 650 | weapon.wilderness_voice.b.01@1 | R09 | 조준에서 ±15° 안 피해 +8%p |
| `weapon.wilderness_voice.b.03` | 가장자리 울림 | 900 | weapon.wilderness_voice.b.02@1 | R30 | 반각의 ≥80% 각도에서 밀침 +0.25 |
| `weapon.wilderness_voice.b.04` | 군세를 향해 | 1200 | weapon.wilderness_voice.b.03@1 | R60 | 발성 시작 시 거리 5 내 밀집 방향 우선 |
| `weapon.wilderness_voice.b.05` | 방향 전환 | 1500 | weapon.wilderness_voice.b.04@1 | R120 | 뒤 반원 거리 4의 적이 앞보다 ≥3 많으면 바로 다음 발성을 그쪽으로 조준(보스·정예 우선 유지) |

### 압력

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.wilderness_voice.c.01` | 밀어내는 소리 | 450 | weapon.wilderness_voice.base.01@1 | R03 | 음파 밀침 +0.25 |
| `weapon.wilderness_voice.c.02` | 가까운 압력 | 650 | weapon.wilderness_voice.c.01@1 | R09 | 거리 ≤1.2에서 밀침 +0.25 |
| `weapon.wilderness_voice.c.03` | 굳센 표적 | 900 | weapon.wilderness_voice.c.02@1 | R30 | 같은 정예 원본 3연속 적중 후 3초간 피해 +8%p |
| `weapon.wilderness_voice.c.04` | 날아오는 위협 | 1200 | weapon.wilderness_voice.c.03@1 | R60 | 음파 접촉 적탄 제거 20%, 대기 2초 |
| `weapon.wilderness_voice.c.05` | 몰아치는 끝말 | 1500 | weapon.wilderness_voice.c.04@1 | R120 | 원본 누적 15적중 시 다음 마지막 음파 피해 +12%p, 대기 5초 |

### 리듬

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.wilderness_voice.d.01` | 호흡 정리 | 450 | weapon.wilderness_voice.base.01@1 | R03 | 한 외침 원본 일반 적 ≥5처치 시 다음 대기 -10%p, 대기 4초 |
| `weapon.wilderness_voice.d.02` | 쉬지 않는 선포 | 650 | weapon.wilderness_voice.d.01@1 | R09 | 정예 원본 처치 후 3초간 주기 대기부 -6%p, 대기 5초 |
| `weapon.wilderness_voice.d.03` | 깊은 울림 | 900 | weapon.wilderness_voice.d.02@1 | R30 | 같은 발성에서 같은 적 원본 3번째부터 피해 +8%p |
| `weapon.wilderness_voice.d.04` | 위기의 외침 | 1200 | weapon.wilderness_voice.d.03@1 | R60 | 실제 피격 후 4초 안 다음 발성 도달 +12%p, 대기 6초 |
| `weapon.wilderness_voice.d.05` | 길어진 선포 | 1500 | weapon.wilderness_voice.d.04@1 | R120 | 원본 일반 적 누적 15처치 시 다음 발성 +0.5초, 대기 5초 |

### 진화 연계

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.wilderness_voice.e.01` | 사방의 선포 | 850 | weapon.wilderness_voice.base.01@1 | E | 사방 음파 반경 +15%p |
| `weapon.wilderness_voice.e.02` | 먼 메아리 | 1050 | weapon.wilderness_voice.e.01@1 | E + R15 | 진화 메아리 도달 +12%p |
| `weapon.wilderness_voice.e.03` | 연속 메아리 | 1250 | weapon.wilderness_voice.e.02@1 | E + R30 | 다섯 번째 주기 마지막 기본 메아리 0.5초 뒤 35% 메아리 1회 |
| `weapon.wilderness_voice.e.04` | 중심의 고요 | 1500 | weapon.wilderness_voice.e.03@1 | E + R90 | 진화 음파 거리 ≤1.2 밀침 +0.35 |
| `weapon.wilderness_voice.e.05` | 광야의 대합창 | 1800 | weapon.wilderness_voice.e.04@1 | E + R150 | 정예 원본 처치 후 3초 안에 예약되는 첫 기본 메아리의 피해 계수 +0.15, 대기 6초 |

## 엘리야의 불수레

### 기초

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.elijah_chariot.base.01` | 불수레 숙련 | 150 | attack@1 | 무기 해금 | 해당 무기 피해 +10%p |
| `weapon.elijah_chariot.base.02` | 불수레 신속 | 250 | weapon.elijah_chariot.base.01@1 | — | 해당 무기 주기 대기부 -5%p |
| `weapon.elijah_chariot.base.03` | 불수레 행렬 | 350 | weapon.elijah_chariot.base.01@1 | — | 수량 +1수레 |
| `weapon.elijah_chariot.base.04` | 넓은 차체 | 420 | weapon.elijah_chariot.base.03@1 | R03 | 충돌 폭 +10%p |
| `weapon.elijah_chariot.base.05` | 빠른 출발 | 580 | weapon.elijah_chariot.base.04@1 | R09 | 출발 지연 -0.08초(최소 0.05초) |

### 돌진 경로

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.elijah_chariot.a.01` | 긴 돌파 | 450 | weapon.elijah_chariot.base.01@1 | R03 | 이동 거리 +12%p |
| `weapon.elijah_chariot.a.02` | 큰 군세 조준 | 650 | weapon.elijah_chariot.a.01@1 | R09 | 거리 8 내의 최대 밀집 방향 우선 |
| `weapon.elijah_chariot.a.03` | 정예 관통 | 900 | weapon.elijah_chariot.a.02@1 | R30 | 거리 8 내 정예·보스 우선 |
| `weapon.elijah_chariot.a.04` | 완만한 선회 | 1200 | weapon.elijah_chariot.a.03@1 | R60 | 진행 절반 시 가장 가까운 군집 방향으로 최대 12° 1회 선회 |
| `weapon.elijah_chariot.a.05` | 돌아온 불말 | 1500 | weapon.elijah_chariot.a.04@1 | R120 | 다섯 번째 주기 첫 수레의 종료점에서 35% 역방향 수레 1개 |

### 행렬

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.elijah_chariot.b.01` | 넓은 대열 | 450 | weapon.elijah_chariot.base.01@1 | R03 | 평행 중심 간격 +0.30 |
| `weapon.elijah_chariot.b.02` | 모이는 대열 | 650 | weapon.elijah_chariot.b.01@1 | R09 | 단일 보스에게 각 수레 도착점을 수렴 |
| `weapon.elijah_chariot.b.03` | 시간차 돌진 | 900 | weapon.elijah_chariot.b.02@1 | R30 | 수레 인덱스당 0.12초 시간차 |
| `weapon.elijah_chariot.b.04` | 마지막 수레 | 1200 | weapon.elijah_chariot.b.03@1 | R60 | 원본 행렬 마지막 수레 피해 +10%p |
| `weapon.elijah_chariot.b.05` | 교차 행렬 | 1500 | weapon.elijah_chariot.b.04@1 | R120 | 여섯 번째 주기 마지막 수레를 첫 수레 진행 방향과 90°로 교차, 한 대뿐이면 35% 교차 수레 추가 |

### 화염 바퀴

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.elijah_chariot.c.01` | 바퀴의 불씨 | 450 | weapon.elijah_chariot.base.01@1 | R03 | 원본 적중 시 반경 2.5 다른 적으로 속력 6·25% 불씨, 대기 1초 |
| `weapon.elijah_chariot.c.02` | 시작의 불꽃 | 650 | weapon.elijah_chariot.c.01@1 | R09 | 출발 지점 반경 0.8·1.5초·25% 불자리 |
| `weapon.elijah_chariot.c.03` | 끝의 화염 | 900 | weapon.elijah_chariot.c.02@1 | R30 | 자연 종료 위치 반경 1·35% 폭발, 대기 2초 |
| `weapon.elijah_chariot.c.04` | 불길의 폭 | 1200 | weapon.elijah_chariot.c.03@1 | R60 | 경로에 폭 0.7·1초·20% 흔적, 진화 흔적 폭 +15%p |
| `weapon.elijah_chariot.c.05` | 태우는 행렬 | 1500 | weapon.elijah_chariot.c.04@1 | R120 | 한 수레 원본 ≥6적중이면 그 마지막 폭발 계수 +0.15 |

### 바람

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.elijah_chariot.d.01` | 돌파의 바람 | 450 | weapon.elijah_chariot.base.01@1 | R03 | 수레 전방 밀침 +0.30 |
| `weapon.elijah_chariot.d.02` | 뒤따르는 바람 | 650 | weapon.elijah_chariot.d.01@1 | R09 | 원본 수레 뒤 0.5초에 폭 0.6·25% 바람 1회, 대기 2초 |
| `weapon.elijah_chariot.d.03` | 넓은 여파 | 900 | weapon.elijah_chariot.d.02@1 | R30 | 바람 폭 +20%p |
| `weapon.elijah_chariot.d.04` | 흩어지는 화살 | 1200 | weapon.elijah_chariot.d.03@1 | R60 | 수레 접촉 적탄 제거 20%, 대기 2초 |
| `weapon.elijah_chariot.d.05` | 폭풍의 진입 | 1500 | weapon.elijah_chariot.d.04@1 | R120 | 다섯 번째 주기의 첫 수레에 뒤따르는 바람을 한번 보장하고 계수 +0.15, 같은 수레에 바람은 한 번만 적용 |

### 진화 연계

| ID | 이름 | 랭크별 골드 | 선행(모두 필요) | 영구 진행 | 효과 |
|---|---|---:|---|---|---|
| `weapon.elijah_chariot.e.01` | 큰 회오리 | 850 | weapon.elijah_chariot.base.01@1 | E | 진화 회오리 반경 +15%p |
| `weapon.elijah_chariot.e.02` | 긴 불길 | 1050 | weapon.elijah_chariot.e.01@1 | E + R15 | 진화 불길 지속 +20%p |
| `weapon.elijah_chariot.e.03` | 불과 바람의 교차 | 1250 | weapon.elijah_chariot.e.02@1 | E + R30 | 서로 다른 진화 경로가 0.25초 내 교차 시 반경 1·40% 회오리, 대기 3초 |
| `weapon.elijah_chariot.e.04` | 거센 승천 | 1500 | weapon.elijah_chariot.e.03@1 | E + R90 | 자연 종료 지점 반경 1.3·40% 불꽃, 대기 3초 |
| `weapon.elijah_chariot.e.05` | 승천의 행렬 | 1800 | weapon.elijah_chariot.e.04@1 | E + R150 | 정예 원본 처치 시 6초 내 첫 진화 행렬 수량 +1, 대기 6초 |
