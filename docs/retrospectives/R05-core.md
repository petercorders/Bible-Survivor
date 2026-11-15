# R05 Core 회고

2026-11-16. 승인된 R05는 새 패턴 없이 기존 추적/접촉과 R04 성장 후 정산을 재사용했다. R03 보스·진화 receipt와 R04 완료 사실을 이어가기 위해 optional 이전 완료 라운드 번호만 보존했다. 새 전투상태/version/도구/helper 프로젝트를 만들지 않았다.

관련5시험·대표 sling1seed 실제좌표690킬/중간Load/between6, 최소1원구조 변이로 checkpoint를 확인했다. 정확히 옛 R04-only settlement branch로 되돌리면 R05 pending 선택보다 먼저 회복/골드를 적용하는 회귀가 실제 assertion 실패가 되며 복원5/5 PASS를 확인했다. 다른 이미 검증된 변이는 재사용하고 기존 전체 행렬을 반복하지 않았다. 기존 mutation anchor18개가 고유함만 정적으로 확인했다.

관련 시험과 CLI는 첫 실행에서 통과했다. 이후 불필요한 malformed 입력 가지를 제거하고 미래/잘못된 receipt의 실제 assertion에 집중했다. 새 개선을 덧붙이지 않고 source고정 checkpoint를 독립 리뷰에 넘긴다. 증거는 `evidence/r05-core-01/{summary.json,restored.log,cli-result.json}`, 진행은 `docs/progress/R05-core.md`다.

순수 Core 결과를 실제 앱/밸런스 완료로 보고하지 않는다. Unity/DLL/provenance·아트·GUI·사용자 저장·CI hooks·commit/push는 범위 밖으로 유지했다. Sol 모델의 phase `r05-core-01` 시작/끝 실제 session counters만 기록하며 청구액·절감률은 미측정이다. 독립 코드 리뷰와 Unity 연결은 다음 담당자 소유다.
