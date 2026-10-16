# 전투 테스트 맵

## 플레이

- `FirstGarden`에서 집과 밭 오른쪽의 청록색 입구 안으로 이동해 **E**를 누릅니다. 기본 입구 중심은 `(6, 0)`입니다.
- `CombatTest`의 왼쪽 초록색 바닥은 안전 공간입니다. 안쪽으로 이동하면 초록색 근접 몬스터와 보라색 원거리 몬스터가 활동합니다.
- 근접 몬스터는 추격하고 공격을 예고합니다. 원거리 몬스터는 예고 후 해당 방향으로 탄환을 발사합니다.
- 벽·기둥·엄폐물이 이동과 공격을 차단합니다. WASD 이동과 Shift 구르기를 사용합니다.
- 왼쪽 청록색 출구에서 E를 눌러 농장으로 돌아옵니다. 왕복할 때 기존 자동 저장을 사용합니다.

이번 단계에서는 무기 지급·구매 구성·가격을 변경하지 않았습니다. 이미 소지한 무기가 있다면 선택하고 좌클릭으로 사용할 수 있습니다.

## 씬 편집

`Assets/Scenes/CombatTest.unity`를 열면 아래 그룹을 직접 편집할 수 있습니다.

- `Combat Test / EditableMap / Ground`: 바닥.
- `Obstacles`: 외곽 벽 4개, 기둥 2개, 엄폐물 2개. MapObstacle로 충돌하며 Transform 크기를 바꿀 수 있습니다.
- `Enemies`: Chaser spawn과 Spitter spawn. 위치를 옮기거나 기존 몬스터 배치 프리팹으로 추가합니다.
- `Travel`: 농장으로 돌아가는 MapExit와 combat_entry 도착 지점.
- `Entry safe area`: 입구 안전 범위. 몬스터를 이 안에 두면 생성되지 않습니다.

농장의 입구·도착 지점은 `EditableMap / Combat Test Connection`에 있습니다. 두 씬 모두 Build Settings에 등록합니다.

FarmMapLayout의 `Combat Schedule`은 Night Only / Peaceful / Always입니다. 농장은 Peaceful, 전투 테스트 맵은 Always입니다. 전투 맵은 Day Night Lighting을 꺼서 밝기가 일정합니다. 시간 자체는 계속 흐르고 농장으로 돌아가면 해당 시간대가 적용됩니다.

## 현재 범위

농장은 전체 안전하며 기존 몬스터 마커는 남겨두되 생성되지 않습니다. 전투 맵의 몬스터는 기존 임시 전투 동작을 사용합니다. 재입장 시 다시 생성되고, 처치 후 기존 재생성 규칙도 유지합니다. 적 개별 상태 저장, 층·보스·클리어 보상과 새로운 사망 규칙은 아직 추가하지 않았습니다.

## 적용 및 검증

농장 연결 도구는 현재 FirstGarden의 기존 배치를 유지하며 입구를 한 번만 추가합니다. `Tools > Everything Grows > Combat test > Connect current farm`에서 다시 실행할 수 있습니다.

실제 Play 모드에서 왕복 이동·도착 지점·안전구역·인벤토리 유지·자동 저장·시간대 독립 몬스터·벽 충돌·추격·공격 예고·발사 동작을 검사했습니다. 테스트는 별도 프로젝트와 저장 폴더를 사용합니다.
