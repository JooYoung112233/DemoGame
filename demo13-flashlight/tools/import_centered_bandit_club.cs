const string folder="Assets/ChibiSurvivor/Bandit/SimpleBandit/Weapons";
UnityEditor.AssetDatabase.ImportAsset(folder+"/BanditClub.fbx",UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var imported=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/BanditClub.fbx").GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
var mesh=UnityEngine.Object.Instantiate(imported.sharedMesh);mesh.name="BanditClub";
int index=System.Array.FindIndex(imported.bones,b=>b.name=="HandSocket.R");var bind=mesh.bindposes[index];var scale=imported.bones[index].lossyScale;
mesh.vertices=mesh.vertices.Select(v=>Vector3.Scale(bind.MultiplyPoint3x4(v),scale)).ToArray();mesh.boneWeights=new BoneWeight[0];mesh.bindposes=new Matrix4x4[0];mesh.RecalculateNormals();mesh.RecalculateBounds();
var saved=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(folder+"/BanditClubMesh.asset");UnityEditor.EditorUtility.CopySerialized(mesh,saved);UnityEditor.EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(mesh);UnityEditor.AssetDatabase.SaveAssets();return saved.bounds.ToString("F5");
