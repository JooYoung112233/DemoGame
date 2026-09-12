using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Newtonsoft.Json;
public static class GroundImport {
 const string Dir="Assets/Art/Environments/GroundFinish62";
 static string Source=>Path.GetFullPath("../ArtWork/GroundFinish62");
 static string[] names={"CrackBranch_A","CrackBranch_B","BrokenPaving_A","BrokenPaving_B","DirtGravel_Transition","WallWeeds_Dust"};
 static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;Folder(Path.GetDirectoryName(path).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));}
 public static string Run(){
 if(Application.isPlaying)throw new Exception("Import requires stopped editor");
 foreach(var sub in new[]{"Models","Materials","Prefabs"})Folder(Dir+"/"+sub);
 var source=File.ReadAllText("Assets/Shaders/GameLit/GameLit.shader").Replace("\r\n","\n");
 int first=source.IndexOf("\n        Pass\n"),second=source.IndexOf("\n        Pass\n",first+1);
 if(first<0||second<0)throw new Exception("Unexpected GameLit layout");
 string shader=source.Substring(source.IndexOf("Shader \""),second-source.IndexOf("Shader \""))+"\n    }\n}\n";
 shader=shader.Replace("Shader \"BRB/GameLit\"","Shader \"BRB/GroundFadeLit\"")
 .Replace("\"RenderType\" = \"Opaque\"","\"RenderType\" = \"Transparent\"\n            \"Queue\" = \"Transparent-20\"")
 .Replace("ZWrite On","ZWrite Off\n            Blend SrcAlpha OneMinusSrcAlpha\n            Offset -1, -1")
 .Replace("#pragma vertex GameLitVertex","#pragma vertex GroundFadeVertex")
 .Replace("#pragma fragment GameLitFragment","#pragma fragment GroundFadeFragment")
 .Replace("#include \"GameLitInput.hlsl\"","#define _SURFACE_TYPE_TRANSPARENT 1\n            #include \"GameLitInput.hlsl\"")
 .Replace("            ENDHLSL",@"
            // Reuse GameLit lighting; only these ground overlays consume vertex alpha.
            Varyings GroundFadeVertex(Attributes input, half4 vertexColor : COLOR, out half fade : TEXCOORD6)
            {
                fade = vertexColor.a;
                return GameLitVertex(input);
            }
            half4 GroundFadeFragment(Varyings input, half fade : TEXCOORD6) : SV_Target
            {
                half4 color = GameLitFragment(input);
                color.a *= saturate(fade);
                return color;
            }
            ENDHLSL");
 const string sp="Assets/Shaders/GameLit/GroundFadeLit.shader";File.WriteAllText(sp,shader);AssetDatabase.ImportAsset(sp,ImportAssetOptions.ForceSynchronousImport);
 var fadeShader=AssetDatabase.LoadAssetAtPath<Shader>(sp);if(fadeShader==null||ShaderUtil.ShaderHasError(fadeShader))throw new Exception("Ground shader failed: "+JsonConvert.SerializeObject(ShaderUtil.GetShaderMessages(fadeShader)));
 var palette=new Dictionary<string,Color>{
 {"Soil",new Color(.20f,.168f,.119f)},{"DryDust",new Color(.255f,.225f,.166f)},
 {"Crack",new Color(.066f,.065f,.056f)},{"CrackLip",new Color(.12f,.124f,.108f)},
 {"Concrete",new Color(.335f,.323f,.265f)},{"ConcreteDark",new Color(.278f,.274f,.225f)},
 {"Stone",new Color(.29f,.278f,.225f)},{"Grass",new Color(.155f,.175f,.087f)},{"DryGrass",new Color(.28f,.246f,.142f)}};
 var mats=new Dictionary<string,Material>();
 foreach(var kv in palette){
 bool fade=kv.Key=="Soil"||kv.Key=="DryDust",grass=kv.Key.Contains("Grass"),stone=kv.Key.Contains("Concrete")||kv.Key=="Stone";
 string family=stone?"Plaster":"Asphalt",path=Dir+"/Materials/Ground62_"+kv.Key+".mat";
 var mat=AssetDatabase.LoadAssetAtPath<Material>(path);bool fresh=mat==null;
 if(fresh)mat=new Material(Shader.Find("BRB/GameLit"));mat.shader=fade?fadeShader:Shader.Find("BRB/GameLit");mat.name="Ground62_"+kv.Key;
 mat.SetColor("_BaseColor",kv.Value.gamma);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.15f);mat.SetFloat("_BumpScale",grass?0:.24f);
 mat.SetFloat("_GrimeStrength",.06f);mat.SetFloat("_GrimeScale",.6f);mat.SetFloat("_ShadowDesaturation",.2f);mat.SetFloat("_RimStrength",0);
 mat.SetColor("_EmissionColor",Color.black);mat.SetFloat("_Surface",fade?1:0);mat.SetFloat("_Cull",2);
 mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
 if(!grass){var tr="Assets/Art/Environments/Town02/Textures/"+family;
 mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tr+"_Base.png"));mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tr+"_Normal.png"));mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tr+"_Mask.png"));
 mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_GAMELIT_PACKED_MASK");mat.SetFloat("_UsePackedMask",1);}
 mat.renderQueue=fade?2980:2000;
 if(fresh)AssetDatabase.CreateAsset(mat,path);else EditorUtility.SetDirty(mat);mats.Add(mat.name,mat);
 }
 var audit=new List<object>();
 foreach(var name in names){
 string path=Dir+"/Models/"+name+".fbx";File.Copy(Source+"/Models/"+name+".fbx",path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
 var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.importAnimation=false;imp.addCollider=false;imp.isReadable=true;imp.importNormals=ModelImporterNormals.Import;imp.importTangents=ModelImporterTangents.Import;imp.SaveAndReimport();
 var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
 try{
 go.name=name;foreach(var r in go.GetComponentsInChildren<Renderer>()){
 r.sharedMaterials=r.sharedMaterials.Select(m=>mats[m.name]).ToArray();r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;}
 PrefabUtility.SaveAsPrefabAsset(go,Dir+"/Prefabs/"+name+".prefab");
 var mesh=go.GetComponentInChildren<MeshFilter>().sharedMesh;
 audit.Add(new{name,mesh.vertexCount,triangles=mesh.triangles.Length/3,colors=mesh.colors.Length,uv=mesh.uv.Length,tangents=mesh.tangents.Length,bounds=mesh.bounds.ToString(),missingScripts=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go)});
 }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 AssetDatabase.SaveAssets();File.WriteAllText(Source+"/Unity/ImportValidation.json",JsonConvert.SerializeObject(audit,Formatting.Indented));return "6 FBX/prefabs imported; vertex-alpha GameLit overlay shader ready";
 }
}
