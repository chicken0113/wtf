# 코드 수정 안내

편집 도구 메뉴와 저장 방식: [EDITOR_TOOLS.md](EDITOR_TOOLS.md).

## 먼저 볼 곳

요청을 받으면 동작·입력·예외·변경 범위를 사용자에게 설명하고 확인을 받은 뒤 구현한다. 아래 표에서 담당 파일과 진입 함수부터 찾는다. 기존 구현이 있으면 해당 경로를 수정하고 다른 곳에 같은 규칙을 만들지 않는다.

| 수정할 기능 | 규칙·상태 담당 | 연결·표시 담당 |
| --- | --- | --- |
| 아이템/NPC/길드/맵 연결 관리 창 | `Editor/ItemManagerWindow`, `NpcManagerWindow`, `GuildQuestWindow`, `MapConnectionsWindow` | 기존 Inspector/직렬화 데이터/검사 재사용. `ManagementEditorUI`는 씬 선택·명시적 저장 UI 공유 |
| 아이템 사용/특수 효과/성공 시 소모 | `FarmGame.ItemUse`, `ItemUseEffect` | `FarmGame.Input` 단일 호출; 기존 무기/도구 API에 위임 |
| 시작/일시정지/설정/새 게임/이어하기 | `GameMenu`, `GameSession.Menu` | `Editor/MenuFoundationSetup`, `MainMenu.unity`; [FOUNDATION_EXPANSION.md](FOUNDATION_EXPANSION.md) |
| 층 메타데이터/템플릿 추가 | `DungeonFloor`, `Editor/TowerFloorWindow` | 기존 `MapExit` 잠금/저장/맵 배치 도구 재사용 |
| 시작 설정 누락·잘못된 참조·수치 | `SetupDataValidator.cs`, `FarmSetupValidator.cs` | `FarmGame.Startup.cs`, `Editor/ProjectSetupCheck.cs`; 자세한 사용법은 [SETUP_VALIDATION.md](SETUP_VALIDATION.md) |
| 아이템 수량·고정 슬롯·이동/교환·선택·슬롯 제한 | `Assets/Scripts/InventoryState.cs` | `FarmGame.Inventory.cs`가 메시지·바닥 드롭 연결 |
| 밭 갈기·심기·물주기·성장·수확·괭이 제거·재수확 | `Assets/Scripts/FarmingSystem.cs` | `FarmGame`의 기존 공개 함수가 위임 |
| 농사 가능 판정·칸 중앙 아이콘 | `FarmingSystem.PlantHint / InteractionHint / ToolHint`, `FarmActionHint` | `FarmGame.FarmingHints.UpdateFarmingIcon`이 도구 우선·맨손/거리/UI 숨김·칸 중앙 표시, `FarmArt.PlantingIcon`은 공통 심기 그림 |
| 농사 레벨·경험치·심기 해금 | `FarmingProgression.cs`, `FarmingLevelSettings.cs` | `FarmGame.Progression` / `FarmingSystem.PlantHint` / `FarmPersistence`; 설정·저장: [FARMING_LEVELS.md](FARMING_LEVELS.md) |
| 성장 유형·수확 수량·생산·심은 기능 | `PlantDefinition.GrowthType / Function`, `FarmingSystem` | `Editor/PlantDefinitionInspector` 유형별 설정, `PlantPlot.CanHarvest`, `FarmGame.ReceiveHarvest`; [GROWTH_TYPES.md](GROWTH_TYPES.md) |
| 밭 한 칸의 상태 | `Assets/Scripts/PlantPlot.cs` | SpriteRenderer를 보관하지 않음 |
| 작물·흙의 그림·크기·그리는 순서 | `Assets/Scripts/FarmWorldView.cs` | 농사 규칙을 변경하지 않음 |
| 핫바·보관 칸·드래그·골드·지도 | `Assets/Scripts/FarmHud.cs` | `DrawSlot`이 핫바/인벤토리 표시와 슬롯 클릭을 공통 처리 |
| 키 읽기·프레임 처리 순서·메뉴 열기 | `Assets/Scripts/FarmGame.Input.cs` | `Update`, `ProcessMenuInput` |
| 우클릭 대상 선택·조준·UI 클릭 차단 | `Assets/Scripts/FarmGame.Controls.cs` | `ProcessSecondaryPointer` → `InteractWorld / FarmAction`, `UpdateAim` |
| 무기 반동·캐릭터 동작·카메라 흔들림 | `Assets/Scripts/FarmGame.Feedback.cs` | `AnimateFarmer`, `LateUpdate`, `WeaponFeedback` |
| 월드 아이템 줍기·드롭·집 배치 | `Assets/Scripts/FarmGame.World.cs` | `DetachAt`, `Spawn`, `CanPlaceHouse`, `PlaceHouse` |
| NPC 대화·구매·판매 | `Assets/Scripts/NpcManager.cs` | `ShopNpc`, `NpcProfile` 데이터 |
| 체력·피해·무적·사망 복귀·회피·공격 대기 | `Assets/Scripts/PlayerCombatState.cs` | `OpenFieldCombat`이 기존 API와 직렬화 설정 유지 |
| 적 탐지·추적·공격 준비·피격·사망·훈련 표적 회복 | `Assets/Scripts/EnemyCombatSystem.cs` | `TickEnemy`, `MoveEnemy`, `Hit`, `HitMelee` |
| 몬스터 보상 관리 창·검색·추가/복제 | `Editor/MonsterRewardsWindow.cs` | 기존 `MonsterType` 에셋과 `SetupDataValidator.CheckMonsterType`를 직접 사용; `MonsterRewardsWindowCheck.Run` |
| 몬스터 종류별 드롭·골드 | `MonsterType`, `MonsterRewardSystem.Grant` | `EnemyPlacement.monsterType` → `EnemyCombatSystem` → 기존 `OpenFieldCombat.EnemyDefeated`; [MONSTER_REWARDS.md](MONSTER_REWARDS.md) |
| 탄환 생성·이동·벽 차단·최초 명중·수명 | `Assets/Scripts/ProjectileSystem.cs` | `Spawn`, `Tick`, `ClearHostile` |
| 전투 궤적·발사·파편·잔상·공격 예고선 | `Assets/Scripts/CombatEffects.cs` | 캐릭터/손 동작과 카메라는 기존 `FarmGame.Feedback.cs` |
| 전투 연결·처치 수·길드 처치 전달·체력 HUD | `Assets/Scripts/OpenFieldCombat.cs` | `Attack`, `TickCombat`, `EnemyDefeated`, `OnGUI` |
| 피격 사망·병원 귀환 | `PlayerCombatState.Damage` → `GameSession.Recovery.ReturnToHospital` | 기존 `GameSession.Travel / Bind / LoadMap` 자동 저장, `Editor/HospitalSetup` 병원/마을 우클릭 문; [HOSPITAL_RECOVERY.md](HOSPITAL_RECOVERY.md) |
| 층 클리어·처치 ID 유지 | `EnemyCombatSystem.CaptureState / RestoreState / FloorCleared` | `FarmPersistence` → `MapSave.combatCleared / defeatedEnemies`; 배치 ID는 `MapSaveAuthoring` → `MapObjectId` |
| 층 전멸 계단 잠금 | `EnemyCombatSystem.LivingHostileCount` → `OpenFieldCombat` | `MapExit.requireClearFloor / IsCombatBlocked / Travel / InteractionPrompt`; 일반 출입구 기본값 false |
| 탑 1·2층·계단 배치 | `Editor/TowerDungeonSetup.cs` | 기존 `MapExit`/`MapArrivalPoint`/`GameSession.Travel`; [TOWER_DUNGEON.md](TOWER_DUNGEON.md) |
| 맵/플레이어 저장 연결 | `Assets/Scripts/FarmPersistence.cs` | 각 상태 소유자에게 Capture/Restore 위임 |
| 파일 저장·백업·맵 이동·저장 오류 | `Assets/Scripts/GameSession.cs` | 의뢰는 `GameSession.Guild.cs` |
| 저장 파일 필드 | `Assets/Scripts/GameSaveData.cs` | 버전 1 유지 |
| 벽·통행·탄환 차단·길 찾기 | `Assets/Scripts/MapCollision.cs` | Scene의 `MapObstacle`, `FarmBuilding` |

