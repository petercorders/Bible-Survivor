# R02 실제 아트 연결 — 독립 코드 리뷰

2026-11-14. **이전 Core DLL을 고정한 R02 아트 준비 checkpoint의 코드 범위 PASS**. 신규 CRITICAL/HIGH/MEDIUM은 발견하지 않았다. 최신 schema2 Core 동기화·통합 앱 검증·독립 시각 판단·최종 GC/commit/push/CI는 별도 게이트다.

## 범위와 독립성

구현에 참여하지 않은 reviewer가 `AdamR02Art`, 별도 importer/build validator, `AdamArtWorld`/`AdamRoundPresentation`/`AdamGame` 변경, 관련 Edit/Play 시험, QA build/path-gate/capture fixture, public sidecar inventory·검사 driver를 읽었다. 후속 root 변경인 `.github/workflows/art.yml`과 `tools/hooks/pre-commit`의 read-only R02 gate 연결도 검토했다. 소유 문서는 이 보고서뿐이며 제품·시험·외부 원본·사용자 저장은 수정하지 않았다. Editor·Player·GUI·기기는 실행하거나 조작하지 않았다.

동시에 진행된 schema2 Core 소스는 다른 독립 리뷰 범위다. 아직 DLL sync하지 않았다는 사실을 아트 구현 결함이나 최신 Core 성공으로 바꾸어 세지 않는다.

## 판단 근거

- runtime은 별도 고정 index SHA와 manifest SHA를 먼저 검사하고 정확한 b1/b2/b3 세 종을 요구한다. 각 stable clip ID·source·PNG SHA·32px/4프레임·pivot(16,1)·8/8/6fps·loop·sorting·alpha·shadow/ground를 검증한다. imported Sprite는 이름/rect/pivot/PPU/border와 공유 texture 설정 및 원시 RGBA SHA로 확인한다. 모르는 종/clip은 예외이며 Art03 적으로 대체하지 않는다.
- importer는 승인 PNG SHA를 확인한 뒤 Point/Clamp/no mip/Uncompressed/FullRect와 명시 pivot을 적용한다. 프레임 이름별 결정적 sprite GUID 및 name/file-ID pairs를 사용한다. 실제12개 재임포트 등록 근거의 before/after가 동일했다. 별도 build validator는 기존 Art03 계약을 완화하지 않는다.
- public gate는 승인 inventory 자체 SHA·21개 입력 SHA·정확한 공개 파일 집합·제작자 계약 검사를 통과한 후 Unity PNG/manifest/index bytes를 대조한다. source 중복은 고정 source SHA 및 제작자 source identity 검사로 거절된다. `--prepare`는 다른 기존 입력을 덮어쓰지 않는다. 전체 snapshot의 생성 도구를 검수 당시 checker와 함께 고정하고 있다.
- public subset·Unity R02 Resources·새 driver/QA Editor 파일을 검색해 개인 절대경로·캔버스 URL/blob·token/password 패턴은 없었다. 절차적 PNG 생성과 Design 검토 캔버스의 구분은 README/진행 문서에 유지된다. QA marker·고유 suffix/product/bundle·fresh absolute output과 첫 무저장 path gate를 보존한다.
- CI는 기존 pinned Python3.11.6 환경에서 R02 read-only gate를 추가 실행하며 Editor 성공으로 표기하지 않는다. hook은 패키지 manifest 존재 시 같은 gate를 실행하고 비0 종료를 전파한다. 설치된 hook과 versioned hook bytes도 일치했다. CI 실제 실행 성공은 아직 이 리뷰가 확인하지 않았다.

## reviewer가 직접 확인한 실행·결속

`python3 -I -S -B tools/unity_prepare_r02_art.py`: exit0, index SHA `828f685f970523e769af09e4fd609db556e5d02f09e36bb3fba24f6f37dd55e5` 일치. `git diff --check`: exit0.

새 격리 사본에서 동일 read-only driver를 실행했다. 정상/원복은 각각 exit0, Unity index fps 변경·승인 source PNG 한 byte 변경·공개 subset에 미승인 파일 추가는 각각 명시 exit1·traceback0이었다. 원본이나 Unity project를 수정하지 않았다. 이 독립3오류를 제작자7오류나 외부 QA 횟수에 합산하지 않는다.

`evidence/unity-r02-art-20261114/binding-final.json`이 가리키는349개 현재 소스 SHA를 직접 계산해 전부 일치함을 확인했다. source manifest SHA는 `0a28f105874d0dc66dc1e70864e3589aba08d7b152dab7a0ae6de2f4771823f9`다. 정확한 최종 run output의 앱334파일도 전부 manifest와 일치했고, 보호205파일의 현재 SHA도 동일했다. 등록12개 before/after는 같았다. 이후 추가된 root CI/hook 변경은 앱 소스349개 manifest 밖의 별도 검토 대상이다.

