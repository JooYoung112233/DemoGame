using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public static class PawnshopMovementReview
{
    public static string Run(string tag, bool candidate)
    {
        TopDownPlayer.Instance.StartCoroutine(Measure(tag, candidate));
        return "Pawnshop walking tests started: " + tag;
    }
    static IEnumerator Measure(string tag, bool candidate)
    {
        var p = TopDownPlayer.Instance;
        var rb = p.GetComponent<Rigidbody>();
        var cap = p.GetComponent<CapsuleCollider>();
        var origin = rb.position;
        bool oldVirtual = GameInput.Virtual;
        var floor = GameObject.Find("Map/Environment/Structures/Pawnshop/Floor_In").GetComponent<BoxCollider>();
        var oldCenter = floor.center; var roads = UnityEngine.Object.FindObjectsByType<BoxCollider>().Where(c => c.name.StartsWith("Road_")).ToArray();
        var roadStates = roads.Select(c => c.enabled).ToArray();
        bool oldCrouch = p.IsCrouching;
        var rows = new List<object>();
        if (candidate) { floor.center += floor.transform.InverseTransformVector(Vector3.down * .065f); foreach (var road in roads) road.enabled = false; }
        UIManager.Instance.CloseAll();
        GameInput.Virtual = true;
        try
        {
            if (oldCrouch) yield return ToggleCrouch();
            foreach (string mode in tag == "After" ? new[] { "walk", "run", "crouch" } : new[] { "walk", "run" })
            foreach (string route in new[] { "entry", "inside", "exit" })
            {
                GameInput.VSetMove(Vector2.zero);
                GameInput.VReleaseKey(KeyCode.LeftShift);
                yield return new WaitForSeconds(.2f);
                var start = route == "entry" ? new Vector3(34, 0, 36.7f) : new Vector3(34, 0, 39.5f);
                rb.position = start; p.transform.position = start; rb.linearVelocity = Vector3.zero;
                Physics.SyncTransforms();
                yield return new WaitForSeconds(.2f);
                var dir = route == "inside" ? Vector2.right : route == "exit" ? Vector2.down : Vector2.up;
                var from = rb.position;
                if (mode == "run") GameInput.VPressKey(KeyCode.LeftShift);
                if (mode == "crouch") yield return ToggleCrouch();
                p.RefillStamina();
                GameInput.VSetMove(TopDownPlayer.WorldToInput(dir));
                double begin = Time.fixedTimeAsDouble;
                yield return new WaitForSeconds(mode == "run" ? .85f : mode == "crouch" ? 5 : 2.5f);
                float elapsed = (float)(Time.fixedTimeAsDouble - begin);
                float travelled = Vector2.Dot(Plan3D.ToPlan(rb.position - from), dir);
                float expected = StatDB.Instance.playerStat.moveSpeed * (mode == "run" ? StatDB.Instance.playerStat.sprintSpeedMultiplier : mode == "crouch" ? StatDB.Instance.playerStat.crouchSpeedMultiplier : 1) * elapsed;

                float depth = 0;
                bool overlap = floor.enabled && Physics.ComputePenetration(cap, cap.transform.position, cap.transform.rotation, floor, floor.transform.position, floor.transform.rotation, out Vector3 normal, out depth);
                rows.Add(new { mode, route, elapsed, travelled, expected, pass = travelled > expected * .85f, position = rb.position.ToString("F4"), floorOverlap = overlap, depth });
                GameInput.VSetMove(Vector2.zero);
                if (mode == "crouch") yield return ToggleCrouch();
            }
            if (oldCrouch) yield return ToggleCrouch();
        }
        finally
        {
            GameInput.VSetMove(Vector2.zero); GameInput.VReleaseKey(KeyCode.LeftShift); GameInput.Virtual = oldVirtual;
            floor.center = oldCenter;
            if (candidate) for (int i = 0; i < roads.Length; i++) roads[i].enabled = roadStates[i];
            rb.position = origin; p.transform.position = origin; rb.linearVelocity = Vector3.zero; Physics.SyncTransforms();
            File.WriteAllText(Path.GetFullPath("../ArtWork/PawnshopEntryReview/" + tag + ".json"), JsonConvert.SerializeObject(rows, Formatting.Indented));
        }
    }
    static IEnumerator ToggleCrouch()
    {
        GameInput.VPressKey(KeyCode.C); yield return null; GameInput.VEndFrame();
        GameInput.VReleaseKey(KeyCode.C); yield return null; GameInput.VEndFrame();
    }
}
