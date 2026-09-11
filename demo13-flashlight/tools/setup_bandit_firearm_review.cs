const string root="Assets/ChibiSurvivor/Bandit/Firearms",hero="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game";
foreach(var kind in new[]{"Pistol","Rifle"}){
 string path=root+"/Prefabs/Bandit_"+kind+".prefab";var go=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try{
  var review=go.GetComponent<BanditFirearmPreview>()??go.AddComponent<BanditFirearmPreview>();review.kind=kind=="Pistol"?BanditFirearmPreview.FirearmKind.Pistol:BanditFirearmPreview.FirearmKind.AssaultRifle;
  review.idle=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(hero+"/Idle.anim");review.walk=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(hero+"/Walk.anim");
  review.aim=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(root+"/Animations/"+kind+"_Aim.anim");review.armedWalk=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(root+"/Animations/"+kind+"_Walk.anim");review.shoot=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(root+"/Animations/"+kind+"_Shoot.anim");
  UnityEditor.PrefabUtility.SaveAsPrefabAsset(go,path);
 }finally{UnityEditor.PrefabUtility.UnloadPrefabContents(go);}
}
var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
try{
 UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
 foreach(var kind in new[]{"Pistol","Rifle"}){
  var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/Bandit_"+kind+".prefab"),scene);go.transform.position=new Vector3(kind=="Pistol"?-1:1,0,0);go.transform.rotation=Quaternion.Euler(0,30,0);
 }
 var cam=new GameObject("Review Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(0,1.7f,6);cam.transform.LookAt(new Vector3(0,.95f,0));cam.orthographic=true;cam.orthographicSize=1.4f;cam.backgroundColor=new Color(.07f,.08f,.1f);cam.clearFlags=CameraClearFlags.SolidColor;
 var sun=new GameObject("Key Light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.7f;sun.transform.rotation=Quaternion.Euler(45,-25,0);
 var fill=new GameObject("Fill Light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.8f;fill.transform.rotation=Quaternion.Euler(25,140,0);
 var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Review floor";floor.transform.position=new Vector3(0,-.055f,0);floor.transform.localScale=new Vector3(12,.1f,12);floor.GetComponent<Renderer>().sharedMaterial=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(root+"/Gunmetal.mat");
 UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,root+"/BanditFirearmsReview.unity");
 return new{scene=root+"/BanditFirearmsReview.unity",duration=14};
}finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);}
