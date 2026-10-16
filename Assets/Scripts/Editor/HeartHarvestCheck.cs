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
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>(); farm.Initialize(false);
        var combat=farm.Combat;
        void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
        var heart=Resources.Load<PlantDefinition>("Equipment/Heart");
        Check(heart!=null && heart.effect==GrowthEffect.Heart,"Missing heart data");
        Check(combat.HeartCount==5 && combat.MaxHealth==100,"Default hearts");
        Check(combat.HeartAt(combat.HeartRect(2).center)==2 && combat.HeartAt(Vector2.zero)==-1,"Heart hover detection");
        int pieces=farm.WorldPieceCount;
        Check(combat.HarvestHeart(2) && farm.WorldPieceCount==pieces+1,"One press did not drop one heart");
        Check(combat.HeartCount==4 && combat.MaxHealth==80 && combat.Health==80,"Harvest did not reduce HP capacity");
        combat.TickCombat(3); Check(combat.Health<=80,"Safe recovery bypassed removed heart");
        Check(!combat.HarvestHeart(4) && farm.WorldPieceCount==pieces+1,"Stale heart index dropped another item");
        Check(farm.DetachAt(farm.PlayerPosition+Vector2.down*.65f) && farm.SelectedItem==heart,"Dropped heart cannot be picked up");
        Check(farm.UseTool(Vector2Int.zero) && combat.HeartCount==5 && combat.MaxHealth==100,"Heart cannot reattach");
        Check(!combat.RestoreHeart(),"Heart count can exceed initial maximum");
        for(int i=0;i<4;i++) Check(combat.HarvestHeart(0),"Repeated individual harvest failed");
        pieces=farm.WorldPieceCount;
        Check(combat.HeartCount==1 && !combat.IsGameOver,"Premature game over");
        farm.MovePlayer(new Vector2(22,14)); combat.TickCombat(1); combat.DamagePlayer(200);
        Check(combat.Health==20 && combat.MaxHealth==20,"Death restored removed heart capacity");
        Check(farm.Till(Vector2Int.zero) && farm.AddItem(heart) && farm.PlantSelected(Vector2Int.zero),"Heart cannot be planted");
        farm.Water(Vector2.zero,.1f); farm.Tick(heart.growthSeconds);
        Check(farm.IsMature(Vector2Int.zero) && farm.InteractPlot(Vector2Int.zero),"Heart planting and harvest failed");
        pieces=farm.WorldPieceCount; var position=farm.PlayerPosition;
        Check(combat.HarvestHeart(0) && farm.WorldPieceCount==pieces+1,"Last heart did not drop exactly once");
        Check(combat.IsGameOver && combat.Health==0 && combat.MaxHealth==0 && combat.HeartCount==0,"Last heart did not trigger game over");
        combat.TickCombat(5); combat.DamagePlayer(200);
        Check(combat.IsGameOver && combat.Health==0 && farm.PlayerPosition==position,"Game over recovered or respawned");
        Check(!combat.HarvestHeart(0) && !combat.RestoreHeart() && farm.WorldPieceCount==pieces+1,"Game over can be bypassed");
        Debug.Log("HEART_HARVEST_SUCCESS: hover, single drops, pickup, capacity, restore, final heart game over, respawn and planting.");
    }
}
