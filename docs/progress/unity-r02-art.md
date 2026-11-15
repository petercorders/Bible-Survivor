# R02 실제 시트 연결 — 준비 checkpoint

2026-11-15 후속: 아래 마지막 절의 발 pivot 정정·최신 schema2 Unity 통합이 구현자 PASS다. 앞 절들은 이전 DLL로 수행한 이력이며 독립 최종 리뷰·GC·커밋/push·CI는 root 후속이다.

2026-11-14. 기술 checkpoint `2edd86e` 뒤 독립 R02-01~04 CLOSED/오프라인 인계 PASS인 동일 입력을 통합한다. **진행 중인 Core schema2의 최종 sync/rebuild 전 준비 checkpoint**다. 이번 검증은 기존 Unity Core DLL 기준이며 최신 Core 종합 PASS·모바일·성능·자연 수동 플레이를 뜻하지 않는다. Core 소스/공유 시험/DLL/provenance/catalog/sync·기존 checker/CI/hook은 수정하지 않는다.

별도 `art/art-genesis-r02/`에 원본21파일과 공개 README/승인 입력 해시 목록만 둔다. PNG3·편집 가능한 격자/팔레트·계약·생성/변환/manifest·검사/제작자 시험을 포함하며 비공개 Design canvas/URL·raw QA·archive·cache는 제외한다. PNG는 제작자의 절차적 생성기→격자→stdlib 변환 결과다. Design 검토 캔버스와 게임 PNG 생성은 구분하며 원격 캔버스 렌더를 확인했다고 주장하지 않는다. 권리/출처는 원본 manifest의 original/no-third-party-image 고지를 그대로 보존했다.

고정 manifest SHA256: `e3a69af3c0734e5d37848b5780d020d09bbb054538e4173bf8aacafc0db1be89`.
고정 Unity index SHA256: `828f685f970523e769af09e4fd609db556e5d02f09e36bb3fba24f6f37dd55e5`.

`ArtR02`는 Art03의79개 index를 넓히지 않는 별도 리소스/로더/임포터다. b1/b2/b3를 명시 stable ID의 실제 클립으로 교체했으며 기하 표식과 임시 안내는 제거했다. 모르는 적은 명시 거절하고 a몹으로 대체하지 않는다. 기존 terrain/플레이어/폰트·HUD/IMGUI Actors 배치 순서·전투 판정은 유지한다.

| 계약 | 연결 |
| --- | --- |
| 시트/등록 | PNG3,128×32,32px×4프레임,PPU32,FullRect,무테두리,pivot(16,1) bottom-left |
| 텍스처 | Point/Clamp,no mip,무압축,투명 RGB 보정 없음,readable 원시 RGBA SHA 검증 |
| 재생 | b1/b2/b3 8/8/6fps loop,오른쪽 원본·단일 renderer UV flipX1회 |
| 그림자/ground | b1 기존 small/false, b2·b3 기존 wide/true |
| 거절 | 누락/중복/알 수 없는 등록,index/manifest/PNG/pixel SHA,rect/pivot/PPU/fps/loop/border/설정 불일치 |
| 재임포트 | deterministic sprite ID,asset GUID/local file ID/rect/pivot/border/PPU 전후 동일12프레임 |

`python3 -I -S -B tools/unity_prepare_r02_art.py`는 공개 입력 inventory SHA/파일 whitelist와 제작자 계약 검사를 통과한 뒤 결정적 Unity index·PNG·manifest byte를 확인한다. `--prepare`는 없는 승인 입력만 만들며 다른 기존 입력을 덮지 않는다. 빌드 전 새 Unity validator가 실제 로더/임포터/PNG를 검사한다. 기존 `tools/check_art.py`·precommit/CI 연결 변경은 root 소유로 남겨 두었다.

## 구현자 검증과 한계

근거는 새 ignored `evidence/unity-r02-art-20261114/`다. 실제 명령·exit/XML·앱/소스 해시·정확 QA identity/PID/경로·저장은 이 경계에만 남긴다. 공개 문서는 repo-relative 경로와 `Application.persistentDataPath`만 사용한다.

