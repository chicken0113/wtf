using System.Collections.Generic;
using UnityEngine;

namespace EverythingGrows
{
    // Shared rectangle sweeps for movement, navigation and shots. Bounds are world aligned.
    public sealed class MapCollision
    {
        readonly List<Rect> movement=new(), projectiles=new();
        readonly Rect world;
        readonly Rect? forbidden;
        public MapCollision(FarmGame farm,bool avoidSafe=false)
        {
            world=farm.WorldBounds; forbidden=avoidSafe?farm.SafeBounds:(Rect?)null;
            foreach(var obstacle in Object.FindObjectsByType<MapObstacle>(FindObjectsSortMode.None))
            {
                if(!obstacle.isActiveAndEnabled || obstacle.gameObject.scene!=farm.gameObject.scene) continue;
                if(obstacle.blocksMovement) movement.Add(obstacle.Bounds);
                if(obstacle.blocksProjectiles) projectiles.Add(obstacle.Bounds);
            }
            foreach(var building in farm.GetComponentsInChildren<FarmBuilding>())
            {
                if(!building.isActiveAndEnabled) continue;
                movement.Add(building.WorldFootprint); projectiles.Add(building.WorldFootprint);
            }
        }
        static Rect Expand(Rect rect,float radius) => Rect.MinMaxRect(rect.xMin-radius,rect.yMin-radius,rect.xMax+radius,rect.yMax+radius);
        public bool CanStand(Vector2 point,float radius)
        {
            if(point.x<world.xMin+radius || point.x>world.xMax-radius || point.y<world.yMin+radius || point.y>world.yMax-radius) return false;
            if(forbidden.HasValue && Expand(forbidden.Value,radius).Contains(point)) return false;
            foreach(var rect in movement) if(Expand(rect,radius).Contains(point)) return false;
            return true;
        }
        // Entry fraction along a swept segment, including a segment that starts inside a wall.
        public static bool Intersect(Vector2 start,Vector2 end,Rect rect,out float entry)
        {
            float low=0,high=1; Vector2 delta=end-start; entry=0;
            for(int axis=0;axis<2;axis++)
            {
                float origin=start[axis],direction=delta[axis],min=rect.min[axis],max=rect.max[axis];
                if(Mathf.Abs(direction)<.000001f) { if(origin<min || origin>max) return false; continue; }
                float a=(min-origin)/direction,b=(max-origin)/direction;
                if(a>b) (a,b)=(b,a);
                low=Mathf.Max(low,a); high=Mathf.Min(high,b); if(low>high) return false;
            }
            entry=low; return true;
        }
        public float Trace(Vector2 start,Vector2 end,float radius,bool shot=false)
        {
            float fraction=1;
            foreach(var rect in shot?projectiles:movement)
                if(Intersect(start,end,Expand(rect,radius),out float entry)) fraction=Mathf.Min(fraction,entry);
            if(!shot && forbidden.HasValue && Intersect(start,end,Expand(forbidden.Value,radius),out float safeEntry)) fraction=Mathf.Min(fraction,safeEntry);
            return fraction;
        }
        public Vector2 Move(Vector2 start,Vector2 desired,float radius)
        {
            desired=new Vector2(Mathf.Clamp(desired.x,world.xMin+radius,world.xMax-radius),Mathf.Clamp(desired.y,world.yMin+radius,world.yMax-radius));
            Vector2 delta=desired-start;
            float fraction=Trace(start,desired,radius);
            if(fraction>=1) return desired;
            Vector2 point=start+delta*Mathf.Max(0,fraction-.001f/Mathf.Max(.001f,delta.magnitude));
            // Resolve each remaining axis separately to slide along wall faces.
            Vector2 horizontal=new Vector2(desired.x,point.y);
            point=Vector2.Lerp(point,horizontal,Mathf.Max(0,Trace(point,horizontal,radius)-.001f));
            Vector2 vertical=new Vector2(point.x,desired.y);
            return Vector2.Lerp(point,vertical,Mathf.Max(0,Trace(point,vertical,radius)-.001f));
        }
        // Bounded A*: cardinal grid steps prevent cutting across blocked diagonal corners.
        public List<Vector2> FindPath(Vector2 start,Vector2 goal,float radius)
        {
            const float step=.5f;
            var from=Vector2Int.RoundToInt(start/step); var target=Vector2Int.RoundToInt(goal/step);
            var open=new List<Vector2Int> {from}; var cost=new Dictionary<Vector2Int,float> {{from,0}};
            var previous=new Dictionary<Vector2Int,Vector2Int>(); var closed=new HashSet<Vector2Int>();
            Vector2Int best=from; float bestDistance=Vector2.Distance(start,goal);
            Vector2Int[] directions={Vector2Int.right,Vector2Int.left,Vector2Int.up,Vector2Int.down};
            for(int visited=0;open.Count>0 && visited<8000;visited++)
            {
                int index=0; float score=float.PositiveInfinity;
                for(int i=0;i<open.Count;i++) { float f=cost[open[i]]+Vector2.Distance((Vector2)open[i]*step,goal); if(f<score) {index=i;score=f;} }
                var current=open[index]; open.RemoveAt(index); closed.Add(current);
                Vector2 point=current==from?start:(Vector2)current*step;
                float distance=Vector2.Distance(point,goal);
                if(distance<bestDistance) {bestDistance=distance;best=current;}
                if(current==target || (distance<step && Trace(point,goal,radius)>=1 && CanStand(goal,radius))) {best=current;break;}
                foreach(var direction in directions)
                {
                    var next=current+direction; Vector2 nextPoint=(Vector2)next*step;
                    if(closed.Contains(next) || !CanStand(nextPoint,radius) || Trace(point,nextPoint,radius)<1) continue;
                    float nextCost=cost[current]+Vector2.Distance(point,nextPoint);
                    if(cost.TryGetValue(next,out float oldCost) && oldCost<=nextCost) continue;
                    cost[next]=nextCost; previous[next]=current; if(!open.Contains(next)) open.Add(next);
                }
            }
            var path=new List<Vector2>();
            for(var cell=best;cell!=from && previous.ContainsKey(cell);cell=previous[cell]) path.Add((Vector2)cell*step);
            path.Reverse();
            if(path.Count>0 && CanStand(goal,radius) && Trace(path[path.Count-1],goal,radius)>=1) path.Add(goal);
            return path;
        }
    }
}
