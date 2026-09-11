using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using Newtonsoft.Json;
public static class InspectUIRepair
{
    static string Root=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../ArtWork/HideoutUIRepair"));
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
    public static string Town(){
        if(TitleScreen.Instance!=null)typeof(TitleScreen).GetMethod("Hide",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(TitleScreen.Instance,null);
        SceneTransitionManager.Instance.TransitionTo("Safehouse","default");return "Transition requested";}
    public static string Hideout(){
        if(TitleScreen.Instance!=null)typeof(TitleScreen).GetMethod("Hide",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(TitleScreen.Instance,null);
        Time.timeScale=1;
        SceneTransitionManager.Instance.TransitionTo("Hideout","default");return "Transition requested";}
    public static string Inspect()
    {
        var p=TopDownPlayer.Instance;var s=p==null?null:new SerializedObject(p);
        string scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var result=new{scene,playing=Application.isPlaying,hideout=HideoutController.IsActive,diorama=HideoutDiorama.Active,
            uiOpen=UIManager.Instance!=null&&UIManager.Instance.IsAnyUIOpen(),
            speed=StatDB.Instance.playerStat.moveSpeed,sprint=StatDB.Instance.playerStat.sprintSpeedMultiplier,
            cadence=s?.FindProperty("animCadenceMatchesSpeed").boolValue,
            walkClip=s?.FindProperty("walkClipSpeed").floatValue,runClip=s?.FindProperty("runClipSpeed").floatValue,
            player=p==null?null:new{p.CanMove,pos=p.transform.position.ToString()},
            canvases=UnityEngine.Object.FindObjectsByType<Canvas>().Select(c=>new{path=PathOf(c.transform),c.sortingOrder,c.renderMode,enabled=c.enabled,scale=c.scaleFactor}),
            buttons=UnityEngine.Object.FindObjectsByType<Button>().Select(b=>new{path=PathOf(b.transform),b.interactable,text=string.Join(" ",b.GetComponentsInChildren<Text>().Select(t=>t.text)),rect=b.GetComponent<RectTransform>().rect.ToString()}),
            doors=UnityEngine.Object.FindObjectsByType<SceneDoor3D>().Select(d=>new{d.name,d.TargetScene,d.SpawnPointId,pos=d.transform.position.ToString()}),
            controllers=UnityEngine.Object.FindObjectsByType<HideoutController>(FindObjectsInactive.Include).Select(c=>new{c.name,c.enabled,active=c.gameObject.activeInHierarchy}),
            events=UnityEngine.Object.FindObjectsByType<EventSystem>().Length};
        Directory.CreateDirectory(Root);string json=JsonConvert.SerializeObject(result,Formatting.Indented);
        File.WriteAllText(System.IO.Path.Combine(Root,scene+"-inspect.json"),json);
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Root,scene+"-current.png"));return json;
    }
    public static string ClickExit()=>Click("ExitButton");
    public static string HomeProps()=>JsonConvert.SerializeObject(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t=>t.name.Contains("Home_")||t.name.Contains("Window")||t.name.Contains("Awning")).Select(t=>new{path=PathOf(t),pos=t.position.ToString(),active=t.gameObject.activeSelf}),Formatting.Indented);
    public static string Close(){UIManager.Instance.CloseAll();Time.timeScale=1;return "Closed";}
    public static string ModelState()=>JsonConvert.SerializeObject(UnityEngine.Object.FindObjectsByType<MeshRenderer>().Where(r=>r.transform.parent!=null && r.transform.parent.name=="WorkshopStore62").Select(r=>new{r.name,materials=r.sharedMaterials.Select(m=>m.name),mesh=r.GetComponent<MeshFilter>().sharedMesh.name,vertices=r.GetComponent<MeshFilter>().sharedMesh.vertexCount,scale=r.transform.lossyScale.ToString()}));
    public static string Map(){UIManager.Instance.CloseAll();UnityEngine.Object.FindAnyObjectByType<MapSelectUI>(FindObjectsInactive.Include).Show();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Root,"Map-current.png"));return "Opened";}
    public static string ConfirmExit()=>Click("Yes");
    static string Click(string name){
        var b=UnityEngine.Object.FindObjectsByType<Button>().Single(x=>x.name==name);
        Vector3[] corners=new Vector3[4];b.GetComponent<RectTransform>().GetWorldCorners(corners);
        var data=new PointerEventData(EventSystem.current){position=(corners[0]+corners[2])*.5f};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new Exception("Button blocked: "+name);
        b.onClick.Invoke();return "Raycast and onClick passed: "+name;
    }
    public static string Inventory(){UIManager.Instance.ShowCharacterPanel();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Root,"Inventory-current.png"));return "Opened";}
    public static string Menu(){UIManager.Instance.CloseAll();PauseMenu.Show();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Root,"Menu-current.png"));return "Opened";}
    public static string Settings(){SettingsUI.Show();ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Root,"Settings-current.png"));return "Opened";}
}
