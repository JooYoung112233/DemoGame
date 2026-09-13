using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
public static class ArmoryApply
{
    public static string Run()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Edit mode required");
        GameTuning.Instance.interactionDistance = 1.8f;
        EditorUtility.SetDirty(GameTuning.Instance);
        const string path = "Assets/Resources/UI/CharacterPanelUI.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var panel = root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="PanelRoot");
            var old = panel.Find("ControlHint");
            var hint = old != null ? old.GetComponent<UnityEngine.UI.Text>()
                : new GameObject("ControlHint",typeof(RectTransform),typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
            hint.transform.SetParent(panel,false);
            hint.text="우클릭: 착용·보관 메뉴    |    Ctrl+클릭: 창고 ↔ 가방    |    아이템 선택 후 1~6: 퀵슬롯 등록";
            hint.font=root.GetComponentsInChildren<UnityEngine.UI.Text>(true).First(t=>t.name=="LeftWeight").font; hint.fontSize=16;
            hint.color=UITheme.TextBright; hint.alignment=TextAnchor.MiddleCenter; hint.raycastTarget=false;
            var rt=(RectTransform)hint.transform;
            rt.anchorMin=new Vector2(.03f,.94f); rt.anchorMax=new Vector2(.93f,.995f); rt.offsetMin=rt.offsetMax=Vector2.zero;
            var weight=root.GetComponentsInChildren<UnityEngine.UI.Text>(true).First(t=>t.name=="LeftWeight");
            ((RectTransform)weight.transform).sizeDelta=new Vector2(250,30);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        // Systems has a baked scene copy, not a prefab instance. Apply the same static hint there.
        var sourceHint=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<UnityEngine.UI.Text>(true).First(t=>t.name=="ControlHint");
        foreach(var ui in Object.FindObjectsByType<CharacterPanelUI>(FindObjectsInactive.Include))
        {
            if(!ui.gameObject.scene.IsValid() || !ui.gameObject.scene.isLoaded)continue;
            var panel=ui.GetComponentsInChildren<Transform>(true).First(t=>t.name=="PanelRoot");
            var old=panel.Find("ControlHint"); if(old!=null)Object.DestroyImmediate(old.gameObject);
            var hint=Object.Instantiate(sourceHint.gameObject,panel,false); hint.name="ControlHint";
            var weight=ui.GetComponentsInChildren<UnityEngine.UI.Text>(true).First(t=>t.name=="LeftWeight");
            ((RectTransform)weight.transform).sizeDelta=new Vector2(250,30);
            hint.GetComponent<UnityEngine.UI.Text>().font=weight.font;
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);EditorSceneManager.SaveScene(ui.gameObject.scene);
        }
        AssetDatabase.SaveAssets();
        var pistol=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Characters/Firearms/PistolWeapon.prefab");
        var hero=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game/SimpleHero.prefab");
        var bat=hero.GetComponentsInChildren<MeshFilter>(true).First(m=>m.name=="Hero_Bat");
        return JsonConvert.SerializeObject(new {pistolBounds=pistol.GetComponent<MeshFilter>().sharedMesh.bounds.ToString(), batBounds=bat.sharedMesh.bounds.ToString(), batMaterial=bat.GetComponent<Renderer>().sharedMaterial.name});
    }
}
