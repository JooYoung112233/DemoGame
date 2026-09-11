if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Runtime required");
var rows=new System.Collections.Generic.List<object>();
foreach(var n in UnityEngine.Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None)){
 var a=n.GetComponentInChildren<Animator>();if(a==null)continue;
 var clip=a.GetCurrentAnimatorClipInfo(0).Single().clip;var state=a.GetCurrentAnimatorStateInfo(0);
 var inter=n.GetComponent<InteractableObject>();var skin=n.GetComponentInChildren<SkinnedMeshRenderer>();
 if(!state.loop||state.normalizedTime<1||!a.enabled||!skin.enabled||inter.Type!=InteractableObject.InteractType.NPC)throw new System.Exception("NPC runtime validation failed: "+n.Data.npcId);
 rows.Add(new{id=n.Data.npcId,clip=clip.name,seconds=clip.length,loopsElapsed=state.normalizedTime,loop=state.loop,bones=skin.bones.Length,shader=skin.sharedMaterial.shader.name,interaction=inter.Type.ToString(),range=inter.InteractRange,prompt=inter.PromptText,canInteract=inter.CanInteract});
}
if(rows.Count!=4)throw new System.Exception("Expected four animated NPCs");
foreach(var id in new[]{"pawnshop","veteran_scavenger","district_warden","wandering_merchant"})System.IO.File.Copy("Assets/Library/StoryNPC62Review/"+id+".png","D:/Demo/ArtWork/StoryNPC62/UnityIntegration/"+id+".png",true);
var result=new{date="2026-09-11",cameraPitch=62,cameraYaw=0,npcs=rows,scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name).ToArray(),roofs=UnityEngine.Object.FindObjectsByType<BuildingInterior>(FindObjectsSortMode.None).Select(b=>new{name=b.name,inside=b.PlayerInside,roofVisible=b.transform.Find("Roof_Art02").GetComponentInChildren<Renderer>().enabled}).ToArray()};
System.IO.File.WriteAllText("D:/Demo/ArtWork/StoryNPC62/UnityIntegration/RuntimeVerified.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return Newtonsoft.Json.JsonConvert.SerializeObject(result);
