const string folder="Assets/ChibiSurvivor/Bandit/Bandit01";
var owned=new System.Collections.Generic.List<UnityEngine.Material>(); var sceneBefore=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool playing=UnityEditor.EditorApplication.isPlaying;
var preview=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();var previousRT=UnityEngine.RenderTexture.active;UnityEngine.RenderTexture rt=null;UnityEngine.Texture2D pixels=null;
try{
 var instance=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ChibiSurvivor/Bandit/Bandit01/Bandit01.prefab"),preview);
 var animator=instance.GetComponentInChildren<UnityEngine.Animator>();animator.enabled=false;
 var clip=UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ChibiSurvivor/Bandit/Bandit01/Bandit01.fbx").OfType<UnityEngine.AnimationClip>().Single(c=>c.name=="Idle");clip.SampleAnimation(instance,0); ToonMaterial.ApplyTo(instance,"BanditRender",owned);
 var cg=new UnityEngine.GameObject("SurfaceReviewCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cg,preview);var camera=cg.AddComponent<UnityEngine.Camera>();camera.enabled=false;camera.scene=preview;camera.orthographic=true;camera.orthographicSize=1.13f;
 camera.transform.position=new UnityEngine.Vector3(3.4f,2.6f,6);camera.transform.LookAt(new UnityEngine.Vector3(0,.95f,0));camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.09f,.105f,.11f);camera.nearClipPlane=.05f;camera.farClipPlane=50;
 var data=cg.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.SetRenderer(0);data.renderPostProcessing=false;
 var lights=new System.Collections.Generic.List<UnityEngine.Light>();
 foreach(var row in new[]{new[]{-3f,3f,5f,1.2f},new[]{4f,2f,3f,7f},new[]{0f,3f,-4f,10f}}){var go=new UnityEngine.GameObject("SurfaceReviewLight");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,preview);go.transform.position=new UnityEngine.Vector3(row[0],row[1],row[2]);go.transform.LookAt(UnityEngine.Vector3.up);var light=go.AddComponent<UnityEngine.Light>();light.type=lights.Count==0?UnityEngine.LightType.Directional:UnityEngine.LightType.Point;light.range=20;light.intensity=row[3];light.color=new UnityEngine.Color(1,.95f,.88f);light.shadows=lights.Count==0?UnityEngine.LightShadows.Soft:UnityEngine.LightShadows.None;lights.Add(light);}
 rt=new UnityEngine.RenderTexture(900,1000,24);rt.Create();camera.targetTexture=rt;pixels=new UnityEngine.Texture2D(900,1000,UnityEngine.TextureFormat.RGB24,false);
 foreach(var mode in new[]{"Neutral","Dark"}){
  if(mode=="Dark"){lights[0].intensity=.7f;lights[0].color=new UnityEngine.Color(.66f,.76f,1);lights[1].intensity=8;lights[2].intensity=22;lights[2].color=new UnityEngine.Color(1,.61f,.33f);}
  camera.Render();UnityEngine.RenderTexture.active=rt;pixels.ReadPixels(new UnityEngine.Rect(0,0,900,1000),0,0);pixels.Apply();System.IO.File.WriteAllBytes(folder+"/Unified_Unity_"+mode+".png",pixels.EncodeToPNG());
 }
 return new{images=new[]{folder+"/Unified_Unity_Neutral.png",folder+"/Unified_Unity_Dark.png"},playStatePreserved=playing==UnityEditor.EditorApplication.isPlaying,activeScenePreserved=sceneBefore==UnityEngine.SceneManagement.SceneManager.GetActiveScene()};
}finally{foreach(var m in owned)if(m!=null)UnityEngine.Object.DestroyImmediate(m);UnityEngine.RenderTexture.active=previousRT;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);}

