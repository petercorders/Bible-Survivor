# Visual quality 통합 독립 UI / 무기 시각 리뷰

**지정 캡처 및 한정 영상 표본 관측 PASS. 확인된 차단 HIGH/MEDIUM 없음.** 미관측 동작·음향을 통과로 세지 않는다. 2026-11-18 KST, task `task_45f03a892bb9` / dispatch `ctx_7afb1fd91634`. 제품 구현자와 별도 세션의 독립 리뷰다.

기준은 `evidence/visual-quality-20261118/handoff.md`의 최종 `build/VisualQa.app`, run `visualf37f399abf3d`다. 실패한 capture-first/second/third/fourth-fixture 기록을 최종 성공 캡처와 섞지 않는다. 기존 외부 UI/effects/world 패키지별 PASS는 재사용하며 재생성/전체hash/픽셀행렬을 반복하지 않았다.

## 지정 원본 직접 열람

아래 `evidence/visual-quality-20261118/capture/` 원본 11개를 전부 실제 view_image로 열었다.

| 원본 | 직접 관측 |
|---|---|
| `04-660-growth.png`, `03-1100-growth.png` | 레벨 머리표/큰 선택 제목/정지·진화 안내, 카드명·신규 단계 chip·효과 설명·번호 키캡이 구분된다. 세 카드의 제목과 수치 설명, 하단 기본무기/보조 안내 및 선택즉시저장이 패널 안에 읽힌다. 660에서 세 카드와 footer가 겹치거나 핵심 문장이 잘리는 차단 결함은 보이지 않는다. |
| `05-660-boss.png`, `06-660-boss-lowfx.png` | 0:04 아래 보스 경과로 역할이 분리되어 /180초 모순이 없다. 보스명/24920/26000과 phase가 별도 두 줄에 읽힌다. 세 방향의 흰 심선·어두운 rim·화살표가 모두 보이며 lowFX에서도 유지된다. optional band 차이가 보이고 필수선이 장식 아래 묻히지 않는다. 하단 무기/보조 아이콘과 키캡은 전투영역 밖이다. 우하단 개발워터마크 부근 F9 설명은 밀집해 있으나 이번 핵심 선택·회피 안내를 막는 HIGH/MEDIUM으로 판정할 근거는 없다. |
| `07-1100-boss-reward.png`, `08-660-boss-reward.png` | 카드2개(구름 강화4→5/5와 골드+100), 고정골드+500 적립 footer/선택즉시저장이 구분되고 크기별로 읽힌다. 정적 PNG로 frozen 시계·실제 선택 성공을 입증하지 않는다. |
| `09-660-exodus-complete.png`, `10-1100-exodus-complete.png` | 출애굽기완료/성막완성, 완료골드+280·진행유지, 레위기미구현/다음책준비, 세 명시새도전과 성장·장비초기화 주의가 읽힌다. 기존 진행 유지와 새도전 초기화의 구분이 보인다. |
| `11-1100-save-retry.png` | 저장/불러오기 중단, 전투정지·재로드 시 미저장진행 폐기 설명과 같은저장재시도/저장다시불러오기 두 행동이 읽힌다. 버튼 라벨의 가장자리 여유가 작아 원본을 다시 열었으나 행동을 오인시키는 HIGH/MEDIUM은 확정하지 않았다. `owned visual retry`는 해당 기술 fixture의 예외 문구이며 일반 제품 실패 문구로 일반화하지 않는다. |
| `12-1100-death-pose.png`, `13-1100-death-restart.png` | 전투종료 문구와 포즈, 이후 사망·이번도전종료 및 성장초기화/영구골드·해금유지 설명, 1~3 새무기 버튼이 구분된다. 정지 한 장으로 사망 애니메이션 전체를 인증하지 않는다. |

잉크 면/양피지 주제·값/청동 행동의 계층은 실제 문구가 얹힌 위 캡처에서 확인했다. 보스의 보라/회색 작은 적탄과 세 필수선, 영상의 청록 XP 마름모·흰 별/연기 잔해는 표본에서 색·형태로 구분된다. 실제 모든 겹침·색각/흑백 군집 접근성 인증은 하지 않았다.

## 영상에서 본 것과 보지 못한 것

ffmpeg/ffprobe는 현 PATH와 일반 설치경로에 없었다. macOS AVFoundation의 읽기 전용 AVAssetImageGenerator로 최종 silent `video/{jawbone,sling,bible}.mp4`에서 소수 프레임만 디코드했다(exit0, API deprecation 경고만). 각0/.1/1.5/4.5초, jawbone·sling만 .2/.3/.4초, sling만1.0/5.8초를 추가 확인했다. 전프레임 행렬/새 게임 캡처/앱실행이 아니다. 임시 파생 프레임은 `/var/folders/ly/4sm0t3qs4pgg3mpqcrrh7c580000gn/T/visual-independent-ctx7afb1fd91634/`에만 만들었다.