`FarmGame.cs`는 Unity 컴포넌트 초기화, 기존 공개 API, 시스템 연결을 맡는다. 기존 컴포넌트와 직렬화 필드를 유지하므로 씬마다 새 컴포넌트를 붙일 필요가 없다.

시작 순서는 `FarmGame.Startup.Initialize → 공유 설정 검사 → InitializeRuntime → IsInitialized=true → GameSession.Bind`다. 필수 오류가 있으면 오브젝트 생성과 저장 연결을 시작하지 않는다. 사용 코드에서 준비 여부를 확인할 때는 `IsInitialized`를 사용한다. 데이터 누락 검사를 입력/전투/UI에 각각 중복 구현하지 않는다.

`FarmGame.*.cs`는 하나의 컴포넌트를 주제별로 나눈 연결 코드다. 이 파일 분할 자체를 독립된 시스템으로 간주하지 않는다. 실제 인벤토리 상태는 `InventoryState`, 밭 상태와 농사 규칙은 `FarmingSystem`, 그림은 `FarmWorldView`, HUD는 `FarmHud`가 소유한다.

## 주요 호출 흐름

### 심기·수확

`FarmGame.Input.Update → ProcessSecondaryPointer(우클릭) → FarmAction → FarmingSystem.InteractPlot → PlantSelected / Collect / Uproot`