- 외부 원본은 읽기 전용으로 유지했다. whitelist 사본의 checker0와 전체verify0을 확인한 동일21입력만 복사했다. 첫 격리 verify는 selftest의 상대 sibling 참조 폴더가 없어 실패했고, 격리 사본에 참조를 보완한 재실행이 통과했다. 제작자 재실행을 독립 QA 횟수에 합산하지 않는다.
- 실제 compile/EditMode84/PlayMode30 PASS. 기존 Unity 회귀와 R02 세 무기 명중 후 표현 on/off의 Run/Profile/World 정확 동등, 두 창 크기의 실제 DrawGameView·이동·좌우 방향·정지·reload를 포함한다. 저장 operation ID가 다른 전체 파일 bytes 동일을 주장하지 않는다.
- 실제 macOS Development 앱에서 660×440의12프레임×좌우2와1100×720의3종 frame0×좌우2, 총30개 probe로 실제 DrawGameView 출력의 불투명 픽셀·프레임·정수 배치·UV 반전을 검사한다. 원시 임포트 RGBA SHA와 화면 RGB 검사는 별도다.
- 두 해상도×초기40/50적×세 무기×기본/저효과의24조합에서 실제 이동·명중·처치·P1 겹침·정지를 캡처했다. 초기 아레나 normal/low12쌍과 전투 중 HUD24쌍의 RGB가 정확히 같았다. 저효과 입자0, 관측 최대 입자164≤256. 이 짧은 staged fixture를 자연 플레이/부하 성능 수용으로 사용하지 않는다.
- 원래 `2edd86e`의 World/표현 코드를 그대로 되돌린 별도 앱은 build0 후 첫 실제 프레임버퍼 검사에서344픽셀 중0일치로 exit2였다. pivot/fps/임포트 pivot 가드 제거도 각각 실제 assertion FAIL, 원복 시험 PASS를 확인했다. 공개 입력 드라이버는 정상/원복0,중복source·indexfps·누락/변경PNG·대문자추가PNG·private파일·inventory변조7음성1/traceback0을 확인했다.

### 색공간 관측·검사 교정

첫 앱의 원시 RGB 직접 비교는0일치로 실패했다. 임포트된 RGBA는 원본 해시와 일치했고, 실제 Linear 프로젝트의 기존 IMGUI 출력에서는 예를 들어 원본(27,20,36)이(92,79,105)로 보였다. 화면 비교의 기준은 관측된 출력 인코딩에 맞춰 `Color.gamma`를 사용한다. 해당 API는 선형 색의 sRGB 근사 변환을 제공한다([Unity Color.gamma](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/color/gamma)). 일반 텍스처 샘플링/색공간 설명만으로 이 legacy blit 경로의 전체 원인을 단정하지 않는다([Unity linear color space](https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/graphics-color/color-spaces/linear-color-space/space)).

이후 정상 앱 재실행의 첫 캡처가 앞선 실행보다 RGB 최대4만큼 달라 허용차3에서 실패했다. 이 실패도 보존한다. 검사에3프레임 대기와 화면 RGB 허용차5/불투명 픽셀98% 이상을 사용하고 실제 max delta/일치 수를 기록한다. 원본 PNG/임포트 pixel SHA·배치/프레임/flip 검사는 완화하지 않았다. 기존 Art03·폰트·프로젝트 색공간·공용 blit는 수정하지 않았으며, 원본 팔레트가 화면에서 그대로 보인다는 주장은 하지 않는다.

50마리 근접 군집에서는 적끼리 겹쳐 특히 낮은 B2의 종 구분/피격 tint를 항상 읽을 수 있다고 판단하지 않는다. 단독 프레임/flip 픽셀 확인과 군집 가독성 판단은 구분한다. 독립 PC UI 리뷰에 단독 probe·40/50·normal/low·피격/처치/정지 이미지를 인계하며, 색공간 관측과 군집 겹침을 디자인 피드백으로 root에 전달한다.

최종 명령/해시/저장 원복 결속은 아래 완료 집계에 기록한다. 최종 Core sync/rebuild·동일 독립 PC 코드/UI 리뷰·GC·commit/push/해당 SHA CI는 root 후속 게이트다. native active 입력·모바일·실기기·성능·자연 수동 전투는 NOT_RUN이며 synthetic 전달을 게임 진행으로 합산하지 않는다. 사용자 iOS/실저장·unknown 옛 QA와 기존 PC 저장은 보존한다.

## 이전 시각 연결 집계·다음 소유자 (깊이 정렬 전)

