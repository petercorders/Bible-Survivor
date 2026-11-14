# 제품 ParticleSystem — 독립 PC 코드 리뷰

2026-11-14. **최종 재검토 PASS — MEDIUM CPT-01 CLOSED, 추가 HIGH/MEDIUM 없음.** 구현자 실행 근거를 읽고 소스·XML·CSV·저장 사본을 대조했다. 검토자는 제품/Core/Editor/앱/GUI/원본 PC·iOS 저장을 실행하거나 수정하지 않았으며 소유 파일은 이 보고서뿐이다. 아래 최초 발견과 역사적 P1 근거, 최종 카메라 수정·독점 QA 재검증을 구분한다.

## MEDIUM CPT-01 — Clear 후 빈 RT 카메라 잔류: CLOSED

위치: `UnityGame/Assets/Adam/Runtime/AdamImpactParticles.cs:113`, `AdamGame.cs:34`.

실제 입자를 방출하고 Prepare한 뒤 camera.enabled는 true다. `Clear()`는 ParticleSystem과 ParticleCount만 비우고 camera.enabled를 바꾸지 않는다. 이어 Reload한 저장에 Run이 없으면 Host.World가 null이고 ArtWorld.Update는 Snapshot을 null로 비운다. 이때 `PrepareParticles`는 Snapshot null에서 바로 반환하므로 카메라를 다시 끌 기회가 없다. Draw는 ParticleCount0에서 생략해 화면에는 드러나지 않아도 enabled RT 카메라는 계속 렌더 대상에 남는다. 직접 Clear 호출도 같은 상태를 만든다.

이는 “입자가 없거나 꺼졌으면 RT 카메라도 렌더하지 않는다”는 수용 조건과 달랐다. 최초 권고는 Clear에서 보유 카메라를 비활성화하고, 다음 유효 Prepare에서 ParticleCount/Enabled에 따라 다시 켜는 기존 흐름을 유지하는 것이었다. 최초 시험은 Clear 뒤 count만 확인하여 이 경계를 잡지 않았다. 본인은 당시 실행하지 않았으며 위 경로를 코드의 직접 상태 전이로 확인했다.

수정은 Clear에서 camera.enabled=false를 먼저 설정하는 최소 변경이다. 보강된 실제 PlayMode 두 시험은 live burst→Prepare→Clear와 Run 없는 SaveStore Reload→Snapshot null→Prepare early return을 모두 검사하고, 같은 소유 카메라가 다음 World의 실제 projectile hit에서 다시 켜지는 것도 확인한다. 최초 수정 전 두 assertion FAIL 및 정확한 원래 Clear body를 되살린 변이 exit2/두 assertion FAIL→정상 복원 exit0/두 시험 PASS를 같은 검토자가 대조하여 CPT-01을 닫는다.

## 확인한 구현 경계

- AdamGame.Arena를 스프라이트와 파티클이 공유하고 AdamWorldProjection.Point/CameraPosition에서 동일32px 격자를 계산한다. 홀수 arena는 ceil 크기의 정수 배율 RT를 마지막 arena edge에서 clip한다. Draw는 Update 이후 arena/origin이 달라진 경우 한 프레임을 생략하여 이전 RT를 늘려 합성하지 않는다.
- RT는 Point/AA1/Clamp이며 명시 orthographic size/aspect를 쓴다. 별도 layer30을 카메라가 렌더하고 기존 카메라 및 이후 SRP callback의 카메라에서 해당 bit를 제외한다. Dispose는 구독을 해제하고 자신이 처음 관측한 bit만 복원하며 RT/material/root를 해제한다. 여러 실제 카메라의 중복 렌더/다중 인스턴스 소유·장시간 resize의 GPU 메모리는 시험한 것으로 일반화하지 않는다.
- 16시스템×16=256 capacity이며 실제 프리셋 수는 그보다 작다. 새 FrameImpacts에서 한 번만 소비하고 target0·비무기 원인은 장식 입자로 바꾸지 않는다. 각 일반 burst는 빈 슬롯을 찾고 포화한 처치 burst만 낮은 priority를 교체한다. 사용자 경고는 P0 tint/FX에 남는다.
- World generation은 cursor/풀을 비운다. terminal/disabled/lowFX는 clear하며 pause/성장/background는 기존 입자 상태를 동결하고 늦은 batch cursor를 소비한다. resume 때 오래된 사건을 재생하지 않는다. OnGUI는 합성만 하며 Core 이벤트를 다시 읽지 않는다.
- 합성은 terrain/적 뒤가 아닌 위, 플레이어/무기 아래이며 모달은 이후 그려진다. shader는 원본 작은 색 파편용으로 외부 팩/샘플을 제품에 임포트하지 않는다. Core 충돌/피해/난수/저장 모델은 변경하지 않는다.
- F9는 wasPressedThisFrame로 토글하고 held 반복을 피한다. Disable은 자원을 Dispose하고 Enable은 새 소유 풀을 만든다. 구현자 PlayMode는 실제 InputSystem 장치로 이 경계를 검사한다. native 키 전달·실물 입력 PASS와 다르다.

