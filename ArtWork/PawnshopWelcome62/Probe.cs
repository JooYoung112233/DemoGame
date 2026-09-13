using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
public static class WelcomeProbe
{
 static string Root=>Path.GetFullPath("../ArtWork/PawnshopWelcome62");
 static void Place(Vector3 p){var player=TopDownPlayer.Instance;var rb=player.GetComponent<Rigidbody>();player.transform.position=p;rb.position=p;rb.linearVelocity=Vector3.zero;Physics.SyncTransforms();CameraFollow.Instance.SnapToTarget();}
 public static string Run(){TopDownPlayer.Instance.StartCoroutine(Test());return "Testing entrance visibility, feet, dialogue and walking exit";}
 static IEnumerator Test(){
  var player=TopDownPlayer.Instance;var origin=player.transform.position;bool virtualBefore=GameInput.Virtual;GameInput.Virtual=true;UIManager.Instance.CloseAll();
  var npc=InteractableObject.All.Single(n=>n.name=="전당포 주인");var building=UnityEngine.Object.FindObjectsByType<BuildingInterior>().Single(b=>b.name=="Pawnshop");var label=UnityEngine.Object.FindObjectsByType<TextMesh>().Single(t=>t.name=="Sign_전당포").GetComponent<Renderer>();
  try{
   Place(new Vector3(34,0,37.25f));GameInput.VSetMove(Vector2.zero);yield return new WaitForSeconds(.2f);
   bool outside=!building.PlayerInside;GameInput.VSetMove(Vector2.up);yield return new WaitForSeconds(.9f);GameInput.VSetMove(Vector2.zero);yield return new WaitForSeconds(.3f);
   var entry=player.transform.position;bool entered=building.PlayerInside,hidden=!label.enabled;
   var skin=npc.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh,true);var points=mesh.vertices.Select(v=>skin.transform.TransformPoint(v)).ToArray();UnityEngine.Object.DestroyImmediate(mesh);
   float feet=points.Min(p=>p.y),head=points.Max(p=>p.y);var projected=points.Select(p=>Camera.main.WorldToViewportPoint(p)).ToArray();bool onScreen=projected.All(p=>p.x>.05f&&p.x<.95f&&p.y>.08f&&p.y<.92f&&p.z>0);
   ScreenCapture.CaptureScreenshot(Path.Combine(Root,"Entrance_GameUI.png"));yield return new WaitForSeconds(.15f);
   var target=(InteractableObject)typeof(InteractionSystem).GetField("currentTarget",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player.GetComponent<InteractionSystem>());
   GameInput.VPressKey(KeyCode.E);yield return null;GameInput.VEndFrame();GameInput.VReleaseKey(KeyCode.E);yield return new WaitForSeconds(.3f);bool dialogue=UIManager.Instance.IsAnyUIOpen();UIManager.Instance.CloseAll();
   // Check that approaching from either side of the owner stays possible.
   var approaches=new List<object>();bool bothSides=true;
   foreach(int side in new[]{-1,1}){
    GameInput.VSetMove(Vector2.zero);Place(entry);yield return new WaitForSeconds(.15f);
    // Clear the doorway before turning sideways; its jambs still bound the entry position.
    GameInput.VSetMove(Vector2.up);yield return new WaitForSeconds(.65f);GameInput.VSetMove(Vector2.right*side);yield return new WaitForSeconds(.85f);GameInput.VSetMove(Vector2.up);yield return new WaitForSeconds(.35f);GameInput.VSetMove(Vector2.zero);yield return new WaitForSeconds(.2f);
    var p=player.transform.position;bool moved=side*(p.x-entry.x)>.75f&&p.z-entry.z>.55f;bothSides&=moved;approaches.Add(new{side,position=p.ToString(),pass=moved});
   }
   Place(entry);yield return new WaitForSeconds(.15f);GameInput.VSetMove(Vector2.down);yield return new WaitForSeconds(1.7f);GameInput.VSetMove(Vector2.zero);yield return new WaitForSeconds(.25f);bool exited=!building.PlayerInside,restored=label.enabled;
   var report=new{pass=outside&&entered&&hidden&&onScreen&&Mathf.Abs(feet)<.02f&&head>1.6f&&target==npc&&dialogue&&bothSides&&exited&&restored,entry=entry.ToString(),npc=npc.transform.position.ToString(),feetY=feet,headY=head,ownerInViewport=onScreen,viewportMin=projected.Aggregate((a,p)=>Vector3.Min(a,p)).ToString(),viewportMax=projected.Aggregate((a,p)=>Vector3.Max(a,p)).ToString(),outside,entered,signHidden=hidden,npcTarget=target==null?null:target.name,dialogueOpened=dialogue,sideApproaches=approaches,exited,signRestored=restored};
   File.WriteAllText(Path.Combine(Root,"GameplayValidation.json"),JsonConvert.SerializeObject(report,Formatting.Indented));
  }finally{GameInput.VSetMove(Vector2.zero);GameInput.Virtual=virtualBefore;Place(origin);}
 }
}