최종 source manifest349파일 SHA256은 `0a28f105874d0dc66dc1e70864e3589aba08d7b152dab7a0ae6de2f4771823f9`, 실제 앱 file-manifest SHA256은 `672372ee1fc3332a5131097d17965d35e3a99fb8635688c39982eba9455969e7`이다. 선택된 이전 Core DLL SHA256은 `198baeeb60a1443c48e2cc01a3efbaf84f23674352dba0cc50efea472b6bf6e0`이며 이번 단계에서 바꾸지 않았다. 진행 중인 공유 Core 소스는 앱 소스 manifest에 넣지 않고 선택 DLL/provenance로 경계를 고정했다.

| 최종 게이트 | 결과 |
| --- | --- |
| compile / Edit / Play | 실제 exit0 /84개 exit0 /30개 exit0, 실패·skip0 |
| macOS build·격리 | 새 marker/suffix/product/bundle/output, 실제 무저장 probe·realpath 대조, build exit0 |
| 실제 출력 | 30probe 불투명 픽셀 전부 일치, 최종 max RGB delta1, 24staged 시각 조합 완료 |
| 저장 재실행 | 캡처/새 프로세스 Load 각각 exit0, Run/Profile/World 정확 동일 |
| 변이/원복 | 정확 옛 기하 코드 앱 build0→픽셀 assertion exit2, 정상 원복 최종 앱 capture/load0; 세 가드 제거 assertion exit2→원복 PASS |
| 원본 | 외부/공개 whitelist21파일 byte 동일, 기존 Unity 보호205파일 byte 동일, 원본 PNG79개의 자동 overridden0 항목과 빈 아이콘만 분류·원복 |
| 저장/프로세스 | 원본 PC6개 존재/해시 보존, 자체 새 QA3개만 원복, 자체 캡처/Load 자식 exit0·같은 QA identity writer 부재 확인 |
| 드라이버 | read-only R02 checker·git diff whitespace exit0, 기존 CI/checker/hook은 변경하지 않음 |

최종 index는 ignored `binding-final.json`이며 실행 벡터/exit/XML은 `engine-final.json`, `final2/build-command.json`, `final2/capture-commands.json`에 있다. 등록 전후12개는 `registration-final.json`, 색/형태 probe는 `pixel-probes-final.json`, HUD/lowFX 격리는 `visual-isolation-final.json`, 보존·원복은 `protected-final.json`·`pc-preservation-final.json`·`qa-restoration-final.json`이다. 이전 실패/변이 앱과 로그는 별도 폴더에 보존하며 최신 리뷰 앱은 private handoff가 지정한 최종 앱 하나다.

제품/Editor/Player GUI 소유를 root에 반환한다. **최종 Core sync/rebuild 이전 준비 checkpoint**로 인계하며, 최신 schema2 저장·최종 DLL 조합의 시험/빌드·동일 독립 PC 코드/UI 리뷰·최종 GC·commit/push/해당 SHA CI는 아직 남아 있다. 신규 read-only 드라이버를 precommit/CI에 연결하는 작업도 root에 인계한다. 이번 변경에 대한 커밋·push·추가 worker는 실행하지 않았다.


## 디자인 P1: 적 본체 깊이 정렬 준비 checkpoint

2026-11-14. 외부 `art-genesis-r02/iterations/unity-feedback-01.md`의 승인된 P1만 반영했다. 적 본체는 실제 `AdamWorldProjection.Point`의 반올림된 화면 발 y 오름차순, 동률 Core ID 오름차순으로 그린다. 재사용 리스트·단일 comparer와 삽입 정렬을 사용하고 indexed loop로 소비한다. CoreCapture/저장/판정 순서와 그림자→드롭→적 본체→P1 입자→플레이어/무기→스프라이트 효과→HUD의 기존 패스 관계는 바꾸지 않았다. 그림자·플레이어를 정렬에 넣지 않았다. PNG·팔레트·index·원본03·폰트·기존 색공간·tint/스쿼시 수치는 그대로다.

근거는 새 ignored `evidence/unity-r02-depth-20261114/`다. 이 절의 SHA는 **이전 DLL로 실행한 표시 변경 준비 checkpoint**이며 최신 schema2 Core 종합 결과가 아니다. Core/DLL/provenance/catalog/sync 및 공유 Core 시험은 수정/실행하지 않았다.

