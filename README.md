# EverythingGrows

새 게임의 2D 탑뷰 첫 시제품. 설계는 [Docs/GAME_DESIGN.md](Docs/GAME_DESIGN.md).

Unity Hub에서 C:/Users/MSI/wtf를 Add project from disk로 등록하고 Unity 6000.3.25f1로 엽니다.
현재 열린 씬을 세팅해 저장한 Assets/Scenes/FirstGarden.unity를 열고 Play 합니다. Garden.unity는 초기 샘플로 남아 있습니다. 처음 설정이 필요하면 Tools > Everything Grows > Create prototype을 실행합니다.

- WASD: 이동
- 우클릭: 물건·구름 뜯기 / 심은 대상 회수 / UI 칸 뜯기
- 비어 있는 슬롯 선택 + 좌클릭: 맨손 경작
- Tab: 도구 사용 / 심기 모드 전환. 심기 모드에서 물건 선택 + 경작한 땅 좌클릭
- 도구 모드 좌클릭: 삽은 경작, 물뿌리개는 물주기. 도구 이외의 물건은 바로 심기
- Space: 마우스 위치에 임시 맨손 물주기
- F: 손에 든 삽 / 물뿌리개 사용
- E: 생산된 돌 수확
- 1~9, 마우스 휠 또는 슬롯 클릭: 선택. Q: 내려놓기. H: 도움말 표시
- R: 들고 있는 인벤토리 칸을 UI에 다시 붙여 원래 용량 복구

Assets/Settings의 PlantDefinition에서 성장 시간·효과 범위·생산 주기·보관 보너스를 조절합니다.
픽셀 탑뷰 임시 그래픽을 적용했습니다. 캐릭터는 이동 방향을 보고, 손에 든 도구는 사용 시 회전합니다. 인벤토리에 물건 아이콘을 표시합니다. 모든 UI/배경을 구현한 상태는 아닙니다.
그래픽 원본과 생성 프롬프트는 [Docs/ART.md](Docs/ART.md)에 기록했습니다. 실제 스타듀밸리 에셋은 사용하지 않았습니다.
마지막 보관 칸은 보호하며 공간이 부족하면 뜯기·회수는 거절합니다. 생긴 창고를 회수해도 기존 물건을 버리지 않습니다.
현재 물주기는 성장 시작 조건이고 자동화 효과는 단순한 첫 구현입니다. 발견을 유도하는 진행, 세부 농사 조건, 저장, 경제, 멀티플레이는 아직 없습니다.

작업 저장소: https://github.com/chicken0113/wtf — 로컬 폴더 C:/Users/MSI/wtf. Unity Hub에는 이 폴더를 등록합니다.

현재 첫 단계: 마우스 위치에 경작/식재 가능 여부를 초록·빨강으로 표시하고, 심어진 대상은 물 필요 여부와 성장 진행 막대를 표시합니다. 한 밭에 한 대상만 심을 수 있고 실패한 식재는 아이템을 소비하지 않습니다.
현재 씬을 다시 연결하려면 Tools > Everything Grows > Set up current scene을 사용합니다. 현재 씬의 오브젝트를 유지하고 Farm 데이터와 2D 카메라를 연결합니다. 저장되지 않은 씬은 FirstGarden 이름의 새 파일로 저장합니다.
