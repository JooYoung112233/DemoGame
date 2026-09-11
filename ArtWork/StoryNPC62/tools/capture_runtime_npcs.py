"""Capture existing NPCs via Unity MCP, no desktop automation or scene-file edits."""
import json, subprocess, sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
wrapper=root.parents[1]/'demo13-flashlight/tools/unity_mcp_call.py'
for identity in ('pawnshop','veteran_scavenger','district_warden','wandering_merchant'):
    code='''if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play required");
var npc=UnityEngine.Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).Single(n=>n.Data.npcId=="IDENTITY");
var p=GameObject.FindGameObjectWithTag("Player");var pos=new Vector3(npc.transform.position.x,0,npc.transform.position.z-1.8f);p.GetComponent<Rigidbody>().position=pos;p.transform.position=pos;Physics.SyncTransforms();CameraFollow.Instance?.SnapToTarget();
var camera=GameObject.Find("NPCReviewCamera")?.GetComponent<Camera>();if(camera==null){camera=new GameObject("NPCReviewCamera").AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.tag="Untagged";camera.enabled=false;}
camera.transform.rotation=Quaternion.Euler(62,0,0);camera.transform.position=npc.transform.position+Vector3.up*.15f-camera.transform.forward*15f;camera.orthographic=true;camera.orthographicSize=3.6f;
return "Ready IDENTITY";
'''.replace('IDENTITY',identity)
    args=root/'tools'/f'preview_{identity}_runtime.json'
    args.write_text(json.dumps({'code':code,'timeout':30000}),encoding='utf-8')
    cap=root/'tools'/f'capture_{identity}.json'
    cap.write_text(json.dumps({'width':1000,'height':800,'camera':'NPCReviewCamera','save_path':f'Library/StoryNPC62Review/{identity}.png','source':'camera'}),encoding='utf-8')
    for tool,request in [('eval',args),('capture_game_view',cap)]:
        p=subprocess.run([sys.executable,str(wrapper),tool,str(request)],capture_output=True,text=True,encoding='utf-8',timeout=80)
        print(identity,tool,p.stdout,flush=True)
        if p.returncode:raise SystemExit(p.returncode)
