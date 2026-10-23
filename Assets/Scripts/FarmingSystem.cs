using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    // Owns farming state and decisions. FarmGame supplies inventory/world operations;
    // FarmWorldView only draws the resulting state.
    internal sealed class FarmingSystem
    {
        private readonly FarmGame farm;
        private readonly FarmWorldView visuals;
        private readonly Dictionary<Vector2Int, PlantPlot> plots = new();
        public IEnumerable<PlantPlot> Plots => plots.Values;
        public FarmingSystem(FarmGame farm, FarmWorldView visuals) { this.farm = farm; this.visuals = visuals; }
        public bool TryGetPlot(Vector2Int cell, out PlantPlot plot) => plots.TryGetValue(cell, out plot);
        public bool HasPlot(Vector2Int cell) => plots.ContainsKey(cell);
        public int StorageBonus
        {
            get
            {
                int total = 0;
                foreach (var plot in plots.Values)
                    if (plot.plant != null && plot.mature && plot.plant.Function == PlantFunction.Storage) total += plot.HarvestItem.storageBonus;
                return total;
            }
        }
        public bool Till(Vector2Int cell)
        {
            if(!TillHint(cell).Allowed) return false;
            plots[cell]=new PlantPlot { cell=cell }; visuals.AddPlot(plots[cell]); return true;
        }
        public FarmActionHint TillHint(Vector2Int cell)
        {
            if(!farm.CanFarmAt(cell)) return new(false,"This ground cannot be tilled.");
            if(plots.ContainsKey(cell)) return new(false,"Already tilled.");
            return new(true,"Till soil");
        }
        public FarmActionHint PlantHint(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot)) return new(false,"Till the soil with a hoe first.");
            if(plot.plant!=null) return new(false,"This patch is occupied.");
            var item=farm.SelectedItem;
            if(item==null) return new(false,"Select an item to plant.");
            if(farm.FarmingLevel<item.requiredFarmingLevel) return new(false,"Requires farming level "+item.requiredFarmingLevel+".");
            if(item.transformed && item.effect!=GrowthEffect.House) return new(false,"Already transformed; use this item instead.");
            if(item.isSeed && item.GrowthType!=PlantGrowthType.Functional && item.harvestResult==null) return new(false,"These seeds have no crop configured.");
            if(item.GrowthType==PlantGrowthType.Harvest && item.effect==GrowthEffect.Crop && !item.isSeed && item.harvestResult==null) return new(false,"Plant seeds, not harvested vegetables.");
            return new(true,"Plant "+item.label);
        }
        public FarmActionHint InteractionHint(Vector2Int cell)
        {
            if(!farm.WithinReach(cell)) return new(false,"Walk closer first.");
            if(!plots.TryGetValue(cell,out var plot))
                return farm.CanFarmAt(cell)?PlantHint(cell):new(false,"This ground cannot be planted.");
            if(plot.plant==null) return PlantHint(cell);
            if(plot.wilted) return new(false,"Use a hoe to remove this plant.");
            if(!plot.mature) return new(false,plot.wet?"Still growing.":"Water this plant first.");
            if(!plot.CanHarvest) return new(false,"Producing; wait for the next harvest.");
            return new(true,"Harvest "+plot.HarvestItem.label);
        }
        public FarmActionHint ToolHint(Vector2Int cell, GrowthEffect tool)
        {
            if(tool==GrowthEffect.Dig)
                return plots.TryGetValue(cell,out var occupied) && occupied.plant!=null?new(true,"Remove planted object (no refund)"):TillHint(cell);
            if(tool==GrowthEffect.Water)
                return plots.ContainsKey(cell)?new(true,"Water soil"):new(false,"Till the soil before watering.");
            if(tool==GrowthEffect.Scythe)
                return new(false,"Use a hoe to remove planted objects.");
            return new(false,"");
        }
        public bool PlantSelected(Vector2Int cell)
        {
            var hint=PlantHint(cell);
            if(!hint.Allowed) { farm.Notify(hint.Text); return false; }
            var plot=plots[cell];
            plot.wilted=plot.regrowing=false;
            plot.plant=farm.SelectedItem;
            visuals.RefreshPlant(plot);
            farm.RemoveInventoryItem(farm.SelectedIndex); farm.Notify("Planted. Water it and see what happens."); return true;
        }
        public int Water(Vector2 point,float radius)
        {
            int count=0; foreach(var plot in plots.Values) if(Vector2.Distance(point,plot.cell)<=radius)
            { plot.wet=true; visuals.RefreshSoil(plot); count++; } return count;
        }
        public void Tick(float seconds)
        {
            if(seconds<=0 || (farm.Combat!=null && farm.Combat.IsGameOver)) return;
            var snapshot=new List<PlantPlot>(plots.Values);
            foreach(var plot in snapshot)
            {
                if(plot.plant==null || plot.wilted) continue;
                float activeSeconds=seconds;
                if(!plot.mature && plot.wet)
                {
                    float duration=Mathf.Max(.1f,plot.regrowing?plot.plant.regrowSeconds:plot.plant.growthSeconds);
                    float remaining=(1-plot.progress)*duration;
                    activeSeconds=Mathf.Max(0,seconds-remaining);
                    plot.progress=Mathf.Min(1,plot.progress+seconds/duration);
                    if(plot.progress>=1) { plot.mature=true; farm.Notify(plot.plant.label+" grew into a new function."); }
                    visuals.RefreshPlant(plot);
                }
                if(!plot.mature) continue;
                if(plot.plant.GrowthType==PlantGrowthType.Producer && !plot.plant.UsesRegrowth)
                {
                    plot.production=Mathf.Min(plot.plant.productionSeconds,plot.production+activeSeconds);
                    if(plot.production>=plot.plant.productionSeconds)plot.ready=true;
                }
                else if(plot.plant.GrowthType==PlantGrowthType.Functional)
                {
                    if(plot.plant.Function==PlantFunction.Storage || plot.plant.Function==PlantFunction.None)continue;
                    float interval=plot.plant.FunctionInterval;
                    plot.production+=activeSeconds;
                    if(interval>0 && plot.production<interval)continue;
                    plot.production=interval>0?plot.production%interval:0;
                    switch(plot.plant.Function)
                    {
                        case PlantFunction.Till:
                            Till(plot.cell+Vector2Int.left); Till(plot.cell+Vector2Int.right); Till(plot.cell+Vector2Int.up); Till(plot.cell+Vector2Int.down); break;
                        case PlantFunction.Water: Water(plot.cell,plot.plant.effectRadius); break;
                    }
                }
            }
        }
        public bool InteractPlot(Vector2Int cell)
        {
            var hint=InteractionHint(cell);
            if(!hint.Allowed) { farm.Notify(hint.Text); return false; }
            if(plots.TryGetValue(cell,out var plot) && plot.plant!=null)
            {
                return Collect(cell);
            }
            return PlantSelected(cell);
        }
        public bool Collect(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot) || !plot.CanHarvest)return false;
            var planted=plot.plant;
            if(planted.GrowthType!=PlantGrowthType.Producer)return Uproot(cell);
            if(!farm.ReceiveHarvest(plot.HarvestItem,planted.harvestQuantity))return false;
            if(planted.UsesRegrowth)
            {
                plot.mature=false;plot.regrowing=true;plot.progress=0;plot.wet=false;
                visuals.RefreshPlant(plot);visuals.RefreshSoil(plot);
                farm.Notify("Harvested. Water again for the next harvest.");
            }
            else
            {
                plot.ready=false;plot.production=0;farm.Notify("Collected a new "+plot.HarvestItem.label+".");
            }
            farm.RecordFarmHarvest(planted);return true;
        }
        public bool Uproot(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot) || plot.plant==null || !plot.mature || plot.wilted) return false;
            var item=plot.HarvestItem;var planted=plot.plant;
            ClearPlant(plot);
            int quantity=planted.GrowthType==PlantGrowthType.Harvest?planted.harvestQuantity:1;
            if(!farm.ReceiveHarvest(item,quantity))return false;
            farm.RecordFarmHarvest(planted);return true;
        }
        public bool UseHoe(Vector2Int cell)
        {
            if(plots.TryGetValue(cell,out var plot) && plot.plant!=null)
            {
                ClearPlant(plot);farm.Notify("Removed planted object. No item returned.");return true;
            }
            return Till(cell);
        }
        private void ClearPlant(PlantPlot plot)
        {
            plot.plant=null;plot.mature=plot.ready=plot.regrowing=plot.wilted=false;
            plot.progress=plot.production=0;
            visuals.RemovePlant(plot.cell);farm.ReconcileInventoryCapacity();
        }
        public bool ClearDeadCrop(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot) || !plot.wilted) return false;
            ClearPlant(plot);return true;
        }
        public List<PlotSave> CapturePlots(SaveItemCatalog catalog)
        {
            var saved = new List<PlotSave>();
            foreach (var plot in plots.Values)
                saved.Add(new PlotSave {cell=plot.cell,item=catalog.Id(plot.plant),wet=plot.wet,mature=plot.mature,ready=plot.ready,wilted=plot.wilted,regrowing=plot.regrowing,progress=plot.progress,production=plot.production});
            return saved;
        }
        public void RestorePlots(List<PlotSave> saved, SaveItemCatalog catalog)
        {
            var restored = new List<PlantPlot>();
            foreach (var plot in saved)
                restored.Add(new PlantPlot {cell=plot.cell,plant=catalog.Resolve(plot.item),wet=plot.wet,mature=plot.mature,ready=plot.ready,wilted=plot.wilted,regrowing=plot.regrowing,progress=Mathf.Clamp01(plot.progress),production=Mathf.Max(0,plot.production)});
            visuals.ClearPlots();
            plots.Clear();
            foreach (var plot in restored) { plots.Add(plot.cell, plot); visuals.AddPlot(plot); }
        }
    }
}