- 좌클릭 괭이는 `ToolHint(Dig) → UseHoe`로 심어진 대상 제거를 우선한다. 자동 주변 경작은 기존 `Till`만 호출하여 주변 작물을 제거하지 않는다. 제거와 수확은 `ClearPlant`로 상태/그림/용량을 정리하되 제거에는 아이템 반환이 없다. `Uproot`는 성장 전/시든 대상 회수를 거부한다.
- 성장 전 회수는 없다. 우클릭은 UI → 가리킨 NPC/문 → 바닥 아이템 → 수확 → 심기 순서다. E/F 바인딩은 제거했고 인벤토리/상점 우클릭과 Space/Shift는 유지한다.
- 심기 가능 여부는 `FarmingSystem.PlantHint`가 판단하며 `PlantSelected`도 같은 결과를 사용한다. 도구는 `ToolHint`, 우클릭 작물 행동은 `InteractionHint`를 실행과 안내가 공유한다. 이 쿼리는 아이템이나 밭 상태를 바꾸지 않는다.
- `FarmGame.FarmingHints.FarmHint`는 농사 행동의 바닥 아이템 → 건물 → 밭 우선순위를 따른다. `PrimaryFarmHint`는 좌클릭 도구/집 설치 판정이다.
- 화면에서는 `UpdatePlantingPreview → UpdateFarmingIcon`이 한 개의 SpriteRenderer를 재사용한다. 먼저 괭이/물뿌리개/낫의 `ToolHint`가 가능하면 도구 그림을 사용한다. 도구 작업이 불가능하면 도구를 포함한 현재 아이템의 `PlantHint`를 확인하고, 심을 수 있을 때 공통 새싹 그림을 사용한다. 둘 다 불가능하면 숨긴다. 가능한 칸에서만 원래 색(심기는 초록) 아이콘과 초록색 한 칸 테두리를 함께 표시한다. 불가능하면 둘 다 숨긴다. 줍기·수확 가능 여부가 심기 아이콘의 색을 덮어쓰지 않는다.
- 아이콘은 마우스가 가리키는 정수 칸 중앙에 고정한다. 맨손·거리 밖·월드 밖·메뉴/UI·이동/오류·게임오버에서는 숨긴다. 모든 숨김 경로는 `HideFarmingPreview`로 아이콘과 테두리를 함께 끈다. 테두리 그림은 `FarmArt.FarmingBorderIcon`의 속이 빈 사각형을 재사용한다. 집 전체 설치 미리보기, 커서 안내 텍스트, 작물 위 이름/상태표/진행 막대는 제거했다. 실제 집 설치/심기와 농사 규칙은 유지한다.
- 밭으로 쓸 수 있는 위치와 건물 충돌은 `FarmGame.World.CanFarmAt`에서 연결한다.
- 수확물은 `ReceiveHarvest → InventoryState.TryAdd`로 들어간다. 공간이 없으면 연결 코드가 바닥에 드롭한다.
- 성장은 `FarmingSystem.Tick`, 모습은 `FarmWorldView.RefreshPlant`를 본다.
- 설치 미리보기는 `FarmGame.UpdatePlantingPreview`의 안내용 판정이다. 실제 아이템 소비·설치/심기는 각 실행 함수가 판단한다.

