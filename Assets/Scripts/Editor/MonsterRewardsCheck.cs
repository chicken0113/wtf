using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using EverythingGrows;

public static class MonsterRewardsCheck
{
    private static void Check(bool value,string text) { if(!value)throw new Exception(text); }
    private static FarmGame Reload(MonsterType type=null)
    {
        string typePath=type!=null?AssetDatabase.GetAssetPath(type):null;
        if(type!=null) { EditorUtility.SetDirty(type);AssetDatabase.SaveAssetIfDirty(type); }
        GameSession.ResetForTests();EditorSceneManager.OpenScene("Assets/Scenes/LegacyVillage/CombatTest.unity");
        if(!string.IsNullOrEmpty(typePath))type=AssetDatabase.LoadAssetAtPath<MonsterType>(typePath);
        var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        if(type!=null)foreach(var marker in (f.Layout!=null?f.Layout.GetComponentsInChildren<EnemyPlacement>():f.GetComponentsInChildren<EnemyPlacement>()))if(!marker.trainingDummy)marker.monsterType=type;
        f.Initialize(false);GameSession.Bind(f);Check(f.IsInitialized && string.IsNullOrEmpty(GameSession.LastError),f.InitializationError+GameSession.LastError);return f;
    }
    private static object[] Hostiles(FarmGame f)=>CombatCheckAccess.Enemies(f.Combat).Cast<object>().Where(e=>!(bool)CombatCheckAccess.Read(e,"trainingDummy")).ToArray();
    public static void Run()
    {
        var saveRoot=Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR");
        if(!Application.isBatchMode || string.IsNullOrEmpty(saveRoot) || !Path.GetFullPath(Application.dataPath).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase))throw new Exception("Isolated project and saves only.");
        AssetDatabase.DeleteAsset("Assets/Resources/Monsters/RewardCheck.asset");
        var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");var f=Reload();int gold=f.Gold;
        var enemies=Hostiles(f);Check(enemies.Length==2,"Expected current two placed monsters");
        foreach(var enemy in enemies)
        {
            bool ranged=(bool)CombatCheckAccess.Read(enemy,"ranged");
            Check((UnityEngine.Object)CombatCheckAccess.Read(enemy,"monsterType")==MonsterType.Default(ranged),"Legacy placement default kind mismatch");
            int before=f.WorldPieceCount;CombatCheckAccess.DefeatEnemy(f.Combat,enemy);
            var piece=f.CaptureMap(catalog,0).pieces.Last();
            Check(f.WorldPieceCount==before+1 && piece.item==catalog.Id(Resources.Load<PlantDefinition>(ranged?"Equipment/Bow":"Equipment/Sword")) && f.Gold==gold,"Default reward changed");
        }
        Check(f.Combat.FloorCleared && GameSession.Save(f),"Default clear save failed");f=Reload();
        Check(f.Combat.Kills==2 && f.WorldPieceCount==2 && f.Gold==gold && f.Combat.FloorCleared,"Reload repeated or lost default rewards");

