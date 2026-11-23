# Steam 통합 출시 QA #49

**출시 보류.** 지정 후보에서 R09 완료 후 숫자키 `1`로 기존 도전이 교체됐다. PlayMode 기준 실패 두 건도 남아 있다. 이 보고서는 독립 QA 실행 결과이며 릴리스 승인은 포함하지 않는다.

## 대상과 실행 환경

- 로컬 검증 시작 HEAD, 원격 `refs/heads/issue/47`, `refs/pull/48/head` 및 열린 Draft #48의 head는 모두 `93def30331be719bcf356f137618cb0f634624e1`이다. `git ls-remote`와 `gh pr view 48`로 확인했다.
- Draft #44의 `3405908d68f7c3b29a7554ac3b4f463387f495d2`를 대상으로 `git merge-base --is-ancestor`가 1을 반환했다. 두 변경을 함께 포함한 UI 회귀는 **NOT_RUN**이다.
- Unity 6000.6.9f1, macOS 26.5.1 (25F80), Mac16,11, 24 GiB, 키보드 입력 상태 주입과 별도 macOS 배경 창 입력. 제품 체크아웃에서 표준 명령 `AdamProject.BuildMac`으로 Development Player를 빌드했다. 같은 격리 프로젝트를 사용해 별도 저장 경로, 입력 주입, 캡처 및 측정 코드만 더한 두 번째 QA Player에서 실행했다. 아래 같은 빌드 실행은 최종 QA Player 한 바이너리를 사용했다.
- 체크아웃, 표준 Player, 최종 QA Player와 별도 자연 경로 실행기의 Core DLL SHA-256은 `b6b82856cca03c8110355d63be9e3051589ecfd42364981e2f831fd12517be63`이다. Steam Build ID는 미발급/미확인이다. 설치나 소매 성능 판정에는 Steam으로 설치한 후보가 필요하다.
- #45 및 Draft #46의 추가 일곱 항목을 기준으로 삼았다. #48의 SHA가 찍힌 독립 검토 기록에는 보조 상한·보스 진입, 사자 배치, 같은 위치의 조준 경계와 해당 변이가 정리돼 있다. GitHub 승인 리뷰는 없으며 자동 작성 검토 기록과 CI는 출시 승인 수에 포함하지 않았다. [#45](https://github.com/petercorders/Bible-Survivor/issues/45) · [Draft #46](https://github.com/petercorders/Bible-Survivor/pull/46) · [Draft #48 독립 검토 기록](https://github.com/petercorders/Bible-Survivor/pull/48#issuecomment-7095392890)

## 재현된 결함

| ID | 심각도 | 조건과 재현 | 기대와 실제 | 증거 |
| --- | --- | --- | --- | --- |
| QA49-01 | 높음 · 출시 차단 | 완료된 R09의 이전 형식 저장 사본 또는 스키마 4 v2 준비 저장을 macOS QA Player 1100×720에서 로드한다. 출애굽기 진입과 새 도전이 함께 표시될 때 숫자키 `1`을 한 번 누른다. | 현재 도전을 덮어쓰기 전에 사용자가 명시적으로 확정할 수 있어야 한다. 실제로 확인 화면 없이 무기가 회전 성경인 R01과 새 RunId가 저장되며, 1,425골드(이전 형식) 또는 1,432골드(v2)는 유지된다. 동일 원본에서 `Enter`는 기존 RunId와 골드를 유지하며 R10을 저장한다. | `player-b/chapter.facts`, `r09-before-digit1.png`, `r09-after-digit1.png`, `r09-risk.json`, `r09-continue.json`. 스키마 4 v2 재현은 `player-d/chapter.facts` 및 대응 저장. |

숫자 입력은 `AdamGame.Update`에서 곧바로 `SelectSlot`을 호출한다. 책 완료 상태에서 `AdamHost.CanStartNew`가 참이므로 `SelectSlot`이 `Host.Start`를 호출한다. #45에서 보고된 유형을 현재 SHA에서 재현했다. 재검증 시 #44를 포함하는 별도 승인 후보에서 숫자키 1–3과 클릭, 취소, 확정, `Enter`의 RunId·라운드·장비·골드·완주 영수증을 대조한다.

## 일곱 항목 실행 결과

