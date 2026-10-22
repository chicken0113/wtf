using UnityEngine;

namespace EverythingGrows
{
    // HUD presentation and UI hit regions. Gameplay changes go through FarmGame APIs.
    internal sealed class FarmHud
    {
        private readonly FarmGame farm;
        private readonly FarmArt art;
        private readonly Camera view;
        private GUIStyle titleStyle, smallStyle, hintStyle, cropStyle;
        private Vector2 inventoryScroll;
        public FarmHud(FarmGame farm, FarmArt art, Camera view) { this.farm=farm;this.art=art;this.view=view; }
        public Rect InventoryPanelRect
        {
            get
            {
                float width=Mathf.Min(460,Screen.width-24);
                bool trading=farm.Shop!=null && farm.Shop.IsTrading;
                if(trading) width=Mathf.Min(400,Screen.width*.46f);
                float height=Mathf.Min(Screen.height-100,Mathf.Max(230,116+Mathf.CeilToInt(farm.Capacity/5f)*58));
                return new Rect(trading?Screen.width*.5f+8:(Screen.width-width)*.5f,Mathf.Max(60,(Screen.height-height)*.5f),width,height);
            }
        }
        public Rect SlotRect(int index) => new Rect((Screen.width-Mathf.Min(8,farm.Capacity)*56)/2+(index%8)*56,Screen.height-72-(index/8)*56,52,52);
        public bool OverUI(Vector2 point)
        {
            if(farm.InventoryOpen || (farm.Combat!=null && farm.Combat.HudRect.Contains(point))) return true;
            if(farm.MapOpen && MapRect.Contains(point)) return true;
            for(int i=0;i<farm.Capacity;i++) if(SlotRect(i).Contains(point)) return true;
            return false;
        }
        private void Panel(Rect rect,Color color)
        {
            var previous=GUI.color; GUI.color=new Color(.24f,.13f,.08f); GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=color; GUI.DrawTexture(new Rect(rect.x+3,rect.y+3,rect.width-6,rect.height-6),Texture2D.whiteTexture); GUI.color=previous;
        }
        public void Draw()
        {
            if(farm.Combat==null || farm.Combat.IsGameOver) return;
            titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize=16,fontStyle=FontStyle.Bold };
            smallStyle ??= new GUIStyle(GUI.skin.label) { fontSize=12,alignment=TextAnchor.MiddleCenter };
            if(farm.InventoryOpen) { if(farm.PersonalInventoryOpen || (farm.Shop!=null && farm.Shop.IsTrading)) DrawInventoryWindow(); return; }
            if(farm.MapOpen) { DrawMap(); return; }
            for(int i=0;i<farm.Capacity;i++)
            {
                Rect rect=SlotRect(i);
                if (DrawSlot(rect, i, true)) break;
            }
            DrawCropStates();
            DrawFarmingHints();
            foreach(var piece in farm.WorldPieces) if(piece.model!=null) { var position=view.WorldToScreenPoint(piece.model.transform.position); GUI.Label(new Rect(position.x-60,Screen.height-position.y+12,120,22),"E: "+piece.definition.label); }
        }
        // Status badges leave the planted item's sprite and color intact.
        private void DrawCropStates()
        {
            cropStyle ??= new GUIStyle(GUI.skin.label) { fontSize=10,alignment=TextAnchor.MiddleCenter,clipping=TextClipping.Clip,wordWrap=false };
            foreach(var plot in farm.Farming.Plots)
            {
                if(plot.plant==null) continue;
                Vector3 point=view.WorldToScreenPoint((Vector2)plot.cell+Vector2.up*.7f);
                if(point.z<=0 || point.x<0 || point.x>Screen.width || point.y<0 || point.y>Screen.height) continue;
                string status;
                Color color;
                if(plot.wilted) { status="DEAD"; color=new Color(.7f,.55f,.5f); }
                else if(plot.CanHarvest) { status="E: HARVEST"; color=new Color(.5f,1,.65f); }
                else if(plot.mature) { status="PRODUCING"; color=new Color(.65f,.85f,1); }
                else if(!plot.wet) { status="NEEDS WATER"; color=new Color(.5f,.8f,1); }
                else { status="GROWING "+Mathf.FloorToInt(plot.progress*100)+"%"; color=new Color(1,.85f,.4f); }
                float width=Mathf.Clamp(view.WorldToScreenPoint((Vector2)plot.cell+Vector2.right).x-point.x-4,42,110);
                if(width<70)
                {
                    if(!plot.wilted && plot.CanHarvest) status="E: PICK";
                    else if(!plot.wilted && plot.mature) status="WAIT";
                    else if(!plot.wilted && !plot.wet) status="WATER";
                    else if(!plot.wilted) status="GROW "+Mathf.FloorToInt(plot.progress*100)+"%";
                }
                var rect=new Rect(point.x-width*.5f,Screen.height-point.y-25,width,34);
                Color previous=GUI.color;
                GUI.color=new Color(.08f,.12f,.1f,.88f); GUI.DrawTexture(rect,Texture2D.whiteTexture);
                GUI.color=Color.white; GUI.Label(new Rect(rect.x,rect.y,rect.width,16),plot.plant.label,cropStyle);
                GUI.color=color; GUI.Label(new Rect(rect.x,rect.y+15,rect.width,16),status,cropStyle);
                if(!plot.mature && !plot.wilted)
                {
                    var bar=new Rect(rect.x,rect.yMax,rect.width,3);
                    GUI.color=new Color(.15f,.2f,.18f); GUI.DrawTexture(bar,Texture2D.whiteTexture);
                    GUI.color=color; GUI.DrawTexture(new Rect(bar.x,bar.y,bar.width*plot.progress,bar.height),Texture2D.whiteTexture);
                }
                GUI.color=previous;
            }
        }
        private void DrawFarmingHints()
        {
            if(!farm.ShowFarmingHints || !farm.WorldBounds.Contains(farm.FarmingPointer)) return;
            hintStyle ??= new GUIStyle(GUI.skin.label) { fontSize=12,wordWrap=true,padding=new RectOffset(8,8,5,5) };
            var action=farm.FarmHint(farm.FarmingPointer);
            var primary=farm.PrimaryFarmHint(farm.FarmingPointer);
            string actionText="E: "+action.Text;
            string primaryText=string.IsNullOrEmpty(primary.Text)?null:"LMB: "+primary.Text;
            float desiredWidth=Mathf.Max(hintStyle.CalcSize(new GUIContent(actionText)).x,primaryText==null?0:hintStyle.CalcSize(new GUIContent(primaryText)).x);
            float width=Mathf.Min(Screen.width-16,Mathf.Clamp(desiredWidth,160,300));
            float actionHeight=hintStyle.CalcHeight(new GUIContent(actionText),width);
            float primaryHeight=primaryText==null?0:hintStyle.CalcHeight(new GUIContent(primaryText),width);
            var mouse=Event.current.mousePosition;
            var rect=new Rect(Mathf.Clamp(mouse.x+18,8,Mathf.Max(8,Screen.width-width-8)),Mathf.Clamp(mouse.y+20,8,Mathf.Max(8,Screen.height-actionHeight-primaryHeight-8)),width,actionHeight+primaryHeight);
            Color previous=GUI.color;
            GUI.color=new Color(.06f,.1f,.09f,.94f); GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=action.Allowed?new Color(.6f,1,.7f):new Color(1,.72f,.55f);
            GUI.Label(new Rect(rect.x,rect.y,width,actionHeight),actionText,hintStyle);
            if(primaryText!=null)
            {
                GUI.color=primary.Allowed?new Color(.6f,1,.7f):new Color(1,.72f,.55f);
                GUI.Label(new Rect(rect.x,rect.y+actionHeight,width,primaryHeight),primaryText,hintStyle);
            }
            GUI.color=previous;
        }
        private void DrawInventoryWindow()
        {
            var panel=InventoryPanelRect; Panel(panel,new Color(.48f,.3f,.17f,.98f));
            GUI.Label(new Rect(panel.x+16,panel.y+10,panel.width-70,28),"Inventory",titleStyle);
            if(GUI.Button(new Rect(panel.xMax-42,panel.y+8,30,26),"X")) { farm.Shop?.Close(); farm.SetInventoryOpen(false); return; }
            GUI.Label(new Rect(panel.x+16,panel.y+40,panel.width-32,24),"Gold: "+farm.Gold+" G");
            var viewport=new Rect(panel.x+12,panel.y+76,panel.width-24,panel.height-90);
            int columns=Mathf.Max(1,Mathf.Min(5,Mathf.FloorToInt((viewport.width-18)/58)));
            var content=new Rect(0,0,viewport.width-18,Mathf.CeilToInt(farm.Capacity/(float)columns)*58);
            inventoryScroll=GUI.BeginScrollView(viewport,inventoryScroll,content);
            for(int i=0;i<farm.Capacity;i++)
            {
                var rect=new Rect(i%columns*58,i/columns*58,52,52);
                if (DrawSlot(rect, i, false)) break;
            }
            GUI.EndScrollView();
        }
        private Rect MapRect => new Rect(Screen.width/2-170,Screen.height/2-120,340,240);

