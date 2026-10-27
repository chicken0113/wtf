using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EverythingGrows
{
    public sealed partial class GameSession : MonoBehaviour
    {
        static GameSession runner;
        static GameSaveData data;
        static SaveItemCatalog catalog;
        static FarmGame current;
        static bool loaded,blocked;
        static string pendingScene,pendingArrival;
        static Vector2? pendingPosition;
        static string error;
        public static bool Busy { get; private set; }
        public static float ArrivalGraceUntil { get; private set; }
        public static string DirectoryOverride;
        public static bool Enabled => !Application.isBatchMode || !string.IsNullOrEmpty(DirectoryOverride) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR"));
        public static string SaveDirectory => !string.IsNullOrEmpty(DirectoryOverride)?DirectoryOverride:Application.isBatchMode?Environment.GetEnvironmentVariable("WTF_SAVE_TEST_DIR"):Path.Combine(Application.persistentDataPath,"wtf");
        public static string SavePath => Path.Combine(SaveDirectory,"wtf-autosave.json");
        public static double WorldSeconds => data?.worldSeconds??0;
        public static string LastError => error;
        public static int RemoteStorageBonus(string activeScene)
        {
            if(!Enabled || data==null || catalog==null || blocked) return 0;
            int total=0;
            foreach(var map in data.maps) if(map.scene!=activeScene) foreach(var plot in map.plots)
            {
                if(!plot.mature || string.IsNullOrEmpty(plot.item)) continue;
                var item=catalog.Resolve(plot.item);
                if(item.Function==PlantFunction.Storage) total+=item.HarvestItem.storageBonus;
            }
            return total;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRuntime() { runner=null; data=null; catalog=null; current=null; loaded=blocked=Busy=false; pendingScene=pendingArrival=error=null; pendingPosition=null; ArrivalGraceUntil=0; pendingRespawn=false; }
        public static void ResetForTests() { ResetRuntime(); }
        static void EnsureRunner()
        {
            if(runner!=null || !Application.isPlaying) return;
            var root=new GameObject("Game save and map travel"); runner=root.AddComponent<GameSession>(); DontDestroyOnLoad(root);
        }
        static void Load()
        {
            if(loaded) return;
            loaded=true; catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");
            var setup=new SetupValidationReport(); SetupDataValidator.CheckCatalog(catalog,setup);
            if(setup.HasErrors) throw new InvalidOperationException(setup.FirstError);
            if(!File.Exists(SavePath) && !File.Exists(SavePath+".bak")) { data=new GameSaveData(); return; }
            Exception failure=null;
            foreach(var path in new[]{SavePath,SavePath+".bak"})
            {
                if(!File.Exists(path)) continue;
                try
                {
                    var candidate=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
                    if(candidate==null || candidate.version!=1 || candidate.player==null || candidate.maps==null || string.IsNullOrEmpty(candidate.currentScene)) throw new InvalidDataException("Unsupported or invalid save data.");
                    candidate.guildQuests ??= new System.Collections.Generic.List<GuildQuestSave>();
                    Validate(candidate); data=candidate; return;
                }
                catch(InvalidOperationException) { throw; }
                catch(Exception problem) { failure=problem; }
            }
            throw new InvalidDataException("Autosave and backup could not be loaded. Existing files were preserved.",failure);
        }
        static void Validate(GameSaveData candidate)
        {
            ValidateGuildQuests(candidate);
            ValidateRecovery(candidate);
            ValidateRegionalEnemies(candidate);
            if(string.IsNullOrEmpty(candidate.currentScene)) throw new InvalidDataException("Save the Unity scene to disk before saving game progress.");
            if(candidate.player.inventory==null || candidate.player.removedSlots<0 || candidate.player.removedHearts<0 || candidate.player.gold<0 || candidate.player.farmingExperience<0) throw new InvalidDataException("Invalid player save.");
            var slots=new System.Collections.Generic.HashSet<int>();
            foreach(var stack in candidate.player.inventory)
            {
                if(stack==null || string.IsNullOrEmpty(stack.item) || stack.count<1)throw new InvalidDataException("Invalid item quantity.");
                if(candidate.player.inventoryUsesSlots && (stack.slot<0 || !slots.Add(stack.slot)))throw new InvalidDataException("Invalid or duplicate inventory slot.");
                catalog.Resolve(stack.item);
            }
            foreach(var map in candidate.maps)
            {
                if(map==null || map.plots==null || map.pieces==null || map.buildings==null || map.removedObjects==null) throw new InvalidDataException("Invalid map save.");
                map.forageNodes??=new System.Collections.Generic.List<ForageNodeSave>();
                var forageIds=new System.Collections.Generic.HashSet<string>();
                foreach(var node in map.forageNodes)
                {
                    if(node==null || string.IsNullOrWhiteSpace(node.id) || !forageIds.Add(node.id) || string.IsNullOrWhiteSpace(node.item) || node.remaining<0 || double.IsNaN(node.readyAt) || double.IsInfinity(node.readyAt) || node.readyAt<0)throw new InvalidDataException("Invalid gathering node in "+map.scene);
                    catalog.Resolve(node.item);
                }
                map.defeatedEnemies ??= new System.Collections.Generic.List<string>();
                if(map.defeatedEnemies.Any(string.IsNullOrWhiteSpace) || map.defeatedEnemies.Distinct().Count()!=map.defeatedEnemies.Count)
                    throw new InvalidDataException("Invalid defeated-enemy IDs in map: "+map.scene);
                foreach(var plot in map.plots) if(!string.IsNullOrEmpty(plot.item)) catalog.Resolve(plot.item);
                foreach(var piece in map.pieces) catalog.Resolve(piece.item);
                foreach(var building in map.buildings) if(catalog.Resolve(building.item).effect!=GrowthEffect.House) throw new InvalidDataException("Invalid house item.");
            }
        }
        public static void Bind(FarmGame farm)
        {
            if(!Enabled || farm==null || !farm.IsInitialized) return;
            EnsureRunner(); bool first=current==null && !loaded;
            try
            {
                Load(); if(blocked) return;
                EnsureDefaultRespawn(farm);
                string scene=farm.gameObject.scene.path;
                if(first && Application.isPlaying && !string.IsNullOrEmpty(data.currentScene) && data.currentScene!=scene)
                {
                    if(SceneUtility.GetBuildIndexByScenePath(data.currentScene)<0) throw new InvalidOperationException("Saved scene is unavailable. Enable it in Build Settings before resuming: "+data.currentScene);
                    Busy=true; pendingScene=data.currentScene; runner.StartCoroutine(runner.LoadMap(data.currentScene,scene)); return;
                }
                MapArrivalPoint arrival=null;
                if(pendingScene==scene && !string.IsNullOrEmpty(pendingArrival))
                { arrival=MapArrivalPoint.Find(farm.gameObject.scene,pendingArrival); if(arrival==null) throw new InvalidOperationException("Arrival point not found: "+pendingArrival); }
                var map=data.maps.Find(entry=>entry.scene==scene);
                if(map!=null) farm.RestoreMap(map,catalog);
                if(!string.IsNullOrEmpty(data.currentScene))
                {
                    farm.RestorePlayer(data.player,catalog);
                    farm.Cycle.RestoreElapsed(data.player.cycleElapsed);
                    CleanupUnloadedRegions(farm);
                    if(data.currentScene==scene)
                    {
                        // Scene edits can put an old saved position inside a new river or building.
                        var position=data.player.position;
                        if(!new MapCollision(farm).CanStand(position,farm.PlayerRadius)) position=farm.SpawnPosition;
                        farm.TeleportPlayer(position);
                    }
                }
                if(arrival!=null) farm.TeleportPlayer(arrival.transform.position);
                if(pendingScene==scene && pendingPosition.HasValue)
                {
                    var position=pendingPosition.Value;
                    farm.TeleportPlayer(new MapCollision(farm).CanStand(position,farm.PlayerRadius)?position:farm.SpawnPosition);
                }
                if(pendingRespawn && pendingScene==scene)
                { farm.TeleportPlayer(ResolveRespawn(farm,data.respawn!=null && data.respawn.IsSet && data.respawn.scene==scene?data.respawn:data.defaultRespawn));pendingRespawn=false; }
                if(map!=null) farm.Tick(Mathf.Max(0,(float)(data.worldSeconds-map.updatedAt)));
                farm.ReconcileRestoredInventory();
                MapExit.PrepareArrival(farm);
                current=farm;
                ArrivalGraceUntil=Time.unscaledTime+1;
                if(pendingScene==scene) { pendingScene=pendingArrival=null; pendingPosition=null; }
                // Do not write on entry until the arriving map has been validated by the travel coroutine.
                if(string.IsNullOrEmpty(data.currentScene)) Save(farm);
            }
            catch(Exception problem) { error=problem.Message; if(!Busy) blocked=true; Debug.LogError("Save restore failed: "+problem); }
        }
        public static void AdvanceTime(float seconds) { if(Enabled && data!=null && !Busy && !blocked && seconds>0) data.worldSeconds+=seconds; }
        public static bool Save(FarmGame farm)
        {
            if(!Enabled) return true;
            if(blocked || farm==null) return false;
            if(!farm.IsInitialized) { error=farm.InitializationError??"게임 초기화를 완료한 뒤 저장하세요."; return false; }
            try
            {
                Load(); if(data==null) return false;
                var map=farm.CaptureMap(catalog,data.worldSeconds);
                var player=farm.CapturePlayer(catalog);
                var next=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
                next.maps.RemoveAll(entry=>entry.scene==map.scene); next.maps.Add(map); next.player=player; next.currentScene=map.scene;
                Validate(next); Directory.CreateDirectory(SaveDirectory);
                string temporary=SavePath+".tmp";
                File.WriteAllText(temporary,JsonUtility.ToJson(next,true));
                if(File.Exists(SavePath)) File.Replace(temporary,SavePath,SavePath+".bak"); else File.Move(temporary,SavePath);
                data=next; error=null; return true;
            }
            catch(Exception problem) { error="Autosave failed: "+problem.Message; Debug.LogError(error); return false; }
        }
        public static bool SaveAfterHouseTime(FarmGame farm) => Save(farm);
        public static bool EnterHouse(FarmGame farm,string target,string arrival)
        {
            if(farm==null || !farm.IsInitialized) return false;
            Load();if(data==null||Busy||farm==null)return false;
            string previous=data.houseReturnScene;Vector2 position=data.houseReturnPosition;
            data.houseReturnScene=farm.gameObject.scene.path;data.houseReturnPosition=farm.PlayerPosition;
            if(Travel(farm,target,arrival))return true;
            data.houseReturnScene=previous;data.houseReturnPosition=position;return false;
        }
        public static bool ReturnFromHouse(FarmGame farm,string fallbackScene,string fallbackArrival)
        {
            if(farm==null || !farm.IsInitialized) return false;
            Load();return data!=null&&!string.IsNullOrEmpty(data.houseReturnScene)
                ? Travel(farm,data.houseReturnScene,null,data.houseReturnPosition)
                : Travel(farm,fallbackScene,fallbackArrival);
        }
        public static bool Travel(FarmGame farm,string target,string arrival,Vector2? position=null)
        {
            if(!Application.isPlaying || Busy || blocked || farm==null || !farm.IsInitialized || farm.Combat.IsGameOver) return false;
            if(string.IsNullOrEmpty(target) || SceneUtility.GetBuildIndexByScenePath(target)<0)
            { error="Destination scene is not enabled in Build Settings."; return false; }
            if(SceneUtility.GetBuildIndexByScenePath(farm.gameObject.scene.path)<0) { error="Enable the current scene in Build Settings before travelling."; return false; }
            if(!position.HasValue && string.IsNullOrWhiteSpace(arrival)) { error="Set the destination arrival ID."; return false; }
            farm.Combat.ResetBossEncounters();
            if(!Save(farm)) return false;
            error=null; EnsureRunner(); pendingScene=target; pendingArrival=arrival; pendingPosition=position; Busy=true;
            runner.StartCoroutine(runner.LoadMap(target,farm.gameObject.scene.path)); return true;
        }
        IEnumerator LoadMap(string target,string origin)
        {
            AsyncOperation operation=null;
            try { operation=SceneManager.LoadSceneAsync(target,LoadSceneMode.Single); }
            catch(Exception problem) { error="Could not load destination: "+problem.Message; }
            if(operation==null) { Busy=false; pendingScene=pendingArrival=null; pendingPosition=null; yield break; }
            yield return operation;
            var scene=SceneManager.GetActiveScene();
            if(current==null || current.gameObject.scene!=scene || !string.IsNullOrEmpty(pendingScene))
            {
                var failedFarm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
                error=error??(failedFarm!=null?failedFarm.InitializationError:null)??"Destination scene needs an initialized FarmGame.";
                pendingScene=origin; pendingArrival=null; pendingPosition=null; pendingRespawn=false;
                operation=null;
                try { operation=SceneManager.LoadSceneAsync(origin,LoadSceneMode.Single); }
                catch(Exception problem) { error+=" Return failed: "+problem.Message; }
                if(operation!=null) yield return operation;
            }
            Busy=false;
            if(current!=null && current.gameObject.scene==SceneManager.GetActiveScene()) Save(current);
        }
        void OnGUI()
        {
            if(Busy) GUI.Box(new Rect(Screen.width/2-100,Screen.height/2-24,200,48),"Saving / travelling...");
            if(string.IsNullOrEmpty(error)) return;
            var rect=new Rect((Screen.width-500)*.5f,80,500,132); GUI.Box(rect,"Save / map travel");
            GUI.Label(new Rect(rect.x+16,rect.y+32,468,64),error,new GUIStyle(GUI.skin.label) {wordWrap=true});
            if(!blocked && current!=null && GUI.Button(new Rect(rect.x+16,rect.yMax-32,110,24),"저장 재시도"))Save(current);
            if(blocked && GameMenu.Instance!=null && GUI.Button(new Rect(rect.x+16,rect.yMax-32,110,24),"타이틀로"))GameMenu.Instance.ReturnToTitle(false);
            if(!blocked && GUI.Button(new Rect(rect.xMax-90,rect.yMax-32,75,24),"닫기")) error=null;
        }
    }
}
