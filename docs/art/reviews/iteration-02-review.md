# 03 독립 재검토 — iteration 02

2026-11-14 · 독립 QA Codex · 검토 범위 A03-01/A03-02 수정·동일 회귀 입력·소비자 호환.

**PASS — 현재 스냅샷의 오프라인 production-draft 에셋 인계 준비 완료.** A03-01(HIGH), A03-02(MEDIUM)는 해결됐으며 이번 범위에서 추가 HIGH/MEDIUM은 발견하지 않았다. Unity 임포트·동작·글꼴·실기기 지원은 **NOT_RUN**이다. Bitmap 글꼴의 빈 glyf와 후속 outline 글꼴 패치 계획은 공유된 후속 엔진 과제로 남기며 이번 PASS를 Unity 지원 PASS로 전용하지 않는다.

## 스냅샷·범위

- `art-revision-03-iteration02-result.md`, 갱신된 `art-production-readiness.md`, 이전 독립 리뷰/GC, 실제 `check_art03.py`·contract·manifest·UI 데이터·build/verify/tests 변경을 읽었다.
- manifest SHA-256 `0c7f6ff0b86d83217beedf62f298294cd6c01c036ab589e2abed383c1e429751`.
- checker SHA-256 `3654344e3a9e5c2dd69d320935cc39f0dd3f359db769e5261bc7fa102c4ccebb`; contract SHA-256 `80444372c948b1bb60cb03422c3eeab169e134f7acc50c21c4e317aac51c8b98`.
- Python 3.11.6 / NumPy 2.4.3 / Pillow 12.1.1. 원본 01/02/03은 읽기 전용. 전체 검사는 `__pycache__`·canvas를 제외한 **새 임시 전체 사본**에서 실행했고, 별도 독립 변이도 각각 새 사본에 넣었다.
- 임시 QA 루트 `<qa>` = `<local temporary evidence>`. `<qa03>` = 그 안의 `art-concept-03`; 독립 입력은 `<qa>/independent/<case>`이다. 영구 결과는 본 문서와 GC 문서이며 임시 로그를 납품 원본으로 취급하지 않는다.
- 이전 QA가 보존한 반복01 사본과 **실제 export 해시를 비교**: 달라진 파일은 `manifest_v3.json`과 `ui/ui-pieces_v3.json`뿐. PNG79개·pixel-source·tiles는 그대로이며, manifest `assets` 배열도 완전히 같다(197프레임·각 에셋의 fps/loop/rect/pivot/hash 포함). 따라서 이전 직접 화면·움직임 QA의 범위를 재사용했다. 동일 화면을 전부 다시 보거나 이미지 편집을 하지 않았다.

## 결함 재판정

| ID | 판정 | 독립 재현·코드 확인 | 수용 조건 충족 |
| --- | --- | --- | --- |
| A03-01 HIGH | **PASS / 해결** | `check_top`이 ppu/pivotSpace/frameLayout/layerOrder/flip의 필수·타입·계약값을 확인. 개별 플레이어 에셋 layerOrder는 정수·계약 순번 비교. 이전 잘못된 값4종+전체 삭제, 단독 삭제5종, ppu 문자열/bool, 옛 flip 문자열/반대 방향, 개별 layerOrder 오류를 직접 주입해 모두 exit1/명시 오류 확인. | 기존 false PASS를 같은 입력에서 0→1로 바꾸었고 정상 사본0 유지. |
| A03-02 MEDIUM | **PASS / 해결** | font role4개, 경로·크기·해시, 실제 파일, 라이선스·출처 및 manifest 고지 검사 확인. 이전 없는 폰트 ref(데이터 SHA 정합), role 삭제/교환, 실제 폰트 삭제/바이트 변경, 고지 삭제, 폴더 전체 누락, unsafe 경로·출처·manifest 고지 오류를 각각 직접 주입해 exit1/FONT 확인. | 폰트를 포함한 정상 baseline0. 폰트 없는 사본1이며 `--src-root`를 정상 원본으로 지정해도 root의 누락을1로 거절. |

A02-06의 잔여 검사 거짓 통과는 위 두 결함 해결로 닫는다. A02-01~05/07~11은 iteration01의 오프라인 인계·정지/동작 검토 근거를 유지한다. 123+59 팔레트 방향은 승인 기준이며 색수 자체를 미결/BLOCKED로 두지 않는다.

