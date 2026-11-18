# 턱뼈 몽둥이·이동 중 휘두르기·타격감

사용자 피드백: 턱뼈가 뼈 몽둥이처럼 보이고, 이동 중에도 바로 앞/손에서 휘두르며, 명중 반응이 더 분명해야 한다. 기존 지속 승인 안의 표시·음향 개선으로 진행한다. Core 판정·피해·속도·범위·저장 형식은 바꾸지 않는다.

추가 사용자 승인: 타격감에 디자인이 필요하면 이번 작업에 한해 Claude Design과 Fable 5.2를 일시 사용한다. 기존 Claude 제외 규칙의 제한적 예외이며 코드/검증 담당이나 전역 기본 모델을 바꾸지 않는다. 실제 요청 모델·해결된 모델·Design 도구 사용을 구분해 기록하고, 사용 불가를 다른 모델로 몰래 대체하거나 Claude Code/스크립트 PNG를 Design 제품 제작으로 보고하지 않는다. 디자인 자문/별도 캔버스는 외부 새 폴더에서 수행하며 기존 아트 작성자·Unity 작성자 파일 소유와 충돌하지 않는다.

읽기 진단: AdamArtWorld.Draw는 GUI.BeginGroup(arena) 안에서 jawbone FacingMatrix(pos, facing)를 적용한다. 기존 enemy footpivot에서 확인한 그룹/전역 pivot 차이를 턱뼈의 실제 렌더에서도 재현해야 한다. 공격 도중 현재 facing으로 매번 회전하는 점과 프레임별 attach에 중심 고정 sprite를 놓는 방식도 손잡이 연결과 비교한다. 이 단계는 소스 가설이며 실제 화면 PASS가 아니다.

외부 아트 작성자는 새 art-jawbone-feedback-01만 소유한다. 기존 Art03/제품을 수정하지 않고 Codex 스크립트 픽셀로 두 후보를 만든다: 새 32px 뼈 몽둥이 held 1frame(명시 grip 픽셀과 방향), 기존 UI에 대응할 16px 아이콘 1frame. 두꺼운 타격부·짧은 치아/턱 흔적·좁고 어두운 손잡이로 몽둥이 실루엣을 읽히게 한다. 계약/source/manifest/최소 생성·재생성 검사/대표1보드만 납품한다. Claude 제외, script pixels 출처를 명시한다. 최종 manifest/그립은 통합자에게 전달한다.

Unity 작성자는 AdamArtWorld와 필요한 새 jawbone 표현 helper/loader/importer·관련 시험, AdamImpactAudio/Particles의 턱뼈 전용 조정 및 격리 QA만 소유한다. 기존 발y/실루엣/다른 무기·보스·pause/lowFX를 보존한다. 아트 후보 독립 검수 전 제품 복사하지 않는다. 손잡이 기준 배치·초기 방향 유지·휘두름의 준비/빠른 타격/짧은 회수로 읽히게 하되 Core clock·명중 사건만 소비하며 게임 시간을 멈추지 않는다. 명중할 때만 짧은 둔탁한 소리/접촉 강조를 보강하고 새 판정·가짜 명중·전역 화면흔들림은 추가하지 않는다. lowFX/음소거는 기존 설정을 따른다.

사용자가 플레이 중인 PID98774/window244879/bundle r1580111c7154f5와 그 저장은 추가 조회·입력·종료·원복하지 않는다. 기존 iOS/unknownparticlesqa/pivot9534f7594886도 불조회다. 새 QA identity·noHost probe·realpath·정확 child PID·자기 저장만 관리하며 Editor/build/GUI는 Unity 통합자 한 명만 실행한다.

검증은 변경 관련 배치/방향/clock·명중/무명중·pause/lowFX/audio 검사와 실제 이동/회전 중 짧은 대표 A/B로 제한한다. 원래 회전 기준점 회귀를 한 번 실제 실패로 확인하고 복원한다. 대표 660/1100의 짧은 영상/핵심 화면으로 손잡이 연결·앞쪽 스윙·명중 반응을 독립 코드/시청각 리뷰한다. 새 전체 조합/해시/프레임 행렬과 과거 전체suite 반복은 필요 없다. 문서 수정으로 앱 재빌드하지 않는다. 끝에 프로젝트 GC·커밋/업로드·해당SHA CI로 마감하며 새 사용자 플레이 앱 실행은 이번 개선 인도 단계에서 한다.
