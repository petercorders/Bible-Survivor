# 전체 비주얼 품질 개선 — Claude Design / Fable 5.2

사용자 승인: 이번에는 Claude Fable 5.2와 실제 Claude Design으로 전체적인 퀄리티를 고급스럽게 변경한다. 기존 턱뼈 위치·몽둥이·타격감 작업을 포함한 하나의 시각 개선 작업으로 묶는다. 스타일 기본값은 기존 탑다운 픽셀 아트이며 사진풍/3D/새 게임 규칙으로 바꾸지 않는다.

디자인 주도권은 실제 Fable 5.2/Claude Design 담당에게 둔다. 현재 실제 게임 캡처와 격자/에셋 계약을 읽고 팔레트·재질감·명암·테두리·여백·타이포·효과 밀도를 통일한 디자인을 만든다. 대상은 캐릭터/무기 읽힘, 공통 지형/그림자, HUD/선택·보상·완료 패널과 버튼, 명중·처치 표현이다. 챕터별 적을 다른 종으로 바꾸거나 기존 경고·경험치·플레이어를 장식에 묻히게 하지 않는다. 필수 보스 예고는 lowFX에서도 보존한다.

현재 화면과 개선 방향을 비교할 수 있는 실제 Design 아트보드와 컴포넌트/토큰·정확 에셋 인계 계약을 먼저 고정한다. 방향 설명만 납품하지 않고 같은 디자인에서 사용할 게임용 픽셀 원본·공통 UI 조각을 별도 제작 단계로 연결한다. Design 캔버스/렌더/게임용 격자·PNG 출처를 구분하며 다른 모델의 결과를 Fable Design 제작으로 부르지 않는다. 기존 외부 원본은 보존하고 새 외부 art-visual-quality-01에 작성한다.

Core 판정·데이터·저장·R01~R15 진행·R09→R10 원자 전환과 입력 중복 방지는 유지한다. UI 표현 교체를 이유로 보상 카드 수·키·저장 retry 동작을 바꾸지 않는다. 유료 구매/계정 설정 변경은 포함하지 않는다. Claude/Fable 승인은 이번 시각 개선 범위에 한정한다.

소유는 디자인/픽셀 원본·UI 표현·월드 표현·통합 실행으로 나누되 공유 파일은 한 명만 편집한다. Unity 런타임 작성자는 현재 턱뼈 수정 소유 반환을 완료했으므로 새 구체 배정 전 추가 제품 편집하지 않는다. Editor/빌드/GUI는 통합자 한 명만 실행한다. 공통 스타일을 반영하는 단일 PC 통합본으로 인도하며 작은 컴포넌트마다 앱을 재빌드하지 않는다. 마지막에 변경 관련 검사·대표 전투/모달 화면과 실제 입력·짧은 스윙/명중 영상·독립 코드/시각 리뷰·프로젝트GC·커밋/CI로 묶는다. 전체 픽셀/과거 시험 행렬은 반복하지 않는다.

사용자 플레이 PID98774/window244879/bundle r1580111c7154f5 및 저장, 기존 iOS/unknownparticlesqa/pivot9534f7594886은 계속 불조회·불조작·불종료다. 새 격리 QA identity에서만 작업한다. 현재 턱뼈 수정 관련 검사/회귀/A-B 증거는 소스가 동일하면 재사용하고 최종 시각 리뷰만 통합본에서 한다.

## 제작·표현 소유 계약

Design 방향은 잉크 남색/청동/양피지이며 외부 art-visual-quality-01의 실제3보드·tokens.json을 원본으로 사용한다. 원본 Art03와 챕터 아트는 수정하지 않고 독립 검수 뒤 sidecar로 적용한다. 사용자 전체 시각 개선 승인을 이 구체 공통33ID 계약에 연결한다. 캐릭터/적은 새 종으로 다시 그리지 않고 배경 대비·그림자·효과/정보 계층으로 읽힘을 개선한다.

P1은 새 외부 art-visual-quality-01-ui에서 UI15ID, P2는 art-visual-quality-01-world에서 월드12ID, P3는 art-visual-quality-01-effects에서 효과4ID를 제작한다. 몽둥이2ID는 검수된 v2를 그대로 재사용하여 총33ID로 묶는다. 세 제작자는 Fable5.2로 Design 방향을 게임용 픽셀 원본으로 옮기며 기존Design캔버스·스크립트픽셀출처를 명확히 구분한다.

세 패키지 공통 manifest: schema=bible-survivors.visual-quality-art, schemaVersion=1, bundle=ui/world/effects, ppu=32, pivotSpace=bottom-left-pixel-edge, borderOrder=left,bottom,right,top. assets의 각 항목은 id/file/cell[width,height]/frames/fps/loop/pivot[x,y]/alpha/borders[left,bottom,right,top]/sha256를 포함한다. PNG strip frame0은왼쪽이며 투명RGB0, binary 또는 계약상blend만 허용한다. 기존ID의 빠진fps/loop/borders는 현재Art03 index에서 읽어 채우고 원계약을 유지한다. 신규keycap은Design의 L,R,T,B=[3,3,3,4] 의도를 런타임Vector4 L,B,R,T=[3,4,3,3]으로 정규화한다. 바깥contract원본은 수정하지 않고 제작manifest에 이 해석을 명시한다. manifest+해당PNG만runtime public whitelist이며 격자/source·generator·regen근거·한대표보드는외부에 보존한다.

U1은 AdamGame 및 새AdamHud/Choice표시helper·관련UI시험만, U2는 AdamArt/AdamArtWorld/필요Impact표현/새AdamVisualQualityArt와EditorImporter·관련World시험을 소유한다. U1은 HUD/모달표현만 바꾸고 기존 입력/상태분기를 보존한다. U2는 새 manifest loader와 일괄임포트/Editor/빌드/GUI 단독소유다. P1/P2/P3는제품/Editor/GUI를 건드리지 않는다. 3개원본과U1/U2로최대5제품작성자이며 검수자는제품편집하지않는다.

HUD의시간표시는실Core기준이며보스시간이180초를넘는경우3:06/3:00같은모순분모를피하고일반시간과보스경과역할을구분한다. 막대/아이콘/토큰은실Inventory/health/XP에서읽고 예시디자인숫자/카드문구를제품에하드코딩하지않는다. 기존캐릭터손잡이/회전교정과반픽셀pivot·lowFX/mute·R09→R10원자성·ExodusComplete는유지한다.

## 추가 사용자 피드백: 물맷돌과 세 무기 타격감

사용자가 물맷돌도 밋밋하며 다른 무기도 같다고 명시했다. 이번 표현 개선은 턱뼈뿐 아니라 물맷돌의 준비/실발사/실투사체 궤적/실명중과 성경의 페이지·접촉 리듬까지 같은 품질 기준으로 연결한다. 실제 Core 이벤트와 좌표를 사용하며 판정·피해·주기·저장은 변경하지 않는다. 빗나감에는 명중효과를 만들지 않고 lowFX/mute/pause/cleanup을 보존한다. U2가 기존 clip과 코드 표현을 우선 재사용하고 P3가 Design 방향을 제공한다. 추가 PNG는 정확 ID/계약과 독립 검수를 거친다. 기존 턱뼈 검사의 물맷돌/성경 PCM 불변 근거는 새 표현 변경에 전용하지 않으며 최종 실제 발사·명중·빗나감 영상/엔진 음향 근거를 구분한다.
