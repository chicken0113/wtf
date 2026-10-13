using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    public sealed class FarmGame : MonoBehaviour
    {
        [SerializeField] private PlantDefinition[] definitions;
        private readonly List<PlantDefinition> inventory = new();
        private readonly Dictionary<Vector2Int, Plot> plots = new();
        private readonly List<Piece> pieces = new();
        private Sprite sprite;
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
        private PlantDefinition Held => selected < inventory.Count ? inventory[selected] : null;
        private sealed class Piece { public PlantDefinition definition; public GameObject model; }
        private sealed class Plot { public Vector2Int cell; public SpriteRenderer soil, model; public PlantDefinition plant; public bool wet, mature, ready; public float progress, production; }
        public void Configure(PlantDefinition[] data) => definitions=data;
        private void Awake() { if(Application.isPlaying) Initialize(); }
        public void Initialize()
        {
            if(player!=null) return;
            sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),new Vector2(.5f,.5f),2);
            view=Camera.main;
            if(view==null) { var cameraObject=new GameObject("Main Camera"); view=cameraObject.AddComponent<Camera>(); cameraObject.tag="MainCamera"; }
            view.orthographic=true; view.orthographicSize=6.7f; view.transform.position=new Vector3(0,0,-10); view.backgroundColor=new Color(.12f,.22f,.22f);
            for(int x=-5;x<=5;x++) for(int y=-3;y<=3;y++) Draw("Ground",new Vector2(x,y),new Vector2(.97f,.97f),new Color(.3f+(x+y)%2*.015f,.43f,.25f),-10);
            player=Draw("Farmer",Vector2.zero,new Vector2(.4f,.55f),new Color(.96f,.73f,.35f),10).transform;
            plantingPreview=Draw("Planting preview",Vector2.zero,Vector2.one*.95f,new Color(.4f,.9f,.5f,.35f),-2);
            plantingPreview.enabled=false;
            if(definitions==null || definitions.Length<5) { message="Missing plant definitions. Run Tools > Everything Grows > Create prototype."; return; }
            Spawn(definitions[0],new Vector2(-3,0)); Spawn(definitions[1],new Vector2(-2,0)); Spawn(definitions[2],new Vector2(2,-1)); Spawn(definitions[3],new Vector2(4,3));
        }
        private SpriteRenderer Draw(string label,Vector2 position,Vector2 size,Color color,int order)
        {
            var root=new GameObject(label); root.transform.SetParent(transform,false); root.transform.position=position; root.transform.localScale=new Vector3(size.x,size.y,1);
            var renderer=root.AddComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.color=color; renderer.sortingOrder=order; return renderer;
        }
        private void Spawn(PlantDefinition definition,Vector2 point)
        {
            var renderer=Draw(definition.label,point,new Vector2(.6f,.5f),definition.color,3);
            pieces.Add(new Piece { definition=definition,model=renderer.gameObject });
        }
        private static bool Inside(Vector2Int cell) => cell.x>=-5 && cell.x<=5 && cell.y>=-3 && cell.y<=3;
        public bool Till(Vector2Int cell)
        {
            if(!Inside(cell) || plots.ContainsKey(cell)) return false;
            plots[cell]=new Plot { cell=cell,soil=Draw("Tilled soil",cell,new Vector2(.9f,.9f),new Color(.37f,.23f,.14f),-5) }; return true;
        }
        public bool AddItem(PlantDefinition definition)
        {
            if(definition==null || inventory.Count>=Capacity) { message="No space. Plant or drop an item first."; return false; }
            inventory.Add(definition); selected=inventory.Count-1; return true;
        }
        public bool DetachSlot()
        {
            if(definitions==null || definitions.Length<5 || Capacity<=1 || inventory.Count+1>Capacity-1)
            { message="Keep the final slot and enough room to carry the detached slot."; return false; }
            removedSlots++; inventory.Add(definitions[4]); selected=inventory.Count-1; message="A slot was detached. Your storage capacity decreased."; return true;
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
            inventory.RemoveAt(selected); removedSlots--; selected=Mathf.Min(selected,Capacity-1);
            message="The detached slot was reattached to the inventory."; return true;
        }
        public bool PlantSelected(Vector2Int cell)
        {
            if(Held==null || !plots.TryGetValue(cell,out var plot) || plot.plant!=null) { message="Select an item and an empty tilled patch."; return false; }
            plot.plant=Held; plot.model=Draw("Growing "+Held.label,cell,Vector2.one*.15f,Held.color,4);
            inventory.RemoveAt(selected); selected=Mathf.Min(selected,Capacity-1); message="Planted. Water it and see what happens."; return true;
        }
        public int Water(Vector2 point,float radius)
        {
            int count=0; foreach(var plot in plots.Values) if(plot.plant!=null && Vector2.Distance(point,plot.cell)<=radius)
            { plot.wet=true; plot.soil.color=new Color(.18f,.25f,.29f); count++; } return count;
        }
        public void Tick(float seconds)
        {
            if(seconds<=0) return;
            var snapshot=new List<Plot>(plots.Values);
            foreach(var plot in snapshot)
            {
                if(plot.plant==null) continue;
                float activeSeconds=seconds;
                if(!plot.mature && plot.wet)
                {
                    float remaining=(1-plot.progress)*Mathf.Max(.1f,plot.plant.growthSeconds);
                    activeSeconds=Mathf.Max(0,seconds-remaining);
                    plot.progress=Mathf.Min(1,plot.progress+seconds/Mathf.Max(.1f,plot.plant.growthSeconds));
                    plot.model.transform.localScale=Vector3.one*Mathf.Lerp(.15f,.75f,plot.progress);
                    if(plot.progress>=1) { plot.mature=true; message=plot.plant.label+" grew into a new function."; }
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
        public bool Collect(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot) || !plot.ready || !AddItem(plot.plant)) return false;
            plot.ready=false; plot.production=0; message="Collected a new "+plot.plant.label+"."; return true;
        }
        public bool Uproot(Vector2Int cell)
        {
            if(!plots.TryGetValue(cell,out var plot) || plot.plant==null) return false;
            int losing=plot.mature && plot.plant.effect==GrowthEffect.Storage ? plot.plant.storageBonus : 0;
            if(inventory.Count+1>Mathf.Max(1,Capacity-losing)) { message="Free storage before uprooting this plant."; return false; }
            var item=plot.plant; plot.plant=null; plot.mature=false; plot.ready=false; plot.progress=plot.production=0;
            Remove(plot.model.gameObject); plot.model=null; inventory.Add(item); selected=inventory.Count-1; message="Uprooted. You can plant it elsewhere."; return true;
        }
        private void Remove(GameObject root) { root.SetActive(false); if(Application.isPlaying) Destroy(root); else DestroyImmediate(root); }
        private Vector2 MousePoint => view.ScreenToWorldPoint(Input.mousePosition);
        private Vector2Int MouseCell => Vector2Int.RoundToInt(MousePoint);
        private bool Near(Vector2 point) => Vector2.Distance(player.position,point)<=2.8f;
        private Rect SlotRect(int index) => new Rect(12+(index%8)*55,Screen.height-65-(index/8)*55,50,50);
        private bool OverUI()
        {
            Vector2 point=new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y);
            if(new Rect(10,10,450,135).Contains(point)) return true;
            for(int i=0;i<Capacity;i++) if(SlotRect(i).Contains(point)) return true;
            return false;
        }
        private void Update()
        {
            if(player==null) return;
            Vector2 move=new Vector2(Input.GetKey(KeyCode.D)?1:Input.GetKey(KeyCode.A)?-1:0,Input.GetKey(KeyCode.W)?1:Input.GetKey(KeyCode.S)?-1:0);
            Vector3 next=player.position+(Vector3)Vector2.ClampMagnitude(move,1)*3*Time.deltaTime;
            next.x=Mathf.Clamp(next.x,-5.5f,5.5f); next.y=Mathf.Clamp(next.y,-3.5f,3.5f); player.position=next;
            for(int i=0;i<Mathf.Min(9,Capacity);i++) if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))) selected=i;
            Tick(Time.deltaTime);
            if(Input.GetKeyDown(KeyCode.R)) RestoreSlot();
            UpdatePlantingPreview();
            if(OverUI()) return;
            var point=MousePoint; var cell=MouseCell;
            if(!Near(point)) { if(Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1)) message="Walk closer first."; return; }
            if(Input.GetMouseButtonDown(1))
            {
                if(PieceAt(point)!=null) DetachAt(point); else Uproot(cell);
            }
            if(Input.GetMouseButtonDown(0)) { if(Held==null) Till(cell); else PlantSelected(cell); }
            if(Input.GetKeyDown(KeyCode.Space)) Water(point,.7f);
            if(Input.GetKeyDown(KeyCode.F) && Held!=null) { if(Held.effect==GrowthEffect.Dig) Till(cell); else if(Held.effect==GrowthEffect.Water) Water(point,Held.effectRadius); }
            if(Input.GetKeyDown(KeyCode.E)) Collect(cell);
            if(Input.GetKeyDown(KeyCode.Q) && Held!=null) { var item=Held; inventory.RemoveAt(selected); Spawn(item,player.position+(Vector3)Vector2.right*.6f); }
        }
        private void OnGUI()
        {
            GUI.Box(new Rect(10,10,450,135),"EVERYTHING GROWS — first prototype");
            GUI.Label(new Rect(22,35,430,22),"WASD move | RMB detach / uproot | LMB till / plant");
            GUI.Label(new Rect(22,57,430,22),"Space water by hand | F use held tool | E collect fruit");
            GUI.Label(new Rect(22,79,430,22),"1-9 select | Q drop | RMB slot: detach | R reattach slot");
            GUI.Label(new Rect(22,101,430,38),message);
            for(int i=0;i<Capacity;i++)
            {
                Rect rect=SlotRect(i); Color color=GUI.backgroundColor;
                GUI.backgroundColor=i==selected?new Color(.95f,.78f,.3f):Color.white;
                if(GUI.Button(rect,(i+1)+"\n"+(i<inventory.Count?inventory[i].label:""))) selected=i;
                GUI.backgroundColor=color;
                if(Event.current.type==EventType.MouseDown && Event.current.button==1 && rect.Contains(Event.current.mousePosition)) { DetachSlot(); Event.current.Use(); break; }
            }
            foreach(var plot in plots.Values) if(plot.plant!=null)
            {
                Vector3 position=view.WorldToScreenPoint((Vector2)plot.cell+Vector2.up*.6f);
                string status=plot.mature?(plot.ready?"READY":plot.plant.effect.ToString()):!plot.wet?"Needs water":Mathf.RoundToInt(plot.progress*100)+"%";
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
        private void OnDestroy() { if(sprite!=null) { if(Application.isPlaying) Destroy(sprite); else DestroyImmediate(sprite); } }
        private void UpdatePlantingPreview()
        {
            var cell=MouseCell;
            plantingPreview.enabled=!OverUI() && Inside(cell);
            plantingPreview.transform.position=new Vector3(cell.x,cell.y,0);
            bool possible=Near(MousePoint) && (Held==null?!plots.ContainsKey(cell):plots.TryGetValue(cell,out var plot) && plot.plant==null);
            plantingPreview.color=possible?new Color(.4f,.9f,.5f,.35f):new Color(.95f,.3f,.25f,.35f);
        }
    }
}
