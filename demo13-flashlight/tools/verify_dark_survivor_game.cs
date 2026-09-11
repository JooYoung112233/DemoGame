const string folder="Assets/ChibiSurvivor/Player/DarkSurvivor";
const string output="Library/CodexBlender/DarkSurvivorGame";
var player=TopDownPlayer.Instance;
if (player==null) throw new System.Exception("No runtime player");
var model=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(folder+"/DarkSurvivor.prefab");
var live=player.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
if (live.Length!=40) throw new System.Exception("Expected 39 character meshes and one sword proxy");
if (live.Any(r=>!UnityEditor.AssetDatabase.GetAssetPath(r.sharedMaterial).StartsWith(folder+"/Materials/"))) throw new System.Exception("Runtime replaced an editable material");
var instance=UnityEngine.Object.Instantiate(model);
var cameraObject=new UnityEngine.GameObject("CharacterValidationCamera");
var camera=cameraObject.AddComponent<UnityEngine.Camera>();
var target=new UnityEngine.RenderTexture(800,800,24);
var pixels=new UnityEngine.Texture2D(800,800,UnityEngine.TextureFormat.RGB24,false);
var originalTarget=UnityEngine.RenderTexture.active;
var enabledStates=live.Select(r=>r.enabled).ToArray();
var report=new System.Collections.Generic.List<object>();
try
{
    instance.transform.position=player.transform.position;
    var anim=instance.GetComponentInChildren<UnityEngine.Animator>();anim.enabled=false;
    foreach(var r in live) r.enabled=false;
    camera.CopyFrom(UnityEngine.Camera.main);camera.enabled=false;camera.targetTexture=target;
    camera.orthographic=true;camera.orthographicSize=1.15f;camera.nearClipPlane=.05f;
    var look=instance.transform.position+new UnityEngine.Vector3(0,.86f,0);
    camera.transform.position=look+new UnityEngine.Vector3(2.5f,2.5f,4f);
    camera.transform.LookAt(look);
    // Sword transitions and grip are checked by verify_dark_survivor_sword.cs.
    var clips=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(folder+"/DarkSurvivor.fbx").OfType<UnityEngine.AnimationClip>().Where(c=>c.name=="Idle"||c.name=="Walk"||c.name=="Run").ToArray();
    foreach(var clip in clips)
    {
        var sums=new System.Collections.Generic.List<float>();
        foreach(var fraction in new[]{.0f,.25f,.5f,.75f,1f})
        {
            clip.SampleAnimation(instance,clip.length*fraction);
            float checksum=0;
            var bounds=new UnityEngine.Bounds();bool first=true;
            foreach(var renderer in instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>())
            {
                if (!renderer.enabled) continue;
                var mesh=new UnityEngine.Mesh();renderer.BakeMesh(mesh,false);
                foreach(var v in mesh.vertices)
                {
                    // BakeMesh output here is in scaled renderer space (FBX nodes
                    // carry 100x unit scale); applying lossyScale again doubles it.
                    var w=renderer.transform.position+renderer.transform.rotation*v;
                    if (float.IsNaN(w.x)||float.IsNaN(w.y)||float.IsNaN(w.z)) throw new System.Exception("Nonfinite mesh");
                    checksum+=w.x*.37f+w.y*.23f+w.z*.61f;
                    if(first){bounds=new UnityEngine.Bounds(w,UnityEngine.Vector3.zero);first=false;}else bounds.Encapsulate(w);
                }
                UnityEngine.Object.DestroyImmediate(mesh);
            }
            sums.Add(checksum);
            if (bounds.size.magnitude>4) throw new System.Exception("Exploded pose "+clip.name+" "+bounds+" root "+instance.transform.lossyScale);
            if (bounds.min.y<player.transform.position.y-.04f) throw new System.Exception("Feet below ground "+clip.name);
        }
        if(sums.Max()-sums.Min()<.01f) throw new System.Exception("Static animation "+clip.name);
        anim.enabled=true;anim.Rebind();
        anim.SetFloat("Speed",clip.name=="Run"?2f:clip.name=="Walk"?1f:0f);
        anim.Update(0);anim.Update(clip.length*.25f);
        camera.Render();UnityEngine.RenderTexture.active=target;
        pixels.ReadPixels(new UnityEngine.Rect(0,0,800,800),0,0);pixels.Apply();
        System.IO.File.WriteAllBytes(output+"/Unity_"+clip.name+".png",pixels.EncodeToPNG());
        anim.enabled=false;
        report.Add(new {clip=clip.name,seconds=clip.length,meshChecksums=sums,loopDifference=UnityEngine.Mathf.Abs(sums[0]-sums[sums.Count-1])});
    }
}
finally
{
    for(int i=0;i<live.Length;i++)if(live[i]!=null)live[i].enabled=enabledStates[i];
    UnityEngine.RenderTexture.active=originalTarget;camera.targetTexture=null;
    UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
}
var result=new {liveMeshes=live.Length,editableMaterialsPreserved=true,clips=report};
System.IO.File.WriteAllText(output+"/UnityCheck.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
return result;
