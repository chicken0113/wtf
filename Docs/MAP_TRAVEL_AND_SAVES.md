# 맵 상태 유지와 자동 저장

## 현재 동작

- 새 게임을 처음 시작할 때 기본 아이템을 포함한 초기 상태를 자동 저장한다.
- 출입구 이동 전에 현재 맵과 플레이어를 저장한다. 목적지의 도착 지점 확인과 복원이 끝나면 도착 상태도 저장한다.
- 다시 실행하면 마지막으로 저장한 맵·위치·소지품으로 이어서 시작한다.
- 저장 대상: 인벤토리 아이템/수량/선택 칸, 골드, 떼어낸 인벤토리 칸과 하트, HP, 처치 수, 낮밤 진행도, 맵별 경작지/물/식재 대상/성장도/재수확 상태, 드롭 아이템의 위치와 크기, 직접 설치한 집, 수확해서 사라진 기존 집.
- 다른 맵에 남겨 둔 성숙한 창고 식물의 인벤토리 용량 효과도 유지하므로 이동 때문에 소지품이 넘쳐 떨어지지 않는다.
- 다른 맵에서 게임 시간이 흐르면, 돌아왔을 때 물을 준 작물은 그 시간만큼 성장한다. 게임을 종료한 시간과 씬 로딩 시간은 성장 시간에 포함하지 않는다.
- 적과 탄환은 현재 시제품의 전투 초기화 규칙을 따른다. 이번 저장 대상에 적 개별 상태는 포함하지 않는다.

## 씬에서 출입구와 도착 지점 배치

1. 기존 집의 위치나 계층을 편집하기 전에 **Tools > Everything Grows > Map travel > Prepare current map IDs**를 실행하고 씬을 저장한다. 각 집에 고유 저장 ID를 부여하며 위치와 외형은 바꾸지 않는다. 씬을 복제한 뒤에도 해당 메뉴를 사용할 수 있다.
2. **Assets/Prefabs/Map/MapArrivalPoint.prefab**을 도착할 씬에 놓는다. `Arrival Id`를 지정하고 실제 플레이어가 설 위치로 이동한다. 같은 씬 안에서는 ID를 중복시키지 않는다.
3. **Assets/Prefabs/Map/MapExit.prefab**을 출발할 씬에 놓는다. Inspector의 `Destination Scene`에 대상 씬 파일을 지정하고, 도착 지점과 같은 `Arrival Id`를 입력한다.
4. `Area Size`로 출입구 영역을 조절한다. 기본은 영역 안에서 E를 눌러 이동하며 `Require Interact`를 끄면 닿아서 이동한다. 도착 직후에는 1초간 자동 이동을 막는다.
5. Inspector에 Build Settings 경고가 나타나면 **Enable destination in Build Settings**를 누른다. 출발 씬도 Build Settings에 등록되어 있어야 하며 Tools > Everything Grows > Map travel > Enable current scene in Build Settings로 등록할 수 있다. 대상 씬에는 기본 데이터가 설정된 FarmGame과 활성화된 도착 지점이 필요하다.
6. 왕복 연결은 반대 방향 출입구와 도착 지점을 별도로 배치한다. GameObject > Everything Grows 메뉴에서도 두 오브젝트를 생성할 수 있다.

출입구와 도착 지점은 Scene에서 초록 영역·청록 표시로 보인다. 플레이 중에는 출입구 영역 안에서 E 이동 안내가 표시된다. 실제 문의 외형은 출입구에 SpriteRenderer나 장식 오브젝트를 붙여 구성할 수 있다.

## 저장 파일과 실패 처리

- 기본 단일 자동 저장 슬롯: `Application.persistentDataPath/wtf/wtf-autosave.json`.
- 현재 Windows 프로젝트 설정의 경로는 `%USERPROFILE%/AppData/LocalLow/DefaultCompany/EverythingGrows/wtf/`이다. 원격 Git 저장소가 아닌 로컬 플레이 데이터다.
- 임시 파일에 기록한 후 교체하며 이전 파일은 `.bak`으로 보관한다. 기본 파일의 JSON이 손상됐으면 백업을 읽는다.
- 저장된 아이템 참조가 사라졌거나 복원할 수 없는 경우 기존 파일을 새 게임으로 덮어쓰지 않는다. 저장/이동 오류를 화면에 표시한다.
- 대상 씬이 Build Settings에 없으면 이동을 시작하지 않는다. 도착 지점이 없거나 중복됐거나 대상 씬에 FarmGame이 없으면 저장한 출발 맵으로 돌아온다.
- 씬 파일 경로는 저장 맵의 키다. 저장을 이어 쓸 때 씬 경로와 Map Object ID를 유지한다.
- 아이템은 GUID 기반 `Assets/Resources/SaveItemCatalog.asset`으로 참조한다. 에디터가 PlantDefinition 변경 시 갱신하며 **Tools > Everything Grows > Refresh save item catalog**로 직접 갱신할 수도 있다.

## 집에서 시간 보내기 연동

`GameSession.SaveAfterHouseTime(farm)`을 행동 완료 시 호출하도록 연결 지점을 제공한다. 집에서 시간을 보내는 행동 자체는 아직 구현하지 않았으며, 이번 작업에서 수면·날짜 규칙을 새로 추가하지 않는다.

## 검증

별도 임시 Unity 프로젝트와 테스트 전용 저장 폴더에서 실제 Play 모드로 맵 왕복, 잘못된 도착 지점의 원복, 소지품/HP/골드 유지, 밭과 성장도, 부재 시간 성장, 드롭 크기, 설치한 집, 수확한 원래 집의 제거 상태, 재실행 및 백업 복원을 검사한다. 사용자 플레이 저장 파일은 검증에 사용하지 않는다.
