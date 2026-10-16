using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    public sealed class FarmGame : MonoBehaviour
    {
        [SerializeField] private PlantDefinition[] definitions;
        [SerializeField] private FarmMapLayout mapLayout;
        public FarmMapLayout Layout => mapLayout;
        public Rect WorldBounds => mapLayout!=null?mapLayout.World:new Rect(-24.5f,-16.5f,49,33);
        public Rect SafeBounds => mapLayout!=null?mapLayout.Safe:new Rect(-6.5f,-4.5f,13,9);
        public Vector2 SpawnPosition => mapLayout!=null?mapLayout.Spawn:Vector2.zero;
        public bool IsSafe(Vector2 point) => SafeBounds.Contains(point);
        public void ConfigureMap(FarmMapLayout layout) => mapLayout=layout;
        private sealed class InventoryStack { public PlantDefinition item; public int count=1; public InventoryStack(PlantDefinition definition) { item=definition; } }
        private readonly List<InventoryStack> inventory = new();
        private readonly Dictionary<Vector2Int, Plot> plots = new();
        private readonly List<Piece> pieces = new();
        private Sprite sprite;
        private FarmArt art;
        private SpriteRenderer farmer, heldModel;
        private float walkTime, actionTime;
        private int facing;
        private bool showHelp, showMap;
        public float CropProgress(Vector2Int cell) => plots.TryGetValue(cell,out var plot)?plot.progress:0;
        public bool IsWilted(Vector2Int cell) => plots.TryGetValue(cell,out var plot) && plot.wilted;
        private OpenFieldCombat combat;
        private NpcManager shop;
        public NpcManager Shop => shop;
        public PlantDefinition InventoryItem(int index) => index>=0 && index<inventory.Count?inventory[index].item:null;
        public int InventoryQuantity(int index) => index>=0 && index<inventory.Count?inventory[index].count:0;
        public bool CanAddItem(PlantDefinition item) => item!=null && (inventory.Count<Capacity || (item.stackable && inventory.Exists(stack=>stack.item==item)));
        public bool RemoveInventoryItem(int index) { if(index<0 || index>=inventory.Count) return false; if(--inventory[index].count<=0) inventory.RemoveAt(index); selected=Mathf.Clamp(selected,0,Capacity-1); return true; }
        public OpenFieldCombat Combat => combat;
        public Vector2 PlayerPosition => player==null?Vector2.zero:(Vector2)player.position;
        public PlantDefinition SelectedItem => Held;
        public void Notify(string text) => message=text;
        public void MovePlayer(Vector2 point)
        {
            if(player==null) return;
            point=new Vector2(Mathf.Clamp(point.x,WorldBounds.xMin+.5f,WorldBounds.xMax-.5f),Mathf.Clamp(point.y,WorldBounds.yMin+.5f,WorldBounds.yMax-.5f));
            var buildings=mapLayout!=null?mapLayout.GetComponentsInChildren<FarmBuilding>():System.Array.Empty<FarmBuilding>();
            bool Blocked(Vector2 candidate) { foreach(var building in buildings) if(building.Blocks(candidate)) return true; return false; }
            if(Blocked(point))
            {
                Vector2 horizontal=new Vector2(point.x,player.position.y),vertical=new Vector2(player.position.x,point.y);
                point=!Blocked(horizontal)?horizontal:!Blocked(vertical)?vertical:(Vector2)player.position;
            }
            player.position=new Vector3(point.x,point.y,0);
        }
        public void DropCombatMaterial(Vector2 point,bool ranged) { var item=Resources.Load<PlantDefinition>("Equipment/"+(ranged?"Bow":"Sword")); if(item!=null) Spawn(item,point); }
        private GUIStyle titleStyle, smallStyle;
        private Camera view;
        private Transform player;
        private SpriteRenderer plantingPreview;
        private int selected, removedSlots;
        private string message = "Try taking a tool and planting it. Right click to detach.";
        public int Capacity => Mathf.Max(1, 5 + StorageBonus - removedSlots);
        public int InventoryCount => inventory.Count;
        public int RemovedSlots => removedSlots;
        public int WorldPieceCount => pieces.Count;
        public bool IsMature(Vector2Int cell) => plots.TryGetValue(cell,out var plot) && plot.mature;
        public int StorageBonus { get { int result=0; foreach(var plot in plots.Values) if(plot.plant!=null && plot.mature && plot.plant.effect==GrowthEffect.Storage) result+=plot.plant.storageBonus; return result; } }
        private PlantDefinition Held => selected>=0 && selected < inventory.Count ? inventory[selected].item : null;
        private sealed class Piece { public PlantDefinition definition; public GameObject model; }
        private sealed class Plot { public Vector2Int cell; public SpriteRenderer soil, model; public PlantDefinition plant; public bool wet, mature, ready, wilted, regrowing; public float progress, production; }
        public void Configure(PlantDefinition[] data) => definitions=data;
        private void Awake() { if(Application.isPlaying) Initialize(); }
        public void Initialize()
        {
            if(player!=null) return;
            if(mapLayout==null) mapLayout=GetComponentInChildren<FarmMapLayout>();
            art=new FarmArt();
            sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),new Vector2(.5f,.5f),2);
            view=Camera.main;
            if(view==null) { var cameraObject=new GameObject("Main Camera"); view=cameraObject.AddComponent<Camera>(); cameraObject.tag="MainCamera"; }
            view.orthographic=true; view.orthographicSize=4.8f; view.transform.position=new Vector3(0,0,-10); view.backgroundColor=new Color(.12f,.22f,.22f);
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
            heldModel=Draw("Held object",Vector2.zero,Vector2.one*.55f,Color.white,11);
            heldModel.transform.SetParent(player,true); heldModel.enabled=false;
            plantingPreview=Draw("Planting preview",Vector2.zero,Vector2.one*.95f,new Color(.4f,.9f,.5f,.35f),-2);
            plantingPreview.enabled=false;
            if(definitions==null || definitions.Length<5) { message="Missing plant definitions. Run Tools > Everything Grows > Create prototype."; return; }
            if(mapLayout!=null)
            {
                foreach(var placement in mapLayout.GetComponentsInChildren<FarmItemPlacement>())
                    if(placement.item!=null) Spawn(placement.item,placement.transform.position,new Vector2(Mathf.Abs(placement.transform.lossyScale.x),Mathf.Abs(placement.transform.lossyScale.y)));
                mapLayout.HideMarkers();
                if(mapLayout.showSafeMarkers)
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
            if(mapLayout!=null) foreach(var ground in mapLayout.GetComponentsInChildren<FarmGround>()) if(ground.GetComponent<StartingPlot>()!=null) Till(Vector2Int.RoundToInt(ground.transform.position));
            message="RMB: take item. E: plant / harvest. Click: use / attack. Walk outside to fight.";
        }
        private SpriteRenderer Draw(string label,Vector2 position,Vector2 size,Color color,int order)
        {
            var root=new GameObject(label); root.transform.SetParent(transform,false); root.transform.position=position; root.transform.localScale=new Vector3(size.x,size.y,1);
            var renderer=root.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.color=color; renderer.sortingOrder=order; return renderer;
        }
        private SpriteRenderer ArtDraw(string label,Vector2 position,Vector2 size,int index,Color fallback,int order)
        {
            var renderer=Draw(label,position,size,fallback,order);
            if(art.Available) { renderer.sprite=art.Get(index); renderer.color=Color.white; }
            return renderer;
        }
        public void DropItem(PlantDefinition definition,Vector2 point) => Spawn(definition,point);
        private void Spawn(PlantDefinition definition,Vector2 point,Vector2? size=null)
        {
            var renderer=ArtDraw(definition.label,point,size??Vector2.one*.8f,art.ItemIndex(definition),definition.color,3);
            pieces.Add(new Piece { definition=definition,model=renderer.gameObject });
        }
        private bool Inside(Vector2Int cell) => mapLayout!=null?mapLayout.CanFarm(cell):cell.x>=-23 && cell.x<=23 && cell.y>=-15 && cell.y<=15;
        public bool Till(Vector2Int cell)
        {
            if(!Inside(cell) || plots.ContainsKey(cell)) return false;
            plots[cell]=new Plot { cell=cell,soil=ArtDraw("Tilled soil",cell,Vector2.one,10,new Color(.37f,.23f,.14f),-90) }; return true;
        }
        public bool AddItem(PlantDefinition definition)
        {
            if(!CanAddItem(definition)) { message="No space. Plant or drop an item first."; return false; }
            if(definition.stackable)
            {
                int existing=inventory.FindIndex(stack=>stack.item==definition);
                if(existing>=0) { inventory[existing].count++; selected=existing; return true; }
            }
            inventory.Add(new InventoryStack(definition)); selected=inventory.Count-1; return true;
        }
        public bool DetachSlot()
        {
            if(definitions==null || definitions.Length<5 || Capacity<=1 || inventory.Count+1>Capacity-1)
            { message="Keep the final slot and enough room to carry the detached slot."; return false; }
            removedSlots++; inventory.Add(new InventoryStack(definitions[4])); selected=inventory.Count-1; message="A slot was detached. Your storage capacity decreased."; return true;
        }
        public bool DetachAt(Vector2 point)
        {
            var nearest=PieceAt(point);
            if(nearest==null || !AddItem(nearest.definition)) return false;
            Remove(nearest.model); pieces.Remove(nearest); message="Detached "+nearest.definition.label+". Its old place is empty."; return true;
        }
        private Piece PieceAt(Vector2 point)
        {
            Piece nearest=null; float distance=.65f;
            foreach(var piece in pieces) if(piece.model!=null) { float d=Vector2.Distance(piece.model.transform.position,point); if(d<distance) { distance=d; nearest=piece; } }
            return nearest;
        }
        public bool RestoreSlot()
        {
            if(removedSlots<=0 || definitions==null || definitions.Length<5 || Held!=definitions[4]) return false;
            RemoveInventoryItem(selected); removedSlots--; selected=Mathf.Min(selected,Capacity-1);
            message="The detached slot was reattached to the inventory."; return true;
        }
        public bool PlantSelected(Vector2Int cell)
        {
            if(Held==null || !plots.TryGetValue(cell,out var plot) || plot.plant!=null) { message="Select an item and an empty tilled patch."; return false; }
            if(Held.transformed) { message="This item has already transformed. Use it instead of planting again."; return false; }
            if(Held.isSeed && Held.harvestResult==null) { message="These seeds have no crop configured."; return false; }
            if(Held.effect==GrowthEffect.Crop && !Held.isSeed && Held.harvestResult==null) { message="Plant seeds, not harvested vegetables."; return false; }
            plot.wilted=plot.regrowing=false;
            plot.plant=Held; plot.model=ArtDraw("Growing "+Held.label,cell,Vector2.one*.45f,12,Held.color,4);
            RemoveInventoryItem(selected); selected=Mathf.Min(selected,Capacity-1); message="Planted. Water it and see what happens."; return true;
        }
        public int Water(Vector2 point,float radius)
        {
            int count=0; foreach(var plot in plots.Values) if(Vector2.Distance(point,plot.cell)<=radius)
            { plot.wet=true; if(art.Available) plot.soil.sprite=art.Get(11); else plot.soil.color=new Color(.18f,.25f,.29f); count++; } return count;
        }
        public void Tick(float seconds)
        {
            if(seconds<=0 || (combat!=null && combat.IsGameOver)) return;
            var snapshot=new List<Plot>(plots.Values);
            foreach(var plot in snapshot)
            {
                if(plot.plant==null || plot.wilted) continue;
                float activeSeconds=seconds;
                if(!plot.mature && plot.wet)
                {
                    float duration=Mathf.Max(.1f,plot.regrowing?plot.plant.regrowSeconds:plot.plant.growthSeconds);
                    float remaining=(1-plot.progress)*duration;
                    activeSeconds=Mathf.Max(0,seconds-remaining);
                    plot.progress=Mathf.Min(1,plot.progress+seconds/duration);
                    plot.model.transform.localScale=Vector3.one*Mathf.Lerp(.45f,1f,plot.progress);
                    if(plot.progress>=1) { plot.mature=true; if(art.Available) plot.model.sprite=art.Get(plot.plant.harvestResult!=null?art.ItemIndex(plot.plant.harvestResult):plot.plant.effect==GrowthEffect.Produce?15:art.ItemIndex(plot.plant)); message=plot.plant.label+" grew into a new function."; }
                }
                if(!plot.mature) continue;
                switch(plot.plant.effect)
                {
                    case GrowthEffect.Dig:
                        Till(plot.cell+Vector2Int.left); Till(plot.cell+Vector2Int.right); Till(plot.cell+Vector2Int.up); Till(plot.cell+Vector2Int.down); break;
                    case GrowthEffect.Water: Water(plot.cell,plot.plant.effectRadius); break;
                    case GrowthEffect.Produce:
                        plot.production+=activeSeconds; if(plot.production>=plot.plant.productionSeconds) plot.ready=true; break;
                }
            }
        }
        public bool InteractPlot(Vector2Int cell)
        {
            if(!Inside(cell) || !Near(cell)) { message="Walk closer first."; return false; }
            if(plots.TryGetValue(cell,out var plot) && plot.plant!=null)
            {
                if(plot.wilted) { message="Use a scythe to clear the dead crop."; return false; }
                if(!plot.mature) { message=plot.wet?"Still growing. Wait before harvesting.":"Water this plant first."; return false; }
                if(plot.plant.effect==GrowthEffect.Produce || plot.plant.harvestResult!=null) return Collect(cell);
                return Uproot(cell);
            }
            return PlantSelected(cell);
        }
        public bool Collect(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot)) return false;
            if(plot.mature && plot.plant!=null && plot.plant.harvestResult!=null)
            {
                if(plot.plant.isSeed && plot.plant.regrowSeconds>0)
                {
                    if(!AddItem(plot.plant.harvestResult)) return false;
                    plot.mature=false; plot.regrowing=true; plot.progress=0; plot.wet=false;
                    plot.model.transform.localScale=Vector3.one*.45f;
                    if(art.Available) { plot.model.sprite=art.Get(12); plot.soil.sprite=art.Get(10); }
                    message="Harvested. Water again for the next harvest."; return true;
                }
                bool harvested=Uproot(cell); if(harvested) message="Harvested "+Held.label+". Click to use it."; return harvested;
            }
            if(!plot.ready || !AddItem(plot.plant)) return false;
            plot.ready=false; plot.production=0; message="Collected a new "+plot.plant.label+"."; return true;
        }
        public bool Uproot(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot) || plot.plant==null) return false;
            var item=plot.mature && plot.plant.harvestResult!=null?plot.plant.harvestResult:plot.plant;
            int needed=item.stackable && inventory.Exists(stack=>stack.item==item)?0:1;
            int losing=plot.mature && plot.plant.effect==GrowthEffect.Storage ? plot.plant.storageBonus : 0;
            if(inventory.Count+needed>Mathf.Max(1,Capacity-losing)) { message="Free storage before uprooting this plant."; return false; }
            plot.plant=null; plot.mature=false; plot.ready=false; plot.progress=plot.production=0; plot.regrowing=plot.wilted=false;
            Remove(plot.model.gameObject); plot.model=null; AddItem(item); message="Uprooted. You can plant it elsewhere."; return true;
        }
        public bool ClearDeadCrop(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot) || !plot.wilted) return false;
            Remove(plot.model.gameObject); plot.model=null; plot.plant=null; plot.wilted=plot.regrowing=plot.mature=false; plot.progress=0; return true;
        }
        public bool UseTool(Vector2Int cell)
        {
            if(Held==null) { message="Select a hoe or watering can."; return false; }
            if(Held.effect==GrowthEffect.Heart)
            {
                if(!combat.RestoreHeart()) return false;
                RemoveInventoryItem(selected); message="Heart reattached. Maximum HP restored."; return true;
            }
            if(Held.effect==GrowthEffect.Scythe) return ClearDeadCrop(cell);
            if(Held.effect!=GrowthEffect.Dig && Held.effect!=GrowthEffect.Water) return false;
            bool used=Held.effect==GrowthEffect.Dig?Till(cell):Water(cell,.1f)>0;
            if(used) actionTime=.25f; return used;
        }
        private void Remove(GameObject root) { root.SetActive(false); if(Application.isPlaying) Destroy(root); else DestroyImmediate(root); }
        private Vector2 MousePoint => view.ScreenToWorldPoint(Input.mousePosition);
        private Vector2Int MouseCell => Vector2Int.RoundToInt(MousePoint);
        private bool Near(Vector2 point) => Vector2.Distance(player.position,point)<=2.8f;
        private Rect SlotRect(int index) => new Rect((Screen.width-Mathf.Min(8,Capacity)*56)/2+(index%8)*56,Screen.height-72-(index/8)*56,52,52);
        private bool OverUI()
        {
            Vector2 point=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
            if((shop!=null && shop.IsOpen) || new Rect(Screen.width-230,94,220,30).Contains(point) || new Rect(10,10,520,showHelp?174:62).Contains(point) || (combat!=null && combat.HudRect.Contains(point))) return true;
            if(showMap && new Rect(Screen.width/2-170,Screen.height/2-120,340,240).Contains(point)) return true;
            for(int i=0;i<Capacity;i++) if(SlotRect(i).Contains(point)) return true;
            return false;
        }
        private void Update()
        {
            if(player==null) return;
            if(combat.IsGameOver) { plantingPreview.enabled=false; return; }
            var guiMouse=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
            int hoveredHeart=combat.HeartAt(guiMouse);
            if(Input.GetKeyDown(KeyCode.E) && hoveredHeart>=0) { combat.HarvestHeart(hoveredHeart); return; }
            if(shop!=null && shop.IsOpen)
            {
                Tick(Time.deltaTime); combat.TickCombat(Time.deltaTime); shop.CheckDistance();
                if(Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)) shop.Close();
                plantingPreview.enabled=false; return;
            }
            Vector2 move=new Vector2(Input.GetKey(KeyCode.D)?1:Input.GetKey(KeyCode.A)?-1:0,Input.GetKey(KeyCode.W)?1:Input.GetKey(KeyCode.S)?-1:0);
            if(Input.GetKeyDown(KeyCode.LeftShift)) combat.Roll(move.sqrMagnitude>0?move:MousePoint-PlayerPosition);
            if(!combat.IsRolling) MovePlayer(PlayerPosition+Vector2.ClampMagnitude(move,1)*3.8f*Time.deltaTime);
            combat.TickCombat(Time.deltaTime);
            for(int i=0;i<Mathf.Min(9,Capacity);i++) if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))) selected=i;
            AnimateFarmer(move);
            if(Input.GetKeyDown(KeyCode.H)) showHelp=!showHelp;
            if(Input.GetKeyDown(KeyCode.M)) showMap=!showMap;
            float wheel=Input.mouseScrollDelta.y;
            if(wheel!=0) selected=(selected+(wheel>0?-1:1)+Capacity)%Capacity;
            Tick(Time.deltaTime);
            if(Input.GetKeyDown(KeyCode.R)) RestoreSlot();
            if(Input.GetKeyDown(KeyCode.Q) && Held!=null) { var item=Held; RemoveInventoryItem(selected); Spawn(item,player.position+(Vector3)Vector2.right*.6f); }
            if(Input.GetKeyDown(KeyCode.E) && shop!=null && shop.OpenNearby()) { plantingPreview.enabled=false; return; }
            UpdatePlantingPreview();
            if(OverUI()) return;
            var point=MousePoint; var cell=MouseCell;
            if(Held!=null && Held.effect==GrowthEffect.Weapon && Input.GetMouseButton(0))
                if(combat.Attack(Held,point)) actionTime=.25f;
            if(!Near(point)) { if(Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1) || (Input.GetMouseButtonDown(0) && (Held==null || Held.effect!=GrowthEffect.Weapon))) message="Walk closer first."; return; }
            if(Input.GetMouseButtonDown(1))
            {
                if(PieceAt(point)!=null) DetachAt(point);
                else if(plots.TryGetValue(cell,out var planted) && planted.plant!=null)
                { if(planted.plant.isSeed) message="E: harvest ripe crops. Scythe: clear dead crops."; else if(!planted.mature) Uproot(cell); else message="Press E to harvest."; }
            }
            if(Input.GetMouseButtonDown(0))
            {
                if(Held==null) message="Select a hoe to till soil.";
                else if(Held.effect==GrowthEffect.Dig || Held.effect==GrowthEffect.Water || Held.effect==GrowthEffect.Scythe || Held.effect==GrowthEffect.Heart) UseTool(cell);
                else if(Held.effect!=GrowthEffect.Weapon) message="This item has no direct use. Press E to plant it.";
                actionTime=.25f;
            }
            if(Input.GetKeyDown(KeyCode.F)) UseTool(cell);
            if(Input.GetKeyDown(KeyCode.E)) InteractPlot(cell);
        }
        private void LateUpdate()
        {
            if(player==null || view==null) return;
            float halfHeight=view.orthographicSize, halfWidth=halfHeight*view.aspect;
            view.transform.position=new Vector3(ClampCamera(player.position.x,WorldBounds.xMin,WorldBounds.xMax,halfWidth),ClampCamera(player.position.y,WorldBounds.yMin,WorldBounds.yMax,halfHeight),-10);
        }
        private static float ClampCamera(float value,float min,float max,float half) => max-min<=half*2?(min+max)*.5f:Mathf.Clamp(value,min+half,max-half);
        private void AnimateFarmer(Vector2 move)
        {
            walkTime+=Time.deltaTime; actionTime=Mathf.Max(0,actionTime-Time.deltaTime);
            bool walking=move.sqrMagnitude>0;
            if(Held!=null && Held.effect==GrowthEffect.Weapon) move=MousePoint-PlayerPosition;
            if(move.sqrMagnitude>0) facing=Mathf.Abs(move.x)>Mathf.Abs(move.y)?(move.x<0?2:3):(move.y>0?1:0);
            if(art.Available) farmer.sprite=art.Get(facing);
            // Sort by feet position so walking behind plants hides the farmer correctly.
            farmer.sortingOrder=30-Mathf.RoundToInt(player.position.y*10);
            farmer.transform.localScale=new Vector3(1.1f,1.1f+(walking?Mathf.Sin(walkTime*18)*.025f:0),1);
            heldModel.enabled=Held!=null && art.Available;
            if(heldModel.enabled)
            {
                heldModel.sprite=art.Get(art.ItemIndex(Held));
                heldModel.transform.localPosition=new Vector3(facing==2?-.3f:.3f,-.12f,0);
                heldModel.transform.localRotation=Quaternion.Euler(0,0,actionTime>0?Mathf.Sin(actionTime/.25f*Mathf.PI)*-80:0);
                heldModel.sortingOrder=farmer.sortingOrder+1;
            }
            foreach(var piece in pieces) if(piece.model!=null) piece.model.GetComponent<SpriteRenderer>().sortingOrder=30-Mathf.RoundToInt(piece.model.transform.position.y*10);
            foreach(var plot in plots.Values) if(plot.model!=null) plot.model.sortingOrder=29-Mathf.RoundToInt(plot.cell.y*10);
        }
        private void Panel(Rect rect,Color color)
        {
            var previous=GUI.color; GUI.color=new Color(.24f,.13f,.08f); GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=color; GUI.DrawTexture(new Rect(rect.x+3,rect.y+3,rect.width-6,rect.height-6),Texture2D.whiteTexture); GUI.color=previous;
        }
        private void OnGUI()
        {
            if(player==null || combat.IsGameOver) return;
            titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize=16,fontStyle=FontStyle.Bold };
            smallStyle ??= new GUIStyle(GUI.skin.label) { fontSize=12,alignment=TextAnchor.MiddleCenter };
            Panel(new Rect(10,10,520,showHelp?174:62),new Color(.48f,.3f,.17f,.95f));
            GUI.Label(new Rect(22,16,495,24),"Farm  |  H: help",titleStyle);
            GUI.Label(new Rect(22,39,495,28),message);
            if(showHelp)
            {
                GUI.Label(new Rect(22,70,425,22),"WASD move | Click: use / attack | E: plant / harvest");
                GUI.Label(new Rect(22,92,425,22),"RMB: take | E: plant / harvest / shop | Q: drop");
                GUI.Label(new Rect(22,114,495,22),"1-9 / wheel select | RMB slot detach | R restore slot");
                GUI.Label(new Rect(22,136,495,22),"Weapon + click: attack | Shift: dodge | E: harvest grown weapon");
            }
            for(int i=0;i<Capacity;i++)
            {
                Rect rect=SlotRect(i);
                Panel(rect,i==selected?new Color(.85f,.64f,.3f):new Color(.55f,.36f,.2f));
                if(i<inventory.Count && art.Available) art.Icon(new Rect(rect.x+6,rect.y+5,40,40),art.ItemIndex(inventory[i].item));
                else if(i<inventory.Count) GUI.Label(rect,inventory[i].item.label,smallStyle);
                GUI.Label(new Rect(rect.x+4,rect.y+1,16,16),(i+1).ToString());
                if(InventoryQuantity(i)>1) GUI.Label(new Rect(rect.x+26,rect.y+1,24,16),InventoryQuantity(i).ToString(),smallStyle);
                if(shop!=null && shop.IsOpen && i<inventory.Count) GUI.Label(new Rect(rect.x,rect.y+35,rect.width,16),shop.SalePrice(inventory[i].item)+" G",smallStyle);
                if(Event.current.type==EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                { if(Event.current.button==0) selected=i; else if(Event.current.button==1) { if(shop!=null && shop.IsOpen) shop.Sell(i); else DetachSlot(); } Event.current.Use(); break; }
            }
            GUI.Label(new Rect(Screen.width/2-150,Screen.height-100,300,22),Held!=null?Held.label+" - click: use | E: plant / harvest":"Empty hands - select a hoe",smallStyle);
            if(showMap) DrawMap();
            foreach(var plot in plots.Values) if(plot.plant!=null)
            {
                Vector3 position=view.WorldToScreenPoint((Vector2)plot.cell+Vector2.up*.6f);
                string status=plot.wilted?"DEAD - scythe":plot.mature?(plot.ready || plot.plant.harvestResult!=null?"E: HARVEST":plot.plant.effect.ToString()):!plot.wet?"Needs water":Mathf.RoundToInt(plot.progress*100)+"%";
                GUI.Label(new Rect(position.x-70,Screen.height-position.y,140,28),plot.plant.label+" "+status);
                if(!plot.mature)
                {
                    Rect bar=new Rect(position.x-28,Screen.height-position.y+22,56,5);
                    Color previous=GUI.color; GUI.color=new Color(.15f,.15f,.15f); GUI.DrawTexture(bar,Texture2D.whiteTexture);
                    GUI.color=plot.wet?new Color(.4f,.8f,.55f):new Color(.7f,.7f,.7f);
                    GUI.DrawTexture(new Rect(bar.x,bar.y,bar.width*plot.progress,bar.height),Texture2D.whiteTexture); GUI.color=previous;
                }
            }
            foreach(var piece in pieces) if(piece.model!=null) { var position=view.WorldToScreenPoint(piece.model.transform.position); GUI.Label(new Rect(position.x-50,Screen.height-position.y+12,100,22),piece.definition.label); }
        }
        private void DrawMap()
        {
            Rect rect=new Rect(Screen.width/2-170,Screen.height/2-120,340,240);
            Panel(rect,new Color(.24f,.34f,.24f));
            var previous=GUI.color; GUI.color=new Color(.52f,.63f,.3f);
            float scale=Mathf.Min((rect.width-24)/WorldBounds.width,(rect.height-48)/WorldBounds.height);
            Vector2 origin=rect.center-WorldBounds.center*new Vector2(scale,-scale);
            GUI.DrawTexture(new Rect(origin.x+SafeBounds.xMin*scale,origin.y-SafeBounds.yMax*scale,SafeBounds.width*scale,SafeBounds.height*scale),Texture2D.whiteTexture);
            GUI.color=Color.yellow; GUI.DrawTexture(new Rect(origin.x+PlayerPosition.x*scale-3,origin.y-PlayerPosition.y*scale-3,6,6),Texture2D.whiteTexture); GUI.color=previous;
            GUI.Label(new Rect(rect.x+12,rect.y+8,310,24),"M: close | Center: safe farm | Outside: enemies");
        }
        private void OnDestroy() { art?.Dispose(); if(sprite!=null) { if(Application.isPlaying) Destroy(sprite); else DestroyImmediate(sprite); } }
        private void UpdatePlantingPreview()
        {
            var cell=MouseCell;
            plantingPreview.enabled=!OverUI() && Inside(cell);
            plantingPreview.transform.position=new Vector3(cell.x,cell.y,0);
            plots.TryGetValue(cell,out var plot);
            bool harvest=plot!=null && plot.plant!=null && plot.mature && (plot.plant.effect!=GrowthEffect.Produce || plot.ready);
            bool canTill=Held!=null && Held.effect==GrowthEffect.Dig && plot==null;
            bool canPlant=Held!=null && !Held.transformed && plot!=null && plot.plant==null;
            bool possible=Near(MousePoint) && (harvest || canTill || canPlant);
            plantingPreview.color=possible?new Color(.4f,.9f,.5f,.35f):new Color(.95f,.3f,.25f,.35f);
        }
    }
}
