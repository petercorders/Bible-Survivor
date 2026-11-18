# VQ03 책 머릿장과 출전 띠 독립 코드 리뷰

**판정 — 최종 소스·앱 결합 PASS. 출시를 막는 HIGH/MEDIUM 소스 결함은 발견하지 않았다.** 기준은 `796ecfed4a7506dd758b4b7c435e4ff0b15924f8`이다. U2 `ctx_3afe6a40a285`의 최종 `handoff.md`를 읽고 `binding.json`의 제품 변경 18개 전체와 격리 QA 사본을 직접 해시 대조했다. 격리 Player 실행 파일·`Adam.Unity.dll`·`BibleSurvivor.Core.dll`의 실제 해시도 결합 기록과 일치한다. Core DLL은 기준 커밋의 제품 사본과 byte-identical이다. `binding.json` SHA-256은 `2b9798807c5507477d5edf42c2c34df23415480c3084582e065a4417d6562c37`, 최종 `Adam.Unity.dll`은 `220812196d49490652712290c9b0fe6783f852984b87eedd1e90d392a6334325`다.

## 모델 확인

- 요청 및 실행 설정 — Orca Dispatch `ctx_f79dd3c7e689`의 launch requested와 effective에 각각 `gpt-6.2-sol / medium`을 확인했다. Orca 세션 ID는 `ecf4e7fb-589c-4ef3-a1c4-bcc08a013baa`다.
- 실제 Codex 실행 — 세션 `01a1ed7a-8f6f-7283-968f-31f865e28d98`의 첫 `turn_context`(2026-11-18 07:06:36.240 UTC)에 `gpt-6.2-sol / medium`이 기록됐다. 첫 소스 확인에서 로컬 Host 전환 기록과 외부 Session 교체 시 초기화 경로를 구분했고 실제 모델과 함께 코디네이터에 보고했다(`msg_cd4bc1896cfe`). 모델 대체는 관측되지 않았다.

## 소스 검토