        var type=ScriptableObject.CreateInstance<MonsterType>();type.label="Shared reward test";type.gold=17;
        var seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");var heart=Resources.Load<PlantDefinition>("Equipment/Heart");
        type.drops=new[]{new MonsterDrop {item=seed,probability=1,minimum=2,maximum=4},new MonsterDrop {item=MonsterType.Default(true).drops[0].item,probability=0,minimum=99,maximum=99},new MonsterDrop {item=heart,probability=1,minimum=1,maximum=1}};
        AssetDatabase.CreateAsset(type,"Assets/Resources/Monsters/RewardCheck.asset");AssetDatabase.SaveAssets();
        GameSession.DirectoryOverride=Path.Combine(saveRoot,"configured");f=Reload(type);type=Resources.Load<MonsterType>("Monsters/RewardCheck");catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");heart=Resources.Load<PlantDefinition>("Equipment/Heart");gold=f.Gold;enemies=Hostiles(f);
        foreach(var enemy in enemies)Check((UnityEngine.Object)CombatCheckAccess.Read(enemy,"monsterType")==type,"Placements did not share type asset");
        int expectedDrops=0;
        foreach(var enemy in enemies)
        {
            int before=f.WorldPieceCount,oldGold=f.Gold,kills=f.Combat.Kills;
            CombatCheckAccess.DefeatEnemy(f.Combat,enemy);int amount=f.WorldPieceCount-before;expectedDrops+=amount;
            Check(amount>=3 && amount<=5 && f.Gold==oldGold+17,"Independent chance/quantity/gold failed");
            CombatCheckAccess.DefeatEnemy(f.Combat,enemy);
            Check(f.WorldPieceCount==before+amount && f.Gold==oldGold+17 && f.Combat.Kills==kills+1,"Duplicate hit awarded twice");
        }
        var drops=f.CaptureMap(catalog,0).pieces;
        Check(drops.Count(p=>p.item==catalog.Id(heart))==2 && drops.All(p=>p.item==catalog.Id(heart)||p.item==catalog.Id(seed)),"Guaranteed/zero-chance rows failed");
        Check(GameSession.Save(f),"Configured reward save failed");f=Reload(type);type=Resources.Load<MonsterType>("Monsters/RewardCheck");catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");heart=Resources.Load<PlantDefinition>("Equipment/Heart");
        Check(f.Gold==gold+34 && f.WorldPieceCount==expectedDrops && f.Combat.Kills==2 && f.Combat.FloorCleared,"Restore duplicated or lost configured rewards");

        type.gold=0;type.drops=Array.Empty<MonsterDrop>();GameSession.DirectoryOverride=Path.Combine(saveRoot,"empty");f=Reload(type);type=Resources.Load<MonsterType>("Monsters/RewardCheck");catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");heart=Resources.Load<PlantDefinition>("Equipment/Heart");gold=f.Gold;
        foreach(var enemy in Hostiles(f))CombatCheckAccess.DefeatEnemy(f.Combat,enemy);
        Check(f.Gold==gold && f.WorldPieceCount==0 && f.Combat.Kills==2,"No-reward type failed");
        type.gold=100;f.Combat.SpawnEnemy(new Vector2(9,5),false,null,true);var dummy=CombatCheckAccess.Enemies(f.Combat).Cast<object>().Last();
        CombatCheckAccess.DefeatEnemy(f.Combat,dummy);Check(f.Gold==gold && f.WorldPieceCount==0 && f.Combat.Kills==2,"Training target granted rewards");
        type.gold=int.MaxValue;GameSession.DirectoryOverride=Path.Combine(saveRoot,"overflow");f=Reload(type);type=Resources.Load<MonsterType>("Monsters/RewardCheck");catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");seed=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds");heart=Resources.Load<PlantDefinition>("Equipment/Heart");CombatCheckAccess.DefeatEnemy(f.Combat,Hostiles(f)[0]);
        Check(f.Gold==int.MaxValue,"Gold overflow");
        type.gold=-1;var report=new SetupValidationReport();SetupDataValidator.CheckMonsterType(type,catalog,report,new System.Collections.Generic.HashSet<PlantDefinition>(),true);Check(report.HasErrors,"Negative gold accepted");
        type.gold=0;type.drops=new[]{new MonsterDrop {item=seed,probability=float.NaN,minimum=5,maximum=2}};
        report=new SetupValidationReport();SetupDataValidator.CheckMonsterType(type,catalog,report,new System.Collections.Generic.HashSet<PlantDefinition>(),true);Check(report.HasErrors,"Invalid probability/range accepted");
        AssetDatabase.DeleteAsset("Assets/Resources/Monsters/RewardCheck.asset");
        Debug.Log("MONSTER_REWARDS_SUCCESS: legacy drops, shared kinds, independent guaranteed/zero chance rows, quantity bounds, gold, empty loot, training exclusion, duplicate hits, cleared-floor save/reload, overflow and configuration errors.");
    }
}
