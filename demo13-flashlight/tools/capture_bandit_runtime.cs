var go=UnityEngine.GameObject.Find("BanditProbe_bandit_ranged");
var p=go.transform.position;
var visual=go.GetComponent<BanditEnemyVisual>();visual.SetVisible(true);visual.SetFacing(UnityEngine.Vector2.down);
foreach(var t in go.GetComponentsInChildren<UnityEngine.Transform>(true))t.gameObject.layer=31;
var hands=go.GetComponentsInChildren<UnityEngine.Transform>().Where(t=>t.name.StartsWith("HandSocket.")).ToArray();
float grip=UnityEngine.Vector3.Distance(hands.Single(t=>t.name=="HandSocket.R").position,hands.Single(t=>t.name=="HandSocket.L").position);
var cameraGo=new UnityEngine.GameObject("BanditCaptureCamera");var camera=cameraGo.AddComponent<UnityEngine.Camera>();
camera.transform.position=p+new UnityEngine.Vector3(2.8f,2.5f,-6);camera.transform.LookAt(p+UnityEngine.Vector3.up*.87f);
camera.orthographic=true;camera.orthographicSize=1.1f;camera.cullingMask=1<<31;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=new UnityEngine.Color(.13f,.14f,.15f);
var lightGo=new UnityEngine.GameObject("BanditCaptureLight");var light=lightGo.AddComponent<UnityEngine.Light>();light.type=UnityEngine.LightType.Directional;light.intensity=1.4f;light.cullingMask=1<<31;lightGo.transform.rotation=UnityEngine.Quaternion.Euler(45,-35,0);
var rt=new UnityEngine.RenderTexture(720,900,24);var tex=new UnityEngine.Texture2D(720,900,UnityEngine.TextureFormat.RGB24,false);var old=UnityEngine.RenderTexture.active;
try{camera.targetTexture=rt;camera.Render();UnityEngine.RenderTexture.active=rt;tex.ReadPixels(new UnityEngine.Rect(0,0,720,900),0,0);tex.Apply();System.IO.File.WriteAllBytes("Assets/ChibiSurvivor/Bandit/Bandit01/RuntimeBat.png",tex.EncodeToPNG());}
finally{UnityEngine.RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraGo);UnityEngine.Object.DestroyImmediate(lightGo);}
return new{gripMetres=grip,expectedGripMetres=.152f,image="Assets/ChibiSurvivor/Bandit/Bandit01/RuntimeBat.png"};
