public static class WorldComicApply {
 static string Out=>System.IO.Path.GetFullPath("../ArtWork/WorldComic62/Unity");
 static string MatRoot=>WorldComicImport.Root+"/Materials";
 static readonly System.Collections.Generic.List<object> Report=new();
 public static string Run(){
 if(UnityEngine.Application.isPlaying)throw new System.Exception("Stop play before saving");
 var paths=UnityEditor.AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art","Assets/ChibiSurvivor","Assets/Resources"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).Distinct().ToArray();
 int changed=0,remaps=0;
 foreach(var path in paths){
 if(path.StartsWith(WorldComicImport.Root+"/"))continue;
 if(!path.EndsWith(".mat"))continue;
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);if(m==null)continue;
 var family=WorldComicImport.Family(m);
 if(!WorldComicImport.Eligible(family)){Report.Add(new{path,name=m.name,status=family});continue;}
 var previous=UnityEditor.AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap"));WorldComicImport.Style(m,family);
 UnityEditor.EditorUtility.SetDirty(m);UnityEditor.AssetDatabase.SaveAssetIfDirty(m);changed++;
 Report.Add(new{path,name=m.name,status="textured",family,previous});
 }
 foreach(var path in paths.Where(p=>p.EndsWith(".fbx",System.StringComparison.OrdinalIgnoreCase))){
 var importer=UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.ModelImporter;if(importer==null)continue;
 bool dirty=false;var existing=importer.GetExternalObjectMap();
 foreach(var m in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<UnityEngine.Material>()){
 var family=WorldComicImport.Family(m);if(!WorldComicImport.Eligible(family))continue;
 var id=new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),m.name);
 if(existing.TryGetValue(id,out var mapped)&&mapped is UnityEngine.Material mm&&UnityEditor.AssetDatabase.GetAssetPath(mm).EndsWith(".mat"))continue;
 string target=MatRoot+"/"+UnityEditor.AssetDatabase.AssetPathToGUID(path).Substring(0,8)+"_"+m.name+".mat";
 var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(target);
 if(mat==null){mat=new UnityEngine.Material(m);mat.name=m.name;WorldComicImport.Style(mat,family);UnityEditor.AssetDatabase.CreateAsset(mat,target);}
 importer.AddRemap(id,mat);dirty=true;remaps++;Report.Add(new{path,name=m.name,status="external-remap",family,target});
 }
 if(dirty)importer.SaveAndReimport();
 }
 System.IO.File.WriteAllText(Out+"/MaterialApply.json",Newtonsoft.Json.JsonConvert.SerializeObject(Report,Newtonsoft.Json.Formatting.Indented));
 return $"Saved {changed} materials, remapped {remaps} FBX material slots";
 }
 public static string Scenes(){
 if(UnityEngine.Application.isPlaying)throw new System.Exception("Stop play before saving");
 int changed=0;
 foreach(var path in new[]{"Assets/Scenes/Safehouse.unity","Assets/Scenes/Hideout.unity","Assets/Scenes/Pawnshop.unity"}){
 var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
 if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
 if(scene.isDirty)throw new System.Exception("Scene has existing unsaved changes: "+path);
 int edits=0;
 foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Renderer>(true))){
 if(r is UnityEngine.SpriteRenderer||r is UnityEngine.ParticleSystemRenderer)continue;
 var mats=r.sharedMaterials;bool dirty=false;
 for(int i=0;i<mats.Length;i++){
 var m=mats[i];if(m==null||!m.HasProperty("_BaseMap"))continue;string mp=UnityEditor.AssetDatabase.GetAssetPath(m);
 if(mp.EndsWith(".mat"))continue;
 if(mp.EndsWith(".fbx",System.StringComparison.OrdinalIgnoreCase)){
 var imp=UnityEditor.AssetImporter.GetAtPath(mp) as UnityEditor.ModelImporter;
 if(imp!=null&&imp.GetExternalObjectMap().TryGetValue(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(UnityEngine.Material),m.name),out var replacement)&&replacement is UnityEngine.Material rm){mats[i]=rm;dirty=true;}
 continue;
 }
 if(!string.IsNullOrEmpty(mp))continue;
 var family=WorldComicImport.Family(m);if(!WorldComicImport.Eligible(family))continue;
 // Inline greybox surfaces have no useful material names; use the actual mesh role.
 string rn=r.name.ToLowerInvariant();if(rn.Contains("floor")||rn.Contains("wall"))family="Plaster";else if(rn.Contains("crate")||rn.Contains("shelf"))family="Wood";
 var color=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):UnityEngine.Color.white;
 string signature=UnityEditor.EditorJsonUtility.ToJson(m);string hash=UnityEngine.Hash128.Compute(signature).ToString().Substring(0,12);
 string target=MatRoot+"/Scene_"+family+"_"+hash+".mat";
 var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(target);if(mat==null){mat=new UnityEngine.Material(m){name="Scene_"+family};WorldComicImport.Style(mat,family);UnityEditor.AssetDatabase.CreateAsset(mat,target);}
 mats[i]=mat;dirty=true;
 }
 if(dirty){r.sharedMaterials=mats;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(r);UnityEditor.EditorUtility.SetDirty(r);edits++;}
 }
 if(edits>0){UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);changed+=edits;}
 if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
 }
 return $"Saved scene material bindings on {changed} renderers";
 }
}