| 검사 | 실제 결과 |
| --- | --- |
| compile / EditMode | exit0 /87개 PASS, 정렬 3개 신규 포함 |
| PlayMode | 첫 실행29/30: 실시간 터치 대기의 시뮬레이션 경과 .2초가 .15초 제한 초과; 로그 보존. 제품/시험 변경 없이 앱 캡처 종료 후 직렬 재실행30/30 PASS |
| 순서/변이 | 660/1100 y 역순·화면 반올림 동률 ID·입력 순서 불변; 원래 정렬 없는 ID 순서 변이는 실제 두 assertion 실패 exit2, 정상/원복3개 exit0 |
| 정렬 할당 | warmup 후120개×1,000회 정렬0바이트; 리스트 재사용·Clear 후 재사용 확인. 전체 프레임 CPU/할당 성능 수용은 NOT_RUN |
| A/B 앱 | 동일 fixture, 이전 본체 ID 순서/새 y 정렬 별도 새 출력 앱 모두 build0·capture0·새 프로세스 Load0 |
| 전투/저장 | 660/1100×40/50혼합×세 무기×normal/low 총24개 Run/Profile/World 정확 동등. operation ID 포함 전체 파일 byte 동일 주장은 하지 않음 |
| 출력 | 앱별30개 단독 프레임/flip probe와24개 군집 촬영; B3 겹침이 화면 발 순서로 바뀜. 전후 전투 HUD48쌍 최대 RGB 차이1 이하 |
| 보존/원복 | 작업 시작 baseline 중 기존 파일 변화는 AdamArtWorld.cs 하나뿐. 자동 overridden0 PNG meta79개/빈 아이콘 항목만 분류 원복. 기존12 sprite 등록 재임포트 동일·이전 DLL/아트/폰트/설정 byte 보존 |
| 저장/프로세스 | 두 새 suffix identity의 캡처/Load 자식 모두 exit0, 동일 identity writer 부재 확인 후 자체 QA3 원래 부재 원복. 기존 PC6 존재/해시 보존. unknown 옛 QA 및 사용자 iOS는 조회/조작하지 않음 |

군집 가독성 개선을 보편적으로 주장하지 않는다. 같은 staged 장면의 B2 고유색 면적은 660/50/sling tick80에서59→12px, 1100/50/sling에서229→52px였다. 이는 실제 앞뒤 겹침이 바뀐 결과이며 B2 마리 수·순수 가림량·자연 밸런스 지표가 아니다. B3 세로열의 뒤/앞 덮임은 바로잡혔지만 B2를 항상 읽을 수 있다는 수용 판정은 남겨 둔다.

### B2 0.09초 피격 반응 관측 (색/마스크 변경 없음)

정상 Core Sling 비치명 명중 후 동일 B2를 안정 최소 ID로 지정해 실제 DrawGameView를 0/.0333/.0833/.1초에 캡처했다. normal은 앞3시점 EnemyReacting=true, .1초 false이고 low는 모두 false였다. 실제 normal 이미지에서는 붉은 tint와 가로 늘어남/세로 눌림 및 P1 입자4개를 관측했다. 660의 tint 후보 픽셀은300/304/303→0,1100은1243/1250/1246→0이었다. normal/low ROI 차이는 만료 뒤에도 P1 때문에39/158px 남는다. tint 후보·차이 픽셀 수를 인간 가독성 점수로 사용하지 않는다. 두 모드8개 저장 상태 쌍의 Core Run/Profile은 정확히 일치했다.

첫 B2 fixture는 저장 전/후 서로 다른 '첫 B2'를 골라 실제 다른 적의 명중을 감시해 exit2였다. 실패 앱/로그는 보존하고 배치/관측 모두 최소 ID로 고친 동일 fixture로 전후 앱을 새 suffix에 재빌드했다. 제품 tint/피격 알고리즘을 고쳐 통과시키지 않았다. 두 시각 신호의 자연 군집 가독성과 추가 마스크/색 아닌 신호 도입 여부는 root에 근거를 인계하며 이번에 확대하지 않는다.