## 독립 읽기 전용 대조

| 자료 | 직접 확인 |
| --- | --- |
| EditMode-final XML |51 passed,0 failed/skipped |
| PlayMode-handoff XML |18 passed,0 failed/skipped |
| 원래 bounds0 변이 | 실제 RT alpha assertion 실패1·exit2 |
| 원래 screen aspect 변이 | 실제 카메라 투영32.729px 오차 assertion 실패1·exit2 |
| batch 재소비 변이 | Expected2/Actual4 assertion 실패1·exit2 |
| baseline/복원 | mutation-handoff 원장의 정상0·복원0 |
| 화면/저장 비교 |660/1100/홀수/resize × 세 무기12쌍 각각 World bytes·Run/Profile 동일 |
| 실제 전체 저장 bytes | operation ID가 달라 같지 않으며 동등 범위를 숨기지 않음 |

15개 PNG 비교 기록은 arena 밖 변경0과 의도적 F9 상태 문구 제외 영역을 명시한다. 본인은660×440 실제 제품 경로의 정적 PNG도 열어 제목/HUD/arena/하단 F9 안내가 보이는 점을 확인했다. 정적 이미지 읽기는 native 실행/조작 리뷰로 합산하지 않는다. 별도 실행·전체15개 픽셀 재계산은 하지 않았으며 각 PNG 분석 원장과12쌍 저장/World 파일의 직접 대조를 구분한다.

## 성능 해석

frames.csv4조건을 직접 다시 파싱해 각각600샘플의 p95 프레임·p95 표현 CPU·관리 할당 중앙값이 performance-results와 정확히 같은지 확인했다.40/120적 양쪽에서 끔은0입자, 켬은 최소/최대 모두64입자였다. 각 켬/끔 World bytes도 같았다. 고정 Core snapshot과 delta0 표현을 반복하므로 실제 전투 진행·256입자 최악 부하를 측정한 결과가 아니다.

표현 CPU p95 차이는40적+0.3601ms,120적−0.0025ms이고 후자는 잡음이지 개선 근거가 아니다. 양쪽 할당 중앙값 증분36B/frame은 임시 가드를 통과하지만 전체 관리 할당은 약77.6KB/100.6KB 매 frame이다. 무할당 표현이나 전체 게임 예산 통과를 주장할 수 없다. ProfilerRecorder는 이전 완료 frame의 전역 관리 할당을 읽고 QA CSV 구성 등도 포함한다. 초기 입자0·할당API0의 무효 비교는 제외됨을 확인했다.

CPU 측정은 명시 Update+OnGUI Repaint 구간이며 GPU·입자 카메라 렌더 비용 전체를 분리 계측하지 않았다. 호스트 다른 앱 부하는 통제하지 않았고 켬/끔 순서·고정 장면의 한 실행이므로 장기 성능/resize 간 메모리 안정성·모바일 배터리/발열/프레임 수용은 NOT_RUN이다.

## 최초 QA 경계와 최종 독점 인계

