var rows=new System.Collections.Generic.List<object>();
foreach(var id in new[]{"pawnshop","veteran_scavenger","district_warden","wandering_merchant"}){
 var p=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/NPC/"+id+".prefab");
 rows.Add(new{id,path=UnityEditor.AssetDatabase.GetAssetPath(p),children=p.GetComponentsInChildren<Transform>(true).Select(t=>new{t.name,pos=t.localPosition.ToString(),rot=t.localEulerAngles.ToString(),scale=t.localScale.ToString(),components=t.GetComponents<Component>().Select(c=>c.GetType().Name).ToArray()}).ToArray()});
}
var safe=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Safehouse.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
return Newtonsoft.Json.JsonConvert.SerializeObject(new{prefabs=rows,safehouse=safe.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NPCController>(true)).Select(n=>new{n.name,id=n.Data==null?null:n.Data.npcId,pos=n.transform.position.ToString(),rot=n.transform.eulerAngles.ToString(),scale=n.transform.lossyScale.ToString(),prefab=UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(n.gameObject)}).ToArray(),rooms=UnityEditor.AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).ToArray()});
