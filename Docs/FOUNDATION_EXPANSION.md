# 콘텐츠 확장을 위한 기반

## 아이템 사용과 효과

- 좌클릭은 `FarmGame.UseSelectedItem`을 거친다. 기존 근접/원거리 공격은 `OpenFieldCombat.Attack`, 기본 도구는 기존 `UsePrimaryTool/UseTool`에 위임한다.
- 아이템 관리 → **Special use effect**: 비워두면 기존 동작, 연결하면 해당 `ItemUseEffect.TryUse`가 실행된다.
- 새 특수 동작은 `ItemUseEffect`를 상속하는 ScriptableObject 한 종류로 추가한다. `TryUse(farm,item,target)`는 대상/거리/자원 등을 확인하고 실제 성공 때만 true를 반환해야 한다. 실패 전에 아이템이나 월드 상태를 소모하지 않는다.
- `repeatWhileHeld`로 누르고 있기 반복 여부를 선택한다. 기존 `attackInterval`을 특수 사용 간격으로 재사용한다.
- **Consume on successful use**: 성공 시 한 개만 소비한다. 실패/재사용 대기/회피/UI에서는 소비하지 않는다. 집 설치와 하트 재부착의 기존 자체 소비는 중복 차감하지 않는다.
- 기존 아이템은 모두 특수 효과 없음·추가 소모 없음이다. 이번에 새 특수 무기나 소모품 콘텐츠는 추가하지 않았다.

## 몬스터 종류별 능력치

`Tools → Everything Grows → 몬스터 관리`에서 종류별로 행동·능력치·보상을 함께 설정한다.

- Behavior: Melee / Ranged. LegacyPlacement는 이전 배치의 Ranged 값을 사용하는 호환 모드다.
- Max Health, Move Speed, Return Speed, Detection Range.
- Attack Range, Melee Hit Range, Attack Half Hearts(반 하트 단위 피해), Windup Seconds, Attack Cooldown.
- Projectile Speed / Lifetime.
- 기존 종류 값: 체력 55, 탐지 7, 피해 반 하트. 근접 속도 2/사거리 1.05/재사용 1초, 원거리 속도 1.3/사거리 4.8/재사용 1.7초. 기존 공격 예고·발사·피격·처치·길드·보상 경로는 유지한다.
- 새로운 행동/공격 패턴을 넣을 위치는 `EnemyCombatSystem.TickEnemy`의 추적/공격 준비와 실행 부분이다. 입력이나 인벤토리에 몬스터별 분기를 넣지 않는다.
- 훈련 표적은 기존 체력 55와 3초 회복, 공격·보상 제외를 유지한다. 보통 적은 재등장하지 않으며 처치 상태는 기존 저장에 포함된다.

## 탑의 층 추가

`Tools → Everything Grows → 탑 층 관리`를 사용한다.

1. 템플릿 씬, 새 층 번호, Preparation / Normal / Boss 유형을 고른다.
2. 이전 층 씬과 그 씬의 도착 ID를 지정한다. 다음 층은 선택 사항이다.
3. **새 층 씬 생성**에서 새 파일 경로를 정한다. 기존 파일은 덮어쓰지 않는다.
4. 맵 배치에서 적/장애물을 편집하고, 맵 연결 관리에서 왕복 연결을 완성한다. 생성 도구는 이전/다음 층을 자동 수정하지 않는다.

- 템플릿을 새 GUID의 씬으로 복사한다. 기존 적·출입구·도착점은 제거하고 공통 우클릭 계단과 `from_previous` / `from_next` 도착점을 만든다. 저장용 건물/적 ID는 새로 준비한다.
- 새 씬은 빌드 목록에 추가한다. 다른 열린 씬이나 미저장 작업을 저장/삭제/닫지 않는다.
- 기존 층의 FarmMapLayout에 DungeonFloor 설정을 추가할 수도 있다. 명시적 저장 버튼으로 저장한다.
- 클리어 규칙은 AllHostiles(일반 적 전멸) / NoCombat(정비층)이다. 보스층도 현재는 배치 적 전멸 규칙을 사용한다. Boss 구분 자체가 보스나 보상을 생성하지 않는다.
- DungeonFloor가 없는 기존 1·2층은 기존 계단 잠금/처치 저장 규칙대로 동작한다. 기존 씬을 강제로 재생성하지 않는다.

