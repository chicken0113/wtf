using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Pure layout data. Uses a private seeded RNG, never changes UnityEngine.Random state.
internal sealed class WorldGenerationPlan
{
    internal enum Terrain { Meadow, Forest, Water }
    internal readonly int Width,Height;
    internal readonly Terrain[] Cells;
    internal readonly bool[] Reserved;
    internal readonly Vector2Int Start,Boss;
    internal readonly Vector2Int[] Camps;
    static readonly Vector2Int[] Directions={Vector2Int.right,Vector2Int.up,Vector2Int.left,Vector2Int.down};
    internal int Index(Vector2Int p)=>p.y*Width+p.x;
    internal Vector2 Position(Vector2Int p)=>new(p.x-Width*.5f+.5f,p.y-Height*.5f+.5f);
    internal bool Inside(Vector2Int p)=>p.x>=0&&p.y>=0&&p.x<Width&&p.y<Height;
    internal bool Land(Vector2Int p)=>Inside(p)&&Cells[Index(p)]!=Terrain.Water;
    internal WorldGenerationPlan(WorldGenerationSettings settings)
    {
        settings.Validate();Width=settings.width;Height=settings.height;Cells=new Terrain[Width*Height];Reserved=new bool[Cells.Length];
        var rng=new System.Random(settings.seed);float ox=rng.Next(100,10000),oy=rng.Next(100,10000),phase=(float)rng.NextDouble()*6;
        var lakes=Enumerable.Range(0,4).Select(_=>new Vector2(rng.Next(6,Width-6),rng.Next(6,Height-6))).ToArray();
        for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
        {
            var p=new Vector2Int(x,y);float noise=Mathf.PerlinNoise(ox+x*.085f,oy+y*.085f);
            float streamX=Width*.57f+Mathf.Sin(y*.15f+phase)*Width*.095f;
            bool ford=Mathf.Abs(y-Height*.27f)<2||Mathf.Abs(y-Height*.7f)<2;
            bool water=x<2||y<2||x>=Width-2||y>=Height-2||(!ford&&Mathf.Abs(x-streamX)<settings.waterAmount*6);
            foreach(var lake in lakes)
            {
                float rx=Width*settings.waterAmount*.45f,ry=Height*settings.waterAmount*.35f;
                water|=Mathf.Pow((x-lake.x)/rx,2)+Mathf.Pow((y-lake.y)/ry,2)<.7f+noise*.6f;
            }
            Cells[Index(p)]=water?Terrain.Water:noise<settings.forestAmount?Terrain.Forest:Terrain.Meadow;
        }
        Start=new Vector2Int(Width/3,Height/3);Clear(Start,5);
        var reachable=Reachable(Start);
        for(int i=0;i<Cells.Length;i++)if(!reachable.Contains(i))Cells[i]=Terrain.Water;
        Boss=Choose(rng,new[]{Start},true);Clear(Boss,7);
        Camps=new[]{Choose(rng,new[]{Start,Boss},false),Vector2Int.zero};Clear(Camps[0],4);
        Camps[1]=Choose(rng,new[]{Start,Boss,Camps[0]},false);Clear(Camps[1],4);
        foreach(var destination in Camps.Append(Boss))ReservePath(destination);
    }
    Vector2Int Choose(System.Random rng,Vector2Int[] avoid,bool far)
    {
        var result=Start;float best=float.NegativeInfinity;
        for(int y=8;y<Height-8;y++)for(int x=8;x<Width-8;x++)
        {
            var p=new Vector2Int(x,y);if(!Land(p))continue;float distance=avoid.Min(a=>Vector2.Distance(a,p));
            float score=distance+(far?0:(float)rng.NextDouble()*4);
            if(score>best){best=score;result=p;}
        }
        if(best<9)throw new InvalidOperationException("공터를 충분히 분리할 수 없습니다. 다른 시드를 사용하세요.");return result;
    }
    void Clear(Vector2Int center,int radius)
    {
        for(int y=center.y-radius;y<=center.y+radius;y++)for(int x=center.x-radius;x<=center.x+radius;x++)
        {
            var p=new Vector2Int(x,y);if(!Inside(p)||x<2||y<2||x>=Width-2||y>=Height-2||Vector2.Distance(p,center)>radius)continue;
            Cells[Index(p)]=Terrain.Meadow;Reserved[Index(p)]=true;
        }
    }
    internal HashSet<int> Reachable(Vector2Int start)
    {
        var seen=new HashSet<int>();var queue=new Queue<Vector2Int>();queue.Enqueue(start);seen.Add(Index(start));
        while(queue.Count>0)
        {
            var p=queue.Dequeue();foreach(var d in Directions){var next=p+d;if(Land(next)&&seen.Add(Index(next)))queue.Enqueue(next);}
        }
        return seen;
    }
    void ReservePath(Vector2Int destination)
    {
        var queue=new Queue<Vector2Int>();var previous=new Dictionary<Vector2Int,Vector2Int>();queue.Enqueue(Start);previous[Start]=Start;
        while(queue.Count>0&&!previous.ContainsKey(destination))
        {var p=queue.Dequeue();foreach(var d in Directions){var n=p+d;if(Land(n)&&!previous.ContainsKey(n)){previous[n]=p;queue.Enqueue(n);}}}
        if(!previous.ContainsKey(destination))throw new InvalidOperationException("주요 장소로 가는 육지 경로가 없습니다.");
        for(var p=destination;;p=previous[p])
        {
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){var n=p+new Vector2Int(dx,dy);if(Inside(n))Reserved[Index(n)]=true;}
            if(p==Start)break;
        }
    }
}
