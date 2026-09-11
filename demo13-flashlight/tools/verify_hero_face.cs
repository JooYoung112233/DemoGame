var results=new System.Collections.Generic.List<object>();
foreach(var path in new[]{"Assets/ChibiSurvivor/Player/DarkSurvivor/DarkSurvivor.prefab","Assets/ChibiSurvivor/Player/DarkSurvivor/SurfaceReview/DarkSurvivor_Textured.prefab","Assets/ChibiSurvivor/Player/AxeChopReview/DarkSurvivor_AxeChop_Review.prefab"}){
 var root=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);if(root==null)throw new System.Exception(path);
 var rs=root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true);var head=rs.Single(r=>r.name=="Hero_Head");var nose=rs.Single(r=>r.name=="Hero_Nose");
 var map=UnityEditor.AssetDatabase.GetAssetPath(head.sharedMaterial.GetTexture("_BaseMap"));
 if(!map.EndsWith("Hero_Face_Neutral.png")||rs.Any(r=>r.name=="Hero_FaceDetails")||head.sharedMesh.subMeshCount!=1)throw new System.Exception("Face not migrated: "+path);
 results.Add(new{path,texture=map,noseVertices=nose.sharedMesh.vertexCount,headHasNormal=head.sharedMaterial.HasProperty("_BumpMap")&&head.sharedMaterial.GetTexture("_BumpMap")!=null});
}
var player=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(r=>r.name=="Player");
if(player!=null){var head=player.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).Single(r=>r.name=="Hero_Head");var map=UnityEditor.AssetDatabase.GetAssetPath(head.sharedMaterial.GetTexture("_BaseMap"));if(!map.EndsWith("Hero_Face_Neutral.png"))throw new System.Exception("Saved LookDev texture incorrect");results.Add(new{sceneFace=map,shader=head.sharedMaterial.shader.name});}
return results;
