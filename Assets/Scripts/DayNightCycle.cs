using UnityEngine;
namespace EverythingGrows
{
    public sealed class DayNightCycle : MonoBehaviour
    {
        public enum Phase { Daylight, Dusk, Night, Dawn }
        private FarmGame farm;
        private SpriteRenderer shade;
        private Sprite shadeSprite;
        public DayNightSettings Settings { get; private set; }
        public float Elapsed { get; private set; }
        float Day => Mathf.Max(1,Settings.daylightSeconds);
        float Dusk => Mathf.Max(1,Settings.duskSeconds);
        float Night => Mathf.Max(1,Settings.nightSeconds);
        float Dawn => Mathf.Max(1,Settings.dawnSeconds);
        public float CycleSeconds => Day+Dusk+Night+Dawn;
        public Phase CurrentPhase => Elapsed<Day?Phase.Daylight:Elapsed<Day+Dusk?Phase.Dusk:Elapsed<Day+Dusk+Night?Phase.Night:Phase.Dawn;
        public bool ShopsOpen => CurrentPhase==Phase.Daylight;
        public bool MonstersActive => farm.Layout!=null && farm.Layout.combatSchedule!=FarmMapLayout.CombatSchedule.NightOnly
            ? farm.Layout.combatSchedule==FarmMapLayout.CombatSchedule.Always
            : CurrentPhase==Phase.Dusk || CurrentPhase==Phase.Night;
        public float Darkness => (farm.Layout!=null && !farm.Layout.dayNightLighting?0:Settings.nightDarkness)*(CurrentPhase==Phase.Daylight?0:CurrentPhase==Phase.Dusk?Mathf.SmoothStep(0,1,(Elapsed-Day)/Dusk):CurrentPhase==Phase.Night?1:1-Mathf.SmoothStep(0,1,(Elapsed-Day-Dusk-Night)/Dawn));
        public void Initialize(FarmGame owner)
        {
            if(farm!=null) return;
            farm=owner; Settings=Resources.Load<DayNightSettings>("WorldCycle");
            if(Settings==null) { Debug.LogError("Missing Resources/WorldCycle settings."); enabled=false; return; }
            var root=new GameObject("World night shade"); root.transform.SetParent(Camera.main.transform,false); root.transform.localPosition=Vector3.forward;
            shade=root.AddComponent<SpriteRenderer>(); shade.sortingOrder=32760;
            shadeSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),Vector2.one*.5f,2); shade.sprite=shadeSprite;
            Apply();
        }
        public void RestoreElapsed(float elapsed) { Elapsed=Mathf.Repeat(Mathf.Max(0,elapsed),CycleSeconds); Apply(); }
        public void Tick(float seconds)
        {
            if(Settings==null || seconds<=0 || farm.Combat.IsGameOver) return;
            var previous=CurrentPhase; Elapsed=Mathf.Repeat(Elapsed+seconds,CycleSeconds); Apply();
            if(previous!=CurrentPhase && farm.Layout!=null && farm.Layout.combatSchedule!=FarmMapLayout.CombatSchedule.NightOnly)
                farm.Notify(CurrentPhase==Phase.Daylight?"Daylight.":CurrentPhase==Phase.Dusk?"Evening.":CurrentPhase==Phase.Night?"Night.":"Dawn.");
            else if(previous!=CurrentPhase) farm.Notify(CurrentPhase==Phase.Daylight?"Daylight. Merchants are visiting.":CurrentPhase==Phase.Dusk?"Evening. Merchants left; monsters are appearing.":CurrentPhase==Phase.Night?"Night. Watch for monsters outside the farm.":"Dawn. Monsters are disappearing.");
        }
        void Apply()
        {
            farm.Combat.SetNightActive(MonstersActive); farm.Shop?.UpdateAvailability();
            RefreshLighting();
        }
        void LateUpdate() { if(Settings!=null) RefreshLighting(); }
        public void RefreshLighting()
        {
            if(shade==null || Camera.main==null) return;
            var camera=Camera.main;
            shade.transform.localScale=new Vector3(camera.orthographicSize*2*camera.aspect+.2f,camera.orthographicSize*2+.2f,1);
            shade.color=new Color(.02f,.04f,.12f,Darkness); shade.enabled=Darkness>0;
        }
        void OnDestroy()
        {
            if(shade!=null) { if(Application.isPlaying) Destroy(shade.gameObject); else DestroyImmediate(shade.gameObject); }
            if(shadeSprite!=null) { if(Application.isPlaying) Destroy(shadeSprite); else DestroyImmediate(shadeSprite); }
        }
    }
}
