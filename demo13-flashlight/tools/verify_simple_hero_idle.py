import bpy,math,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;sc.render.fps=30
bpy.ops.import_scene.fbx(filepath=str(OUT/'SimpleHero_Stage3_Idle.fbx'))
rig=next(o for o in sc.objects if o.type=='ARMATURE');parts=[o for o in sc.objects if o.type=='MESH'];actions=list(bpy.data.actions)
assert len(parts)==17 and len(rig.data.bones)==23
assert len(actions)==1 and 'Idle' in actions[0].name
rig.animation_data.action=actions[0];a,b=map(int,actions[0].frame_range)
assert abs((b-a)/30-3)<.04
for o in parts:
 assert any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers)
 assert o.data.uv_layers.active
 assert all(v.groups and abs(sum(g.weight for g in v.groups)-1)<1e-4 for v in o.data.vertices)
first=None;drift=0;motion=0;loop=0
for f in sorted(set(list(range(a,b+1,3))+[b])):
 sc.frame_set(f);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();positions={}
 for o in parts:
  ev=o.evaluated_get(dg);m=ev.to_mesh();positions[o.name]=[ev.matrix_world@v.co for v in m.vertices];ev.to_mesh_clear()
  assert all(all(math.isfinite(c) for c in v) and v.length<5 for v in positions[o.name])
 if first is None:first=positions
 for n,ps in positions.items():
  d=max((p-q).length for p,q in zip(ps,first[n]));motion=max(motion,d)
  if n.startswith('Study_Boot'):drift=max(drift,d)
  if f==b:loop=max(loop,d)
assert drift<.001 and loop<.001 and motion>.001,(drift,loop,motion)
report={'fbx_skin_animation_reimport_passed':True,'clips':[actions[0].name],'bones':23,'meshes':17,'seconds':(b-a)/30,'boot_drift_m':drift,'loop_error_m':loop,'motion_m':motion,'all_vertices_weighted':True}
(OUT/'Stage3ExportCheck.json').write_text(json.dumps(report,indent=2));print('IDLE_EXPORT_VERIFIED',report)
