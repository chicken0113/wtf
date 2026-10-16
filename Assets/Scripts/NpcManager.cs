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
        public bool IsOpen => CanTrade && activeNpc!=null && activeNpc.IsVisiting && activeProfile!=null;
        public void UpdateAvailability()
        {
            bool visiting=CanTrade;
            foreach(var npc in farm.GetComponentsInChildren<ShopNpc>()) npc.SetVisiting(visiting);
            if(!visiting) Close();
        }
        public void Initialize(FarmGame owner) { if(farm!=null) return; farm=owner; Gold=Mathf.Max(0,startingGold); art=new FarmArt(); }
        public void RestoreGold(int gold) { Gold=Mathf.Max(0,gold); }
        public bool Open(ShopNpc npc)
        {
            if(!CanTrade || npc==null || !npc.IsVisiting || !npc.isActiveAndEnabled || Vector2.Distance(farm.PlayerPosition,npc.transform.position)>npc.interactionRange) return false;
            var profile=npcs.Find(entry=>entry!=null && entry.id==npc.npcId); if(profile==null) { farm.Notify("This NPC has no shop profile."); return false; }
            activeNpc=npc; activeProfile=profile; scroll=Vector2.zero; return true;
        }
        public bool OpenNearby()
        {
            if(!CanTrade) return false;
            ShopNpc nearest=null; float distance=float.PositiveInfinity;
            foreach(var npc in farm.GetComponentsInChildren<ShopNpc>())
            { float d=Vector2.Distance(farm.PlayerPosition,npc.transform.position); if(npc.IsVisiting && npc.isActiveAndEnabled && d<=npc.interactionRange && d<distance) { nearest=npc; distance=d; } }
            return Open(nearest);
        }
        public void Close() { activeNpc=null; activeProfile=null; }
        public void CheckDistance() { if(IsOpen && Vector2.Distance(farm.PlayerPosition,activeNpc.transform.position)>activeNpc.interactionRange) Close(); }
        public bool Buy(int index)
        {
            if(!IsOpen || index<0 || index>=activeProfile.offers.Count) return false;
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
            if(!IsOpen) return false;
            var item=farm.InventoryItem(index); if(item==null) return false;
            int price=SalePrice(item); if(Gold>int.MaxValue-price) return false;
            if(!farm.RemoveInventoryItem(index)) return false;
            Gold+=price; farm.Notify("Sold "+item.label+" for "+price+" gold."); return true;
        }
        private void OnGUI()
        {
            if(farm==null || farm.Combat.IsGameOver) return;
            if(!IsOpen)
            {
                foreach(var npc in farm.GetComponentsInChildren<ShopNpc>()) if(CanTrade && npc.IsVisiting && Vector2.Distance(farm.PlayerPosition,npc.transform.position)<=npc.interactionRange)
                {
                    var position=Camera.main.WorldToScreenPoint(npc.transform.position+Vector3.up*.7f);
                    GUI.Label(new Rect(position.x-60,Screen.height-position.y,120,24),"E: open shop");
                }
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
    }
}
