const string archive="D:/Demo/Archive/CharacterAndAnimation_20260910_182541/Assets/Kevin Iglesias/Human Animations";
const string output="Assets/ChibiSurvivor/Bandit/Firearms/Source";
System.IO.Directory.CreateDirectory(output);
var names=new[]{"Gun/HumanM@Gun_Aim01","Gun/HumanM@Gun_Aim02","Gun/HumanM@Gun_Aim01_Shoot01","Gun/HumanM@Gun_Aim02_Shoot01","AssaultRifle/HumanM@AssaultRifle_Aim01","AssaultRifle/HumanM@AssaultRifle_Aim01_Shoot01"};
foreach(var name in names)System.IO.File.Copy(archive+"/Animations/Male/Combat/"+name+".fbx",output+"/"+System.IO.Path.GetFileName(name)+".fbx",true);
foreach(var name in new[]{"Human_Gun","Human_AssaultRifle"})System.IO.File.Copy(archive+"/Unity Demo Scenes/Human Soldier Animations/Models/"+name+".fbx",output+"/"+name+".fbx",true);
UnityEditor.AssetDatabase.Refresh();
var rows=new System.Collections.Generic.List<object>();
foreach(var path in System.IO.Directory.GetFiles(output,"*.fbx")){
 var importer=(UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(path);importer.animationType=UnityEditor.ModelImporterAnimationType.Generic;importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.animationCompression=UnityEditor.ModelImporterAnimationCompression.Off;importer.SaveAndReimport();
 var p=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);var go=UnityEngine.Object.Instantiate(p);
 try{
  var clip=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
  if(clip!=null)clip.SampleAnimation(go,0);
  rows.Add(new{path,clip=clip==null?null:clip.name,length=clip==null?0:clip.length,bones=go.GetComponentsInChildren<Transform>().Where(t=>new[]{"B-hand.R","B-hand.L","B-handProp.R","B-chest","B-upperArm.R","B-upperArm.L","B-hips"}.Contains(t.name)).Select(t=>new{t.name,p=t.position.ToString("F3"),up=t.up.ToString("F3"),forward=t.forward.ToString("F3")}).ToArray(),meshes=go.GetComponentsInChildren<Renderer>().Select(r=>new{r.name,bounds=r.bounds.ToString()}).ToArray()});
 }finally{UnityEngine.Object.DestroyImmediate(go);}
}
return rows;
