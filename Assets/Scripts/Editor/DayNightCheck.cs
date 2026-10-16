using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class DayNightCheck
{
    public static void Run()
    {
        if(!Application.isBatchMode) throw new Exception("Use isolated validation.");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); farm.Initialize(false);
        void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
        var cycle=farm.Cycle; var settings=cycle.Settings; var shop=farm.Shop;
        var npc=farm.GetComponentInChildren<ShopNpc>(); var renderer=npc.GetComponent<SpriteRenderer>();
        int enemies=farm.Layout.GetComponentsInChildren<EnemyPlacement>().Count(marker=>!farm.IsSafe(marker.transform.position));
        Check(enemies>0 && cycle.ShopsOpen && !cycle.MonstersActive && farm.Combat.EnemyCount==0 && cycle.Darkness==0,"Daylight schedule incorrect");
        farm.MovePlayer(npc.transform.position-Vector3.up);
        Check(npc.IsVisiting && renderer.enabled && shop.Open(npc) && shop.Buy(0),"Daylight merchant unavailable");
        int gold=shop.Gold, carried=farm.InventoryCount;
        cycle.Tick(settings.daylightSeconds);
        Check(cycle.CurrentPhase==DayNightCycle.Phase.Dusk && farm.Combat.EnemyCount==enemies,"Evening monster arrival failed");
        Check(!npc.IsVisiting && !renderer.enabled && !shop.IsOpen && !shop.Open(npc),"Merchant remained after closing");
        Check(!shop.Buy(0) && !shop.Sell(0) && shop.Gold==gold && farm.InventoryCount==carried,"Trade occurred after closing");
        cycle.Tick(settings.duskSeconds*.5f);
        Check(cycle.Darkness>0 && cycle.Darkness<settings.nightDarkness,"Dusk did not darken gradually");
        Snapshot("Dusk.png");
        cycle.Tick(settings.duskSeconds*.5f);
        Check(cycle.CurrentPhase==DayNightCycle.Phase.Night && Mathf.Approximately(cycle.Darkness,settings.nightDarkness),"Night brightness incorrect");
        Snapshot("Night.png");
        var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");
        Check(farm.AddItem(seed) && farm.PlantSelected(new Vector2Int(1,-2)),"Crop setup failed");
        farm.Water(new Vector2(1,-2),.1f); farm.Tick(seed.growthSeconds);
        Check(farm.IsMature(new Vector2Int(1,-2)),"Crops stopped growing at night");
        cycle.Tick(settings.nightSeconds);
        Check(cycle.CurrentPhase==DayNightCycle.Phase.Dawn && farm.Combat.EnemyCount==0 && !npc.IsVisiting,"Dawn monster departure failed");
        foreach(var monster in farm.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name=="Chaser" || r.name=="Spitter")) Check(!monster.enabled,"Hidden daytime monster remains visible");
        cycle.Tick(settings.dawnSeconds*.5f);
        Check(cycle.Darkness>0 && cycle.Darkness<settings.nightDarkness,"Dawn did not brighten gradually");
        cycle.Tick(settings.dawnSeconds*.5f);
        Check(cycle.ShopsOpen && cycle.Darkness==0 && npc.IsVisiting && renderer.enabled && shop.Open(npc),"Merchant did not return next daylight");
        cycle.Tick(settings.daylightSeconds);
        Check(farm.Combat.EnemyCount==enemies,"Monsters did not return next evening");
        for(int i=0;i<5;i++) farm.Combat.HarvestHeart(0);
        float elapsed=cycle.Elapsed; cycle.Tick(50); Check(cycle.Elapsed==elapsed,"Cycle continued after game over");
        Debug.Log("DAY_NIGHT_SUCCESS: phases, smooth brightness, monster arrival/departure, merchant trading limits, repeat visits, night crop growth and game over.");
    }
    public static void ValidateAll() { Run(); OpenFieldCheck.Run(); }
    static void Snapshot(string path)
    {
        var camera=Camera.main; camera.transform.position=new Vector3(0,0,-10);
        var target=new RenderTexture(1280,720,24); camera.targetTexture=target; UnityEngine.Object.FindFirstObjectByType<DayNightCycle>().RefreshLighting(); camera.Render(); RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGBA32,false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
        System.IO.File.WriteAllBytes(path,image.EncodeToPNG()); camera.targetTexture=null; RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
    }
}
