"""Keep source timing; enforce two-hand contact between the original 30 Hz keys."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview';CACHE=ROOT/'Library/CodexBlender/AxeReview'
SUBSTEPS=16
bpy.ops.wm.open_mainfile(filepath=str(CACHE/'FittedReview.blend'));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig'];arm=rig.data;newrest={b.name:b.matrix_local.copy() for b in arm.bones};bone_data={b.name:(b.head_local.copy(),b.tail_local.copy(),b.use_deform) for b in arm.bones};grip_errors=[]
code=Path(__file__).with_name('build_dark_survivor_game.py').read_text(encoding='utf8');code=code[code.index('def fit_sword_grip(pose):'):code.index('clips=[]')]
code=code.replace("axis=sum((pose['HandSocket.'+s][0].to_quaternion()@Vector((0,1,0)) for s in ['R','L']),Vector()).normalized()","axis=pose['HandSocket.R'][0].to_quaternion()@Vector((0,1,0))")
exec(code.replace("socketq=axis.to_track_quat('Y','Z')","socketq=pose['HandSocket.R'][0].to_quaternion()"))
exec(Path(__file__).with_name('axe_left_grip_solver.py').read_text(encoding='utf8'))
left_rolls=[p['roll'] for p in json.loads((OUT/'LeftHandFix.json').read_text())['frames']]
for name in ['AxeChop01']:
 old=bpy.data.actions[name];rig.animation_data.action=old;end=int(old.frame_range[1]);samples=[]
 for j in range((end-1)*SUBSTEPS+1):
  frame=1+j/SUBSTEPS;scene.frame_set(int(frame),subframe=frame%1);bpy.context.view_layer.update()
  samples.append({p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones})
 old.name=name+'_Source30';a=bpy.data.actions.new(name);a.use_fake_user=True;rig.animation_data.action=a;prev={}
 for j,sample in enumerate(samples,1):
  for p in rig.pose.bones:p.location,p.rotation_quaternion,p.scale=sample[p.name]
  bpy.context.view_layer.update();fit_sword_grip({p.name:(p.matrix.copy(),p.location.copy()) for p in rig.pose.bones})
  time=(j-1)/SUBSTEPS;idx=int(time);t=time-idx;roll=left_rolls[idx]*(1-t)+left_rolls[min(idx+1,len(left_rolls)-1)]*t
  solve_left_grip(rig,roll_hint=roll)
  for p in rig.pose.bones:
   q=p.rotation_quaternion.copy()
   if p.name in prev and prev[p.name].dot(q)<0:q.negate()
   p.rotation_quaternion=q;prev[p.name]=q
   for prop in ['location','rotation_quaternion','scale']:p.keyframe_insert(prop,frame=j,group=p.name)
 for fc in a.fcurves:
  for k in fc.keyframe_points:k.interpolation='LINEAR'
 bpy.data.actions.remove(old)
scene.render.fps=30*SUBSTEPS;scene.frame_start=1;scene.frame_end=(end-1)*SUBSTEPS+1;rig.animation_data.action=bpy.data.actions['AxeChop01'];scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/DarkSurvivor_AxeChop.blend'))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in scene.objects:
 if o.type=='MESH' and (o.name.startswith('Hero_') or o.name.startswith('Review_Axe_')) and o.name!='Hero_SwordProxy':o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'DarkSurvivor_AxeChop.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
report=json.loads((OUT/'ChopReview.json').read_text());report['export_sampling_fps']=30*SUBSTEPS;report['dense_support_hand_error_m']=max(grip_errors);(OUT/'ChopReview.json').write_text(json.dumps(report,indent=2))
print('DENSE_GRIP_READY',max(grip_errors),flush=True)
