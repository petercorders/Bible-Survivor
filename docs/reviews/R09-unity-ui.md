# R09 최종 Unity 대표 화면 독립 UI 검토

2026-11-17 · **PASS — 지정 PC 대표3화면의 관측 가능한 UI 범위**. 막는 HIGH/MEDIUM 없음. 기존 본인 오프라인 8PNG/27frames 아트 QA는 재사용한다.

`evidence/r09-unity-host-20261117/handoff.md`와 `docs/progress/R09-unity.md`를 읽고 `final-review/capture/` 지정3장을 view_image로 직접 확인했다. 최종은 교정 후 정상 빌드한 `final-review/build/R09Qa.app`이라는 인계 범위를 유지한다.

| 대표 원본 | 직접 관측·판정 |
|---|---|
| `01-660-judah-chain-warning-lowfx.png` | PASS. ‘창세기 / 유다’, ‘은잔의 참소자 16000/16000 · 사슬 3방향 예고’, HP bar가 읽힌다. ‘F6 효과 낮음’에서 검정/흰색 세 선과 방향 화살표가 별도로 펼쳐지고 배우 위/상단 HUD 아래에서 보인다. 원형 slam이 아니다. 일부 무기명 글자와 선이 같은 전투영역에 놓이지만 방향/보스 상태를 막는 수준은 아니다. |
| `02-660-judah-live-chain-projectiles.png` | PASS(표본 존재/예고와 구분). 직선 예고가 사라지고 보스 가까이에 밝은 금속 고리/사슬 조각들이 보인다. ‘사슬 5초 주기’ 안내로 공격 단계가 구분된다. 출발 직후라 탄들이 서로/보스에 가깝고 작아 이 한 장만으로 세 탄 각각의 궤적·동작이나 혼잡 시 식별을 충분히 보증하지 않는다. 그러나 선을 계속 피해영역처럼 남기거나 탄이 전혀 보이지 않는 차단 결함은 관측하지 못했다. |
| `03-1100-genesis-complete.png` | PASS. ‘창세기 완료 · 진행 저장됨’, ‘출애굽기 준비 중 · 다음 책 미구현’, ‘진행 유지 · 새 도전 선택 시 성장·장비 초기화’가 읽힌다. 각 버튼도 ‘새 도전’으로 명시되어 완료 즉시 자동 초기화/다음 책 진입과 구분된다. 세 시작 무기 선택지는 새 도전 버튼이며 보존 중인 현재 장비 목록으로 오독하지 않는다. |

은잔 보스명·HP16000 표기는 직접 확인했지만 동적 규칙 소스/모든 보스 상태 검증은 코드 리뷰·개발자 시험의 범위다. 완료 화면은 명시 선택 전 진행 유지라는 안내를 확인한다. 실제 장비/폼·receipt 보존, 새 프로세스 Load와 명시 새 도전의 상태 전이는 개발자 실행 근거를 재사용하며 정지 이미지로 증명한 것으로 세지 않는다. 일반 I3 실제화면 가독성 및 전체 프레임은 이번 표본에서 관측하지 않았다.

## 검증·실패 이력 구분

개발자 Edit136은 **초기135 PASS + importer 교정 후 실패했던1개 PASS의 합**이며 단일 최종 소스의 전체136 재실행이 아니다. Play31은1회, Texture2D 명시 importer 교정/재import와 정상 BuildPipeline2회·preflight 실패를 구분한다. 실패 XML·첫 앱 QA pulse clock 오류·scene/identity guard 실패는 보존 이력이며 최종 PASS에 섞지 않는다. 최종 앱 capture/Reload exit0 뒤 cleanup wrapper exit1, 이후 자기 저장만 정확히 복구한 follow-up exit0도 별도 인계 사실로 남긴다. 본인은 관련 앱/시험/복구를 실행하지 않았다.

예고/적탄 Load·threshold6/12·직접kill 뒤 탄clear/350·턱뼈진화·정산 pending0/Choose0·완료160·3receipt·새 process Load/명시 새 도전은 **개발자 근거 재사용**이다. 앞 일반성장1pending/1Choose와 보상후0/0을 구분한다. HP/장비 준비·timeout·direct damage/kill 앱 fixture를 실제9round 좌표CLI나 자연 보스전·완주로 합산하지 않는다. 앱에서 보스gold100 선택도 관측한 것으로 세지 않는다.

## 범위와 종료

**NOT_RUN:** native 입력/focus, 모바일·기기, 성능, 자연 플레이/밸런스·앱9라운드 완주, 전애니메이션/일반 I3·전군집 가독성, 실제 적탄 궤적/충돌·cleanup 독립 실행. 아트는 Codex 스크립트 픽셀 제작이며 Claude/Design 미사용이다. 본 QA도 Claude 호출 없음.

새 캡처·앱/GUI/Editor 실행·사용자저장/보호identity 조회조작·전픽셀/hash·재빌드/시험 없이 문서·지정이미지 읽기와 소유 보고서만 작성했다. 자체usage 별도 미계측·타 담당 수치 합산/비용 추정 없음. 제품/원본 수정·필수 수정 ID 없음. root 최종 코드 리뷰/GC/통합·CI는 별도이며 후속 책/새 무기는 시작하지 않았다.
