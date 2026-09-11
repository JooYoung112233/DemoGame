// Reapply the approved hideout-only dressing and lighting without rebuilding facility anchors.
if (UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Apply in Edit Mode.");
const string folder = "Assets/Art/Environments/Hideout02";
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Hideout.unity");
bool opened = !scene.isLoaded;
if (!opened && scene.isDirty) throw new System.Exception("Preserve unsaved Hideout edits first.");
if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Hideout.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
System.Func<float,float,float,Vector3> pos = (x,y,z) => new Vector3(-x,z,-y);
System.Action<GameObject> dress = root => {
    var old = root.transform.Find("LivedInDetails");
    if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
    var group = new GameObject("LivedInDetails"); group.transform.SetParent(root.transform,false);
    System.Action<string,float,float,float,float,float> prop = (name,x,y,z,scale,yaw) => {
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/Prefabs/"+name+".prefab");
        if (asset == null) throw new System.Exception("Missing prop: "+name);
        var o = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(asset,root.scene);
        o.transform.SetParent(group.transform,false); o.transform.localPosition=pos(x,y,z);
        o.transform.localScale=Vector3.one*scale; o.transform.localRotation=Quaternion.Euler(0,-yaw,0);
        // Shelf-top dressing is not a new gameplay blocker.
        foreach(var c in o.GetComponentsInChildren<Collider>()) c.enabled=false;
    };
    prop("SupplyCase02",-2.48f,2.47f,1.228f,.62f,-8);
    prop("SupplyCase02",-1.55f,2.48f,.708f,.68f,7);
    prop("SupplyCase02",-2.40f,2.49f,.40f,.68f,-5);
    prop("Tin02",-1.77f,2.48f,1.228f,1.05f,0);
    prop("Tin02",-1.60f,2.48f,1.228f,.90f,0);
    prop("Tin02",-2.31f,2.48f,.708f,.86f,0);
    prop("Tin02",-2.17f,2.45f,.898f,.72f,0);
    prop("Tin02",2.57f,1.20f,.855f,.8f,0);
    prop("Tin02",-2.52f,-2.45f,.855f,.85f,0);
    prop("WoodCrate01",2.48f,.80f,.06f,.54f,6);
    System.Action<string,PrimitiveType,Vector3,Vector3,Vector3,string> detail=(name,type,position,scale,rotation,material)=>{
        var o=GameObject.CreatePrimitive(type); o.name=name;o.transform.SetParent(group.transform,false);
        o.transform.localPosition=position;o.transform.localScale=scale;o.transform.localEulerAngles=rotation;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        o.GetComponent<Renderer>().sharedMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(folder+"/Materials/Hideout_"+material+".mat");
    };
    for(int i=0;i<3;i++) detail("WorkbenchNotes",PrimitiveType.Cube,pos(2.43f,.20f,.856f+i*.003f),new Vector3(.20f,.003f,.26f),new Vector3(0,-8+i*4,0),"Paper");
    detail("FoldedCloth",PrimitiveType.Cube,pos(-1.63f,2.48f,.223f),new Vector3(.26f,.065f,.27f),new Vector3(0,8,0),"Blanket");
    detail("FoldedClothTop",PrimitiveType.Cube,pos(-1.64f,2.48f,.279f),new Vector3(.25f,.046f,.26f),new Vector3(0,11,0),"Canvas");
    for(int i=0;i<2;i++) detail("StoredBandage",PrimitiveType.Cylinder,pos(2.45f,-1.75f,.32f+i*.115f),new Vector3(.11f,.13f,.11f),new Vector3(90,0,0),"Linen");
    var lightGO=new GameObject("WorkbenchWarmLight");lightGO.transform.SetParent(group.transform,false);lightGO.transform.localPosition=pos(2.25f,.75f,1.65f);
    var light=lightGO.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,.78f,.52f);light.intensity=.55f;light.range=3.2f;light.shadows=LightShadows.Soft;light.shadowNormalBias=.15f;
    foreach(var lamp in root.GetComponentsInChildren<Light>()) if(lamp.name=="LanternLight") { lamp.intensity=.25f;lamp.range=2.8f; }
};
var prefab=UnityEditor.PrefabUtility.LoadPrefabContents(folder+"/Hideout02_Interior.prefab");
try { dress(prefab);UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab,folder+"/Hideout02_Interior.prefab"); }
finally { UnityEditor.PrefabUtility.UnloadPrefabContents(prefab); }
try {
    var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
    var room=all.Single(t=>t.name=="Hideout02_Placed");dress(room.gameObject);
    foreach(var l in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true))) {
        if(l.name=="Sun3D") { l.intensity=.95f;l.color=new Color(1f,.89f,.76f); }
        if(l.name=="Bulb") { l.intensity=.45f;l.color=new Color(1f,.83f,.63f); }
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    return new {decorations=room.Find("LivedInDetails").childCount,facilities=room.GetComponentsInChildren<HideoutFacilityAnchor>().Length,sceneSaved=true};
} finally {if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
