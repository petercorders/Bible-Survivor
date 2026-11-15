# R02 아트 인계·독립 QA 상태

최신 2026-11-15: footpivot 정정과 검수된 schema2 DLL 통합은 구현자 compile/Edit90/Play31/mac build·Player 저장/reload·96실제 픽셀 비교를 통과했다. 최종 통합 코드/PC 시각 독립 재검토는 진행 중이다. 아래 이전 단계의 임포트/동기화 보류는 당시 이력이며 현재 게이트와 구분한다.

2026-11-14. 원본은 별도 `bible-survivors-docs/art-genesis-r02/`에 보존하며 제품 승격 전이다. Unity 구현자는 명시 개발용 기하 표식으로 R02 기술 연결을 계속하고, 최종 sidecar 임포트는 독립 검수 후 진행한다.

- 제작 인계: B1 시기의 눈/B2 분노의 사냥개/B3 갈라진 가면,32×32×4/8·8·6fps. 제작자 자체 verify7단계 PASS·오류 입력16건은 독립 QA 결과로 합산하지 않는다.
- 출처 구분: Artifact Design type 캔버스를 실제 만들었으나 게임 PNG는 Claude 작성 numpy 스크립트 결과이며 Design 제품 내보내기가 아니다. 캔버스 렌더는 미확인이다. 개인 캔버스/원본 blob·테스트 캐시는 공개 통합 대상이 아니다.
- root 예비 관측: 단독 보드에서 눈/사족 짐승/가면의 종 구분이 보인다. 이것만으로 혼잡 장면·모션·피격/게임 렌더를 통과 처리하지 않는다.
- 독립 QA 요청: 원본/PNG/manifest·계약/재생성·원본03/font 보존, QA 직접 오류 주입/CLI 명시 거절, 움직임/발접지/flip·40/50 혼합/흑백 판독을 검토한다. 보고 원본은 `art-readiness-reviews/r02-01-review.md`와 `r02-01-gc.md`다.
- 판단 대기: silhouette IoU 최대0.73과 기존03 내부 기준0.57의 차이, 붉은 B2의 피격 색 반응, pivot(16,1)의 원본03 정합. 수치만으로 자동 실패시키지 않고 계약·실제 이미지·게임 반응 범위를 구분해 판단한다.

Unity 임포트/실렌더/새 제품 파티클 겹침·모바일 실물은 NOT_RUN이다. QA가 결함을 확인하면 같은 제작자 수정→같은 독립 QA 재검토를 거쳐 최종 source/hash를 Unity 단일 작성자에게 인계한다. 원본03/font·콘텐츠 수치/판정은 변경하지 않는다. 사용 중인 iOS 앱/기기/저장은 조회·조작하지 않는다.

## 독립 QA 1차 결과·수정 루프

그림/12프레임 원본 RGBA·실루엣/40·50흑백/모션 표본은 PASS다. pivot16,1은 원본03과 정합했고, IoU 수치와B2 tint만으로 시각 결함을 만들지 않았다. 실제 Unity 피격/새 파티클 겹침은 별도다.

전체 오프라인 인계는 FAIL: HIGH R02-01은 잘못된PPU/좌표계 계약을 canonical 재생성해도 전체verify가0인 문제, MEDIUM R02-02는 없는 선언 참조/필수 메타/bbox의 단독검사PASS, MEDIUM R02-03은 중첩 타입의 traceback 및 숫자boolean 수용이다. QA 독립54입력은 명시거절35·수용16·traceback3이었으며 수용16을 모두 개별 결함으로 세지 않았다. 제작자16자체검사와 합산하지 않는다.

root가 같은 제작자에게 지원 계약 자체의 검증·선언 경로/실측 필드·타입 안전성을 수정하도록 돌려보냈다. PNG 재제작은 요청하지 않았다. 같은 독립 QA가 같은 오류와 canonical 변이를 재주입한 뒤 최종승격 여부를 판단한다. Unity 담당에게 최종 아트 임포트 보류·개발용 기술 연결 지속을 통보했다. 원격 Design 실행은 로컬자료만으로 독립 확증되지 않았음을 유지한다.

제작자 수정 인계: r02-support-1의 지원값/닫힌 schema·타입 선검사, 선언 경로/역할/실파일/해시, 권리·출처/실측 bbox 검증과 재생성/오류 재주입 도구를 추가했다. PNG3·기존manifest/계약/격자·원본03/font는 byte 불변이라고 보고했다. 제작자54개 재생·추가18입력/5변이 통과는 독립 결과로 합산하지 않는다. 같은 QA에게 r02-02 재검토를 요청했으며 보고서는 `art-readiness-reviews/r02-02-review.md`/`r02-02-gc.md`로 남긴다. 독립 결과 전 최종 승격 보류는 유지한다.

## 독립 QA 2차 결과·중복 source 수정

같은 QA가 기존54입력을 직접 재주입해 모두 명시 exit1·traceback0을 확인했고, canonical PPU16/top-left 재생성도 전체검사 exit1로 거절했다. R02-01~03은 CLOSED다. 원본13파일과 제작·안내9파일의 byte 불변, 정상 전체검사 exit0 및 기존 그림 표본 PASS를 유지한다.

