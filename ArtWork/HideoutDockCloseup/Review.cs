using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using Newtonsoft.Json;
public static class DockCloseupReview
{
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/HideoutDockCloseup"));
    public static string Save(){
        var tune=GameTuning.Instance;tune.hideoutViewZoom=2f;tune.hideoutDockDimAlpha=.32f;
        EditorUtility.SetDirty(tune);AssetDatabase.SaveAssetIfDirty(tune);return AssetDatabase.GetAssetPath(tune);
    }
    static void Select(string module){
        var a=UnityEngine.Object.FindObjectsByType<HideoutFacilityAnchor>().Single(x=>x.moduleKey==module);
        var d=UnityEngine.Object.FindAnyObjectByType<HideoutDiorama>();
        typeof(HideoutDiorama).GetMethod("Select",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(d,new object[]{a});
    }
    static bool Hit(Button b){
        var corners=new Vector3[4];b.GetComponent<RectTransform>().GetWorldCorners(corners);
        var data=new PointerEventData(EventSystem.current){position=(corners[0]+corners[2])*.5f};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        return hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==b;
    }
    public static string Controls(){
        var bar=UnityEngine.Object.FindObjectsByType<Button>().Where(b=>b.name.StartsWith("Btn_") && b.transform.parent.name=="HideoutFacilityBar").ToArray();
        var close=UnityEngine.Object.FindObjectsByType<Button>().Single(b=>b.name=="Close" && b.transform.parent.name=="Panel" && b.transform.parent.parent.name=="HideoutDockPanel");
        var result=new{facilityButtons=bar.Length,facilityHits=bar.All(Hit),closeHit=Hit(close)};
        if(result.facilityButtons!=7||!result.facilityHits||!result.closeHit)throw new Exception("Blocked control: "+JsonConvert.SerializeObject(result));
        File.WriteAllText(Path.Combine(Root,"Controls.json"),JsonConvert.SerializeObject(result));
        close.onClick.Invoke();return JsonConvert.SerializeObject(result);
    }
    public static string Review(){GameTuning.Instance.hideoutViewZoom=2;GameTuning.Instance.hideoutDockDimAlpha=.32f;UnityEngine.Object.FindAnyObjectByType<HideoutDiorama>().StartCoroutine(Capture());return "Review started";}
    static IEnumerator Capture(){
        var rows=new System.Collections.Generic.List<object>();
        UIManager.Instance.CloseAll();yield return new WaitForSecondsRealtime(1.2f);
        rows.Add(new{module="initial",ortho=Camera.main.orthographicSize,dimVisible=GameObject.Find("RightDockDim")!=null});
        ScreenCapture.CaptureScreenshot(Path.Combine(Root,"initial-after.png"));yield return null;
        foreach(var module in new[]{"workbench","bed","cooking","medical","radio","generator","stash"}){
            UIManager.Instance.CloseAll();Select(module);yield return new WaitForSecondsRealtime(1.2f);
            var cam=Camera.main;var a=UnityEngine.Object.FindObjectsByType<HideoutFacilityAnchor>().Single(x=>x.moduleKey==module);
            var dim=GameObject.Find("RightDockDim").GetComponent<Image>();
            Vector3 pos=cam.WorldToViewportPoint(a.StandPosition+Vector3.up*.65f);
            rows.Add(new{module,ortho=cam.orthographicSize,characterViewport=pos.ToString(),dimAlpha=dim.color.a,dimRaycast=dim.raycastTarget,uiOpen=HideoutDockPanel.Instance.IsOpen});
            ScreenCapture.CaptureScreenshot(Path.Combine(Root,module+"-after.png"));yield return null;
        }
        UIManager.Instance.CloseAll();yield return new WaitForSecondsRealtime(1.2f);
        rows.Add(new{module="closed",ortho=Camera.main.orthographicSize,dimVisible=GameObject.Find("RightDockDim")!=null});
        ScreenCapture.CaptureScreenshot(Path.Combine(Root,"closed-after.png"));
        yield return null;
        File.WriteAllText(Path.Combine(Root,"Validation.json"),JsonConvert.SerializeObject(rows,Formatting.Indented));
        Select("workbench");
    }
    public static string Inspect(){return JsonConvert.SerializeObject(new{playing=Application.isPlaying,dock=HideoutDockPanel.Instance.IsOpen,buttons=UnityEngine.Object.FindObjectsByType<Button>().Select(b=>b.name+" parent="+b.transform.parent.name).ToArray()});}
    public static string Baseline(){GameTuning.Instance.hideoutViewZoom=1;GameTuning.Instance.hideoutDockDimAlpha=0;Select("workbench");return "Baseline selected";}
    public static string CaptureBaseline(){ScreenCapture.CaptureScreenshot(Path.Combine(Root,"workbench-before.png"));float before=Camera.main.orthographicSize;File.WriteAllText(Path.Combine(Root,"Before.json"),JsonConvert.SerializeObject(new{ortho=before}));return before.ToString();}
}