소스352파일 manifest SHA256 `cb5ef04cfa73b022887bde43150c84b433ea3191ba43a284cf98f789d65e1372`, 전후 앱 file-manifest SHA256은 각각 `f5f5d2afc5e657fbb9f4488b6d8817d2efb4780ee486c239ecdcae329f5d9881` / `972394a5faf12a996643c39febc99ccb45958c45bba8152388a9676d12592b3c`이다. 해시는 `source-pre-sync.json`, `app-manifests.json`에 있고 `binding-pre-sync.json`으로 명령/exit/XML/캡처/원복을 묶는다. `engine-final.json`의 최초 Play 실패와 `serial-PlayMode/`의 재실행 PASS를 함께 보존한다. 정렬 변이는 `mutation-results.json`,24전투 동등은 `combat-equivalence.json`, B2 관측은 `b2-visible-area.json`·`b2-reaction-observation.json`, 원복은 `restoration.json`이다. 최신 리뷰 앱/정확 PID·경로·재실행 절차는 private handoff만 참조한다.

제품/Editor/Player GUI 소유를 root에 반환한다. 최종 schema2 Core sync/rebuild는 root의 별도 승인·소유 인계 뒤 수행할 게이트이며 지금 실행하지 않았다. 동일 독립 PC 코드/UI 재검토·GC·commit/push·해당 SHA CI도 root 후속이다. native active 입력·자연 수동 전투·모바일·실기기·전체 성능은 NOT_RUN; 자동 driver 상태 진행과 실제 DrawGameView 캡처를 native 입력 PASS로 바꾸지 않는다. 추가 worker/commit/push는 하지 않았다.


## 후속 P2: 짧은 피격 실루엣 신호 (이전 DLL 준비 단계)

사용자가 가독성 개선과 용도별 모델·멀티 에이전트 사용을 후속 승인했다. 제품/Editor/Player는 단일 작성자로 유지하며 고정된 캡처 읽기만 경량 helper에 분리했다. Core/DLL/provenance/catalog/sync 금지는 유지한다. P1 정렬 리뷰와 이번 P2 결과는 별도 증거로 묶는다.

새 ignored 근거는 `evidence/unity-r02-hit-signal-20261114/`다. 실제 세 종×660/1100×단독/밀집×기본/저효과의24조건에서 비치명 Sling 명중 후0/.0333/.0833/.1초 총96개 시점을 캡처했다. 첫 군집 배치는 대상과 다른 적의 거리가 커 '군집 맥락' 표본으로만 보존하고, 별도 새 suffix에 더 가까운 배치로 다시 비교했다. 자연 전투 밸런스나 native 입력 표본은 아니다.

### 측정에 근거한 최소 표시 변경

전체 ROI의 차이는 스쿼시·배경·입자·다른 적까지 세므로 종별 tint 강도로 사용하지 않았다. 실제 frame·UV 반전·픽셀 중심·기존 grouped GUI 변환을 맞추고 화면 RGB가 원본/tint의 관측된 출력값과 허용차5 안에서 일치하는 픽셀만 비교했다.1100 단독 첫 명중에서 B1/B2/B3의 불투명344/327/521개가 전부 대응했고 평균 RGB 거리69.2/51.9/71.9였다. B2의 약한 tint를 보강할 근거로 사용하며 이 수치를 인간 가독성 점수로 부르지 않는다. 보정 전 helper 결과는 작은 대응 표본과 잘못된 frame/flip 전제를 가진 예비치여서 최종 근거로 채택하지 않았다.

- 세 R02 적에 공통으로, 실제 명중 후 기존0.09초 반응 중 처음0.04초만 alpha0.65 흰 실루엣을 겹친다. 화면 전체/배경/HUD에 번쩍임을 넣지 않는다. 명중 대상만 같은 본체 패스 안에서 동일 프레임/반전/변환을 사용한다.
- 원본 PNG/RGBA·팔레트·manifest/index/GUID/임포트 설정은 바꾸지 않는다. 로더가 처음 R02를 준비할 때 원본 alpha로 별도128×32 파생 텍스처3개를 만들고 재사용한다. Point/Clamp/no mip이며 총 픽셀 데이터48KiB(전체 메모리 측정 아님)다. 파생물은 승인 원본 SHA 대상과 구분한다.
- 기본 tint·스쿼시·P1·깊이 정렬·다른 패스/판정/저장/타이밍은 유지한다. 저효과에서는 새 강조도 꺼진다. 저효과의 기존 원본 실루엣/이동·HUD 체력/경험치·플레이어 접촉 경고는 남지만 적별 명중 강조를 제공한다는 뜻은 아니다.
- Clear/reload는 반응 상태만 비우고 파생 텍스처를 재사용한다. Game 파괴 시 ArtWorld.Dispose로 파생 텍스처만 해제하고 Resources 원본은 파괴하지 않는다.

