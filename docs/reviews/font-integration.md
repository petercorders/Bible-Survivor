# 폰트 sidecar 저장소 통합 독립 검토

2026-11-14 KST. **PASS — 통합 범위에서 새 HIGH/MEDIUM 없음.** 원본 폰트 QA와 Unity 실행은 별도 게이트다. 이 리뷰는 래퍼·재생성 비교·오프라인 범위·배치·고지 보존만 검토했다. 원본/도구/Core/Unity 파일은 변경하지 않았고 이 보고서만 작성했다.

## 읽기 검토

- `art/art-unity-font-01/`의 파일20개를 제작 원본의 대응 파일과 직접 바이트 비교하여 **20/20 동일**을 확인했다. 캐시/results가 없고, 패키지와 선별 QA/GC 텍스트에서 실제 개인 절대 경로나 비공개 artifact 식별자를 발견하지 않았다.
- TTF4개와 저작권/SIL OFL1.1 전문이 원본 그대로 남아 있다. contract/manifest는 공식 배포판 URL·archive SHA·멤버·파일 SHA·라이선스를 보존한다. 이번 리뷰가 공식 ZIP을 새로 다운로드/대조한 것은 아니다. 공식 ZIP5파일 동일성은 별도 `font-01-review.md`의 실제 QA 근거다.
- `tools/check_fonts.py`는 sidecar를 임시 폴더에 복사하고 verify_all의 실제 실패를 wrapper exit1로 전파한다. verify_all은 원본 폰트를 보존한 상태에서 manifest와 text metrics를 임시 폴더에 재생성해 저장된 JSON과 바이트 비교한다. 비교 실패는 이후 검사가 통과해도 최종 실패로 남는다.
- 기본 게이트는 네트워크 접근 없이 표준 라이브러리 경로를 실행한다. archive 미제공은 summary의 `NOT_RUN: no archive supplied`와 해당 시험의 SKIP으로 구분한다. `--zip`은 기존 archive를 전달하며 `--full`은 변이/Pillow 검사를 요청한다. 기본 실행을 공식 archive 비교·전체26오류·8변이·Pillow 실행으로 합산하지 않는다.
- sibling Bitmap 경로는 래퍼가 저장소의 art03 폴더를 명시적으로 전달한다. `.github/workflows/art.yml`은 기본 폰트 명령을 별도 단계로 실행하고, 로컬 소스 훅은 실패 코드를 전파한다. 실제 원격 job/설치된 훅 실행은 coordinator의 별도 결과다.
- `art/README.md`는 현재 독립 QA 완료 상태와 새 검사 진입점을 설명한다. sidecar 원본 README/RESULT의 “QA 전”은 복사 당시 제작자의 역사적 상태이고 현재 QA 보고서와 구분해서 읽어야 한다(비차단 문서 관찰).

## 실제 격리 실행

새 임시 복사본에 sidecar, 기존 Bitmap 폰트와 래퍼만 복사했다. SDK/패키지 설치, 네트워크, Editor/GUI 실행은 하지 않았다.

| 입력/명령 | 실제 결과 |
|---|---|
| `python3 tools/check_fonts.py`, 변경 없는 기본 입력 | wrapper0 / VERIFY_ALL PASS; manifest·metrics 재생성 동일 |
| 기본 오류 시험 세부 결과 | 양성1 통과, 오류24건 정상 거절; `zip_member_differs`, `build_refuses_overwrite` 2건 SKIP |
| manifest의 `roles.title.measured.glyphs`만 1로 변조 후 standalone checker 실행 | exit0 / PASS — 단독 검사의 설명 필드 검증 경계 재현 |
| 같은 변조 상태로 통합 wrapper 실행 | manifest 재생성 결과 DIFFERENT → verify_all1 → wrapper1 / FAIL |

따라서 단독 검사에서 놓치는 이 실측 설명 필드 변조는 실제 저장소 진입점에서 차단된다. 이는 이번에 넣은 `measured.glyphs` 입력의 증거이며, 이전 QA의 `measured.lineHeight` 입력을 재실행했다고 주장하지 않는다. 두 임시 실행 후 변조 파일은 복원했다.

근거: `<temporary>/bible-font-review-d24bp6k3/`의 baseline/changed-measured-standalone/changed-measured-wrapper 로그와 `evidence/font-integration-20261113T165308296079Z`, `font-integration-20261113T165333034285Z`의 summary 및 verify_all/negative 결과. 실제 임시 경로는 coordinator에게 인계했다.

## 미실행 범위

이번 리뷰의 ZIP 대조·8변이·Pillow/full 반복·실제 Ubuntu CI는 **NOT_RUN**이다. Unity import/IMGUI/TMP/렌더·fallback·기기·성능도 **NOT_RUN**이며 현재 진행 중인 Unity 담당자의 증거와 분리한다. 기존 독립 폰트 QA의 26negative/8mutants/독립15오류 결과를 이번 실행 횟수에 합산하지 않는다.
