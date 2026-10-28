using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;
using static PelicanTownLayout;

internal static class WorldSceneGenerator
{
    internal const string DefaultWorld="Assets/Scenes/OpenWorld/GeneratedWorld.unity";
    internal static string Generate(WorldGenerationSettings settings,string requestedPath)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Play 중에는 생성할 수 없습니다.");
        settings.Validate();
        if(!requestedPath.StartsWith("Assets/Scenes/OpenWorld/",StringComparison.Ordinal)||!requestedPath.EndsWith(".unity",StringComparison.Ordinal)||requestedPath.Contains(".."))throw new ArgumentException("OpenWorld 폴더의 새 씬 경로를 지정하세요.");
        string recipeJson=EditorJsonUtility.ToJson(settings);
        string path=AssetDatabase.GenerateUniqueAssetPath(requestedPath);var plan=new WorldGenerationPlan(settings);
        var active=SceneManager.GetActiveScene();var preview=EditorSceneManager.OpenPreviewScene(SingleWorldSetup.World);Scene scene=default;
        try
        {
            scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            foreach(var original in preview.GetRootGameObjects()){var clone=UnityEngine.Object.Instantiate(original);clone.name=original.name;SceneManager.MoveGameObjectToScene(clone,scene);}
            var farm=ManagementEditorUI.Components<FarmGame>(scene).Single();var map=farm.Layout;
            foreach(string name in new[]{"Ground and paths","Trees and rocks","Abandoned clearing","Ground","Decoration","Travel"})
            {var old=map.transform.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            map.worldBounds.transform.localScale=new Vector3(settings.width,settings.height,1);map.safeArea.transform.localScale=map.worldBounds.transform.localScale;
            map.showSafeMarkers=false;map.passiveHealthRecovery=false;map.playerSpawn.position=plan.Position(plan.Start);
            Paint(map,plan,settings);PlaceExistingActors(farm,plan);
            foreach(var camera in ManagementEditorUI.Components<Camera>(scene))camera.transform.position=(Vector3)map.Spawn+Vector3.back*10;
            Root("Generated world - seed "+settings.seed,map.transform,Vector2.zero);
            Verify(farm,plan);
            if(!EditorSceneManager.SaveScene(scene,path))throw new IOException("생성한 월드를 저장하지 못했습니다.");
            ConfigureRegions(path);
            var recipe=ScriptableObject.CreateInstance<WorldGenerationSettings>();EditorJsonUtility.FromJsonOverwrite(recipeJson,recipe);recipe.name=Path.GetFileNameWithoutExtension(path);recipe.scene=AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            EnsureFolder("Assets/Settings/WorldGeneration");AssetDatabase.CreateAsset(recipe,AssetDatabase.GenerateUniqueAssetPath("Assets/Settings/WorldGeneration/"+recipe.name+".asset"));
            var scenes=EditorBuildSettings.scenes.ToList();scenes.Add(new EditorBuildSettingsScene(path,true));EditorBuildSettings.scenes=scenes.ToArray();
            Debug.Log("WORLD_GENERATED: "+path+" seed="+recipe.seed);return path;
        }
        finally
        {
            if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
            EditorSceneManager.ClosePreviewScene(preview);if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);
        }
    }
    internal static void EnsureFolder(string path)
    {if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
    static void Paint(FarmMapLayout map,WorldGenerationPlan plan,WorldGenerationSettings settings)
    {
        var ground=Group(map.transform,"Ground");var decor=Group(map.transform,"Decoration");var water=Group(decor,"Water collisions");var trees=Group(decor,"Forest and scattered trees");
        var rng=new System.Random(settings.seed^7157);
        for(int y=0;y<plan.Height;y++)for(int x=0;x<plan.Width;x++)
        {
            var cell=new Vector2Int(x,y);int i=plan.Index(cell);var point=plan.Position(cell);var terrain=plan.Cells[i];
            var chunk=Group(ground,"Chunk "+x/8+" - "+y/8);var tile=Root(terrain.ToString(),chunk,point);tile.AddComponent<FarmGround>().canFarm=terrain!=WorldGenerationPlan.Terrain.Water;
            float variation=.94f+(float)rng.NextDouble()*.1f;
            Color color=terrain==WorldGenerationPlan.Terrain.Water?new Color(.26f,.53f,.67f):terrain==WorldGenerationPlan.Terrain.Forest?new Color(.63f,.8f,.67f):new Color(.94f,1,.83f);
            Draw("Surface",tile.transform,point,Vector2.one,new Color(color.r*variation,color.g*variation,color.b*variation,1),-1000,terrain==WorldGenerationPlan.Terrain.Water?Tile:EditableMapBuilder.PreviewIcon(9));
            float density=terrain==WorldGenerationPlan.Terrain.Forest?settings.treeDensity:settings.treeDensity*.12f;
            if(terrain!=WorldGenerationPlan.Terrain.Water&&!plan.Reserved[i]&&rng.NextDouble()<density)Tree(trees,point,.9f+(float)rng.NextDouble()*.35f);
        }
        // Row runs keep water collision inexpensive and independently editable.
        for(int y=0;y<plan.Height;y++)for(int x=0;x<plan.Width;)
        {
            if(plan.Cells[plan.Index(new Vector2Int(x,y))]!=WorldGenerationPlan.Terrain.Water){x++;continue;}
            int begin=x;while(x<plan.Width&&plan.Cells[plan.Index(new Vector2Int(x,y))]==WorldGenerationPlan.Terrain.Water)x++;
            var p=plan.Position(new Vector2Int(begin,y))+Vector2.right*(x-begin-1)*.5f;
            var obstacle=Root("Water row",water,p).AddComponent<MapObstacle>();obstacle.size=new Vector2(x-begin,1);obstacle.blocksProjectiles=false;
        }
        foreach(var point in plan.Camps.Append(plan.Start).Append(plan.Boss))
        {
            string label=point==plan.Start?"출발 공터":point==plan.Boss?"보스 서식지":"거점 후보 공터";
            Root(label,Group(decor,"Landmark signs"),plan.Position(point)+Vector2.up*2.5f).AddComponent<WorldLabel>().text=label;
        }
    }
    static void PlaceExistingActors(FarmGame farm,WorldGenerationPlan plan)
    {
        Vector2 start=plan.Position(plan.Start),camp1=plan.Position(plan.Camps[0]),camp2=plan.Position(plan.Camps[1]);int loose=0;
        foreach(var item in farm.GetComponentsInChildren<FarmItemPlacement>())
        {
            if(item.forage!=null)item.transform.position=item.forage.name=="StoneFragments"?camp1:camp2;
            else if(item.item.name=="Sword")item.transform.position=start+new Vector2(-1,-1);
            else if(item.item.name=="CampingBed")item.transform.position=start+new Vector2(1,-1);
            else{item.transform.position=(loose<2?camp1:camp2)+new Vector2(loose%2==0?-1:1,-1);loose++;}
        }
        foreach(var enemy in farm.GetComponentsInChildren<EnemyPlacement>())enemy.transform.position=plan.Position(plan.Boss);
        foreach(var npc in farm.GetComponentsInChildren<ShopNpc>())npc.transform.position=npc.npcId==SingleWorldSetup.Trader?start+new Vector2(2,2):camp2+new Vector2(2,1);
        var manager=farm.GetComponent<NpcManager>();foreach(var profile in manager.npcs)for(int i=0;i<profile.visitLocations.Length;i++)profile.visitLocations[i].position=(i%2==0?camp1:camp2)+new Vector2(-2,1);
        var sign=farm.Layout.transform.Find("Camp - start and night trade/Night merchant sign");if(sign!=null)sign.position=start+new Vector2(2,3.5f);
    }
    static void ConfigureRegions(string path)
    {
        var region=ScriptableObject.CreateInstance<SpawnRegion>();region.id=Guid.NewGuid().ToString("N");region.label=Path.GetFileNameWithoutExtension(path)+" 필드";region.scenePath=path;region.useMapBounds=true;
        string asset=AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/SpawnRegions/"+Path.GetFileNameWithoutExtension(path)+".asset");AssetDatabase.CreateAsset(region,asset);
        foreach(string name in new[]{"Chaser","Spitter"}){var type=Resources.Load<MonsterType>("Monsters/"+name);type.spawnRegions=type.spawnRegions.Append(region).ToArray();EditorUtility.SetDirty(type);AssetDatabase.SaveAssetIfDirty(type);}
    }
    internal static void Verify(FarmGame farm,WorldGenerationPlan plan)
    {
        var collision=new MapCollision(farm);Vector2 start=plan.Position(plan.Start);
        foreach(var target in plan.Camps.Append(plan.Boss))
        {
            var point=plan.Position(target);var path=collision.FindPath(start,point,.25f);
            if(!collision.CanStand(point,.35f)||path.Count==0||Vector2.Distance(path[path.Count-1],point)>.05f)throw new InvalidOperationException("주요 공터로 이동할 수 없습니다: "+point);
        }
        if(!farm.Layout.CanFarm(start)||!collision.CanStand(start,.35f))throw new InvalidOperationException("시작 공터에서 농사를 지을 수 없습니다.");
        foreach(var item in farm.GetComponentsInChildren<FarmItemPlacement>())if(!collision.CanStand(item.transform.position,.35f))throw new InvalidOperationException("채집물 접근 불가");
        foreach(var npc in farm.GetComponentsInChildren<ShopNpc>())if(!collision.CanStand(npc.transform.position,.35f))throw new InvalidOperationException("상인 접근 불가");
        if(farm.GetComponentsInChildren<MapExit>().Length!=0)throw new InvalidOperationException("생성 월드에 외부 출구가 남았습니다.");
    }
}
