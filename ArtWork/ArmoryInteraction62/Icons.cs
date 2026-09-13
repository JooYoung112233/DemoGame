using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
public static class ArmoryIcons
{
    public static string Run()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Edit mode required");
        const string folder="Assets/Art/Icons/Armory";
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        var pistol=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Characters/Firearms/PistolWeapon.prefab").GetComponent<MeshFilter>();
        var hero=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ChibiSurvivor/Player/SimpleHeroStudy/Game/SimpleHero.prefab");
        var bat=hero.GetComponentsInChildren<MeshFilter>(true).First(m=>m.name=="Hero_Bat");
        Capture(pistol,"Pistol9",new Vector3(1,.3f,-.2f),-25);
        Capture(bat,"Bat",new Vector3(0,0,-1),-35);
        AssetDatabase.SaveAssets(); return "Two weapon icons captured from current meshes and materials.";
    }
    static void Capture(MeshFilter source,string itemName,Vector3 view,float roll)
    {
        const string folder="Assets/Art/Icons/Armory";
        var root=new GameObject("ArmoryIconPreview"); root.hideFlags=HideFlags.HideAndDontSave; root.transform.position=new Vector3(1000,1000,1000);
        var prev=RenderTexture.active; RenderTexture rt=null; Texture2D tex=null;
        try
        {
            var mesh=new GameObject("Weapon",typeof(MeshFilter),typeof(MeshRenderer)); mesh.transform.SetParent(root.transform,false);mesh.layer=31;
            mesh.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;
            mesh.GetComponent<MeshRenderer>().sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;
            mesh.transform.localPosition=-source.sharedMesh.bounds.center;
            var cameraGO=new GameObject("Camera",typeof(Camera));cameraGO.transform.SetParent(root.transform,false);
            var cam=cameraGO.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=source.sharedMesh.bounds.extents.magnitude*1.08f;
            cam.transform.localPosition=view.normalized*3;cam.transform.LookAt(root.transform);cam.transform.Rotate(0,0,roll,Space.Self);
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.cullingMask=1<<31;cam.nearClipPlane=.1f;cam.farClipPlane=10;cam.allowHDR=false;cam.allowMSAA=true;
            var lightGO=new GameObject("Light",typeof(Light));lightGO.transform.SetParent(root.transform,false);lightGO.transform.rotation=Quaternion.Euler(35,-35,0);
            var light=lightGO.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.cullingMask=1<<31;
            rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            tex=new Texture2D(256,256,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,256,256),0,0);tex.Apply();
            string path=folder+"/"+itemName+".png";File.WriteAllBytes(path,tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var item=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Resources/Items/Weapon/"+itemName+".asset");item.icon=AssetDatabase.LoadAssetAtPath<Sprite>(path);EditorUtility.SetDirty(item);
        }
        finally {RenderTexture.active=prev;if(tex!=null)Object.DestroyImmediate(tex);if(rt!=null){rt.Release();Object.DestroyImmediate(rt);}Object.DestroyImmediate(root);}
    }
}
