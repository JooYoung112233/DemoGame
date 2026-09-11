"""Offline FBX re-import validation; does not prove Unity clips or physics."""
import bpy,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT.parent/'ArtWork/CharacterProportionC';rows=[]
def set_idle():
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 action=next(a for a in bpy.data.actions if a.name=='Idle' or a.name.endswith('|Idle'))
 rig.animation_data_create();rig.animation_data.action=action
 if action.slots:rig.animation_data.action_slot=action.slots[0]
 rig.data.pose_position='POSE';bpy.context.scene.frame_set(int(action.frame_range[0]));bpy.context.view_layer.update()
 return rig
def dimensions():
 deps=bpy.context.evaluated_depsgraph_get();points=[]
 for o in bpy.context.scene.objects:
  if o.type!='MESH' or not o.name.startswith(('Study_','Gear_','Bandit_Body')):continue
  ev=o.evaluated_get(deps);me=ev.to_mesh();points.extend([o.matrix_world@v.co for v in me.vertices]);ev.to_mesh_clear()
 return [max(p[i] for p in points)-min(p[i] for p in points) for i in range(3)]
for label in ['Player','PlayerBat','Bandit','BanditCombat']:
 bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~'/f'{label}_C.blend'));set_idle();expected=dimensions();actions=[a.name for a in bpy.data.actions]
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(OUT/'Models'/f'{label}_C.fbx'));rig=set_idle();actual=dimensions()
 assert len(rig.data.bones)==23,(label,'bones',len(rig.data.bones))
 assert max(abs(a-b) for a,b in zip(actual,expected))<.006,(label,expected,actual)
 for name in actions:assert any(a.name==name or a.name.endswith('|'+name) for a in bpy.data.actions),(label,name)
 for o in bpy.context.scene.objects:
  if o.type=='MESH':
   assert o.data.uv_layers and all(math.isfinite(n) for v in o.data.vertices for n in v.co),o.name
 rows.append({'model':label,'sourceIdleDimensions':expected,'fbxIdleDimensions':actual,'maxDimensionError':max(abs(a-b) for a,b in zip(actual,expected)),'bones':len(rig.data.bones),'actionCount':len(actions),'uvPresent':True})
(OUT/'FBXValidation.json').write_text(json.dumps(rows,indent=2),encoding='utf8');print('C_FBX_VALIDATED',json.dumps(rows))
