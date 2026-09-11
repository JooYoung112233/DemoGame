if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play mode must be stopped.");
var project=System.IO.Path.GetFullPath(".");var scan=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Library/character-archive-dependencies.json"));
if(scan["externalDependencies"].Any())throw new System.Exception("Unresolved legacy dependencies");
var candidates=scan["folders"].Select(t=>(string)t).Where(System.IO.Directory.Exists).ToList();
const string study="Assets/ChibiSurvivor/Player/SimpleHeroStudy";
foreach(var p in System.IO.Directory.GetFiles(study))
{
 var name=System.IO.Path.GetFileName(p);if(name.EndsWith(".meta"))continue;
 if(name.StartsWith("Stage")||name.StartsWith("RunStability")||name.StartsWith("UVSurface")||name.StartsWith("Study_Face")||System.Text.RegularExpressions.Regex.IsMatch(name,@"^SimpleHero_Stage[1-4]"))candidates.Add(p.Replace('\\','/'));
}
foreach(var p in System.IO.Directory.GetFiles(study+"/BlenderSource~"))if(System.IO.Path.GetFileName(p)!="SimpleHero_Stage5_Slash.blend")candidates.Add(p.Replace('\\','/'));
foreach(var p in new[]{"ArtSource/MotionReferences","ArtSource/CharacterArchive"})if(System.IO.Directory.Exists(p))candidates.Add(p);
var assetCandidates=candidates.Where(p=>p.StartsWith("Assets/")).ToArray();
bool Moving(string p)=>assetCandidates.Any(c=>p==c||p.StartsWith(c+"/"));
var roots=UnityEditor.AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!Moving(p)&&new[]{".unity",".prefab",".asset",".controller",".overrideController",".mat"}.Contains(System.IO.Path.GetExtension(p))).ToArray();
var needed=UnityEditor.AssetDatabase.GetDependencies(roots,true).Where(Moving).ToArray();if(needed.Length>0)throw new System.Exception("Still needed: "+string.Join(",",needed));
var archive=System.IO.Path.GetFullPath(System.IO.Path.Combine(project,"../Archive/CharacterAndAnimation_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss")));
var archiveParent=System.IO.Path.GetFullPath(System.IO.Path.Combine(project,"../Archive"))+System.IO.Path.DirectorySeparatorChar;
if(!archive.StartsWith(archiveParent,System.StringComparison.OrdinalIgnoreCase))throw new System.Exception("Archive escaped approved folder");
var records=new System.Collections.Generic.List<object>();var planned=new System.Collections.Generic.List<string>();
foreach(var p in candidates){planned.Add(p);if(System.IO.File.Exists(p+".meta"))planned.Add(p+".meta");}
var progress=new System.Collections.Generic.List<object>();System.IO.Directory.CreateDirectory(archive);
void Manifest(){var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{project,archive,planned,moved=progress},Newtonsoft.Json.Formatting.Indented);System.IO.File.WriteAllText(System.IO.Path.Combine(archive,"manifest.json"),json);System.IO.File.WriteAllText("Library/character-archive-manifest.json",json);}
Manifest();UnityEditor.AssetDatabase.DisallowAutoRefresh();UnityEditor.AssetDatabase.StartAssetEditing();
try{
 foreach(var relative in planned){
  var source=System.IO.Path.GetFullPath(relative);var target=System.IO.Path.GetFullPath(System.IO.Path.Combine(archive,relative));
  if(!source.StartsWith(project+System.IO.Path.DirectorySeparatorChar,System.StringComparison.OrdinalIgnoreCase)||!target.StartsWith(archive+System.IO.Path.DirectorySeparatorChar,System.StringComparison.OrdinalIgnoreCase))throw new System.Exception("Unsafe archive path");
  if(System.IO.File.Exists(target)||System.IO.Directory.Exists(target))throw new System.Exception("Archive target exists "+target);
  System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target));
  if(System.IO.Directory.Exists(source))System.IO.Directory.Move(source,target);else if(System.IO.File.Exists(source))System.IO.File.Move(source,target);else continue;
  progress.Add(new{from=relative,to=target});Manifest();
 }
}finally{UnityEditor.AssetDatabase.StopAssetEditing();UnityEditor.AssetDatabase.AllowAutoRefresh();}
UnityEditor.AssetDatabase.Refresh();return new{archive,moved=progress.Count,currentCharacter=study+"/Game/SimpleHero.prefab"};
