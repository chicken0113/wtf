using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    // The source sheet is four columns by four rows, read from its top left.
    public sealed class FarmArt
    {
        private readonly Texture2D atlas = Resources.Load<Texture2D>("FarmAtlas");
        private readonly Dictionary<int, Sprite> sprites = new();
        private readonly List<Texture2D> generated = new();
        public bool Available => atlas != null;
        public Sprite Get(int index)
        {
            if(index>=16) return Procedural(index);
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
            if(item.iconIndex>=0) return item.iconIndex;
            if (item.effect == GrowthEffect.Storage) return 8;
            if (item.effect == GrowthEffect.Dig) return 4;
            if (item.effect == GrowthEffect.Produce) return 6;
            return item.name == "Cloud" ? 7 : 5;
        }
        public void Icon(Rect rect, int index)
        {
            if(index>=16) { GUI.DrawTexture(rect,Get(index).texture); return; }
            if (atlas != null) GUI.DrawTextureWithTexCoords(rect, atlas, new Rect(index % 4 / 4f, (3 - index / 4) / 4f,.25f,.25f));
        }
        private Sprite Procedural(int index)
        {
            if(sprites.TryGetValue(index,out var cached)) return cached;
            var texture=new Texture2D(32,32,TextureFormat.RGBA32,false) { filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp };
            var pixels=new Color[32*32];
            void Box(int x,int y,int w,int h,Color c) { for(int a=x;a<x+w;a++) for(int b=y;b<y+h;b++) if(a>=0 && a<32 && b>=0 && b<32) pixels[b*32+a]=c; }
            var brown=new Color(.42f,.23f,.12f); var metal=new Color(.78f,.84f,.87f); var gold=new Color(1,.8f,.25f);
            if(index==16 || index==17) { Box(14,3,4,9,brown); Box(8,11,16,3,index==17?gold:metal); Box(13,14,6,14,index==17?new Color(.65f,.92f,1):metal); Box(15,28,2,2,Color.white); }
            else if(index==18) { Box(21,4,2,24,metal); Box(9,7,3,18,brown); Box(12,4,5,3,brown); Box(12,25,5,3,brown); Box(17,7,3,18,brown); Box(5,15,24,2,gold); }
            else if(index==19) { Box(8,9,5,12,brown); Box(8,19,19,6,metal); Box(25,20,5,3,Color.gray); Box(15,13,7,3,Color.gray); }
            else { Color body=index==20?new Color(.46f,.76f,.29f):new Color(.7f,.4f,.72f); Box(5,6,22,13,body*.8f); Box(8,19,16,5,body); Box(3,8,26,8,body); Box(10,15,3,4,Color.white); Box(20,15,3,4,Color.white); Box(11,15,2,2,Color.black); Box(20,15,2,2,Color.black); Box(13,9,7,2,Color.black); }
            texture.SetPixels(pixels); texture.Apply(); generated.Add(texture);
            var result=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),32); sprites.Add(index,result); return result;
        }
        public void Dispose()
        {
            foreach (var value in sprites.Values)
                if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
            foreach(var texture in generated) if(Application.isPlaying) Object.Destroy(texture); else Object.DestroyImmediate(texture);
            generated.Clear(); sprites.Clear();
        }
    }
}
