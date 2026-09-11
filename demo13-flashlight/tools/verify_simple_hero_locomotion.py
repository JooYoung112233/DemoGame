import bpy,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;sc.render.fps=30;bpy.ops.import_scene.fbx(filepath=str(OUT/'SimpleHero_Stage4_Locomotion.fbx'))
rig=next(o for o in sc.objects if o.type=='ARMATURE');parts=[o for o in sc.objects if o.type=='MESH'];actions={a.name.split('|')[-1]:a for a in bpy.data.actions}
assert set(actions)=={'Idle','Walk','Run'} and len(parts)==17 and len(rig.data.bones)==23
for image in bpy.data.images:
 if image.source=='FILE' and image.filepath:assert image.packed_file or Path(bpy.path.abspath(image.filepath)).exists(),image.filepath
for o in parts:
 assert any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers)
 assert o.data.uv_layers.active and all(0<=u.uv.x<=1 and 0<=u.uv.y<=1 for u in o.data.uv_layers.active.data)
 assert all(v.groups and abs(sum(g.weight for g in v.groups)-1)<1e-4 for v in o.data.vertices)
reports=[]
for name,action in actions.items():
 rig.animation_data.action=action
 if action.slots:rig.animation_data.action_slot=action.slots[0]
 a,b=map(int,action.frame_range);first=None;motion=0;min_z=100;loop=0
 for k in range((b-a)*2+1):
  f=a+k*.5;sc.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();ps={}
  for o in parts:
   ev=o.evaluated_get(dg);m=ev.to_mesh();ps[o.name]=[ev.matrix_world@v.co for v in m.vertices];ev.to_mesh_clear()
   assert all(all(math.isfinite(c) for c in p) and p.length<5 for p in ps[o.name])
   if o.name.startswith('Study_Boot'):min_z=min(min_z,min(p.z for p in ps[o.name]))
  if first is None:first=ps
  delta=max((p-q).length for n in ps for p,q in zip(ps[n],first[n]));motion=max(motion,delta)
  if f==b:loop=delta
 assert loop<.001 and min_z>-.008 and motion>.001,(name,loop,min_z,motion)
 reports.append({'clip':name,'seconds':(b-a)/30,'loop_error_m':loop,'min_sole_z_m':min_z,'motion_m':motion,'half_frame_samples':(b-a)*2+1})
report={'fbx_reimport_passed':True,'bones':23,'meshes':17,'uv_weights_valid':True,'clips':reports}
(OUT/'Stage4ExportCheck.json').write_text(json.dumps(report,indent=2));print('LOCOMOTION_EXPORT_VERIFIED',report)