- 턱뼈 .2→.3초: 플레이어 손 부근에서 좁은 손잡이와 둥근 머리가 다른 각도로 이어지고, 가는 앞날/옅은호와 처치 잔해가 보인다. 손에서 크게 떨어지는 표본은 관측되지 않는다. 초기/후반 프레임은 이동과 경기장 상대위치 변화를 보여준다. **활성스윙의 좌우반전 전후는 이 표본에서 직접 확인하지 못했으므로 반전 전체 PASS로 쓰지 않는다.**
- 물맷돌 .2초: 플레이어의 뻗은 팔/해제 자세와 왼쪽 접촉 위치의 작은 흰 별, 이후 연기·XP가 구분된다. sampled 위치에서 작은 돌의 release→비행trail 연속 궤적 자체는 명확하게 포착하지 못했다. 이는 미관측이며 돌/궤적이 없다는 결함을 뜻하지 않는다. 실제 발사ID/velocity/trail 사용은 생산자 근거만 재사용한다.
- 성경 0/.1/1.5초: 책이 플레이어 위에서 왼쪽 위/아래로 위치를 바꾸는 공전 표본과 접촉 뒤 연기·XP가 보인다. 손에 고정된 몽둥이처럼 돌지 않고 배우 주변에 독립된 책으로 읽힌다. .1초 접촉은 보이지만 모든 접촉의 흰별/페이지 애니메이션을 표본에서 확인한 것으로 세지 않는다.

생산자 facts: actual hit/played jaw1/1, sling5/5, bible3/1(동일 step audio cap), sling7shots/5hits/1live·최소1 no-hit 제거. 이 숫자/명중·빗나감 전수를 본인이 세거나 실시간 관찰한 것이 아니다. 표본 프레임의 XP/잔해만 보고 hit·miss를 추가 추정하지 않는다. fixed-tick60프레임/10fps/6초 movie는 자연 실시간 성능 증거가 아니다.

## 음향·시험 경계

현 도구 목록에 로컬 오디오를 받아 주관 청취할 수 있는 입력 도구가 없어 **jaw thud / stone crack / page 차이의 주관 청취 NOT_RUN**이다. 사용자 앱/OS재생기를 열지 않았고 파형/RMS를 청취PASS로 쓰지 않는다. 생산자의 `audio-facts.json`에 있는 `capture/*-engine.wav`는 실제48kHz stereo engine mix, `*-pcm.wav`는22.05kHz mono 생성 원본으로 구분했다. `video-with-audio`는 편의mux이며 정확한 A/V동기·지연 인증이 아니다. 주관음향 승인이 필요하면 실제청취 담당의 후속 확인이 남는다.

생산자 재사용: native numeric3/F6/F7/Esc/Return 및 pointer 앱상태, DrawGameView World/whole save bytes불변, mute0voices/pause World·storefreeze, 관련36/36·delta13/13·final2/2 PASS. 겹친 사례를51 unique로 합산하거나 본인 실행으로 세지 않는다. 기존 반픽셀 손anchor/좌우pivot의 회귀시험은 소스동일 구간의 재사용 근거이며 본인 반전영상 직접검증과 구분한다.

자연밸런스/전체앱 native연속/모바일/성능·실시간 지연은 **NOT_RUN**. 새 제품/Editor/Player/GUI/저장 실행이나 보호identity 조회·조작 없음. 소유 repo 리뷰와 외부 두 보고서의 통합섹션만 작성하고 타인 편집은 보존했다. 새worker/commitpush 없음.

## 자기 usage

phase `visual-quality-final-UI-weapons-review`, 세션 `01a1ea49-7c85-7cb0-854f-074ad240421e`, 관측 gpt-6.1-sol/effort 미측정. 기존 usage 도구 start/end를 `--ledger /dev/null`로 출력 수집(양 exit0).

- start recorded_at `2026-11-18T00:16:05.197584+00:00`, event `2026-11-17T23:36:27.993Z`: input5888303/cached5518336/cache_write0/output36290/reasoning1391/total5924593. 오래된 provider baseline이라 정확한 이번 시작량으로 전용하지 않는다.
- end recorded_at `2026-11-18T00:17:47.487953+00:00`, event `2026-11-18T00:17:22.501Z`: input7609431/cached7210880/cache_write0/output39053/reasoning2275/total7648484, resultPASS. 후행 작성/인계 및 telemetry지연 미포함.
- 자동delta BASELINE, 총량 재계산 없음. reasoning은 output에 포함되어 중복합산하지 않는다. 청구·가격·요청귀속 근거가 없어 비용/API equivalent cost/절감률 **미계산**, 미측정을0으로 합산하지 않는다.
