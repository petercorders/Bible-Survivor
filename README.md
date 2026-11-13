# Bible Survivor

성경의 인물·사건·상징을 소재로 한 Unity 기반 탑다운 2D 픽셀 서바이벌 게임 프로젝트입니다. 플레이어는 자신의 캐릭터를 만들고 성경 이야기에서 착안한 적과 싸우며 무기와 영구 스킬을 성장시킵니다.

## 현재 상태

아담 1라운드의 Unity 독립 C# 코어와 자동 검증을 개발하고 있습니다. 아직 Unity 프로젝트와 플레이 가능한 게임은 포함되어 있지 않습니다. 문서의 전투 수치와 경제 수치는 초기 설계이며 플레이 검증 결과가 아닙니다. 전체 스킬 420종은 목표 규모이며, 무기 전용 300종의 효과 개요를 작성했습니다.

[R01 구현·검증 상태](docs/progress/R01.md) · [개발 계획](docs/genesis-development-plan.md) · [모바일 고려 사항](docs/mobile-platform-plan.md)

## 코어 검사 실행

.NET SDK 10.0.404와 Python 3를 준비한 환경에서:

```sh
python3 tools/check_all.py
python3 tools/check_mutations.py
bash tools/dotnet.sh run --project tools/BibleSurvivor.Sim -- bible
```

시뮬레이션은 처치를 주입해 웨이브·성장·골드·저장 규칙을 확인하는 도구입니다. 실제 전투나 Unity 실행을 대신하지 않습니다. [코어 하네스 안내](docs/harness/standalone-core.md)

## 기획 문서

- [전체 게임 기획](docs/game-design.md)
- [창세기 라운드·전투 설계](docs/genesis-balance.md)
- [무기·보조 아이템·진화](docs/item-recommendations.md)
- [영구 스킬트리](docs/skill-tree.md)
- [무기별 스킬 300종](docs/weapon-skill-trees.md)
- [골드·구매·환불](docs/gold-shop.md)

## 개발 순서

첫 목표는 아담 라운드의 3분 전투입니다. 회전 성경·삼손의 턱뼈·다윗의 물맷돌을 시작으로 1~3라운드와 노아 보스를 구현하고, 검증 후 창세기 9라운드 및 후속 책으로 확장합니다.

기획·아트·개발·QA·독립 리뷰를 연결하는 로컬 전용 하네스를 사용합니다. 이 저장소에는 실행 가능한 코어 검사·콘텐츠 검사·회귀 변이 검사와 CI 정의를 포함합니다. Unity 검증은 설치 후 별도로 연결합니다.

게임의 적·보스·무기 효과에는 창작이 포함됩니다. 성경 본문의 내용과 게임적 각색은 기획 문서에서 구분합니다. 그래픽과 UI는 이 프로젝트의 독자적인 에셋으로 제작합니다.
