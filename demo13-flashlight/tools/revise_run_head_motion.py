"""Change only Run's Head rotation, preserving the entire gait for a clear A/B review."""
import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Euler
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Assets/ChibiSurvivor/Player/RunReview/CompactSurvivor_RunReview_v3.blend'
OUT=SOURCE.parent/'HeadMotionReview';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];action=bpy.data.actions['Run']
rig.animation_data.action=action
if action.slots:rig.animation_data.action_slot=action.slots[0]
head=rig.pose.bones['Head'];rest=rig.data.bones['Head'].matrix_local.copy();p=head.parent.name
parent_rest=rig.data.bones[p].matrix_local.copy();chest_rest=rig.data.bones['Chest'].matrix_local.copy()
PATH='pose.bones["Head"].rotation_quaternion';COUNT=24;FPS=30;LAG=.075
def digest_unchanged():
    return hashlib.sha256(repr([(a.name,[(fc.data_path,fc.array_index,[(tuple(k.co),k.interpolation) for k in fc.keyframe_points]) for fc in a.fcurves if not (a==action and fc.data_path==PATH)]) for a in bpy.data.actions]).encode()).hexdigest()
before=digest_unchanged();original=[];samples=[]
def set_time(frame):
    base=math.floor(frame);scene.frame_set(base,subframe=frame-base);bpy.context.view_layer.update()
for f in range(1,COUNT+2):
    set_time(1+((f-1-LAG*FPS)%COUNT))
    delayed=(rig.pose.bones['Chest'].matrix.to_3x3()@chest_rest.to_3x3().inverted()).to_euler('XYZ')
    set_time(f)
    current=(head.matrix.to_3x3()@rest.to_3x3().inverted()).to_euler('XYZ')
    original.append(tuple(math.degrees(v) for v in current))
    samples.append((delayed.copy(),head.parent.matrix.copy(),head.matrix.translation.copy()))
previous=None
for f,(delayed,pm,location) in enumerate(samples,1):
    t=math.tau*(f-1)/COUNT
    # Delayed torso rotation plus a small late nod after landing. Keep the joint
    # position attached to the unchanged neck; no floating-head translation.
    desired=Euler((delayed.x-math.radians(11)+math.radians(4.5)*math.sin(2*t-.70),
                   delayed.y+math.radians(1.8)*math.sin(t-.50),
                   delayed.z+math.radians(2.0)*math.sin(t-.35)),'XYZ')
    world=(desired.to_matrix()@rest.to_3x3()).to_4x4();world.translation=location
    basis=rest.inverted()@parent_rest@pm.inverted()@world;q=basis.to_quaternion()
    if previous is not None and previous.dot(q)<0:q.negate()
    previous=q.copy();head.rotation_quaternion=q
    head.keyframe_insert('rotation_quaternion',frame=f,group='Head')
for fc in action.fcurves:
    if fc.data_path==PATH:
        for k in fc.keyframe_points:k.interpolation='LINEAR'
assert before==digest_unchanged(),'A channel outside the Head rotation changed'
revised=[];snapshots={}
for f in range(1,COUNT+2):
    set_time(f);e=(head.matrix.to_3x3()@rest.to_3x3().inverted()).to_euler('XYZ')
    revised.append(tuple(math.degrees(v) for v in e))
    if f in [1,COUNT+1]:
        dg=bpy.context.evaluated_depsgraph_get();coords=[]
        for o in scene.objects:
            if o.type!='MESH':continue
            ev=o.evaluated_get(dg);mesh=ev.to_mesh();coords.extend(v.co.copy() for v in mesh.vertices);ev.to_mesh_clear()
        snapshots[f]=coords
seam=max((a-b).length for a,b in zip(snapshots[1],snapshots[COUNT+1]));assert seam<1e-5,seam
def ranges(values):return {name:round(max(v[i] for v in values)-min(v[i] for v in values),3) for i,name in enumerate(['pitch_degrees','roll_degrees','yaw_degrees'])}
report={'changed':'Run: Head rotation only','other_actions_and_all_other_run_channels_unchanged':True,
 'head_joint_translation_unchanged':True,'source':str(SOURCE),'lag_seconds':LAG,'loop_seam_m':seam,
 'before_ranges':ranges(original),'after_ranges':ranges(revised),'frames':[1,25],'fps':FPS,
 'nominal_forward_speed_mps':float(action['nominal_forward_speed_mps']),'approval':'pending','unity_connected':False}
action['head_motion_review']='Delayed torso follow and landing nod; head-rotation-only A/B candidate'
scene.frame_start=1;scene.frame_end=25;scene.frame_set(1)
stage=ROOT/'Library/CodexBlender/RunHeadCandidate.blend';bpy.ops.wm.save_as_mainfile(filepath=str(stage));stage.replace(OUT/'CompactSurvivor_RunHeadReview.blend')
(OUT/'HeadMotionCheck.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('HEAD_MOTION_READY',json.dumps(report),flush=True)
