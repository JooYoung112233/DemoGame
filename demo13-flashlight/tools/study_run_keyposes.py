"""Three explicitly authored reference poses for approval, not a finished run clip."""
import bpy,math,ast,json
from pathlib import Path
from mathutils import Vector,Matrix,Euler
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ChibiSurvivor/Player/RunReview/KeyPoseStudy';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/ChibiSurvivor/Player/WalkReview/CompactSurvivor_WalkReview.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];arm=rig.data
rest={b.name:b.matrix_local.copy() for b in arm.bones};parent={b.name:b.parent.name if b.parent else None for b in arm.bones}
tree=ast.parse((ROOT/'tools/rig_compact_survivor.py').read_text(encoding='utf-8'))
names={'V','radians','transform','world_rot','follow','segment'}
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in names],type_ignores=[]),'<pose math>','exec'))
boot=bpy.data.objects['Compact_Boots']
points={s:[v.co.copy()-arm.bones['Foot.'+s].head_local for v in boot.data.vertices if (v.co.x>=0)==(s=='L')] for s in ['L','R']}
# Angles are authored in the sagittal plane, from downward vertical.
# Each entry: reference frame index; thigh, knee flexion, shoe pitch per leg;
# upper arm angle and elbow bend per arm. The original reference has asymmetric arms.
poses=[
 {'reference':13,'label':'Landing','legs':{'L':(-10,16,0),'R':(10,88,80)},'arms':{'L':(-12,85),'R':(23,30)},'floor':0},
 {'reference':18,'label':'Push-off / initial flight','legs':{'L':(-38,40,0),'R':(36,20,62)},'arms':{'L':(22,34),'R':(-16,86)},'floor':.025},
 {'reference':28,'label':'Flight reach','legs':{'L':(-40,25,-12),'R':(30,55,76)},'arms':{'L':(22,34),'R':(-16,86)},'floor':.055},
]
def direction(angle,x=0):
    a=radians(angle);return V((x,math.sin(a),-math.cos(a))).normalized()
def make_pose(cfg):
    d={'Root':rest['Root'].copy(),'Hips':transform(V((0,-.04,.53)),world_rot(x=20)@rest['Hips'].to_3x3())}
    follow('Spine',d,world_rot(x=4));follow('Chest',d);follow('Neck',d)
    follow('Head',d,world_rot(x=-24));follow('Backpack',d)
    for s,sign in [('L',1),('R',-1)]:
        follow('Clavicle.'+s,d)
        shoulder=(d['Clavicle.'+s]@rest['Clavicle.'+s].inverted()@rest['UpperArm.'+s]).translation
        angle,bend=cfg['arms'][s]
        elbow=shoulder+direction(angle,sign*.25)*arm.bones['UpperArm.'+s].length
        wrist=elbow+direction(angle-bend,sign*.15)*arm.bones['Forearm.'+s].length
        d['UpperArm.'+s]=segment('UpperArm.'+s,shoulder,elbow);d['Forearm.'+s]=segment('Forearm.'+s,elbow,wrist)
        follow('Hand.'+s,d);follow('HandSocket.'+s,d)
        hip=(d['Hips']@rest['Hips'].inverted()@rest['Thigh.'+s]).translation
        thigh,bend,pitch=cfg['legs'][s]
        knee=hip+direction(thigh)*arm.bones['Thigh.'+s].length
        ankle=knee+direction(thigh+bend)*arm.bones['Shin.'+s].length
        d['Thigh.'+s]=segment('Thigh.'+s,hip,knee);d['Shin.'+s]=segment('Shin.'+s,knee,ankle)
        d['Foot.'+s]=transform(ankle,world_rot(x=pitch)@rest['Foot.'+s].to_3x3())
    low=min((d['Foot.'+s]@rest['Foot.'+s].inverted()@v.co).z for s in ['L','R'] for v in boot.data.vertices if (v.co.x>=0)==(s=='L'))
    for n in d:
        if n!='Root':d[n].translation.z+=cfg['floor']-low
    return d
action=bpy.data.actions.new('Run_ThreePoseStudy_NOT_ANIMATION');action.use_fake_user=True;rig.animation_data.action=action
for frame,cfg in enumerate(poses,1):
    d=make_pose(cfg)
    for b in arm.bones:
        n=b.name;p=parent[n];basis=rest[n].inverted()@rest[p]@d[p].inverted()@d[n] if p else rest[n].inverted()@d[n]
        loc,q,scale=basis.decompose();pb=rig.pose.bones[n];pb.location=loc;pb.rotation_quaternion=q;pb.scale=(1,1,1)
        pb.keyframe_insert('location',frame=frame,group=n);pb.keyframe_insert('rotation_quaternion',frame=frame,group=n)
    mark=action.pose_markers.new(cfg['label']);mark.frame=frame
for fc in action.fcurves:
    for k in fc.keyframe_points:k.interpolation='CONSTANT'
if action.slots:rig.animation_data.action_slot=action.slots[0]
action['stage']='Three static reference poses, not a playable run cycle; approval pending'
scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=32
scene.camera.location=(-6.7,-4.5,1.8);target=V((0,0,.8));scene.camera.rotation_euler=(target-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=1.98
scene.render.resolution_x=512;scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.frame_start=1;scene.frame_end=3;scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RunKeyPoseStudy.blend'))
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA';scene.render.film_transparent=True
for f in range(1,4):
    scene.frame_set(f);scene.render.filepath=str(OUT/f'Pose_{f}.png');bpy.ops.render.render(write_still=True)
(OUT/'PoseStudy.json').write_text(json.dumps({'status':'static poses for review, not an animation','poses':poses,'existing_walk_and_run_files_unchanged':True},indent=2),encoding='utf-8')
print('KEY_POSES_READY',flush=True)
