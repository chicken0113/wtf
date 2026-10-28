using UnityEngine;
namespace EverythingGrows
{
    [DisallowMultipleComponent]
    public sealed class World3D : MonoBehaviour
    {
        public WorldModelLibrary models;
        readonly System.Collections.Generic.List<Renderer> occluded=new();
        [Range(30,80)] public float cameraPitch=52;
        [Min(3)] public float cameraSize=7;
        public static readonly Quaternion ModelRotation=Quaternion.Euler(-90,0,0);
        public void ConfigureCamera(Camera camera,Vector2 center)
        {
            camera.orthographic=true;camera.orthographicSize=cameraSize;camera.nearClipPlane=.1f;camera.farClipPlane=150;
            Follow(camera,center);camera.backgroundColor=new Color(.3f,.43f,.49f);
        }
        public void Follow(Camera camera,Vector2 center)
        {
            float angle=cameraPitch*Mathf.Deg2Rad;var offset=new Vector3(0,-Mathf.Cos(angle),-Mathf.Sin(angle))*30;
            camera.transform.SetPositionAndRotation((Vector3)center+offset,Quaternion.LookRotation(-offset,Vector3.back));
        }
        void LateUpdate()
        {
            foreach(var r in occluded)if(r!=null)r.forceRenderingOff=false;occluded.Clear();
            var farm=GetComponent<FarmGame>();var camera=Camera.main;if(farm==null||!farm.IsInitialized||camera==null)return;
            var point=(Vector3)farm.PlayerPosition+Vector3.back*.6f;var direction=point-camera.transform.position;
            foreach(var hit in Physics.RaycastAll(camera.transform.position,direction.normalized,direction.magnitude,1<<29,QueryTriggerInteraction.Collide))
                foreach(var r in hit.collider.GetComponentsInChildren<Renderer>()){r.forceRenderingOff=true;occluded.Add(r);}
        }
        public ModelView3D Show(SpriteRenderer source,GameObject prefab,bool pickable=false)
        {
            var view=source.GetComponent<ModelView3D>();if(view==null)view=source.gameObject.AddComponent<ModelView3D>();view.Configure(this,source,prefab,pickable);return view;
        }
        public ModelView3D Show(SpriteRenderer source,ModelKind3D kind,bool pickable=false)=>Show(source,models.Get(kind),pickable);
        public void Item(SpriteRenderer source,PlantDefinition item,bool held=false)
        {var v=Show(source,models.Item(item),!held);v.elevation=held?.65f:.05f;v.held=held;}
    }
}