최종 EditMode XML은84/84, PlayMode XML은30/30·failed0/skip0이다. 검사기/임포트 pivot/fps 가드 제거의 실제 assertion 실패와 원복, pinned `2edd86e`의 원래 기하 표현을 되돌린 앱의 build0→실제 출력 assertion exit2→정상 복원 근거를 읽었다. 이는 **개발자 실행 근거를 reviewer가 확인한 것**이며 reviewer가 Unity84/30개나 앱을 직접 재실행한 것으로 보고하지 않는다.

## 남은 통합 경계

별도 schema2 리뷰 이후 원본 Core DLL/provenance를 동기화하고 이 아트와 결합한 실제 compile/Edit/Play/macOS 빌드·격리 저장 재개 및 최종 소스/앱 SHA를 다시 고정해야 한다. 그 후 변경분과 근거 드리프트를 재대조한다. 현재 새 codec의 Unity/IL2CPP 지원은 이 코드 리뷰에서 검증하지 않았다.

색공간/피격 tint·50적 군집의 B2 판독·P1 겹침은 독립 PC 시각 리뷰가 판단할 대상이다.30 frame/flip probe와24 staged 조합은 자연 수동 플레이나 밸런스·성능의 대용이 아니다. native active 입력·모바일/실물·성능·remote Design 확증은 이 리뷰에서 NOT_RUN/미확인으로 유지한다. 사용 중인 iOS 및 unknown 옛 QA 프로세스/저장에는 접근하지 않았다.

## 깊이 정렬 최소 수정 재리뷰

2026-11-14. **이전 DLL 기준 준비 checkpoint의 코드 PASS 유지**, 새 HIGH/MEDIUM 없음. 이번 재리뷰는 `AdamArtWorld`의 적 본체 순서와 신규 `AdamEnemyDepthTests`를 대상으로 한다. 최신 schema2 Core sync·앱 통합 성공으로 확대하지 않는다.

정렬 키는 실제 그릴 때와 같은 `AdamWorldProjection.Point`의 반올림된 화면 발 y다. 화면 y가 작은 적을 먼저 그려 아래쪽 발의 본체가 나중에 덮으며, 반올림 후 동률은 Core ID 오름차순으로 고정한다. 월드 y를 그대로 오름차순 정렬하거나 반올림 전 미세 차이로 동률을 깨지 않는다. 위치/순서용 `EnemySprite` 값과 재사용 리스트만 만들고 Core snapshot 원소/순서·충돌·피해를 수정하지 않는다. 프레임 내 indexed loop는 이 결과를 그대로 소비한다. 기존 그림자/드롭→적 본체→P1→플레이어/무기→기존 효과/HUD 패스는 유지된다.

128 capacity 리스트·단일 comparer·삽입 정렬은 현재 최대120 actor 범위에서 buffer를 재사용한다. Clear도 같은 리스트를 비운다. warmup 후120개×1,000호출의 managed allocation0 회귀가 최종87Edit에 포함돼 있으나, 이는 정렬/투영 함수만의 개발자 시험이다. 전체 프레임·GPU·최악 CPU 성능 PASS는 아니다.

새 `evidence/unity-r02-depth-20261114/binding-pre-sync.json`의 모든 evidence SHA와 현재352소스 SHA를 직접 대조해 불일치0을 확인했다. source manifest SHA는 `cb5ef04cfa73b022887bde43150c84b433ea3191ba43a284cf98f789d65e1372`다. 전/후 실제 앱334파일씩과 보호205파일도 현재 bytes가 manifest와 일치했다. 선택된 이전 Core DLL·Art03/font/설정은 보존됐다.

실제 XML을 읽어 Edit87/87, 첫 Play29/30 실패 및 같은 코드의 직렬 재실행30/30을 모두 확인했다. 첫 실패는 타이밍 제한 초과 관측이며 성공 숫자에 숨기지 않는다. 원래 ID 순서(정렬 제거) 변이는 두 창 크기의 예상순서 assertion2개가 실패했고, 정상/원복은3개 PASS였다. 새 정렬이 필요하다는 회귀 근거로 그 원래 구조를 사용했으며 컴파일 실패를 검출로 세지 않았다.24개 전투 Run/Profile/World 동등 근거와 각 앱 build/capture/load 종료 기록은 개발자 실행 근거로 확인했고 reviewer 자신의 Editor/Player 실행은0이다.

B2의 특정 군집 고유색 면적은 오히려 줄어든 표본도 있으며, 이 정렬 변경을 B2 가독성 일괄 개선으로 판정하지 않는다. 피격 tint/마스크·팔레트·본체 프레임은 변경되지 않았다. B2 및 군집의 사람 판독은 별도 시각 리뷰 범위이고 native active 입력·자연 밸런스·모바일/실물·전체 성능은 여전히 NOT_RUN이다. 최종 schema2 sync 후 새 소스/앱 해시와 통합 시험을 다시 고정하는 게이트가 남는다.
