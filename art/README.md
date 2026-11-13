# 검수된 오프라인 아트

`art-concept-03/`은 32px 기준 production-draft 인계 패키지입니다. 독립 QA 반복02에서 오프라인 인계 준비를 통과했습니다. PNG79개/197프레임, 원본·명시 계약·manifest·타일·UI 조각·글꼴 출처와 라이선스를 포함합니다. 그림은 격자/절차 생성 및 스크립트 합성으로 제작했고, Design 캔버스 게시와 제품 생성은 구분합니다. 비공개 캔버스와 blob 자료는 포함하지 않습니다.

- 검사: 저장소 루트에서 `python3 tools/check_art.py` (표준 라이브러리만 필요).
- 전체 재생성/검토 미디어 재빌드: 패키지의 `requirements.txt` 환경에서 `python3 tools/check_art.py --full`. 내보낸 파일을 먼저 재생성 비교한 뒤 전체 루프를 임시 사본에서 실행합니다. 원본 파일은 덮어쓰지 않습니다.
- 로컬 움직임 검토: `art-concept-03/review/index.html`.
- [`docs/art/`](../docs/art/)의 인계 계약·제작 결과·독립 QA/GC 기록을 함께 읽습니다. 원래 문서의 `art-concept-03/` 경로는 이 폴더 아래이며, 원래 `art-readiness-reviews/`는 `docs/art/reviews/`로 옮겼습니다.

`art-concept-01/02`는 이전 형식 변환 및 02와의 비교에 필요한 선별된 원본/PNG/레이어/장면 fixture입니다. 전체 이전 제작 폴더의 복사본이 아닙니다. 03의 변환/음성 시험/리뷰 재빌드가 이를 참조합니다.

Unity 임포트·인게임 동작·실기기·성능은 별도 검증 대상입니다. manifest의 `flip`은 반복02 구조 객체를 사용합니다. 현재 Galmuri Bitmap TTF는 Unity 폰트 지원을 검증한 파일이 아니며, 외곽선판 사용은 별도 계약/출처/임포트 검증을 거쳐야 합니다. 검토용 보상·꾸미기 시안은 게임 기능 구현을 뜻하지 않습니다.
