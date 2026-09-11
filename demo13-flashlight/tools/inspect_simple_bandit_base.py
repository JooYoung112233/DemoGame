import bpy,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_TwoHandBat.blend'))
r=bpy.data.objects['SimpleHero_Rig'];r.data.pose_position='REST';bpy.context.view_layer.update()
rows=[]
for o in bpy.context.scene.objects:
 if o.type=='MESH' and o.name.startswith(('Study_','Gear_','Hero_Bat')):
  ps=[o.matrix_world@v.co for v in o.data.vertices];rows.append({'name':o.name,'bounds':[[round(min(p[i] for p in ps),4),round(max(p[i] for p in ps),4)] for i in range(3)],'v':len(ps),'mod':[(m.name,m.type) for m in o.modifiers]})
print('PARTS',json.dumps(rows),flush=True)
