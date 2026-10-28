using UnityEngine;

namespace EverythingGrows
{
    public sealed partial class FarmGame : MonoBehaviour
    {
        [SerializeField] private PlantDefinition[] definitions;
        [SerializeField] private FarmMapLayout mapLayout;
        public FarmMapLayout Layout => mapLayout;
        public Rect WorldBounds => mapLayout!=null?mapLayout.World:new Rect(-24.5f,-16.5f,49,33);
        public Rect SafeBounds => mapLayout!=null?mapLayout.Safe:new Rect(-6.5f,-4.5f,13,9);
        public Vector2 SpawnPosition => mapLayout!=null?mapLayout.Spawn:Vector2.zero;
        public bool IsSafe(Vector2 point) => !(combat!=null && combat.UsesRegions) && ((mapLayout!=null && mapLayout.combatSchedule==FarmMapLayout.CombatSchedule.Peaceful) || SafeBounds.Contains(point));
        public void ConfigureMap(FarmMapLayout layout) => mapLayout=layout;
        private FarmWorldView worldView;
        private FarmHud hud;
        private FarmingSystem farming;
        private ForageSystem foraging;
        internal FarmingSystem Farming => farming;
        private FarmArt art => worldView.Art;
        private Sprite sprite => worldView.Square;
        private SpriteRenderer farmer, heldModel;
        private Transform player;
        private Camera view;
        private SpriteRenderer plantingPreview;
        private float walkTime, actionTime;
        private int facing;
        private bool showMap, showInventory;
        private OpenFieldCombat combat;
        private NpcManager shop;
        private string message = "RMB: pick up, plant or harvest. Left click: use tools.";
        public bool MapOpen => showMap;
        public void SetMapOpen(bool open) => showMap = open;
        public bool WorldInputBlocked => InventoryOpen || showMap || GameMenu.IsBlocking;
        public bool InventoryOpen => showInventory || (shop != null && shop.IsOpen);
        internal bool PersonalInventoryOpen => showInventory;
        public void SetInventoryOpen(bool open) => showInventory = open;
        public Rect InventoryPanelRect => hud.InventoryPanelRect;
        public int Gold => shop != null ? shop.Gold : carriedGold;
        public DayNightCycle Cycle { get; private set; }
        public OpenFieldCombat Combat => combat;
        public NpcManager Shop => shop;
        public Vector2 PlayerPosition => player == null ? Vector2.zero : (Vector2)player.position;
        public PlantDefinition SelectedItem => Held;
        public void Notify(string text) => message = text;
        [SerializeField, Min(.05f)] private float playerRadius = .25f;
        public float PlayerRadius => playerRadius;
        public float CropProgress(Vector2Int cell) => farming != null && farming.TryGetPlot(cell, out var plot) ? plot.progress : 0;
        public bool IsWilted(Vector2Int cell) => farming != null && farming.TryGetPlot(cell, out var plot) && plot.wilted;
        public bool IsMature(Vector2Int cell) => farming != null && farming.TryGetPlot(cell, out var plot) && plot.mature;
        public int StorageBonus => GameSession.RemoteStorageBonus(gameObject.scene.path) + (farming != null ? farming.StorageBonus : 0);
        public bool Till(Vector2Int cell) => farming.Till(cell);
        public bool PlantSelected(Vector2Int cell) => farming.PlantSelected(cell);
        public int Water(Vector2 point, float radius) => farming.Water(point, radius);
        public void Tick(float seconds) { farming.Tick(seconds);foraging?.Tick(); }
        public bool InteractPlot(Vector2Int cell) => farming.InteractPlot(cell);
        public bool Collect(Vector2Int cell) => farming.Collect(cell);
        public bool Uproot(Vector2Int cell) => farming.Uproot(cell);
        public bool ClearDeadCrop(Vector2Int cell) => farming.ClearDeadCrop(cell);
        public void MovePlayer(Vector2 point)
        {
            if(player==null) return;
            Vector2 resolved=new MapCollision(this).Move(PlayerPosition,point,playerRadius);
            player.position=new Vector3(resolved.x,resolved.y,0);
        }
        // Respawn, saved positions and map arrivals are placement, not swept movement.
        public void TeleportPlayer(Vector2 point)
        {
            if(player==null) return;
            player.position=new Vector3(Mathf.Clamp(point.x,WorldBounds.xMin+playerRadius,WorldBounds.xMax-playerRadius),Mathf.Clamp(point.y,WorldBounds.yMin+playerRadius,WorldBounds.yMax-playerRadius),0);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color=Color.cyan;
            Vector2 point=player!=null?PlayerPosition:SpawnPosition;
            Gizmos.DrawWireCube(point,Vector2.one*playerRadius*2);
        }
        public void DropCombatMaterial(Vector2 point,bool ranged) => MonsterRewardSystem.Grant(this,MonsterType.Default(ranged),point);
        public void Configure(PlantDefinition[] data) => definitions=data;
        private void Awake() { if(Application.isPlaying) Initialize(); }
        public World3D Presentation3D {get;private set;}
        private void InitializeRuntime(bool grantStartingItems, GameSetupResources resources)
        {
            Presentation3D=GetComponent<World3D>();
            if(mapLayout==null) mapLayout=GetComponentInChildren<FarmMapLayout>();
            worldView=new FarmWorldView(transform);
            farming=new FarmingSystem(this,worldView);
            controlSettings=resources.Controls;
            view=Camera.main;
            if(view==null) { var cameraObject=new GameObject("Main Camera"); view=cameraObject.AddComponent<Camera>(); cameraObject.tag="MainCamera"; }
            hud=new FarmHud(this,art,view);
            view.orthographic=true; view.orthographicSize=4.8f; view.transform.position=new Vector3(0,0,-10); view.backgroundColor=new Color(.12f,.22f,.22f);
            if(Presentation3D!=null)Presentation3D.ConfigureCamera(view,SpawnPosition);
            if(mapLayout==null)
            {
            for(int x=-24;x<=24;x++) for(int y=-16;y<=16;y++)
            {
                var ground=ArtDraw("Ground",new Vector2(x,y),Vector2.one,9,new Color(.3f,.43f,.25f),-100);
                if(!OpenFieldCombat.Safe(new Vector2(x,y))) ground.color=new Color(.67f,.74f,.68f);
                if((Mathf.Abs(y)<=1 && Mathf.Abs(x)>6) || (Mathf.Abs(x)<=1 && Mathf.Abs(y)>4))
                    Draw("Field path",new Vector2(x,y),Vector2.one,new Color(.42f,.37f,.24f),-95);
            }
            for(int x=-6;x<=6;x+=2) { Draw("Safe area marker",new Vector2(x,4.6f),new Vector2(.12f,.18f),new Color(.95f,.78f,.3f),-80); Draw("Safe area marker",new Vector2(x,-4.6f),new Vector2(.12f,.18f),new Color(.95f,.78f,.3f),-80); }
            for(int y=-4;y<=4;y+=2) { Draw("Safe area marker",new Vector2(6.6f,y),new Vector2(.12f,.18f),new Color(.95f,.78f,.3f),-80); Draw("Safe area marker",new Vector2(-6.6f,y),new Vector2(.12f,.18f),new Color(.95f,.78f,.3f),-80); }
            for(int x=-7;x<=7;x++) if(Mathf.Abs(x)>5) for(int y=-4;y<=4;y+=2)
                ArtDraw("Border flowers",new Vector2(x,y),Vector2.one*.7f,14,Color.yellow,-50);
            }
            farmer=ArtDraw("Farmer",SpawnPosition,Vector2.one*1.1f,0,new Color(.96f,.73f,.35f),10);
            player=farmer.transform;
            if(Presentation3D!=null)Presentation3D.Show(farmer,ModelKind3D.Player);
            heldModel=Draw("Held object",Vector2.zero,Vector2.one*.55f,Color.white,11);
            heldModel.transform.SetParent(player,true); heldModel.enabled=false;
            plantingPreview=Draw("Farming action icon",Vector2.zero,Vector2.one*.95f,new Color(.4f,.9f,.5f,.35f),-2);
            HideFarmingPreview();
            if(mapLayout!=null)
            {
                foreach(var placement in mapLayout.GetComponentsInChildren<FarmItemPlacement>())
                    if(placement.forage==null && placement.item!=null) Spawn(placement.item,placement.transform.position,new Vector2(Mathf.Abs(placement.transform.lossyScale.x),Mathf.Abs(placement.transform.lossyScale.y)));
                foraging=new ForageSystem(this,mapLayout.GetComponentsInChildren<FarmItemPlacement>());
                mapLayout.HideMarkers();
                if(mapLayout.showSafeMarkers && SpawnRegion.ForScene(gameObject.scene.path).Length==0)
                {
                var safe=SafeBounds;
                for(float x=safe.xMin;x<=safe.xMax;x+=2) { Draw("Safe marker",new Vector2(x,safe.yMin),new Vector2(.12f,.18f),Color.yellow,-80); Draw("Safe marker",new Vector2(x,safe.yMax),new Vector2(.12f,.18f),Color.yellow,-80); }
                for(float y=safe.yMin;y<=safe.yMax;y+=2) { Draw("Safe marker",new Vector2(safe.xMin,y),new Vector2(.12f,.18f),Color.yellow,-80); Draw("Safe marker",new Vector2(safe.xMax,y),new Vector2(.12f,.18f),Color.yellow,-80); }
                }
            }
            else
            {
            Spawn(definitions[0],new Vector2(-3,0)); Spawn(definitions[1],new Vector2(-2,0)); Spawn(definitions[2],new Vector2(2,-1)); Spawn(definitions[3],new Vector2(4,3));
            var sword=Resources.Load<PlantDefinition>("Equipment/Sword"); var bow=Resources.Load<PlantDefinition>("Equipment/Bow");
            if(sword!=null) Spawn(sword,new Vector2(-1,1)); if(bow!=null) Spawn(bow,new Vector2(1,1));
            }
            combat=gameObject.AddComponent<OpenFieldCombat>(); combat.Initialize(this);
            shop=GetComponent<NpcManager>(); if(shop!=null) shop.Initialize(this);
            Cycle=GetComponent<DayNightCycle>(); if(Cycle==null) Cycle=gameObject.AddComponent<DayNightCycle>(); Cycle.Initialize(this,resources.Cycle);
            if(mapLayout!=null) foreach(var ground in mapLayout.GetComponentsInChildren<FarmGround>()) if(ground.GetComponent<StartingPlot>()!=null) Till(Vector2Int.RoundToInt(ground.transform.position));
            if(grantStartingItems)
            {
                var loadout=resources.Starting;
                if(loadout!=null && loadout.items!=null) foreach(var entry in loadout.items)
                    if(entry!=null && entry.item!=null) for(int i=0;i<Mathf.Max(1,entry.quantity);i++)
                        if(!AddItem(entry.item)) Spawn(entry.item,SpawnPosition+Vector2.down*.6f);
                selected=0;
            }
            message="RMB: pick up / plant / harvest / talk / enter. LMB: use / attack. Space or Shift: dodge.";
            foreach(var building in GetComponentsInChildren<FarmBuilding>()) if(!building.runtimePlaced) authoredBuildingKeys[building]=MapObjectId.Key(building);
            foreach(var building in GetComponentsInChildren<FarmBuilding>()) HouseInteriorLink.EnsureDoor(building);
        }
        private SpriteRenderer Draw(string label,Vector2 position,Vector2 size,Color color,int order) => worldView.Draw(label,position,size,color,order);
        private SpriteRenderer ArtDraw(string label,Vector2 position,Vector2 size,int index,Color fallback,int order) => worldView.ArtDraw(label,position,size,index,fallback,order);
        public bool UseTool(Vector2Int cell)
        {
            if(combat!=null&&combat.IsRolling)return false;
            if(Held==null) { message="Select a hoe or watering can."; return false; }
            if(Held.effect==GrowthEffect.House) return PlaceHouse(cell);
            if(Held.effect==GrowthEffect.Heart)
            {
                if(!combat.RestoreHeart()) return false;
                RemoveInventoryItem(selected); message="Heart reattached. Maximum HP restored."; return true;
            }
            if(Held.effect!=GrowthEffect.Dig && Held.effect!=GrowthEffect.Water) return false;
            var hint=farming.ToolHint(cell,Held.effect);
            if(!hint.Allowed) { Notify(hint.Text); return false; }
            bool used=Held.effect==GrowthEffect.Dig?farming.UseHoe(cell):Water(cell,.1f)>0;
            if(used) actionTime=.25f; return used;
        }
        private void Remove(GameObject root) => FarmWorldView.Remove(root);
        private Vector2 MousePoint => WorldProjection.Pointer(view,Input.mousePosition)-cameraFeedbackOffset;
        private Vector2Int MouseCell => Vector2Int.RoundToInt(MousePoint);
        private bool Near(Vector2 point) => Vector2.Distance(player.position,point)<=2.8f;
        internal bool WithinReach(Vector2 point) => Near(point);
        private bool OverUI() => hud.OverUI(new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y));
        private void OnGUI() { if(player!=null) hud?.Draw(); }
        private void OnDestroy() => worldView?.Dispose();
    }
}
