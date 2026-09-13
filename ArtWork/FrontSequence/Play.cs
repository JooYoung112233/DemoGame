using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// MCP-only harness. Movement and E/Space use the game's input layer.
// uGUI buttons invoke their real listener; no OS input or forced scene teleport.
public static class FrontSequencePlay
{
    static string Dir => Path.GetFullPath("../ArtWork/FrontSequence");
    static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent)+"/"+t.name;
    static object Pos(Vector3 p) => new {x=p.x,y=p.y,z=p.z};
    static bool Busy => (StoryPlayer.Instance != null && StoryPlayer.Instance.IsPlaying)
        || (NarrationUI.Instance != null && NarrationUI.Instance.IsShowing)
        || (DialogueUI.Instance != null && DialogueUI.Instance.IsShowing);
    static object Snapshot()
    {
        var p=TopDownPlayer.Instance;
        var interact=p!=null?p.GetComponent<InteractionSystem>():null;
        var target=interact!=null?typeof(InteractionSystem).GetField("currentTarget",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(interact) as InteractableObject:null;
        return new {
            scene=SceneManager.GetActiveScene().name,
            scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).name).ToArray(),
            player=p!=null?Pos(p.transform.position):null,canMove=p!=null&&p.CanMove,
            title=TitleScreen.IsShowing,story=Busy,ui=UIManager.Instance!=null&&UIManager.Instance.IsAnyUIOpen(),
            target=target!=null?target.name:null,hidden=HideoutController.IsActive,
            buttons=UnityEngine.Object.FindObjectsByType<Button>().Where(b=>b.gameObject.activeInHierarchy).Select(b=>new{path=PathOf(b.transform),b.interactable,text=string.Join(" ",b.GetComponentsInChildren<Text>().Select(t=>t.text))}).ToArray(),
            texts=UnityEngine.Object.FindObjectsByType<Text>().Where(t=>t.gameObject.activeInHierarchy&&!string.IsNullOrEmpty(t.text)).Select(t=>new{path=PathOf(t.transform),t.text}).ToArray(),
            interactions=UnityEngine.Object.FindObjectsByType<InteractableObject>().Select(i=>new{i.name,type=i.Type.ToString(),position=Pos(i.transform.position),i.CanInteract,i.InteractRange}).ToArray(),
            doors=UnityEngine.Object.FindObjectsByType<SceneDoor3D>().Select(d=>new{d.name,position=Pos(d.transform.position)}).ToArray(),
            suppressWrites=SaveManager.SuppressWrites
            ,stash=MainStash.Instance!=null?MainStash.Instance.GetGrid().GetAll().Select(x=>new{id=x.item.data.itemId,x.gridX,x.gridY,x.item.stackCount,x.item.ammoCount}).ToArray():null
            ,equipment=p!=null?p.GetComponent<PlayerEquipment>().GetSaveData():null
        };
    }
    public static string Run()
    {
        if(!Application.isPlaying||!SaveManager.SuppressWrites) return "Refusing: play mode with write suppression required.";
        var c=JObject.Parse(File.ReadAllText(Dir+"/Command.json"));
        SceneTransitionManager.Instance.StartCoroutine(Batch(c));
        return "Started "+(string)c["label"];
    }
    static IEnumerator Batch(JObject c)
    {
        if(c["steps"] is JArray steps){foreach(JObject step in steps)yield return Execute(step);}
        else yield return Execute(c);
    }
    static IEnumerator Key(KeyCode k)
    {
        GameInput.VPressKey(k); yield return null;
        GameInput.VEndFrame();
        GameInput.VReleaseKey(k); yield return null;
        GameInput.VEndFrame();
    }
    static IEnumerator Execute(JObject c)
    {
        string label=(string)c["label"],op=(string)c["op"];
        var before=Snapshot(); var notes=new List<string>();
        bool success=true; float began=Time.realtimeSinceStartup;
        GameInput.Virtual=true;
        try
        {
            if(op=="walk")
            {
                foreach(var node in (JArray)c["points"])
                {
                    var goal=new Vector2((float)node[0],(float)node[1]);
                    float end=Time.realtimeSinceStartup+((float?)c["budget"]??18f);
                    string walkScene=SceneManager.GetActiveScene().name;
                    var last=Plan3D.ToPlan(TopDownPlayer.Instance.transform.position); float stuck=0;
                    while(Vector2.Distance(Plan3D.ToPlan(TopDownPlayer.Instance.transform.position),goal)>.22f)
                    {
                        var p=TopDownPlayer.Instance; var pos=Plan3D.ToPlan(p.transform.position);
                        var delta=goal-pos; GameInput.VSetMove(delta.normalized);
                        if(Camera.main!=null)GameInput.VSetMousePos(Camera.main.WorldToScreenPoint(Plan3D.ToWorld(goal)));
                        yield return null;GameInput.VEndFrame();
                        if(SceneManager.GetActiveScene().name!=walkScene){notes.Add("Scene transition reached via walking");break;}
                        var next=Plan3D.ToPlan(p.transform.position);
                        stuck=Vector2.Distance(next,last)<.002f?stuck+Time.unscaledDeltaTime:0;last=next;
                        if(Time.realtimeSinceStartup>end||stuck>2.5f){success=false;notes.Add("WALK_BLOCKED goal="+goal+" actual="+next+" canMove="+p.CanMove);break;}
                    }
                    GameInput.VSetMove(Vector2.zero);yield return new WaitForSecondsRealtime(.25f);
                    if(!success)break;
                }
            }
            else if(op=="key")yield return Key((KeyCode)Enum.Parse(typeof(KeyCode),(string)c["key"]));
            else if(op=="itemmenu")
            {
                var panel=UnityEngine.Object.FindAnyObjectByType<CharacterPanelUI>();
                var root=(RectTransform)typeof(CharacterPanelUI).GetField("containerGridRoot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(panel);
                var placed=MainStash.Instance.GetGrid().GetAll()[(int)c["index"]];
                var point=root.TransformPoint(new Vector3(placed.gridX*74+30,-placed.gridY*74-30,0));
                GameInput.VSetMousePos(RectTransformUtility.WorldToScreenPoint(null,point));
                GameInput.VPressMouse(1);yield return null;GameInput.VEndFrame();GameInput.VReleaseMouse(1);yield return null;GameInput.VEndFrame();
            }
            else if(op=="button")
            {
                var path=(string)c["path"];
                var b=UnityEngine.Object.FindObjectsByType<Button>().SingleOrDefault(x=>PathOf(x.transform)==path);
                if(b==null||!b.IsActive()||!b.IsInteractable()){success=false;notes.Add("Button unavailable: "+path);}
                else b.onClick.Invoke();
            }
            else if(op=="story")
            {
                float end=Time.realtimeSinceStartup+50f;
                while(Busy&&Time.realtimeSinceStartup<end)
                {
                    foreach(var t in UnityEngine.Object.FindObjectsByType<Text>())
                        if(t.gameObject.activeInHierarchy&&t.text.Length>12&&!notes.Contains(t.text))notes.Add(t.text);
                    var key=DialogueUI.Instance!=null&&DialogueUI.Instance.IsShowing?KeyCode.E:KeyCode.Space;
                    yield return Key(key);yield return new WaitForSecondsRealtime(.35f);
                }
                success=!Busy;
            }
            yield return new WaitForSecondsRealtime((float?)c["wait"]??1.2f);
            File.WriteAllText(Dir+"/"+label+".json",JsonConvert.SerializeObject(new{op,success,seconds=Time.realtimeSinceStartup-began,notes,before,after=Snapshot()},Formatting.Indented));
            ScreenCapture.CaptureScreenshot(Dir+"/"+label+".png");
        }
        finally {GameInput.Virtual=false;}
    }
}
