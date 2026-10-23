# 성장 유형 설정

Project에서 아이템 `PlantDefinition` 에셋을 선택하고 Inspector의 **Planted growth / 성장 설정**을 수정한다. **Growth type / 성장 유형**은 동시에 하나만 선택한다.

| 유형 | 설정 | 성숙 후 우클릭 |
| --- | --- | --- |
| 수확형 / Harvest | Harvest item, Harvest quantity | 지정한 수량을 받고 식재물이 사라짐 |
| 생산형 / Producer | Produced item, Quantity per harvest, Production interval | 생산품을 받고 식재물은 유지, 다음 생산을 기다림 |
| 기능형 / Functional | Grown function, 기능별 범위/간격/보관량 | 기능을 종료하고 회수 아이템 한 개를 받음 |

- 결과 아이템을 비우면 자기 자신이 결과물이다. 씨앗은 기존처럼 작물 결과를 연결해야 한다.
- Growth seconds, Required farming level, Harvest experience는 공통 설정이다. 경험치는 **한 번 수확할 때 한 번** 지급하며, 수확 개수에 곱하지 않는다.
- 손에 들었을 때의 Item use와 성장 후 유형/기능은 별개다. 예를 들어 무기에 물주기 기능을 설정해도 손에 들고 공격하는 동작은 유지된다.
- 성장 중 회수는 불가능하다. 괭이 제거는 반환과 경험치 없이 식재물을 없앤다.
- 인벤토리가 가득 차면 수확 수량 중 들어가지 못한 물건은 기존 드롭 경로로 바닥에 남긴다.

## 생산형

- **Regrowth seconds = 0**: 최초 성장 완료 이후 Production interval만큼 지나면 수확 가능. 수확하지 않은 생산품은 한 묶음만 대기하며 무한히 누적하지 않는다. 수확하면 생산 타이머가 다시 시작된다.
- **Regrowth seconds > 0**: 토마토처럼 최초 성숙 시 바로 수확하고, 이후 다시 물을 주면 해당 시간만큼 재성장한다. 이 경우 Production interval은 사용하지 않는다.
- 생산품을 받는 행동과 생산체 자체를 회수하는 기존 `Uproot` API는 구분된다. 일반 월드 우클릭은 생산품 수확이며, 회수 API는 성숙한 대상의 기존 회수 결과 한 개를 반환한다.

## 기능형

- Till: 인접한 상하좌우 네 칸 경작. 기존 경작 규칙을 사용하므로 다른 식재물을 제거하지 않는다.
- Water: Water range 안의 경작지에 물을 준다.
- Storage: 성숙한 동안 인벤토리 보관 칸 증가. 기존과 같이 **Recovered item**의 Storage bonus를 사용하며, 비우면 자신의 Storage bonus를 사용한다. 회수/제거 시 넘치는 물건은 보존해서 떨어뜨린다. 다른 맵에서도 효과를 유지한다.
- Till/Water의 Action interval이 0이면 매 틱 동작, 양수면 해당 초 간격으로 동작한다. 간격은 성숙 시점부터 계산한다. Storage는 상시 효과라 간격을 사용하지 않는다.

## 기존 데이터와 저장

- 기존 GUID/저장 ID/씬 배치를 유지한다. 모든 현재 아이템 에셋에 유형을 명시적으로 기록했다.
- 괭이/물뿌리개/구름/인벤토리 칸은 기존 기능을 유지한다. 기존 기능 간격은 0, 기존 수량은 1이다.
- 돌은 생산형, 토마토 씨앗은 물주기 재성장 생산형이며 다른 씨앗/무기/집은 수확형이다. 변형 완료 물건의 추가 변형 제한과 집 예외도 유지한다.
- `GrowthEffect`는 직렬화 호환을 위해 유지하며, 명시된 새 유형의 성장 행동을 고르는 데 사용하지 않는다. 이전 도구/테스트가 생성한 `Legacy` 데이터만 기존 Effect로 해석한다. Inspector에서 열면 현재 동작에 맞는 명시적 설정으로 변환한다.
- 생산과 기능의 타이머는 기존 `PlotSave.production`으로 저장하며 새 저장 버전을 만들지 않는다. 재성장 상태/물 상태/성장 진행/완료 여부도 기존 필드를 유지한다.
- 성숙한 모습은 기존과 같이 결과 아이템 그림을 사용한다. 별도 성장 단계 그림이나 특수 효과는 이번 작업에 포함하지 않는다.

## 코드와 검증

- 설정/기존 데이터 해석: `PlantDefinition`, 조건부 Inspector: `Editor/PlantDefinitionInspector`.
- 성장/생산/기능/수확 실행: `FarmingSystem`, 수확 가능 여부: `PlantPlot`.
- 여러 개 수확의 인벤토리/드롭 연결: `FarmGame.ReceiveHarvest`. 다른 맵의 보관 효과: `GameSession.RemoteStorageBonus`.
- 설정 오류: 기존 `SetupDataValidator.CheckItem`을 사용한다.
- 격리 프로젝트/저장 전용 `GrowthCategoriesCheck.Run`은 기존 `FarmingFeedbackCheck.Run`과 수량/드롭/XP/반복 생산/타이머 복원/독립된 사용·성장 기능/회수/잘못된 설정을 검사한다.
