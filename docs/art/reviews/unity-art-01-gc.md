# Unity art 01 GC 근거

2026-11-14 독립 아트 QA의 관찰 기록이다. 규칙 저장소·학습 엔트리·코드·CI·git은 변경하지 않았다. root가 통합 회고에 수집할 근거이며 자동 승격을 뜻하지 않는다.

| 관찰 | 독립 근거 | 통합 시 유지할 조건 |
|---|---|---|
| 납품 manifest 통과가 실제 엔진 등록 보존을 대신하지 않음 | 원본/repo/Resources PNG79와 font4 바이트, 실제 meta197frame rect/pivot/border, Point/PPU32를 직접 대조하여 불일치0 | 납품 SHA와 실제 import registration을 함께 보존 |
| offline outline QA와 Unity 지원은 증거 층이 다름 | 기존 font01 bytes 유지 + 개발자 실제 FontEngine 한글 index/샘플 atlas XML + 최종 screenshot을 각각 확인 | offline 반복 PASS를 Unity실행 횟수에 합산하지 않음; 전체 glyph 픽셀 렌더와 전역 fallback은 미검증 유지 |
| Python 카드 실측만으로 실제 문자열/레이아웃을 입증할 수 없었음 | 실제 C# Describe()71개 exact 문자열 및 Unity GUIStyle CalcSize 테스트 소스와 Passed XML 확인 | 71개 데이터 생성과 실제 소비자 문자열을 같은 계약에서 대조 |
| 사망은 core 시간이 그대로인 terminal 전이와 presentation clock이 모두 필요 | 옛 terminal 갱신 조건/조기 modal/정지 프레임의 실제 변이 실패1 + restored0fail XML을 각각 확인; 사망 pose 및 이후 modal 이미지 직접 관찰 | 같은 회귀 자체를 실패시킨 증거를 보관; 다른 변이로 대신 세지 않음 |
| 정지 이미지는 모든 중간 프레임/속도를 입증하지 않음 | death-fixed-resume에는 누운 자세, frame0/2/3은 이미 modal; jawbone06/10은 서로 다른 접전 상태 | 화면 관찰과 동작 테스트의 주장 범위를 분리 |
| 증거 후 병행 편집은 시험 수치의 적용 범위를 바꿈 | final-source-build-sha 대비 AdamPlayTests.cs 1개 불일치, git diff에서 사망 Digit경계 테스트3개 추가 | root 최종 통합 때 현재 소스 해시와 최종 XML/build를 다시 결속(UAR-01 LOW) |
| 연출 gallery와 자연 전투는 다름 | AdamArtEvidence가 실제 core ID/time 후 위치를 직접 배치하며 화면 밖 drop도 유지 | 아트 가독성 관측으로만 사용, 전투 밀집/밸런스 PASS로 쓰지 않음 |

판정: 관측한 macOS Unity 아트·폰트 통합 인계 PASS, 새 HIGH/MEDIUM 없음. 본인 엔진 실행 NOT_RUN; 개발자 39Edit/10Play는 읽은 외부 실행 증거다. 최종 통합 테스트·별도 UI 직접조작·GC 반영·commit/push는 root 소유다.


## 후속: UAR-01 종료 근거

동일 날짜 death digit 수정 후 독립 재대조에서 최신 `evidence/unity-death-digit-20261114/final-source-build-sha.json`의 231개 실제 SHA가 전부 일치했다. Adam 전체 파일229개 누락0 + 빌드DLL2개로 구성되며 기존 snapshot 대비 AdamGame/AdamPlayTests/Adam.Unity.dll만 달라졌다. 원본 입력229개, 등록 PNG79/TTF4의 bytes/meta/GUID와 Core DLL은 불변이다.

기존 표의 UAR-01 LOW는 **CLOSED**다. 추가된 digit3건을 포함하는 실제 fixed Play13/13·Edit39/39·compile0·build0와, 옛 가드 baseline10PASS/3FAIL 및 격리 원회귀0/3FAIL→원복3/3PASS XML을 직접 읽었다. 새 hash snapshot이 현재 소스·시험·빌드에 일치하여 과거 시험 수치의 적용 범위 문제가 해소되었다. 기존 관측 시각 PASS 범위는 그대로이며 UA01/ART-UI-01 별도 reviewer의 판정을 합산하지 않는다.

도구 관찰: 첫 ad hoc reader는 log-check.json을 dict로 가정했으나 실제 list여서 AttributeError/exit1이었다. list 처리로 교정한 reader는 exit0이었다. 이는 제품 실패로 분류하지 않으며, 구조 확인 뒤 검사하는 것으로 충분해 별도 전역 규칙 승격은 제안하지 않는다.

GC 수집 후보 강화: 병행 작업의 마지막 통합에서는 최종 파일 집합 누락 검사와 SHA/시험XML/빌드DLL 결속을 함께 남긴다. 이번 범위에서 본인 Editor·Player·GUI는 실행하지 않았고 원본·코드·저장은 수정하지 않았다. 남은 통합/CI/commit/push는 root 소유다.
