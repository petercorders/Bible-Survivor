# R06 Core 회고

2026-11-16. R03 보스 경로를 기존 파일의 라운드 정의로 재사용했다. R03 ID/정책/fingerprint·과거 receipt를 유지하면서 현재 R06과 병존시키고 두 문턱은 optional mask로 기록했다. 새 적 투사체 엔진/과한 history 시스템/도구 프로젝트는 만들지 않았다.

관련 R06 7시험·기존 R03 boss12/reward11·대표 sling1seed 실제 bosskill·최소3변이로 검증했다. 전행렬은 반복하지 않았다. 저장된 소환 mask 유실, 단일 완료 slot 덮어쓰기, 발견한 동일 tick 지원 좌표 누락을 정확한 옛 구조로 재현해 실제 FAIL→복원PASS를 확인했다.

실제 CLI의 threshold 직후 Save가 actor/좌표 불일치를 잡았다. 보스 무기 hit로 생성된 지원 geometry가 다음 tick에야 동기화되는 기존 경계였으며, tick 끝 동기화로 최소 수정했다. 같은 tick의 실제 무기 명중·7 actor·Validate 회귀를 추가했다. 별도 개선이나 큰 리팩토링을 늘리지 않았다. 최초 신규 시험의 nullable 경고는 수정했다.

근거는 `evidence/r06-core-01/{summary.json,restored.log,cli-result.json,boss-series.log}`, 진행·미검증은 `docs/progress/R06-core.md`다. 장비8/HP10000 기술 fixture를 자연 밸런스 완료로 보고하지 않는다. 소스 고정 후 독립 Sol high 코드 리뷰로 인계하며 Unity/DLL/provenance·아트·GUI·사용자 저장·CI hooks·commit/push는 실행하지 않았다. 구현 모델은 Sol이며 phase `r06-core-01` 자기 실제 counters만 기록하고 비용/절감률은 추정하지 않는다.
