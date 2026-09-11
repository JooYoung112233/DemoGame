"""Source compatibility, sampled deformation and FBX round-trip checks."""
import bpy,json,math,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT.parent/'ArtWork/BanditRoles62'
def rigsig(rig):return hashlib.sha256(repr([(b.name,b.parent.name if b.parent else None,[tuple(row) for row in b.matrix_local]) for b in rig.data.bones]).encode()).hexdigest()
def actionsig():return hashlib.sha256(repr([(a.name,[(c.data_path,c.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right),k.interpolation) for k in c.keyframe_points]) for c in a.fcurves]) for a in bpy.data.actions]).encode()).hexdigest()
def weights(o):return [sorted((o.vertex_groups[g.group].name,round(g.weight,6)) for g in v.groups) for v in o.data.vertices]
bpy.ops.wm.open_mainfile(filepath=str(ROOT.parent/'ArtWork/CharacterProportionC/BlenderSource~/Bandit_C.blend'));rig=bpy.data.objects['SimpleHero_Rig'];sourceRig=rigsig(rig);sourceActions=actionsig();body=bpy.data.objects['Bandit_Body'];hood=body.vertex_groups['Part_Bandit_Hood'].index
sourceWeights=[w for v,w in zip(body.data.vertices,weights(body)) if not any(g.group==hood and g.weight>.99 for g in v.groups)]
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/Bandit_Ranged_C.blend'));rig=bpy.data.objects['SimpleHero_Rig'];body=bpy.data.objects['Bandit_Body'];assert rigsig(rig)==sourceRig and actionsig()==sourceActions
assert weights(body)[:len(sourceWeights)]==sourceWeights,'Existing skin weights changed'
assert len(body.data.uv_layers)==1,'UV0 must remain populated for inherited and new parts'
assert all(p.area>1e-10 and p.normal.length>.9 for p in body.data.polygons),'Degenerate surfaces'
assert all(math.isfinite(n) for u in body.data.uv_layers[0].data for n in u.uv),'Invalid UV'
names={b.name for b in rig.data.bones}
for v in body.data.vertices:assert abs(sum(g.weight for g in v.groups if body.vertex_groups[g.group].name in names)-1)<.001,v.index
def activate(action,frame):
 rig.data.pose_position='POSE';rig.animation_data.action=action
 if action.slots:rig.animation_data.action_slot=action.slots[0]
 bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
def bounds():
 deps=bpy.context.evaluated_depsgraph_get();ev=body.evaluated_get(deps);me=ev.to_mesh();ps=[body.matrix_world@v.co for v in me.vertices]
 assert all(math.isfinite(n) for p in ps for n in p)
 result=[max(p[i] for p in ps)-min(p[i] for p in ps) for i in range(3)];ev.to_mesh_clear();return result
count=0
for action in bpy.data.actions:
 for frame in range(int(action.frame_range[0]),int(action.frame_range[1])+1):
  activate(action,frame);size=bounds();assert max(size)<4 and min(size)>.05,(action.name,frame,size);count+=1
activate(bpy.data.actions['Idle'],1);expected=bounds();actionNames=[a.name for a in bpy.data.actions]
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(OUT/'Models/Bandit_Ranged_C.fbx'));rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');body=bpy.data.objects['Bandit_Body']
assert len(rig.data.bones)==23
for name in actionNames:assert any(a.name.endswith('|'+name) or a.name==name for a in bpy.data.actions),name
action=next(a for a in bpy.data.actions if a.name.endswith('|Idle') or a.name=='Idle');activate(action,int(action.frame_range[0]));actual=bounds();err=max(abs(a-b) for a,b in zip(expected,actual));assert err<.006,(actual,expected)
report={'bones':23,'sourceSkeletonPreserved':True,'allSourceActionKeysPreserved':True,'retainedBodyVertices':len(sourceWeights),'retainedBodyWeightsPreserved':True,'normalisedNewWeights':True,'metricAndAtlasUV0':True,'sampledFrames':count,'fbxActionCount':len(actionNames),'fbxIdleDimensionError':err,'viewPitchYaw':[62,0],'notValidated':['Unity GameLit materials','firearm aim/shoot/draw/stow','world-space IK/weapon clipping','runtime physics']}
(OUT/'RigAndFBXValidation.json').write_text(json.dumps(report,indent=2),encoding='utf8');print('RANGED_C_VALIDATED',json.dumps(report))
