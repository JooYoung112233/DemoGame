const string game="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game",output="D:/Demo/ArtWork/PlayerFirearms";
System.IO.Directory.CreateDirectory(output);var rows=new System.Collections.Generic.List<string>();
foreach(var mode in new[]{"Walk","SprintFront","SprintBack"}){
 var utility=new UnityEditor.PreviewRenderUtility();var stage=new GameObject("Player weapons");var garbage=new System.Collections.Generic.List<UnityEngine.Object>();var actors=new System.Collections.Generic.List<GameObject>();
 try{
  for(int i=0;i<4;i++){
   var actor=new GameObject("Player");actors.Add(actor);var visual=actor.AddComponent<ChibiPlayerVisual>();
   visual.Initialize(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(game+"/SimpleHero.prefab"),UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(game+"/SimpleHero.controller"),null,1);
   var animator=actor.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
   if(i<2){var data=ScriptableObject.CreateInstance<WeaponData>();garbage.Add(data);data.isRanged=true;data.firearmStance=i==0?WeaponData.FirearmStance.Pistol:WeaponData.FirearmStance.AssaultRifle;visual.SetFirearmEquipped(data);}else visual.SetMeleeEquipped(true,i==2);
   bool sprint=mode.StartsWith("Sprint");
   for(int f=0;f<100;f++){visual.SetSprintWeaponStowed(sprint);visual.UpdateMotion(Vector2.up,sprint?5:2,sprint,true,1,.02f);animator.Update(.02f);actor.GetComponentInChildren<PlayerFirearmVisual>()?.Tick(.02f);}
   var pivot=new GameObject("Pose").transform;pivot.SetParent(stage.transform,false);pivot.localPosition=new Vector3((i-1.5f)*1.2f,0,0);pivot.localRotation=Quaternion.Euler(0,mode=="SprintBack"?155:30,0);
   foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()){
    if(!skin.enabled)continue;var mesh=new Mesh();skin.BakeMesh(mesh);garbage.Add(mesh);var body=new GameObject("Body");body.transform.SetParent(pivot,false);body.transform.localPosition=skin.transform.position;body.transform.localRotation=skin.transform.rotation;body.AddComponent<MeshFilter>().sharedMesh=mesh;body.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
   }
   foreach(var renderer in actor.GetComponentsInChildren<MeshRenderer>()){
    if(!renderer.enabled)continue;var mesh=renderer.GetComponent<MeshFilter>();if(mesh==null)continue;var prop=new GameObject(renderer.name);prop.transform.SetParent(pivot,false);prop.transform.localPosition=renderer.transform.position;prop.transform.localRotation=renderer.transform.rotation;prop.transform.localScale=renderer.transform.lossyScale;prop.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;prop.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
   }
   foreach(var r in actor.GetComponentsInChildren<Renderer>())r.enabled=false;
  }
  utility.AddSingleGO(stage);utility.camera.transform.position=new Vector3(0,1.8f,7);utility.camera.transform.LookAt(new Vector3(0,.9f,0));utility.camera.orthographic=true;utility.camera.orthographicSize=1.22f;utility.camera.nearClipPlane=.01f;utility.camera.farClipPlane=20;utility.camera.clearFlags=CameraClearFlags.SolidColor;utility.camera.backgroundColor=new Color(.055f,.065f,.08f);
  utility.lights[0].intensity=1.8f;utility.lights[0].transform.rotation=Quaternion.Euler(38,-25,0);utility.lights[1].intensity=1.1f;utility.lights[1].transform.rotation=Quaternion.Euler(20,150,0);utility.ambientColor=new Color(.45f,.45f,.45f);
  utility.BeginStaticPreview(new Rect(0,0,1600,700));utility.Render(true);var png=utility.EndStaticPreview();System.IO.File.WriteAllBytes(output+"/"+mode+".png",png.EncodeToPNG());UnityEngine.Object.DestroyImmediate(png);rows.Add(mode);
 }finally{utility.Cleanup();foreach(var actor in actors){var visual=actor.GetComponent<ChibiPlayerVisual>();typeof(ChibiPlayerVisual).GetField("view",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(visual,null);UnityEngine.Object.DestroyImmediate(actor);}foreach(var o in garbage)if(o!=null)UnityEngine.Object.DestroyImmediate(o);}
}
return rows;