| 항목 | 실행한 범위와 결과 | 남은 확인과 통과 조건 |
| --- | --- | --- |
| 첫 플레이 관찰 | PASS · R01 v2 시작과 복원, 가속한 실제 전투 틱에서 첫 성장에 도달해 숫자키로 선택, 스킬 구매 취소와 확정, 유도 사망과 재도전, `Enter` R09→R10, 준비 저장으로 R180 완주 화면과 새 도전 후 완주 영수증 보존. FAIL · QA49-01. | BLOCKED · 초심자·숙련자 무안내 관찰. 첫 성장·책 전환·구매/취소·사망 복귀·완주 후 행동에서 오해를 기록하고, 의도치 않은 새 도전 0건을 확인한다. |
| 절전·계정·강제 종료 | PASS · R01 커밋 직후 QA 프로세스 SIGKILL, 중단 전후 저장 바이트 동일 및 동일 RunId 재시작. 수동으로 주입한 focus/pause 콜백에서 전투 시간과 이동이 멈췄고, 이동키를 뗀 뒤 Enter로 재개했다. QA 주입 없이 시작한 같은 Player에서 구매 확인 전 SIGKILL 후 1,000골드·0랭크, 구매 확정 후 SIGKILL과 재시작에서 900골드·1랭크 및 동일 영수증을 확인했다. R09 완료/R10 전환, R180 보상 선택 전후, 완주 정산 후에도 같은 방식으로 중단·복원했다. | BLOCKED · 물리적 절전/최대절전, OS 포커스와 OS·Steam 계정 분리. 보상·구매·책 전환·완주에서 실제 절전/복원을 재현하고 마지막 확정 상태와 이동키를 누른 채 복귀할 때의 입력을 비교한다. |
| 빌드 조합·장기 경제 | PASS · 420개 고유 노드/442개 유료 단계, 22개 트리, 총 378,050골드. 재실행 Core 626/626에 전체 구매·재로드·환불, 두 6슬롯 기본/진화 체크포인트 및 R180 제어 정산을 포함한다. 동일 Core DLL의 고정 시드 3개가 R01–R30 정산을 마쳤다. FAIL · #47의 시드 7 자연 완주 기준은 R159 사망으로 미충족. | NOT_RUN · 전체 442단계 개별 효과 인수와 대표 2요인 전표, 다중 시드 R180 장기 경제, 투자·환불 반복과 모든 상한을 장기 자연 경로에서 검증하는 작업. |
| 접근성·언어·엔딩 | PASS · QA Player에서 F7 충격음 음소거, F6 아트 효과 감소, F9 피격 파티클 토글 상태와 대표 한국어 구매·취소·결과 화면을 확인했다. R180 완주 화면과 기록은 추가로 검증했다. | NOT_RUN · 게임 전체 무음 상태의 단서, 최장 노드명·설명, 지원 언어 전체, 후기 최악 섬광·흔들림, 접근성 실사용. 대상 빌드는 별도 엔딩 컷신 없이 완주 화면을 표시해 컷신 조작은 적용 제외. |
| 화면·입력 장치 전환 | PASS · QA Player에서 640×400 요청이 최소 660×440으로 보정됐고, 1280×800 및 1512×648 창에서 구매 결과 대화상자의 글자·확인 버튼과 선택 윤곽을 확인했다. QA 주입 없이 시작한 같은 빌드에 macOS 배경 입력으로 K/Esc를 보내 상점 열기와 닫기를 화면으로 확인했다. 좌표 클릭은 화면 상태가 바뀌지 않아 미확인이다. | BLOCKED · Windows 배율·보조 모니터와 지원 입력 장치 전환/재연결, 실제 클릭 영역 전체. Deck 실기기 1280×800 검사는 출시 지원을 표기할 때 적용한다. |
| 최악 장면·반복 세션 | PASS · 동일 QA Player에서 R180 보스 진입·재로드 3회와 R03 442단계 투자 6무기 기본·진화를 계측했다. R180은 각 시도에서 약 5초 전투 후 사망 화면으로 전환됐다. 장기 성능은 미측정. | BLOCKED · Steam 설치 최소 사양 기기와 성능 예산. 같은 시드의 최악 조합으로 장기 연속 전투와 반복 재도전·재개를 측정하고 CPU/GPU·메모리·GC·개체 수와 공격 생략을 검증한다. |
| Steam 설치·복구 | BLOCKED · 이 환경에서 Steam 클라이언트, 앱·비공개 브랜치, 빌드 ID와 검수 계정 접근을 확보하지 못했다. | 같은 Build ID로 신규 설치, 이전 후보의 업데이트, 기존 저장 열기, 온라인/지원 오프라인 실행, 복사본 롤백을 검증한다. 외부 Playtest 운영 시 저장 격리를, Cloud 지원 시 두 기기 충돌과 복원을 확인한다. |

