using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
public static class NightMerchantExampleSetup
{
    public const string Id="forest_night_trader",ScenePath="Assets/Scenes/ForestClearing.unity";
    public static void Author()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("배치 작성 전용입니다.");
        EditorSceneManager.OpenScene("Assets/Scenes/FirstGarden.unity");if(!Apply())throw new InvalidOperationException("야간 상인 배치를 보류했습니다.");Debug.Log("NIGHT_MERCHANT_AUTHORED");
    }
    public static bool Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return false;
        var active=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.IsValid() || !scene.isLoaded;
        if(!opened && scene.isDirty)return false;
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var farm=ManagementEditorUI.Components<FarmGame>(scene).Single();var manager=farm.GetComponent<NpcManager>();
            if(manager!=null && manager.npcs.Any(p=>p.id==Id))return true;
            if(manager==null)manager=farm.gameObject.AddComponent<NpcManager>();
            var group=new GameObject("Night merchant visit points");SceneManager.MoveGameObjectToScene(group,scene);group.transform.SetParent(farm.Layout.transform,false);
            var locations=new Transform[2];
            for(int i=0;i<2;i++)
            {
                var point=new GameObject("Visit point "+(i+1));point.transform.SetParent(group.transform,false);point.transform.position=farm.SpawnPosition+new Vector2(-2-i*3,-3);
                if(!new MapCollision(farm).CanStand(point.transform.position,.3f))throw new InvalidOperationException("숲 상인 후보 위치가 막혔습니다.");locations[i]=point.transform;
            }
            manager.npcs.Add(new NpcProfile {id=Id,displayName="밤의 행상인",greeting="밤에만 들르는 행상인이야.",visitingMerchant=true,visitChance=.35f,visitLocations=locations,offers=new System.Collections.Generic.List<ShopOffer> {
                new ShopOffer {item=Resources.Load<PlantDefinition>("Equipment/CampingBed"),buyPrice=20},
                new ShopOffer {item=Resources.Load<PlantDefinition>("Equipment/RespawnToken"),buyPrice=20},
                new ShopOffer {item=Resources.Load<PlantDefinition>("Crops/ParsnipSeeds"),buyPrice=10} }});
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Map/Merchant.prefab"),scene);
            root.name="Night travelling merchant";root.transform.SetParent(farm.Layout.transform,true);root.transform.position=farm.SpawnPosition+Vector2.down*3;
            var npc=root.GetComponent<ShopNpc>();npc.npcId=Id;PrefabUtility.RecordPrefabInstancePropertyModifications(npc);
            EditorUtility.SetDirty(manager);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("상인 예시 씬 저장 실패");return true;
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid() && active.isLoaded)SceneManager.SetActiveScene(active);}
    }
}
