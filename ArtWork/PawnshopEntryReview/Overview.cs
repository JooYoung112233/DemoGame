using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

public static class PawnshopOverview
{
    public static string Run()
    {
        var main = Camera.main;
        var go = new GameObject("PawnshopReviewCamera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = go.AddComponent<Camera>(); camera.CopyFrom(main); camera.enabled = false;
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        var originalData = main.GetComponent<UniversalAdditionalCameraData>();
        if (originalData != null) EditorUtility.CopySerialized(originalData, data);
        var rt = new RenderTexture(1440, 1080, 24, RenderTextureFormat.ARGBHalf);
        var old = RenderTexture.active;
        try
        {
            foreach (int pitch in new[] { 62, 30 })
            {
                camera.transform.rotation = Quaternion.Euler(pitch, 0, 0);
                camera.transform.position = new Vector3(34, 1, 40) - camera.transform.forward * 30;
                camera.orthographicSize = 9; camera.aspect = 4f / 3; camera.nearClipPlane = .3f;
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                var tex = new Texture2D(1440, 1080, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1440, 1080), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.GetFullPath("../ArtWork/PawnshopEntryReview/Final_Overview" + pitch + ".png"), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }
        finally
        {
            RenderTexture.active = old; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(go);
        }
        return "Captured the live town from two review angles without changing the gameplay camera.";
    }
}
