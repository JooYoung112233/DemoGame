var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var owned=new System.Collections.Generic.List<UnityEngine.Material>();
var root=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Resources/Characters/Bandit01.prefab"),scene);
try{
 var animator=root.GetComponentInChildren<UnityEngine.Animator>();animator.cullingMode=UnityEngine.AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
 ToonMaterial.ApplyTo(root,"BanditUnifiedProbe",owned);
 var body=root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().Single(r=>r.name=="Bandit_Body");
 if(body.sharedMaterials.Length!=1||body.sharedMaterial.GetTexture("_BaseMap")==null)throw new System.Exception("Runtime toon loses atlas");
 var reports=new System.Collections.Generic.List<object>();
 foreach(var state in new[]{"Locomotion","SwordWalk","SwordSlash"}){
  int id=UnityEngine.Animator.StringToHash("Base Layer."+state);if(!animator.HasState(0,id))throw new System.Exception("Missing state "+state);
  animator.Play(id,0,0);animator.SetFloat("Speed",state=="Locomotion"?2:0);animator.Update(.05f);
  var infos=animator.GetCurrentAnimatorClipInfo(0);if(infos.Length==0)throw new System.Exception("Missing controller motion "+state);
  var mesh=new UnityEngine.Mesh();body.BakeMesh(mesh,false);if(mesh.vertices.Any(v=>float.IsNaN(v.x)||v.magnitude>5))throw new System.Exception("Invalid animated mesh "+state);UnityEngine.Object.DestroyImmediate(mesh);
  reports.Add(new{state,clips=infos.Select(c=>c.clip.name).ToArray()});
 }
 var result=new{runtimeBodyRenderers=1,totalRenderers=root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>().Length,bodyMaterialSlots=1,toonAtlasPreserved=true,states=reports};System.IO.File.WriteAllText("Assets/ChibiSurvivor/Bandit/Bandit01/UnifiedControllerCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
}finally{UnityEngine.Object.DestroyImmediate(root);foreach(var m in owned)if(m!=null)UnityEngine.Object.DestroyImmediate(m);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
