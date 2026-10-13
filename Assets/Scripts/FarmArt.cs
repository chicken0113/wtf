using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    // The source sheet is four columns by four rows, read from its top left.
    public sealed class FarmArt
    {
        private readonly Texture2D atlas = Resources.Load<Texture2D>("FarmAtlas");
        private readonly Dictionary<int, Sprite> sprites = new();
        public bool Available => atlas != null;
        public Sprite Get(int index)
        {
            if (atlas == null) return null;
            if (!sprites.TryGetValue(index, out var result))
            {
                int width = atlas.width / 4, height = atlas.height / 4;
                var rect = new Rect(index % 4 * width, (3 - index / 4) * height, width, height);
                // Crop the decorative grass border so neighboring ground tiles meet.
                if(index == 9) rect = new Rect(rect.x+width*.18f,rect.y+height*.18f,width*.64f,height*.64f);
                result = Sprite.Create(atlas, rect, new Vector2(.5f,.5f), rect.width);
                sprites.Add(index,result);
            }
            return result;
        }
        public int ItemIndex(PlantDefinition item)
        {
            if (item.effect == GrowthEffect.Storage) return 8;
            if (item.effect == GrowthEffect.Dig) return 4;
            if (item.effect == GrowthEffect.Produce) return 6;
            return item.name == "Cloud" ? 7 : 5;
        }
        public void Icon(Rect rect, int index)
        {
            if (atlas != null) GUI.DrawTextureWithTexCoords(rect, atlas, new Rect(index % 4 / 4f, (3 - index / 4) / 4f,.25f,.25f));
        }
        public void Dispose()
        {
            foreach (var value in sprites.Values)
                if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
            sprites.Clear();
        }
    }
}
