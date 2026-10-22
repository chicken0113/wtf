using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    // Rendering resources and temporary feedback only; no damage or reward decisions.
    internal sealed class CombatEffects
    {
        private sealed class Flash { public SpriteRenderer model; public float time,duration; public Color tint; public Vector2 velocity; }
        private readonly Transform root;
        private readonly Sprite square;
        private readonly List<Flash> flashes = new();
        public FarmArt Art { get; } = new();
        public CombatEffects(Transform root)
        {
            this.root = root;
            square = Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),Vector2.one*.5f,2);
        }
        public SpriteRenderer Draw(string name,Vector2 point,Vector2 size,Color color,int icon=-1)
        {
            var obj=new GameObject(name); obj.transform.SetParent(root,false); obj.transform.position=point; obj.transform.localScale=new Vector3(size.x,size.y,1);
            var renderer=obj.AddComponent<SpriteRenderer>(); renderer.sprite=icon<0?square:Art.Get(icon); renderer.color=color; renderer.sortingOrder=50; return renderer;
        }
        private SpriteRenderer Effect(string name,Vector2 point,Vector2 size,Color tint,float duration,Vector2 velocity=default)
        {
            var model=Draw(name,point,size,tint); model.sortingOrder=35-Mathf.RoundToInt(point.y*10);
            flashes.Add(new Flash {model=model,time=duration,duration=duration,tint=tint,velocity=velocity}); return model;
        }
        public void Afterimage(SpriteRenderer source)
        {
            var tint=source.color; tint.a=.23f;
            var model=Effect("Dodge trail",source.transform.position,source.transform.lossyScale,tint,.12f);
            model.sprite=source.sprite;model.flipX=source.flipX;model.flipY=source.flipY;
            model.transform.rotation=source.transform.rotation;model.sortingOrder=source.sortingOrder-1;
        }
        public void Sparks(Vector2 point,Color tint)
        {
            for(int i=0;i<5;i++)
            {
                float angle=i*Mathf.PI*2/5;
                Vector2 velocity=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*1.8f;
                Effect("Impact spark",point,Vector2.one*.09f,tint,.14f,velocity);
            }
        }
        public void ToolEffect(Vector2 point,GrowthEffect kind)
        {
            Sparks(point,kind==GrowthEffect.Water?new Color(.35f,.75f,1):kind==GrowthEffect.Scythe?new Color(.6f,.85f,.3f):new Color(.6f,.38f,.18f));
        }
        public void MeleeArc(Vector2 origin, Vector2 direction, float range)
        {
            float angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
            float radius=range*.8f;
            for(int i=0;i<12;i++)
            {
                float degrees=angle-65+(i+.5f)*130/12;
                Vector2 radial=new Vector2(Mathf.Cos(degrees*Mathf.Deg2Rad),Mathf.Sin(degrees*Mathf.Deg2Rad));
                var flash=Effect("Sword swing",origin+radial*radius,new Vector2(radius*.2f,.07f),new Color(1,.9f,.45f),.12f);
                flash.transform.rotation=Quaternion.Euler(0,0,degrees+90);
            }
        }
        public void MuzzleFlash(Vector2 point, Vector2 direction)
        {
            var flash = Effect("Muzzle flash",point,new Vector2(.3f,.18f),new Color(1,.9f,.45f),.07f);
            flash.transform.rotation = Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        }
        public void AttackWarning(SpriteRenderer warning, Vector2 point, Vector2 aim, bool ranged, int order)
        {
            float length = ranged ? 4.4f : 1.2f;
            warning.transform.position = point+aim*length*.5f;
            warning.transform.localScale = new Vector3(length,.045f,1);
            warning.transform.rotation = Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg);
            warning.color = new Color(1,.4f,.18f,.55f);
            warning.sortingOrder = order+1;
        }
        public void Tick(float seconds)
        {
            for(int i=flashes.Count-1;i>=0;i--)
            {
                var flash=flashes[i];flash.time-=seconds;
                if(flash.time<=0) { FarmWorldView.Remove(flash.model.gameObject); flashes.RemoveAt(i);continue; }
                flash.model.transform.position+=(Vector3)(flash.velocity*seconds);
                var tint=flash.tint;tint.a*=flash.time/flash.duration;flash.model.color=tint;
            }
        }
        public void Dispose()
        {
            Art.Dispose();
            if(square!=null) { if(Application.isPlaying) Object.Destroy(square); else Object.DestroyImmediate(square); }
        }
    }
}