### 인벤토리와 상점

`FarmHud / NpcManager → FarmGame의 인벤토리 API → InventoryState`

슬롯 수·수량 규칙을 UI나 상점에 다시 만들지 않는다. 창 표시와 클릭 위치는 `FarmHud`, 구매 가격·거래 성공 여부는 `NpcManager`에 둔다. 용량 감소로 넘치는 물건의 월드 배출은 `FarmGame.Inventory.DropOverflow`가 담당한다.

### 저장·복원

`GameSession → FarmPersistence → InventoryState.Capture/Restore + FarmingSystem.CapturePlots/RestorePlots`

아이템은 `SaveItemCatalog`의 ID로 저장한다. 인벤토리와 밭 복원은 아이템 ID들을 먼저 확인한 뒤 해당 상태를 교체한다. 파일 저장 실패·백업 복구·이동 실패 처리는 기존 `GameSession`을 사용하고 별도의 저장 경로를 만들지 않는다. 전체 맵 복원이 하나의 트랜잭션인 구조는 아니다.

### 공격·피해·처치

`FarmGame.Input → OpenFieldCombat.Attack → PlayerCombatState.TryBeginAttack → EnemyCombatSystem.HitMelee / ProjectileSystem.Spawn`

- 공격 대기와 회피 중 공격 제한은 `PlayerCombatState` 한곳에서 판단한다. 피해·사거리·간격은 기존 무기 데이터를 읽는다.
- `ProjectileSystem.Tick`은 벽과 대상의 충돌 시점 중 가장 빠른 것을 선택한다. 적 명중은 `EnemyCombatSystem.Hit`, 플레이어 명중은 `PlayerCombatState.Damage`로 전달한다. 별도의 탄환 피해 규칙을 만들지 않는다.
- 근접 공격의 벽 차단·부채꼴 판정은 `EnemyCombatSystem.HitMelee`를 수정한다. 원거리와 근접의 적 피격 처리는 같은 `Hit`으로 모인다.
- 일반 적 처치는 `OpenFieldCombat.EnemyDefeated → 처치 수 증가 / GameSession.RecordGuildKill / MonsterRewardSystem.Grant` 한 경로로 처리한다. 훈련 표적은 이 콜백을 호출하지 않는다.
- `PlayerCombatState`와 `EnemyCombatSystem`은 기존 컴포넌트의 직렬화 수치를 참조한다. `startingHearts`, `healthPerHeart`, `enemyRadius` 필드 이름과 컴포넌트 GUID를 유지한다.
- 저장은 기존 `FarmPersistence → OpenFieldCombat.RestorePlayerState → PlayerCombatState.Restore`로 이어진다. 처치 수는 연결 컴포넌트에서 복원하며 저장 필드·버전은 바꾸지 않는다. 적 개체와 탄환의 저장은 이번에 추가하지 않았다.

### 전투 프레임 순서와 종료

`OpenFieldCombat.TickCombat → 플레이어 타이머/회피 → 적 행동/분리 → 탄환 이동/명중 → 임시 효과 수명`

적이 발사한 탄환은 같은 틱의 탄환 처리에 포함된다. 순서를 바꾸면 회피·공격·명중 시점이 달라질 수 있다. GameOver·메뉴 일시정지의 기존 차단 경로를 유지한다.

