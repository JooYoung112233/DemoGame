const string folder="Assets/ChibiSurvivor/Player/AxeChopReview";
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try{
 var root=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/DarkSurvivor_AxeChop_Review.prefab"),preview);
 root.GetComponentInChildren<UnityEngine.Animator>().enabled=false;
 var transforms=root.GetComponentsInChildren<UnityEngine.Transform>().ToDictionary(t=>t.name);
 var rest=new System.Collections.Generic.Dictionary<string,UnityEngine.Matrix4x4>();
 foreach(var r in root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())for(int i=0;i<r.bones.Length;i++)if(!rest.ContainsKey(r.bones[i].name))rest[r.bones[i].name]=r.localToWorldMatrix*r.sharedMesh.bindposes[i].inverse;
 var u=transforms["UpperArm.L"];var f=transforms["Forearm.L"];
 UnityEngine.Vector3 shoulder=rest[u.name].GetColumn(3),elbow=rest[f.name].GetColumn(3),wrist=rest["Hand.L"].GetColumn(3);
 var normal=UnityEngine.Vector3.Cross(elbow-shoulder,wrist-elbow).normalized;
 var clip=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(folder+"/DarkSurvivor_AxeChop.fbx").OfType<UnityEngine.AnimationClip>().Single(c=>c.name=="AxeChop01");float maxError=0;
 for(int i=0;i<=180;i++){
  clip.SampleAnimation(root,clip.length*i/180);
  var un=u.rotation*UnityEngine.Quaternion.Inverse(rest[u.name].rotation)*normal;
  var fn=f.rotation*UnityEngine.Quaternion.Inverse(rest[f.name].rotation)*normal;
  maxError=UnityEngine.Mathf.Max(maxError,UnityEngine.Vector3.Angle(un,fn));
 }
 var report=new{samples=181,maxHingeMisalignmentDegrees=maxError,status=maxError<1?"Pass":"Needs attention"};
 System.IO.File.WriteAllText(folder+"/UnityElbowHingeCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
