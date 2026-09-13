using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
public static class BatComfortSample
{
    public static string Run()
    {
        const string game="Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game/";
        var root=new GameObject("BatGripPreview");root.hideFlags=HideFlags.HideAndDontSave;root.transform.position=new Vector3(1000,1000,1000);
        var clone=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(game+"SimpleHero.prefab"),root.transform,false);
        var a=clone.GetComponentInChildren<Animator>();a.enabled=false;
        var ts=clone.GetComponentsInChildren<Transform>(true);
        var bat=ts.Single(t=>t.name=="Hero_Bat");var mesh=bat.GetComponent<MeshFilter>().sharedMesh;
        var report=new List<object>();
        var baked=new List<Mesh>();
        var prev=RenderTexture.active;RenderTexture rt=null;Texture2D tex=null;
        try
        {
            foreach(var t in ts)t.gameObject.layer=31;
            foreach(var r in clone.GetComponentsInChildren<Renderer>())if(r.name.Contains("Sword"))r.enabled=false;
            var skins=clone.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();
            var frozen=skins.Select(s=>{var g=new GameObject(s.name+"_Sample",typeof(MeshFilter),typeof(MeshRenderer));g.layer=31;g.transform.SetParent(s.transform,false);g.GetComponent<MeshRenderer>().sharedMaterials=s.sharedMaterials;var m=new Mesh();baked.Add(m);g.GetComponent<MeshFilter>().sharedMesh=m;return m;}).ToArray();
            bat.GetComponent<Renderer>().enabled=true;
            var camGO=new GameObject("Camera",typeof(Camera));camGO.transform.SetParent(root.transform,false);var cam=camGO.GetComponent<Camera>();
            cam.orthographic=true;cam.orthographicSize=1.4f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.14f,.16f);cam.cullingMask=1<<31;cam.nearClipPlane=.1f;cam.farClipPlane=10;
            var lightGO=new GameObject("Light",typeof(Light));lightGO.transform.SetParent(root.transform,false);lightGO.transform.rotation=Quaternion.Euler(35,-35,0);var light=lightGO.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.cullingMask=1<<31;
            rt=new RenderTexture(700,700,24);tex=new Texture2D(700,700,TextureFormat.RGB24,false);cam.targetTexture=rt;
            var rings=mesh.vertices.GroupBy(v=>Mathf.Round(v.y*1000)).OrderBy(g=>g.Key).Select(g=>new{y=g.Key/1000f,x=g.Average(v=>v.x),z=g.Average(v=>v.z),count=g.Count()}).ToArray();
            foreach(var spec in new[]{("BatWalk",0f),("BatWalk",.6f),("BatSwing",.6f),("BatSwing",1.1f)})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(game+spec.Item1+".anim");clip.SampleAnimation(clone,spec.Item2);
                for(int i=0;i<skins.Length;i++){skins[i].BakeMesh(frozen[i],true);skins[i].enabled=false;}
                report.Add(new{clip=spec.Item1,time=spec.Item2,right=bat.InverseTransformPoint(ts.Single(t=>t.name=="HandSocket.R").position).ToString("F5"),left=bat.InverseTransformPoint(ts.Single(t=>t.name=="HandSocket.L").position).ToString("F5"),meshRings=rings});
                foreach(float pitch in new[]{62f,20f})
                {
                    var center=root.transform.position+Vector3.up*.9f;
                    cam.transform.rotation=Quaternion.Euler(pitch,180,0);cam.transform.position=center-cam.transform.forward*3;
                    cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,700,700),0,0);tex.Apply();
                    File.WriteAllBytes(Path.GetFullPath($"../ArtWork/BatComfort/{spec.Item1}-{spec.Item2:F1}-{pitch}.png"),tex.EncodeToPNG());
                }
            }
            var json=JsonConvert.SerializeObject(report,Formatting.Indented);File.WriteAllText(Path.GetFullPath("../ArtWork/BatComfort/GripSamples.json"),json);return json;
        }
        finally{RenderTexture.active=prev;foreach(var c in root.GetComponentsInChildren<Camera>())c.targetTexture=null;if(tex!=null)Object.DestroyImmediate(tex);if(rt!=null){rt.Release();Object.DestroyImmediate(rt);}Object.DestroyImmediate(root);foreach(var m in baked)Object.DestroyImmediate(m);}
    }
}
