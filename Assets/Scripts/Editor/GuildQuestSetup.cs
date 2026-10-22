using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

[InitializeOnLoad]
public static class GuildQuestSetup
{
    const string Request="Library/GuildQuest.request",Result="Library/GuildQuest.result",Catalog="Assets/Resources/GuildQuests.asset",Marker="Guild requests 2026-10-22";
    static GuildQuestSetup(){if(!Application.isBatchMode)EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){File.WriteAllText(Result,"WAITING_FOR_PLAY_STOP");return;}
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty)){File.WriteAllText(Result,"WAITING_FOR_SCENE_SAVE");return;}
        File.Delete(Request);try{Apply();File.WriteAllText(Result,"SUCCESS");}catch(Exception e){Debug.LogException(e);File.WriteAllText(Result,e.ToString());}
    }
    [MenuItem("Tools/Everything Grows/Guild quests")]
    public static void Edit(){Selection.activeObject=AssetDatabase.LoadAssetAtPath<GuildQuestCatalog>(Catalog);EditorGUIUtility.PingObject(Selection.activeObject);}
    [MenuItem("Tools/Everything Grows/World/Add guild requests")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Stop Play and save scenes first.");
        var catalog=AssetDatabase.LoadAssetAtPath<GuildQuestCatalog>(Catalog);
        if(catalog==null)
        {
            catalog=ScriptableObject.CreateInstance<GuildQuestCatalog>();
            catalog.quests.Add(new GuildQuestDefinition{id="clear_dungeon",title="Clear the ruins",description="Defeat any 2 monsters in the dungeon, then report to the guild.",requiredKills=2,rewardGold=50,repeatable=true});
            catalog.quests.Add(new GuildQuestDefinition{id="ranged_watch",title="Stop the spitter",description="Defeat 1 ranged dungeon monster and return for your reward.",enemyKind=GuildEnemyKind.Ranged,requiredKills=1,rewardGold=40,repeatable=false});
            AssetDatabase.CreateAsset(catalog,Catalog);
        }
        if(catalog.quests.Any(q=>q==null||string.IsNullOrWhiteSpace(q.id)||q.requiredKills<1||q.rewardGold<0||AssetDatabase.LoadAssetAtPath<SceneAsset>(q.targetScene)==null)
            ||catalog.quests.GroupBy(q=>q.id).Any(g=>g.Count()>1))throw new Exception("Invalid/duplicate guild request definitions.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=EditorSceneManager.OpenScene(InteriorWorldSetup.Guild);var f=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
            if(f.Layout.transform.Find(Marker)==null)
            {
                var profile=f.GetComponent<NpcManager>().npcs.Single(p=>p.id=="guild_reception");profile.guildQuestGiver=true;
                var board=f.Layout.transform.Find("Guild furniture/Notice board - decoration");if(board==null)throw new Exception("Guild notice board missing.");
                var npc=board.gameObject.AddComponent<ShopNpc>();npc.npcId="guild_reception";npc.daytimeOnly=false;npc.interactionRange=2.6f;
                board.name="Quest notice board - E";
                var label=new GameObject("Guild request board label");label.transform.SetParent(board,false);label.transform.localPosition=Vector3.up*1.2f;label.AddComponent<WorldLabel>().text="Guild requests [E]";
                var marker=new GameObject(Marker);marker.transform.SetParent(f.Layout.transform,false);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();Debug.Log("GUILD_QUESTS_AUTHORED: reception and board share requests; existing layout retained.");
        }
        finally{if(setup.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);}
    }
}
