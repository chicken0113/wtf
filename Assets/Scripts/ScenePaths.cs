namespace EverythingGrows
{
    // Folder organization only; scene GUIDs and gameplay identities do not change.
    public static class ScenePaths
    {
        public static readonly string[] Names={"MainMenu","DungeonEntrance","ForestClearing","TrainingGround","FirstGarden","FarmTownPassage","Town","GeneralStore","FarmHouseInterior","GuildInterior","SmithInterior","HospitalInterior","TowerFirstFloor","CombatTest"};
        public static string Folder(string name)=>name=="MainMenu"?"System":name=="DungeonEntrance" || name=="ForestClearing"?"OpenWorld":name=="TrainingGround"?"Tests":"LegacyVillage";
        public static string Original(string name)=>"Assets/Scenes/"+name+".unity";
        public static string Organized(string name)=>"Assets/Scenes/"+Folder(name)+"/"+name+".unity";
        public static string Resolve(string path)
        {
            foreach(var name in Names)if(path==Original(name))return Organized(name);
            return path;
        }
        // Run before validation, in memory only. The normal atomic autosave commits migrated data.
        public static void Migrate(GameSaveData save)
        {
            if(save==null)return;
            save.currentScene=Resolve(save.currentScene);save.houseReturnScene=Resolve(save.houseReturnScene);
            if(save.respawn!=null)save.respawn.scene=Resolve(save.respawn.scene);
            if(save.defaultRespawn!=null)save.defaultRespawn.scene=Resolve(save.defaultRespawn.scene);
            if(save.maps!=null)foreach(var map in save.maps)if(map!=null)map.scene=Resolve(map.scene);
            if(save.nightVisits!=null)foreach(var visit in save.nightVisits)if(visit!=null)visit.scene=Resolve(visit.scene);
            if(save.guildQuests!=null)foreach(var quest in save.guildQuests)if(quest!=null)quest.targetScene=Resolve(quest.targetScene);
        }
    }
}
