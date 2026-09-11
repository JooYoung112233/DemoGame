const string root="Assets/ChibiSurvivor/Bandit/Firearms", hero="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game";
System.IO.Directory.CreateDirectory(root+"/Animations");UnityEditor.AssetDatabase.Refresh();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var target=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChibiSurvivor/Bandit/SimpleBandit/SimpleBandit.prefab"),scene);
var report=new System.Collections.Generic.List<object>();
try{
 target.GetComponentInChildren<Animator>().enabled=false;
 var tb=target.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
 var map=new System.Collections.Generic.Dictionary<string,string>{{"Spine","B-spine"},{"Chest","B-chest"},{"Neck","B-neck"},{"Head","B-head"}};
 foreach(var side in new[]{"L","R"})foreach(var pair in new[]{("Clavicle","shoulder"),("UpperArm","upperArm"),("Forearm","forearm"),("Hand","hand")})map[pair.Item1+"."+side]="B-"+pair.Item2+"."+side;
 var rest=tb.ToDictionary(p=>p.Key,p=>p.Value.rotation);
 var localRest=tb.ToDictionary(p=>p.Key,p=>(p.Value.localPosition,p.Value.localRotation));
 var upper=tb["Chest"].GetComponentsInChildren<Transform>().Where(t=>!t.GetComponent<Renderer>()&&!t.GetComponent<MeshFilter>()).ToArray();
 var all=map.Keys.Select(n=>tb[n]).Concat(upper).Distinct().ToArray();
 string[] props={"m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z","m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w"};
 void Solve(string side,Vector3 palm,Quaternion handRotation){
  var arm=tb["UpperArm."+side];var fore=tb["Forearm."+side];var hand=tb["Hand."+side];var socket=tb["HandSocket."+side];
  var offset=hand.InverseTransformPoint(socket.position);var wrist=palm-handRotation*Vector3.Scale(offset,hand.lossyScale);
  var shoulder=arm.position;float a=Vector3.Distance(arm.position,fore.position),b=Vector3.Distance(fore.position,hand.position);
  var delta=wrist-shoulder;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,(a+b)*.98f);var dir=delta.normalized;wrist=shoulder+dir*d;
  float along=(a*a-b*b+d*d)/(2*d);var pole=new Vector3(side=="L"?-1:1,-.65f,-.25f);pole-=dir*Vector3.Dot(pole,dir);
  var elbow=shoulder+dir*along+pole.normalized*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
  arm.rotation=Quaternion.FromToRotation(fore.position-arm.position,elbow-shoulder)*arm.rotation;
  fore.rotation=Quaternion.FromToRotation(hand.position-fore.position,wrist-fore.position)*fore.rotation;hand.rotation=handRotation;
 }
 foreach(var kind in new[]{"Pistol","Rifle"}){
  Quaternion weaponLocal=Quaternion.identity;Vector3 gripOffset=new Vector3(0,-.035f,0);
  foreach(var mode in new[]{"Aim","Walk","Shoot"}){
   foreach(var p in localRest){tb[p.Key].localPosition=p.Value.Item1;tb[p.Key].localRotation=p.Value.Item2;}
   string srcName=kind=="Pistol"?"Gun_Aim01":"AssaultRifle_Aim01";if(mode=="Shoot")srcName+="_Shoot01";
   string path=root+"/Source/HumanM@"+srcName+".fbx";
   var source=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);
   try{
    var sb=source.GetComponentsInChildren<Transform>().ToDictionary(t=>t.name);
    var sr=sb.ToDictionary(p=>p.Key,p=>p.Value.rotation);
    var correction=new System.Collections.Generic.Dictionary<string,Quaternion>();
    foreach(var p in map){
     string next=p.Key.StartsWith("UpperArm")?p.Key.Replace("UpperArm","Forearm"):p.Key.StartsWith("Forearm")?p.Key.Replace("Forearm","Hand"):p.Key.StartsWith("Hand.")?p.Key.Replace("Hand.","HandSocket."):null;
     var aligned=rest[p.Key];
     if(next!=null){string sn=next.StartsWith("HandSocket")?"B-handProp."+next.Last():map[next];aligned=Quaternion.FromToRotation(tb[next].position-tb[p.Key].position,sb[sn].position-sb[p.Value].position)*aligned;}
     correction[p.Key]=Quaternion.Inverse(sr[p.Value])*aligned;
    }
    var src=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
    var lower=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(hero+(mode=="Walk"?"/Walk.anim":"/Idle.anim"));
    float length=mode=="Walk"?lower.length:src.length;
    var clip=new AnimationClip{name=kind+"_"+mode,frameRate=30};
    // Bake lower body from accepted locomotion, upper body from the free authored gun clip.
    var tracks=tb.Values.Where(t=>!t.GetComponent<Renderer>()&&!t.GetComponent<MeshFilter>()).ToArray();
    var curves=tracks.ToDictionary(t=>t,t=>Enumerable.Range(0,7).Select(_=>new System.Collections.Generic.List<Keyframe>()).ToArray());
    int count=Mathf.CeilToInt(length*30);float supportError=0;
    for(int i=0;i<=count;i++){
     float time=length*i/count;lower.SampleAnimation(target,mode=="Walk"?time:time%lower.length);src.SampleAnimation(source,mode=="Walk"?time/length*src.length:time);
     foreach(var p in map)tb[p.Key].rotation=sb[p.Value].rotation*correction[p.Key];
     var socket=tb["HandSocket.R"];
     if(mode=="Aim"&&i==0)weaponLocal=Quaternion.Inverse(socket.rotation);
     var gunQ=socket.rotation*weaponLocal;var gunP=socket.position-gunQ*gripOffset;
     if(kind=="Rifle"){
      // The adult source's grip sits too far from the short opposite arm.
      // Keep its hand orientation/recoil, seat the stock on the right shoulder instead of centering the weapon.
      var sourceHand=sb["B-hand.R"];var sourceChest=sb["B-chest"];
      var anchor=tb["Chest"].position+new Vector3(.145f,.10f,.18f)+(sourceHand.position-sourceChest.position-new Vector3(.206f,-.058f,.159f))*.35f;
      Solve("R",anchor,tb["Hand.R"].rotation);
      gunP=socket.position-gunQ*gripOffset;
      var desired=gunP+gunQ*new Vector3(0,-.04f,.155f);
      Solve("L",desired,tb["Hand.L"].rotation);supportError=Mathf.Max(supportError,Vector3.Distance(tb["HandSocket.L"].position,desired));
     }
     foreach(var t in tracks){var p=t.localPosition;var q=t.localRotation;float[] vals={p.x,p.y,p.z,q.x,q.y,q.z,q.w};for(int c=0;c<7;c++)curves[t][c].Add(new Keyframe(time,vals[c]));}
    }
    foreach(var t in tracks)for(int c=0;c<7;c++){
     var curve=new AnimationCurve(curves[t][c].ToArray());for(int k=0;k<curve.length;k++){UnityEditor.AnimationUtility.SetKeyLeftTangentMode(curve,k,UnityEditor.AnimationUtility.TangentMode.Linear);UnityEditor.AnimationUtility.SetKeyRightTangentMode(curve,k,UnityEditor.AnimationUtility.TangentMode.Linear);}
     UnityEditor.AnimationUtility.SetEditorCurve(clip,UnityEditor.EditorCurveBinding.FloatCurve(UnityEditor.AnimationUtility.CalculateTransformPath(t,target.transform),typeof(Transform),props[c]),curve);
    }
    clip.EnsureQuaternionContinuity();var settings=UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=mode!="Shoot";UnityEditor.AnimationUtility.SetAnimationClipSettings(clip,settings);
    string dst=root+"/Animations/"+clip.name+".anim";var saved=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(dst);if(saved==null)UnityEditor.AssetDatabase.CreateAsset(clip,dst);else{UnityEditor.EditorUtility.CopySerialized(clip,saved);UnityEngine.Object.DestroyImmediate(clip);}
    report.Add(new{kind,mode,source=path,length,supportError,weaponLocal=new[]{weaponLocal.x,weaponLocal.y,weaponLocal.z,weaponLocal.w}});
   }finally{UnityEngine.Object.DestroyImmediate(source);}
  }
 }
 UnityEditor.AssetDatabase.SaveAssets();System.IO.Directory.CreateDirectory("D:/Demo/ArtWork/SimpleBandit/Firearms");System.IO.File.WriteAllText("D:/Demo/ArtWork/SimpleBandit/Firearms/Retarget.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
