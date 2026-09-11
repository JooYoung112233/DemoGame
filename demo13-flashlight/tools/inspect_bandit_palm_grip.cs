var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChibiSurvivor/Bandit/SimpleBandit/SimpleBandit.prefab"),scene);
var baked=new Mesh();
try{
 var anim=go.GetComponentInChildren<Animator>();anim.Rebind();anim.Update(0);
 var body=go.GetComponentsInChildren<SkinnedMeshRenderer>().Single();var club=go.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="BanditClub");var weights=body.sharedMesh.boneWeights;
 var rows=new System.Collections.Generic.List<object>();
 foreach(var phase in new[]{0f,.25f,.5f,.75f}){
  anim.Play("SwordWalk",0,phase);anim.Update(0);body.BakeMesh(baked);var v=baked.vertices;var matrix=Matrix4x4.TRS(body.transform.position,body.transform.rotation,Vector3.one);
  foreach(var side in new[]{"L","R"}){
   int idx=System.Array.FindIndex(body.bones,b=>b.name=="Hand."+side);var ids=Enumerable.Range(0,weights.Length).Where(i=>weights[i].boneIndex0==idx&&weights[i].weight0>.99f).ToArray();
   var points=ids.Select(i=>club.transform.InverseTransformPoint(matrix.MultiplyPoint3x4(v[i]))).ToArray();var center=points.Aggregate(Vector3.zero,(a,b)=>a+b)/points.Length;
   var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points)bounds.Encapsulate(p);
   rows.Add(new{phase,side,count=ids.Length,palmCenterInClub=center.ToString("F5"),bounds=bounds.ToString("F5"),socket=club.transform.InverseTransformPoint(go.GetComponentsInChildren<Transform>().Single(t=>t.name=="HandSocket."+side).position).ToString("F5")});
  }
 }
 return new{clubBounds=club.GetComponent<MeshFilter>().sharedMesh.bounds.ToString("F5"),rows};
}finally{UnityEngine.Object.DestroyImmediate(baked);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
