using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Newtonsoft.Json;

public static class PawnshopEntryApply
{
    public static string Run()
    {
        if (Application.isPlaying) throw new Exception("Stop play before saving the town.");
        const string path = "Assets/Scenes/Safehouse.unity";
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            var map = scene.GetRootGameObjects().Single(g => g.name == "Map");
            var colliders = map.GetComponentsInChildren<Collider>(true);
            var floors = colliders.OfType<BoxCollider>().Where(c => c.name == "Floor_In" || c.name.StartsWith("Road_")).ToArray();
            if (floors.Length != 9) throw new Exception("Expected five building floors and four road overlays; got " + floors.Length);
            var ground = colliders.Single(c => c.name == "Ground" && c.enabled && !c.isTrigger);
            foreach (var floor in floors)
            {
                var b = floor.bounds; var g = ground.bounds;
                if (b.min.x < g.min.x || b.max.x > g.max.x || b.min.z < g.min.z || b.max.z > g.max.z || Mathf.Abs(g.max.y) > .001f)
                    throw new Exception("Overlay is not supported by the Y=0 town ground: " + floor.name);
            }
            // Scene-owned solid obstacles and interaction connections must stay untouched.
            string Snapshot() => JsonConvert.SerializeObject(new {
                otherColliders = colliders.Except(floors).Select(c => new { path = AnimationUtility.CalculateTransformPath(c.transform, map.transform), json = EditorJsonUtility.ToJson(c) }),
                interactions = map.GetComponentsInChildren<InteractableObject>(true).Select(c => new { c.name, json = EditorJsonUtility.ToJson(c) })
            });
            string before = Snapshot();
            var changes = floors.Select(c => new { path = AnimationUtility.CalculateTransformPath(c.transform, map.transform), beforeTop = c.bounds.max.y }).ToArray();
            foreach (var floor in floors)
            {
                floor.enabled = false;
                if (floor.name == "Floor_In")
                {
                    var pos = floor.transform.position;
                    pos.y = -.035f;
                    floor.transform.position = pos;
                    EditorUtility.SetDirty(floor.transform);
                }
                EditorUtility.SetDirty(floor);
            }
            if (before != Snapshot()) throw new Exception("Unexpected obstacle or interaction change.");
            var pawn = map.GetComponentsInChildren<BuildingInterior>(true).Single(b => b.name == "Pawnshop");
            var shell = pawn.transform.Find("Shell_Art02").GetComponentInChildren<MeshFilter>();
            var mesh = shell.sharedMesh;
            if (mesh == null || mesh.uv.Length != mesh.vertexCount || mesh.triangles.Length / 3 != 1996)
                throw new Exception("Pawnshop model import/UV validation failed.");
            var materialPaths = shell.GetComponent<Renderer>().sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray();
            if (materialPaths.Any(string.IsNullOrEmpty)) throw new Exception("Missing pawnshop material.");
            int missing = map.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            if (missing != 0) throw new Exception("Missing script in Safehouse.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Path.GetFullPath("../ArtWork/PawnshopEntryReview/Applied.json"), JsonConvert.SerializeObject(new { changes, groundTop = ground.bounds.max.y, obstaclesAndInteractionsPreserved = true, missingScripts = missing, triangles = mesh.triangles.Length / 3, materialPaths }, Formatting.Indented));
            return "Saved 9 visual-only floor/road overlays and verified pawnshop import.";
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
}