최초 검토 시 final-binding은 재발행 전이었다. 기존 QA 원복 시점 이후 원장 밖 동일 bundle PID가 관측되어 stale revision이 발생했고, 원래 native Enter 성공/최종 QA 저장 원복 판정은 보류/INVALID였다. 검토자는 해당 프로세스를 조회·조작·종료하지 않았고 사용자 소유나 제품 결함으로 단정하지 않는다. PC 원본6개 보존은 별도 기록이며 QA 원복의 무효화와 합치지 않는다. 이전 원장 밖 프로세스/옛 QA 저장 상태가 이번 재검증으로 해결됐다고 표시하지 않는다.

최종 수정 인계는 별도 `evidence/combat-particles-cpt01-20261114/`다. 새 suffix의 bundle/product/save path를 사용하는 전용 앱만 유효하며 기존 build-final/build-profile 앱과 구분한다. 실제 Enter/입력에 따른 진행은 도구가 focus를 돌려 정지시킬 수 있어 최종 인계도 NOT_RUN/미귀속으로 남겼다. 아래 실제 Commit/Host Load/종료/새 QA 저장 원복 근거와 입력 귀속을 혼합하지 않는다.

| 최종 직접 대조 | 결과 |
| --- | --- |
| final-source-sha + manifest hash | **320/320 현재 파일 일치**, manifest SHA도 final-binding과 같음 |
| 이전 source-before320 대조 | 변경2개뿐: Clear 최소 수정과 기존 PlayMode 파일의 두 회귀 추가 |
| QA 사본 Runtime/DLL/shader | **13/13 직접 byte 동일**, Core/DLL/provenance 변경 없음 |
| 보호 원본335개 | 현재 파일 해시335/335 일치. Unity 보호201·PC6 보존은 구현자 기록으로 별도 확인 |
| 최종 새 앱 | **334/334 파일 일치**, manifest SHA `7bfce240fe329ead4563d412686748bf0ce5d7a5c160288bc505af0efb820ed5` |
| 최종 엔진 시험 | compile0·**54 Edit/20 Play PASS**, 실패/skipped0 |
| 동일 원래 Clear 변이 | baseline2PASS0→원래 구조2FAIL/exit2→복원2PASS0, 두 실패는 camera true/expected false assertion |
| 실제 독점 QA 실행 | 성공한 두 원장 Player의 실제 exit0/0, 별도 무저장 probe0·새 경로 확인 |
| 실제 Host 저장 복원 | 1회차 committed 파일과2회차 loaded-state의 Run/Profile 및 loaded-world 직접 일치 |

새 앱의 Info.plist bundle/product와 probe facts는 같은 suffix를 사용하고 실제 저장 realpath는 기존 canonical 두 디렉터리 및 옛 shared QA 경로 밖이다. probe는 Host 저장 파일 생성 전에 종료했다는 기록을 확인했다. 새 QA3파일은 종료 후 본래 부재 상태로 복원됐고 PC6개 보존은 별도다. 검토자는 원본 사용자 저장에 접근하거나 현재 프로세스를 조회하지 않았다.

첫 증거 수집 시 부모 runner가 아직 쓰는 JSON을 읽어 중단되어 Player exit를 수집하지 못한 시도는 actual exit null이며 PASS로 세지 않았다. 수정 runner는 완성된 JSON과 프로세스 wait를 유지해 별도 두 성공 실행을 기록했다. 미수집 종료 원인이나 옛 프로세스 소유자를 추정하지 않는다.

앞 절의15PNG·12쌍 저장·40/120적 고정64입자 성능은 이전 P1 앱/소스의 역사적 측정이다. 이번 Clear 수명 수정에서 같은 픽셀/성능을 재측정했다고 주장하지 않는다. 현재 코드의 나머지 Runtime이 동일하다는 해시 근거와 재실행한 새54/20·특정 회귀·새 앱 근거를 구분한다. 실제 독립 PC 시각/조작 리뷰는 별도 담당 범위다.

최종 추가 HIGH/MEDIUM은 발견하지 않았다. 실카메라 조합·장시간 resize/GPU/모바일 성능·공식 팩 제품 임포트는 미실행으로 남기며 독립 PC 시각/조작 리뷰·GC/commit/push/해당 SHA CI는 root 인계 범위다.
