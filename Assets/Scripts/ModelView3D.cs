using UnityEngine;
namespace EverythingGrows
{
    // A typed presentation adapter: SpriteRenderer remains a hidden compatibility handle.
    // Gameplay owns its position/visibility; only real mesh prefabs are drawn in a 3D scene.
    [ExecuteAlways]
    public sealed class ModelView3D : MonoBehaviour
    {
        [SerializeField] SpriteRenderer source;[SerializeField] World3D world;[SerializeField] GameObject prefab,model;
        Renderer[] renderers;MaterialPropertyBlock block;
        public float elevation=.05f,buried;
        public bool held,actor;public Vector2 facing=Vector2.up;public float roll;
        public GameObject Model=>model;
        public void Configure(World3D owner,SpriteRenderer handle,GameObject next,bool pickable)
        {
            world=owner;source=handle;source.forceRenderingOff=true;
            if(model!=null&&prefab==next)return;
            if(model!=null)FarmWorldView.Remove(model);prefab=next;
            if(next==null)return;
            model=Instantiate(next,transform);model.name="3D "+source.name;renderers=model.GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();
            if(pickable)
            {
                var bounds=new Bounds(Vector3.zero,Vector3.zero);
                foreach(var r in renderers){foreach(var p in new[]{r.bounds.min,r.bounds.max})bounds.Encapsulate(model.transform.InverseTransformPoint(p));}
                var collider=model.AddComponent<BoxCollider>();collider.isTrigger=true;collider.center=bounds.center;collider.size=Vector3.Max(bounds.size,Vector3.one*.15f);model.layer=30;model.AddComponent<ModelPickProxy>().owner=source;
            }
            Sync();
        }
        void OnEnable(){if(source!=null)source.forceRenderingOff=true;if(model!=null&&model.transform.parent!=transform)model.transform.SetParent(transform,true);}
        void LateUpdate()=>Sync();
        public void Sync()
        {
            if(source==null||model==null)return;
            bool visible=source.enabled&&source.gameObject.activeInHierarchy;model.SetActive(visible);if(!visible)return;
            var size=source.sprite!=null?Vector2.Scale(source.sprite.bounds.size,source.transform.lossyScale):Vector2.one;
            float scale=Mathf.Clamp(Mathf.Max(Mathf.Abs(size.x),Mathf.Abs(size.y)),.04f,5);
            model.transform.position=new Vector3(source.transform.position.x,source.transform.position.y,-elevation+buried*scale);
            model.transform.rotation=actor?World3D.ModelRotation*Quaternion.Euler(roll*360,Mathf.Atan2(facing.x,facing.y)*Mathf.Rad2Deg,0):source.transform.rotation*World3D.ModelRotation*(held?Quaternion.Euler(0,0,-65):Quaternion.identity);
            model.transform.localScale=Vector3.one;var actual=model.transform.lossyScale;
            model.transform.localScale=new Vector3(scale/Mathf.Max(.001f,Mathf.Abs(actual.x)),scale/Mathf.Max(.001f,Mathf.Abs(actual.y)),scale/Mathf.Max(.001f,Mathf.Abs(actual.z)));
            renderers??=model.GetComponentsInChildren<Renderer>();block??=new MaterialPropertyBlock();
            foreach(var r in renderers)
            {
                block.Clear();var baseColor=r.sharedMaterial!=null?r.sharedMaterial.color:Color.white;block.SetColor("_Color",baseColor*source.color);r.SetPropertyBlock(block);
            }
        }
        void OnDestroy(){if(model!=null&&model.transform.parent!=transform)FarmWorldView.Remove(model);}
    }
}
