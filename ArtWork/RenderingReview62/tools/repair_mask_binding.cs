var rows=new System.Collections.Generic.List<object>();
foreach(var name in new[]{"Pawnshop","VeteranScavenger","DistrictWarden","WanderingMerchant"}){
 var path="Assets/ChibiSurvivor/NPC/StoryNPC62/Materials/"+name+".mat";var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);var tp="Assets/ChibiSurvivor/NPC/StoryNPC62/Textures/"+name+"/"+name+"_Mask.png";var tex=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(tp);if(tex==null)throw new System.Exception(tp);
 m.SetTexture("_MetallicGlossMap",tex);UnityEditor.EditorUtility.SetDirty(m);UnityEditor.AssetDatabase.SaveAssetIfDirty(m);
 var so=new UnityEditor.SerializedObject(m);var props=so.FindProperty("m_SavedProperties.m_TexEnvs");bool serialized=false;for(int i=0;i<props.arraySize;i++){var e=props.GetArrayElementAtIndex(i);if(e.FindPropertyRelative("first").stringValue=="_MetallicGlossMap")serialized=e.FindPropertyRelative("second.m_Texture").objectReferenceValue==tex;}
 if(!serialized||m.GetTexture("_MetallicGlossMap")!=tex)throw new System.Exception("Mask binding did not persist: "+name);rows.Add(new{name,texture=UnityEditor.AssetDatabase.GetAssetPath(m.GetTexture("_MetallicGlossMap")),serialized});
}
System.IO.File.WriteAllText("D:/Demo/ArtWork/RenderingReview62/MaskBindings.json",Newtonsoft.Json.JsonConvert.SerializeObject(rows,Newtonsoft.Json.Formatting.Indented));return Newtonsoft.Json.JsonConvert.SerializeObject(rows);
