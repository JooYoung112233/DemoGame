using System;
using System.IO;
using UnityEngine;
public static class RenderReplacementReview
{
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/HideoutUIRepair"));
    static void Capture(string name,Vector3 target,float size)
    {
        var cam=Camera.main;var pos=cam.transform.position;var rot=cam.transform.rotation;float oldSize=cam.orthographicSize;
        var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;
        var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        try{
            var q=Quaternion.Euler(62,0,0);cam.transform.SetPositionAndRotation(target-q*Vector3.forward*35,q);cam.orthographicSize=size;
            cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Root,name+".png"),tex.EncodeToPNG());
        }finally{cam.targetTexture=oldTarget;RenderTexture.active=oldActive;cam.transform.SetPositionAndRotation(pos,rot);cam.orthographicSize=oldSize;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    public static string Run(){
        Capture("Replacements-All62",new Vector3(61,1,15),11);
        Capture("Home-Review62",new Vector3(64,1,12),6.8f);
        Capture("Timber-Review62",new Vector3(52,1,20),3.5f);
        Capture("Workshop-Review62",new Vector3(68,1,21),3.8f);return "4 review images saved; camera restored";
    }
}
