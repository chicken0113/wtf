using UnityEngine;
namespace EverythingGrows
{
    // Explicit ground-plane conversion, shared by aiming, picking and region visibility.
    public static class WorldProjection
    {
        public static Vector2 GroundPoint(Camera camera,Vector3 screen)
        {
            if(camera==null)return Vector2.zero;
            var ray=camera.ScreenPointToRay(screen);return new Plane(Vector3.forward,Vector3.zero).Raycast(ray,out float distance)?(Vector2)ray.GetPoint(distance):(Vector2)camera.transform.position;
        }
        public static Vector2 Pointer(Camera camera,Vector3 screen)
        {
            if(camera==null)return Vector2.zero;
            Physics.SyncTransforms();var ray=camera.ScreenPointToRay(screen);float nearest=float.PositiveInfinity;ModelPickProxy target=null;
            foreach(var hit in Physics.RaycastAll(ray,200,1<<30,QueryTriggerInteraction.Collide))
            {
                var proxy=hit.collider.GetComponent<ModelPickProxy>();
                if(proxy!=null&&proxy.owner!=null&&proxy.owner.enabled&&proxy.owner.gameObject.scene==camera.gameObject.scene&&hit.distance<nearest){target=proxy;nearest=hit.distance;}
            }
            return target!=null?(Vector2)target.owner.transform.position:GroundPoint(camera,screen);
        }
        public static Rect GroundBounds(Camera camera,float padding)
        {
            var bounds=new Bounds(GroundPoint(camera,Vector3.zero),Vector3.zero);
            foreach(var p in new[]{new Vector3(camera.pixelWidth,0),new Vector3(0,camera.pixelHeight),new Vector3(camera.pixelWidth,camera.pixelHeight)})bounds.Encapsulate(GroundPoint(camera,p));
            return Rect.MinMaxRect(bounds.min.x-padding,bounds.min.y-padding,bounds.max.x+padding,bounds.max.y+padding);
        }
    }
}