추가 MEDIUM R02-04 때문에 최종 인계는 FAIL이다. pixel-source 목록의 같은 B1 이름을 앞에 복제하고 첫 행을31문자로 잘라도 뒤 정상 항목이 dict에서 덮어써 단독 checker는 exit0이었다. 같은 source의 converter는 exit1로 실패했다. source SHA를 갱신한 입력에서도 재현되므로 해시 검사로 대신 해결할 수 없다.

동일 제작자에게 dict 변환 전 이름 중복과 정확한 세 종 각 한 개를 검사하도록 반환했다. 원래 오류 구조·중복 순서/누락/추가 경계와 정상 baseline을 검증하고 PNG/격자/계약/manifest 불변을 유지한다. 제작자 결과 후 같은 QA의 독립 재주입이 필요하며 그 전 Unity 최종 임포트는 보류한다. R02 기술 checkpoint `2edd86e`와 해당 CI 성공은 아트 최종 인계 PASS를 뜻하지 않는다.

제작자는 `source_identity()` 사전 검사를 추가하고 원래31문자 중복 입력을 옛 checker0/converter1→수정 checker1·traceback0으로 재현했다고 보고했다. 정상 전체검사·재생성 동일 및 기존 원본 해시 불변도 보고했으며, 이는 아직 제작자 근거다. 같은 QA에게 원래 두 중복 입력과 정상 baseline을 독립 재주입하도록 요청했다. 새 검토 문서는 `art-readiness-reviews/r02-03-review.md`/`r02-03-gc.md`다. converter 단독의 유효중복 last-write-wins 잔존과 검사 선행 경계도 검토 대상으로 전달했다. 독립 결과 전 인계 FAIL/임포트 보류를 유지한다.

## 독립 QA 3차 결과·Unity 인계

같은 QA는 두 원래 중복 입력을 새 temp에 직접 주입해 옛 checker0→새 SOURCE1·traceback0을 확인했다. 정상2·오류8경계, identity 검사 제거0→원복1, 원본13파일 불변과 정상 전체verify0도 직접 확인했다. 유효중복은 converter 단독0이지만 전체verify4단계에서 SOURCE1·최종1로 거절돼 승격 관문을 우회하지 못했다. R02-04 CLOSED, 신규 HIGH/MEDIUM 없음, 기존 그림 표본 PASS 및 **오프라인 인계 PASS**다.

root는 검사0인 같은 입력만 Unity 담당에게 인계했다. 별도 strict sidecar 임포터/로더·안정 sprite GUID/rect/pivot/fps와 실제 PC 660/1100 혼합·피격/P1 겹침을 후속 검증한다. Art03/font는 불변이며 private Design 자료는 공개 통합에서 제외한다. R03 Core schema 작업은 다른 작성자가 맡아 Core DLL sync를 최종 인계 전까지 보류한다. 이 PASS는 실제 Unity/앱·원격 Design 확증·모바일/성능 PASS가 아니다.

## Unity 준비 checkpoint 독립 리뷰

이전 DLL을 고정한 실제 시트 연결은 독립 코드·PC 시각 리뷰에서 신규 HIGH/MEDIUM 없이 준비 범위 PASS다. `docs/reviews/unity-r02-art-code.md`와 `unity-r02-art-ui.md`에 소스349/앱334·승인21입력·실제 frame/flip·24조합 및 거절 검사 범위를 기록했다. 구현자의84Edit/30Play를 리뷰어 본인 실행으로 합산하지 않는다. 50근접 군집의 B2 가림과 원본보다 밝은 화면색은 비차단 디자인 피드백이며 연속 영상/모든 짧은 P1·tint 순간·native active 입력·모바일/성능 수용은 확인하지 않았다.

최신 schema2 Core의 독립 리뷰에서 별도 중첩 저장 필드 보존 결함 R03S-01을 찾아 수정 후 닫았다. 재검토에서 내부 World 버전이 미래 버전이면 옛 backup으로 돌아가 원문을 대체할 수 있는 R03S-02를 추가 발견해 수정 중이다. 아트 준비 PASS를 최신 DLL 조합의 PASS로 바꾸지 않으며, 저장 수정·같은 리뷰어 재검토 뒤 최종 sync/rebuild와 결속 재확인을 진행한다.

후속 최종 저장 재검토에서 R03S-01/02 모두 CLOSED, 신규 HIGH/MEDIUM 없음으로 순수 저장 slice PASS다. 리뷰어는 원래 입력을 포함한25경계와 정확한 World guard 제거 변이 실패→원복을 직접 확인했다. root는 P2/발 pivot 최소수정 뒤 동일 Unity 작성자가 검수된 Core를 sync/rebuild하도록 승인했다. 순수 Core PASS와 아직 실행 전인 최종 앱 조합 검증은 구분한다.

디자인 피드백에 따라 적 본체만 실제 화면의 발 y·동률 ID 순으로 정렬하는 표시층 수정도 준비했다. 이전 DLL 기준87Edit/직렬30Play 및 정렬제거 원래 구조의 assertion 실패·원복 성공 근거를 별도 기록했다. B2의0.09초 피격 반응은 캡처에서 관측됐으나 군집 고유색 면적이 일부 감소해 보편적인 가독성 개선이라고 주장하지 않는다. 원본 PNG/팔레트·플레이어/그림자/P1/HUD 순서와 Core 전투 순서는 유지한다. 최종 DLL 조합의 독립 재검토는 후속이다.
