using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class OpenFieldCheck
{
    public static void Run()
    {
        if(!Application.isBatchMode) throw new Exception("Use an isolated batch project.");
        PrototypeBuilder.CreateAndValidate();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var names=new[]{"Hoe","WateringCan","Stone","Cloud","InventorySlot"}; var data=new PlantDefinition[5];
        for(int i=0;i<5;i++) data[i]=AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/"+names[i]+".asset");
        var farm=new GameObject("Open field check").AddComponent<FarmGame>(); farm.Configure(data); farm.Initialize();
        void Check(bool value,string text) { if(!value) throw new Exception(text); }
        var sword=Resources.Load<PlantDefinition>("Equipment/Sword"); var bow=Resources.Load<PlantDefinition>("Equipment/Bow");
        Check(sword!=null && bow!=null && sword.harvestResult!=null && bow.harvestResult!=null,"Weapon data missing");
        Check(farm.Till(new Vector2Int(9,3)),"Cannot farm in open field");
        Check(farm.AddItem(sword) && farm.Till(Vector2Int.zero) && farm.InteractPlot(Vector2Int.zero),"Sword planting");
        Check(!farm.InteractPlot(Vector2Int.zero),"Premature harvest");
        farm.Tick(10); Check(!farm.IsMature(Vector2Int.zero),"Dry weapon grew");
        farm.Water(Vector2.zero,1); farm.Tick(8);
        Check(farm.InteractPlot(Vector2Int.zero) && farm.SelectedItem==sword.harvestResult,"Sword did not become Excalibur");
        Check(!farm.InteractPlot(Vector2Int.zero) && farm.InventoryCount==1,"Transformed weapon replanted or consumed");
        Check(farm.AddItem(bow) && farm.InteractPlot(Vector2Int.zero),"Harvest did not free plot");
        farm.Tick(8); Check(farm.InteractPlot(Vector2Int.zero) && farm.SelectedItem==bow.harvestResult,"Bow did not become gun");
        var combat=farm.Combat;
        Check(combat.EnemyCount==6,"Initial enemy population");
        combat.DamagePlayer(20); Check(combat.Health==100,"Safe farm damage");
        farm.MovePlayer(new Vector2(8,0)); combat.DamagePlayer(20); Check(combat.Health==80,"Field damage");
        combat.DamagePlayer(20); Check(combat.Health==80,"Damage invulnerability");
        combat.TickCombat(.7f); Check(combat.Roll(Vector2.up),"Dodge did not start");
        combat.DamagePlayer(20); Check(combat.Health==80,"Dodge immunity");
        combat.TickCombat(.2f); Check(farm.PlayerPosition.y>1.5f,"Dodge movement");
        farm.MovePlayer(Vector2.zero); combat.TickCombat(3); Check(combat.Health>80,"Safe recovery");
        farm.MovePlayer(new Vector2(9,0)); combat.SpawnEnemy(new Vector2(10,0),false);
        Check(combat.Attack(sword.harvestResult,new Vector2(10,0)),"Melee attack");
        Check(!combat.Attack(sword.harvestResult,new Vector2(10,0)),"Attack cooldown");
        combat.TickCombat(.41f); combat.Attack(sword.harvestResult,new Vector2(10,0)); Check(combat.Kills>=1,"Melee enemy death");
        int kills=combat.Kills;
        farm.MovePlayer(new Vector2(18,-12)); combat.SpawnEnemy(new Vector2(20,-12),false);
        combat.TickCombat(.41f);
        for(int shot=0;shot<3;shot++) { Check(combat.Attack(bow.harvestResult,new Vector2(20,-12)),"Gun attack cooldown did not expire"); for(int frame=0;frame<7;frame++) combat.TickCombat(.05f); }
        Check(combat.Kills>kills,"Ranged projectile damage");
        Check(OpenFieldCombat.SegmentDistance(Vector2.zero,new Vector2(10,0),new Vector2(5,.2f))<.3f,"Swept projectile collision");
        farm.MovePlayer(new Vector2(17,12)); combat.SpawnEnemy(new Vector2(20,12),true);
        for(int frame=0;frame<24;frame++) combat.TickCombat(.05f);
        float before=combat.Health; farm.MovePlayer(new Vector2(17,10));
        for(int frame=0;frame<24;frame++) combat.TickCombat(.05f);
        Check(combat.Health==before,"Enemy shot tracks player instead of being avoidable");
        int carried=farm.InventoryCount;
        farm.MovePlayer(new Vector2(22,14)); combat.TickCombat(1); combat.DamagePlayer(200);
        Check(farm.PlayerPosition==Vector2.zero && combat.Health==100 && farm.InventoryCount==carried,"Solo death recovery lost items");
        farm.MovePlayer(new Vector2(8,1));
        var camera=Camera.main; camera.transform.position=new Vector3(5,0,-10);
        var target=new RenderTexture(1280,720,24); camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
        var image=new Texture2D(1280,720,TextureFormat.RGBA32,false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
        System.IO.File.WriteAllBytes("OpenFieldCheck.png",image.EncodeToPNG()); camera.targetTexture=null; RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("OPEN_FIELD_VALIDATION_SUCCESS: crop transformation, harvest, no second transformation, free field, safe zone, melee, projectile, dodge, recovery.");
    }
}
