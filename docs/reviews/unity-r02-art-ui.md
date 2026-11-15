# R02 아트 PC 시각 독립 리뷰 — 최종 통합 checkpoint

2026-11-15 · **최신 schema2 DLL에 결속된 source/실제 이미지 범위 PASS**. 신규 명백한 HIGH/MEDIUM 없음. native 조작·자연 가독성·모바일·성능·출시 승인을 뜻하지 않는다.

## 이번 재검토 범위

같은 QA가 이전 R02 원본·시트 및2026-11-14 준비 checkpoint 리뷰를 재사용하고, `docs/progress/unity-r02-art.md`의 최종절과 ignored `evidence/unity-footpivot-schema2-20261115/handoff.md`, `final-binding.json`을 읽었다. 새 변경은 표시층 foot anchor 정정과 schema2 DLL/provenance 결속이며 P2의0.04초 파생 alpha 강조도 함께 확인했다.

Editor·Player·GUI·앱·실저장·iOS·unknownQA 조회/조작은 하지 않았다. 구현자 compile/Edit90/Play31/build/schemaPlayer/reload/96단언을 QA 본인 엔진 실행 횟수에 합산하지 않았다. 본인은 Python/Pillow의 읽기 전용 파일·픽셀 대조와 view_image 관람만 수행했다.

## 최종 결속 확인

- source387파일·지정 최종 app334파일의 실제 SHA가 기록과 일치, drift0. final-binding의 artifacts와 XML SHA도 모두 일치했다.
- source manifest: `f72a25add0be9c48b6d2f800441669a65e5866e1446ded24670baf7f54a55079`.
- app manifest: `7c9306aaa5f22b518bf9883cb3ce6d98dbaa3dea4fe6be906ca34882dbc22a10`.
- embedded Core DLL은 최종 source DLL과 같은 `bece4324eb8d722205fecc0870f23320f99c07ccba0799fe6b7ec29beab5f54c`.
- provenance SHA `ff84e4d07f45aebb9399f729a7a5b584d4c24214d5a78d481dc3dcaa460cbb9e`가 같은 결속에 포함된다.
- bound Edit XML90/90, Play31/31, fail/skip0 및 build0/네 Player 명령 exit0 자료를 읽었다. 이는 구현자 실행 근거다. schema 저장 의미의 독립 코드 판단은 별도 담당 범위다.

이전의 “옛 DLL 준비 단계이므로 최신 DLL과 다시 결속해야 함”은 이번 source/앱 시각 증거에 대해 해소되었다. 이후 소스/DLL/앱이 달라지면 이 PASS는 자동 승계되지 않는다.

## 발 anchor·프레임·반전·강조

`AdamArtWorld.Draw()`는 `GUI.BeginGroup(arena)` 내부의 local anchor를 `anchor+arena.position`으로 바꿔 window 기준 ReactionMatrix에 넘긴다. x1.12/y0.85 squash를 발에 고정하고, 본체와 mask가 같은 anchor/frame/UV/matrix를 사용한 뒤 GUI matrix/color를 복원한다. 적은 발y로 정렬되고, mask는 해당 본체 바로 뒤 패스여서 다른 적의 앞뒤 관계와 분리되지 않는다.

파생 mask는 승인 시트의 alpha만 복사한 별도128×32 텍스처3개다. 원본 RGB/alpha/PNG/팔레트·Point·importer 계약을 바꾸지 않는다. R02 실제 명중의0.09초 반응 중 첫0.04초에만 opacity0.65가 적용되며 low에서는 꺼진다.

최종 `final-separated/capture`의 **96개 actual/reference PNG 쌍**(2해상도×3종×4frame×좌우×body/mask)을 읽었다. 원본 source alpha와 발좌표·squash·반전식을 별도 Python으로 계산해 화면을 다시 샘플링했다. **37,014/37,014 표본 일치, RGB 최대차1**, 별도 검사 exit0이었다. 96은 구현자가 캡처한 조건 수이며 본인이 앱을96번 실행한 것이 아니다.

96개 전체의 진단 contact sheet와660 B2/1100 반전 B3 원본 actual/reference도 열었다. 짧은 강조가 대상 몸 형태에 맞으며 떨어진 별도 흰 상자나 frame/flip 불일치, foot drift는 관찰하지 못했다. 기준 화면은 제품 ReactionMatrix를 부르지 않고 window foot anchor로 직접 행렬을 만들어 비교하므로 잘못된 local anchor를 기대값에 복제하지 않는다. 다만 본체/mask Draw API 일부는 공유하므로, 기존 원본·격자/임포트 독립 QA를 이 검사로 대체하지 않는다.

옛 local-anchor 한 호출을 정확히 복원한 구현자 변이는 동일 드라이버/DLL에서 build0 뒤 첫342표본 중0으로 assertion exit2였고 최종 정상 build/capture/reload는0이었다. 로그와 결속 자료를 직접 읽었으며 이 변이를 본인이 실행했다고 세지 않았다.

## P2·low·정지·HUD의 관측 경계

최종96쌍은 실제 제품 `AdamArtWorld.Draw`를 이용한 **제어 World 렌더**다. 최종 driver는 HUD/modal 전체 DrawGameView를 촬영하지 않으므로, 최신 최종 앱에서 native 정지/재개를 직접 조작했다는 주장은 하지 않는다.

P2 이전 단계 `unity-r02-hit-signal-20261114/after/capture`의 B2 단독 normal tick2/tick5,660 crowd normal/low tick0,1100 persisted pause를 열어 강조 on→off·low 비강조·정지창 글자/경계를 확인했다. 이들은 최종 foot/DLL 정정 **이전** 이미지이며 현재96개 foot 검증과 구분한다. AdamGame.cs SHA가 그 이전 build-inputs와 최종 모두 `ce2db4c1…`로 같아서 HUD 코드 불변 근거로 재사용한다. 이전 이미지의 발 위치나 DLL을 최종값이라고 표시하지 않는다.

0.04초 반응은 소스·고정 시점 표본의 근거다. 연속 사람 관람·자연 밀집 장면에서의 감지율/접근성/번쩍임 정량 수용은 NOT_RUN이다. 기존50마리 B2 가림 및 화면이 원본보다 밝게 보이는 색공간 피드백은 여전히 범위 밖으로 해소됐다고 주장하지 않는다. 화면 RGB 허용차5와 현재 최대1은 원본/임포트 해시·좌표·frame 검증의 완화가 아니다.

## 남은 게이트

root의 같은 최종 코드 검토·GC·commit/push·결과SHA CI가 남는다. native active 입력·자연 수동 전투/가독성·모바일/IL2CPP·기기·성능/전체 메모리·접근성은 NOT_RUN이다. 원격 Design 실행은 여전히 독립 미확인이며 numpy→격자→게임PNG, Pillow보드, Design 검토 캔버스를 구분한다. 보호448/PC6 및 저장 roundtrip은 구현자의 보존 자료를 읽은 범위이며 QA가 실저장을 열거나 복원하지 않았다.

모델 usage는 본인 provider CODEX_THREAD_ID로 `final-ui-review` start/end 구간만 기록했다. 초기 파일 읽기와 최종 문서 작성 일부는 그 구간 밖이며 실제 청구액/절감률은 계산하지 않았다.
