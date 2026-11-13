# Unity 통합 후 디자인 인계

기준: 검수03 반복02 + font01, Unity6000.6.9f1 아담 macOS 시제품 완료 커밋4891099. 다음 Claude Design 담당이 새 UI/아트를 만들 때 원본 계약과 함께 읽는다. 이전 mailbox 종료로 전송되지 못한 최종 피드백을 파일로 보존한다.

- 원본79PNG/197프레임과 outline4역할을 실제 임포트했다. PPU32/Point/rect/pivot/layerOrder/부모flipX/9slice·GUID 재임포트는 독립 대조 및 엔진 시험 근거가 있다. 검수 원본은 변경하지 않았다.
- title/label24·body20·small16, GUI.matrix1에서660/1100 카드가 읽힌다. C#의 실제71카드를 font01 데이터와 대조하고 역할별 폭·행 상자를 엔진에서 검사했다. Python/Pillow 실측을 Unity 결과로 대신하지 않는다. 기존 패널 투과·HUD 조각·가려진 재개 버튼은 수정됐다.
- 빈 fontNames는 임포터가 자체 가족명으로 정규화한다. 외부 fontReferences 없음·명시적 원본 Font 사용·FontEngine 자체 face·샘플 binary atlas로 확인하며 빈 배열만 요구하지 않는다. 전체 한글 coverage와 전체 한글 픽셀 렌더는 다르다. Bitmap TTF는 동적 폰트로 사용하지 않았다.
- 연출 자료와 자연 전투를 구분한다. 공개 README 화면은 gallery 위치를 연출한 시각 검토 장면이다. 작은 PC 창 최소660은 모바일 화면 방향/지원기기 확정값이 아니다. 물리 터치·기기·성능·흑백 접근성 실측은 남아 있다.
- 보조7종 아이콘과 미사용 시안 기능은 이번 통합 범위가 아니다. 이후 필요한 디자인은 개발자가 stableID/크기/상태·실제 화면 재현으로 요청한다. 검수된03/font01을 조용히 덮어쓰지 않고 새 패치·출처/해시·검토 범위로 인계한다.

실제 근거는 `docs/progress/unity-art.md`와 `docs/art/reviews/unity-art-01-review.md`, `docs/reviews/unity-art-ui.md`에 있다. 상세 PNG/실행 로그는 ignored evidence에 보존했다. 이 문서는 새 Design 제품 실행이나 새로운 그림 제작 결과가 아니다.