| 범위 | 확인 결과 |
| --- | --- |
| 세션 표시 | `AdamBookTitle.cs:10–16,37–42`는 실제 `Host.Session?.RoundNumber`로 책과 책 내 회차를 결정한다. null이면 창세기 미진입 9칸과 회차 문구 없음. 해당 책의 이전 회차만 cleared, 실제 회차 current, 이후 ahead이며 창세기 3·6·9와 출애굽기 3·6은 상태와 무관하게 마름모다. 사망 화면도 종료된 실제 세션을 사용한다. |
| 입력 | `AdamGame.cs:113–129,179–204,413–438`의 방향키→숫자/Enter 경로에는 띠 타이머나 애니메이션 대기가 없다. 같은 프레임의 중복 명령은 `commandFrame`, 키의 재진입은 입력 edge 및 실제 Host 상태가 제어한다. 마우스 이동·드래그·다운에서만 포인터 포커스를 갱신하고 실제 카드 rect가 시각 오프셋과 독립적으로 즉시 클릭을 받는다. |
| 리소스와 폴백 | 승인받은 외부 `art-visual-quality-03-frontispiece/RESULT.md`, `tokens.json`, `production/manifest.json`, `art-readiness-reviews/visual-quality-03-frontispiece-review.md`를 대조했다. 제품의 manifest 및 PNG 두 개는 외부 승인 해시와 일치하고 기존 Book03 crest를 재사용한다. manifest SHA `9e36c6c12cd2fff0699e8eb798552f9ddec8ab23aaf306b6ccc2f18d245cd24e` 및 정확한 두 ID를 고정했다. runtime의 세 clip gate는 horizon·fade·crest가 모두 등록된 경우만 새 타이틀/출전 띠를 연다. 이전 UI는 해당 optional 묶음 전체가 없을 때 적용된다. manifest가 설치된 불완전 묶음은 기존 import 검증처럼 오류로 드러난다. |
| 크기와 알파 | `AdamBookTitle.cs:17–34`는 x0..300 잉크를 유지하고 x≥300에서 400×55의 책별 프레임을 x2로 오른쪽 고정한다. 폭 660에서 origin −140 및 초점 x520, 폭 1100에서 origin300 및 초점 x960이다. 더 넓을 때 해당 프레임의 [0,8) 열을 16px 너비로 반복하고 별도 32px fade를 x300에 한 번만 그린다. horizon은 opaque·pivot(400,0) 두 프레임, fade는 binary·pivot(0,0) 한 프레임이다. importer의 Point/Clamp/PPU32/FullRect·무 mipmap/압축 및 기존 표시 캐시 생명주기를 따른다. |
| 색상 | `AdamBookTitle.cs:50–51`의 최종 두 식은 solid fill용 raw Lapis/Gold를 기존 `AdamHud.GuiColor`에 한 번 통과시킨다. 이미 변환된 Ink 계열을 다시 변환하지 않으며 glyph 및 sprite 경로는 기존 입력을 유지한다. horizon은 기존 Linear IMGUI texture 캐시를 한 번 거친다. 해당 경로의 `.linear.linear`는 기존 두 encode를 상쇄하기 위한 것으로, 이번 draw에서 별도 보정을 추가하지 않았다. |
| 출전의 발생과 저장 | `AdamGame.cs:328–358`에서 Start·Tick·선택·재개·출애굽기 진입·저장 재시도 전후를 실제 Session 참조와 run/round로 비교한다. Initialize 및 외부 Reload는 대기/활성 출전 기록을 초기화한다. 실패한 진입은 플레이가 가능해질 때까지 보류하며, 출애굽기 후보는 기존 Host의 durable readback 이후에만 세션으로 들어온다. 기존 저장/재시도/불러오기 및 입력 해제의 호출 순서, Core·Host·save schema·타이밍을 보존한다. |
| 표시 및 우선순위 | `AdamSortieRibbon.cs:14–29`는 실제 플레이가 열린 시점부터 unscaled 표시 경과를 누적한다. 기본은 30Hz 폭 단계로 0–.2초 확장, .2–1.4초 고정, 1.4–1.6초 수축, lowFX는 즉시 완성 상태에서 .8초 종료다. 열린 pause/modal 또는 boss는 표시를 숨기고 이미 전달한 출전은 되살아나지 않는다. `AdamArtWorld.cs:251,461–494`에서 기존 배우/무기 위에 hook을 실행한 뒤 optional band와 의무 danger·charge·hostile projectile·chain·chariot 경고를 그대로 그린다. `AdamGame.cs:365`는 arena group의 좌표를 보정해 660의 (180,136)→(160,20), 1100의 (400,150)→(380,34)로 그린다. boss HUD는 별도 상위 draw에 남는다. |

두 가지 작은 디자인 차이를 기록했다. 출전 띠의 pips y는 이전 공통 token의 +40 대신 +52이며, 기본 마지막 .2초에 표면/룰이 수축하는 동안 crest·글자·pips는 같은 자리에서 유지되다가 만료 프레임에 사라진다(`AdamGame.cs:368–379`). 실제 동작을 프레임별 디자인 승인으로 합산하지 않았다.

## 확인한 실행 증거

아래 근거는 저장소 내 `evidence/visual-quality-03-frontispiece-20261118/`의 기존 실행을 읽어 재사용했다.

