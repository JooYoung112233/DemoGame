using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public static class FrontSequenceVerify
{
    public static string Run()
    {
        var results=new List<object>();
        void Check(string name,bool ok){results.Add(new{name,ok});}
        var sm=SaveManager.Instance;
        if(!SaveManager.SuppressWrites||sm==null||sm.CurrentSlot!=1||sm.HasSave(1))
            return "Refusing: isolated empty slot 1 required.";
        var dir=Path.GetFullPath("../ArtWork/FrontSequence");
        var before=Directory.GetFiles(Application.persistentDataPath,"save*.json")
            .ToDictionary(p=>Path.GetFileName(p),File.ReadAllBytes);
        sm.Save();sm.WriteToDisk(sm.BuildSaveData());sm.WriteJson(sm.ToJson(sm.BuildSaveData()));
        Check("All save entry points honor suppression",!sm.HasSave(1));
        Check("Existing save bytes unchanged",before.All(kv=>File.ReadAllBytes(Path.Combine(Application.persistentDataPath,kv.Key)).SequenceEqual(kv.Value)));
        Check("No extra saves",Directory.GetFiles(Application.persistentDataPath,"save*.json").Length==before.Count);
        var panel=UnityEngine.Object.FindAnyObjectByType<CharacterPanelUI>();
        Check("Inventory visible after facility button",panel!=null&&panel.IsShowing);
        Check("Facility dock closed before inventory",HideoutDockPanel.Instance!=null&&!HideoutDockPanel.Instance.IsOpen&&!HideoutDockPanel.Instance.HasAdopted);
        Check("Adopted module panel closed",HideoutUI.Instance==null||!HideoutUI.Instance.IsShowing);
        var narration=NarrationUI.Instance.GetComponentsInChildren<Canvas>(true).First();
        var fade=ScreenEffectManager.Instance.GetComponentsInChildren<Canvas>(true).First();
        Check("Narration above story fade",narration.sortingOrder>fade.sortingOrder);
        Check("Prologue chain completed",StoryPlayer.Instance.GetPlayedScenes().Contains("S-000")&&StoryPlayer.Instance.GetPlayedScenes().Contains("S-001"));
        Check("One starter kit only",MainStash.Instance.GetGrid().ItemCount==4);
        Check("Virtual input released",!GameInput.Virtual);
        var json=JsonConvert.SerializeObject(results,Formatting.Indented);
        File.WriteAllText(dir+"/RegressionChecks.json",json);
        return json;
    }
}
