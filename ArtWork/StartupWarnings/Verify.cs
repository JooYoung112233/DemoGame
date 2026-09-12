using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

public static class StartupWarningReview
{
    static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../../ArtWork/StartupWarnings"));
    public static string SaveCompatibility()
    {
        var parse = typeof(SaveManager).GetMethod("ParseJson", BindingFlags.Static | BindingFlags.NonPublic);
        GameSaveData Read(string json) => (GameSaveData)parse.Invoke(null, new object[] { json });
        var rows = new List<object>();
        foreach (string path in Directory.GetFiles(Application.persistentDataPath, "save*.json"))
        {
            string original = File.ReadAllText(path);
            var expected = JObject.Parse(original);
            var actual = JObject.Parse(SaveManager.Instance.ToJson(Read(original)));
            bool same = expected.Properties().All(p => JToken.DeepEquals(p.Value, actual[p.Name]));
            bool untouched = File.ReadAllText(path) == original;
            rows.Add(new { file = Path.GetFileName(path), same, untouched });
            if (!same || !untouched) throw new Exception("Existing save compatibility failed: " + Path.GetFileName(path));
        }
        const string legacy = "{\"version\":1,\"currency\":123,\"bagItems\":[{\"itemId\":\"bag\",\"count\":1,\"containerItems\":[{\"itemId\":\"gun\",\"count\":1,\"durability\":0.75,\"ammoCount\":7,\"ammoItemId\":\"ammo\",\"attachments\":[\"scope\"]}]}]}";
        var legacyData = Read(legacy);
        bool legacyOK = legacyData.currency == 123 && legacyData.bagItems[0].containerItems[0].ammoCount == 7 && legacyData.bagItems[0].containerItems[0].attachments[0] == "scope";
        var data = new GameSaveData { version = 1 };
        var node = new GridItemEntry { itemId = "root", count = 1 };
        data.bagItems.Add(node);
        for (int i = 0; i < 15; i++) { var child = new GridItemEntry { itemId = "level" + i, count = i + 1, attachments = new[] { "scope" }, ammoCount = 7 }; node.containerItems = new List<GridItemEntry> { child }; node = child; }
        var decoded = Read(SaveManager.Instance.ToJson(data));
        node = decoded.bagItems[0];
        int depth = 0;
        while (node.containerItems != null && node.containerItems.Count > 0) { depth++; node = node.containerItems[0]; }
        bool pass = legacyOK && depth == 15 && node.itemId == "level14" && node.attachments[0] == "scope" && node.ammoCount == 7;
        var result = new { pass, existingSaves = rows, legacyOK, nestedDepth = depth };
        File.WriteAllText(Path.Combine(Root, "save-compatibility.json"), JsonConvert.SerializeObject(result, Formatting.Indented));
        if (!pass) throw new Exception("Save roundtrip failed");
        return JsonConvert.SerializeObject(result);
    }
    public static string Snapshot()
    {
        var d = DenseAnomalyController.Instance;
        return JsonConvert.SerializeObject(new {
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            playing = Application.isPlaying,
            visionOverlayObjects = UnityEngine.Object.FindObjectsByType<VisionDarkness>(FindObjectsInactive.Include).Length,
            anomaly = d != null,
            shaderChecked = d == null ? (object)null : typeof(DenseAnomalyController).GetField("_shaderChecked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(d),
            fogObjects = GameObject.Find("AnomalyFogQuad") != null
        });
    }
    public static string MissingShader()
    {
        DenseAnomalyController.Instance.StartCoroutine(CheckMissing());
        return "Started 180-frame missing-resource regression";
    }
    static IEnumerator CheckMissing()
    {
        var d = DenseAnomalyController.Instance;
        float oldIntensity = d.intensity, oldSpeed = d.lerpSpeed;
        bool oldBaseline = d.useRegionBaseline;
        int warnings = 0;
        Application.LogCallback handler = (message, stack, type) => { if (type == LogType.Warning && message.StartsWith("[DenseAnomaly]")) warnings++; };
        Application.logMessageReceived += handler;
        try
        {
            if (Shader.Find("BRB/AnomalyFog") != null) throw new Exception("Test requires missing shader");
            d.enabled = false; d.enabled = true;
            d.useRegionBaseline = false; d.lerpSpeed = 100f; d.SetIntensity(1f);
            for (int i = 0; i < 180; i++) yield return null;
            bool checkedOnce = (bool)typeof(DenseAnomalyController).GetField("_shaderChecked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(d);
            bool noQuad = GameObject.Find("AnomalyFogQuad") == null;
            File.WriteAllText(Path.Combine(Root, "missing-shader-regression.json"), JsonConvert.SerializeObject(new { frames = 180, warnings, checkedOnce, noQuad, pass = warnings == 1 && checkedOnce && noQuad }, Formatting.Indented));
            d.SetIntensity(0f);
            yield return null;
        }
        finally
        {
            Application.logMessageReceived -= handler;
            d.intensity = oldIntensity; d.lerpSpeed = oldSpeed; d.useRegionBaseline = oldBaseline;
        }
    }
}
