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
        private void LateUpdate()
        {
            if(player==null || view==null) return;
            if(combat!=null&&!WorldInputBlocked&&!combat.IsGameOver&&!GameSession.Busy&&string.IsNullOrEmpty(GameSession.LastError))
            {TickFeedback(Time.deltaTime);AnimateFarmer(frameMovement);}
            float halfHeight=view.orthographicSize/(Presentation3D!=null?Mathf.Sin(Presentation3D.cameraPitch*Mathf.Deg2Rad):1), halfWidth=view.orthographicSize*view.aspect;
            var center=new Vector2(ClampCamera(player.position.x,WorldBounds.xMin,WorldBounds.xMax,halfWidth),ClampCamera(player.position.y,WorldBounds.yMin,WorldBounds.yMax,halfHeight));
            var shaken=center+CameraFeedback();
            shaken=new Vector2(ClampCamera(shaken.x,WorldBounds.xMin,WorldBounds.xMax,halfWidth),ClampCamera(shaken.y,WorldBounds.yMin,WorldBounds.yMax,halfHeight));
            cameraFeedbackOffset=shaken-center;if(Presentation3D!=null)Presentation3D.Follow(view,shaken);else view.transform.position=new Vector3(shaken.x,shaken.y,-10);
        }
        private static float ClampCamera(float value,float min,float max,float half) => max-min<=half*2?(min+max)*.5f:Mathf.Clamp(value,min+half,max-half);
        private void AnimateFarmer(Vector2 move)
        {
            walkTime+=Time.deltaTime; actionTime=Mathf.Max(0,actionTime-Time.deltaTime);
            bool walking=move.sqrMagnitude>0;
            var aim=AimDirection;
            facing=Mathf.Abs(aim.x)>Mathf.Abs(aim.y)?(aim.x<0?2:3):(aim.y>0?1:0);
            if(art.Available) farmer.sprite=art.Get(facing);
            // Sort by feet position so walking behind plants hides the farmer correctly.
            farmer.sortingOrder=30-Mathf.RoundToInt(player.position.y*10);
            farmer.transform.localScale=new Vector3(1.1f,1.1f+(walking?Mathf.Sin(walkTime*18)*.025f:0),1);
            farmer.transform.localRotation=Quaternion.identity;
            farmer.color=Presentation3D!=null?Color.Lerp(Color.white,new Color(1,.4f,.4f),combat.PlayerHitFlash):Color.Lerp(new Color(.96f,.73f,.35f),Color.white,combat.PlayerHitFlash);
            if(Presentation3D!=null){var pose=farmer.GetComponent<ModelView3D>();pose.actor=true;pose.facing=aim;pose.roll=combat.IsRolling?combat.RollProgress:0;}
            if(combat.IsRolling)
            {
                float roll=combat.RollProgress;
                farmer.transform.localRotation=Quaternion.Euler(0,0,(combat.RollDirection.x<0?1:-1)*roll*360);
                farmer.transform.localScale=new Vector3(1.1f+Mathf.Sin(roll*Mathf.PI)*.13f,1.1f-Mathf.Sin(roll*Mathf.PI)*.2f,1);
                if(ghostDelay<=0){combat.Afterimage(farmer);ghostDelay=.04f;}
            }
            heldModel.enabled=Held!=null && art.Available&&!combat.IsRolling;
            if(heldModel.enabled)
            {
                heldModel.sprite=art.ItemSprite(Held);
                if(Presentation3D!=null)Presentation3D.Item(heldModel,Held,true);
                float heldSize=Mathf.Max(heldModel.sprite.bounds.size.x,heldModel.sprite.bounds.size.y);
                heldModel.transform.localScale=Vector3.one*(.5f/Mathf.Max(.01f,heldSize));
                float action=weaponFeedbackDuration>0?Mathf.Clamp01(weaponFeedbackTime/weaponFeedbackDuration):0;
                var heldAim=Held.effect==GrowthEffect.Weapon&&action>0&&!rangedFeedback?actionAim:aim;
                float recoil=Controls!=null?Controls.weaponRecoil:.12f;
                heldModel.transform.localPosition=(Vector3)(heldAim*(.32f-(rangedFeedback?action*recoil:0))+Vector2.down*.1f);
                float rotation=Held.effect==GrowthEffect.Weapon?Mathf.Atan2(heldAim.y,heldAim.x)*Mathf.Rad2Deg-(Held.ranged?0:90):Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg-90;
                float swing=Held.effect==GrowthEffect.Weapon?(rangedFeedback?action*12:Mathf.Lerp(-65,65,1-action)):actionTime>0?Mathf.Sin(actionTime/.25f*Mathf.PI)*-70:0;
                heldModel.transform.localRotation=Quaternion.Euler(0,0,rotation+(Held.effect==GrowthEffect.Weapon&&action<=0?0:swing));
                heldModel.sortingOrder=farmer.sortingOrder+(aim.y>.35f?-1:1);
            }
            foreach(var piece in pieces) if(piece.model!=null) piece.model.GetComponent<SpriteRenderer>().sortingOrder=30-Mathf.RoundToInt(piece.model.transform.position.y*10);
            worldView.SortPlants();
        }
    }
}
