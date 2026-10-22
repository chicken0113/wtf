using System;
using System.Collections.Generic;
using UnityEngine;
namespace EverythingGrows
{
    [Serializable] public sealed class ShopOffer
    {
        public PlantDefinition item;
        [Min(0)] public int buyPrice=10;
        [Tooltip("-1 uses the item's Sell Price.")] public int sellPrice=-1;
    }
    [Serializable] public sealed class NpcProfile
    {
        public string id="farm_merchant",displayName="Farm merchant";
        public bool shopEnabled=true;
        public bool guildQuestGiver;
        [TextArea] public string greeting="Welcome. Take a look around.";
        public List<ShopOffer> offers=new();
    }
    public sealed class NpcManager : MonoBehaviour
    {
        [Min(0)] public int startingGold=100;
        public List<NpcProfile> npcs=new();
        private FarmGame farm;
        private FarmArt art;
        private ShopNpc activeNpc;
        private NpcProfile activeProfile;
        private Vector2 scroll;
        public int Gold { get; private set; }
        public bool CanTrade => farm!=null && !farm.Combat.IsGameOver && (farm.Cycle==null || farm.Cycle.ShopsOpen);
        public bool IsOpen => activeNpc!=null && CanInteract(activeNpc) && activeNpc.IsVisiting && activeProfile!=null;
        public bool IsTrading => IsOpen && CanTrade && activeProfile.shopEnabled;
        public bool IsQuestDesk => IsOpen && activeProfile.guildQuestGiver;
        private bool CanInteract(ShopNpc npc) => farm!=null && !farm.Combat.IsGameOver && npc!=null && npc.isActiveAndEnabled && (!npc.daytimeOnly || CanTrade);
        public void UpdateAvailability()
        {
            foreach(var npc in farm.GetComponentsInChildren<ShopNpc>()) npc.SetVisiting(CanInteract(npc));
            if(activeNpc!=null && !activeNpc.IsVisiting) Close();
        }
        public void Initialize(FarmGame owner) { if(farm!=null) return; farm=owner; Gold=Mathf.Max(0,startingGold); art=new FarmArt(); }
        public void RestoreGold(int gold) { Gold=Mathf.Max(0,gold); }
        public bool Open(ShopNpc npc)
        {
            if(!CanInteract(npc) || !npc.IsVisiting || Vector2.Distance(farm.PlayerPosition,npc.transform.position)>npc.interactionRange) return false;
            var profile=npcs.Find(entry=>entry!=null && entry.id==npc.npcId); if(profile==null) { farm.Notify("This NPC has no shop profile."); return false; }
            activeNpc=npc; activeProfile=profile; scroll=Vector2.zero; return true;
        }
        public bool OpenNearby()
        {
            ShopNpc nearest=null; float distance=float.PositiveInfinity;
            foreach(var npc in farm.GetComponentsInChildren<ShopNpc>())
            { float d=Vector2.Distance(farm.PlayerPosition,npc.transform.position); if(CanInteract(npc) && npc.IsVisiting && d<=npc.interactionRange && d<distance) { nearest=npc; distance=d; } }
            return Open(nearest);
        }
        public void Close() { activeNpc=null; activeProfile=null; }
        public void CheckDistance() { if(IsOpen && Vector2.Distance(farm.PlayerPosition,activeNpc.transform.position)>activeNpc.interactionRange) Close(); }
        public bool Buy(int index)
        {
            if(!IsTrading || index<0 || index>=activeProfile.offers.Count) return false;
            var offer=activeProfile.offers[index]; if(offer==null || offer.item==null || offer.buyPrice<0) return false;
            if(Gold<offer.buyPrice) { farm.Notify("Not enough gold."); return false; }
            if(!farm.AddItem(offer.item)) return false;
            Gold-=offer.buyPrice; farm.Notify("Bought "+offer.item.label+"."); return true;
        }
        public int SalePrice(PlantDefinition item)
        {
            if(item==null) return 0;
            var offer=activeProfile?.offers.Find(entry=>entry!=null && entry.item==item);
            return Mathf.Max(0,offer!=null && offer.sellPrice>=0?offer.sellPrice:item.sellPrice);
        }
        public bool Sell(int index)
        {
            if(!IsTrading) return false;
            var item=farm.InventoryItem(index); if(item==null) return false;
            int price=SalePrice(item); if(Gold>int.MaxValue-price) return false;
            if(!farm.RemoveInventoryItem(index)) return false;
            Gold+=price; farm.Notify("Sold "+item.label+" for "+price+" gold."); return true;
        }
        private void OnGUI()
        {
            if(farm==null || farm.Combat.IsGameOver || farm.MapOpen) return;
            if(!IsOpen)
            {
                foreach(var npc in farm.GetComponentsInChildren<ShopNpc>()) if(CanInteract(npc) && npc.IsVisiting && Vector2.Distance(farm.PlayerPosition,npc.transform.position)<=npc.interactionRange)
                {
                    var position=Camera.main.WorldToScreenPoint(npc.transform.position+Vector3.up*.7f);
                    var profile=npcs.Find(entry=>entry!=null && entry.id==npc.npcId);
                    GUI.Label(new Rect(position.x-90,Screen.height-position.y,180,24),profile!=null&&profile.guildQuestGiver?"E: guild requests":profile!=null && profile.shopEnabled && CanTrade?"E: open shop":"E: talk");
                }
                return;
            }
            if(!IsTrading)
            {
                if(IsQuestDesk) { DrawGuildQuests(); return; }
                float dialogueWidth=Mathf.Min(480,Screen.width-32); var box=new Rect((Screen.width-dialogueWidth)*.5f,Screen.height-210,dialogueWidth,150);
                GUI.Box(box,activeProfile.displayName);
                GUI.Label(new Rect(box.x+16,box.y+32,box.width-32,70),activeProfile.greeting,new GUIStyle(GUI.skin.label) {wordWrap=true,fontSize=15});
                if(GUI.Button(new Rect(box.xMax-90,box.yMax-36,75,26),"Close")) Close();
                return;
            }
            float width=Mathf.Min(400,Screen.width*.46f),height=Mathf.Min(350,Screen.height-120);
            Rect panel=new Rect(Screen.width*.5f-width-8,farm.InventoryPanelRect.y,width,height);
            GUI.Box(panel,activeProfile.displayName);
            if(GUI.Button(new Rect(panel.xMax-65,panel.y+4,56,24),"Close")) { Close(); return; }
            GUI.Label(new Rect(panel.x+12,panel.y+29,width-24,22),"RMB item: sell | Tab / E / Esc: close");
            Rect viewport=new Rect(panel.x+10,panel.y+58,width-20,height-68);
            scroll=GUI.BeginScrollView(viewport,scroll,new Rect(0,0,width-40,activeProfile.offers.Count*48));
            for(int i=0;i<activeProfile.offers.Count;i++)
            {
                var offer=activeProfile.offers[i]; if(offer==null || offer.item==null) continue;
                float y=i*48; art.ItemIcon(new Rect(4,y+2,40,40),offer.item);
                GUI.Label(new Rect(50,y+10,width-190,26),offer.item.label+"  |  "+offer.buyPrice+" G");
                var previous=GUI.enabled; GUI.enabled=Gold>=offer.buyPrice && farm.CanAddItem(offer.item);
                if(GUI.Button(new Rect(width-135,y+6,82,30),"Buy")) Buy(i);
                GUI.enabled=previous;
            }
            GUI.EndScrollView();
        }
        private void OnDestroy() { art?.Dispose(); }
        private void DrawGuildQuests()
        {
            float width=Mathf.Min(590,Screen.width-32),height=Mathf.Min(510,Screen.height-72);
            var panel=new Rect((Screen.width-width)*.5f,(Screen.height-height)*.5f,width,height);
            GUI.Box(panel,activeProfile.displayName+" - Guild requests");
            if(GUI.Button(new Rect(panel.xMax-70,panel.y+5,60,25),"Close")) { Close(); return; }
            GUI.Label(new Rect(panel.x+16,panel.y+34,width-32,44),"Accept a request, defeat dungeon monsters, then return here for your reward.",new GUIStyle(GUI.skin.label){wordWrap=true});
            var catalog=Resources.Load<GuildQuestCatalog>("GuildQuests");
            if(catalog==null){GUI.Label(new Rect(panel.x+16,panel.y+90,width-32,30),"No requests posted.");return;}
            var viewport=new Rect(panel.x+12,panel.y+85,width-24,height-97);
            scroll=GUI.BeginScrollView(viewport,scroll,new Rect(0,0,width-48,catalog.quests.Count*150));
            for(int i=0;i<catalog.quests.Count;i++)
            {
                var quest=catalog.quests[i];if(quest==null)continue;
                var state=GameSession.GuildQuestState(quest.id);float y=i*150;float rowWidth=width-54;
                GUI.Box(new Rect(0,y,rowWidth,140),"");
                GUI.Label(new Rect(12,y+8,rowWidth-24,24),quest.title,new GUIStyle(GUI.skin.label){fontStyle=FontStyle.Bold,fontSize=16});
                GUI.Label(new Rect(12,y+34,rowWidth-24,42),quest.description,new GUIStyle(GUI.skin.label){wordWrap=true});
                int required=state!=null&&state.accepted?state.requiredKills:quest.requiredKills;
                int reward=state!=null&&state.accepted?state.rewardGold:quest.rewardGold;
                bool done=state!=null&&state.accepted&&state.progress>=required;
                int progress=state!=null&&state.accepted?state.progress:!quest.repeatable&&state!=null&&state.completedRuns>0?required:0;
                GUI.Label(new Rect(12,y+83,rowWidth-24,24),$"{progress} / {required}  |  Reward: {reward} G"+(quest.repeatable?"  |  Repeatable":""));
                if(state!=null&&state.accepted)
                {
                    if(done){if(GUI.Button(new Rect(rowWidth-140,y+106,128,25),"Claim reward"))GameSession.ClaimGuildQuest(farm,quest.id);}
                    else GUI.Label(new Rect(rowWidth-140,y+106,128,25),"In progress");
                }
                else if(!quest.repeatable&&state!=null&&state.completedRuns>0)GUI.Label(new Rect(rowWidth-140,y+106,128,25),"Completed");
                else if(GUI.Button(new Rect(rowWidth-140,y+106,128,25),"Accept"))GameSession.AcceptGuildQuest(farm,quest.id);
            }
            GUI.EndScrollView();
        }
    }
}
