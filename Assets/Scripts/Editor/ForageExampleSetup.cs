using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
// Explicit one-time authoring only. Never rebuilds maps on editor startup.
public static class ForageExampleSetup
{
    public static void Author()
    {
        if(!Application.isBatchMode)throw new System.InvalidOperationException("배치 작성 전용 진입점입니다.");
        EditorSceneManager.OpenScene("Assets/Scenes/LegacyVillage/FirstGarden.unity");
        if(!Apply())throw new System.InvalidOperationException("채집물 예시 배치가 보류되었습니다.");
        Debug.Log("FORAGE_EXAMPLES_AUTHORED");
    }
    public static bool Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return false;
        const string path="Assets/Scenes/OpenWorld/ForestClearing.unity";var active=SceneManager.GetActiveScene();
        var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid() || !scene.isLoaded;
        if(!opened && scene.isDirty)return false;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var farm=ManagementEditorUI.Components<FarmGame>(scene).Single();bool changed=false;
            string[] names={"StoneFragments","CloudFind"};
            for(int i=0;i<names.Length;i++)
            {
                var profile=Resources.Load<ForageDefinition>("Foraging/"+names[i]);if(farm.GetComponentsInChildren<FarmItemPlacement>().Any(p=>p.forage==profile))continue;
                var point=farm.SpawnPosition+new Vector2(-3-i*2,1);
                if(!new MapCollision(farm).CanStand(point,.5f))point=farm.SpawnPosition+Vector2.down*(2+i*2);
                ForageWindow.Place(profile,farm,point);changed=true;
            }
            if(changed && !EditorSceneManager.SaveScene(scene))throw new System.InvalidOperationException("채집물 예시 씬을 저장하지 못했습니다.");
            return true;
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid() && active.isLoaded)SceneManager.SetActiveScene(active);}
    }
}
