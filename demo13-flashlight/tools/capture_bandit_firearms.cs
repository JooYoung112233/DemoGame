const string root="Assets/ChibiSurvivor/Bandit/Firearms",output="D:/Demo/ArtWork/SimpleBandit/Firearms";
System.IO.Directory.CreateDirectory(output+"/Frames");
var rows=new System.Collections.Generic.List<string>();
for(int frame=0;frame<140;frame++){ string mode="Timeline";
 var utility=new UnityEditor.PreviewRenderUtility();var stage=new GameObject("Firearms review");var garbage=new System.Collections.Generic.List<UnityEngine.Object>();
 try{
  for(int i=0;i<2;i++){
   var kind=i==0?"Pistol":"Rifle";var go=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/Bandit_"+kind+".prefab"));garbage.Add(go);
   var bones=go.GetComponentsInChildren<Transform>();var gun=bones.Single(t=>t.name=="Firearm");
   go.GetComponent<BanditFirearmPreview>().SampleTimeline(frame/10f);
   var pivot=new GameObject(kind).transform;pivot.SetParent(stage.transform,false);pivot.localPosition=new Vector3(i==0?-.75f:.75f,0,0);pivot.localRotation=Quaternion.Euler(0,mode=="StowedBack"?150:mode=="Aim"||mode=="Walk"||mode=="Shoot"?35:20,0);
   foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()){
    var mesh=new Mesh();skin.BakeMesh(mesh);garbage.Add(mesh);var body=new GameObject("Body");body.transform.SetParent(pivot,false);body.transform.localPosition=skin.transform.position;body.transform.localRotation=skin.transform.rotation;body.AddComponent<MeshFilter>().sharedMesh=mesh;body.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
   }
   foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>()){
    var mesh=renderer.GetComponent<MeshFilter>();if(mesh==null)continue;
    var prop=new GameObject(renderer.name);prop.transform.SetParent(pivot,false);prop.transform.localPosition=renderer.transform.position;prop.transform.localRotation=renderer.transform.rotation;prop.transform.localScale=renderer.transform.lossyScale;prop.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;prop.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
   }
   foreach(var r in go.GetComponentsInChildren<Renderer>())r.enabled=false;
  }
  utility.AddSingleGO(stage);utility.camera.transform.position=new Vector3(0,1.55f,6);utility.camera.transform.LookAt(new Vector3(0,.95f,0));utility.camera.orthographic=true;utility.camera.orthographicSize=1.16f;utility.camera.nearClipPlane=.01f;utility.camera.farClipPlane=20;utility.camera.clearFlags=CameraClearFlags.SolidColor;utility.camera.backgroundColor=new Color(.055f,.065f,.08f);
  utility.lights[0].intensity=1.8f;utility.lights[0].transform.rotation=Quaternion.Euler(38,-25,0);utility.lights[1].intensity=1.1f;utility.lights[1].transform.rotation=Quaternion.Euler(20,150,0);utility.ambientColor=new Color(.45f,.45f,.45f);
  utility.BeginStaticPreview(new Rect(0,0,900,600));utility.Render(true);var png=utility.EndStaticPreview();System.IO.File.WriteAllBytes(output+"/Frames/"+frame.ToString("D4")+".png",png.EncodeToPNG());UnityEngine.Object.DestroyImmediate(png);rows.Add(mode);
 }finally{utility.Cleanup();foreach(var o in garbage)UnityEngine.Object.DestroyImmediate(o);}
}
return rows;
