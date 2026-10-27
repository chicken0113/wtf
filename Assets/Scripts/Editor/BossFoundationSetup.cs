using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
public static class BossFoundationSetup
{
    public const string ScenePath="Assets/Scenes/OpenWorld/ForestClearing.unity";
    public const string PrefabPath="Assets/Prefabs/Map/FieldBossSpawn.prefab";
    public static bool Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return false;
        var active=SceneManager.GetActiveScene();if(string.IsNullOrEmpty(active.path))return false;
        var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.IsValid() || !scene.isLoaded;
        if(!opened && scene.isDirty){Debug.LogWarning("숲이 수정 중이어서 임시 보스 배치를 보류했습니다. 숲을 저장한 뒤 다시 컴파일하거나 FieldBossSpawn 프리팹을 직접 배치하세요.");return false;}
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var farm=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FarmGame>()).Single();
            var type=Resources.Load<MonsterType>("Monsters/FieldBoss");
            if(farm.GetComponentsInChildren<EnemyPlacement>().Any(m=>m.monsterType==type))return true;
            var collision=new MapCollision(farm);Vector2 home=default;bool found=false;
            for(float radius=8;radius<=16 && !found;radius+=2)for(int i=0;i<12;i++)
            {
                var point=farm.SpawnPosition+new Vector2(Mathf.Cos(i*Mathf.PI/6),Mathf.Sin(i*Mathf.PI/6))*radius;
                if(!collision.CanStand(point,1))continue;home=point;found=true;break;
            }
            if(!found)throw new InvalidOperationException("숲에 임시 보스를 배치할 빈 공간이 없습니다.");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);if(prefab==null || type==null)throw new InvalidOperationException("임시 보스 프리팹/종류가 없습니다.");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);root.transform.SetParent(farm.Layout.transform,true);root.transform.position=home;
            var marker=root.GetComponent<MapObjectId>();if(marker==null)marker=root.AddComponent<MapObjectId>();marker.AssignNew();
            PrefabUtility.RecordPrefabInstancePropertyModifications(marker);
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("숲의 보스 배치를 저장하지 못했습니다.");
            Debug.Log("FIELD_BOSS_AUTHORED: ForestClearing "+home);return true;
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid() && active.isLoaded)SceneManager.SetActiveScene(active);}
    }
}
