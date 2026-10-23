# 몬스터 종류별 보상

## Unity에서 수정

- 기존 근접 적 공통 설정: `Assets/Resources/Monsters/Chaser.asset`.
- 기존 원거리 적 공통 설정: `Assets/Resources/Monsters/Spitter.asset`.
- 새 종류: Project 우클릭 → Create → Everything Grows → Monster Type.
- 배치된 몬스터의 `EnemyPlacement.Monster Type`에 종류 에셋을 연결한다. 같은 에셋을 사용하는 배치는 같은 보상 규칙을 공유한다. 배치별 보상 수치 덮어쓰기는 없다.
- Monster Type을 비우면 기존 Ranged 값에 따라 Chaser/Spitter를 자동 사용한다. 기존 씬/프리팹을 다시 저장하거나 재배치할 필요가 없다.
- Ranged는 공격 방식이며 Monster Type은 보상 종류다. 새 종류 에셋을 만든다고 새로운 모델/AI/능력치가 자동 추가되지는 않는다.

## 설정 항목

| 항목 | 의미 |
| --- | --- |
| Label | 종류 이름 |
| Gold | 처치 즉시 보유 골드에 더할 수량. 0이면 지급 없음 |
| Drops | 드롭 목록. 빈 목록이면 아이템 없음 |
| Item | 떨어뜨릴 아이템 |
| Probability | 0은 절대 안 나옴, 0.5는 50%, 1은 확정 |
| Minimum / Maximum | 성공한 행에서 나올 최소/최대 수량. 양끝 포함, 최소 1 |

- **각 행은 독립적으로 추첨**한다. 여러 행이 동시에 나올 수도 있고 모두 실패할 수도 있다. 같은 아이템을 여러 행에 넣으면 각각 추첨한다.
- 드롭은 기존처럼 몬스터가 죽은 위치에 떨어지고, 줍기는 기존 인벤토리 규칙을 사용한다. 자동 획득으로 바꾸지 않는다.
- Gold=0이고 Drops가 비어 있으면 보상 없는 종류다.
- 기존 기본값은 골드 0, Chaser는 검 1개 확정, Spitter는 활 1개 확정이다.
- 훈련 표적은 어떤 종류 설정에도 골드/아이템/처치 의뢰 보상을 주지 않는다.
- 골드는 기존 정수 범위의 최댓값에서 멈추며 오버플로하지 않는다.

## 저장·확장 위치

- `MonsterType`: 공유 설정. `MonsterRewardSystem.Grant`: 확률/수량/지급 실행.
- `EnemyPlacement.monsterType` → `EnemyCombatSystem.Spawn`에서 종류를 기억한다.
- 기존 `OpenFieldCombat.EnemyDefeated` 한 경로에서 처치 수·길드 진행·보상을 처리하고, 층 클리어 시 보상까지 기존 자동 저장에 포함한다.
- 사망한 적을 다시 때려도 재지급하지 않으며, 저장에서 처치 상태를 복원하는 과정은 보상 콜백을 실행하지 않는다. 보상 결과는 기존 골드와 바닥 아이템으로 저장한다. 저장 버전/아이템 ID/적 배치 ID 변경 없음.
- `SetupDataValidator.CheckMonsterType`이 음수 골드, 누락 아이템, 잘못된 확률/수량과 저장 미등록 아이템을 확인한다. Resources의 종류와 씬에서 참조한 종류 모두 검사한다.
- 테스트는 `Editor/MonsterRewardsCheck.Run`을 격리 Unity 프로젝트와 테스트 저장으로 실행한다. 기본 드롭·공유 종류·확률 0/1·수량·골드·빈 보상·훈련 제외·중복 타격·저장 복원·잘못된 설정을 확인한다.
