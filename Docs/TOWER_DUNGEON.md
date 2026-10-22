# 탑 던전 1·2층

## 현재 구성

외부 입구 필드 `DungeonEntrance` → **탑 1층 `TowerFirstFloor`** → **탑 2층 `CombatTest`**.

- 외부↔1층은 기존 규칙대로 주황색 자동 통로다.
- 1층↔2층은 청록색 난간이 있는 계단 영역에서 **F**를 눌러 이동한다. 접촉이나 E만으로는 이동하지 않는다.
- 1층은 전체 안전 구역이며 낮/밤에 몬스터가 없다. 실내 조명은 일정하고, 기존 시간은 계속 흐른다. 벤치·기둥·배너는 장식이다. 새로운 상점/회복 시설은 추가하지 않았다.
- 2층은 기존 근접/원거리 적 두 마리와 장애물 전투 구성을 사용한다. 입구의 기존 안전 구역을 유지한다. 적을 남겨둔 채로도 1층에 내려올 수 있다.
- 3층·보스·전멸 해금·새로운 보상·새 몬스터·전투 밸런스는 이번 범위에 없다.
- 적 개체를 저장하는 기능은 추가하지 않았다. 층에 재진입하면 기존 씬의 적 생성/재등장 규칙을 따른다.

## 씬 편집

| 대상 | 위치 |
| --- | --- |
| 1층 | `Assets/Scenes/TowerFirstFloor.unity` |
| 2층 | `Assets/Scenes/CombatTest.unity` — 기존 경로/GUID 유지 |
| 탑 외관 | `DungeonEntrance`의 `EditableMap/Tower exterior` |
| 계단 | 각 층의 `EditableMap/Travel` 아래 `Up to 2F` / `Down to 1F` |
| 이동 연결 | 계단의 `MapExit`: destinationScene / arrivalId / areaSize / requireInteract |
| 도착 위치 | 같은 씬의 `MapArrivalPoint` |
| 벽·장애물 | `MapObstacle` |
| 적 위치/종류 | 2층의 `EnemyPlacement` |
| 안전 범위 | `FarmMapLayout.safeArea` |

씬에서 위치·크기·장식을 직접 수정한다. 계단 그림과 MapExit는 같은 부모에 있어 함께 움직인다. 도착 지점은 별도 오브젝트이므로 계단을 크게 옮기면 도착 위치와 접근 경로도 확인한다.

## 저장과 호환

- 기존 `GameSession.Travel`의 이동 전/후 자동 저장을 사용한다. 새 저장 형식은 없다.
- 층별 드롭 아이템, 소지품·골드·체력·처치 수가 기존 저장 경로로 유지된다.
- `CombatTest` 경로/GUID와 기존 도착 ID를 유지하므로 이전 던전 저장과 길드의 해당 씬 처치 목표도 유지된다. 예전 저장에서 던전에 있던 플레이어는 2층으로 이어서 시작한다.
- 새 1층을 빌드 목록과 월드 연결 검사에 추가했다. 총 12개 씬/24개 출입구다.

## 작성·검사 코드

- `Editor/TowerDungeonSetup.cs`: 한 번만 배치하는 편집 도구. Play 중이거나 저장하지 않은 씬이 있으면 중단한다. 이미 작성된 층의 배치를 다시 만들지 않는다.
- Unity 메뉴: `Tools > Everything Grows > World > Create tower first and second floors`.
- 설정 검사: `ProjectSetupCheck.Run`, 동선: `WorldRouteAudit.Validate`.
- 격리된 프로젝트와 테스트 저장에서 `TowerDungeonCheck.Run` / `Resume`로 F 계단·안전 층·전투·층별 상태·재시작을 확인한다.
- 기존 `InteriorWorldCheck`와 `GuildQuestCheck`도 추가된 1층을 경유한다.
