using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
public static class WorldComicInventory {
 static string Out=>Path.GetFullPath("../ArtWork/WorldComic62/Unity");
 static string P(UnityEngine.Object x)=>x==null?null:AssetDatabase.GetAssetPath(x);
 public static string Run(){
 Directory.CreateDirectory(Out);
 var live=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include).Where(r=>!(r is SpriteRenderer)&&!(r is ParticleSystemRenderer)).ToArray();
 var materials=AssetDatabase.FindAssets("t:Material",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath).Distinct().SelectMany(path=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Select(m=>new{path,m})).Select(x=>new{
 x.path,x.m.name,shader=x.m.shader.name,
 textures=x.m.GetTexturePropertyNames().Select(n=>new{name=n,path=P(x.m.GetTexture(n)),scale=new[]{x.m.GetTextureScale(n).x,x.m.GetTextureScale(n).y}}).ToArray(),
 color=x.m.HasProperty("_BaseColor")?new[]{x.m.GetColor("_BaseColor").r,x.m.GetColor("_BaseColor").g,x.m.GetColor("_BaseColor").b,x.m.GetColor("_BaseColor").a}:null,
 liveUses=live.Count(r=>r.sharedMaterials.Contains(x.m))
 }).ToArray();
 var assets=AssetDatabase.FindAssets("t:Model t:Prefab t:Scene",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath).Distinct().Select(path=>new{path,materials=AssetDatabase.GetDependencies(path,true).Where(p=>p.EndsWith(".mat")).ToArray()}).Where(x=>x.materials.Length>0).ToArray();
 var renderers=live.Select(r=>new{name=r.name,scene=r.gameObject.scene.name,root=r.transform.root.name,active=r.gameObject.activeInHierarchy,enabled=r.enabled,position=new[]{r.bounds.center.x,r.bounds.center.y,r.bounds.center.z},size=new[]{r.bounds.size.x,r.bounds.size.y,r.bounds.size.z},materials=r.sharedMaterials.Select(m=>new{path=P(m),name=m==null?null:m.name,shader=m==null?null:m.shader.name,baseMap=m!=null&&m.HasProperty("_BaseMap")?P(m.GetTexture("_BaseMap")):null}).ToArray()}).ToArray();
 File.WriteAllText(Out+"/Inventory.json",JsonConvert.SerializeObject(new{materials,assets,renderers},Formatting.Indented));
 return $"Inventory: {materials.Length} materials, {assets.Length} dependent model/prefab/scenes, {renderers.Length} live non-sprite renderers";
 }
}
