using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    // Creates and updates visuals. It does not decide planting or growth rules.
    internal sealed class FarmWorldView
    {
        private sealed class CropVisual { public SpriteRenderer soil, plant; }
        private readonly Transform root;
        private readonly Dictionary<Vector2Int, CropVisual> crops = new();
        public FarmArt Art { get; } = new();
        public Sprite Square { get; }

        public FarmWorldView(Transform root)
        {
            this.root = root;
            Square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * .5f, 2);
        }

        public SpriteRenderer Draw(string label, Vector2 position, Vector2 size, Color color, int order)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(root, false);
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = Square;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        public SpriteRenderer ArtDraw(string label, Vector2 position, Vector2 size, int index, Color fallback, int order)
        {
            var renderer = Draw(label, position, size, fallback, order);
            if (Art.Available) { renderer.sprite = Art.Get(index); renderer.color = Color.white; }
            return renderer;
        }

        public void AddPlot(PlantPlot plot)
        {
            crops.Add(plot.cell, new CropVisual {
                soil = ArtDraw("Tilled soil", plot.cell, Vector2.one, plot.wet ? 11 : 10, new Color(.37f, .23f, .14f), -90)
            });
            RefreshPlant(plot);
        }

        public void RefreshPlant(PlantPlot plot)
        {
            var visual = crops[plot.cell];
            if (plot.plant == null) { RemovePlant(plot.cell); return; }
            if (visual.plant == null) visual.plant = Draw("Growing " + plot.plant.label, plot.cell, Vector2.one, Color.white, 4);
            var item = plot.mature ? plot.HarvestItem : plot.plant;
            var source = Art.ItemSprite(item);
            visual.plant.sprite = plot.mature ? source : Art.BuriedSprite(item);
            visual.plant.color = Color.white;
            float longest = Mathf.Max(source.bounds.size.x, source.bounds.size.y);
            float size = plot.mature ? (item.effect == GrowthEffect.House ? Mathf.Min(1, item.plantedDisplaySize) : item.matureDisplaySize)
                : Mathf.Min(1, plot.plant.plantedDisplaySize) * Mathf.Lerp(.8f, 1, plot.progress);
            visual.plant.transform.localScale = Vector3.one * (size / Mathf.Max(.01f, longest));
            float height = source.bounds.size.y * size / Mathf.Max(.01f, longest);
            visual.plant.transform.position = (Vector2)plot.cell + Vector2.up * (plot.mature ? height * .5f - .18f : -.18f);
        }

        public void RefreshSoil(PlantPlot plot)
        {
            var soil = crops[plot.cell].soil;
            if (Art.Available) soil.sprite = Art.Get(plot.wet ? 11 : 10);
            else if (plot.wet) soil.color = new Color(.18f, .25f, .29f);
        }

        public SpriteRenderer PlantModel(Vector2Int cell) => crops.TryGetValue(cell, out var visual) ? visual.plant : null;
        public void RemovePlant(Vector2Int cell)
        {
            if (!crops.TryGetValue(cell, out var visual) || visual.plant == null) return;
            Remove(visual.plant.gameObject);
            visual.plant = null;
        }

        public void SortPlants()
        {
            foreach (var entry in crops)
                if (entry.Value.plant != null) entry.Value.plant.sortingOrder = 29 - Mathf.RoundToInt(entry.Key.y * 10);
        }

        public void ClearPlots()
        {
            foreach (var visual in crops.Values) { if (visual.plant != null) Remove(visual.plant.gameObject); Remove(visual.soil.gameObject); }
            crops.Clear();
        }

        public void Dispose()
        {
            Art.Dispose();
            if (Square != null) { if (Application.isPlaying) Object.Destroy(Square); else Object.DestroyImmediate(Square); }
        }

        public static void Remove(GameObject obj)
        {
            obj.SetActive(false);
            if (Application.isPlaying) Object.Destroy(obj); else Object.DestroyImmediate(obj);
        }
    }
}
