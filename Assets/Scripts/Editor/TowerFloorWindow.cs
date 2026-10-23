using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

public sealed class TowerFloorWindow : EditorWindow
{
    private string scenePath,previousArrival="tower_from_second",nextArrival="from_previous",message;
    private SceneAsset template,previous,next;
    private int number=3;
    private DungeonFloorKind kind=DungeonFloorKind.Normal;
    private Vector2 scroll;
    [MenuItem("Tools/Everything Grows/탑 층 관리")]
    public static void Open()=>GetWindow<TowerFloorWindow>("탑 층 관리");
    private void OnEnable() { minSize=new Vector2(540,440);template=AssetDatabase.LoadAssetAtPath<SceneAsset>(TowerDungeonSetup.Battle);previous=AssetDatabase.LoadAssetAtPath<SceneAsset>(TowerDungeonSetup.Lobby); }
    private void OnGUI()
    {
        scroll=EditorGUILayout.BeginScrollView(scroll);var scene=ManagementEditorUI.ScenePicker(ref scenePath);
        var map=ManagementEditorUI.Components<FarmMapLayout>(scene).FirstOrDefault();
        using(new EditorGUI.DisabledScope(ManagementEditorUI.Playing))
        {
            EditorGUILayout.LabelField("현재 층 설정",EditorStyles.boldLabel);
            if(map!=null)
            {
                var floor=map.GetComponent<DungeonFloor>();
                if(floor==null)
                {
                    if(GUILayout.Button("이 씬에 층 설정 추가"))
                    {
                        floor=Undo.AddComponent<DungeonFloor>(map.gameObject);floor.floorNumber=scene.path==TowerDungeonSetup.Lobby?1:2;
                        if(scene.path==TowerDungeonSetup.Lobby) { floor.kind=DungeonFloorKind.Preparation;floor.clearRule=FloorClearRule.NoCombat; }
                        EditorSceneManager.MarkSceneDirty(scene);
                    }
                }
                if(floor!=null)
                {
                    var data=new SerializedObject(floor);data.Update();EditorGUILayout.PropertyField(data.FindProperty("floorNumber"));EditorGUILayout.PropertyField(data.FindProperty("kind"));EditorGUILayout.PropertyField(data.FindProperty("clearRule"));
                    if(data.ApplyModifiedProperties())EditorSceneManager.MarkSceneDirty(scene);
                }
                ManagementEditorUI.SaveSceneButton(scene);
            }
            EditorGUILayout.Space();EditorGUILayout.LabelField("템플릿에서 새 층 만들기",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("템플릿 파일을 복사하여 별도 씬을 만듭니다. 몬스터 배치와 기존 출입구/도착점을 제거하고 표준 계단을 배치합니다. 기존 씬과 역방향 연결은 자동 수정하지 않습니다.",MessageType.Info);
            template=(SceneAsset)EditorGUILayout.ObjectField("템플릿 씬",template,typeof(SceneAsset),false);
            number=Mathf.Max(1,EditorGUILayout.IntField("층 번호",number));kind=(DungeonFloorKind)EditorGUILayout.EnumPopup("층 유형",kind);
            previous=(SceneAsset)EditorGUILayout.ObjectField("이전 층",previous,typeof(SceneAsset),false);previousArrival=EditorGUILayout.TextField("이전 층 도착 ID",previousArrival);
            next=(SceneAsset)EditorGUILayout.ObjectField("다음 층 (선택)",next,typeof(SceneAsset),false);nextArrival=EditorGUILayout.TextField("다음 층 도착 ID",nextArrival);
            using(new EditorGUI.DisabledScope(template==null || previous==null))
                if(GUILayout.Button("새 층 씬 생성"))
                {
                    string path=EditorUtility.SaveFilePanelInProject("새 층 저장","TowerFloor"+number,"unity","기존 파일은 덮어쓰지 않습니다.","Assets/Scenes");
                    if(!string.IsNullOrEmpty(path))try { CreateFloor(AssetDatabase.GetAssetPath(template),path,number,kind,AssetDatabase.GetAssetPath(previous),previousArrival,next!=null?AssetDatabase.GetAssetPath(next):null,nextArrival);message="생성 완료. 맵 배치/연결 관리에서 몬스터와 왕복 연결을 설정하세요."; }
                    catch(Exception error) { message=error.Message;Debug.LogException(error); }
                }
        }
        if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.Info);
        EditorGUILayout.EndScrollView();
    }
    internal static Scene CreateFloor(string template,string path,int number,DungeonFloorKind kind,string previous,string previousArrival,string next,string nextArrival)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || number<1 || !path.StartsWith("Assets/",StringComparison.Ordinal) || !path.EndsWith(".unity",StringComparison.Ordinal) || File.Exists(path))throw new InvalidOperationException("새 층 번호/경로를 확인하세요. 기존 파일은 덮어쓰지 않습니다.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(template)==null || AssetDatabase.LoadAssetAtPath<SceneAsset>(previous)==null || string.IsNullOrWhiteSpace(previousArrival) || (!string.IsNullOrEmpty(next) && (AssetDatabase.LoadAssetAtPath<SceneAsset>(next)==null || string.IsNullOrWhiteSpace(nextArrival))))throw new InvalidOperationException("템플릿/이전·다음 층과 도착 ID를 확인하세요.");
        var original=SceneManager.GetActiveScene();Scene created=default;
        if(!AssetDatabase.CopyAsset(template,path))throw new IOException("층 템플릿 복사 실패: "+path);
        try
        {
            created=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var farm=ManagementEditorUI.Components<FarmGame>(created).Single();var map=farm.Layout!=null?farm.Layout:farm.GetComponentInChildren<FarmMapLayout>();
            if(map==null)throw new InvalidOperationException("템플릿에 FarmMapLayout이 없습니다.");
            foreach(var e in ManagementEditorUI.Components<EnemyPlacement>(created))DestroyImmediate(e.gameObject);
            foreach(var e in ManagementEditorUI.Components<MapExit>(created))DestroyImmediate(e.gameObject);
            foreach(var a in ManagementEditorUI.Components<MapArrivalPoint>(created))DestroyImmediate(a.gameObject);
            foreach(var id in ManagementEditorUI.Components<MapObjectId>(created))DestroyImmediate(id);
            var floor=map.GetComponent<DungeonFloor>();if(floor==null)floor=map.gameObject.AddComponent<DungeonFloor>();
            floor.floorNumber=number;floor.kind=kind;floor.clearRule=kind==DungeonFloorKind.Preparation?FloorClearRule.NoCombat:FloorClearRule.AllHostiles;
            map.combatSchedule=kind==DungeonFloorKind.Preparation?FarmMapLayout.CombatSchedule.Peaceful:FarmMapLayout.CombatSchedule.Always;
            var collision=new MapCollision(farm);
            Vector2 Open(Vector2 desired)=>Enumerable.Range(Mathf.CeilToInt(map.World.xMin+2),Mathf.Max(1,Mathf.FloorToInt(map.World.width-4)))
                .SelectMany(x=>Enumerable.Range(Mathf.CeilToInt(map.World.yMin+2),Mathf.Max(1,Mathf.FloorToInt(map.World.height-4))).Select(y=>new Vector2(x,y)))
                .Where(p=>collision.CanStand(p,.8f)).OrderBy(p=>Vector2.Distance(p,desired)).First();
            Vector2 low=Open(map.World.center+Vector2.down*(map.World.height*.3f)),high=Open(map.World.center+Vector2.up*(map.World.height*.3f));
            MapExitTypesSetup.GeneratePrefabs();
            void Stair(string name,Vector2 point,string target,string arrival)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MapExitTypesSetup.DoorPath),created);go.name=name;go.transform.SetParent(map.transform,true);go.transform.position=point;
                var exit=go.GetComponent<MapExit>();exit.destinationScene=target;exit.arrivalId=arrival;exit.requireInteract=true;exit.requireClearFloor=true;PrefabUtility.RecordPrefabInstancePropertyModifications(exit);
            }
            void Arrival(string id,Vector2 point) { var go=new GameObject(id);SceneManager.MoveGameObjectToScene(go,created);go.transform.SetParent(map.transform,true);go.transform.position=point;go.AddComponent<MapArrivalPoint>().arrivalId=id; }
            Stair("이전 층 계단",low,previous,previousArrival);if(!string.IsNullOrEmpty(next))Stair("다음 층 계단",high,next,nextArrival);
            Arrival("from_previous",Open(low+Vector2.up*2.5f));Arrival("from_next",Open(high+Vector2.down*2.5f));
            if(map.playerSpawn!=null)map.playerSpawn.position=Open(low+Vector2.up*2.5f);
            SceneManager.SetActiveScene(created);MapSaveAuthoring.PrepareCurrentMap();EditorSceneManager.SaveScene(created);
            var builds=EditorBuildSettings.scenes.ToList();builds.Add(new EditorBuildSettingsScene(path,true));EditorBuildSettings.scenes=builds.ToArray();
            return created;
        }
        catch { if(created.IsValid())EditorSceneManager.CloseScene(created,true);AssetDatabase.DeleteAsset(path);throw; }
        finally { if(original.IsValid() && original.isLoaded)SceneManager.SetActiveScene(original); }
    }
}
