import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend'))
for name in ['Hero_TrouserLeg_1','Hero_TrouserCuff_1','Hero_Hips','Hero_PackStrap_1','Hero_LanternLoop','Hero_BackpackFlap']:
 o=bpy.data.objects[name];print('PART',name,len(o.data.vertices));groups={}
 for v in o.data.vertices:groups.setdefault(round(v.co.z,4),[]).append(v)
 print('SECTIONS',[(z,len(vs),min(v.co.x for v in vs),max(v.co.x for v in vs),min(v.co.y for v in vs),max(v.co.y for v in vs)) for z,vs in sorted(groups.items())])
 if 'Strap' in name or 'Loop' in name:print('VERTICES',json.dumps([(v.index,list(v.co)) for v in o.data.vertices]))
