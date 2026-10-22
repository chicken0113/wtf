# 코드 수정 안내

## 먼저 볼 곳

요청을 받으면 동작·입력·예외·변경 범위를 사용자에게 설명하고 확인을 받은 뒤 구현한다. 아래 표에서 담당 파일과 진입 함수부터 찾는다. 기존 구현이 있으면 해당 경로를 수정하고 다른 곳에 같은 규칙을 만들지 않는다.

| 수정할 기능 | 규칙·상태 담당 | 연결·표시 담당 |
| --- | --- | --- |
| 아이템 수량·겹치기·선택·슬롯 제한 | `Assets/Scripts/InventoryState.cs` | `FarmGame.Inventory.cs`가 메시지·바닥 드롭 연결 |
| 밭 갈기·심기·물주기·성장·수확·재수확 | `Assets/Scripts/FarmingSystem.cs` | `FarmGame`의 기존 공개 함수가 위임 |
| 밭 한 칸의 상태 | `Assets/Scripts/PlantPlot.cs` | SpriteRenderer를 보관하지 않음 |
| 작물·흙의 그림·크기·그리는 순서 | `Assets/Scripts/FarmWorldView.cs` | 농사 규칙을 변경하지 않음 |
| 핫바·인벤토리·골드·지도·작물 안내 | `Assets/Scripts/FarmHud.cs` | `DrawSlot`이 핫바/인벤토리 표시와 슬롯 클릭을 공통 처리 |
| 키 읽기·프레임 처리 순서·메뉴 열기 | `Assets/Scripts/FarmGame.Input.cs` | `Update`, `ProcessMenuInput` |
| E/F 대상 선택·조준·UI 클릭 차단 | `Assets/Scripts/FarmGame.Controls.cs` | `FarmAction`, `InteractWorld`, `UpdateAim` |
| 무기 반동·캐릭터 동작·카메라 흔들림 | `Assets/Scripts/FarmGame.Feedback.cs` | `AnimateFarmer`, `LateUpdate`, `WeaponFeedback` |
| 월드 아이템 줍기·드롭·집 배치 | `Assets/Scripts/FarmGame.World.cs` | `DetachAt`, `Spawn`, `CanPlaceHouse`, `PlaceHouse` |
| NPC 대화·구매·판매 | `Assets/Scripts/NpcManager.cs` | `ShopNpc`, `NpcProfile` 데이터 |
| 피해·회피·적 행동·탄환 | `Assets/Scripts/OpenFieldCombat.cs` | 이번 분리 작업의 대상 밖. 별도 분리는 다음 단계 |
| 맵/플레이어 저장 연결 | `Assets/Scripts/FarmPersistence.cs` | 각 상태 소유자에게 Capture/Restore 위임 |
| 파일 저장·백업·맵 이동·저장 오류 | `Assets/Scripts/GameSession.cs` | 의뢰는 `GameSession.Guild.cs` |
| 저장 파일 필드 | `Assets/Scripts/GameSaveData.cs` | 버전 1 유지 |
| 벽·통행·탄환 차단·길 찾기 | `Assets/Scripts/MapCollision.cs` | Scene의 `MapObstacle`, `FarmBuilding` |

`FarmGame.cs`는 Unity 컴포넌트 초기화, 기존 공개 API, 시스템 연결을 맡는다. 기존 컴포넌트와 직렬화 필드를 유지하므로 씬마다 새 컴포넌트를 붙일 필요가 없다.

`FarmGame.*.cs`는 하나의 컴포넌트를 주제별로 나눈 연결 코드다. 이 파일 분할 자체를 독립된 시스템으로 간주하지 않는다. 실제 인벤토리 상태는 `InventoryState`, 밭 상태와 농사 규칙은 `FarmingSystem`, 그림은 `FarmWorldView`, HUD는 `FarmHud`가 소유한다.

## 주요 호출 흐름

### 심기·수확

`FarmGame.Input.Update → FarmAction(E) → FarmingSystem.InteractPlot → PlantSelected / Collect / Uproot`

- 심기 가능 여부는 `FarmingSystem.PlantSelected`가 판단한다.
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

## 수치 변경 위치

| 수치 | Unity에서 볼 데이터 |
| --- | --- |
| 이동·회피·탄속·반동·흔들림·농기구 연속 간격 | `Assets/Resources/PlayerControls.asset` (`Tools > Everything Grows > Player controls`) |
| 성장 시간·수확 결과·크기·무기 피해/사거리/간격 | 각 `PlantDefinition` 에셋 |
| 시작 아이템 | `Assets/Resources/StartingInventory.asset` |
| NPC 판매 물품·가격 | 해당 씬의 `NpcManager` 프로필 |
| 길드 의뢰 | `Assets/Resources/GuildQuests.asset` |
| 맵 배치·범위·통로 | 각 씬의 `FarmMapLayout`, `MapExit`, `MapArrivalPoint` |

## 변경 후 필요한 검증

`Assets/Scripts/Editor`의 기존 동작 검사를 재사용한다. 게임 규칙을 테스트 안에 다시 구현하지 않는다.

| 변경한 부분 | 검사 진입점 |
| --- | --- |
| 인벤토리·농사·수확·집 | `FoundationRefactorCheck.Run` — 기존 슬롯/보관/성장/재수확/하트/집 검사 묶음 |
| E/F·메뉴 차단·조준·회피 | `BasicControlsCheck.Run` |
| 공격·반동·회피 모습·농기구 연속 사용 | `ActionFeelCheck.Run` |
| 실제 프레임의 인벤토리/상점 UI·초기 지급 | `OpenFieldPlayCheck.RunInventory` |
| 상점·맵 왕복·작물/소지품/집 저장 | `InteriorWorldCheck.Run`, 이후 같은 테스트 저장으로 `Resume` |
| 길드 보상·중복 지급·저장 실패 | `GuildQuestCheck.Run`, `Resume` |

이 검사는 프로젝트를 임시 폴더에 복사한 Unity batch 환경에서만 실행한다. Play 검사는 `WTF_SAVE_TEST_DIR`로 별도 저장 위치를 지정한다. 사용자가 연 씬과 실제 자동 저장으로 테스트하지 않는다. 예전 씬 생성 함수가 포함된 검사도 있으므로 검사 프로젝트의 씬/설정/에셋을 본 프로젝트로 통째로 복사하지 않는다.

## 유지할 경계

- 새 패키지·공통 프레임워크·이벤트 버스 없이 현재 클래스 간 호출로 연결한다.
- 기존 `.meta`/GUID, 직렬화 필드 이름, 씬 배치, 저장 DTO와 버전을 유지한다. 변경이 필요하면 마이그레이션을 먼저 제안한다.
- 규칙을 바꾸는 작업과 구조 정리를 구분한다. 리팩터링 중 발견한 새 기획은 확인 없이 추가하지 않는다.
- 현재 농사는 `FarmGame`의 인벤토리·월드 API에 의존한다. 독립된 서버 시뮬레이션이나 멀티 구조까지 분리한 상태는 아니다.
- 전투 클래스와 초기화 코드에는 추가 정리 여지가 있다. 이 단계가 프로젝트 전체의 기술 부채 제거를 의미하지 않는다.
