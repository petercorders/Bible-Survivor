# R09 Unity integration retro

2026-11-17. Three Unity authors wrote disjoint Host/UI/World areas; integrator waited for ownership return/Core-art PASS, synced once and then owned Editor/Player. Returned sources were preserved; the necessary integration correction was confined to explicit Texture2D import and a narrow reimport command.

Actual failures and recovery:

- First lookup used a nonexistent Unity path, corrected before edits. Source-only authoring proceeded under the Editor gate.
- First final Edit suite:135/136 PASS; new version1 PNG metadata became textureShape2, so no Sprite frames loaded. Explicit reimport still failed; adding Texture2D fixed that same test. Reused135 PASS; reran only failed1. Final Play31 and provenance/project/portable PASS, no full suite repetition.
- First app build succeeded, but QA driver copied a Bible pulse clock into non-Bible slots. Core correctly rejected it after valid intro (exit2). Preserve seed/failed source/trace/log/PIDs/restoration; correct only the per-slot fixture. Do not weaken validation or hand-patch the app.
- Reusing the owned QA project without resetting its scene hit its fresh-driver guard before BuildPipeline. Restore product scene; successful normal rebuild to a fresh final identity. Total2 successful BuildPipeline calls plus1 preflight failure. Final product source/Core/DLL remain fixed.
- Final no-Host probe correctly reported the new company/product save path, while the runner assumed bundle path only. Allow only either exact fresh identity path after realpath; app unchanged. No protected path inspection.
- Final capture/reload apps passed with three screens, but cleanup's all-files guard included Unity Analytics ancillary files. Preserve wrapper exit1; follow-up removes exactly own main/bak/lock to their pre-run absent state, retaining ancillary files. No unnecessary replay or build for housekeeping.
- Editor auto-added default Android/iOS entries to79 Art03 metas. Strictly remove only exact inserted blocks that recover initially clean HEAD bytes; preserve GUIDs/other content. No mobile verification is inferred.

The old R08 completion-boundary expression was replayed in the owned isolated QA project and failed an actual assertion, then the restored test passed. Source restored; final app was built from good source. Technical fixture details and PendingLevels versus candidate count versus actual Choose counts are explicit: normal1/1/1, final settlement0/0. App350 and160 are separated; no app gold100 claim.

Potential harness follow-up for root GC only: existing QA capture scaffolding should constrain optional weapon fields by actual form, reset an owned QA scene before rebuilding, accept the exact supported macOS identity path forms, and restore SaveStore's explicit files without conflating Unity ancillary telemetry. No global rule/checker/CI changes or new helper project were made. Independent final code/UI review remains root-owned; no automatic subsequent book work.

## Coordinator GC

사용자 지시에 따라 제품 작성 상한을5로 두고 Core/외부아트/UI/월드/Host의 파일 소유를 분리했다. 실제로 코어·아트가 반환된 뒤 Unity 작성3명이 병행했으며 Editor/빌드/GUI는 통합자만 실행했다. 상한5를5명상시가동으로 보고하지 않는다. 소스 독립리뷰도 통합 실행과 병행한 뒤 최종 변경부분만 대조해 같은 파일·전행렬 반복을 피했다. 병행의 동일 조건 시간/비용 비교는 미측정이다.

그럼에도 importer 기본 texture shape, 무기별 선택 필드, QA scene/실제 macOS 저장 경로·부수 analytics를 충분히 읽지 않아 실패와 추가 빌드/정리 작업이 발생했다. 실제 실패만 최소 교정하고 원FAIL XML·앱exit2·preflight1·wrapper1/후속복구0를 보존했다. 다음 개선은 기존 fixture의 유효 form/scene/명시 SaveStore 파일 계약에 한정하며 새 하네스 프로젝트·전역 규칙 승격을 추가하지 않는다. 문서/후처리 때문에 게임을 다시 실행하지 않았다.

root는 ProjectSettings의 빈 iOS/Android 아이콘 자동직렬화만 제거해 HEAD 바이트와 동일함을 assert했다. 첫 guard가 YAML 목록 dash를 빼고 비교해 쓰기 전에 거절한 뒤 정확한 빈 texture 행 집합으로 교정했다. 원 설정/사후정리 기록은 ignored evidence에 보존하고 Runtime·검증 앱·원 binding은 그대로 둔다. 전역 설정·사용자 아이콘을 변경한 것으로 취급하지 않는다.

모델은 Sol 구현/독립 Sol high 코드검토로 유지했고 사용자 요청대로 Claude/Claude Design을 사용하지 않았다. Codex 스크립트 픽셀 제작을 Design 제품 생성으로 보고하지 않는다. 실제 자기 usage의 캐시·구간·공급자 계수/effort 미측정을 보존하고 전체 청구/토큰 절감률을 추정하지 않는다. 어려운 문제가 이 범위에서 해결돼 Astra 호출도 없었다. R01~R09 대표 actual 좌표 CLI와 앱 기술 fixture·자연 완주/밸런스/native/mobile/performance 미검증을 분리한다. R09 리뷰·GC·commitpush/CI 후 후속책/추가 무기 작업 없이 종료한다.
