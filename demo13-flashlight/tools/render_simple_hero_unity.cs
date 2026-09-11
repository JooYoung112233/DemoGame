const string folder="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game";
var utility=new UnityEditor.PreviewRenderUtility();
try{
 var instance=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/SimpleHero.prefab"));utility.AddSingleGO(instance);
 var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(folder+"/Idle.anim");clip.SampleAnimation(instance,0);
 utility.camera.transform.position=new Vector3(2.6f,1.9f,4f);utility.camera.transform.LookAt(new Vector3(0,.9f,0));utility.camera.orthographic=true;utility.camera.orthographicSize=1.12f;utility.camera.nearClipPlane=.01f;utility.camera.farClipPlane=20;utility.camera.clearFlags=CameraClearFlags.SolidColor;utility.camera.backgroundColor=new Color(.07f,.075f,.075f);
 utility.lights[0].intensity=1.2f;utility.lights[0].transform.rotation=Quaternion.Euler(35,-30,0);utility.lights[1].intensity=.55f;utility.lights[1].transform.rotation=Quaternion.Euler(20,150,0);utility.ambientColor=new Color(.32f,.32f,.32f);
 utility.BeginStaticPreview(new Rect(0,0,700,850));utility.Render(true);var texture=utility.EndStaticPreview();
 var path=folder+"/Unity_PlayerPreview.png";System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);return path;
}finally{utility.Cleanup();}
