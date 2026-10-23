using System;
using UnityEngine;

namespace EverythingGrows
{
    // Invoked only by the ordinary enemy kill callback, never by save restoration.
    internal static class MonsterRewardSystem
    {
        public static void Grant(FarmGame farm,MonsterType type,Vector2 point)
        {
            if(type==null)throw new InvalidOperationException("Monster type is missing; restore the monster type asset before combat.");
            farm.AddRewardGold(type.gold);
            foreach(var drop in type.drops)
            {
                if(drop.probability<=0 || (drop.probability<1 && UnityEngine.Random.value>=drop.probability))continue;
                long range=(long)drop.maximum-drop.minimum+1;
                int quantity=(int)Math.Min(drop.maximum,(long)drop.minimum+(long)(UnityEngine.Random.value*(double)range));
                for(int i=0;i<quantity;i++)farm.DropItem(drop.item,point);
            }
        }
    }
}
