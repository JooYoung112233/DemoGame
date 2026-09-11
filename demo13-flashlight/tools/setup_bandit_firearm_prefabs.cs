const string root="Assets/ChibiSurvivor/Bandit/Firearms";
System.IO.Directory.CreateDirectory(root+"/Prefabs");UnityEditor.AssetDatabase.Refresh();
var mat=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(root+"/Gunmetal.mat");
if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",new Color(.20f,.23f,.25f));mat.SetFloat("_Metallic",.5f);mat.SetFloat("_Smoothness",.3f);UnityEditor.AssetDatabase.CreateAsset(mat,root+"/Gunmetal.mat");}
var cloth=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(root+"/Holster.mat");if(cloth==null){cloth=new Material(Shader.Find("Universal Render Pipeline/Lit"));cloth.SetColor("_BaseColor",new Color(.13f,.11f,.075f));UnityEditor.AssetDatabase.CreateAsset(cloth,root+"/Holster.mat");}
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
try{foreach(var kind in new[]{"Pistol","Rifle"}){
 var go=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChibiSurvivor/Bandit/SimpleBandit/SimpleBandit.prefab"),scene);
 UnityEditor.PrefabUtility.UnpackPrefabInstance(go,UnityEditor.PrefabUnpackMode.Completely,UnityEditor.InteractionMode.AutomatedAction);go.name="Bandit_"+kind;
 var club=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="BanditClub");UnityEngine.Object.DestroyImmediate(club.gameObject);
 var animator=go.GetComponentInChildren<Animator>();animator.enabled=false;animator.runtimeAnimatorController=null;
 var bones=go.GetComponentsInChildren<Transform>();Transform Bone(string n)=>bones.Single(t=>t.name==n);
 UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(root+"/Animations/"+kind+"_Aim.anim").SampleAnimation(go,0);
 var socket=Bone("HandSocket.R");var q=Quaternion.Inverse(socket.rotation);
 var grip=new GameObject("FirearmGrip").transform;grip.SetParent(socket,false);grip.localScale=Vector3.one/socket.lossyScale.x;grip.localRotation=q;grip.position=socket.position;
 var gun=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(root+"/Source/Human_"+(kind=="Pistol"?"Gun":"AssaultRifle")+".fbx"),scene);gun.name="Firearm";gun.transform.SetParent(grip,false);gun.transform.localPosition=new Vector3(0,.035f,0);gun.transform.localRotation=Quaternion.identity;
 foreach(var r in gun.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
 var muzzle=new GameObject("Muzzle").transform;muzzle.SetParent(gun.transform,false);muzzle.localPosition=new Vector3(0,.07f,kind=="Pistol"?.235f:.54f);
 // Author stow anchors in the normal standing pose, then let their bones carry them.
 UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game/Idle.anim").SampleAnimation(go,0);
 var stow=new GameObject("FirearmStow").transform;stow.SetParent(kind=="Pistol"?Bone("Thigh.R"):Bone("Chest"),false);
 stow.localScale=Vector3.one/stow.parent.lossyScale.x;stow.position=kind=="Pistol"?new Vector3(.29f,.58f,0):new Vector3(.02f,.99f,-.19f);
 stow.rotation=kind=="Pistol"?Quaternion.Euler(90,0,0):Quaternion.LookRotation(new Vector3(-.65f,-.75f,0),Vector3.back);
 var holster=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.Object.DestroyImmediate(holster.GetComponent<Collider>());holster.name=kind=="Pistol"?"ThighHolster":"BackMount";holster.transform.SetParent(stow,false);holster.transform.localPosition=new Vector3(0,.02f,.10f);holster.transform.localScale=kind=="Pistol"?new Vector3(.09f,.12f,.19f):new Vector3(.11f,.055f,.22f);holster.GetComponent<Renderer>().sharedMaterial=cloth;
 gun.transform.SetParent(stow,false);gun.transform.localPosition=new Vector3(0,.035f,0);gun.transform.localRotation=Quaternion.identity;
 UnityEditor.PrefabUtility.SaveAsPrefabAsset(go,root+"/Prefabs/Bandit_"+kind+".prefab");UnityEngine.Object.DestroyImmediate(go);
}UnityEditor.AssetDatabase.SaveAssets();return "Firearm prefabs created";}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
