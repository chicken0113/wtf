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
        public bool IsDaytime => CurrentPhase==Phase.Daylight;
        public bool MonstersActive => farm.Combat.UsesRegions?true:farm.Layout!=null && farm.Layout.combatSchedule!=FarmMapLayout.CombatSchedule.NightOnly
            ? farm.Layout.combatSchedule==FarmMapLayout.CombatSchedule.Always
            : CurrentPhase==Phase.Dusk || CurrentPhase==Phase.Night;
        public float Darkness => (farm.Layout!=null && !farm.Layout.dayNightLighting?0:Settings.nightDarkness)*(CurrentPhase==Phase.Daylight?0:CurrentPhase==Phase.Dusk?Mathf.SmoothStep(0,1,(Elapsed-Day)/Dusk):CurrentPhase==Phase.Night?1:1-Mathf.SmoothStep(0,1,(Elapsed-Day-Dusk-Night)/Dawn));
        public void Initialize(FarmGame owner) => Initialize(owner,Resources.Load<DayNightSettings>("WorldCycle"));
        public void Initialize(FarmGame owner,DayNightSettings settings)
        {
            if(farm!=null) return;
            var report=new SetupValidationReport();
            SetupDataValidator.CheckCycle(settings,report);
            if(owner==null || owner.Combat==null || Camera.main==null)
                report.Error("CYCLE_OWNER",this,"FarmGame / Combat / MainCamera","시간 시스템의 연결이 준비되지 않았습니다.","FarmGame 초기화 순서와 MainCamera를 확인하세요.");
            report.Publish();
            if(report.HasErrors) { enabled=false;return; }
            farm=owner;Settings=settings;enabled=true;
            var root=new GameObject("World night shade"); root.transform.SetParent(Camera.main.transform,false); root.transform.localPosition=Vector3.forward;
            shade=root.AddComponent<SpriteRenderer>(); shade.sortingOrder=32760;
            shadeSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),Vector2.one*.5f,2); shade.sprite=shadeSprite;
            Apply();
        }
        public void RestoreElapsed(float elapsed) { Elapsed=Mathf.Repeat(Mathf.Max(0,elapsed),CycleSeconds); Apply(true); }
        public void Tick(float seconds)
        {
            if(Settings==null || seconds<=0 || farm.Combat.IsGameOver) return;
            var previous=CurrentPhase; Elapsed=Mathf.Repeat(Elapsed+seconds,CycleSeconds); Apply();
            if(previous!=CurrentPhase && IsDaytime)GameSession.CleanupUnloadedRegions(farm);
            if(previous!=CurrentPhase && farm.Combat.UsesRegions)farm.Notify(IsDaytime?"낮입니다. 몬스터가 깨어나고 지역 스폰을 시작합니다.":"몬스터가 잠듭니다. 공격하면 깨어납니다.");
            else if(previous!=CurrentPhase && farm.Layout!=null && farm.Layout.combatSchedule!=FarmMapLayout.CombatSchedule.NightOnly)
                farm.Notify(CurrentPhase==Phase.Daylight?"Daylight.":CurrentPhase==Phase.Dusk?"Evening.":CurrentPhase==Phase.Night?"Night.":"Dawn.");
            else if(previous!=CurrentPhase) farm.Notify(CurrentPhase==Phase.Daylight?"Daylight. Merchants are visiting.":CurrentPhase==Phase.Dusk?"Evening. Merchants left; monsters are appearing.":CurrentPhase==Phase.Night?"Night. Watch for monsters outside the farm.":"Dawn. Monsters are disappearing.");
        }
        void Apply(bool restoring=false)
        {
            if(farm.Combat.UsesRegions)farm.Combat.SetRegionalDaytime(IsDaytime,restoring);
            else farm.Combat.SetNightActive(MonstersActive);
            farm.Shop?.UpdateAvailability();
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
