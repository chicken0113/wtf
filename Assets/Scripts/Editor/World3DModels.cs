using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EverythingGrows;

// Small prototype meshes. All prefabs use Y-up, with feet at zero; replace via data later.
internal static class World3DModels
{
    const string Folder="Assets/Resources/Models3D";
    static Material Material(string name,Color color)
    {
        string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
        material=new Material(Shader.Find("Standard")){color=color,enableInstancing=true};material.SetFloat("_Glossiness",.12f);AssetDatabase.CreateAsset(material,path);return material;
    }
    static void Part(Transform root,PrimitiveType shape,Vector3 p,Vector3 scale,Material material)
    {
        var part=GameObject.CreatePrimitive(shape);SceneManager.MoveGameObjectToScene(part,root.gameObject.scene);part.transform.SetParent(root,false);part.transform.localPosition=p;part.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());part.GetComponent<Renderer>().sharedMaterial=material;
    }
    internal static WorldModelLibrary Ensure()
    {
        WorldSceneGenerator.EnsureFolder(Folder);const string path="Assets/Resources/World3DModels.asset";
        var library=AssetDatabase.LoadAssetAtPath<WorldModelLibrary>(path);if(library!=null)return library;
        var brown=Material("Wood",new Color(.43f,.28f,.14f));var green=Material("Leaves",new Color(.23f,.49f,.24f));var grey=Material("Metal",new Color(.58f,.64f,.69f));var white=Material("Cream",new Color(.9f,.86f,.7f));var dark=Material("Dark",new Color(.12f,.16f,.2f));
        var preview=EditorSceneManager.NewPreviewScene();var entries=new System.Collections.Generic.List<WorldModelLibrary.Entry>();
        try
        {
            foreach(ModelKind3D kind in Enum.GetValues(typeof(ModelKind3D)))
            {
                var root=new GameObject(kind.ToString());SceneManager.MoveGameObjectToScene(root,preview);
                void P(PrimitiveType type,Vector3 point,Vector3 size,Material mat)=>Part(root.transform,type,point,size,mat);
                void Box(Vector3 point,Vector3 size,Material mat)=>P(PrimitiveType.Cube,point,size,mat);
                if(kind==ModelKind3D.Ground||kind==ModelKind3D.ForestGround||kind==ModelKind3D.Water||kind==ModelKind3D.Soil)
                {
                    var color=kind==ModelKind3D.Water?new Color(.22f,.49f,.62f):kind==ModelKind3D.Soil?new Color(.31f,.19f,.1f):kind==ModelKind3D.ForestGround?new Color(.3f,.43f,.26f):new Color(.48f,.62f,.32f);
                    Box(new Vector3(0,-.08f,0),new Vector3(1,.16f,1),Material(kind.ToString(),color));
                }
                else if(kind==ModelKind3D.Tree)
                {P(PrimitiveType.Cylinder,new Vector3(0,.6f,0),new Vector3(.2f,.6f,.2f),brown);P(PrimitiveType.Sphere,new Vector3(0,1.4f,0),new Vector3(1.3f,1.4f,1.3f),green);}
                else if(kind==ModelKind3D.Player||kind==ModelKind3D.Merchant||kind==ModelKind3D.Enemy||kind==ModelKind3D.RangedEnemy)
                {
                    var color=kind==ModelKind3D.Player?new Color(.28f,.53f,.8f):kind==ModelKind3D.Merchant?new Color(.8f,.52f,.2f):kind==ModelKind3D.Enemy?new Color(.42f,.66f,.24f):new Color(.65f,.35f,.64f);
                    P(PrimitiveType.Capsule,new Vector3(0,.4f,0),new Vector3(.45f,.4f,.45f),Material(kind.ToString(),color));P(PrimitiveType.Sphere,new Vector3(0,.9f,0),Vector3.one*.4f,white);
                    for(int x=-1;x<=1;x+=2)P(PrimitiveType.Sphere,new Vector3(x*.075f,.94f,.18f),Vector3.one*.055f,dark);
                }
                else if(kind==ModelKind3D.Hoe||kind==ModelKind3D.Sword||kind==ModelKind3D.Bow)
                {Box(new Vector3(0,.4f,0),new Vector3(.07f,.8f,.07f),brown);Box(new Vector3(kind==ModelKind3D.Hoe?.16f:0,.78f,0),kind==ModelKind3D.Hoe?new Vector3(.4f,.12f,.2f):new Vector3(.15f,.45f,.1f),grey);}
                else if(kind==ModelKind3D.WateringCan)
                {Box(new Vector3(0,.25f,0),new Vector3(.45f,.4f,.35f),Material("Can",new Color(.2f,.58f,.7f)));Box(new Vector3(.34f,.36f,0),new Vector3(.35f,.12f,.12f),grey);}
                else if(kind==ModelKind3D.Bed)
                {Box(new Vector3(0,.1f,0),new Vector3(.8f,.2f,1.2f),brown);Box(new Vector3(0,.24f,0),new Vector3(.75f,.15f,1.1f),white);}
                else if(kind==ModelKind3D.Cloud)
                {for(int x=-1;x<=1;x++)P(PrimitiveType.Sphere,new Vector3(x*.3f,.45f,0),Vector3.one*.65f,white);}
                else if(kind==ModelKind3D.House)
                {Box(new Vector3(0,.5f,0),Vector3.one,white);Box(new Vector3(0,1.1f,0),new Vector3(1.2f,.25f,1.2f),brown);}
                else if(kind==ModelKind3D.Plant)
                {P(PrimitiveType.Cylinder,new Vector3(0,.3f,0),new Vector3(.16f,.3f,.16f),Material("Root",new Color(.9f,.5f,.18f)));P(PrimitiveType.Sphere,new Vector3(0,.75f,0),new Vector3(.55f,.35f,.55f),green);}
                else if(kind==ModelKind3D.Border)
                {for(int axis=0;axis<2;axis++)for(int sign=-1;sign<=1;sign+=2)Box(axis==0?new Vector3(sign*.48f,0,0):new Vector3(0,0,sign*.48f),axis==0?new Vector3(.035f,.025f,1):new Vector3(1,.025f,.035f),Material("Highlight",Color.green));}
                else
                {var material=kind==ModelKind3D.Heart?Material("Heart",new Color(.85f,.17f,.22f)):kind==ModelKind3D.Projectile?Material("Bullet",new Color(1,.7f,.12f)):kind==ModelKind3D.Seed?brown:grey;P(PrimitiveType.Sphere,new Vector3(0,.3f,0),new Vector3(.65f,.6f,.65f),material);}
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+kind+".prefab");entries.Add(new WorldModelLibrary.Entry {kind=kind,prefab=prefab});UnityEngine.Object.DestroyImmediate(root);
            }
            library=ScriptableObject.CreateInstance<WorldModelLibrary>();library.models=entries.ToArray();AssetDatabase.CreateAsset(library,path);return library;
        }
        finally{EditorSceneManager.ClosePreviewScene(preview);}
    }
}
