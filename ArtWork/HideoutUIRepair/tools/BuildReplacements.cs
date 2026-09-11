using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;

// Metric UVs, chamfered edges, separate roof meshes. Existing authored Town02 materials.
public static class BuildTownReplacements
{
    const string Dir="Assets/Art/Environments/TownReplacements62";
    static readonly List<Vector3> V=new List<Vector3>();
    static readonly List<Vector2> UV=new List<Vector2>();
    static readonly Dictionary<string,List<int>> Tris=new Dictionary<string,List<int>>();
    static readonly List<object> Audit=new List<object>();
    static GameObject root;
    static void Polygon(Vector3[] points,Vector3 outward,Vector3 center,Quaternion rot,string mat)
    {
        if(Vector3.Dot(Vector3.Cross(points[1]-points[0],points[2]-points[0]),outward)<0)Array.Reverse(points);
        int start=V.Count;var n=rot*outward;
        int axis=Mathf.Abs(n.x)>Mathf.Abs(n.y)?(Mathf.Abs(n.x)>Mathf.Abs(n.z)?0:2):(Mathf.Abs(n.y)>Mathf.Abs(n.z)?1:2);
        foreach(var p in points){var w=center+rot*p;V.Add(w);UV.Add(axis==0?new Vector2(w.z,w.y):axis==1?new Vector2(w.x,w.z):new Vector2(w.x,w.y));}
        if(!Tris.ContainsKey(mat))Tris[mat]=new List<int>();
        for(int i=1;i<points.Length-1;i++){Tris[mat].Add(start);Tris[mat].Add(start+i);Tris[mat].Add(start+i+1);}
    }
    static void Box(Vector3 center,Vector3 size,string mat,float bevel=.025f,Quaternion? rotation=null)
    {
        var h=size*.5f;float r=Mathf.Min(bevel,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.4f);var rot=rotation??Quaternion.identity;
        for(int a=0;a<3;a++)for(int sign=-1;sign<=1;sign+=2){int b=(a+1)%3,c=(a+2)%3;var ps=new Vector3[4];int i=0;
            foreach(var st in new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)}){var p=Vector3.zero;p[a]=sign*h[a];p[b]=st.x*(h[b]-r);p[c]=st.y*(h[c]-r);ps[i++]=p;}
            var n=Vector3.zero;n[a]=sign;Polygon(ps,n,center,rot,mat);}
        for(int a=0;a<3;a++)for(int b=a+1;b<3;b++){int c=3-a-b;for(int sa=-1;sa<=1;sa+=2)for(int sb=-1;sb<=1;sb+=2){
            var ps=new Vector3[4];for(int i=0;i<4;i++){var p=Vector3.zero;bool nearA=i==0||i==3;p[a]=sa*(h[a]-(nearA?0:r));p[b]=sb*(h[b]-(nearA?r:0));p[c]=(i<2?-1:1)*(h[c]-r);ps[i]=p;}
            var n=Vector3.zero;n[a]=sa;n[b]=sb;Polygon(ps,n,center,rot,mat);}}
        for(int sx=-1;sx<=1;sx+=2)for(int sy=-1;sy<=1;sy+=2)for(int sz=-1;sz<=1;sz+=2){var s=new Vector3(sx,sy,sz);var ps=new Vector3[3];for(int a=0;a<3;a++){var p=Vector3.Scale(h-Vector3.one*r,s);p[a]=h[a]*s[a];ps[a]=p;}Polygon(ps,s,center,rot,mat);}
    }
    static void B(float x,float y,float z,float w,float h,float d,string mat,float angle=0)=>Box(new Vector3(x,y,z),new Vector3(w,h,d),mat,.025f,Quaternion.Euler(angle,0,0));
    static void Beam(Vector3 a,Vector3 b,float thickness,string mat)=>Box((a+b)*.5f,new Vector3(thickness,thickness,(b-a).magnitude),mat,.012f,Quaternion.LookRotation(b-a));
    static void Flush(string name)
    {
        if(V.Count==0)return;
        string path=Dir+"/"+root.name+"_"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool isNew=mesh==null;
        if(isNew)mesh=new Mesh{name=name};else mesh.Clear();
        mesh.SetVertices(V);mesh.SetUVs(0,UV);mesh.subMeshCount=Tris.Count;
        var materials=new List<Material>();int i=0;foreach(var kv in Tris){mesh.SetTriangles(kv.Value,i++);var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environments/Town02/Materials/Town_"+kv.Key+".mat");if(m==null)throw new Exception("Missing material "+kv.Key);materials.Add(m);}
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        mesh.UploadMeshData(false);
        if(isNew)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=materials.ToArray();
        Audit.Add(new{model=root.name,part=name,vertices=V.Count,triangles=Tris.Sum(k=>k.Value.Count)/3,bounds=mesh.bounds.ToString(),uv=mesh.uv.Length,normals=mesh.normals.Length});V.Clear();UV.Clear();Tris.Clear();
    }
    static void Window(float x,float z)
    {
        B(x,1.65f,z,1.65f,1.15f,.14f,"WoodDark");B(x,1.67f,z-.09f,1.42f,.92f,.045f,"Glass");
        foreach(float s in new[]{-1f,1f}){B(x+s*.78f,1.65f,z-.14f,.1f,1.2f,.15f,"Wood");B(x,1.65f+s*.56f,z-.14f,1.65f,.1f,.15f,"Wood");}
        B(x,1.65f,z-.16f,.06f,1.1f,.08f,"WoodDark");B(x,1.02f,z-.19f,1.88f,.12f,.35f,"Concrete");
    }
    static void Crate(float x,float z,float w,float h,float d)
    {
        B(x,h*.5f+.12f,z,w,h,d,"WoodDark");
        for(int i=0;i<4;i++)B(x-w*.5f+(i+.5f)*w/4,h+.14f,z,w/4-.025f,.07f,d,"Wood");
        foreach(float s in new[]{-1f,1f}){B(x+s*(w*.5f-.12f),h*.5f+.12f,z-d*.5f-.025f,.09f,h,.05f,"Wood");B(x,h*.5f+.12f,z+s*d*.5f,w,.1f,.045f,"Steel");}
    }
    static void Home()
    {
        root=new GameObject("ReclaimedHome62");
        B(0,.18f,0,14,.36f,9,"Concrete");
        B(7,1.45f,0,.22f,2.9f,9,"Plaster");
        foreach(float s in new[]{-1f,1f}){B(0,1.45f,s*4.4f,14,2.9f,.22f,"Plaster");B(-7,1.45f,s*2.65f,.22f,2.9f,3.6f,"Plaster");B(0,.47f,s*4.54f,14,.45f,.12f,"Brick");}
        B(-7,2.62f,0,.22f,.56f,1.7f,"Plaster");
        // West-facing entrance, same approach as the existing hideout trigger.
        B(-7.15f,1.2f,0,.12f,2.3f,1.4f,"WoodDark");
        foreach(float s in new[]{-1f,1f})B(-7.24f,1.23f,s*.77f,.16f,2.46f,.12f,"Wood");
        B(-7.24f,2.45f,0,.16f,.12f,1.68f,"Wood");B(-7.24f,1.82f,0,.07f,.42f,.74f,"Glass");B(-7.28f,1.07f,-.46f,.10f,.1f,.17f,"Edge");
        B(-7.43f,.12f,0,.8f,.24f,2.05f,"Concrete");
        foreach(float x in new[]{-4.5f,0f,4.5f})Window(x,-4.58f);
        foreach(float x in new[]{-6.85f,6.85f})B(x,1.5f,-4.56f,.20f,2.9f,.15f,"WoodDark");
        Flush("Walls");
        foreach(float s in new[]{-1f,1f}){
            B(0,3.67f,s*2.30f,14.5f,.16f,4.83f,"Roof",s*17f);
            for(int i=0;i<=20;i++)B(-7.15f+i*.715f,3.77f,s*2.30f,.038f,.055f,4.82f,"Steel",s*17f);
            B(0,2.98f,s*4.62f,14.5f,.17f,.16f,"WoodDark");
        }
        B(0,4.40f,0,14.6f,.14f,.22f,"Steel");
        // Gables and fascia make this a pitched-roof dwelling, not a freight box.
        foreach(float x in new[]{-7.03f,7.03f})for(int i=0;i<18;i++){
            float z=-4.25f+i*.5f;float h=1.38f*(1-Mathf.Abs(z)/4.6f);B(x,2.94f+h*.5f,z,.15f,h,.48f,"Wood");
        }
        foreach(float x in new[]{-7.3f,7.3f})foreach(float s in new[]{-1f,1f})Beam(new Vector3(x,4.42f,0),new Vector3(x,3.02f,s*4.67f),.15f,"WoodDark");
        B(4,4.45f,1,.55f,1.25f,.6f,"Brick");B(4,5.10f,1,.78f,.13f,.82f,"Concrete");
        Flush("PitchedRoof");Save();
    }
    static void Shelter(string name,float w,bool workshop)
    {
        root=new GameObject(name);
        B(0,.10f,0,w,.2f,2.4f,"Concrete");
        foreach(float x in new[]{-w*.5f+.12f,0,w*.5f-.12f})foreach(float z in new[]{-1.08f,1.08f})B(x,1.14f,z,.16f,2.28f,.16f,"WoodDark");
        B(0,2.24f,-1.08f,w,.2f,.18f,"WoodDark");B(0,2.5f,1.08f,w,.2f,.18f,"WoodDark");
        for(int i=0;i<(int)(w/.22f);i++)B(-w*.5f+.11f+i*.22f,1.25f,1.1f,.19f,2.1f,.08f,workshop?"Olive":"Wood");
        foreach(float x in new[]{-w*.5f+.12f,w*.5f-.12f})Beam(new Vector3(x,.25f,.92f),new Vector3(x,2.25f,-.92f),.12f,"Wood");
        if(workshop){B(w*.25f,1.1f,0,w*.45f,2.0f,2.02f,"Plaster");Window(w*.25f,-1.03f);B(-w*.22f,.82f,-.68f,w*.40f,.15f,.7f,"Wood");B(-w*.16f,1.02f,-.8f,.8f,.28f,.4f,"Red");foreach(float x in new[]{-w*.38f,-w*.06f})B(x,.42f,-.68f,.12f,.84f,.12f,"WoodDark");Crate(-w*.36f,-.7f,.8f,.65f,.65f);}
        else {Crate(-w*.28f,-.2f,1.2f,.85f,1.1f);Crate(w*.24f,.35f,1.4f,1.2f,1.1f);Crate(w*.24f,-.55f,1.1f,.55f,.6f);}
        Flush("Structure");
        if(workshop){
            // Set back the cover over the work bay so tools read from the 62 degree camera.
            B(-w*.25f,2.62f,.65f,w*.5f+.13f,.13f,1.3f,"DarkOlive",-7);
            B(w*.25f,2.55f,0,w*.5f+.13f,.13f,2.6f,"DarkOlive",-7);
            for(float x=-w*.5f;x<=w*.5f;x+=.4f)B(x,x<0?2.70f:2.63f,x<0?.65f:0,.035f,.04f,x<0?1.28f:2.58f,"Steel",-7);
        }else{
            B(0,2.55f,0,w+.26f,.13f,2.6f,"DarkOlive",-7);
            for(float x=-w*.5f;x<=w*.5f;x+=.4f)B(x,2.63f,0,.035f,.04f,2.58f,"Steel",-7);
        }
        Flush("Canopy");Save();
    }
    static void Save(){PrefabUtility.SaveAsPrefabAsset(root,Dir+"/"+root.name+".prefab");UnityEngine.Object.DestroyImmediate(root);}
    public static string Build()
    {
        Directory.CreateDirectory(Dir);AssetDatabase.Refresh();Audit.Clear();Home();Shelter("TimberSupplyShelter62",6,false);Shelter("WorkshopStore62",8,true);AssetDatabase.SaveAssets();
        var json=JsonConvert.SerializeObject(Audit,Formatting.Indented);File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/HideoutUIRepair/ModelAudit.json")),json);return json;
    }
    public static string Install()
    {
        if(Application.isPlaying)throw new Exception("Stop review play before scene persistence");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Safehouse.unity",OpenSceneMode.Additive);
        try{
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var pairs=new[]{("Container_Home","ReclaimedHome62"),("Container_D1","TimberSupplyShelter62"),("Container_D2","WorkshopStore62")};
            foreach(var pair in pairs){var target=all.Single(t=>t!=null && t.name==pair.Item1);
                var existing=target.Find(pair.Item2);if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);
                foreach(var r in target.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                var detail=all.FirstOrDefault(t=>t!=null && t.name==pair.Item1+"_Detail");if(detail!=null)detail.gameObject.SetActive(false);
                if(pair.Item1=="Container_Home")foreach(var t in all.Where(t=>t!=null && (t.name=="Home_Door"||t.name=="Home_Door_Handle")))t.gameObject.SetActive(false);
                if(pair.Item1=="Container_Home")foreach(var t in all.Where(t=>t!=null && t.name=="Window01" && t.parent!=null && t.parent.name=="HomeYard"))t.gameObject.SetActive(false);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"/"+pair.Item2+".prefab"),scene);
                go.name=pair.Item2;go.transform.position=new Vector3(target.position.x,0,target.position.z);go.transform.SetParent(target,true);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Installed 3 replacements; original collision and entrance retained";
        }finally{EditorSceneManager.CloseScene(scene,true);}
    }
}