## 시작·일시정지 메뉴

- 빌드 첫 씬은 `Assets/Scenes/System/MainMenu.unity`다. Unity에서도 이 씬을 열어 Play하면 타이틀부터 확인할 수 있다. 개별 작업 씬에서 Play하는 편집 테스트 흐름도 유지한다.
- 타이틀: 이어하기 / 새 게임 / 설정 / 종료.
- Esc: 대화·상점·인벤토리·지도가 열려 있으면 기존 창부터 닫는다. 일반 플레이에서는 일시정지, 다시 Esc로 계속한다.
- 일시정지: 계속하기 / 설정 / 저장 후 타이틀 / 저장 후 종료.
- 일시정지는 시간과 게임 입력을 멈춘다. UI를 여닫은 클릭이 다음 프레임에 공격으로 이어지지 않도록 기존 입력 차단을 유지한다.
- 설정: 전체 음량, 창/전체 화면, 1280×720 / 1600×900 / 1920×1080. **적용 및 저장**으로 적용한다. `options.json`을 게임 저장 폴더에 보관하며 새 게임 초기화와 별개다. Unity 기본 Audio 모듈을 활성화했으며 새 배경음/효과음은 추가하지 않았다.

## 저장과 새 게임

- 새 게임은 기존 저장이 있을 때 확인 화면을 거친다. 기존 자동 저장/백업/임시 파일은 같은 저장 폴더의 `PreviousRuns/<고유 폴더>`로 옮긴다. 이동 실패 시 가능한 원상 복구 후 새 게임을 시작하지 않는다.
- 이어하기는 저장 데이터와 대상 씬을 먼저 확인한다. 주 파일이 없어도 정상 백업이 있으면 이어할 수 있다. 불러오기 실패 시 파일을 보존하고 다시 시도할 수 있다.
- 저장 후 타이틀/종료는 저장 성공 후에만 진행한다. 실패하면 현재 플레이를 유지하고 오류를 표시한다. 저장 재시도 성공 시 오류를 해제한다.
- 복원 자체가 막힌 오류에서는 오류 창의 타이틀 버튼으로 돌아갈 수 있다. 자동 저장/여행의 기존 원자적 교체·백업 경로를 재사용한다. 저장 버전은 1이다.

## 코드와 검사

- 사용: `ItemUseEffect`, `FarmGame.ItemUse`. 몬스터: `MonsterType`, `EnemyCombatSystem`.
- 층: `DungeonFloor`, `Editor/TowerFloorWindow`, 기존 `MapExit`.
- 메뉴: `GameMenu`, `GameSession.Menu`. 타이틀 작성/빌드 등록: `Editor/MenuFoundationSetup`.
- `FoundationExpansionCheck.Run`: 격리 프로젝트/저장에서 효과 성공·실패·소모·대기, 종류별 체력/행동, 저장 실패·재시도·백업·새 게임 보관, 층 템플릿·계단·덮어쓰기 방지·미저장 씬 보존 검사.
- `MenuFlowCheck.Run`: 별도 저장의 실제 Play에서 타이틀→새 게임→일시정지→설정 저장/복원→타이틀→이어하기→저장 실패/재시도→새 게임 초기화를 확인한다.
- 기존 `CombatRefactorCheck.Run`으로 기존 전투/충돌/훈련 표적/피격·탄환 동작도 확인했다. 실제 사용자 저장은 검사에 사용하지 않는다.