- **고유 18건 PASS.** `related.xml/log`는 14 PASS 및 1 FAIL, 종료 2다. 실패는 EditMode에서 Initialize를 재호출해 런타임 음성 정리의 `Destroy`를 실행한 시험 배치 문제다. 동일 동작을 PlayMode로 옮긴 `lifecycle.xml/log` 1 PASS, 종료 0과 `input.xml/log`의 키보드 포커스·Enter 해제, 즉시 포인터 카드, 작은 모달 재개 3 PASS, 종료 0을 확인했다. 총계는 14+1+3이다. `fallback.txt`는 격리 QA의 Editor에서 Frontispiece03 또는 Book03을 각각 빼고 기존 UI로 돌아간 후 복원한 증거다.
- **최종 macOS Player PASS.** `build.log`, `player-commands.json`, `path-probe.log`, `capture.log`, `capture/facts.txt`, `capture/complete.txt`, `restoration.json`에서 격리 build, probe PID 34428·capture PID 34431 종료 0 및 자기 QA 저장 경로 복원을 확인했다. 실제 숫자 3 입력은 sling을 선택했다. 무세션·두 책 사망, 새 Genesis 및 실제 R10 진입, Reload 후 재출전 없음, lowFX 0/.79초 표시·.81초 종료, Core pause 고정 및 재개 후 미재생, boss/오류 모달 우선, 주입한 자기 QA 저장 오류의 UI 재시도를 확인했다. 제어된 draw 사이 World와 저장 bytes 불변 assertion이 포함된다.
- **네이티브 일반 입력 재사용.** 이전 두 색 보정 전의 `native-normal.json/log`는 일반 Player PID 33281에서 실제 Right로 포커스 0→1, Enter로 실제 jawbone 선택, 두 번째 Enter 뒤 같은 run ID를 보여 준다. 종료 −15는 소유 PID의 의도된 종료이며 자기 세 파일 absent 상태 복원이 기록됐다. 초기 렌더 대기 실패는 `native-startup-probe-failure`에 보존돼 있다. 보정 뒤의 native 숫자 3 증거와 이 Right/Enter 증거를 구분했다. 포인터 근거는 PlayMode의 실제 IMGUI 경로다.
- **최종 색·우선순위.** `pip-before-correction.json`의 gold 255/245/208 및 lapis 194/211/236이 `pip-after-correction.json`에서 기대 255/232/160 및 138/166/214로 각각 일치했다. `horizon-runtime-probe.json`의 660/1100/1400 표본은 crop/anchor/반복을 뒷받침한다. 밝은 표본과 달리 Genesis leaf 56/84/44→56/84/42, Exodus dark 28/34/45→6/25/43(최대 채널 차 22)의 텍스처 편차가 관측됐다. 지평선 색의 정확한 재현은 PASS로 판정하지 않았다. `hook-layer-probe.json`은 lowFX에서도 실제 저장된 세 boss 방향의 흰 경고가 불투명 진단 hook 위에 그려졌음을 보여 준다.
- **최종 변경 범위.** 기존 시험 및 일반 네이티브 증거 이후 런타임 차이는 위 solid fill 두 식이다. `meta-correction.json`에서 최초 import 전 신규 GUID 하나를 정상 길이로 고친 기록, 시험 소스와 XML에서 lifecycle의 PlayMode 이동을 확인했다. 최종 앱과 U2 handoff는 모두 도착했으며 최종 소스 결합 대기는 없다.

모바일·성능·입력 지연 측정·일반 장시간 플레이·독립 심미 평가는 이 검토에서 실행하지 않았다. 제품, Unity Editor, 앱, 사용자 저장에는 개입하지 않았다. 이미지와 영상은 출력하거나 게시하지 않았다.

## 자기 사용량

Codex 세션의 첫 usage 이벤트(07:06:43.600 UTC)는 input 20,093 / cached 7,296 / output 218 / reasoning 0 / total 20,311이다. 종료 직전 관측 이벤트(07:18:54.985 UTC)의 세션 누계는 input 1,466,172 / cached 1,357,696 / output 10,117 / reasoning 735 / total 1,476,289이다. 두 이벤트 사이의 차이는 input 1,446,079 / cached 1,350,400 / output 9,899 / reasoning 735 / total 1,455,978이다. 캐시는 input에, reasoning은 output에 포함된다. 첫 이벤트 이전 및 마지막 이벤트 뒤의 보고 사용량은 이 차이에 포함되지 않는다. 실제 청구액·환산·절감률은 미계산이다.
