import json,subprocess,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
wrapper=root.parents[1]/'demo13-flashlight/tools/unity_mcp_call.py'
label=sys.argv[1]
for identity in ('district_warden','pawnshop'):
 code=f'''if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play required");
var npc=UnityEngine.Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None).Single(n=>n.Data.npcId=="{identity}");
var p=GameObject.FindGameObjectWithTag("Player");var pos=new Vector3(npc.transform.position.x,0,npc.transform.position.z-1.8f);p.GetComponent<Rigidbody>().position=pos;p.transform.position=pos;Physics.SyncTransforms();CameraFollow.Instance?.SnapToTarget();
var c=Camera.main;var data=c.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();if(!data.renderPostProcessing)throw new System.Exception("Main camera post-processing disabled");return "Ready {identity}, real main camera with post-processing";'''
 for tool,args in [('eval',{'code':code,'timeout':30000}),('capture_game_view',{'width':1280,'height':960,'camera':'Main Camera','source':'camera','save_path':f'Library/RenderingReview62/{label}_{identity}.png'})]:
  file=root/'tools'/f'{label}_{identity}_{tool}.json';file.write_text(json.dumps(args),encoding='utf-8')
  p=subprocess.run([sys.executable,str(wrapper),tool,str(file)],capture_output=True,text=True,encoding='utf-8',timeout=80)
  print(identity,tool,p.stdout,flush=True)
  if p.returncode:raise SystemExit(p.returncode)