`SetNightActive`는 실제 활성 상태가 바뀔 때 적 상태를 전환하고, 비활성 전환 때 적 탄환만 제거한다. 훈련 표적의 활성·재등장은 일반 적과 다르므로 두 경로를 함께 검사한다. 현재 규칙을 보존한 것으로, 과거 논의했던 웨이브 잔류 규칙을 새로 구현한 작업은 아니다.

## 수치 변경 위치

| 수치 | Unity에서 볼 데이터 |
| --- | --- |
| 이동·회피·반동·흔들림·농기구 연속 간격 | `Assets/Resources/PlayerControls.asset` (`Tools > Everything Grows > 플레이어 조작 설정`) |
| 성장 시간·수확 결과·크기·무기 피해/사거리/간격 | 각 `PlantDefinition` 에셋 |
| 시작 아이템 | `Assets/Resources/StartingInventory.asset` |
| NPC 판매 물품·가격 | 해당 씬의 `NpcManager` 프로필 |
| 길드 의뢰 | `Assets/Resources/GuildQuests.asset` |
| 맵 배치·범위·통로 | 각 씬의 `FarmMapLayout`, `MapExit`, `MapArrivalPoint` |

## 변경 후 필요한 검증

`Assets/Scripts/Editor`의 기존 동작 검사를 재사용한다. 게임 규칙을 테스트 안에 다시 구현하지 않는다.

| 변경한 부분 | 검사 진입점 |
| --- | --- |
| 시작 설정·누락 처리·대체값 | `ProjectSetupCheck.Run`(정상 프로젝트), `SetupValidationCheck.Run`(격리된 오류 주입) |
| 인벤토리·농사·수확·집 | `FoundationRefactorCheck.Run` — 기존 슬롯/보관/성장/재수확/하트/집 검사 묶음 |
| 농사 판정/안내·작물 상태 | `FarmingFeedbackCheck.Run` — 잘못된 심기·도구·거리·생산/재수확·가득 찬 인벤토리, `RunVisual` — 도구 우선/공통 심기/칸 중앙 고정/맨손·거리·메뉴·불가능한 칸의 동시 숨김/초록 한 칸 테두리와 실제 렌더 |
| 병원 귀환·반 하트·사망 전 층 상태·재시작 | `HospitalRecoveryCheck.Prepare / Run / Resume` — 별도 프로젝트와 테스트 저장에서 실행 |
| 탑 1·2층·계단·재시작 | `TowerDungeonCheck.Run` / `Resume`, `WorldRouteAudit.Validate` — 테스트 저장과 별도 프로젝트에서 실행 |
| E/F·메뉴 차단·조준·회피 | `BasicControlsCheck.Run` |
| 전투 분리·하트·길 찾기·탄환 차단·재등장·피격 취소 | `CombatRefactorCheck.Run` — 기존 충돌/하트 검사와 전투 생명주기 검사 |
| 공격·반동·회피 모습·농기구 연속 사용 | `ActionFeelCheck.Run` |
| 실제 프레임의 인벤토리/상점 UI·초기 지급 | `OpenFieldPlayCheck.RunInventory` |
| 상점·맵 왕복·작물/소지품/집 저장 | `InteriorWorldCheck.Run`, 이후 같은 테스트 저장으로 `Resume` |
| 길드 보상·중복 지급·저장 실패 | `GuildQuestCheck.Run`, `Resume` |

이 검사는 프로젝트를 임시 폴더에 복사한 Unity batch 환경에서만 실행한다. Play 검사는 `WTF_SAVE_TEST_DIR`로 별도 저장 위치를 지정한다. 사용자가 연 씬과 실제 자동 저장으로 테스트하지 않는다. 예전 씬 생성 함수가 포함된 검사도 있으므로 검사 프로젝트의 씬/설정/에셋을 본 프로젝트로 통째로 복사하지 않는다.

검사가 적/탄환의 내부 상태나 임의 발사를 필요로 할 때는 `Editor/CombatCheckAccess.cs`를 사용한다. 내부 소유 클래스의 변경 지식은 이 도우미에 모으고, 게임 실행 코드에는 검사 전용 공개 API를 추가하지 않는다.

