# 창세기 콘텐츠 데이터 계약

`content/genesis/catalog.json`은 기획 문서를 전사한 **밸런스 초기 데이터**다. 라운드 2~9의 데이터가 있어도 해당 게임 로직·몬스터 행동·보스·플레이 화면이 구현되었다는 뜻은 아니다. `schema_version=1`, `chapter_id=genesis`, `status=design_data`다.

## 읽기 계약

- UTF-8 JSON을 사용한다. 필드명은 snake_case, 참조 ID는 소문자 ASCII snake_case다. 배열 순서는 라운드/웨이브/보스 소환 순서를 보존한다. 이름과 설명은 한국어 표시 문자열이다.
- 정수 필드에 bool·문자열·실수를 대입하지 않는다. 비율은 유한한 양수다. 중복 JSON 프로퍼티와 중복 ID는 오류다.
- `sources`는 저장소 루트 기준 원본 기획 문서 경로다. 기획 변경 시 원본과 데이터를 함께 검토하며 검사기만 바꾸어 근거 없이 수치를 승인하지 않는다.

## 객체

| 키 | 내용 |
|---|---|
| `rules` | 일반 전투 180초, 예정 생성 170초 마감, 보스 매 3라운드, 진입 2초, 지원군 상한 12, 처치 5회당 골드 1, 보상 후보 부족 시 골드 100, 슬롯/선택 개수 |
| `monsters[]` | `id`, `source_code`(문서 A1~I3), `name_ko`, `max_health`, `contact_damage`, `speed_ratio`, `behavior_id`, `behavior_ko`; d3만 `dash` 객체 포함 |
| `rounds[]` | `id=genesis_01..09`, `number`, `person_ko`, `concurrency_cap`, `spawn_budget`, `raw_xp_budget`, `completion_gold`, nullable `boss_id`/`unlock_weapon_id`, `waves[]` |
| `waves[]` | `id=genesis_01_w1..w3`, `start_seconds`, `end_seconds`, `spawn_budget`, `composition[{monster_id,count}]` |
| `bosses[]` | `id`, `round_number`, `name_ko`, `max_health`, `contact_damage`, `gold`, `attack`, `support_summons[]` |
| `attack` | `pattern_id`, `cooldown_seconds`, `telegraph_seconds`, `damage`, `projectile_count`; 0은 투사체 수 규칙이 없는 공격(공격 없음이 아님), 은잔 보스만 3 |
| `support_summons[]` | `health_ratio`, `monster_id`, `count`; 각 체력 임계치는 1회 발동, 임계치 배열 순서 보존 |
| `weapon_unlocks[]` | `weapon_id`, `name_ko`, `round_number`, `requires_boss_reward`; 0은 처음부터, 보스 라운드는 보상 확정 후 해금 |
| `evolution_pairs[]` | `weapon_id`, `passive_id`, `evolved_weapon_id`, `name_ko`, `required_weapon_level=8`, `required_passive_level=1`, `consumes_passive=false`, `replaces_weapon_slot=true` |

시간 단위는 초, 속도는 무강화 플레이어 기준 비율, 체력·피해·경험치·골드는 기획의 단위 그대로다. 몬스터 분포는 문서의 비율에 구간 예산을 곱한 정확한 **정수 개체 수**다. 현재 모든 분포가 나머지 없이 정수로 나뉘므로 반올림 정책을 만들지 않았다.

웨이브 생성 구간은 `[0,60]`, `[60,120]`, `[120,170]`의 시작/종료 경계 정보다. 동일 경계에서 생성이 중복되지 않도록 스케줄러가 각 웨이브의 개체 ID와 누적 생성 수로 관리해야 한다. 데이터는 생성 간격·동시 상한 처리의 구현을 대신하지 않는다. 170~180초는 정리 구간이며 상한 때문에 남은 생성 예산 처리의 최종 실행 규칙은 코어 구현 결정 기록과 시험을 따른다.

원시 XP는 라운드 총예산이다. `floor(raw_xp_budget/spawn_budget)`를 각 예정 개체에 주고 나머지는 체력 높은 개체부터, 동률이면 예정 생성 순서대로 1씩 추가한다. 시간 종료 시 생존/미생성 적의 XP는 지급하지 않는다.

보스는 `nephilim_tyrant`(3), `sitnah_keeper`(6), `silver_cup_accuser`(9)다. 몬스터 행동 ID와 공격 패턴 ID는 데이터 분류이며 실제 런타임 구현 여부를 뜻하지 않는다. 소환군은 최대 12마리이고 본 표의 총 소환량도 6/12/12다.

## 무기 ID

`bible`, `jawbone`, `sling`, `moses_staff`, `daniel_lion`, `fire_pillar`, `lightning`, `cross`, `wilderness_voice`, `elijah_chariot`.

대응 보조 ID: `scroll`, `hair`, `stones`, `east_wind`, `seal`, `cloud`, `thunder`, `crown`, `underwear`, `whirlwind`. 통합 시 Growth.cs의 공통 보조 카탈로그와 같은 stable ID로 맞췄으며 아이템 이름·효과·진화 조합은 변경하지 않았다.

이 파일에는 무기별 8레벨 수치/420개 스킬 수치를 중복 수록하지 않았다. 현재 작업 범위는 라운드·몬스터·보스·보상·해금·진화 연결의 데이터 계약이다.

## 검사

```sh
python3 tools/check_content.py
python3 tools/check_content.py /path/to/candidate-catalog.json
```

표준 라이브러리만 사용한다. 종료 코드 0은 데이터 검사 PASS이고 1은 읽기/스키마/참조/예산 검사 FAIL이다. 별도 후보 경로를 받을 수 있으므로 원본을 변경하지 않고 음성 시험을 실행할 수 있다. 27몬스터 수치, 27웨이브 정수 분포, 3보스 수치·지원군·주기, 10해금/진화 연결과 전체 일반 적 6210·원시 XP 18600·기본 골드 2667을 확인한다. 골드 2667은 모든 일반 예산 처치와 모든 완료/보스 골드의 합이며 지원군/선택 골드는 제외한다.

이 검사는 Unity 실행·충돌·실제 난이도·모바일 성능 검사와 별개다.
