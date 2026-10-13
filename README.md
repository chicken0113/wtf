# EverythingGrows

새 게임의 2D 탑뷰 첫 시제품. 설계는 [Docs/GAME_DESIGN.md](Docs/GAME_DESIGN.md).

Unity Hub에서 C:/Users/MSI/wtf를 Add project from disk로 등록하고 Unity 6000.3.25f1로 엽니다.
Assets/Scenes/Garden.unity를 열고 Play 합니다. 처음 설정이 필요하면 Tools > Everything Grows > Create prototype을 실행합니다.

- WASD: 이동
- 우클릭: 물건·구름 뜯기 / 심은 대상 회수 / UI 칸 뜯기
- 비어 있는 슬롯 선택 + 좌클릭: 맨손 경작
- 물건 선택 + 경작한 땅 좌클릭: 심기
- Space: 마우스 위치에 임시 맨손 물주기
- F: 손에 든 삽 / 물뿌리개 사용
- E: 생산된 돌 수확
- 1~9 또는 슬롯 클릭: 선택. Q: 내려놓기
- R: 들고 있는 인벤토리 칸을 UI에 다시 붙여 원래 용량 복구

Assets/Settings의 PlantDefinition에서 성장 시간·효과 범위·생산 주기·보관 보너스를 조절합니다.
최종 그림과 연출은 미정이며 현재 단색 도형은 조작과 기능 확인용입니다. 모든 UI/배경을 구현한 상태는 아닙니다.
마지막 보관 칸은 보호하며 공간이 부족하면 뜯기·회수는 거절합니다. 생긴 창고를 회수해도 기존 물건을 버리지 않습니다.
현재 물주기는 성장 시작 조건이고 자동화 효과는 단순한 첫 구현입니다. 발견을 유도하는 진행, 세부 농사 조건, 저장, 경제, 멀티플레이는 아직 없습니다.

작업 저장소: https://github.com/chicken0113/wtf — 로컬 폴더 C:/Users/MSI/wtf. Unity Hub에는 이 폴더를 등록합니다.

현재 첫 단계: 마우스 위치에 경작/식재 가능 여부를 초록·빨강으로 표시하고, 심어진 대상은 물 필요 여부와 성장 진행 막대를 표시합니다. 한 밭에 한 대상만 심을 수 있고 실패한 식재는 아이템을 소비하지 않습니다.