## 유지할 경계

- 새 패키지·공통 프레임워크·이벤트 버스 없이 현재 클래스 간 호출로 연결한다.
- 기존 `.meta`/GUID, 직렬화 필드 이름, 씬 배치, 저장 DTO와 버전을 유지한다. 변경이 필요하면 마이그레이션을 먼저 제안한다.
- 규칙을 바꾸는 작업과 구조 정리를 구분한다. 리팩터링 중 발견한 새 기획은 확인 없이 추가하지 않는다.
- 현재 농사는 `FarmGame`의 인벤토리·월드 API에 의존한다. 독립된 서버 시뮬레이션이나 멀티 구조까지 분리한 상태는 아니다.
- 전투도 별도 상태 소유 클래스로 분리했다. 상태 클래스는 현재 Unity 월드와 설정 컴포넌트를 참조하며 멀티/서버 전용 시뮬레이션으로 분리한 상태는 아니다. 초기화·HUD 등에는 추가 정리 여지가 있으며 프로젝트 전체의 기술 부채를 제거했다는 의미는 아니다.

### 일반 적의 영구 처치

일반 적은 사망 후 자동 재등장하지 않는다. 배치 적의 ID와 층 클리어 상태는 맵별 저장에 포함되며 재입장·탑 외부 이동·재실행에도 유지한다. 복원은 보상 콜백을 호출하지 않는다. 마지막 처치의 보상/드롭 이후 `OpenFieldCombat.EnemyDefeated`가 기존 자동 저장을 호출한다. 훈련 표적의 3초 회복은 유지한다. 버전 1의 이전 저장은 새 필드가 없으면 미클리어로 읽는다.

### 병원 귀환

피격으로 체력이 0이 되면 `PlayerCombatState.Damage`가 반 하트 체력과 짧은 무적을 설정하고 `GameSession.ReturnToHospital`을 호출한다. 기존 맵 이동이 출발 맵/플레이어를 저장하고 도착 검증 후 병원 위치를 다시 저장한다. 병원 경로/복귀 ID는 `GameSession.Recovery.cs`에서 관리한다. 맵의 `passiveHealthRecovery`는 기존 안전 구역 자동 회복을 제어하며 병원에서만 false다. 최종 하트 슬롯을 직접 수확했을 때의 GameOver는 유지한다. 병원 문/복귀 위치는 씬에서 직접 편집한다.

### 우클릭 입력과 메뉴

`FarmGame.Input.Update`는 우클릭을 `ProcessSecondaryPointer`에 한 번 전달한다. 메뉴 상태에서는 닫기/안쪽 UI에 먼저 전달하고, 월드에서는 하트/UI 차단 후 `InteractWorld → FarmAction`을 사용한다. 가리킨 NPC는 `NpcManager.PointedNpc`, 문은 `MapExit.PointedDoor`가 고른다. 닿거나 공격/수확할 수 있는지만 보고 근처 NPC를 자동 선택하지 않는다. 창의 그림과 바깥 클릭 판정은 `NpcManager.PanelRect / ContainsUI`를 공유한다. `SecondaryPointerConsumed`로 메뉴를 여닫는 클릭이 같은 프레임에 판매/슬롯 떼기로 재사용되는 것을 막는다. Esc 닫기는 기존 `ProcessMenuInput`을 재사용한다. 저장/씬 재작성은 없다. 검사는 `SecondaryControlsCheck.Run`과 기존 `BasicControlsCheck.Run`, `TowerDungeonCheck.Run`을 사용한다.

### 고정 슬롯과 드래그

