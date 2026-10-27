# 사망 후 병원 복귀

## 동작

- 피격으로 체력이 0이 되면 `HospitalInterior`의 `hospital_recovery` 도착점으로 이동한다.
- 체력은 최대 체력의 절반이 아니라 **하트 반 칸**이다. 떼어낸 하트 슬롯 수는 그대로 유지한다.
- 골드와 소지품은 잃지 않는다. 손실 수치는 이후 결정한다.
- 기존 맵 이동 자동 저장을 재사용한다. 출발 맵의 처치/클리어/드롭 상태와 소지품을 저장하고 병원에 도착하면 위치와 반 하트 체력을 다시 자동 저장한다.
- 병원에서는 안전 구역 자동 회복을 끈다. 다른 맵의 기존 회복 규칙은 유지한다.
- 최종 HP 슬롯을 직접 수확해 발생하는 GameOver는 기존 규칙을 유지한다.
- 현재 싱글플레이 처리다. 멀티 부활이나 아이템 손실, 새 보상 규칙/진행 UI는 추가하지 않았다.

## 편집 위치

| 항목 | 위치 |
| --- | --- |
| 병원 내부 | `Assets/Scenes/LegacyVillage/HospitalInterior.unity` |
| 사망 복귀 위치 | `EditableMap/Travel/Arrival - hospital_recovery` |
| 정상 출입 위치 | `EditableMap/Travel/Arrival - hospital_entry` |
| 마을 병원 문 | Town의 `Clinic exterior/Hospital entrance` |
| 마을로 돌아오는 위치 | Town의 `Arrival - town_from_hospital` |
| 반 하트 계산/피격 | `PlayerCombatState.Damage / HalfHeartHealth` |
| 병원 목적지 | `GameSession.Recovery.cs`의 상수 |
| 안전 구역 자동 회복 | `FarmMapLayout.passiveHealthRecovery` |

병원은 기존 실내 작성 코드와 타일/기본 도형을 재사용한 간단한 편집 가능 씬이다. 침대·대기 의자·의약품 수납장을 배치했다. 마을과 병원은 우클릭 문으로 왕복하며 NPC 진료/새 판매 기능은 없다.

## 작성과 검사

`Tools > Everything Grows > World > Add hospital recovery room`은 병원이 없을 때만 내부를 만들고 기존 마을의 병원 건물에 문을 추가한다. Play 중이거나 저장하지 않은 씬이 있으면 작업하지 않으며 재실행해도 사용자의 배치를 다시 만들지 않는다. 실행 시 맵 생성은 없다.

`HospitalRecoveryCheck.Prepare`는 격리된 프로젝트에서 작성·설정/연결·접근 동선·재적용 시 배치 보존을 검사한다. `Run`과 `Resume`는 별도 테스트 저장으로 실제 사망→병원 도착→우클릭 출입→던전 재방문→재실행을 검사한다. 소지품/골드/하트 슬롯, 반 하트 유지, 일부 처치/클리어와 드롭, 자동 저장을 확인한다. `HeartHarvestCheck`는 최종 하트 GameOver가 유지되는지 검사한다.

병원 이동도 기존 저장/목적지 검증을 따른다. 실패 시 기존 오류 표시와 병원 이동 실패 안내를 사용하며 골드·아이템을 차감하지 않는다. 실제 사용자 저장으로 검사를 실행하지 않는다.
