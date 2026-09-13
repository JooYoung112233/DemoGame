using UnityEngine;
using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
public static class LampClearanceCapture
{
    public static string Run(){SceneTransitionManager.Instance.StartCoroutine(Test());return "Capturing lamp during bat swing without saving player progress";}
    static IEnumerator Test()
    {
        var p=TopDownPlayer.Instance;var eq=p.GetComponent<PlayerEquipment>();var equips=eq.GetSaveData();var states=eq.GetWeaponStates();
        bool virt=GameInput.Virtual,suppress=SaveManager.SuppressWrites;GameInput.Virtual=true;GameInput.VSetMove(Vector2.zero);SaveManager.SuppressWrites=true;
        try
        {
            var g=new InventoryGrid(4,1);var bat=new ItemInstance(ItemDatabase.Get("bat"));g.TryAutoPlace(bat);eq.TryEquipWeaponFromGrid(bat,g);
            yield return new WaitForSeconds(.3f);
            typeof(TopDownPlayer).GetMethod("StartLightCombo",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);
            for(int i=0;i<3;i++){yield return new WaitForSeconds(.2f);ScreenCapture.CaptureScreenshot(Path.GetFullPath($"../ArtWork/LampClearance/Swing{i}.png"));}
            yield return new WaitForSeconds(.3f);
            var lamp=p.GetComponentInChildren<WornLamp>();var head=p.GetComponentsInChildren<Transform>().Single(t=>t.name=="Head");
            File.WriteAllText(Path.GetFullPath("../ArtWork/LampClearance/Lamp.json"),JsonConvert.SerializeObject(new{lamp=lamp.transform.position.ToString(),head=head.position.ToString()},Formatting.Indented));
        }
        finally{eq.ResetForNewGame();eq.LoadSaveData(equips);eq.RestoreWeaponStates(states);GameInput.Virtual=virt;SaveManager.SuppressWrites=suppress;}
    }
}
