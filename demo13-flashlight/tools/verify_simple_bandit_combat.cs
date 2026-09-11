const string folder="Assets/ChibiSurvivor/Bandit/SimpleBandit";var instance=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/SimpleBandit.prefab"));instance.hideFlags=HideFlags.HideAndDontSave;instance.transform.position=new Vector3(0,-1000,0);
try{
 var animator=instance.GetComponentInChildren<Animator>();animator.Rebind();animator.Update(0);
 var body=instance.GetComponentsInChildren<SkinnedMeshRenderer>().Single();var club=instance.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="BanditClub");
 var socket=club.transform.parent;if(socket.name!="HandSocket.R")throw new System.Exception("Club attachment");
 var bones=instance.GetComponentsInChildren<Transform>();var left=bones.Single(t=>t.name=="HandSocket.L");var right=bones.Single(t=>t.name=="HandSocket.R");
 if(body.sharedMaterials.Length!=1||body.sharedMesh.uv.Length!=body.sharedMesh.vertexCount)throw new System.Exception("Body atlas");
 var mesh=club.GetComponent<MeshFilter>().sharedMesh;if(mesh.uv.Length!=mesh.vertexCount||Mathf.Abs(mesh.bounds.size.y-.833f)>.005f)throw new System.Exception("Club dimensions/UV");
 float maxGrip=0,maxAttachment=0;var records=new System.Collections.Generic.List<object>();var baked=new Mesh();
 try{
 foreach(var test in new[]{("Idle",0f),("Walk",1f),("Run",2f),("Attack",0f)}){
  animator.SetFloat("Speed",test.Item2);animator.Play(test.Item1=="Attack"?"Base Layer.BatAttack":"Base Layer.Locomotion",0,0);animator.Update(0);
  Vector3[] first=null;float displacement=0;
  for(int i=0;i<180;i++){
   animator.Update(1f/120);maxGrip=Mathf.Max(maxGrip,Vector3.Distance(left.position,right.position-right.up*.15f));
   maxAttachment=Mathf.Max(maxAttachment,club.transform.localPosition.magnitude+Quaternion.Angle(club.transform.localRotation,Quaternion.identity));
   body.BakeMesh(baked);var vertices=baked.vertices;if(vertices.Any(v=>!float.IsFinite(v.x)||!float.IsFinite(v.y)||!float.IsFinite(v.z)))throw new System.Exception("Nonfinite skin");
   if(first==null)first=vertices;else for(int j=0;j<vertices.Length;j++)displacement=Mathf.Max(displacement,(vertices[j]-first[j]).magnitude);
  }
  if(test.Item1!="Idle"&&displacement<.04f)throw new System.Exception("Frozen motion: "+test.Item1);
  records.Add(new{motion=test.Item1,maxMeshDisplacement=displacement});
 }
 animator.SetFloat("Speed",0);animator.Play("Base Layer.Locomotion",0,0);animator.Update(.05f);animator.SetTrigger("Attack");int entries=0;bool wasAttack=false,returned=false;
 for(int i=0;i<300;i++){animator.Update(.01f);bool active=animator.GetCurrentAnimatorStateInfo(0).IsName("BatAttack");if(active&&!wasAttack)entries++;if(wasAttack&&!active)returned=true;wasAttack=active;}
 if(entries!=1||!returned)throw new System.Exception("Single attack / recovery transition");
 if(maxGrip>.01f||maxAttachment>.001f)throw new System.Exception("Grip or attachment drift");
 var result=new{passed=true,oneBodyMesh=true,rigidWeapon=true,maxGripError=maxGrip,maxAttachmentError=maxAttachment,singleAttackEntries=entries,returnedToReady=returned,motions=records,gameplayApplied=false};
 System.IO.File.WriteAllText("D:/Demo/ArtWork/SimpleBandit/CombatReview/UnityCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
 }finally{UnityEngine.Object.DestroyImmediate(baked);}
}finally{UnityEngine.Object.DestroyImmediate(instance);}