## 무투자 v2 플레이 정책 표본

3개 시드는 설치 카탈로그의 R01에서 물맷돌·주무기 우선(C7)으로 시작하고 R30에 정해진 표본을 끝냈다. 표는 자동 플레이의 R30 누적 잔액, 접한 보스들의 최소–최대 처치 시간, 일반 웨이브 최저 처치율이다. 세 표본에서 R30까지 10개 무기가 해금됐다. 모든 영구 스킬의 자연 해금·투자까지 검증하는 표본은 별도로 필요하다.

| 시드 | R30 골드 | 보스 시간 범위 | 일반 웨이브 최저 처치율 | 종료 |
| --- | ---: | ---: | ---: | --- |
| 7 | 18,372 | 13.75–77.78초 | 79.06% | 0 |
| 17 | 18,478 | 13.75–77.78초 | 79.16% | 0 |
| 29 | 18,327 | 13.75–77.78초 | 77.86% | 0 |

#47의 별도 장기 실행은 완성된 JSON과 로그에서 종결을 확인했다. 같은 정책의 시드 7은 R01–R158을 정산한 뒤 R159에서 사망했다. R159 일반 웨이브 413/506, 받은 피해 100, R180 보스 시간 없음, `completed=false`, `meetsCriterion=false`다. R100 이후 관측한 일반 라운드의 최저 처치율은 72.77%다. 이는 해당 자동 정책의 비교 결과다. 이번 QA가 독립 실행한 세 표본은 R30까지다.

## macOS Player 성능 표본

1100×720 Development Player에서 기본 v2 R180 보스의 기술 준비 저장(월드 시드 7)과 최대 투자 R03의 성경·지팡이·불수레·십자가·사자·불기둥 조합(월드 시드 419)을 사용했다. R180은 움직이지 않는 표본으로 세 번 재로드했다. R03은 기본과 진화의 두 기술 준비 저장을 사용했다. `Time.unscaledDeltaTime` 프레임 간격, Unity 할당 메모리의 시작·종료치, Gen 0 GC 차이, 표본 개체 수 및 원본/보조 공격 생략 카운터를 기록했다.

| 표본 | 프레임 간격 p95 / p99 | 할당 메모리 시작→끝 | Gen 0 GC | 원본 / 보조 공격 생략 변화 |
| --- | --- | --- | ---: | --- |
| R180 ×3, 각 20초 창 | 16.77–16.88 / 17.54–17.61 ms | 136.0–139.0 → 136.7–138.8 MB | 127–142 | +0 / +0 |
| R03 6무기 기본, 15초 | 16.99 / 17.58 ms | 137.7 → 137.7 MB | 133 | +0 / +73 |
| R03 6무기 진화, 15초 | 16.73 / 17.62 ms | 137.7 → 137.6 MB | 117 | +0 / +192 |

R180의 프레임 수치에는 약 5초의 전투와 사망 후 화면이 함께 들어 있다. 이 값에 R180 전투 성능 예산을 적용하지 않았다. 개체 수는 약 120개 기록 프레임마다 본 표본이며 최대 동시 개체나 메모리 피크를 확정할 수 없다. 원본 생략은 이 표본에서 증가하지 않았고 보조 생략은 R03 두 구성에서 증가했다. 상한을 넘는 모든 조합을 이 두 실행으로 판단하지 않았다. R03의 첫 시도는 R01 저장을 잘못 넣어 준비 상태 검증에서 멈췄고, 같은 Player에 442단계의 정식 구매 원장으로 만든 R03 저장을 사용해 재실행했다. `player-b/late.exit`는 3, `player-c/late.exit`는 0이다.

## 실행 명령과 증거

`evidence/issue49/`는 이 작업공간의 로컬 증거 디렉터리다. 원본 #47 증거는 `../steam-permanent-skills/evidence/issue47/`에서 읽었다.

