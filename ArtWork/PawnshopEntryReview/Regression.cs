using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public static class PawnshopRegression
{
    public static string Run()
    {
        TopDownPlayer.Instance.StartCoroutine(Check());
        return "Roof and collision regression started";
    }
    static IEnumerator Check()
    {
        var p = TopDownPlayer.Instance;
        var rb = p.GetComponent<Rigidbody>();
        var pawn = UnityEngine.Object.FindObjectsByType<BuildingInterior>().Single(b => b.name == "Pawnshop");
        var roof = pawn.transform.Find("Roof_Art02").GetComponentInChildren<Renderer>();
        var rows = new List<object>();
        void Place(Vector3 pos) { rb.position = pos; p.transform.position = pos; rb.linearVelocity = Vector3.zero; Physics.SyncTransforms(); }
        Place(new Vector3(34, 0, 40.5f));
        yield return new WaitForSeconds(.4f);
        rows.Add(new { check = "Inside hides roof", pass = pawn.PlayerInside && !roof.enabled });
        Place(new Vector3(34, 0, 35.5f));
        yield return new WaitForSeconds(.4f);
        rows.Add(new { check = "Outside restores roof", pass = !pawn.PlayerInside && roof.enabled });
        var floors = UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include)
            .Where(c => c.gameObject.scene.name == "Safehouse" && (c.name == "Floor_In" || c.name.StartsWith("Road_"))).ToArray();
        rows.Add(new { check = "Nine decorative floor and road colliders disabled", pass = floors.Length == 9 && floors.All(c => !c.enabled) });
        bool ground = Physics.Raycast(new Vector3(34, .5f, 40.5f), Vector3.down, out var groundHit, 1, ~0, QueryTriggerInteraction.Ignore);
        rows.Add(new { check = "Ground still supports the interior at Y=0", pass = ground && groundHit.collider.name == "Ground" && Mathf.Abs(groundHit.point.y) < .001f });
        bool leftWall = Physics.Raycast(new Vector3(32, .9f, 37), Vector3.forward, out var wall, 3, ~0, QueryTriggerInteraction.Ignore);
        rows.Add(new { check = "Front wall still solid", pass = leftWall && wall.collider.name == "Pawnshop_S_a" });
        bool blocked = Physics.Raycast(new Vector3(34, .9f, 37), Vector3.forward, 3, ~0, QueryTriggerInteraction.Ignore);
        rows.Add(new { check = "Doorway open at body height", pass = !blocked });
        rows.Add(new { check = "Updated art adds no physical step", pass = pawn.transform.Find("Shell_Art02").GetComponentsInChildren<Collider>().Length == 0 });
        File.WriteAllText(Path.GetFullPath("../ArtWork/PawnshopEntryReview/Regression.json"), JsonConvert.SerializeObject(rows, Formatting.Indented));
    }
}
