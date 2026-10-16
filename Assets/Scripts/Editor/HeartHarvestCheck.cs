using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class HeartHarvestCheck
{
    public static void Run()
    {
        if(!Application.isBatchMode) throw new Exception("Use an isolated batch project.");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        // Exercise damage/night behavior in a temporary fixture; the real farm stays peaceful.
        farm.Layout.combatSchedule=FarmMapLayout.CombatSchedule.NightOnly; farm.Initialize(false);
        var combat=farm.Combat;
        void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
        var heart=Resources.Load<PlantDefinition>("Equipment/Heart");
        Check(heart!=null && heart.effect==GrowthEffect.Heart,"Missing heart data");
        Check(combat.HeartCount==5 && combat.MaxHealth==100,"Default hearts");
        Check(combat.HeartAt(combat.HeartRect(2).center)==2 && combat.HeartAt(Vector2.zero)==-1,"Heart hover detection");
        farm.TeleportPlayer(new Vector2(22,14)); combat.DamagePlayer(combat.HalfHeartHealth);
        Check(combat.Health==90 && combat.HeartFill(4)==.5f && combat.HeartFill(3)==1,"Half heart damage display failed");
        combat.TickCombat(.7f); combat.DamagePlayer(combat.HalfHeartHealth);
        Check(combat.Health==80 && combat.HeartFill(4)==0 && combat.HeartCount==5,"Empty heart display failed");
        farm.TeleportPlayer(farm.SpawnPosition); combat.TickCombat(5);
        var hoe=UnityEditor.AssetDatabase.LoadAssetAtPath<PlantDefinition>("Assets/Settings/Hoe.asset");
        while(farm.AddItem(hoe)) {}
        int fullPieces=farm.WorldPieceCount;
        Check(combat.HarvestHeart(0) && combat.HeartCount==4 && farm.InventoryCount==farm.Capacity && farm.WorldPieceCount==fullPieces+1,"Full inventory heart did not drop");
        while(farm.InventoryCount>0) farm.RemoveInventoryItem(0);
        Check(farm.DetachAt(farm.PlayerPosition+Vector2.down*.65f) && farm.UseTool(Vector2Int.zero) && combat.HeartCount==5,"Dropped heart cannot be picked up and restored");
        int pieces=farm.WorldPieceCount;
        Check(combat.HarvestHeart(2) && farm.WorldPieceCount==pieces && farm.SelectedItem==heart && farm.InventoryQuantity(0)==1,"One press did not grant one heart");
        Check(combat.HeartCount==4 && combat.MaxHealth==80 && combat.Health==80,"Harvest did not reduce HP capacity");
        combat.TickCombat(3); Check(combat.Health<=80,"Safe recovery bypassed removed heart");
        Check(!combat.HarvestHeart(4) && farm.WorldPieceCount==pieces && farm.InventoryQuantity(0)==1,"Stale heart index granted another item");
        Check(farm.UseTool(Vector2Int.zero) && combat.HeartCount==5 && combat.MaxHealth==100,"Heart cannot reattach");
        Check(!combat.RestoreHeart(),"Heart count can exceed initial maximum");
        for(int i=0;i<4;i++) Check(combat.HarvestHeart(0),"Repeated individual harvest failed");
        pieces=farm.WorldPieceCount;
        Check(combat.HeartCount==1 && !combat.IsGameOver,"Premature game over");
        farm.TeleportPlayer(new Vector2(22,14)); combat.TickCombat(1); combat.DamagePlayer(200);
        Check(combat.Health==20 && combat.MaxHealth==20,"Death restored removed heart capacity");
        Check(farm.Till(Vector2Int.zero) && farm.AddItem(heart) && farm.PlantSelected(Vector2Int.zero),"Heart cannot be planted");
        farm.Water(Vector2.zero,.1f); farm.Tick(heart.growthSeconds);
        Check(farm.IsMature(Vector2Int.zero) && farm.InteractPlot(Vector2Int.zero),"Heart planting and harvest failed");
        pieces=farm.WorldPieceCount; var position=farm.PlayerPosition;
        Check(combat.HarvestHeart(0) && farm.WorldPieceCount==pieces && farm.InventoryQuantity(0)==6,"Last heart was not added exactly once");
        Check(combat.IsGameOver && combat.Health==0 && combat.MaxHealth==0 && combat.HeartCount==0,"Last heart did not trigger game over");
        combat.TickCombat(5); combat.DamagePlayer(200);
        Check(combat.IsGameOver && combat.Health==0 && farm.PlayerPosition==position,"Game over recovered or respawned");
        Check(!combat.HarvestHeart(0) && !combat.RestoreHeart() && farm.WorldPieceCount==pieces,"Game over can be bypassed");
        Debug.Log("HEART_HARVEST_SUCCESS: half/empty hearts, direct inventory harvest, full inventory drops, capacity, restore, final heart game over and planting.");
    }
}