- `InventoryState`는 슬롯 번호 → 스택을 보관한다. `Count`/`InventoryCount`는 점유된 칸 수이므로 마지막 슬롯 인덱스나 순회 상한으로 쓰면 안 된다. UI/입력은 `HotbarCapacity`와 `Capacity`로 범위를 정하고, 테스트의 정리/마지막 아이템 조회는 `Editor/InventoryCheckAccess`를 사용한다.
- `StartingInventory.hotbarSlots / storageSlots`는 시작할 때 읽는다. 기본 4+1=5칸이며, 성장한 보관 아이템의 기존 보너스는 추가 보관 칸이 된다. 숫자/휠/선택은 핫바만 대상으로 한다. 보관 칸 아이템은 핫바로 옮겨서 사용한다.
- `FarmHud.DrawInventoryWindow`는 보관 칸(스크롤)과 핫바(고정)를 함께 그린다. `DrawSlot`이 누른 원본을 기억하고 `UpdateDrag`가 놓은 칸을 확인해 `FarmGame.MoveInventoryItem → InventoryState.MoveOrSwap`으로 위임한다. 드래그하는 동안 아이템을 미리 제거하지 않는다. 같은 묶음은 합치고 Shift 드래그는 홀수의 큰 절반을 옮긴다. `MoveInventoryItem`의 수량을 `InventoryState.MoveOrSwap`이 검증하며 부분 묶음은 다른 물건과 교환하지 않는다. 창 밖 놓기는 `DropInventoryItems → RemoveAmount + 기존 Spawn`으로 선택한 수량만 발밑에 떨어뜨리고, 창 안 빈 공간/Esc/창 닫기/맵 이동은 취소한다. 거래 중 상점 패널 위도 취소 영역이다.
- 저장 버전은 1이다. 새 `PlayerSave.inventoryUsesSlots=true`일 때 각 `ItemStackSave.slot`을 사용하며 빈칸은 저장 목록에 넣지 않는다. 이전 형식은 목록 순서를 슬롯 번호로 읽는다. 중복/음수 슬롯은 거부하고 기존 인벤토리 교체 전에 참조/번호를 확인한다. 칸 수 감소로 범위 밖인 아이템은 `DropOverflow`가 모두 바닥에 보존한다.
- `InventoryLayoutCheck.Run`은 이동/교환/빈칸·새/구 저장·중복 거부·초과분 드롭·슬롯 떼기/복원을, `RunVisual`은 GUI 슬롯 드래그/취소와 두 영역 화면을 격리 프로젝트에서 검사한다.

- 아이템 툴팁은 `FarmHud.DrawTooltip`이 이름/수량/`PlantDefinition.UsageText`를 표시한다. Unity의 `description`이 있으면 그대로 쓰고 없으면 기존 용도에 맞는 기본 설명을 사용한다. 드래그 중에는 숨기며 Repaint에서만 그려 입력을 소비하지 않는다. `dragSourceQuantity`는 원본 검증, `dragQuantity`는 실제 이동/버리기 수량이다. 바닥 아이템은 기존대로 개별 단위이며 저장 형식은 추가 변경하지 않았다.

## 자유 거점·부활·사망 회수 (2026-10-27)

현재 사망 경로는 PlayerCombatState.Damage → GameSession.RespawnAfterDeath → FarmGame.LeaveDeathCache → Save → 지정 맵/좌표 이동이다. 기존 ReturnToHospital은 과거 검사/호환용으로 남지만 일반 전투 사망에서 호출하지 않는다. FarmGame.Recovery가 회수·임시 침대·맵 저장을 담당하고 GameSession.Respawn이 등록/기본 위치/검증/이동을 담당한다. 기존 입력의 InteractWorld는 가리킨 가방/침대 상호작용을 먼저 처리한다. 자세한 필드·설정은 FREE_CAMP_RECOVERY.md.

## 지역 개체군 (2026-10-27)

OpenFieldCombat.TickCombat → RegionalSpawnSystem.Tick → 기존 EnemyCombatSystem.Tick 순서로 지역 스폰/정리 후 개체 전투를 진행한다. 수면/개체군 저장은 EnemyCombatSystem.Regions, 지역별 조건은 SpawnRegion, 다른 씬의 낮 정리는 GameSession.Regions가 담당한다. 지역이 연결된 필드에서는 SafeBounds 제한과 옛 층 영구 클리어 규칙을 적용하지 않는다. 몬스터 관리창에서 종류의 출현 가능 지역을 연결하고 ‘지역 설정 열기’로 범위/간격/최대 수를 수정한다. REGIONAL_MONSTERS.md 참조.

