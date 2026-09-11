using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;
public static class BakeUIRepair
{
    public static string Run()
    {
        if(Application.isPlaying)throw new Exception("Stop review play before saving UI");
        UIPrefabBaker.BakeCharacterPanelUI();
        UIPrefabBaker.BakeSettingsUI();
        UIPrefabBaker.BakePauseMenu();
        var existing=UnityEngine.Object.FindAnyObjectByType<CharacterPanelUI>();
        bool connected=existing!=null && PrefabUtility.IsPartOfPrefabInstance(existing);
        if(existing!=null){
            // Systems stores a baked UI instance; reconcile its existing serialized view in place.
            existing.ClearGeneratedUI();existing.EditorBake();
            EditorUtility.SetDirty(existing);
            EditorSceneManager.MarkSceneDirty(existing.gameObject.scene);EditorSceneManager.SaveScene(existing.gameObject.scene);
        }
        return JsonConvert.SerializeObject(new{baked=new[]{"CharacterPanelUI","SettingsUI","PauseMenu"},systemsCharacterFound=existing!=null,wasPrefab=connected});
    }
    public static string TownObjects()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Safehouse.unity",OpenSceneMode.Additive);
        try{
            var t=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true));
            return JsonConvert.SerializeObject(t.Where(x=>x.parent!=null && x.parent.name=="HomeYard").Select(x=>new{name=x.name,path=Path(x),pos=x.position.ToString(),prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(x.gameObject)}),Formatting.Indented);
        }finally{EditorSceneManager.CloseScene(scene,true);}
    }
    static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
}
