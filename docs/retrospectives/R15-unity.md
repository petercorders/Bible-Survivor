# R15 Unity 회고

2026-11-18 KST. Host/catalog·기존 Host/Play 경계와 신규 Host6케이스를 작성하고 Core/art 독립 PASS 및 UI/World 반환 뒤 단독 실행했다. Core211/관련8·원 endpoint 변이/선택 Resume15 변이·실좌표 시리즈를 재사용했다. 신규 UI18/World5를 포함한 Edit131은 최초130 PASS+준비오류1, 교정 메서드2 PASS이며 Play14/정적2검사 PASS다. 전투 전체 suite를 재실행하지 않았다.

R12~R14의 seed/DataMember/유효 phase/무기 clock/Reloadpause/예상 오류 콘솔·rawlog/660 두 줄 HUD 교훈을 재사용했다. 카드 확인용 cloud5→4를 기술 변경으로 명시하고 같은 seed의 실제 보스 처치/후보2/골드100/pending2·Choose2/25/280을 build 전에 확인했다. 네 history의 필수 진화를 없애지 않았고 실제 앱의 pending·후보·Choose를 구분했다. 최종3화면은 예고/실제 보상/전체 완료만으로 제한했다.

오류1: 새 Host 시험에서 공개 적탄 API를 Position으로 추정해 compile FAIL이었다. 실제 X/Y 필드로 교정했다. 오류2: 비고갈 기술 seed를 Session.Capture 전에 바꿔 덮어썼다. Capture 뒤 commit 전으로 옮기고 해당2케이스만 재실행했다. 오류3: 앱 driver가 직접 Session.Hit와 World lifecycle을 같은 단계로 취급해 즉시 World 탄 목록 empty를 기대했다. 기존 World 시험의 Advance(0) 경계를 읽고 warning false와 실제 World 정리를 분리했다. 기능 source는 고정했고 실패 앱/원driver/화면/exit2·저장 복구를 보존했다. 최초 요구의 단일 앱 경계 때문에 root에 교정 build를 질의했으나 지속승인 범위라는 답을 받았다; 기능 source가 고정된 실제 QA 교정에서 재승인 대기를 반복하지 않는다.

두 번째 준비 호출은 기존 QA 씬에 캡처 컴포넌트가 남아 BuildPipeline 전 gate exit1이었다. 제품 씬을 격리 QA에 재복사해 해소했고 정상 앱 산출2회와 Editor build 호출3회를 구분했다. 새 검사 프로젝트/전역 규칙/추가 suite를 만들지 않았다. 개선 후보는 기존 fixture 재사용 때 driver뿐 아니라 QA 씬의 설치 상태도 함께 초기화하고, 직접 Session 조작 후 World.Advance 단계의 정리/투영을 대조하는 기존 준비를 재사용하는 것이다. 자동 GC 승격은 하지 않았다.

최종 probe/기록/새 process 모두 exit0, 자기3저장 absent 복구와 정확 PID 종료를 확인했다. 보호 사용자 앱/저장과 타인 변경은 건드리지 않았다. source108·앱 DLL/provenance/catalog·27 sprite·실명령/XML/trace3장 결속 뒤 소유를 반환한다. Sol 기본/실제 gpt-6.1-sol, Claude/Astra/helper 없음; usage.json의 지연 provider interval만 관측치이며 비용/절감률은 미계산이다. 최종 독립 code/UI·GC·commitpush/CI는 root 후속이고 R15 다음 책·무기·모바일은 착수하지 않는다.

Root GC 준비: 최종3원본 화면 독립 UI PASS와 실제 실탄 이동순간은 정지화면 미관측인 한계를 접수했다. 실탄동작/Load/cleanup은 생산자 trace와 기존Core/Host검사로 구분하며 UI리뷰 실행횟수로 합산하지 않는다. 정상앱2개와 BuildPipeline전 중복씬gate exit1은 분리한다. 프로젝트 회고만 기록하고 전역 학습 상태/규칙을 자동편집하지 않는다. 최종 코드 리뷰·필수커밋/원격CI 성공은 별도남은 게이트다.

Root 최종 GC: 독립 Unity코드도 PASS로 반환돼 Core/아트/UI까지 막는 결함이 없다. 코드리뷰는108입력 전체 해시행렬을 다시 계산하지 않고 관련12입력·DLL/catalog 결합만 확인했다. 기존 Core실좌표R09→R15와 최종앱 기술R14→R15를 재사용했고 새 연속15라운드앱 실행을 추가하지 않았다. R13/R14는 정상앱각1회, R15는QA lifecycle교정으로2회이며 실패를숨기지 않았다. Sol기본·Claude/Astra없음·공급자interval사용량만기록,누락비용0처리나절감률추정없음. 최종필수훅/해당SHA원격CI는커밋업로드뒤확인한다.

최종 업로드 CI 후속: 첫 d81337f의 art44150560621은 성공했으나 Core44150560639가210/211에서 R15 swept시험의 Passives[seal] KeyNotFound로 실패했다. 재사용 완료fixture의6슬롯에seal존재는보장되지않는다. 제품피해공식·Unity앱·가드가아닌 테스트 기대계산 한곳을 기존R12방식 TryGetValue/없는방어0으로정정했고관련8/8이통과했다. 이를Linux제품결함이나게임저장손상으로확대하지않는다. 독립최소diff리뷰와복구SHA원격CI를후속확인한다.

복구 독립 리뷰 docs/reviews/R15-ci-recovery.md PASS: 장비없으면방어0/있으면실level이라는 기대식은 raw18/min1 피해와 기존충돌·소멸·사망·cleanup assertion을그대로검증한다. Linux의missing관측을모든플랫폼선택순서의원인으로단정하지않았고 기존제품/앱검증은재사용했다. 복구커밋의필수훅과원격Core/art CI를 마지막게이트로확인한다.
