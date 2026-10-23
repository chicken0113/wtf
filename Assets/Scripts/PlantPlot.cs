using UnityEngine;

namespace EverythingGrows
{
    // Runtime crop state. Rendering and scene objects belong to FarmWorldView.
    internal sealed class PlantPlot
    {
        public Vector2Int cell;
        public PlantDefinition plant;
        public bool wet, mature, ready, wilted, regrowing;
        public float progress, production;
        public bool CanHarvest => plant != null && !wilted && mature && (plant.GrowthType != PlantGrowthType.Producer || plant.UsesRegrowth || ready);
        public PlantDefinition HarvestItem => plant != null ? plant.HarvestItem : null;
    }
}
