const string folder="Assets/ChibiSurvivor/Bandit/Bandit01";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
foreach(var label in new[]{"BaseColor","Normal","Mask"}){
 var t=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(folder+"/Textures/Bandit_"+label+".png");t.textureType=label=="Normal"?UnityEditor.TextureImporterType.NormalMap:UnityEditor.TextureImporterType.Default;t.sRGBTexture=label=="BaseColor";t.convertToNormalmap=false;t.maxTextureSize=2048;t.mipmapEnabled=true;t.wrapMode=UnityEngine.TextureWrapMode.Clamp;t.filterMode=UnityEngine.FilterMode.Trilinear;t.alphaIsTransparency=false;t.alphaSource=UnityEditor.TextureImporterAlphaSource.FromInput;t.textureCompression=UnityEditor.TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
}
var pathMat=folder+"/Materials/Bandit_Surface.mat";var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(pathMat);
if(mat==null){mat=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Lit"));UnityEditor.AssetDatabase.CreateAsset(mat,pathMat);}
mat.SetColor("_BaseColor",UnityEngine.Color.white);mat.SetTexture("_BaseMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/Textures/Bandit_BaseColor.png"));mat.SetTexture("_BumpMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/Textures/Bandit_Normal.png"));mat.SetFloat("_BumpScale",.6f);mat.EnableKeyword("_NORMALMAP");mat.SetTexture("_MetallicGlossMap",UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(folder+"/Textures/Bandit_Mask.png"));mat.SetFloat("_Smoothness",1);mat.SetFloat("_Metallic",1);mat.EnableKeyword("_METALLICSPECGLOSSMAP");UnityEditor.EditorUtility.SetDirty(mat);
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var report=new System.Collections.Generic.List<object>();
try{
 foreach(bool withBat in new[]{false,true}){
  string fbx=folder+(withBat?"/Bandit01_WithBat.fbx":"/Bandit01.fbx");var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(fbx);importer.isReadable=true;importer.importNormals=UnityEditor.ModelImporterNormals.Import;importer.importTangents=UnityEditor.ModelImporterTangents.CalculateMikk;importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),"Bandit_Surface"),mat);importer.SaveAndReimport();
  var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(fbx);var go=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(model,preview);
  try{
   go.name="Bandit01";var animator=go.GetComponentInChildren<UnityEngine.Animator>();animator.runtimeAnimatorController=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.RuntimeAnimatorController>(folder+(withBat?"/BanditBat.controller":"/Bandit01.controller"));animator.applyRootMotion=false;
   if(animator.avatar==null||!animator.avatar.isValid)throw new System.Exception("Invalid bandit avatar");
   var meshes=go.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true);if(meshes.Length!=(withBat?2:1))throw new System.Exception("Unexpected mesh count");
   foreach(var r in meshes){r.updateWhenOffscreen=true;if(r.sharedMesh==null||r.bones.Any(b=>b==null)||r.sharedMaterials.Any(m=>m==null))throw new System.Exception("Invalid renderer "+r.name);}
   var body=meshes.Single(r=>r.name=="Bandit_Body");if(body.sharedMaterials.Length!=1||body.sharedMesh.subMeshCount!=1||body.sharedMaterials[0]!=mat)throw new System.Exception("Body is not one material");
   var clips=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<UnityEngine.AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
   foreach(var name in new[]{"Idle","Walk","Run","SwordWalk","SwordSlash"})if(!clips.Any(c=>c.name==name))throw new System.Exception("Missing "+name);
   foreach(var clip in clips)foreach(var fraction in new[]{0f,.25f,.5f,.75f,1f}){
    clip.SampleAnimation(go,clip.length*fraction);foreach(var r in meshes){var mesh=new UnityEngine.Mesh();r.BakeMesh(mesh,false);if(mesh.vertices.Any(v=>float.IsNaN(v.x)||float.IsInfinity(v.x)||v.magnitude>5))throw new System.Exception("Invalid pose "+clip.name);UnityEngine.Object.DestroyImmediate(mesh);}
   }
   clips.Single(c=>c.name=="Idle").SampleAnimation(go,0);
   UnityEditor.PrefabUtility.SaveAsPrefabAsset(go,folder+(withBat?"/Bandit01_Bat.prefab":"/Bandit01.prefab"));report.Add(new{withBat,renderers=meshes.Length,bodyMaterials=1,clips=clips.Select(c=>c.name).ToArray(),avatarValid=true});
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 UnityEditor.AssetDatabase.SaveAssets();
 var resource=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Resources/Characters/Bandit01.prefab");
 var resourceMeshes=resource.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true);if(resourceMeshes.Length!=2||!resourceMeshes.Any(r=>r.name=="Bandit_Body"))throw new System.Exception("Runtime resource did not inherit unified model");
 var result=new{assets=report,runtimePrefab="Assets/Resources/Characters/Bandit01.prefab",runtimeRenderers=resourceMeshes.Length};System.IO.File.WriteAllText(folder+"/UnifiedUnityCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}