### P2 검증 근거

- 실제 compile exit0·EditMode90개·PlayMode31개 PASS. 새 검사는12프레임 전체 alpha/흰색/투명 RGB·결정성·원본 byte 보존·동일 객체 재사용·조회1,000회 추가 할당0·실제 hit 대상/짧은 만료/정지/저효과/reload·반복 Native 텍스처 생성/해제 후 잔류 없음이다. 기존 세 무기의 R02 표현 on/off 전투 동등과 UI/저장 회귀도 포함한다.
- 전후 앱의96개 시점 Run/Profile/World 정확 동등을 직접 재검사했다. 기본 초기24장(단독/군집×두 크기×세 종×0/.0333초)에서 대상 영역만 밝아지고 HUD와 대상 bounds 밖은 유지됐다. 저효과48장과 강조가 끝난 기본24장의 픽셀은 전후 최대 차이1 이하였다.
- B2 기본 첫 명중은660 단독300개/군집300개,1100 단독1243개/군집1158개의 픽셀이 밝아졌다. 변한 픽셀의 평균 밝기 증가 약118(8-bit 값)이었다. 반응이 없어진 적이 항상 군집 밖으로 드러난다거나 자연 가독성을 해결했다는 주장은 하지 않는다.
- 정확한 강조 그리기 호출만 제거한 별도 원래 구조 앱은 정상 컴파일·실행/Load exit0 후 기대 실루엣 픽셀 assertion에서 exit1이었다. 정상 소스로 돌아간 앱의 동일 검사는 PASS다. 컴파일 오류나 검사 기대값 변경으로 변이를 통과시키지 않았다.

### 모델·위임 적용 기록

`model-execution.json`에 helper task ID와 requested model `gpt-6-luna`, effort `low`, 최소 컨텍스트·읽기 전용 범위를 기록했다. 구현자는 현재 모델을 유지했고 모델 전환을 지원하지 않는 부모 턴을 바꿨다고 주장하지 않는다. resolved actual model/effort와 input/output/cache/reasoning usage 원문은 제공되지 않아 null/미측정이며 총토큰·비용·절감률은 계산하지 않았다. 한 helper의 결과만 받아 수치/원본 이미지와 코드를 직접 검토했다. 첫 방법 오류와 부모의 결정적 산술 교정 비용도 숨기지 않으며, 단순 분석을 경량 모델에 맡겼다는 사실만으로 절감 성공을 주장하지 않는다. 추가 worker나 Design 제품 실행은 하지 않았다.

### 별도 관측: 기존 피격 변환의 grouped GUI pivot

기존 `ReactionMatrix(localFeet)`는 GUI.BeginGroup 내부에서 실제 전역 화면 좌표에 적용된다. 이 경로에서 x+2.4/y−17.4px 오프셋을 관측했다. 예를 들어1100 B2 주색 영역은 저효과 y345..374에서 기존 반응 y333..357로 이동했다. 순수 Matrix4x4 pivot 단위검사만으로 실제 GUIGroup 발 고정을 증명할 수 없었다. 새 강조는 같은 기존 본체 변환에 정합하며 이 오프셋을 새로 만들지 않았다. 실제 화면 발 pivot 정정 여부는 root에 별도 질문/근거를 전달했고 아직 이번 결과의 해결 항목으로 표시하지 않는다.

검사·원복을 마친 P2 준비 checkpoint로 제품/Editor/Player 소유를 root에 반환한다. 기존 pivot 오프셋 정정 범위는 root 질문의 후속 결정으로 남긴다. native active 입력·모바일/실기기·자연 전투·전체 프레임 성능·최신 schema2 Core 조합은 NOT_RUN이다.


