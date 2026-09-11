if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop play mode first");
var project=System.IO.Path.GetFullPath(".");
var manifest=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Library/character-archive-manifest.json"));
var archive=System.IO.Path.GetFullPath((string)manifest["archive"]);
var approved=System.IO.Path.GetFullPath("../Archive")+System.IO.Path.DirectorySeparatorChar;
if(!archive.StartsWith(approved,System.StringComparison.OrdinalIgnoreCase))throw new System.Exception("Invalid archive path");
var candidates=System.IO.Directory.GetFiles("Assets/ChibiSurvivor/Player").Select(p=>p.Replace('\\','/')).Where(p=>!p.EndsWith(".meta")).ToList();
foreach(var p in new[]{"ArtSource/ChibiSurvivor","Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game/UnityCheck.json","Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game/Unity_PlayerPreview.png"})if(System.IO.File.Exists(p)||System.IO.Directory.Exists(p))candidates.Add(p);
bool Moving(string p)=>candidates.Any(c=>p==c||p.StartsWith(c+"/"));
var roots=UnityEditor.AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!Moving(p)&&!UnityEditor.AssetDatabase.IsValidFolder(p)).ToArray();
var needed=UnityEditor.AssetDatabase.GetDependencies(roots,true).Where(Moving).Distinct().ToArray();
if(needed.Length>0)throw new System.Exception("Still referenced: "+string.Join(",",needed));
var moves=candidates.SelectMany(p=>System.IO.File.Exists(p+".meta")?new[]{p,p+".meta"}:new[]{p}).ToArray();
foreach(var relative in moves){
 var source=System.IO.Path.GetFullPath(relative);var target=System.IO.Path.GetFullPath(System.IO.Path.Combine(archive,relative));
 if(!source.StartsWith(project+System.IO.Path.DirectorySeparatorChar,System.StringComparison.OrdinalIgnoreCase)||!target.StartsWith(archive+System.IO.Path.DirectorySeparatorChar,System.StringComparison.OrdinalIgnoreCase))throw new System.Exception("Unsafe path");
 if(System.IO.File.Exists(target)||System.IO.Directory.Exists(target))throw new System.Exception("Target exists "+target);
}
void Save(){var json=manifest.ToString();System.IO.File.WriteAllText(System.IO.Path.Combine(archive,"manifest.json"),json);System.IO.File.WriteAllText("Library/character-archive-manifest.json",json);}
UnityEditor.AssetDatabase.DisallowAutoRefresh();UnityEditor.AssetDatabase.StartAssetEditing();
try{foreach(var relative in moves){
 var source=System.IO.Path.GetFullPath(relative);var target=System.IO.Path.GetFullPath(System.IO.Path.Combine(archive,relative));
 System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target));
 if(System.IO.Directory.Exists(source))System.IO.Directory.Move(source,target);else System.IO.File.Move(source,target);
 ((Newtonsoft.Json.Linq.JArray)manifest["planned"]).Add(relative);
 ((Newtonsoft.Json.Linq.JArray)manifest["moved"]).Add(Newtonsoft.Json.Linq.JObject.FromObject(new{from=relative,to=target}));Save();
}}finally{UnityEditor.AssetDatabase.StopAssetEditing();UnityEditor.AssetDatabase.AllowAutoRefresh();}
UnityEditor.AssetDatabase.Refresh();return new{archive,additionalMoves=moves.Length,remainingPlayerFolders=System.IO.Directory.GetDirectories("Assets/ChibiSurvivor/Player")};
