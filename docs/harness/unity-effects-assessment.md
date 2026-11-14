# Unity 제공 기능·공식 에셋 활용 검토

2026-11-14. 사용자 “유니티에서 제공하는걸 사용할수 있는지도 봐줘”에 따른 읽기 전용 조사. 현재 패키지/표현 코드를 읽고 Unity 공식 문서·공식 게시자 상품 페이지를 확인했다. 신규 패키지 설치/샘플 임포트/렌더러 전환/구매·약관 동의는 하지 않았다. 구현·게임 내 가독성/성능 검증은 별도다.

## 현재 기반

Unity6000.6.9f1, URP17.6.0과 Renderer2D 설정이 있다. Package manifest에 Audio·Particle System 기본 모듈이 포함돼 있다. Cinemachine·VFX Graph는 직접 dependency에 없다. 현재 아담 전투는 `AdamArtWorld.Draw`의 OnGUI/GUI.DrawTexture 기반으로 표시하므로 Scene의 SpriteRenderer/ParticleSystem/카메라 후처리가 전투 그림에 자동 적용되는 구조가 아니다. 패키지가 존재하는 것과 화면 효과가 실제 연결된 것은 다르다.

## 선택지

| 기능/에셋 | 게임 활용 | 판단 |
| --- | --- | --- |
| 기본 Particle System | 명중 스파크·돌가루·먼지·처치 파편·성경 빛 조각 | 우선 후보. 기존 모듈을 쓰고 이벤트당 burst/짧은 수명/풀 재사용. [공식6000.6 안내](https://docs.unity.com/en-us/engine/6000.6/manual/visual-effects/particle-systems) |
| AudioSource와 Audio Mixer | 세 무기 타격음·동시 발음/음량·효과음 그룹 | P0 음원 재생에 Unity 오디오 사용. Mixer는 효과/그룹/믹싱 기능이며 명중 샘플 자체를 제공하는 기능은 아님. [공식 Audio Mixer](https://docs.unity.com/en-us/engine/6000.6/manual/audio/mixer) |
| Cinemachine Impulse | 큰 타격/보스의 짧은 카메라 흔들림 | 후속 후보. Camera 기반 연결과 HUD 분리·강도/끔 옵션을 설계해야 함. P0 도중 패키지 교체는 하지 않음. [공식 Impulse](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineImpulse.html) |
| Particle Pack — Starter Assets | 불·폭발·얼음·소멸의 기존 예제/재사용 | Unity Technologies 공식 게시, 확인 시점 FREE·v3.3. URP 호환 표에6000.0/6000.3 기재; 현재6000.6/2D Renderer의 실제 동작은 별도 시험. [공식 상품](https://assetstore.unity.com/packages/vfx/particles/particle-pack-starter-assets-127325) |
| Happy Harvest | 2D 조명·그림자·Shader/Particle 효과 연결 참고 | Unity 공식 FREE 2D URP 샘플. 전체 프로젝트를 합치지 않고 별도 프로젝트에서 필요한 기법만 검토. [공식 상품](https://assetstore.unity.com/packages/templates/packs/happy-harvest-2d-sample-project-259218) |
| VFX Graph17.6 | 많은 GPU 파티클 | 현 Android GLES3 경로의 기본 효과로는 제외. 공식 요구는 compute/SSBO이며 OpenGL ES 미지원. Vulkan/Metal·다른 target의 실제 검증 없이 모바일 공통 지원으로 채택하지 않음. [공식 요구사항](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.6/manual/System-Requirements.html) |

## 현재 작업에 적용할 순서

1. P0의 정확한 명중 사건·기존 픽셀 스파크·피격 반응을 유지하면서 소리는 Unity AudioSource 기반으로 구현한다. 엔진 재생 기능과 원본/사용권 확인된 음원 제작을 구분한다.
2. 별도 작은 Scene/QA 사본에서 기본 Particle System으로 스파크·먼지·금빛 burst 세 프리셋을 비교한다. 기존 아트 팔레트·Point 픽셀·명중 원점·씬 정렬/GUI 합성·safeArea/화면배율을 맞춘다. 필요하면 전투 화면 안의 카메라 RenderTexture 합성 또는 SpriteRenderer 기반 표현 전환을 비교하되, 선택 전 전투 전체를 재작성하지 않는다.
3. 공식 Particle Pack은 빈 프로젝트/격리 폴더에서 조사한다. 게시자 페이지도 기존 ProjectSettings를 덮지 않도록 별도 프로젝트 임포트를 안내한다. 필요한 텍스처/머티리얼/프리팹만 후보로 분리하고 실제 고지·사용조건·출처/해시를 보존한 뒤 공개 저장소 포함 범위를 확인한다. 유료 에셋은 구매하지 않는다.
4. PC A/B에서 타격점·적 실루엣·색/밀도와 성능을 비교한다. 일반/저효과 설정, 적40/120, 풀·발음 상한, pause/재시작/로드 중복 없음과 Core 결과 불변을 확인한다. 사용자의 최신 독립 리뷰 범위는 PC이며 모바일 결과를 자동 PASS로 올리지 않는다.

원본 픽셀 아트는 Claude Design 담당과 유지하며 Unity 제공 도구는 재생·합성·조절을 맡기는 구성이 적합하다. 기존 공식 팩은 그림체·밀도·색을 조정해야 할 수 있으며 다운로드만으로 현재 게임과의 적합성을 확정하지 않는다. 현 시점 결과는 조사/후보 선정 완료이며 설치·실통합·실측 완료가 아니다.

## 후속 실행 상태

사용자가 조사 후 “진행해줘”라고 승인했다. P0 작성자에게 기본 Particle System의 스파크/먼지/금빛 burst를 별도 QA Scene/전투 영역 합성으로 실검증하도록 전달했다. P0 소스·앱·3무기 A/B를 먼저 고정한 뒤 별도 증거 단계로 나누며, 샘플 Scene 성공과 실제 게임 통합 결과를 구분한다. 새 패키지/전체 렌더러 재작성 없이 기존 모듈로 우선 시험한다.

공식 Particle Pack 획득은 Orca 브라우저의 실제 상품 페이지에서 “내 에셋에 추가”를 선택했으나 `login.unity.com`의 이메일/계정 로그인 화면으로 전환됐다. 해당 브라우저에서 인증된 세션이 없어 팩 추가·다운로드·임포트는 **BLOCKED(인증 필요)**다. 계정/자격 증명/다른 로그인 상태를 변경하지 않았고 비공식 재배포 다운로드를 사용하지 않았다. 상품 조회와 기본 Particle System 개발은 계속 가능하며 이 인증 차단을 전체 작업 차단으로 취급하지 않는다.

사용자 후속 지시 “로그인 petercorders chrome 있어”에 따라 기존 Chrome의 새 탭에서 같은 공식 상품을 열었다. 로그인된 세션에서 무료 에셋 추가·표시된 서비스/EULA 동의를 진행한 뒤 **“내 에셋에 추가되었습니다 / Unity에서 열기 / 내 에셋으로 이동하기”**를 실제 접근성 트리에서 확인했다. 계정 인증 차단은 해당 Chrome 세션에서 해결됐고 결제는 없었다. 비밀번호/쿠키를 읽거나 다른 브라우저로 복사하지 않았다. 추가 완료는 다운로드/임포트 성공과 구분한다. 동시 작업 중인 Unity 프로젝트를 잘못 열지 않도록 “Unity에서 열기”는 root가 누르지 않았으며, 단일 Editor 소유자인 구현 담당에게 격리 QA 프로젝트에서 다운로드·선별 검토하도록 인계했다. 팩 파일의 출처/해시/고지·2D 픽셀 적합성 검토 전 공개 원본 업로드·제품 임포트는 하지 않는다.

구현 담당 후속 관측: 격리 QA Editor의 MyAssets에서 다운로드가 완료돼 Import3.3 버튼으로 전환됐고, 실제 unitypackage167961562bytes를 ignored evidence에 보존했다. 원본 SHA256은 `a023e358cf8cfc53de10be7b8880c611e5e15a571fb6f18201cbef6c3ba52300`이다. pathname519개와 Readme/후보 GUID 의존을 확인했으며 Import는 누르지 않았다. PackageManager 표시는 Unity Companion License이며 StoneImpacts 후보에는 decal·여러 material·외부 GUID 의존이 있어 현재 픽셀 표현에 직접 채택하지 않았다. 원본 팩/비공개 원문은 공개 저장소 업로드 대상이 아니다. 이 결과는 구현자 실행 보고로, root의 독립 다운로드 재실행은 하지 않았다.

기본 Particle System의 QA전용 RT 합성은 세 무기×두 크기에서 전투 World bytes 동일·영역 밖 변경0·풀/저효과/정지 검사를 진행했다. 이동 중 가장자리 probe는 초기 근거가 불충분해 PASS 보류, RT aspect·명시 투영 assertion·동일 tick PNG로 재검증 중이다. 실험과 P0 실제 게임 통합은 별도이며 아직 파티클 제품 적용 완료로 올리지 않는다.

최종 구현자 인계: 기본 Particle System 별도 QA는1100/660의 세 무기6구간, 이동 중 실제 창 resize와 합성 inset, 정수 배율·클리핑·정지·저효과·256입자 상한 검사를 통과했다. 가장자리 누락은 수동 Simulate/Pause 뒤 Renderer.bounds0으로 cull되는 원인이었으며 방출 영역 bounds를 갱신해 수정했다. 해당 bounds 두 설정만 제거한 실제 앱의 같은 PNG 검사는 resize 전/후 각각 실패1, 정상 복원은 각각0으로 통과했다. 실제 제품 arena와 QA arena는 서로 다르므로 제품 HUD/모달·제품 파티클 연결은 여전히 NOT_RUN이다. 출처/세부 증거와 범위는 `../progress/combat-feedback.md`의 별도 실험 절을 따른다.
