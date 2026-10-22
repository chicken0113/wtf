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
            if(!held)suppressPrimaryUntilRelease=false;
            else if(pointerOverUI)suppressPrimaryUntilRelease=true;
            return held&&!pointerOverUI&&!suppressPrimaryUntilRelease;
        }

        // True means this E press was handled, including a targeted action that cannot succeed.
        public bool InteractWorld(Vector2 point,bool pointerOverUI=false)
        {
            if(player==null||combat==null||combat.IsGameOver||combat.IsRolling||WorldInputBlocked||GameSession.Busy||pointerOverUI)return false;
            if(shop!=null&&shop.OpenPointed(point)){plantingPreview.enabled=false;return true;}
            if(Near(point))
            {
                if(PieceAt(point)!=null){DetachAt(point);return true;}
                var cell=Vector2Int.RoundToInt(point);
                if(plots.TryGetValue(cell,out var plot))
                {
                    if(plot.plant!=null)Notify("F: harvest. E: plant on empty tilled soil.");else PlantSelected(cell);
                    return true;
                }
                foreach(var building in GetComponentsInChildren<FarmBuilding>())
                    if(building.Contains(point))
                    {if(!MapExit.TryDoor(this))Notify("E: enter from the blue doormat. F: harvest this house.");return true;}
            }
            if(MapExit.TryDoor(this)){plantingPreview.enabled=false;return true;}
            if(shop!=null&&shop.OpenNearby()){plantingPreview.enabled=false;return true;}
            if(Near(point)&&Inside(Vector2Int.RoundToInt(point)))Notify("Till the soil with a hoe, then press E to plant.");
            else Notify("Walk closer to interact.");
            return true;
        }
        public bool HarvestWorld(Vector2 point,bool pointerOverUI=false)
        {
            if(player==null||combat==null||combat.IsGameOver||combat.IsRolling||WorldInputBlocked||GameSession.Busy||pointerOverUI||!Near(point))return false;
            foreach(var building in GetComponentsInChildren<FarmBuilding>())
                if(building.Contains(point)){HarvestBuildingAt(point);return true;}
            var cell=Vector2Int.RoundToInt(point);
            if(plots.TryGetValue(cell,out var plot)&&plot.plant!=null){InteractPlot(cell);return true;}
            Notify("Point at a grown plant or a house, then press F.");return false;
        }
    }
}
