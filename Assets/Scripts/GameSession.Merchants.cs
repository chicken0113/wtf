using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
namespace EverythingGrows
{
    public sealed partial class GameSession
    {
        static bool merchantStateDirty;
        internal static bool MerchantVisitsNeedSave=>merchantStateDirty;
        static void EnsureMerchantSeed(GameSaveData state)
        {
            if(state.merchantSeed!=0)return;
            merchantStateDirty=true;state.merchantSeed=Guid.NewGuid().GetHashCode();if(state.merchantSeed==0)state.merchantSeed=1;
        }
        static uint VisitHash(int seed,string text)
        {
            uint value=unchecked((uint)seed)^2166136261u;
            foreach(char c in text)value=unchecked((value^c)*16777619u);
            return value;
        }
        public static bool TryNightVisit(FarmGame farm,NpcProfile profile,Vector2 home,out Vector2 position)
        {
            position=home;
            if(data==null || current!=farm || blocked || farm.Cycle==null || !farm.Cycle.ShopsOpen)return false;
            string scene=farm.gameObject.scene.path;long night=farm.Cycle.CycleNumber;
            var visit=data.nightVisits.Find(v=>v.scene==scene && v.profileId==profile.id);
            if(visit==null || visit.cycleNumber!=night)
            {
                // A fixed world seed makes even a first visit late in the night reproducible.
                string key=scene+"\0"+profile.id+"\0"+night.ToString(CultureInfo.InvariantCulture);
                uint roll=VisitHash(data.merchantSeed,key);
                var collision=new MapCollision(farm);
                var points=(profile.visitLocations??Array.Empty<Transform>()).Where(t=>t!=null && collision.CanStand(t.position,.3f)).Select(t=>(Vector2)t.position).ToArray();
                bool appears=points.Length>0 && roll/4294967296.0<profile.visitChance;
                position=appears?points[VisitHash(data.merchantSeed,key+"position")%(uint)points.Length]:home;
                visit=new NightVisitSave {scene=scene,profileId=profile.id,cycleNumber=night,appears=appears,position=position};
                data.nightVisits.RemoveAll(v=>v.scene==scene && v.profileId==profile.id);data.nightVisits.Add(visit);merchantStateDirty=true;
            }
            position=visit.position;return visit.appears;
        }
        static void ValidateNightVisits(GameSaveData state)
        {
            state.nightVisits??=new List<NightVisitSave>();var keys=new HashSet<string>();
            foreach(var visit in state.nightVisits)
                if(visit==null || string.IsNullOrWhiteSpace(visit.scene) || string.IsNullOrWhiteSpace(visit.profileId) || visit.cycleNumber<0 || !SetupDataValidator.Finite(visit.position.x) || !SetupDataValidator.Finite(visit.position.y) || !keys.Add(visit.scene+"\0"+visit.profileId))throw new InvalidDataException("Invalid night merchant visit data.");
        }
    }
}
