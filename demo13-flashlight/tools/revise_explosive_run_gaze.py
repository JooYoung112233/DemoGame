"""Raise the running gaze without altering the accepted gait or head oscillation."""
import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector,Quaternion
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/ChibiSurvivor/Player/ExplosiveRunReview'
OUT=BASE/'GazeReview';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'CompactSurvivor_ExplosiveRun.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];action=bpy.data.actions['Run_Explosive_Unarmed']
rig.animation_data.action=action
if action.slots:rig.animation_data.action_slot=action.slots[0]
head=rig.pose.bones['Head'];path='pose.bones["Head"].rotation_quaternion'
def protected_digest():
 return hashlib.sha256(repr([(a.name,[(fc.data_path,fc.array_index,[(tuple(k.co),k.interpolation) for k in fc.keyframe_points]) for fc in a.fcurves if not(a==action and fc.data_path==path)]) for a in bpy.data.actions]).encode()).hexdigest()
before=protected_digest()
rest=head.bone.matrix_local.to_quaternion()
def gaze():
 forward=(head.matrix.to_quaternion()@rest.inverted())@Vector((0,-1,0))
 return math.degrees(math.asin(max(-1,min(1,forward.z))))
original=[];angles=[]
for f in range(1,26):
 scene.frame_set(f);bpy.context.view_layer.update();original.append(head.rotation_quaternion.copy());angles.append(gaze())
target_mean=-6.0
lift=target_mean-sum(angles[:24])/24
previous=None
for f,q in enumerate(original,1):
 # The head bone's local X is the anatomical right axis. A constant correction
 # retains the imported nod, yaw and roll instead of aiming at a fixed target.
 q=q@Quaternion((1,0,0),-math.radians(lift))
 if previous is not None and previous.dot(q)<0:q.negate()
 previous=q.copy();head.rotation_quaternion=q;head.keyframe_insert('rotation_quaternion',frame=f,group='Head')
for fc in action.fcurves:
 if fc.data_path==path:
  for k in fc.keyframe_points:k.interpolation='LINEAR'
assert before==protected_digest(),'A channel outside the Head rotation changed'
revised=[]
for f in range(1,26):
 scene.frame_set(f);bpy.context.view_layer.update();revised.append(gaze())
assert abs(sum(revised[:24])/24-target_mean)<1
def summary(xs):return {'min':min(xs[:24]),'max':max(xs[:24]),'mean':sum(xs[:24])/24}
stats=json.loads((BASE/'RetargetCheck.json').read_text())
stats.update({'head_gaze_adjusted':True,'head_pitch_lift_degrees':lift,'gaze_elevation_before':summary(angles),'gaze_elevation_after':summary(revised),'changed_channels':[path],'all_other_animation_channels_unchanged':True,'geometry_and_weights_unchanged':True,'approval':'Running gait approved; head gaze correction pending'})
(OUT/'RetargetCheck.json').write_text(json.dumps(stats,indent=2))
rig['review_status']='Accepted gait; head-only forward gaze correction pending approval'
scene.frame_start=1;scene.frame_end=24;scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CompactSurvivor_RunGaze.blend'))
print('GAZE_REVISION_READY',json.dumps({'before':summary(angles),'after':summary(revised),'lift':lift}),flush=True)
