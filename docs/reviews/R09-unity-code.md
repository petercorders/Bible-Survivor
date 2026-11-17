# R09 Unity 독립 코드 리뷰

2026-11-17 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r09-implementation-01.md`, 반환 UI 인계와 최종 `evidence/r09-unity-host-20261117/handoff.md`·binding/verification 근거를 검토했다. 본인의 R09 Core PASS·관련20/최종6/정확2변이/단1회 R01~R09 실제 좌표 CLI는 재사용했다. 잠정 검토한13개 소스의 메모리 기준점과 최종 내용을 비교했으며12개는 동일하고 importer 교정만 추가 확인했다.

## 소스 확인 사항

- Host AwaitingR09는 R08 between9에서만 진입을 허용하고 ChapterComplete/between10만 최종 완료로 처리한다. catalog는 정의9와 세 boss 각각의 실제 수치/예고/공격 개수·패턴을 대조하고 null 해금 adapter를 유지한다. 기존 성장 gate·고정 후보·350/100/25/160 지급·세 receipt와 frozen state/revision/operation retry를 재사용한다.
- UI는 유다/은잔의 참소자·동적16000HP/5초·사슬3방향 예고를 사용하며 기존 두 보스 문구를 유지한다. pending 성장/정산 우선 뒤 창세기 완료·저장/진행 유지·다음 책 미구현을 표시한다. 종료 상태를 자동 초기화하거나 다음 책으로 넘기지 않고 새 도전이라고 명시한 버튼/슬롯 선택만 기존 guarded Host.Start로 연결한다.
- ChainWarning은 실제 현재 boss actor 위치와 저장된 세 방향을 읽고 Core 속도×TTL8.1 구간으로 투영한다. 예고 시/전투 중에만 그리며 벽시각·새 피해/충돌 판정을 만들지 않는다. 필수 선과 실제 적탄은 선택 FX gate 밖의 마지막 world layer에 그려 actor/무기/particles 위·HUD 아래에 남는다. R09에서는 기존 원형 slam 고리를 그리지 않는다.
- 실제 적탄은 snapshot X/Y를 그대로 투영하며 방향은 screen Y 반전이 포함된 순수 회전 한 번으로 표시한다. sprite frame age는 World.Time-BornAt 절대 전투시간을 쓰고 pause 동안 외삽하지 않는다. 보스 windup은 BossElapsed/NextAttackAt/동적1초를 사용한다. live 탄으로부터 BornAt을 읽어 reload 직후 짧은 발사 pose를 복원한다. Core kill clear/reward 단계에서 탄을 숨기고 기존 짧은 death 종료를 유지한다.
- ArtR09 loader/importer는 pinned index/manifest·8PNG/27프레임·actor/boss/중심8,8 탄 pivot·PPU32·Point/Clamp/비mipmap·정확한 frame rect/이름·PNG whitelist/hash·고정 sprite ID/build 검증을 사용한다. 알 수 없는 actor는 다른 아트로 대체하지 않는다.
- 신규 Host/UI/World 시험 소스를 읽었다. Host는 실제 pending1/Choose1을 후보 개수와 분리하고 normal/예고/live탄 disk replay·두 history/진화, reward와 완료 실패 각각의 frozen retry, 성장 뒤 완료·세 receipt·명시 새 도전을 검증한다. World는 저장 방향/현재 원점/8.1구간·pause/lowFX·실제 탄좌표/절대시계/회전과 clip 계약을 다룬다.
- 실제 import 실패 교정은 importer의 `textureShape=Texture2D` 명시와 pinned index의8장만 ForceUpdate/revalidate하는 경로다. 다른 Runtime/시험12개는 잠정 검토와 동일하다. 보스/탄 gameplay를 변경하지 않고 Sprite0개를 만들던 metadata shape 원인을 처리한다.

## 재사용 최종 검증

- `verification-index.json`·원 XML은 초기 Edit **135/136 PASS·1실패**와 importer 교정 뒤 해당1개 **PASS**를 구분한다. 실패는 `EightReviewedSheetsAnd27FramesKeepContractFpsPivotsAndBossClock`의 Missing R09 frames였다. 총136개의 서로 다른 시험에 통과 근거가 있으나 **두 번째 전체136/136 실행은 없다**. Play31은1회 PASS, initial compile/교정 importer 실행·project/portable는 exit0이다. QA에서 정확한 옛 R08 ContentComplete 경계 변이는 실제 Expected False/Actual True 실패→복원 대상1 PASS로 검출됐다. 제품에 변이를 적용하지 않았다.
- 최종 정상 앱은 `final-review/build/R09Qa.app`, identity `r09613956195cd9`다. 고정76 source·app/binding·기록된 provenance를 확인했다. Core 소스 hash 기록은 본인 Core handoff와 일치하며 sync/provenance/binding/app DLL 기록은 `2d62c170…5d5c6`로 연결된다. 새 해시 계산은 하지 않았다. 동일소스8장 재임포트 전후27개 sprite 등록의 이름/GUID/local ID/rect/pivot JSON이 동일하다.
- `final-review/capture/continuous-trace.txt`와 최종 QA driver를 대조했다. 기술 R08 seed→R09 I1/I2/I3 실제1초 추적·normal disk Load→Core 생성 일반gold25 **후보1/PendingLevels1/실제Choose1**→무조기회복 intro→BossElapsed4.5/WorldTime186.5의 실제3방향 lowFX 예고/정확Load→187.5의 실제3탄0.5초 이동·pause/live Load→직접 HP damage6500/4800의 mask1/3·지원6/12 및 각각Load→주입 lethal kill 후 적탄 제거/고정350 once를 확인했다.
- 보상은 동결된2후보의 정확Load와 실제 SelectSlot 턱뼈 진화 once를 확인한다. 앱 정산은 **PendingLevels0/실제Choose0**이며 앞 일반 성장1회와 구분한다. 골드는1456 seed→1624 preboss(기술 일반 처치·25포함)→1974(+350)→2134(+160), 해금null/HP10000·세 receipt/세 진화·PreviousNormalRound8·between10을 유지한다. 보스gold100은 이 앱에서 선택하지 않았으며 기존 Core/UI/Host 근거를 재사용한다.
- 새 process는 완료 Run/Profile을 정확Load하고 unpause/Tick이 자동 다음 책/초기화를 하지 않음을 확인한 뒤 실제 명시 SelectSlot 새 도전으로 R01/new RunId·동일 Profile을 검증했다. 최종 완료 snapshot은 level20/pending0/serial20이며 R03/R06/current R09 완료 receipt와 sling/Bible/jawbone 세 form이 있다. 대표3장은 예고lowFX/live탄/창세기완료이며 독립 UI 판정은 별도 담당 범위다.

## 실패 이력·복구와 한계

정상 BuildPipeline macbuild는 **2회**이며 별도 QA scene 잔존 preflight 실패1회가 있다. 첫 앱 exit2는 QA가 Bible 전용 pulse clock을 비성경 slot에도 넣은 오류로 보존됐고 최종 fixture는 Bible에만 그 clock을 할당한다. 성공한 no-Host probe의 첫 runner 거절은 exact bundle/company-product realpath 두 경로만 허용하도록 좁게 교정됐다. 최종 Player capture/reload는 exit0이지만 wrapper는 Unity Analytics 파일을 SaveStore 파일로 센 정리 guard 때문에 **exit1**이었다. 후속 정확 main/bak/lock 복구는 **exit0**, Analytics는 보존했고 앱을 다시 실행하지 않았다. 이를 wrapper exit0로 바꾸어 주장하지 않는다. 자기49086/49168/49169/49177 exit0·no-own-writer/자기3저장 absent 복구 기록을 재사용했다.

Unity 자동 metadata 정리는 기록된 원본 아트/폰트 bytes 복구와 별도로, root가 빌드 이후 ProjectSettings의 empty iPhone/Android icon129줄만 제거한 사후 정리다. `settings-auto-icons-restoration.json`은 첫 기대문자 guard가 쓰기 전 거절됐고 정확 YAML line 검사 후 HEAD byte동일 assert PASS를 남긴다. 현재 해당 설정 git diff는 없으며 Runtime 변경/재빌드 없이 기존 binding/앱 근거를 보존했다. 리뷰어가 설정이나 실제 저장을 수정한 것은 아니다.

앱은 HP10000/기존 장비8·crown→hair 보조 교체/normal direct Kill·drop 폐기/timeout·무기clock1000·초기boss 위치·직접 boss HP damage/kill의 기술 fixture다. 실제3방향 예고/탄 이동·저장·선택/정산 연결 근거이며 앱 자연 보스전/아홉 라운드 완주 근거가 아니다. 실제 피해/처치를 주입하지 않은9라운드 연속성은 별도 단1회 Core CLI 근거다. 자연밸런스/native input/mobile/device/performance·전프레임은 미검증이다.

리뷰어는 제품/시험/아트/DLL/저장을 수정하거나 Editor/GUI/build·suite/CLI·해시행렬을 실행하지 않았다. 실제 의심 재현이 필요한 새 결함이 없어 probe도 추가하지 않았다. 추가 agent/Claude 호출은 없다. 본 문서만 소유하며 usage `r09-unity-review` start/end는 세션 기준점이다. stale 시작 event·개별 worker 비용/effort 미측정을 구분하고 청구비용·절감률을 추정하지 않는다.