        // Both inventory and hotbar use the same item drawing and click actions.
        private bool DrawSlot(Rect rect, int index, bool hotbar)
        {
            Panel(rect, index == farm.SelectedIndex ? new Color(.85f,.64f,.3f) : new Color(.55f,.36f,.2f));
            var item = farm.InventoryItem(index);
            if (item != null)
            {
                if (!hotbar || art.Available) art.ItemIcon(new Rect(rect.x+6,rect.y+5,40,40), item);
                else GUI.Label(rect, item.label, smallStyle);
            }
            if (hotbar) GUI.Label(new Rect(rect.x+4,rect.y+1,16,16), (index+1).ToString());
            int quantity = farm.InventoryQuantity(index);
            if (quantity > 1)
                GUI.Label(new Rect(rect.x+(hotbar?26:27),rect.y+(hotbar?1:2),24,hotbar?16:18), quantity.ToString(), smallStyle);
            if (farm.Shop != null && farm.Shop.IsTrading && item != null)
                GUI.Label(new Rect(rect.x,rect.y+(hotbar?35:34),52,hotbar?16:18), farm.Shop.SalePrice(item)+" G", smallStyle);
            if (Event.current.type != EventType.MouseDown || !rect.Contains(Event.current.mousePosition)) return false;
            if (Event.current.button == 0) farm.SelectSlot(index);
            else if (Event.current.button == 1)
            {
                if (farm.Shop != null && farm.Shop.IsTrading) farm.Shop.Sell(index);
                else farm.DetachSlot();
            }
            Event.current.Use();
            return true;
        }

        private void DrawMap()
        {
            Rect rect=MapRect;
            Panel(rect,new Color(.24f,.34f,.24f));
            var previous=GUI.color; GUI.color=new Color(.52f,.63f,.3f);
            float scale=Mathf.Min((rect.width-24)/farm.WorldBounds.width,(rect.height-48)/farm.WorldBounds.height);
            Vector2 origin=rect.center-farm.WorldBounds.center*new Vector2(scale,-scale);
            GUI.DrawTexture(new Rect(origin.x+farm.SafeBounds.xMin*scale,origin.y-farm.SafeBounds.yMax*scale,farm.SafeBounds.width*scale,farm.SafeBounds.height*scale),Texture2D.whiteTexture);
            GUI.color=Color.yellow; GUI.DrawTexture(new Rect(origin.x+farm.PlayerPosition.x*scale-3,origin.y-farm.PlayerPosition.y*scale-3,6,6),Texture2D.whiteTexture); GUI.color=previous;
            GUI.Label(new Rect(rect.x+12,rect.y+8,310,24),"M: close | Center: safe farm | Outside: enemies");
        }
    }
}
