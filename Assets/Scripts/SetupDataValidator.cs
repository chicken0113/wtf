using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    public static class SetupDataValidator
    {
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static void CheckResources(GameSetupResources data, SetupValidationReport report, bool startingItems, bool saving)
        {
            CheckCycle(data.Cycle, report);
            if(data.FarmingLevels==null || data.FarmingLevels.experienceToNextLevel==null)
                report.Error("FARMING_LEVELS_MISSING",data.FarmingLevels,"Resources/FarmingLevels.experienceToNextLevel","농사 레벨 설정이 없습니다.","FarmingLevels.asset과 필요 경험치 목록을 복원하세요.");
            else for(int i=0;i<data.FarmingLevels.experienceToNextLevel.Length;i++)
                if(data.FarmingLevels.experienceToNextLevel[i]<1)
                    report.Error("FARMING_LEVEL_XP",data.FarmingLevels,"experienceToNextLevel["+i+"]","레벨 필요 경험치는 양수여야 합니다.","1 이상의 값을 입력하세요.");
            if (data.Controls == null) report.Warning("CONTROLS_DEFAULT", null, "Resources/PlayerControls", "기본 이동·회피 수치를 사용합니다.", "직접 조절하려면 PlayerControls.asset을 복원하세요.");
            else
            {
                Positive(data.Controls, "moveSpeed", data.Controls.moveSpeed, report);
                Positive(data.Controls, "rollSpeed", data.Controls.rollSpeed, report);
                Positive(data.Controls, "rollDuration", data.Controls.rollDuration, report);
                Positive(data.Controls, "projectileSpeed", data.Controls.projectileSpeed, report);
                Positive(data.Controls, "toolDragInterval", data.Controls.toolDragInterval, report);
                Range(data.Controls, "rollCooldown", data.Controls.rollCooldown, 0, float.MaxValue, report);
                Range(data.Controls, "rollInputBuffer", data.Controls.rollInputBuffer, 0, .3f, report);
                Range(data.Controls, "weaponRecoil", data.Controls.weaponRecoil, 0, .3f, report);
                Range(data.Controls, "cameraShake", data.Controls.cameraShake, 0, .2f, report);
            }
            if (data.Atlas == null || data.Atlas.width < 4 || data.Atlas.height < 4)
                report.Warning("ART_FALLBACK", data.Atlas, "Resources/FarmAtlas", "그림판이 없거나 너무 작아 기본 도형으로 표시합니다.", "가로·세로가 4픽셀 이상인 기존 FarmAtlas 그림을 복원하세요.");
            if (data.Heart == null || data.Heart.effect != GrowthEffect.Heart)
                report.Error("HEART_MISSING", data.Heart, "Resources/Equipment/Heart", "하트 수확·복구용 데이터가 없거나 종류가 다릅니다.", "Heart.asset의 연결과 Effect를 확인하세요.");
            if (data.HouseDoor == null || data.HouseDoor.GetComponent<MapExit>() == null || data.HouseDoor.GetComponent<HouseInteriorLink>() == null)
                report.Error("HOUSE_DOOR_MISSING", data.HouseDoor, "Resources/Buildings/HouseDoor", "집 출입 프리팹 또는 필수 컴포넌트가 없습니다.", "HouseDoor.prefab의 MapExit와 HouseInteriorLink를 복원하세요.");
            CheckCatalog(data.Catalog, report, saving);
            var visited = new HashSet<PlantDefinition>();
            if (data.Items != null) foreach (var item in data.Items) CheckItem(item, data.Catalog, report, visited, saving);
            if (data.Catalog != null && data.Catalog.entries != null)
                foreach (var entry in data.Catalog.entries) if (entry != null && entry.item != null) CheckItem(entry.item, data.Catalog, report, visited, saving);
            if(data.MeleeMonster==null || data.RangedMonster==null)
                report.Error("MONSTER_DEFAULTS",null,"Resources/Monsters/Chaser / Spitter","기본 몬스터 종류 설정이 없습니다.","Chaser.asset과 Spitter.asset을 복원하세요.");
            CheckMonsterType(data.MeleeMonster,data.Catalog,report,visited,saving);
            CheckMonsterType(data.RangedMonster,data.Catalog,report,visited,saving);
            if(data.MonsterTypes!=null)foreach(var type in data.MonsterTypes)CheckMonsterType(type,data.Catalog,report,visited,saving);
            if (!startingItems) return;
            if (data.Starting == null || data.Starting.items == null || data.Starting.items.Length == 0)
                report.Error("STARTING_MISSING", data.Starting, "Resources/StartingInventory.items", "기본 지급 아이템 목록이 없습니다.", "StartingInventory.asset에 시작 아이템을 연결하세요.");
            else for (int i = 0; i < data.Starting.items.Length; i++)
            {
                var entry = data.Starting.items[i];
                if (entry == null || entry.item == null) report.Error("STARTING_ITEM", data.Starting, "items[" + i + "]", "기본 지급 아이템이 비어 있습니다.", "아이템을 연결하거나 사용하지 않는 행을 제거하세요.");
                else
                {
                    CheckItem(entry.item, data.Catalog, report, visited, saving);
                    if (entry.quantity < 1) report.Warning("STARTING_QUANTITY", data.Starting, "items[" + i + "].quantity", "기존 규칙에 따라 1개를 지급합니다.", "원하는 수량을 1 이상으로 설정하세요.");
                }
            }
        }

        public static void CheckCycle(DayNightSettings cycle, SetupValidationReport report)
        {
            if (cycle == null) report.Error("TIME_MISSING", null, "Resources/WorldCycle", "필수 시간 설정이 없습니다.", "Assets/Resources/WorldCycle.asset을 복원하세요.");
            else
            {
                Positive(cycle, "daylightSeconds", cycle.daylightSeconds, report);
                Positive(cycle, "duskSeconds", cycle.duskSeconds, report);
                Positive(cycle, "nightSeconds", cycle.nightSeconds, report);
                Positive(cycle, "dawnSeconds", cycle.dawnSeconds, report);
                Range(cycle, "nightDarkness", cycle.nightDarkness, 0, .9f, report);
            }
        }

        public static void CheckCatalog(SaveItemCatalog catalog, SetupValidationReport report, bool required = true)
        {
            if (catalog == null)
            {
                if (required) report.Error("CATALOG_MISSING", null, "Resources/SaveItemCatalog", "저장용 아이템 목록이 없습니다.", "Tools > Everything Grows > 고급 > 저장용 아이템 목록 갱신를 실행하세요.");
                return;
            }
            if (catalog.entries == null) { report.Error("CATALOG_ENTRIES", catalog, "entries", "아이템 목록이 null입니다.", "고급 > 저장용 아이템 목록 갱신를 실행하세요."); return; }
            var ids = new HashSet<string>();
            var items = new HashSet<PlantDefinition>();
            for (int i = 0; i < catalog.entries.Length; i++)
            {
                var entry = catalog.entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.item == null)
                    report.Error("CATALOG_ENTRY", catalog, "entries[" + i + "]", "아이템 또는 저장 ID가 비어 있습니다.", "고급 > 저장용 아이템 목록 갱신를 실행하세요. 기존 ID를 임의로 바꾸지 마세요.");
                else if (!ids.Add(entry.id) || !items.Add(entry.item))
                    report.Error("CATALOG_DUPLICATE", catalog, "entries[" + i + "]", "중복 저장 ID 또는 중복 아이템입니다.", "원래 ID를 보존하며 목록을 갱신하세요.");
            }
        }

        public static void CheckItem(PlantDefinition item, SaveItemCatalog catalog, SetupValidationReport report, HashSet<PlantDefinition> visited, bool saving)
        {
            if (item == null || !visited.Add(item)) return;
            if (saving && catalog != null && catalog.entries != null && !System.Array.Exists(catalog.entries, entry => entry != null && entry.item == item && !string.IsNullOrWhiteSpace(entry.id)))
                report.Error("ITEM_UNREGISTERED", item, "SaveItemCatalog", "이 아이템의 저장 ID가 없습니다.", "고급 > 저장용 아이템 목록 갱신를 실행하세요.");
            if(item.requiredFarmingLevel<1 || item.harvestExperience<0)
                report.Error("ITEM_FARMING_LEVEL",item,"requiredFarmingLevel / harvestExperience","농사 레벨 또는 경험치 설정이 잘못됐습니다.","필요 레벨은 1 이상, 수확 경험치는 0 이상으로 설정하세요.");
            Positive(item, "growthSeconds", item.growthSeconds, report);
            Positive(item, "plantedDisplaySize", item.plantedDisplaySize, report);
            Positive(item, "matureDisplaySize", item.matureDisplaySize, report);
            Range(item, "regrowSeconds", item.regrowSeconds, 0, float.MaxValue, report);
            if (item.sellPrice < 0 || item.storageBonus < 0) report.Error("ITEM_VALUE", item, "sellPrice / storageBonus", "음수 설정입니다.", "0 이상의 값으로 설정하세요.");
            if (item.isSeed && item.GrowthType!=PlantGrowthType.Functional && item.harvestResult == null) report.Error("SEED_RESULT", item, "harvestResult", "씨앗의 수확 결과가 없습니다.", "자랄 작물을 연결하세요.");
            if(item.useEffect!=null)Positive(item,"attackInterval",item.attackInterval,report);
            if (item.effect == GrowthEffect.Weapon) { Positive(item, "damage", item.damage, report); Positive(item, "attackRange", item.attackRange, report); Positive(item, "attackInterval", item.attackInterval, report); }
            if (item.effect == GrowthEffect.Water) Positive(item, "effectRadius", item.effectRadius, report);
            if(!System.Enum.IsDefined(typeof(PlantGrowthType),item.growthType) || !System.Enum.IsDefined(typeof(PlantFunction),item.growthFunction))
                report.Error("GROWTH_TYPE",item,"growthType / growthFunction","알 수 없는 성장 유형 또는 기능입니다.","Inspector에서 성장 유형과 기능을 다시 선택하세요.");
            if(item.harvestQuantity<1)report.Error("HARVEST_QUANTITY",item,"harvestQuantity","수확 수량은 1 이상이어야 합니다.","1 이상의 수량을 설정하세요.");
            if(item.GrowthType==PlantGrowthType.Producer && !item.UsesRegrowth) Positive(item,"productionSeconds",item.productionSeconds,report);
            if(item.GrowthType==PlantGrowthType.Functional)
            {
                Range(item,"functionSeconds",item.FunctionInterval,0,float.MaxValue,report);
                if(item.Function==PlantFunction.Water)Positive(item,"effectRadius",item.effectRadius,report);
                if(item.Function==PlantFunction.None)report.Error("PLANT_FUNCTION",item,"growthFunction","기능형의 기능이 없습니다.","경작/물주기/보관 중 하나를 선택하세요.");
            }
            CheckItem(item.harvestResult, catalog, report, visited, saving);
        }
        public static void CheckMonsterType(MonsterType type,SaveItemCatalog catalog,SetupValidationReport report,HashSet<PlantDefinition> visited,bool saving)
        {
            if(type==null)return;
            if(!System.Enum.IsDefined(typeof(MonsterBehavior),type.behavior) || type.attackHalfHearts<1)report.Error("MONSTER_BEHAVIOR",type,"behavior / attackHalfHearts","몬스터 행동/피해 수치가 유효하지 않습니다.","행동과 반 하트 단위 피해를 확인하세요.");
            foreach(var pair in new[]{("maxHealth",type.maxHealth),("moveSpeed",type.moveSpeed),("returnSpeed",type.returnSpeed),("detectionRange",type.detectionRange),("attackRange",type.attackRange),("meleeHitRange",type.meleeHitRange),("attackCooldown",type.attackCooldown),("projectileSpeed",type.projectileSpeed),("projectileLifetime",type.projectileLifetime)})Positive(type,pair.Item1,pair.Item2,report);
            Range(type,"windupSeconds",type.windupSeconds,0,float.MaxValue,report);
            if(type.gold<0 || type.drops==null)
            { report.Error("MONSTER_REWARDS",type,"gold / drops","골드가 음수이거나 드롭 목록이 없습니다.","골드를 0 이상으로, 드롭 목록을 유효한 배열로 설정하세요. 아이템을 주지 않으려면 목록을 비우세요.");return; }
            for(int i=0;i<type.drops.Length;i++)
            {
                var drop=type.drops[i];string field="drops["+i+"]";
                if(drop==null || drop.item==null) { report.Error("MONSTER_DROP_ITEM",type,field,"드롭 아이템이 비어 있습니다.","아이템을 연결하거나 행을 제거하세요.");continue; }
                Range(type,field+".probability",drop.probability,0,1,report);
                if(drop.minimum<1 || drop.maximum<drop.minimum)report.Error("MONSTER_DROP_AMOUNT",type,field+".minimum / maximum","드롭 수량 범위가 잘못됐습니다.","최소 1 이상, 최대는 최소 이상으로 설정하세요.");
                CheckItem(drop.item,catalog,report,visited,saving);
            }
        }
        public static void Positive(Object context, string field, float value, SetupValidationReport report)
        {
            if (!Finite(value) || value <= 0) report.Error("NUMBER_POSITIVE", context, field, "0 이하이거나 유한한 수가 아닙니다.", "0보다 큰 유한한 수를 입력하세요.");
        }
        public static void Range(Object context, string field, float value, float min, float max, SetupValidationReport report)
        {
            if (!Finite(value) || value < min || value > max) report.Error("NUMBER_RANGE", context, field, "허용 범위를 벗어났습니다.", min + " ~ " + max + " 범위의 유한한 수를 입력하세요.");
        }
    }
}