P2 소스355파일 manifest SHA256은 `95e95e8097abdcbdb63c62a45b1b0aa9818894cb1cb0cef4c209937f0c5ac6e6`, 실제 전체 게임 리뷰 앱 file-manifest SHA256은 `e3571d7f1bd31105068c6f7343b48c0f335f24c003e606d48258e187ce69b1ee`다. 소스/앱·명령/exit/XML·측정/원복은 `binding-pre-sync.json`에 결속한다. 기존 Core DLL 선택 SHA는 앞 절과 동일하며 최신 Core 조합의 종합 PASS가 아니다.

추가 controlled GPU 검사는 실제 제품 DrawHit 메서드의12프레임×두 방향×두 해상도48개 표본에서 각1024개 source pixel의 alpha/프레임/반전을 모두 확인했다. 전체 게임 군집 촬영과 별도 제어 표면 검사라는 범위를 구분한다. 첫 GPU probe는 투명 배경을 순수 검정0으로 가정해344/1024만 맞아 exit2였다. 실제 출력의 빈 배경이0이 아님을 이미지로 확인하고 동일 프레임의 빈 대조 영역을 참조하도록 검사만 고쳤다. 새 앱의48개 모두1024/1024였고 실패 앱/로그를 보존했다.

새 suffix6개 모두 정확 자체 자식 종료 및 같은 identity writer 부재를 확인한 뒤 QA3개씩 원래 부재로 원복했다. 기존 PC6의 존재/해시는 전부 보존했다. 최신 전체 게임 리뷰 앱 하나는 private handoff가 지정하며, 변이/GPU probe/실패 앱은 리뷰 앱으로 사용하지 않는다. 모든 자체 Editor·Player 프로세스를 종료했고 사용자 iOS/unknown QA는 조회하지 않았다. helper도 settled했다. root의 같은 독립 PC 코드/UI 검토·기존 pivot 결정·최종 schema2 sync/rebuild·GC/commit/push/CI가 남아 있다. 이번 단계는 커밋/push하지 않았다.


## 2026-11-15 발 pivot 정정·최신 schema2 Unity 통합

지정된 승인 범위에서 표시층 변환 한 곳만 정정했다. `GUI.BeginGroup(arena)` 안의 본체와 파생 mask가 공유하는 `ReactionMatrix`에 `anchor + arena.position`을 넘긴다. 그룹의 local feet를 window 좌표 변환에 넘기던 원래 x+2.4/y−17.4px 이동을 제거하며, 두 draw의 anchor·frame·UV·matrix는 계속 같다. Core·판정·저장 정책·원본 PNG/팔레트·공용 blit·색공간은 바꾸지 않았다.

`python3 tools/unity_sync_core.py` 실제 exit0으로 최종 독립 R03S-01/02 CLOSED 소스를 기존 절차대로 Release Rebuild했다. DLL SHA256은 `bece4324eb8d722205fecc0870f23320f99c07ccba0799fe6b7ec29beab5f54c`, provenance SHA256은 `ff84e4d07f45aebb9399f729a7a5b584d4c24214d5a78d481dc3dcaa460cbb9e`다. catalog byte는 그대로다. 순수 Core115/기존18/추가10/역사30 및 독립25개는 기존 담당자의 근거이며 이번 Unity 횟수와 합산하지 않는다.

새 ignored 근거는 `evidence/unity-footpivot-schema2-20261115/`다. 최종 macOS Mono Development 앱은 `final-separated/run.json`이 지정하는 한 개이며, `old-offset-separated`가 정확한 원래 표시 구조 변이 앱이다. 각 빌드는 새 suffix/product/bundle/marker/output을 사용했다. Host가 없는 첫 씬의 무저장 path probe exit0 후 실제 `persistentDataPath`의 realpath와 알려진 보호 경계를 대조하고 QA 저장의 부재를 확인했다. 사용자 iOS/실저장과 unknown QA PID97399/옛 QA 저장은 조회·조작하지 않았다.

