using System;
using System.Linq;
using UnityEngine;
namespace EverythingGrows
{
    public sealed partial class GameSession
    {
        // Return a copy: editor/UI callers cannot mutate live save data.
        public static GuildQuestSave GuildQuestState(string id)
        {
            var state=data?.guildQuests?.Find(q=>q.id==id);
            return state==null?null:JsonUtility.FromJson<GuildQuestSave>(JsonUtility.ToJson(state));
        }
        static bool AtGuild(FarmGame farm)=>Enabled&&!blocked&&!Busy&&current==farm&&farm!=null&&farm.Shop!=null&&farm.Shop.IsQuestDesk&&!farm.Combat.IsGameOver;
        public static bool AcceptGuildQuest(FarmGame farm,string id)
        {
            if(!AtGuild(farm))return false;
            var catalog=Resources.Load<GuildQuestCatalog>("GuildQuests");
            var quest=catalog!=null?catalog.quests.Find(q=>q!=null&&q.id==id):null;
            if(quest==null||quest.requiredKills<1||quest.rewardGold<0||string.IsNullOrWhiteSpace(id)||string.IsNullOrEmpty(quest.targetScene))return false;
            var old=data.guildQuests.Find(q=>q.id==id);
            if(old!=null&&(old.accepted||(!quest.repeatable&&old.completedRuns>0)))return false;
            var state=new GuildQuestSave{id=id,targetScene=quest.targetScene,enemyKind=quest.enemyKind,requiredKills=quest.requiredKills,rewardGold=quest.rewardGold,accepted=true,completedRuns=old?.completedRuns??0};
            if(old!=null)data.guildQuests.Remove(old);data.guildQuests.Add(state);
            if(Save(farm)){farm.Notify("Request accepted: "+quest.title);return true;}
            data.guildQuests.Remove(state);if(old!=null)data.guildQuests.Add(old);return false;
        }
        public static void RecordGuildKill(FarmGame farm,bool ranged)
        {
            if(!Enabled||blocked||Busy||data==null||farm!=current)return;
            foreach(var quest in data.guildQuests)
                if(quest.accepted&&quest.progress<quest.requiredKills&&quest.targetScene==farm.gameObject.scene.path
                    &&(quest.enemyKind==GuildEnemyKind.Any||quest.enemyKind==(ranged?GuildEnemyKind.Ranged:GuildEnemyKind.Melee)))
                {quest.progress++;farm.Notify("Guild request: "+quest.progress+" / "+quest.requiredKills);}
        }
        public static bool ClaimGuildQuest(FarmGame farm,string id)
        {
            if(!AtGuild(farm))return false;
            var state=data.guildQuests.Find(q=>q.id==id);
            if(state==null||!state.accepted||state.progress<state.requiredKills||state.completedRuns==int.MaxValue||farm.Gold>int.MaxValue-state.rewardGold)return false;
            var old=JsonUtility.FromJson<GuildQuestSave>(JsonUtility.ToJson(state));int gold=farm.Gold;
            state.accepted=false;state.progress=0;state.completedRuns++;farm.Shop.RestoreGold(gold+state.rewardGold);
            if(Save(farm)){farm.Notify("Guild reward: "+old.rewardGold+" gold.");return true;}
            farm.Shop.RestoreGold(gold);data.guildQuests.Remove(state);data.guildQuests.Add(old);return false;
        }
        static void ValidateGuildQuests(GameSaveData save)
        {
            if(save.guildQuests==null||save.guildQuests.Any(q=>q==null||string.IsNullOrWhiteSpace(q.id)||string.IsNullOrWhiteSpace(q.targetScene)||!Enum.IsDefined(typeof(GuildEnemyKind),q.enemyKind)
                ||q.requiredKills<1||q.rewardGold<0||q.progress<0||q.progress>q.requiredKills||q.completedRuns<0)
                ||save.guildQuests.GroupBy(q=>q.id).Any(g=>g.Count()>1))throw new System.IO.InvalidDataException("Invalid guild request save.");
        }
    }
}
