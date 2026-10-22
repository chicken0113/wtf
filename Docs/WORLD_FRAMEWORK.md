# 월드 기본 구조

## 현재 구성

```text
                  던전 내부
                     │
숲·버려진 공터 ─ 던전 입구 필드 ─ 훈련장
                 /       \
               농장      마을 ↔ 잡화점 내부
                 \       /
                  연결 통로
```

- **FirstGarden**: 기존 농사·집·상인. 연결 통로와 던전 입구 필드로 이동합니다.
- **Town**: 안전한 마을. 64×60 크기이며 사용자 스케치의 위치대로 북서쪽 길드·공원, 북동쪽 대장간, 중앙 돌바닥 광장, 남서쪽 주택·잡화점을 배치했습니다. 그래픽은 기존 프로젝트 에셋이며 건물 12채의 지붕 위에 용도별 표식을 붙였습니다. Rowan은 대장간 안으로 이동했습니다.
- **GeneralStore**: 카운터·진열대·상인이 있는 실내. 낮에 기존 물품을 구매하고 인벤토리 물품을 판매합니다.
- **CombatTest**: 기존 전투 테스트 공간. 던전 입구 필드로 돌아옵니다.
- **FarmTownPassage / DungeonEntrance / ForestClearing / TrainingGround**: 연결 통로·입구 필드·숲·훈련장. [삼각형 월드 배치](TRIANGLE_WORLD.md)를 참고합니다.
- **FarmHouseInterior / GuildInterior / SmithInterior**: 집·길드·대장간 실내. 파란 발판에서 E로 출입합니다. [건물 내부](BUILDING_INTERIORS.md)를 참고합니다. Rowan은 대장간 내부에서 기존 무기 거래를 담당하고, 길드 접수원/게시판은 의뢰를 수락·보고하는 창구입니다.

사용자 스케치 기준으로 마을 서쪽은 농장 방향, 북쪽은 던전 방향입니다. 광장은 스타듀밸리의 돌바닥 광장 분위기를 유지하며, 시설 위치는 사용자 스케치대로 길드 북서쪽·대장간 북동쪽·주택과 상점 남서쪽입니다. **지역 간 이동은 주황색 통로에 몸이 닿으면 자동으로 이동**합니다. 청록색 E 문은 건물 실내 출입에만 사용합니다. 잡화점 앞 청록색 발판에서 E로 들어가고, 실내 아래쪽 발판에서 E로 나옵니다.

마을과 농장은 안전합니다. 마을의 Mira·June, 길드 접수원, 대장간의 Rowan은 가까이에서 E로 대화합니다. 대화창은 E / Esc / Tab 또는 Close로 닫습니다. 주민은 밤에도 남으며, 잡화점·대장간 거래는 기존 낮 영업 규칙을 따릅니다.

## 씬에서 편집

- `Assets/Scenes/Town.unity`의 `EditableMap` 아래 Ground and paths / Buildings / Town districts / Plaza and decorations / Residents / Travel 그룹을 편집합니다.
- `Assets/Scenes/GeneralStore.unity`의 Interior 그룹에서 카운터·벽·진열대, Merchant에서 상인 위치를 편집합니다.
- 마을 건물 프리팹: `Assets/Prefabs/Map/TownGeneralStore.prefab`, `TownSmith.prefab`, `TownHouse.prefab`.
- 각 건물 아래 `Roof identity`는 용도를 구분하는 지붕 표식입니다. 건물과 함께 움직이며 개별 조절·교체할 수 있습니다. 표식 프리팹은 `Assets/Prefabs/Map/RoofSigns`에 있습니다.
- 건물의 MapObstacle은 발밑 충돌 영역입니다. 건물 전체 그림과 독립적으로 조절할 수 있습니다.
- 길 안내 문구는 WorldLabel의 Text, 표시 거리는 Visible Distance입니다.
- 출입구·도착 지점을 옮길 때 연결된 Arrival Id는 유지합니다. 상점 입구나 도착 지점에 장애물을 겹치지 않게 배치합니다.
- 기존 농장·던전의 입출구 위치는 유지하고 목적지만 마을로 연결했습니다. 저장 파일에서 사용하던 기존 도착 ID도 유지합니다.

## NPC 설정

각 씬의 Farm 루트에 있는 NpcManager 또는 `Tools > Everything Grows > NPC manager`에서 설정합니다.

