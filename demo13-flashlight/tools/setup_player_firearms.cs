const string resource="Assets/Resources/Characters/Firearms",source="Assets/ChibiSurvivor/Bandit/Firearms";
System.IO.Directory.CreateDirectory(resource);UnityEditor.AssetDatabase.Refresh();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var rows=new System.Collections.Generic.List<object>();
try{foreach(var kind in new[]{"Pistol","Rifle"}){
 var go=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(source+"/Prefabs/Bandit_"+kind+".prefab"));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
 try{
  var actor=go.GetComponent<BanditFirearmPreview>();actor.Initialize();var bones=go.GetComponentsInChildren<Transform>();var stow=bones.Single(t=>t.name=="FirearmStow");var grip=bones.Single(t=>t.name=="FirearmGrip");var gun=bones.Single(t=>t.name=="Firearm");var holder=stow.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="ThighHolster"||r.name=="BackMount").transform;
  var tracks=bones.Where(t=>!t.IsChildOf(stow)&&!t.IsChildOf(grip)&&t!=stow&&t!=grip&&!t.GetComponent<Renderer>()).ToArray();
  var curves=tracks.ToDictionary(t=>t,t=>Enumerable.Range(0,7).Select(_=>new System.Collections.Generic.List<Keyframe>()).ToArray());
  const float duration=1.2f;const int count=72;
  for(int i=0;i<=count;i++){
   float u=(float)i/count;actor.Sample(0,u,false);foreach(var t in tracks){var p=t.localPosition;var q=t.localRotation;float[] v={p.x,p.y,p.z,q.x,q.y,q.z,q.w};for(int c=0;c<7;c++)curves[t][c].Add(new Keyframe(u*duration,v[c]));}
  }
  var clip=new AnimationClip{name=kind+"_Draw",frameRate=60};string[] props={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
  foreach(var t in tracks)for(int c=0;c<7;c++){
   var curve=new AnimationCurve(curves[t][c].ToArray());for(int k=0;k<curve.length;k++){UnityEditor.AnimationUtility.SetKeyLeftTangentMode(curve,k,UnityEditor.AnimationUtility.TangentMode.Linear);UnityEditor.AnimationUtility.SetKeyRightTangentMode(curve,k,UnityEditor.AnimationUtility.TangentMode.Linear);}
   UnityEditor.AnimationUtility.SetEditorCurve(clip,UnityEditor.EditorCurveBinding.FloatCurve(UnityEditor.AnimationUtility.CalculateTransformPath(t,go.transform),typeof(Transform),props[c]),curve);
  }
  clip.EnsureQuaternionContinuity();string clipPath=resource+"/"+clip.name+".anim";var saved=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);if(saved==null){UnityEditor.AssetDatabase.CreateAsset(clip,clipPath);saved=clip;}else{UnityEditor.EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);}
  string dataPath=resource+"/"+kind+".asset";var set=UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerFirearmSet>(dataPath);if(set==null){set=ScriptableObject.CreateInstance<PlayerFirearmSet>();UnityEditor.AssetDatabase.CreateAsset(set,dataPath);}
  set.aim=actor.aim;set.walk=actor.armedWalk;set.shoot=actor.shoot;set.draw=saved;set.stowBone=stow.parent.name;set.stowPosition=stow.localPosition;set.stowRotation=stow.localRotation;set.gripRotation=grip.localRotation;
  var prop=UnityEngine.Object.Instantiate(gun.gameObject);try{prop.name=kind;prop.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);prop.transform.localScale=Vector3.one;set.weaponPrefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(prop,resource+"/"+kind+"Weapon.prefab");}finally{UnityEngine.Object.DestroyImmediate(prop);}
  var holster=new GameObject(kind+"Holster");try{holster.transform.localPosition=holder.localPosition;holster.transform.localRotation=holder.localRotation;holster.transform.localScale=holder.localScale;holster.AddComponent<MeshFilter>().sharedMesh=holder.GetComponent<MeshFilter>().sharedMesh;holster.AddComponent<MeshRenderer>().sharedMaterials=holder.GetComponent<MeshRenderer>().sharedMaterials;set.holsterPrefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(holster,resource+"/"+kind+"Holster.prefab");}finally{UnityEngine.Object.DestroyImmediate(holster);}
  UnityEditor.EditorUtility.SetDirty(set);rows.Add(new{kind,draw=clipPath,resource=dataPath,stowBone=set.stowBone});
 }finally{UnityEngine.Object.DestroyImmediate(go);}
}UnityEditor.AssetDatabase.SaveAssets();return rows;}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
