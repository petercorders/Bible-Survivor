# R02 적 3종 — Claude Design 제품 제작 인계

사용자의 지정 Claude Design 협업 및 창세기 지속 개발 승인에 따른 독립 아트 작업이다. Unity 구현/GUI/시뮬레이터/사용자 저장은 수정·실행하지 않는다. 파일 소유는 별도 원본 폴더 `/Users/gimgibong/bible-survivors-docs/art-genesis-r02/`이며 저장소 제품·검토 문서는 root가 별도 통합한다. 기존 Art01/02/03/font 산출물을 변경하지 않는다.

먼저 실제 Claude Design 제품 연결과 입력/출력 기능을 확인한다. Anthropic 공식 [시작 안내](https://support.claude.com/en/articles/14604416-get-started-with-claude-design)는 현재 Claude Code의 `/design`, Artifacts의 Design 템플릿과 대화 내 Design을 제공 경로로 설명한다. 이 문서는 기능 존재의 근거이고 현 세션 연결 증거는 아니다. 사용 가능한 제품 경로에서 실제 실행·artifact/template/출력 내역이 확인된 경우만 Design 사용으로 기록한다. 일반 Claude Code가 수동 격자/Pillow/SVG를 만들거나 캔버스에 올린 것만으로 제품 실행이라고 보고하지 않는다. 연결이 없으면 확인한 실패와 구체 브리프/출력 계약·미연결 상태를 남기고 root에게 보고한다. 유료 구매/추가 구독은 하지 않는다.

## 기준·세 적

실제 게임 기준은 `/Users/gimgibong/Bible-Survivor/art/art-concept-03/`와 원본 `/Users/gimgibong/bible-survivors-docs/art-production-workflow.md`, `claude-design-brief.md` 및 저장소 `docs/harness/unity-r02-plan.md`의 시각 인계다. 콘텐츠 권위는 저장소 `content/genesis/catalog.json`이며 새로운 전투 수치/돌진/탄막/분열을 추가하지 않는다. 사람 가인·아벨 자체를 적으로 그리지 않는다.

| stable ID | 성격·구분 | 초기 납품 후보 |
| --- | --- | --- |
| enm_b1-envy-eye_move_32 | 시기의 눈. 플레이어를 향하는 동공·눈 실루엣의 추적 군집 | 투명32×32×4프레임 가로 시트,8fps loop |
| enm_b2-rage-hound_run_32 | 분노의 사냥개. 낮은 몸·귀/턱/발의 빠른 달리기 실루엣, 실제 돌진 없음 | 투명32×32×4프레임 가로 시트,8fps loop |
| enm_b3-cracked-mask_move_32 | 갈라진 가면. 폭 있는 가면·균열·둔중한 움직임, 분열 없음 | 투명32×32×4프레임 가로 시트,6fps loop |

PPU32/Point/no mip·오른쪽 원본/부모flipX·발 기준pivot(16,4)은 초기 계약 후보이며 실제 기준03과 맞춰 확인한다. 크기 후보가 실루엣/디테일을 해치면 이유/비교안을 보고하고 root가 판단한다. 적 몸 크기가 Core 충돌 반경을 바꾸지 않는다. 색뿐 아니라 실루엣·동작으로 구분되며 현재 플레이어6레이어/뱀·나방·가시마물의 명암/픽셀 밀도·배경 대비와 조화돼야 한다.

## 산출물·검증

제품 시안/링크·실행 증거, 원본 편집 가능한 자료, PNG 프레임/시트, 실제 rect/pivot/fps/loop/방향/레이어/alpha/해시/출처·권리 고지의 별도 manifest를 준비한다. 제품에 게임용 PNG 내보내기가 없으면 네이티브 출력과 실제 변환 경로를 구분하고 변환 도구/원본/재생성 바이트 비교를 함께 남긴다. 임의 그림을 제품 산출물로 바꾸어 부르지 않는다.

먼저3종 단독 확대/실제32px, 같은 카메라 적40/50 혼합 배치·그림자/배경 대비, 걷기 루프·flip/발접지·피격 tint/기존파티클 겹침을 비교한다. PNG 정적 비교·프레임 타이밍·Unity 실행·모바일 실물은 각각 구분한다. 원본 격자/시트/manifest 불일치·없는 폰트/참조·잘못된좌표계/레이어/frame layout을 실패로 검출할 수 있도록 계약을 검사한다. 검사기 오류 주입은 QA가 독립적으로 수행할 것이며 제작자 결과와 합산하지 않는다.

root가 독립 아트 검수→필요 수정/같은 담당 재검수→별도 R02 sidecar 통합을 맡는다. 작업자는 리뷰 파일/Unity 제품 파일/commit/push를 수정하지 않으며 추가 worker를 생성하지 않는다. 결과·명령/exit·실제로 쓴 제품/모델/변환 도구·미실행·원본 보존을 원본 폴더 RESULT.md에 남기고 root에 보고한다. usage가 없으면 미측정으로 기록한다.
