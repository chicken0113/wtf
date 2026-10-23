using UnityEngine;

namespace EverythingGrows
{
    // A snapshot of existing Resources assets, also injectable into isolated checks.
    public sealed class GameSetupResources
    {
        public DayNightSettings Cycle;
        public PlayerControlSettings Controls;
        public StartingInventory Starting;
        public FarmingLevelSettings FarmingLevels;
        public SaveItemCatalog Catalog;
        public Texture2D Atlas;
        public PlantDefinition Heart;
        public GameObject HouseDoor;
        public PlantDefinition[] Items;
        public static GameSetupResources Load() => new GameSetupResources {
            Cycle = Resources.Load<DayNightSettings>("WorldCycle"),
            Controls = Resources.Load<PlayerControlSettings>("PlayerControls"),
            Starting = Resources.Load<StartingInventory>("StartingInventory"),
            FarmingLevels = Resources.Load<FarmingLevelSettings>("FarmingLevels"),
            Catalog = Resources.Load<SaveItemCatalog>("SaveItemCatalog"),
            Atlas = Resources.Load<Texture2D>("FarmAtlas"),
            Heart = Resources.Load<PlantDefinition>("Equipment/Heart"),
            HouseDoor = Resources.Load<GameObject>("Buildings/HouseDoor"),
            Items = Resources.LoadAll<PlantDefinition>("")
        };
    }
}
