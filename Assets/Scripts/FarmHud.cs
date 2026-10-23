using UnityEngine;

namespace EverythingGrows
{
    // HUD presentation and UI hit regions. Slot contents are changed only through InventoryState.
    internal sealed class FarmHud
    {
        private readonly FarmGame farm;
        private readonly FarmArt art;
        private readonly Camera view;
        private GUIStyle titleStyle,smallStyle,tooltipStyle;
        private Vector2 inventoryScroll,dragOrigin;
        private int dragSource=-1,hoveredSlot=-1,dragQuantity,dragSourceQuantity;
        private PlantDefinition dragItem;
        private bool dragging,slotClipContainsPointer=true;
        public FarmHud(FarmGame farm,FarmArt art,Camera view) { this.farm=farm;this.art=art;this.view=view; }
        private int Columns(float width)=>Mathf.Max(1,Mathf.FloorToInt((width-42)/58));
        public Rect InventoryPanelRect
        {
            get
            {
                bool trading=farm.Shop!=null && farm.Shop.IsTrading;
                float width=trading?Mathf.Min(400,Screen.width*.46f):Mathf.Min(Screen.width-24,Mathf.Max(460,farm.HotbarCapacity*58+42));
                int columns=Columns(width);
                float rows=Mathf.Ceil(farm.StorageCapacity/(float)columns)+Mathf.Ceil(farm.HotbarCapacity/(float)columns);
                float height=Mathf.Min(Screen.height-100,Mathf.Max(330,180+rows*58));
                return new Rect(trading?Screen.width*.5f+8:(Screen.width-width)*.5f,Mathf.Max(40,(Screen.height-height)*.5f),width,height);
            }
        }
        public Rect SlotRect(int index)
        {
            int columns=Mathf.Max(1,Mathf.Min(farm.HotbarCapacity,(Screen.width-24)/56));
            return new Rect((Screen.width-columns*56)/2+(index%columns)*56,Screen.height-72-(index/columns)*56,52,52);
        }
        public bool OverUI(Vector2 point)
        {
            if(farm.InventoryOpen || (farm.Combat!=null && farm.Combat.HudRect.Contains(point)))return true;
            if(farm.MapOpen && MapRect.Contains(point))return true;
            for(int i=0;i<farm.HotbarCapacity;i++)if(SlotRect(i).Contains(point))return true;
            return false;
        }
        private void Panel(Rect rect,Color color)
        {
            var previous=GUI.color;GUI.color=new Color(.24f,.13f,.08f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=color;GUI.DrawTexture(new Rect(rect.x+3,rect.y+3,rect.width-6,rect.height-6),Texture2D.whiteTexture);GUI.color=previous;
        }
        private void CancelDrag() { dragSource=-1;dragItem=null;dragQuantity=dragSourceQuantity=0;dragging=false; }
        public void Draw()
        {
            hoveredSlot=-1;
            if(farm.Combat==null || farm.Combat.IsGameOver) { CancelDrag();return; }
            titleStyle ??= new GUIStyle(GUI.skin.label) {fontSize=16,fontStyle=FontStyle.Bold};
            smallStyle ??= new GUIStyle(GUI.skin.label) {fontSize=12,alignment=TextAnchor.MiddleCenter};
            if(farm.InventoryOpen)
            {
                if(farm.PersonalInventoryOpen || (farm.Shop!=null && farm.Shop.IsTrading))DrawInventoryWindow();else CancelDrag();
                return;
            }
            CancelDrag();
            if(farm.MapOpen) { DrawMap();return; }
            for(int i=0;i<farm.HotbarCapacity;i++)if(DrawSlot(SlotRect(i),i,true))break;
            DrawTooltip(Event.current.mousePosition);
            foreach(var piece in farm.WorldPieces)if(piece.model!=null)
            { var position=view.WorldToScreenPoint(piece.model.transform.position);GUI.Label(new Rect(position.x-60,Screen.height-position.y+12,120,22),"RMB: "+piece.definition.label); }
        }
        private void DrawInventoryWindow()
        {
            if(dragSource>=0 && (dragSource>=farm.Capacity || farm.InventoryItem(dragSource)!=dragItem || farm.InventoryQuantity(dragSource)!=dragSourceQuantity))CancelDrag();
            hoveredSlot=-1;var mouse=Event.current.mousePosition;var panel=InventoryPanelRect;Panel(panel,new Color(.48f,.3f,.17f,.98f));
            GUI.Label(new Rect(panel.x+16,panel.y+10,panel.width-70,28),"Inventory",titleStyle);
            if(GUI.Button(new Rect(panel.xMax-42,panel.y+8,30,26),"X")) { CancelDrag();farm.Shop?.Close();farm.SetInventoryOpen(false);return; }
            GUI.Label(new Rect(panel.x+16,panel.y+40,panel.width-32,24),"Gold: "+farm.Gold+" G");
            GUI.Label(new Rect(panel.x+16,panel.y+64,panel.width-32,24),"Farming Lv. "+farm.FarmingLevel+"   "+
                (farm.FarmingExperienceToNextLevel==0?"MAX":farm.FarmingExperience+" / "+farm.FarmingExperienceToNextLevel+" XP"));
            int columns=Columns(panel.width);float hotbarHeight=Mathf.Ceil(farm.HotbarCapacity/(float)columns)*58;
            float hotbarY=panel.yMax-16-hotbarHeight;
            GUI.Label(new Rect(panel.x+16,panel.y+90,panel.width-32,22),"Backpack");
            var viewport=new Rect(panel.x+12,panel.y+114,panel.width-24,Mathf.Max(30,hotbarY-panel.y-144));
            var content=new Rect(0,0,viewport.width-18,Mathf.Max(30,Mathf.Ceil(farm.StorageCapacity/(float)columns)*58));
            slotClipContainsPointer=viewport.Contains(mouse);
            inventoryScroll=GUI.BeginScrollView(viewport,inventoryScroll,content);
            for(int i=0;i<farm.StorageCapacity;i++)
                DrawSlot(new Rect(i%columns*58,i/columns*58,52,52),farm.HotbarCapacity+i,false);
            if(farm.StorageCapacity==0)GUI.Label(new Rect(0,0,content.width,25),"No additional storage slots.");
            GUI.EndScrollView();slotClipContainsPointer=true;
            GUI.Label(new Rect(panel.x+16,hotbarY-26,panel.width-32,22),"Hotbar");
            for(int i=0;i<farm.HotbarCapacity;i++)
                DrawSlot(new Rect(panel.x+12+i%columns*58,hotbarY+i/columns*58,52,52),i,true);
            UpdateDrag(mouse);
            DrawTooltip(mouse);
        }
        private void UpdateDrag(Vector2 mouse)
        {
            if(dragSource<0)return;
            if(dragSource>=farm.Capacity || farm.InventoryItem(dragSource)!=dragItem || farm.InventoryQuantity(dragSource)!=dragSourceQuantity)
            { CancelDrag();return; }
            if(Event.current.type==EventType.MouseDrag && Event.current.button==0)
            { dragging|=Vector2.Distance(GUIUtility.GUIToScreenPoint(mouse),dragOrigin)>5;Event.current.Use(); }
            if(Event.current.rawType==EventType.MouseUp && Event.current.button==0)
            {
                bool moved=dragging || Vector2.Distance(GUIUtility.GUIToScreenPoint(mouse),dragOrigin)>5;
                if(moved)
                {
                    if(hoveredSlot>=0)farm.MoveInventoryItem(dragSource,hoveredSlot,dragQuantity);
                    else if(!InventoryPanelRect.Contains(mouse) && !(farm.Shop!=null && farm.Shop.ContainsUI(mouse)))
                        farm.DropInventoryItems(dragSource,dragQuantity);
                }
                CancelDrag();Event.current.Use();return;
            }
            if(dragging && Event.current.type==EventType.Repaint)
            {
                var previous=GUI.color;GUI.color=new Color(1,1,1,.85f);
                art.ItemIcon(new Rect(mouse-Vector2.one*22,Vector2.one*44),dragItem);GUI.color=previous;
                if(dragQuantity>1)GUI.Label(new Rect(mouse.x+6,mouse.y+8,42,20),dragQuantity.ToString(),smallStyle);
            }
        }
        // Both inventory regions and the closed hotbar share the same UI actions.
        private bool DrawSlot(Rect rect,int index,bool hotbar)
        {
            bool inside=slotClipContainsPointer && rect.Contains(Event.current.mousePosition);
            if(inside)hoveredSlot=index;
            Panel(rect,index==farm.SelectedIndex?new Color(.85f,.64f,.3f):new Color(.55f,.36f,.2f));
            var item=farm.InventoryItem(index);
            if(item!=null)
            {
                if(!hotbar || art.Available)art.ItemIcon(new Rect(rect.x+6,rect.y+5,40,40),item);
                else GUI.Label(rect,item.label,smallStyle);
            }
            if(hotbar)GUI.Label(new Rect(rect.x+4,rect.y+1,16,16),(index+1).ToString());
            int quantity=farm.InventoryQuantity(index);
            if(dragging && index==dragSource)quantity-=dragQuantity;
            if(quantity>1)GUI.Label(new Rect(rect.x+26,rect.y+1,24,18),quantity.ToString(),smallStyle);
            if(farm.Shop!=null && farm.Shop.IsTrading && item!=null)GUI.Label(new Rect(rect.x,rect.y+34,52,18),farm.Shop.SalePrice(item)+" G",smallStyle);
            if(Event.current.type!=EventType.MouseDown || !inside)return false;
            if(Event.current.button==1 && farm.SecondaryPointerConsumed) { Event.current.Use();return true; }
            if(Event.current.button==0)
            {
                farm.SelectSlot(index);
                if(farm.InventoryOpen && item!=null)
                {
                    dragSource=index;dragItem=item;dragSourceQuantity=farm.InventoryQuantity(index);
                    dragQuantity=Event.current.shift && item.stackable?1+(dragSourceQuantity-1)/2:dragSourceQuantity;
                    dragOrigin=GUIUtility.GUIToScreenPoint(Event.current.mousePosition);dragging=false;
                }
            }
            else if(Event.current.button==1)
            { CancelDrag();if(farm.Shop!=null && farm.Shop.IsTrading)farm.Shop.Sell(index);else farm.DetachSlot(); }
            else return false;
            Event.current.Use();return true;
        }
        private void DrawTooltip(Vector2 mouse)
        {
            if(Event.current.type!=EventType.Repaint || dragSource>=0 || hoveredSlot<0)return;
            var item=farm.InventoryItem(hoveredSlot);if(item==null)return;
            tooltipStyle ??= new GUIStyle(GUI.skin.box) {fontSize=14,wordWrap=true,alignment=TextAnchor.UpperLeft,padding=new RectOffset(10,10,8,8)};
            var content=new GUIContent((string.IsNullOrWhiteSpace(item.label)?item.name:item.label)+"\nQuantity: "+farm.InventoryQuantity(hoveredSlot)+"\nPlanting level: "+item.requiredFarmingLevel+"\n\n"+item.UsageText);
            float width=Mathf.Min(300,Screen.width-16),height=tooltipStyle.CalcHeight(content,width);
            var rect=new Rect(Mathf.Clamp(mouse.x+18,8,Screen.width-width-8),Mathf.Clamp(mouse.y+18,8,Mathf.Max(8,Screen.height-height-8)),width,height);
            int depth=GUI.depth;GUI.depth=-20;
            var color=GUI.color;GUI.color=new Color(.08f,.07f,.05f,.98f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=color;
            tooltipStyle.normal.textColor=Color.white;GUI.Box(rect,content,tooltipStyle);GUI.depth=depth;
        }
        private Rect MapRect=>new Rect(Screen.width/2-170,Screen.height/2-120,340,240);
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