## 필드 보스 (2026-10-27)

EnemyCombatSystem.Bosses가 보스 추적 범위·귀환·초기화·재등장을 담당하며 기존 TickEnemy의 공격/이동을 재사용한다. GameSession.Travel/RespawnAfterDeath는 출발 저장 전에 살아 있는 보스를 초기화한다. ProjectileSystem의 ownerId는 보스 자신의 탄환만 취소하고, 명중 처리 중에는 취소 표시만 해 반복 목록을 안전하게 유지한다. 재등장 시각은 GameSession.WorldSeconds 기준으로 지역 저장에 기록한다. FIELD_BOSSES.md 참조.

## 탄환 개별 설정 (2026-10-27)

원거리 아이템의 attackRange/projectileSpeed/projectileLifetime과 몬스터 종류의 projectileRange/projectileSpeed/projectileLifetime이 각각 사거리·탄속·최대 수명을 소유한다. ProjectileSettings.Duration/Distance를 실제 발사와 두 관리창의 미리보기가 공유한다. PlayerControls의 기존 공통 탄속 필드는 호환을 위해 숨겨서만 남긴다. PROJECTILE_SETTINGS.md 참조.

## 필드 채집물 (2026-10-27)

FarmItemPlacement.forage가 있는 지점은 ForageSystem이 상태를 소유하고 FarmGame.World의 기존 조각을 표시한다. DetachAt은 forageId가 있을 때만 Collect로 위임한다. FarmGame.Tick의 채집 갱신은 GameSession.WorldSeconds의 절대 예정 시각을 확인하며 맵 복원에서 전달하는 경과 시간을 중복 가산하지 않는다. FarmPersistence는 일반 pieces와 forageNodes를 구분하고, 획득 저장 실패 시 인벤토리/지점 상태를 복구한다. 아이템 관리의 ‘채집물 설정’에서 종류를 편집하고 Scene에 배치한다. FORAGING.md 참조.

## 야간 상인 (2026-10-27)

DayNightCycle.ShopsOpen은 Night에만 true다. NpcManager.UpdateAvailability가 고정/방문 프로필을 확인하고 GameSession.TryNightVisit에 방문 결과를 요청한다. GameSession.Merchants가 저장 seed·씬·프로필·cycleNumber로 방문/불참과 위치를 유지한다. GameSession.Bind는 복원 후 현재 맵 상인을 갱신하고 필요한 첫 결과/이전 저장 seed를 저장한다. 비상점 NPC의 기존 주간/상시 대화는 유지한다. NIGHT_MERCHANTS.md 참조.

## 첫 숲 배치 (2026-10-27)

ForestFieldLayout은 ForestClearing에만 적용하는 명시적 일회성 Editor 배치다. Ground/Decoration 하위의 일반 Scene 오브젝트를 편집하며 런타임 재생성은 없다. 기존 보스·채집·상인·지역 스폰을 그대로 재사용한다. FIRST_FOREST_FIELD.md 참조.

## 씬 폴더 분리 (2026-10-27)

OpenWorld/LegacyVillage/Tests/System으로 Scene 자산을 분류한다. 런타임 ScenePaths.Migrate는 저장 복원 검증 전에 이전 경로를 현재 경로로 변환한다. Editor의 SceneOrganization은 명시적 요청 시에만 자산·문자열 경로를 이동하며 열린 씬을 저장하거나 닫지 않는다. 공통 씬 선택기는 폴더명을 함께 표시한다. SCENE_GROUPS.md 참조.

## 연속 시작 월드 (2026-10-28)

GameMenu.FirstScene은 OpenWorld/ForestClearing을 가리킨다. SingleWorldSetup은 기존 숲의 출구 비활성화·시작 공터·밤 상인·보급품·남북 지역만 작성하며 런타임 재생성은 없다. 기존 게임 시스템을 재사용하고 웨이브는 추가하지 않는다. CONTINUOUS_WORLD.md 참조.
