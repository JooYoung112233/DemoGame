var dio=UnityEngine.Object.FindFirstObjectByType<HideoutDiorama>();
if(dio==null)throw new System.Exception("Hideout is not running");
var anchors=UnityEngine.Object.FindObjectsByType<HideoutFacilityAnchor>().ToDictionary(a=>a.moduleKey);
var results=new System.Collections.Generic.List<object>();
foreach(string key in new[]{"bed","workbench","stash","radio","medical","cooking","generator"}){
 UIManager.Instance?.CloseAll();HideoutDockPanel.Instance?.Hide();
 var button=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="Btn_"+key);
 button.onClick.Invoke();
 var a=anchors[key];float distance=UnityEngine.Vector3.Distance(TopDownPlayer.Instance.transform.position,a.StandPosition);
 results.Add(new{key,type=a.GetComponent<InteractableObject>().Type.ToString(),dockOpened=HideoutDockPanel.Instance!=null&&HideoutDockPanel.Instance.IsOpen,standDistance=distance});
 if(distance>.01f||HideoutDockPanel.Instance==null||!HideoutDockPanel.Instance.IsOpen)throw new System.Exception("Facility selection failed: "+key);
}
UIManager.Instance?.CloseAll();HideoutDockPanel.Instance?.Hide();
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
typeof(HideoutDiorama).GetMethod("PlaceAt",flags).Invoke(dio,new object[]{anchors["idle"],true});
typeof(HideoutDiorama).GetField("_current",flags).SetValue(dio,anchors["idle"]);
typeof(HideoutDiorama).GetMethod("RefreshFacilityBar",flags).Invoke(dio,null);
typeof(HideoutDiorama).GetMethod("FrameRoom",flags).Invoke(dio,null);
var root=UnityEngine.GameObject.Find("Safehouse01_Placed");
var skinned=TopDownPlayer.Instance.GetComponentsInChildren<UnityEngine.SkinnedMeshRenderer>();
var bounds=skinned[0].bounds;foreach(var r in skinned)bounds.Encapsulate(r.bounds);
var report=new{scene="Assets/Scenes/Hideout.unity",rootScale=root.transform.localScale.ToString(),roomMetres="6 x 4",heroHeight=bounds.size.y,cameraRecovered=CameraFollow.Instance!=null,checks=results};
System.IO.File.WriteAllText("Assets/Art/Environments/Safehouse01/ScenePlacementValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
return report;
