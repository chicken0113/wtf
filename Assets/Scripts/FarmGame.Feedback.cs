using UnityEngine;
namespace EverythingGrows
{
    public sealed partial class FarmGame
    {
        private Vector2 frameMovement,actionAim,cameraFeedbackOffset;
        private float weaponFeedbackTime,weaponFeedbackDuration,cameraShakeTime,cameraShakeStrength,feedbackClock,ghostDelay,toolDragCooldown;
        private bool rangedFeedback,toolCellValid;
        private Vector2Int lastToolCell;
        private PlantDefinition lastTool;
        public void WeaponFeedback(Vector2 direction,bool ranged,float interval)
        {
            actionAim=direction;rangedFeedback=ranged;
            weaponFeedbackDuration=Mathf.Clamp(interval*.65f,.08f,.18f);weaponFeedbackTime=weaponFeedbackDuration;
        }
        public void ImpactFeedback(bool playerHurt)
        {
            float strength=Controls!=null?Controls.cameraShake:.06f;
            cameraShakeStrength=Mathf.Max(cameraShakeStrength,strength*(playerHurt?1:.4f));cameraShakeTime=.13f;
        }
        private void TickFeedback(float seconds)
        {
            feedbackClock+=seconds;weaponFeedbackTime=Mathf.Max(0,weaponFeedbackTime-seconds);
            cameraShakeTime=Mathf.Max(0,cameraShakeTime-seconds);if(cameraShakeTime<=0)cameraShakeStrength=0;
            ghostDelay=Mathf.Max(0,ghostDelay-seconds);toolDragCooldown=Mathf.Max(0,toolDragCooldown-seconds);
        }
        private Vector2 CameraFeedback()
        {
            float amount=cameraShakeStrength*Mathf.Clamp01(cameraShakeTime/.13f);
            return new Vector2(Mathf.Sin(feedbackClock*83),Mathf.Cos(feedbackClock*101))*amount;
        }
        public bool UsePrimaryTool(Vector2Int cell,bool freshPress)
        {
            if(combat==null||combat.IsGameOver||combat.IsRolling||WorldInputBlocked||!Near(cell)||Held==null)return false;
            bool repeatable=Held.effect==GrowthEffect.Dig||Held.effect==GrowthEffect.Water||Held.effect==GrowthEffect.Scythe;
            if(!freshPress&&(!repeatable||toolDragCooldown>0||(toolCellValid&&lastTool==Held&&lastToolCell==cell)))return false;
            lastTool=Held;lastToolCell=cell;toolCellValid=true;
            if(!UseTool(cell))return false;
            toolDragCooldown=Controls!=null?Mathf.Max(.02f,Controls.toolDragInterval):.1f;
            if(repeatable)combat.ToolEffect(cell,lastTool.effect);
            return true;
        }
    }
}
