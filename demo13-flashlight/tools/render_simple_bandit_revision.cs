const string folder="Assets/ChibiSurvivor/Bandit/SimpleBandit";
var utility=new UnityEditor.PreviewRenderUtility();
try{
 var instance=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/SimpleBandit.prefab"));
 var animator=instance.GetComponentInChildren<Animator>();animator.Rebind();utility.AddSingleGO(instance);
 utility.camera.orthographic=true;utility.camera.orthographicSize=1.08f;utility.camera.nearClipPlane=.01f;utility.camera.farClipPlane=20;
 utility.camera.clearFlags=CameraClearFlags.SolidColor;utility.camera.backgroundColor=new Color(.065f,.065f,.065f);
 utility.lights[0].intensity=1.2f;utility.lights[0].transform.rotation=Quaternion.Euler(35,-30,0);
 utility.lights[1].intensity=.7f;utility.lights[1].transform.rotation=Quaternion.Euler(20,150,0);utility.ambientColor=new Color(.32f,.32f,.32f);
 var paths=new System.Collections.Generic.List<string>();
 foreach(bool armed in new[]{false,true}){
  animator.SetFloat("Speed",0);animator.SetLayerWeight(animator.GetLayerIndex("SwordHold"),0);
  animator.Play(armed?"Base Layer.SwordWalk":"Base Layer.Locomotion",0,0);animator.Update(.001f);
  instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name=="Bandit_Bat").enabled=armed;
  utility.camera.transform.position=new Vector3(-2.6f,1.9f,5f);utility.camera.transform.LookAt(new Vector3(0,.9f,0));
  utility.BeginStaticPreview(new Rect(0,0,800,900));utility.Render(true);var image=utility.EndStaticPreview();
  var path="D:/Demo/ArtWork/SimpleBandit/"+(armed?"UnityPreview":"UnityNeutral")+".png";
  System.IO.File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);paths.Add(path);
 }
 return paths;
}finally{utility.Cleanup();}