- 최종 compile/EditMode90/PlayMode31은 실제 exit0, 실패·skip0이다. XML과 명령/프로세스 exit는 `engine-bound.json` 및 `bound-*`에 결속한다.
- 실제 제품 `AdamArtWorld.Draw`를 사용한 660×440/1100×720 × b1/b2/b3 × 4frame × 좌우 × 본체/강조 mask의96개 픽셀 단언이 PASS다. 독립 기준은 window 기준 발 anchor를 고정하고 x1.12/y0.85 스쿼시한 같은 원본 sprite/frame/flip의 GPU 출력이다. 37,014/37,014 표본이 일치하며 최대 RGB 차이는1이다. 이 제어 fixture는 자연 군집/입력/가독성·성능 수용을 뜻하지 않는다.
- 격리 프로젝트 한 곳에서 `ReactionMatrix(anchor,1)`만 복구한 정확한 원래 구조는 build exit0 후 실제 첫 픽셀 단언0/342로 exit2였다. 동일 최종 QA 드라이버·DLL로 정상 소스를 복원한 build/capture/reload는 모두 exit0이다. 원래 구조와 최종 소스/드라이버 SHA 비교는 `final-binding.json`에 있다.
- 실제 이전 Unity binary의 schema1 R02 QA 원문을 새 Host로 Load한 뒤 primary/legacy backup byte 불변과 archive 미생성을 단언했다. 첫 v2 Checkpoint는 두 원문을 `.pre-v2`에 정확히 보존했다. envelope3, World2, Profile/World 미지원 중첩 필드는 정상 legacy backup이 있어도 새 Host Load/Checkpoint/RetrySave/Reload와 직접 store Commit이 보존 거절하며 두 원문 byte가 그대로이고 archive도 생기지 않는다.
- archive 게시 경로를 자체 파일로 막은 첫 Checkpoint는 원본을 보존하고 같은 operation/payload 재시도를 유지했다. 자체 장애를 제거한 RetrySave와 같은 operation의 재전송이 성공했고 원문 archive/primary를 다시 쓰지 않았다. 별도 재실행 Load는 기록된 State/World 직렬화와 정확히 같았다. 이 비교는 Load 직후 수행한다. 정상 앱 종료의 기존 `OnApplicationQuit` Checkpoint는 새 revision/operation을 쓰므로 종료 후 primary 전체 byte 불변을 주장하지 않으며, 종료 후 backup은 비교한 State 원문과 같다. 실제 명령·exit·원문 SHA/거절·재시도/재실행 근거는 `schema-summary.json`, `final-separated/commands.json`과 로그에 있다.
- 저장 거절 예외가 Development Console을 켜 화면 기준 렌더를 가린 초기 실패 앱/픽셀/로그를 보존했다. 콘솔 setter만으로 큐에 든 표시를 제거하지 못해 저장 검사와 픽셀 검사를 새 프로세스로 분리한 후 최종 옛 구조 FAIL→원복 PASS를 확인했다. 화면/제품 기대값을 완화하지 않았다. 별도 최초 wrapper는 macOS 경로를 product 이름으로 단정해 실패했으며 실제 bundle 경로·suffix/realpath로 대조하도록 고쳤다.

자체 Editor build/시험 subprocess의 실제 exit와 Player 자식의 정확 PID/exit·같은 app identity writer 부재를 확인했다. 실패/변이/최종의 각 자체 QA 파일은 원래 부재로 복구했고, 최초 v2 이행에서 생성한 자체 archive도 원문/hash를 기록한 뒤 자기 경로에서만 제거했다. 기존 PC6의 존재/해시는 각 실행 전후 보존했다. 원본/설정의 자동 비override import block 및 빈 icon 갱신만 baseline byte와 대조해 원복했으며 보호 파일 결속은 `preservation.json`이다. Core 소스/공유 시험/CIhooks/checker·아트/폰트 원본은 변경하지 않았다.

최종 source/app/DLL/provenance/XML/실제 명령·exit/보존은 `final-binding.json`과 그 SHA 참조에 묶는다. 초기에는 Orca ID를 provider session ID로 잘못 선택해 usage 조회가 실패했다. 후반에는 실제 `CODEX_THREAD_ID`의 start/end 기준점을 기록했으며 초반 구간은 미측정, 청구액·절감률은 미계산이다.

제품/Unity/Editor/GUI 소유를 root에 반환한다. 같은 독립 코드/UI 최종 리뷰·GC 최종·commit/push·해당 SHA CI는 root 후속이다. 미지원 R03 콘텐츠는 대기를 유지하며 새 보스/진화/콘텐츠를 지원했다고 주장하지 않는다. native active 입력·자연 수동 전투·모바일/IL2CPP·실기기·성능은 NOT_RUN이다. helper/worker를 추가하거나 커밋/push하지 않았다.
