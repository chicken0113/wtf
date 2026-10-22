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
                    if (plot.plant != null && plot.mature && plot.plant.effect == GrowthEffect.Storage) total += plot.HarvestItem.storageBonus;
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
            if(item.transformed && item.effect!=GrowthEffect.House) return new(false,"Already transformed; use this item instead.");
            if(item.isSeed && item.harvestResult==null) return new(false,"These seeds have no crop configured.");
            if(item.effect==GrowthEffect.Crop && !item.isSeed && item.harvestResult==null) return new(false,"Plant seeds, not harvested vegetables.");
            return new(true,"Plant "+item.label);
        }
        public FarmActionHint InteractionHint(Vector2Int cell)
        {
            if(!farm.WithinReach(cell)) return new(false,"Walk closer first.");
            if(!plots.TryGetValue(cell,out var plot))
                return farm.CanFarmAt(cell)?PlantHint(cell):new(false,"This ground cannot be planted.");
            if(plot.plant==null) return PlantHint(cell);
            if(plot.wilted) return new(false,"Use a scythe to clear the dead crop.");
            if(!plot.mature) return new(false,plot.wet?"Still growing.":"Water this plant first.");
            if(!plot.CanHarvest) return new(false,"Producing; wait for the next harvest.");
            return new(true,"Harvest "+plot.HarvestItem.label);
        }
        public FarmActionHint ToolHint(Vector2Int cell, GrowthEffect tool)
        {
            if(tool==GrowthEffect.Dig) return TillHint(cell);
            if(tool==GrowthEffect.Water)
                return plots.ContainsKey(cell)?new(true,"Water soil"):new(false,"Till the soil before watering.");
            if(tool==GrowthEffect.Scythe)
                return plots.TryGetValue(cell,out var plot) && plot.wilted?new(true,"Clear dead crop"):new(false,"No dead crop to clear.");
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
                switch(plot.plant.effect)
                {
                    case GrowthEffect.Dig:
                        Till(plot.cell+Vector2Int.left); Till(plot.cell+Vector2Int.right); Till(plot.cell+Vector2Int.up); Till(plot.cell+Vector2Int.down); break;
                    case GrowthEffect.Water: Water(plot.cell,plot.plant.effectRadius); break;
                    case GrowthEffect.Produce:
                        plot.production+=activeSeconds; if(plot.production>=plot.plant.productionSeconds) plot.ready=true; break;
                }
            }
        }
        public bool InteractPlot(Vector2Int cell)
        {
            var hint=InteractionHint(cell);
            if(!hint.Allowed) { farm.Notify(hint.Text); return false; }
            if(plots.TryGetValue(cell,out var plot) && plot.plant!=null)
            {
                if(plot.plant.effect==GrowthEffect.Produce || plot.plant.harvestResult!=null) return Collect(cell);
                return Uproot(cell);
            }
            return PlantSelected(cell);
        }
        public bool Collect(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot)) return false;
            if(plot.mature && plot.plant!=null && plot.plant.harvestResult!=null)
            {
                if(plot.plant.isSeed && plot.plant.regrowSeconds>0)
                {
                    if(!farm.ReceiveHarvest(plot.plant.harvestResult)) return false;
                    plot.mature=false; plot.regrowing=true; plot.progress=0; plot.wet=false;
                    visuals.RefreshPlant(plot);
                    visuals.RefreshSoil(plot);
                    farm.Notify("Harvested. Water again for the next harvest."); return true;
                }
                return Uproot(cell);
            }
            if(!plot.ready || !farm.ReceiveHarvest(plot.plant)) return false;
            plot.ready=false; plot.production=0; farm.Notify("Collected a new "+plot.plant.label+"."); return true;
        }
        public bool Uproot(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot) || plot.plant==null) return false;
            var item=plot.mature?plot.HarvestItem:plot.plant;
            plot.plant=null; plot.mature=false; plot.ready=false; plot.progress=plot.production=0; plot.regrowing=plot.wilted=false;
            visuals.RemovePlant(cell); farm.ReconcileInventoryCapacity(); return farm.ReceiveHarvest(item);
        }
        public bool ClearDeadCrop(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot) || !plot.wilted) return false;
            visuals.RemovePlant(cell); plot.plant=null; plot.wilted=plot.regrowing=plot.mature=false; plot.progress=0; return true;
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
