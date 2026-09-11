const string root="Assets/ChibiSurvivor/Bandit/Firearms";
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var rows=new System.Collections.Generic.List<object>();
try{foreach(var kind in new[]{"Pistol","Rifle"}){
 var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Prefabs/Bandit_"+kind+".prefab"),scene);
 UnityEditor.PrefabUtility.UnpackPrefabInstance(go,UnityEditor.PrefabUnpackMode.Completely,UnityEditor.InteractionMode.AutomatedAction);var actor=go.GetComponent<BanditFirearmPreview>();var bones=go.GetComponentsInChildren<Transform>();var weapon=bones.Single(t=>t.name=="Firearm");var right=bones.Single(t=>t.name=="HandSocket.R");var left=bones.Single(t=>t.name=="HandSocket.L");
 float maxStep=0,maxStepTime=0,maxRight=0,maxLeft=0;Vector3 previous=Vector3.zero;int bad=0;
 for(int i=0;i<=840;i++){
  actor.SampleTimeline(i/60f);if(i>0&&Vector3.Distance(previous,weapon.position)>maxStep){maxStep=Vector3.Distance(previous,weapon.position);maxStepTime=i/60f;}previous=weapon.position;
  foreach(var t in bones)if(float.IsNaN(t.position.x)||float.IsInfinity(t.position.x))bad++;
  if(actor.IsAiming){maxRight=Mathf.Max(maxRight,Vector3.Distance(right.position,weapon.TransformPoint(new Vector3(0,-.035f,0))));if(kind=="Rifle")maxLeft=Mathf.Max(maxLeft,Vector3.Distance(left.position,weapon.TransformPoint(new Vector3(0,-.04f,.18f))));}
 }
 actor.Sample(0,0,false);string stowedParent=weapon.parent.name;float scale=weapon.lossyScale.x;
 actor.Sample(0,1,true);string armedParent=weapon.parent.name;
 if(maxRight>.005f||maxLeft>.005f||bad!=0||maxStep>.08f||stowedParent!="FirearmStow"||armedParent!="FirearmGrip"||Mathf.Abs(scale-1)>.001f)throw new System.Exception("Firearm pose validation failed: "+kind);
 var placement=new Vector3(3,0,4);var rotation=Quaternion.Euler(0,65,0);go.transform.SetPositionAndRotation(placement,rotation);actor.Sample(1,1,true);
 if(Vector3.Distance(go.transform.position,placement)>.001f||Quaternion.Angle(go.transform.rotation,rotation)>.1f)throw new System.Exception("Sampling moved actor");
 typeof(BanditFirearmPreview).GetMethod("FirePreview",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(actor,null);
 if(actor.PreviewShots!=1||go.GetComponentInChildren<LineRenderer>()==null)throw new System.Exception("Preview firing effect missing");
 rows.Add(new{kind,maxWeaponFrameStep=maxStep,maxStepTime,maxRightGripError=maxRight,maxLeftGripError=maxLeft,invalidTransforms=bad,stowedParent,armedParent,weaponWorldScale=scale});
 UnityEngine.Object.DestroyImmediate(go);
}
System.IO.File.WriteAllText("D:/Demo/ArtWork/SimpleBandit/Firearms/Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return rows;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
