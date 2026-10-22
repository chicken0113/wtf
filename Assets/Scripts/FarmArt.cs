using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    // The source sheet is four columns by four rows, read from its top left.
    public sealed class FarmArt
    {
        internal const int PlantingIcon = 31;
        internal const int FarmingBorderIcon = 32;
        private readonly Texture2D atlas = Resources.Load<Texture2D>("FarmAtlas");
        private readonly Dictionary<int, Sprite> sprites = new();
        private readonly List<Texture2D> generated = new();
        private readonly Dictionary<Sprite,Sprite> buriedSprites = new();
        public bool Available => atlas != null && atlas.width>=4 && atlas.height>=4;
        public Sprite Get(int index)
        {
            if(index>=16) return Procedural(index);
            if (!Available)
            {
                if (!sprites.TryGetValue(index,out var fallback))
                {
                    fallback=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),Vector2.one*.5f,2);
                    sprites.Add(index,fallback);
                }
                return fallback;
            }
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
        public Sprite ItemSprite(PlantDefinition item) => item.itemSprite!=null?item.itemSprite:Get(ItemIndex(item));
        public Sprite BuriedSprite(PlantDefinition item)
        {
            var source=ItemSprite(item); if(source==null) return null;
            if(buriedSprites.TryGetValue(source,out var result)) return result;
            var rect=source.rect; rect.y+=rect.height*.3f; rect.height*=.7f;
            result=Sprite.Create(source.texture,rect,new Vector2(.5f,0),source.pixelsPerUnit);
            buriedSprites[source]=result; return result;
        }
        public void ItemIcon(Rect area,PlantDefinition item)
        {
            var source=ItemSprite(item); if(source==null) return;
            var rect=source.rect; float scale=Mathf.Min(area.width/rect.width,area.height/rect.height);
            var fit=new Rect(area.center.x-rect.width*scale*.5f,area.center.y-rect.height*scale*.5f,rect.width*scale,rect.height*scale);
            GUI.DrawTextureWithTexCoords(fit,source.texture,new Rect(rect.x/source.texture.width,rect.y/source.texture.height,rect.width/source.texture.width,rect.height/source.texture.height));
        }
        public int ItemIndex(PlantDefinition item)
        {
            if(item.iconIndex>=0) return item.iconIndex;
            if (item.effect == GrowthEffect.Storage) return 8;
            if (item.effect == GrowthEffect.Dig) return 22;
            if (item.effect == GrowthEffect.Produce) return 6;
            return item.name == "Cloud" ? 7 : 5;
        }
        public void Icon(Rect rect, int index)
        {
            if(index>=16) { GUI.DrawTexture(rect,Get(index).texture); return; }
            if (Available) GUI.DrawTextureWithTexCoords(rect, atlas, new Rect(index % 4 / 4f, (3 - index / 4) / 4f,.25f,.25f));
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
            else if(index==22) { Box(14,3,4,24,brown); Box(6,23,18,4,metal); Box(5,19,5,7,metal); }
            else if(index==FarmingBorderIcon)
            {
                Box(0,0,32,2,Color.white); Box(0,30,32,2,Color.white);
                Box(0,2,2,28,Color.white); Box(30,2,2,28,Color.white);
            }
            else if(index==PlantingIcon)
            {
                // White sprout silhouette, tinted green/red by the action indicator.
                Box(5,4,22,3,Color.white); Box(14,7,4,14,Color.white);
                Box(7,18,8,4,Color.white); Box(5,22,8,4,Color.white);
                Box(18,21,8,4,Color.white); Box(21,25,7,3,Color.white);
            }
            else if(index==29 || index==30)
            {
                string[] rows={"01100110","11111111","11111111","11111111","01111110","00111100","00011000"};
                for(int y=0;y<rows.Length;y++) for(int x=0;x<8;x++)
                    if(rows[y][x]=='1')
                    {
                        bool edge=x==0 || x==7 || y==0 || y==rows.Length-1 || rows[y][x-1]=='0' || rows[y][x+1]=='0' || rows[y-1][x]=='0' || rows[y+1][x]=='0';
                        if(index==29 || edge) Box(4+x*3,25-y*3,3,3,index==29?new Color(1,.22f,.32f):Color.white);
                    }
            }
            else if(index==28) { Box(16,3,3,22,brown); Box(7,22,13,3,metal); Box(5,19,4,5,metal); Box(3,15,3,5,metal); }
            else if(index==23) { Box(7,4,18,23,new Color(.85f,.71f,.43f)); Box(7,25,18,3,brown); Box(11,10,10,9,new Color(.35f,.6f,.23f)); Box(15,9,2,12,Color.white); }
            else if(index>=24 && index<=27)
            {
                Color fruit=index==24?new Color(.95f,.8f,.4f):index==25?new Color(.65f,.45f,.25f):index==26?new Color(.9f,.2f,.15f):new Color(.92f,.92f,.75f);
                Box(10,7,13,13,fruit); Box(12,5,9,3,fruit); Box(8,10,17,7,fruit); Box(14,20,3,6,new Color(.2f,.5f,.18f)); Box(9,23,8,3,new Color(.3f,.65f,.2f)); Box(17,25,7,3,new Color(.3f,.65f,.2f)); Box(11,14,3,4,fruit*1.15f);
            }
            else { Color body=index==20?new Color(.46f,.76f,.29f):new Color(.7f,.4f,.72f); Box(5,6,22,13,body*.8f); Box(8,19,16,5,body); Box(3,8,26,8,body); Box(10,15,3,4,Color.white); Box(20,15,3,4,Color.white); Box(11,15,2,2,Color.black); Box(20,15,2,2,Color.black); Box(13,9,7,2,Color.black); }
            texture.SetPixels(pixels); texture.Apply(); generated.Add(texture);
            var result=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),32); sprites.Add(index,result); return result;
        }
        public void Dispose()
        {
            foreach (var value in sprites.Values)
                if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
            foreach(var texture in generated) if(Application.isPlaying) Object.Destroy(texture); else Object.DestroyImmediate(texture);
            foreach(var value in buriedSprites.Values) if(Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
            buriedSprites.Clear(); generated.Clear(); sprites.Clear();
        }
    }
}
