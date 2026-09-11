if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Preserve Play");
var paths=new[]{
"Assets/ChibiSurvivor/Player/SimpleHeroStudy/Materials/SimpleHero_Surface.mat",
"Assets/ChibiSurvivor/Player/SimpleHeroStudy/Materials/SimpleHero_Gear.mat",
"Assets/ChibiSurvivor/Bandit/SimpleBandit/Materials/SimpleBandit_Surface.mat",
"Assets/ChibiSurvivor/Bandit/SimpleBandit/Weapons/Materials/BanditClub_Surface.mat",
"Assets/ChibiSurvivor/Bandit/Firearms/Gunmetal.mat",
"Assets/ChibiSurvivor/Bandit/Firearms/Holster.mat"};
foreach(var path in paths){
 var dest="D:/Demo/ArtWork/MaterialReview62/BeforeFiles/"+path;System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest));if(!System.IO.File.Exists(dest))System.IO.File.Copy(path,dest);
 var m=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);UnityEditor.Undo.RecordObject(m,"Unify character material readability");
 m.SetFloat("_RimStrength",.018f);m.SetFloat("_GrimeStrength",.10f);m.SetFloat("_ShadowDesaturation",.18f);
 if(m.name=="Holster")m.SetFloat("_Smoothness",.20f);
 UnityEditor.EditorUtility.SetDirty(m);UnityEditor.AssetDatabase.SaveAssetIfDirty(m);
}
System.IO.File.WriteAllText("D:/Demo/ArtWork/MaterialReview62/AppliedCharacters.json",Newtonsoft.Json.JsonConvert.SerializeObject(paths));return paths.Length;
