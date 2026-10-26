using UnityEngine;
namespace EverythingGrows
{
    public sealed partial class FarmGame
    {
        private bool suppressPrimaryUntilRelease;
        private PlayerControlSettings controlSettings;
        public PlayerControlSettings Controls=>controlSettings;
        public Vector2 AimDirection {get;private set;}=Vector2.down;
        public float MoveSpeed=>controlSettings!=null?Mathf.Max(.1f,controlSettings.moveSpeed):4.5f;
        public Vector2 DodgeDirection(Vector2 movement)=>movement.sqrMagnitude>.01f?movement.normalized:AimDirection;
        public void UpdateAim(Vector2 point,bool pointerOverUI)
        {
            if(pointerOverUI)return;
            var delta=point-PlayerPosition;if(delta.sqrMagnitude>.04f)AimDirection=delta.normalized;
        }
        public static Vector2 MovementInput(bool left,bool right,bool up,bool down)
            => Vector2.ClampMagnitude(new Vector2((right?1:0)-(left?1:0),(up?1:0)-(down?1:0)),1);

        // A UI click must finish before the same held button can use a world item.
        public bool ProcessPrimaryPointer(bool held,bool pointerOverUI)
        {
            if(!held){suppressPrimaryUntilRelease=false;toolCellValid=false;}
            else if(pointerOverUI)suppressPrimaryUntilRelease=true;
            return held&&!pointerOverUI&&!suppressPrimaryUntilRelease;
        }

        private int secondaryConsumedFrame=-1;
        internal bool SecondaryPointerConsumed => secondaryConsumedFrame==Time.frameCount;

        // One press routes to UI first, then pointed NPC/door, then existing farming rules.
        public bool ProcessSecondaryPointer(Vector2 point,Vector2 guiPoint)
        {
            if(player==null || combat==null || combat.IsGameOver || GameSession.Busy)return false;
            if(shop!=null && shop.IsOpen)
            {
                if(shop.ContainsUI(guiPoint))return false; // IMGUI owns selling and buttons inside the panels.
                shop.Close();SetInventoryOpen(false);suppressPrimaryUntilRelease=true;
                secondaryConsumedFrame=Time.frameCount;HideFarmingPreview();return true;
            }
            if(WorldInputBlocked)return false;
            int heart=combat.HeartAt(guiPoint);
            if(heart>=0)
            {
                secondaryConsumedFrame=Time.frameCount;
                return combat.HarvestHeart(heart);
            }
            if(hud.OverUI(guiPoint))return false;
            secondaryConsumedFrame=Time.frameCount;
            return InteractWorld(point) || FarmAction(point);
        }
        // Pointed people and doors have priority; a blocked target never falls through to planting.
        public bool InteractWorld(Vector2 point,bool pointerOverUI=false)
        {
            if(player==null||combat==null||combat.IsGameOver||combat.IsRolling||WorldInputBlocked||GameSession.Busy||pointerOverUI)return false;
            if(InteractRecovery(point)){HideFarmingPreview();return true;}
            var npc=shop!=null?shop.PointedNpc(point):null;
            if(npc!=null)
            {
                if(!shop.Open(npc))Notify("대상에게 가까이 다가가세요.");
                HideFarmingPreview();return true;
            }
            var door=MapExit.PointedDoor(this,point);
            if(door==null)return false;
            if(!door.Contains(PlayerPosition))Notify("문이나 계단 앞에 서서 우클릭하세요.");
            else door.TryActivate(this,true);
            HideFarmingPreview();return true;
        }
        // Shared farming operation, also used after pointed NPC/door routing.
        public bool FarmAction(Vector2 point,bool pointerOverUI=false)
        {
            if(player==null||combat==null||combat.IsGameOver||combat.IsRolling||WorldInputBlocked||GameSession.Busy||pointerOverUI)return false;
            var hint=FarmHint(point);
            if(!hint.Allowed) { Notify(hint.Text); return false; }
            if(PieceAt(point)!=null){DetachAt(point);return true;}
            foreach(var building in GetComponentsInChildren<FarmBuilding>())
                if(building.Contains(point)){HarvestBuildingAt(point);return true;}
            var cell=Vector2Int.RoundToInt(point);
            if(farming.HasPlot(cell)){InteractPlot(cell);return true;}
            Notify("RMB: pick up, plant or harvest. Till empty soil with a hoe first.");return false;
        }
    }
}
