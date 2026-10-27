using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EverythingGrows
{
    // Scene-local checks shared by startup and the editor. Cross-map arrival checks
    // remain in the existing WorldConnectionValidator editor tool.
    public static class FarmSetupValidator
    {
        public static void Check(FarmGame farm, GameSetupResources data, SetupValidationReport report, bool saving, bool travel)
        {
            var definitions = farm.SetupDefinitions;
            var visited = new HashSet<PlantDefinition>();
            if (definitions == null || definitions.Length < 5)
                report.Error("DEFINITIONS_MISSING", farm, "definitions", "기본 아이템 참조 5개가 필요합니다.", "FarmGame의 Definitions에 기존 기본 아이템을 연결하세요.");
            else for (int i = 0; i < 5; i++)
            {
                if (definitions[i] == null) report.Error("DEFINITION_EMPTY", farm, "definitions[" + i + "]", "기본 아이템이 비어 있습니다.", "해당 칸의 아이템 에셋을 복원하세요.");
                else SetupDataValidator.CheckItem(definitions[i], data.Catalog, report, visited, saving);
            }
            if (definitions != null && definitions.Length >= 5)
            {
                CheckKind(definitions[0], GrowthEffect.Dig, farm, "definitions[0]", report);
                CheckKind(definitions[1], GrowthEffect.Water, farm, "definitions[1]", report);
                CheckKind(definitions[4], GrowthEffect.Storage, farm, "definitions[4]", report);
            }
            SetupDataValidator.Positive(farm, "playerRadius", farm.PlayerRadius, report);
            var layout = farm.Layout != null ? farm.Layout : farm.GetComponentInChildren<FarmMapLayout>();
            foreach(var marker in layout!=null?layout.GetComponentsInChildren<EnemyPlacement>():farm.GetComponentsInChildren<EnemyPlacement>())
                if(!marker.trainingDummy)SetupDataValidator.CheckMonsterType(marker.monsterType,data.Catalog,report,visited,saving);
            if (layout != null)
            {
                var bounds = layout.World;
                if (!Finite(bounds.center) || !Finite(bounds.size) || bounds.width <= 0 || bounds.height <= 0)
                    report.Error("WORLD_BOUNDS", layout, "worldBounds", "맵 범위가 유효하지 않습니다.", "범위 오브젝트의 위치와 0보다 큰 크기를 확인하세요.");
                else if (!Finite(layout.Spawn) || !bounds.Contains(layout.Spawn))
                    report.Error("SPAWN_OUTSIDE", layout, "playerSpawn", "시작 위치가 맵 바깥이거나 유효하지 않습니다.", "시작 위치를 맵 범위 안으로 옮기세요.");
                if (!Finite(layout.Safe.center) || !Finite(layout.Safe.size)) report.Error("SAFE_BOUNDS", layout, "safeArea", "안전 구역에 유효하지 않은 수가 있습니다.", "안전 구역의 위치와 크기를 확인하세요.");
            }
            var forageIds=new HashSet<string>();
            foreach(var placement in farm.GetComponentsInChildren<FarmItemPlacement>())if(placement.forage!=null)
            {
                SetupDataValidator.CheckForage(placement.forage,data.Catalog,report,visited,saving);
                if(!forageIds.Add(MapObjectId.Key(placement)))report.Error("FORAGE_ID_DUPLICATE",placement,"MapObjectId","채집물 저장 ID가 중복됩니다.","고급 > 현재 맵 저장 ID 준비를 실행하세요.");
            }
            foreach (var placement in farm.GetComponentsInChildren<FarmItemPlacement>())
                if(placement.forage!=null)continue;
                else if (placement.item == null) report.Warning("PICKUP_EMPTY", placement, "item", "비어 있는 배치 표시는 아이템을 만들지 않습니다.", "아이템을 연결하거나 불필요한 표시를 제거하세요.");
                else SetupDataValidator.CheckItem(placement.item, data.Catalog, report, visited, saving);
            foreach (var building in farm.GetComponentsInChildren<FarmBuilding>())
                if (building.HarvestItem == null) report.Error("BUILDING_ITEM", building, "item / Buildings/House", "건물을 수확할 아이템이 없습니다.", "건물의 Item 또는 기본 House 에셋을 복원하세요.");
                else SetupDataValidator.CheckItem(building.HarvestItem, data.Catalog, report, visited, saving);
            CheckNpcs(farm, data, report, visited, saving);
            if (!travel) return;
            var arrivals = new HashSet<string>();
            foreach (var root in farm.gameObject.scene.GetRootGameObjects())
            {
                foreach (var point in root.GetComponentsInChildren<MapArrivalPoint>())
                    if (point.isActiveAndEnabled && (string.IsNullOrWhiteSpace(point.arrivalId) || !arrivals.Add(point.arrivalId)))
                        report.Error("ARRIVAL_ID", point, "arrivalId", "도착 ID가 비었거나 중복됩니다.", "이 씬에서 고유한 도착 ID를 지정하세요.");
                foreach (var exit in root.GetComponentsInChildren<MapExit>())
                    if (exit.isActiveAndEnabled)
                    {
                        if (string.IsNullOrWhiteSpace(exit.destinationScene) || SceneUtility.GetBuildIndexByScenePath(exit.destinationScene) < 0)
                            report.Error("EXIT_SCENE", exit, "destinationScene", "대상 씬이 없거나 빌드 목록에서 비활성입니다.", "MapExit의 대상 씬과 Build Settings를 확인하세요.");
                        if (string.IsNullOrWhiteSpace(exit.arrivalId)) report.Error("EXIT_ARRIVAL", exit, "arrivalId", "대상 도착 ID가 비어 있습니다.", "대상 씬 MapArrivalPoint의 ID를 입력하세요.");
                        if (!Finite(exit.areaSize) || exit.areaSize.x <= 0 || exit.areaSize.y <= 0) report.Error("EXIT_SIZE", exit, "areaSize", "출입 범위가 유효하지 않습니다.", "가로·세로를 0보다 큰 수로 설정하세요.");
                    }
            }
        }
        private static bool Finite(Vector2 value) => SetupDataValidator.Finite(value.x) && SetupDataValidator.Finite(value.y);
        private static void CheckKind(PlantDefinition item, GrowthEffect kind, FarmGame farm, string field, SetupValidationReport report)
        {
            if (item != null && item.effect != kind) report.Error("DEFINITION_KIND", farm, field, "기본 아이템 종류는 " + kind + "이어야 합니다.", "기존 기본 아이템 참조 순서를 확인하세요.");
        }
        private static void CheckNpcs(FarmGame farm, GameSetupResources data, SetupValidationReport report, HashSet<PlantDefinition> visited, bool saving)
        {
            var npcs = farm.GetComponentsInChildren<ShopNpc>();
            var manager = farm.GetComponent<NpcManager>();
            if (manager == null || manager.npcs == null)
            {
                if (npcs.Length > 0) report.Error("NPC_MANAGER", farm, "NpcManager.npcs", "NPC에 필요한 프로필 목록이 없습니다.", "같은 FarmGame에 NpcManager와 프로필을 연결하세요.");
                return;
            }
            var ids = new HashSet<string>();
            foreach (var profile in manager.npcs)
            {
                if (profile == null || string.IsNullOrWhiteSpace(profile.id) || !ids.Add(profile.id)) { report.Error("NPC_PROFILE", manager, "npcs.id", "프로필이 비었거나 ID가 중복됩니다.", "고유 ID의 NPC 프로필을 연결하세요."); continue; }
                if (!profile.shopEnabled) continue;
                if (profile.offers == null) { report.Error("SHOP_OFFERS", manager, profile.id + ".offers", "상점 목록이 null입니다.", "판매 목록을 복원하세요. 빈 목록은 허용됩니다."); continue; }
                foreach (var offer in profile.offers)
                    if (offer == null || offer.item == null || offer.buyPrice < 0 || offer.sellPrice < -1)
                        report.Error("SHOP_OFFER", manager, profile.id + ".offers", "상품 참조 또는 가격이 유효하지 않습니다.", "아이템·구매가·판매가를 확인하세요. 판매가 -1은 기본값입니다.");
                    else SetupDataValidator.CheckItem(offer.item, data.Catalog, report, visited, saving);
            }
            foreach (var npc in npcs)
                if (!manager.npcs.Exists(profile => profile != null && profile.id == npc.npcId))
                    report.Error("NPC_ID", npc, "npcId", "같은 ID의 프로필이 없습니다.", "NpcManager의 프로필 ID와 맞추세요.");
        }
    }
}
