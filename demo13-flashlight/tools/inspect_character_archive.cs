var oldFolders=System.IO.Directory.GetDirectories("Assets/ChibiSurvivor/Player").Select(p=>p.Replace('\\','/')).Where(p=>!p.EndsWith("/SimpleHeroStudy")).Concat(new[]{"Assets/ChibiSurvivor/ReferenceSurvivor","Assets/ExplosiveLLC","Assets/Kevin Iglesias","Assets/ChibiSurvivor/Player/SimpleHeroStudy/MeleeReview"}).ToArray();
bool Old(string p)=>oldFolders.Any(f=>p==f||p.StartsWith(f+"/"));
var paths=UnityEditor.AssetDatabase.GetAllAssetPaths();
var roots=paths.Where(p=>p.StartsWith("Assets/")&&!Old(p)&&new[]{".unity",".prefab",".asset",".controller",".overrideController",".mat"}.Contains(System.IO.Path.GetExtension(p))).ToArray();
var needed=UnityEditor.AssetDatabase.GetDependencies(roots,true).Where(Old).Distinct().ToArray();
var users=roots.Select(p=>new{path=p,dependencies=UnityEditor.AssetDatabase.GetDependencies(p,false).Where(Old).ToArray()}).Where(x=>x.dependencies.Length>0).ToArray();
var report=new{folders=oldFolders,externalDependencies=needed,directUsers=users,resources=paths.Where(p=>Old(p)&&p.Contains("/Resources/")).ToArray()};
System.IO.File.WriteAllText("Library/character-archive-dependencies.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
