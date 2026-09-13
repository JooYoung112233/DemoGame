using UnityEngine;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;
public static class ArmoryInspect
{
    public static string Run()
    {
        var items = new[] { "pistol9", "bat", "mag_9x19", "ammo_9x19" };
        var data = AssetDatabase.FindAssets("t:ItemData").Select(g => AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(g))).Where(d => items.Contains(d.itemId));
        var prefabs = AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath).Where(p => p.Contains("CharacterPanel") || p.Contains("Player") || p.Contains("Firearms"));
        return JsonConvert.SerializeObject(new {
            items = data.Select(d => new { d.itemId, slot=d.equipSlot.ToString(), weapon=d.weaponData != null ? d.weaponData.name : null, bat=d.weaponData != null && d.weaponData.useTwoHandBatAnimations, icon=d.icon != null ? d.icon.name : null }),
            prefabs,
            scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path)
        }, Formatting.Indented);
    }
}