| 확인 | 명령 또는 증거 | 결과 |
| --- | --- | --- |
| Core | `bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests` → `core.log`, `core.exit` | PASS · 626/626 · 종료 0 |
| 기준 입력 | `python3 tools/unity_check_portable.py` → `portable.log`, `portable.exit` | PASS · 종료 0 |
| 기존 근거 읽기 전용 감사 | `python3 evidence/issue49/audit-issue47.py` → `audit-issue47.log` | PASS · 원장, DLL 및 194장 매니페스트 해시, 복구 감사의 값과 경로 대조 |
| #47 EditMode | `q2-final-native/EditMode/results.xml` | PASS · 479/479 · Unity 종료 0 |
| #47 PlayMode | `q2-final-native/PlayMode/results.xml`와 `baseline-playmode/results.xml` | FAIL · 39/41, 기준본 38/40. 동일한 `AdamArtPlayTests.ActualKillAndHitEventsUseBoundedReusableSlotsAndResetOnReload`와 `AdamParticlePlayTests.ActualHitsRenderAtWorldPositionOnceAndPoolPauseLowReloadDisposeStayBounded`. 후보 Unity 종료 2, 래퍼 종료 1 |
| #48 GitHub CI | push·pull request의 `Concrete regression mutations` | FAIL · 두 작업이 모두 `weapon_kill_credit_lost: original regression location changed`로 종료 1. 이 변이의 회귀 검출은 미확인. [push 작업](https://github.com/petercorders/Bible-Survivor/actions/runs/45138205755/job/137569471568) · [PR 작업](https://github.com/petercorders/Bible-Survivor/actions/runs/45138218727/job/137569515808) |
| 구매 전후 실제 중단 | `normal-player-purchase/` · Orca 배경 키보드 입력, 대상 Player에 SIGKILL, 각 시점 저장과 재시작 화면 | PASS · 확정 전 저장 유지, 확정 후 100골드 1회 차감과 동일 영수증 유지 |
| 책 전환·완주 정산 중 실제 중단 | `normal-player-r09/`, `normal-player-r180/` · 일반 Player 배경 키보드, SIGKILL, 저장과 재시작 화면 | PASS · R09 완료 및 R10에서 RunId·무기·1,432골드 보존. R180 보상 전에는 미선택 복원, 선택 후에는 동일 보상 복원. 정산 +2,780골드 1회, 완주 RunId와 시각은 재시작 및 Enter 2회 후에도 동일. |
| #47 캡처와 호환 | `q2-final-player-retest/{manifest.json,crash-save-audit.json}`, `ranks419-native/fullhd/manifest.json`, `q2-legacy-fidelity/compare.log` | 1920×1080 34장은 지정 DLL, 160장은 이전 DLL `e63842d9f4a4cb04844369ce33f631f797078f8ef8986114ee2df33d241a6ec6`. 동일 RunId의 Q2 구매 직전/이후 19,210골드·0랭크 / 18,760골드·1랭크. 고정 v1 R01–R03 전투 비교 PASS. |
| macOS 빌드 | `AdamProject.BuildMac` → `build-mac.log`, `build-probe-final.log`, `final-player-sha256.txt` | 두 빌드 종료 0. QA 코드는 `Issue49Probe.cs`에 보존 |
| 자연 경로 | `natural-{7,17,29}.{json,log,exit}`, `InstalledNaturalGrowthRunner.cs`, #47 `q2-installed-natural/main-sling-7.{json,log}` | R01–R30 세 표본 완료. #47 장기 시드 7은 R159에서 종결한 미완주 결과. |
| 실제 QA Player | `python3 evidence/issue49/run-player.py player-b chapter shop completion crash restore lifecycle late` | 모드별 `.facts`, `.exit`, Player 로그, 저장과 캡처. chapter 모드 종료 0은 대조 실험이 끝났다는 뜻이며 QA49-01은 FAIL이다. SIGKILL 종료 코드 −9는 의도한 중단이다. R03 수정 입력 재시험은 `player-c late`, v2 R09는 `ISSUE49_USE_V2_R09=1 ... player-d chapter`로 실행했다. |

## 출시 재검증 순서

1. QA49-01과 PlayMode 실패 두 건에 대한 수정 후보를 만들고, 기준과 같은 저장·입력으로 재실행한다.
2. 실제로 #44와 #47을 포함한 승인 후보를 확정하고 일곱 항목의 테스트를 같은 Build ID에서 연결한다.
3. 확정 진행 손실·정산 중복·크래시·진행 불능·지원 플랫폼 설치 실패가 0건이어야 한다. 장기 성능, 필요한 실기기와 계정, 비공개 Steam 설치/업데이트 증거를 검토한다.