## 실제 실행·종료코드

### 전체 회귀

`<qa03>`에서 다음 명령을 실제 실행했다.

```sh
python3 -I -B scripts/verify_all.py --catalog <repo>/content/genesis/catalog.json --review
```

**최종 exit0 / VERIFY_ALL: PASS.**

| 단계 | 실제 exit / 결과 |
| --- | --- |
| export build | 0 /79에셋·197프레임 |
| checker / checker+catalog | 각각0 /79/79, top·layers·source·tiles·ui·fonts |
| 음성 CLI 하네스 | 0 /정상1건 exit0, 오류41건 모두 명시 거절 exit1 |
| 검사기 변이 하네스 | 0 /5변이 모두 검출 |
| 재생성 | 0 /83/83 바이트 동일 |
| Pillow 차단 재생성 | 0 /83/83 바이트 동일 |
| legacy v1/v2 변환 | 각각0 /17/17,53/53 픽셀 동일 |
| 변환 파일을 manifest로 입력 | **1 /SCHEMA 거절(기대1)** |
| review build | 0 /23애니메이션·로컬 HTML 생성 |
| GIF/APNG 프레임·지연·반복 메타 | 0 /ANIM_META PASS |

재생성된 임시 export와 **원본 export도 별도 해시 비교**: 파일83개 대83개, 모든 해시 동일. 원본 manifest는 종료 시에도 위 스냅샷 해시와 같았다.

임시 `tests/results/checker_mutations_iter02.txt`를 직접 읽었다. top 검사 제거→13케이스 통과, 개별 layerOrder 검사 제거→1케이스 통과, 폰트 있음 검사 제거→5케이스 traceback, unsafe path 규칙 제거→1케이스 통과, fonts 검사 제거→12케이스 통과를 음성 시험이 모두 실패로 검출했다. traceback exit1을 정상 거절로 세지 않는 것도 확인했다. 이 5개는 제작자 테스트 코드를 QA가 실제 재실행한 결과이며 아래 독립 입력25건과 구분한다.

### QA가 직접 만든 26개 입력(정상1 + 오류25)

제작자의 입력 생성 함수를 사용하지 않았다. 케이스마다 `export/`, `src/`, **`third_party/`**를 복사했다. UI를 변조할 때에는 JSON을 다시 저장하고 manifest의 `data.ui.sha256`도 갱신하여 해시 오류로 우연히 거절되지 않도록 했다.

```sh
python3 -I -B <qa03>/scripts/check_art03.py --root <qa>/independent/<case> --quiet
```

모든 오류 케이스에서 **exit1**, stdout 첫 줄 `FAIL:`, 기대 오류 코드, stdout/stderr에 `Traceback` 없음까지 확인했다. 입력 생성/검사 결과 수집 하네스 자체는 exit0이다.

| case / 주입 내용 | 실제 exit | 코드 |
| --- | --- | --- |
| baseline(폰트·고지 포함) | **0** | PASS79 |
| ppu48 | 1 | TOP |
| pivotSpace=`TOP-LEFT, Y DOWN` | 1 | TOP |
| layerOrder 역순 | 1 | TOP |
| frameLayout=`vertical` | 1 | TOP |
| 최상위5필드 동시 삭제 | 1 | TOP5건 |
| title 폰트=`third_party/galmuri/__qa_missing__.ttf`(UI SHA 정합) | 1 | FONT |
| ppu / pivotSpace / layerOrder / frameLayout / flip **각각 하나만 삭제**(5건) | 각1 | TOP |
| ppu=`"32"` | 1 | TOP type |
| ppu=true | 1 | TOP type |
| flip을 반복01 문자열로 되돌림 | 1 | TOP type |
| flip.artFaces=`left` | 1 | TOP value |
| base_idle의 layerOrder=0(기대1) | 1 | LAYER |
| title role 삭제 | 1 | FONT |
| title↔label 객체 교환 | 1 | FONT |
| 실제 title 폰트 삭제 | 1 | FONT |
| LICENSE.txt 삭제 | 1 | FONT |
| third_party 폴더 전체 누락 | 1 | FONT5건 |
| 실제 폰트 파일을 잘못된 바이트로 교체 | 1 | FONT sha |
| `third_party/galmuri/../galmuri/<실제 폰트>` 경로 | 1 | FONT |
| fonts.source.version을 잘못 변경 | 1 | FONT |
| manifest license.fonts.notice를 없는 파일로 변경 | 1 | FONT |

