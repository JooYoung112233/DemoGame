const string scenePath="Assets/Scenes/MapTool_LookDev.unity";
const string folder="Assets/ChibiSurvivor/Player/DarkSurvivor";
var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool playing=UnityEditor.EditorApplication.isPlaying;
if(!playing&&active.path==scenePath&&active.isDirty)throw new System.Exception("LookDev has unsaved edits; preserve them before disk refresh.");
if(playing||active.path!=scenePath)throw new System.Exception("Open LookDev in edit mode before applying");
var scene=active;
try{
 var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/DarkSurvivor.prefab").GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true).ToDictionary(r=>r.name);
 var dir=folder+"/SurfaceReview/LookDevMaterials";
 if(!UnityEditor.AssetDatabase.IsValidFolder(dir))UnityEditor.AssetDatabase.CreateFolder(folder+"/SurfaceReview","LookDevMaterials");
 int count=0;
 foreach(var root in scene.GetRootGameObjects())foreach(var r in root.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>(true)){
  if(root.name!="Player"||!source.TryGetValue(r.name,out var src))continue;
  if(!r.bones.Select(b=>b.name).SequenceEqual(src.bones.Select(b=>b.name)))throw new System.Exception("Bone mismatch "+r.name);
  r.sharedMesh=src.sharedMesh;
  var previous=r.sharedMaterials;var mats=new UnityEngine.Material[src.sharedMaterials.Length];
  for(int i=0;i<mats.Length;i++){
   var original=src.sharedMaterials[i];var old=i<previous.Length?previous[i]:null;
   if(old==null||old.shader.name!="BRB/Toon"){mats[i]=original;continue;}
   string path=dir+"/LookDev_"+original.name+".mat";
   var m=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
   if(m==null){m=new UnityEngine.Material(old);UnityEditor.AssetDatabase.CreateAsset(m,path);}
   m.SetTexture("_BaseMap",original.GetTexture("_BaseMap"));m.SetColor("_BaseColor",UnityEngine.Color.white);m.SetTextureScale("_BaseMap",UnityEngine.Vector2.one);m.SetTextureOffset("_BaseMap",UnityEngine.Vector2.zero);UnityEditor.EditorUtility.SetDirty(m);mats[i]=m;
  }
  r.sharedMaterials=mats;count++;
 }
 if(count!=40)throw new System.Exception("Unexpected hero renderer count "+count);
 UnityEditor.AssetDatabase.SaveAssets();
 if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath,true))throw new System.Exception("Could not save updated LookDev scene");
 return new{saved=scenePath,renderers=count,playingPreserved=playing==UnityEditor.EditorApplication.isPlaying,activeScenePreserved=active==UnityEngine.SceneManagement.SceneManager.GetActiveScene()};
}finally{UnityEditor.SceneView.RepaintAll();}