- `Shop Enabled`: 거래 가능한 NPC. 끄면 Greeting을 출력하는 대화 NPC입니다. 대화 NPC에게는 구매·판매가 되지 않습니다.
- `Guild Quest Giver`: 길드 의뢰 화면을 여는 NPC/게시판입니다. 의뢰 내용과 보상은 GuildQuests 자산에서 설정합니다.
- `Greeting`: 대화 내용.
- `Offers`: 기존과 동일한 판매 물품·구매가·판매가 설정.
- NPC 오브젝트의 `Npc Id`를 프로필의 Id와 맞춥니다.
- NPC의 `Daytime Only`를 켜면 낮에만 방문합니다. 끄면 밤에도 대화할 수 있습니다. 거래는 낮에만 가능합니다.

잡화점의 초기 물품과 가격은 기존 농장 상인 설정을 복사했습니다. 이후 두 상인의 물품은 각 씬의 NPC 매니저에서 개별 조절합니다.

## 상태 유지와 검증

네 씬은 Build Settings에 등록되어 있습니다. 이동 전후 자동 저장, 공통 인벤토리·골드·HP·시간, 맵별 작물과 드롭 상태 유지가 연결되어 있습니다. 재실행하면 마지막 저장된 맵에서 이어집니다.

별도 Unity 프로젝트와 저장 폴더에서 지역 간 접촉 이동과 상점 실내 E 출입을 포함한 네 씬 왕복, 실내 진입·퇴장, 주민 대화·야간 방문, 상점 구매·판매·야간 종료, 기존 농장 작물·드롭·소지품 유지 및 저장 복원을 검사합니다.

퀘스트·호감도·장비 강화·추가 변형 아이템·던전 층 진행과 보스는 이후 콘텐츠 작업입니다. 현재는 장소와 이동, 주민 상호작용을 갖춘 기본 월드입니다.


## 현재 작업 기준과 연결 점검

현재 마을은 펠리컨 타운의 광장 분위기를 유지하면서 주요 시설 위치를 사용자 스케치에 맞췄습니다. 길은 주요 건물 위치를 유지한 채 구간마다 완만하게 꺾었고, 긴 수직·수평 도로의 반복을 줄였습니다. 세부 배치는 Scene에서 계속 수정할 수 있습니다. 농장·던전 내부의 상세 배치는 아직 보류 상태입니다. 데모 전체 목록은 [DEMO_ROADMAP.md](DEMO_ROADMAP.md)를 기준으로 합니다.

`Tools > Everything Grows > World > Validate world connections`를 실행하면 네 씬의 Build Settings 등록, 출입구 목적지와 도착 ID, 중복 ID, 막힌 도착·스폰 위치, NPC 프로필 누락을 검사합니다. 오류는 Console에서 확인합니다. 도구는 배치를 수정하거나 씬을 저장하지 않습니다. 이 검사는 전체 지형에서 목적지까지 걸어갈 수 있는지를 보장하는 경로 탐색 검사는 아닙니다.

기본 조작은 [FOUNDATION_CONTROLS.md](FOUNDATION_CONTROLS.md)에 정리했습니다. 싱글플레이에서 인벤토리·대화·상점·지도는 게임 시간을 멈추며, 닫는 키로 심기·수확·이동이 동시에 실행되지 않습니다.


## 사용자 초안 적용

최신 위치 기준은 [MAP_LAYOUT_PLAN.md](MAP_LAYOUT_PLAN.md)입니다. 마을 동쪽 가장자리에 강과 다리 두 개가 있고 남쪽은 해안입니다. 시설 위치의 최신 기준은 사용자 그림이며, 광장은 맵 중앙에 둡니다. 농장과 던전에는 Farm Dungeon Shortcut을 추가해 마을을 거치지 않고 이동할 수 있습니다. 기존 출입구의 도착 ID와 상점 실내 연결은 유지합니다.

## 씬 정리

2026-10-22 중복·이전 샘플 Garden, StarterFarm, EditableField, DailyFarm을 씬 파일 및 메타와 함께 정리했다. 현재 Assets/Scenes와 빌드 목록은 FirstGarden(농장), Town(마을), GeneralStore(상점 실내), CombatTest(던전) 네 씬만 유지한다. 현재 씬 경로·GUID와 씬 내용은 바꾸지 않았다.

오래된 Create prototype 메뉴는 노출하지 않고, 해당 생성 코드는 임시 프로젝트의 배치 검증에서만 실행하도록 제한해 이전 샘플 씬이 일반 작업 중 다시 생기지 않도록 했다. 삭제 전 자산 참조를 확인했고 별도 Unity 프로젝트에서 출입구 8개 및 실제 Play의 맵 왕복·거래·작물·소지품·골드·자동 저장 유지 검사가 통과했다.