별도 추가 확인: `font_dir_missing` 케이스에서 `--src-root <art-docs>/art-concept-03`을 지정해도 **exit1 /FONT5건**이었다. 계약/팔레트를 다른 곳에서 읽는 옵션이 패키지 root의 누락 폰트를 감추지 않는다. 정상 폰트4개와 LICENSE 실제 해시는 contract의 기대 해시5개와 모두 같았다. 공식 원격 zip을 QA가 새로 다운로드해 대조했다는 뜻은 아니다.

### 회귀의 옛 구조도 직접 재현

이전 독립 QA 임시 폴더에 보존된 **반복01 checker 원본 전체**를, 이번 독립 입력 중 ppu/pivotSpace/layerOrder/frameLayout/metadata_missing/font_reference 여섯 케이스에 실행했다. 각각 **옛 checker exit0**, **현재 checker exit1**이었다. 비슷한 다른 변이로 대신한 결과가 아니다.

임시 실행 기록: `<qa>/independent-results.json`(26건), `old-checker-same-inputs.json`(6건), `<qa03>/tests/results/verify_all.txt`, `check_negative_results.json`(정상1+오류41), `checker_mutations_iter02.txt`. 임시 폴더가 없어져도 위 표와 명령·입력 정의로 같은 검사를 다시 만들 수 있다.

## flip 형식 변경과 소비자 회귀

- manifest.flip은 이제 `{artFaces:right, faceLeft:flipX, applyOn:parent, note:…}` 객체다. 기존 단순 문자열은 TOP 오류로 명시 거절된다. 이 스냅샷의 **v3 반복02 형식**으로 인계해야 하며 향후 임포터가 이전 문자열을 가정하지 않도록 전달한다.
- 03 scripts 전체를 Python AST로 확인: flip 키를 직접 읽는 곳은 build의 `CONTRACT['flip']`과 checker의 `C.get('flip')`뿐이었다. `build_review03.py`의 로컬 flipX는 별도의 체크박스 bool로 canvas transform을 바꾸며 manifest.flip 문자열을 파싱하지 않는다. review build 실제 exit0이 이 소비 경로 회귀를 확인한다.
- Bible-Survivor의 검색 대상 C#/Python/JSON 소스에서 `manifest_v3`·`ui-pieces_v3`·`bible-survivors.art-manifest` 참조는 발견하지 않았다(rg exit1=검색 결과 없음). 기존 Unity 임포터와 연결돼 호환 검증됐다는 주장으로 쓰지 않는다. 향후 새 임포터는 구조 객체·폰트 license 객체를 읽어야 한다.
- 에셋별 manifest 배열은 반복01과 같고, PNG/source/tiles도 바뀌지 않았으며 GIF/APNG 메타 재검사가0이다. 픽셀·프레임 수·시간표 변경을 동반한 수정이 아니므로 이전 전체 시각 QA를 반복하지 않았다.

## 인계 한계

- PASS의 범위는 현재 아트·원본·변환·검사·로컬 검토 자료를 **오프라인으로 개발자에게 인계할 준비**다. final 게임 에셋/엔진 지원/Unity 글꼴 렌더/Android/iOS 실기기는 NOT_RUN이다.
- 현재 Bitmap TTF의 빈 glyf와 동적 글꼴/TMP 지원 문제는 후속 outline 폰트 패치 계획으로 분리돼 있다. 그 패치가 들어오면 바뀐 계약·SHA·role·폰트 렌더/실제 엔진 검증을 별도 수행해야 하며 이 승인 해시를 그대로 재사용하지 않는다.
- 실제 Claude Design 제품 실행·라이브 캔버스 렌더는 이번 검사에서 수행하지 않았다. 이전 캔버스 파일의 존재와 scripts가 생성한 PNG/격자, Pillow로 합성한 review/UI 시안을 서로 구분한다. `art-ui-draft-01` Pillow 합성을 Design 생성으로 기록하지 않는다.
- canvas·비공개 blob·캐시·임시 실행 로그를 공개 통합물로 넘기지 않는다. 검토 근거 선별·저장소 통합·CI/pre-commit 연결·커밋·push는 coordinator 소유이며 QA는 실행하지 않았다.
