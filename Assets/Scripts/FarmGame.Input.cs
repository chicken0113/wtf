using UnityEngine;

namespace EverythingGrows
{
    public sealed partial class FarmGame
    {
        // Return true when the input belongs to a menu, so the same key cannot act on the world.
        public bool ProcessMenuInput(bool inventoryPressed,bool escapePressed,bool mapPressed,bool closeInteractionPressed=false)
        {
            if(WorldInputBlocked||inventoryPressed||mapPressed)suppressPrimaryUntilRelease=true;
            if(inventoryPressed)
            {
                bool next=!InventoryOpen; shop?.Close(); showMap=false; SetInventoryOpen(next);
                HideFarmingPreview(); return true;
            }
            if(escapePressed && WorldInputBlocked)
            {
                shop?.Close(); SetInventoryOpen(false); showMap=false; HideFarmingPreview(); return true;
            }
            if(mapPressed && !InventoryOpen)
            { showMap=!showMap; HideFarmingPreview(); return true; }
            if(WorldInputBlocked)
            {
                shop?.CheckDistance();
                if(closeInteractionPressed && shop!=null && shop.IsOpen) { shop.Close(); SetInventoryOpen(false); }
                HideFarmingPreview(); return true;
            }
            return false;
        }
        private void Update()
        {
            frameMovement=Vector2.zero;
            if(player==null) return;
            if(combat.IsGameOver || GameSession.Busy || !string.IsNullOrEmpty(GameSession.LastError)) { HideFarmingPreview(); return; }
            var guiMouse=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
            bool secondaryPressed=Input.GetMouseButtonDown(1);
            if(secondaryPressed && WorldInputBlocked) { ProcessSecondaryPointer(MousePoint,guiMouse);return; }
            if(ProcessMenuInput(Input.GetKeyDown(KeyCode.Tab),Input.GetKeyDown(KeyCode.Escape),Input.GetKeyDown(KeyCode.M))) return;
            if(MapExit.TryAutomatic(this)) { HideFarmingPreview(); return; }
            GameSession.AdvanceTime(Time.deltaTime);
            Cycle.Tick(Time.deltaTime);
            Vector2 move=MovementInput(Input.GetKey(KeyCode.A),Input.GetKey(KeyCode.D),Input.GetKey(KeyCode.W),Input.GetKey(KeyCode.S));
            frameMovement=move;
            UpdateAim(MousePoint,OverUI());
            if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.LeftShift)) combat.RequestRoll(DodgeDirection(move));
            if(!combat.IsRolling) MovePlayer(PlayerPosition+move*MoveSpeed*Time.deltaTime);
            combat.TickCombat(Time.deltaTime);
            if(GameSession.Busy) { HideFarmingPreview(); return; }
            UpdateAim(MousePoint,OverUI());
            for(int i=0;i<HotbarCapacity;i++) if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))) selected=i;
            float wheel=Input.mouseScrollDelta.y;
            if(wheel!=0) selected=(selected+(wheel>0?-1:1)+HotbarCapacity)%HotbarCapacity;
            Tick(Time.deltaTime);
            if(Input.GetKeyDown(KeyCode.R)) RestoreSlot();
            if(Input.GetKeyDown(KeyCode.Q) && Held!=null) { var item=Held; RemoveInventoryItem(selected); Spawn(item,player.position+(Vector3)Vector2.right*.6f); }
            bool pointerOverUI=OverUI();
            bool primaryAllowed=ProcessPrimaryPointer(Input.GetMouseButton(0),pointerOverUI);
            if(secondaryPressed) { ProcessSecondaryPointer(MousePoint,guiMouse);return; }
            UpdatePlantingPreview();
            if(OverUI()) return;
            var point=MousePoint; var cell=MouseCell;
            if(Held!=null && Held.effect==GrowthEffect.Weapon && primaryAllowed)
                if(combat.Attack(Held,PlayerPosition+AimDirection)) actionTime=.25f;
            if(!Near(point)) { if((Input.GetMouseButtonDown(0) && (Held==null || Held.effect!=GrowthEffect.Weapon))) message="Walk closer first."; return; }
            if(primaryAllowed && (Held==null||Held.effect!=GrowthEffect.Weapon))
            {
                if(Held==null){if(Input.GetMouseButtonDown(0))message="Select a hoe to till soil.";}
                else if(Held.effect==GrowthEffect.Dig || Held.effect==GrowthEffect.Water || Held.effect==GrowthEffect.Scythe || Held.effect==GrowthEffect.Heart || Held.effect==GrowthEffect.House)
                {if(UsePrimaryTool(cell,Input.GetMouseButtonDown(0)))actionTime=.25f;}
                else if(Input.GetMouseButtonDown(0))message="This item has no direct use. Right click to plant it.";
            }
        }
    }
}
