# 시작 설정 검사

## Unity에서 실행

Play를 정지하고 **Tools > Everything Grows > 검사 > 프로젝트 설정 검사**을 실행한다.

- 빌드 목록의 활성 씬을 읽고 기본 아이템·시간·저장 ID·NPC·맵 설정을 검사한다.
- 로컬 설정에 오류가 없으면 기존 `WorldConnectionValidator`로 맵 간 도착 ID·충돌·출입구 접근을 검사한다. 이 연결 검사는 현재 월드의 씬 목록을 기준으로 한다.
- 이미 열려 있는 씬은 저장 전 편집 상태를 읽는다. 검사 때문에 연 씬만 닫고 원래 활성 씬을 복원한다.
- 씬·에셋·저장 파일을 수정하거나 누락 데이터를 자동 생성하지 않는다. Play 실행을 자동으로 가로채는 기능은 아니다.
- Console의 성공 표시: `PROJECT_SETUP_VALID`. 경고가 있으면 함께 확인한다.

## 게임 시작 시

`FarmGame.Initialize`는 같은 검사 코드를 먼저 실행한다. 오류가 있으면 플레이어·전투·밭 등 런타임 오브젝트를 만들기 전에 중단하고 `InitializationError`에 원인을 남긴다. 컴포넌트가 비활성화되므로 Update/LateUpdate의 연쇄 오류를 막는다. `IsInitialized`로 준비 상태를 확인한다.

초기화되지 않은 게임의 저장·맵 이동·집 출입 요청은 거부한다. 저장 파일의 새 생성·덮어쓰기를 시작하지 않는다. 목적지 초기화 실패는 기존 맵 이동 복귀 경로를 사용한다. 저장 파일 복원 도중 부분 상태 변경을 모두 되돌리는 트랜잭션은 이번 작업 범위가 아니다.

필수 누락을 수정한 뒤 Play를 다시 시작한다. 사전 검사 단계에서 실패했다면 코드에서 `Initialize`를 다시 호출할 수도 있다. 실제 오브젝트 생성 중 예외가 발생한 경우에는 중복 생성 방지를 위해 같은 인스턴스를 재시도하지 않고 Play 재시작을 안내한다. 원래 예외의 스택도 Console에 남긴다.

## 오류와 경고

오류 형식: `[WTF 설정 코드] 씬 경로 :: 오브젝트 경로 | 필드: 문제. 수정: 해결 방법`

에셋 문제는 에셋 이름·Resources 경로·해당 필드를 표시한다. Console의 컨텍스트가 살아 있는 오브젝트/에셋이면 선택할 수 있다. 검사 후 닫힌 씬의 오브젝트는 메시지에 기록된 씬과 계층 경로로 찾는다.

| 구분 | 예시 | 처리 |
| --- | --- | --- |
| 필수 설정 오류 | `TIME_MISSING`, `DEFINITIONS_MISSING`, `DEFINITION_EMPTY` | 초기화 중단 |
| 기본 아이템 연결 오류 | `DEFINITION_KIND`, `STARTING_MISSING`, `STARTING_ITEM`, `HEART_MISSING`, `HOUSE_DOOR_MISSING` | 초기화 중단 |
| 저장 ID 오류 | `CATALOG_MISSING`, `CATALOG_ENTRY`, `CATALOG_DUPLICATE`, `ITEM_UNREGISTERED` | 저장 상태를 읽거나 덮어쓰기 전에 중단 |
| 잘못된 수치/작물 | `NUMBER_POSITIVE`, `NUMBER_RANGE`, `SEED_RESULT` | 초기화 중단; NaN/무한대도 오류 |
| 씬·NPC 오류 | `WORLD_BOUNDS`, `SPAWN_OUTSIDE`, `NPC_PROFILE`, `NPC_ID`, `SHOP_OFFER`, `EXIT_SCENE`, `ARRIVAL_ID` | 해당 초기화/검사 중단 |
| 조작 설정 없음 | `CONTROLS_DEFAULT` | 기존 이동·회피 기본값 사용 |
| 그림판 없음/너무 작음 | `ART_FALLBACK` | 기본 도형으로 아이템 표시; 게임 규칙은 유지 |
| 비어 있는 배치 표시 | `PICKUP_EMPTY` | 아이템을 생성하지 않고 경고 |
| 시작 수량 0 이하 | `STARTING_QUANTITY` | 기존 규칙대로 1개 지급하며 경고 |

기존 FarmAtlas는 가장자리 여분 픽셀이 있어도 기존 정수 나눗셈 방식으로 읽는다. 4의 배수 크기를 강제하거나 기존 그림을 교체하지 않는다. 대체 그림에서 생성한 Sprite만 해제하며 Unity의 흰색 기본 텍스처는 삭제하지 않는다.

저장 목록 오류를 고칠 때는 **Tools > Everything Grows > 고급 > 저장용 아이템 목록 갱신**를 사용한다. 기존 아이템의 GUID/ID를 임의로 바꾸지 않는다. 기존 에디터 자동 갱신 정책은 유지하며 이 검사 도구 자체는 목록을 갱신하지 않는다.

## 코드 담당

| 파일 | 책임 |
| --- | --- |
| `GameSetupResources.cs` | 기존 Resources 참조 스냅샷 |
| `SetupValidationReport.cs` | 진단 코드·위치·수정 안내·로그 |
| `SetupDataValidator.cs` | 리소스·수치·아이템·저장 목록 검사 |
| `FarmSetupValidator.cs` | 기본 참조·배치·NPC·로컬 씬 연결 검사 |
| `FarmGame.Startup.cs` | 검사 → 생성 → 준비 완료 → 저장 연결 순서 |
| `Editor/ProjectSetupCheck.cs` | 읽기 전용 에디터 검사 메뉴 |
| `Editor/SetupValidationCheck.cs` | 별도 프로젝트에서 누락·오류 주입 검사 |

검사 규칙은 런타임과 에디터가 공유한다. UnityEditor API는 에디터 파일에만 둔다. 독립적인 에디터 규칙 목록을 다시 만들지 않는다.

## 검증

임시 프로젝트에서 `SetupValidationCheck.Run`을 실행한다. 필수 누락·잘못된 ID/수치·씨앗 결과 누락, 부분 생성 차단, 기존 저장 바이트 보존, 수리 후 초기화, 실제 Resources 파일을 잠시 옮긴 시간/그림/조작 설정 누락, 대체 그림으로 심기/성장, 저장 전 씬 편집 보존을 검사한다. 리소스 이동은 finally에서 되돌린다. 본 프로젝트에서 실행할 수 없도록 제한했다.

정상 경로는 `ProjectSetupCheck.Run`, `BasicControlsCheck.Run`, `InteriorWorldCheck.Run` 등 기존 검사를 별도 저장 위치로 실행한다. 실제 사용자 저장으로 실패 사례를 만들지 않는다.
