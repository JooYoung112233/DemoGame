using System;
using System.IO;
using System.Collections;
using UnityEngine;

public static class PawnshopCapture
{
    public static string Run(string tag, bool inside)
    {
        var p = TopDownPlayer.Instance;
        UIManager.Instance.CloseAll();
        var pos = new Vector3(34, 0, inside ? 40.5f : 35.5f);
        p.GetComponent<Rigidbody>().position = pos;
        p.transform.position = pos;
        p.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        p.StartCoroutine(Save(tag));
        return "Capture requested";
    }
    static IEnumerator Save(string tag)
    {
        yield return new WaitForSeconds(2);
        yield return new WaitForEndOfFrame();
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Path.GetFullPath("../ArtWork/PawnshopEntryReview/" + tag + ".png"), tex.EncodeToPNG());
        UnityEngine.Object.Destroy(tex);
    }
}
