using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

// Editable mesh symbols; no gameplay, collision or runtime generation is attached.
public static class TownRoofIdentitySetup
{
    const string Art="Assets/Art/RoofSigns", Prefabs="Assets/Prefabs/Map/RoofSigns", Child="Roof identity";
    static readonly (string group,string building,string symbol,string label)[] Entries={
        ("Buildings","TownGuild","Guild","GUILD"),("Buildings","TownSmith","Smith","SMITH"),
        ("Buildings","TownGeneralStore","Store","GENERAL STORE"),("Buildings","TownHouse","Home","VILLAGE HOUSE"),
        ("Town districts","Clinic exterior","Clinic",""),("Town districts","Saloon exterior","Tavern",""),
        ("Town districts","River Road house","HomeBlue",""),("Town districts","Willow Lane east house","HomePurple",""),
        ("Town districts","Mayor house exterior","Mayor",""),("Town districts","Riverside trailer","Trailer",""),
        ("Town districts","East market exterior","Market",""),("Town districts","Museum exterior","Museum","")
    };
    static readonly Color Dark=new Color(.12f,.16f,.2f),Gold=new Color(.94f,.7f,.27f),Cream=new Color(1,.94f,.77f),Steel=new Color(.76f,.88f,.94f),Blue=new Color(.18f,.48f,.78f),Wood=new Color(.53f,.29f,.12f),Red=new Color(.87f,.2f,.22f),Green=new Color(.35f,.7f,.28f);
    static void Folder(string path)
    {if(AssetDatabase.IsValidFolder(path))return;int split=path.LastIndexOf('/');Folder(path.Substring(0,split));AssetDatabase.CreateFolder(path.Substring(0,split),path.Substring(split+1));}
    static Vector2 V(float x,float y)=>new Vector2(x,y);
    static Mesh Shape(string name)
    {
        var existing=AssetDatabase.LoadAllAssetsAtPath(Art+"/Shapes.asset").OfType<Mesh>().FirstOrDefault(m=>m.name==name);if(existing!=null)return existing;
        Vector2[] points;
        if(name=="Disc")points=Enumerable.Range(0,32).Select(i=>V(Mathf.Cos(i*Mathf.PI/16)*.5f,Mathf.Sin(i*Mathf.PI/16)*.5f)).ToArray();
        else if(name=="Shield")points=new[]{V(-.5f,.5f),V(.5f,.5f),V(.42f,-.18f),V(0,-.55f),V(-.42f,-.18f)};
        else if(name=="Triangle")points=new[]{V(-.5f,-.5f),V(.5f,-.5f),V(0,.5f)};
        else if(name=="Heart")points=new[]{V(-.5f,.15f),V(-.45f,.4f),V(-.25f,.5f),V(0,.3f),V(.25f,.5f),V(.45f,.4f),V(.5f,.15f),V(.4f,-.1f),V(0,-.5f),V(-.4f,-.1f)};
        else if(name=="Crown")points=new[]{V(-.5f,-.4f),V(.5f,-.4f),V(.5f,.4f),V(.23f,.05f),V(0,.5f),V(-.23f,.05f),V(-.5f,.4f)};
        else points=new[]{V(-.5f,-.5f),V(-.5f,.5f),V(.5f,.5f),V(.5f,-.5f)};
        var mesh=new Mesh{name=name};mesh.vertices=new[]{Vector3.zero}.Concat(points.Select(p=>(Vector3)p)).ToArray();
        mesh.uv=mesh.vertices.Select(v=>new Vector2(v.x+.5f,v.y+.5f)).ToArray();mesh.colors=mesh.vertices.Select(v=>Color.white).ToArray();
        mesh.triangles=Enumerable.Range(0,points.Length).SelectMany(i=>new[]{0,i+1,(i+1)%points.Length+1}).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
        if(AssetDatabase.LoadMainAssetAtPath(Art+"/Shapes.asset")==null)AssetDatabase.CreateAsset(mesh,Art+"/Shapes.asset");else AssetDatabase.AddObjectToAsset(mesh,Art+"/Shapes.asset");return mesh;
    }
    static Material Material(Color color)
    {
        string path=Art+"/Ink_"+ColorUtility.ToHtmlStringRGB(color)+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
        material=new Material(Shader.Find("Sprites/Default")){name="Roof ink "+ColorUtility.ToHtmlStringRGB(color),color=color};AssetDatabase.CreateAsset(material,path);return material;
    }
    static void Part(Transform parent,string name,string shape,Vector2 point,Vector2 size,Color color,int order,float angle=0)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=point;go.transform.localScale=new Vector3(size.x,size.y,1);go.transform.localRotation=Quaternion.Euler(0,0,angle);
        go.AddComponent<MeshFilter>().sharedMesh=Shape(shape);var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=Material(color);renderer.sortingOrder=order;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    static GameObject Make(string symbol)
    {
        string path=Prefabs+"/Roof"+symbol+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab!=null)return prefab;
        var root=new GameObject("Roof "+symbol);var t=root.transform;
        Part(t,"Brass rim","Disc",V(0,0),V(2.4f,2.1f),Gold,0);Part(t,"Dark sign backing","Disc",V(0,0),V(2.19f,1.9f),Dark,1);
        if(symbol=="Smith")
        {
            Part(t,"Hammer handle","Quad",V(-.13f,-.24f),V(.2f,1.15f),Wood,2,-35);
            Part(t,"Hammer head","Quad",V(.22f,.35f),V(1.05f,.48f),Steel,3,-35);
            Part(t,"Head highlight","Quad",V(.14f,.49f),V(.84f,.1f),Cream,4,-35);
        }
        else if(symbol=="Guild")
        {
            Part(t,"Sword blade","Quad",V(-.43f,.22f),V(.19f,1.05f),Steel,2);
            Part(t,"Sword point","Triangle",V(-.43f,.78f),V(.19f,.22f),Cream,3);
            Part(t,"Sword grip","Quad",V(-.43f,-.49f),V(.16f,.35f),Wood,3);
            Part(t,"Sword guard","Quad",V(-.43f,-.28f),V(.6f,.12f),Gold,4);
            Part(t,"Shield rim","Shield",V(.31f,-.08f),V(.95f,1.28f),Steel,5);
            Part(t,"Shield face","Shield",V(.31f,-.06f),V(.74f,1.02f),Blue,6);
            Part(t,"Shield crest vertical","Quad",V(.31f,-.06f),V(.11f,.65f),Gold,7);
            Part(t,"Shield crest horizontal","Quad",V(.31f,.05f),V(.46f,.11f),Gold,8);
        }
        else if(symbol=="Store")
        {
            Part(t,"Produce crate","Quad",V(0,-.23f),V(1.28f,.85f),Wood,2);
            foreach(float y in new[]{-.5f,-.15f,.17f})Part(t,"Crate slat","Quad",V(0,y),V(1.35f,.1f),Gold,4);
            Part(t,"Red produce","Disc",V(-.32f,.3f),V(.56f,.56f),Red,3);
            Part(t,"Green produce","Disc",V(.32f,.32f),V(.56f,.56f),Green,3);
            Part(t,"Leaf","Triangle",V(.3f,.66f),V(.3f,.3f),Green,4,-25);
        }
        else if(symbol=="Clinic")
        {
            Part(t,"Medical disc","Disc",V(0,0),V(1.5f,1.5f),Cream,2);
            Part(t,"Medical cross upright","Quad",V(0,0),V(.35f,1.13f),Red,3);
            Part(t,"Medical cross bar","Quad",V(0,0),V(1.13f,.35f),Red,4);
        }
        else if(symbol=="Tavern")
        {
            Part(t,"Mug handle outer","Disc",V(.51f,-.07f),V(.67f,.76f),Gold,2);
            Part(t,"Mug handle hole","Disc",V(.51f,-.07f),V(.35f,.43f),Dark,3);
            Part(t,"Mug body","Quad",V(-.12f,-.13f),V(.94f,1.05f),Gold,4);
            Part(t,"Mug highlight","Quad",V(-.33f,-.16f),V(.13f,.65f),Cream,5);
            foreach(float x in new[]{-.43f,-.13f,.17f})Part(t,"Foam","Disc",V(x,.45f),V(.47f,.43f),Cream,6);
        }
        else if(symbol=="Mayor")
        {
            Part(t,"Crown","Crown",V(0,.06f),V(1.43f,1.25f),Gold,2);
            Part(t,"Crown band","Quad",V(0,-.4f),V(1.43f,.18f),Cream,3);
            Part(t,"Crown gem","Disc",V(0,-.15f),V(.22f,.22f),Red,4);
        }
        else if(symbol=="Trailer")
        {
            Part(t,"Caravan body","Quad",V(-.08f,.1f),V(1.45f,.86f),Cream,2);
            Part(t,"Caravan window","Quad",V(-.33f,.17f),V(.44f,.39f),Blue,3);
            Part(t,"Caravan door","Quad",V(.31f,-.02f),V(.27f,.61f),Wood,3);
            foreach(float x in new[]{-.47f,.38f}){Part(t,"Wheel","Disc",V(x,-.4f),V(.45f,.45f),Steel,4);Part(t,"Wheel hub","Disc",V(x,-.4f),V(.2f,.2f),Dark,5);}
        }
        else if(symbol=="Market")
        {
            Part(t,"Coin left","Disc",V(-.35f,-.25f),V(.92f,.92f),Gold,2);
            Part(t,"Coin right","Disc",V(.35f,-.25f),V(.92f,.92f),Gold,3);
            Part(t,"Coin top","Disc",V(0,.32f),V(.99f,.99f),Cream,4);
            Part(t,"Coin inset","Disc",V(0,.32f),V(.76f,.76f),Gold,5);
            Part(t,"Coin mark","Quad",V(0,.32f),V(.13f,.5f),Wood,6);
        }
        else if(symbol=="Museum")
        {
            Part(t,"Book spine","Quad",V(0,-.08f),V(.18f,1.19f),Gold,2);
            Part(t,"Book left cover","Quad",V(-.38f,-.08f),V(.73f,1.18f),Gold,2,12);
            Part(t,"Book right cover","Quad",V(.38f,-.08f),V(.73f,1.18f),Gold,2,-12);
            Part(t,"Book left pages","Quad",V(-.35f,0),V(.59f,.95f),Cream,3,12);
            Part(t,"Book right pages","Quad",V(.35f,0),V(.59f,.95f),Cream,3,-12);
            foreach(float y in new[]{-.2f,.04f,.28f}){Part(t,"Left text","Quad",V(-.35f,y),V(.37f,.055f),Wood,4,12);Part(t,"Right text","Quad",V(.35f,y),V(.37f,.055f),Wood,4,-12);}
        }
        else
        {
            Color accent=symbol=="HomeBlue"?Blue:symbol=="HomePurple"?new Color(.6f,.36f,.78f):Red;
            Part(t,"Home walls","Quad",V(0,-.2f),V(.98f,.87f),Cream,2);
            Part(t,"Home roof","Triangle",V(0,.42f),V(1.5f,.65f),accent,3);
            Part(t,"Home heart","Heart",V(0,-.16f),V(.49f,.52f),accent,4);
        }
        prefab=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);return prefab;
    }
    // One-time authoring entry retained for fixtures; no production menu.
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before authoring.");
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Save open scenes first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Folder(Art);Folder(Prefabs);EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);var map=UnityEngine.Object.FindFirstObjectByType<FarmGame>().Layout;
            foreach(var entry in Entries)
            {
                var building=map.transform.Find(entry.group+"/"+entry.building);if(building==null)throw new Exception("Missing building: "+entry.building);
                if(building.Find(Child)!=null)continue;
                var roof=building.GetComponentsInChildren<SpriteRenderer>().First();var prefab=Make(entry.symbol);
                var badge=(GameObject)PrefabUtility.InstantiatePrefab(prefab,building);badge.name=Child;
                badge.transform.localScale=new Vector3(1/building.lossyScale.x,1/building.lossyScale.y,1);
                badge.transform.position=new Vector3(building.position.x,roof.bounds.max.y+.25f,building.position.z);
                foreach(var renderer in badge.GetComponentsInChildren<MeshRenderer>()){renderer.sortingOrder+=roof.sortingOrder+4;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
                PrefabUtility.RecordPrefabInstancePropertyModifications(badge.transform);
                if(entry.label.Length>0)foreach(var label in map.GetComponentsInChildren<WorldLabel>().Where(l=>l.text==entry.label))label.transform.position=badge.transform.position+Vector3.up*1.5f;
            }
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);EditorSceneManager.SaveScene(map.gameObject.scene);AssetDatabase.SaveAssets();Debug.Log("TOWN_ROOF_IDENTITIES_APPLIED: 12 editable roof symbols.");
        }
        finally{if(setup.Any(s=>s.isLoaded&&s.isActive))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.OpenScene(CombatTestMapSetup.FarmScene);}
    }
    public static void Verify()
    {
        if(!Application.isBatchMode)throw new Exception("Use isolated validation.");Apply();WorldConnectionValidator.Validate();EditorSceneManager.OpenScene(WorldFrameworkSetup.Town);
        var farm=UnityEngine.Object.FindFirstObjectByType<FarmGame>();
        foreach(var entry in Entries)
        {
            var sign=farm.Layout.transform.Find(entry.group+"/"+entry.building+"/"+Child);if(sign==null||sign.GetComponentsInChildren<MapObstacle>().Length>0||sign.GetComponentsInChildren<Collider2D>().Length>0)throw new Exception("Missing or obstructive roof sign: "+entry.building);
            if(sign.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterial==null||r.GetComponent<MeshFilter>().sharedMesh==null))throw new Exception("Missing roof artwork.");
        }
        PelicanTownLayout.Capture(farm,"RoofIdentitiesOverview.png");
        var camera=Camera.main;camera.transform.position=new Vector3(-17,17,-10);camera.orthographicSize=4.6f;
        var target=new RenderTexture(900,800,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var picture=new Texture2D(900,800,TextureFormat.RGBA32,false);picture.ReadPixels(new Rect(0,0,900,800),0,0);picture.Apply();System.IO.File.WriteAllBytes("GuildRoofPreview.png",picture.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(picture);UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("TOWN_ROOF_IDENTITIES_VERIFIED: all twelve signs, artwork and map connections checked.");
    }
}
