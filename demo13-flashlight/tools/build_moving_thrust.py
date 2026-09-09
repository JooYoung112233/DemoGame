"""Create a dynamic upper-body override from the approved flowing thrust.
Root, Hips and both legs remain exclusively owned by locomotion. No Unity edits.
"""
import bpy,math,json,hashlib,shutil
from pathlib import Path
from mathutils import Vector,Matrix,Euler
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ChibiSurvivor/Player'
SOURCE=OUT/'CompactSurvivor_Combat.blend'
if (OUT/'OneHandAttackSet.json').exists() or (OUT/'AttackSet.json').exists():
    raise RuntimeError('Moving attack was deferred by the user; keep the stationary three-attack set. This script is an archived experiment.')
BACKUP=ROOT/'ArtSource/CharacterArchive/2026-09-08/PlayerBeforeMovingAttack'
BACKUP.mkdir(parents=True,exist_ok=True)
for ext in ['.blend','.fbx','.glb']:
    src=SOURCE.with_suffix(ext);dst=BACKUP/src.name
    if src.exists() and not dst.exists():shutil.copy2(src,dst)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];arm=rig.data
meshes=[o for o in scene.objects if o.type=='MESH']
NAME='Attack_OneHand_Thrust_Upper'
if NAME in bpy.data.actions:bpy.data.actions.remove(bpy.data.actions[NAME])
original=list(bpy.data.actions)
def motion_signature():
    v=[(a.name,[(f.data_path,f.array_index,[(tuple(k.co),k.interpolation) for k in f.keyframe_points]) for f in a.fcurves]) for a in sorted(original,key=lambda a:a.name)]
    return hashlib.sha256(repr(v).encode()).hexdigest()
def geometry_signature():
    v=[(o.name,[tuple(v.co) for v in o.data.vertices],[[tuple((g.group,g.weight)) for g in v.groups] for v in o.data.vertices]) for o in sorted(meshes,key=lambda o:o.name)]
    return hashlib.sha256(repr(v).encode()).hexdigest()
old_motion=motion_signature();old_geometry=geometry_signature()
rest={b.name:b.matrix_local.copy() for b in arm.bones}
parents={b.name:b.parent.name if b.parent else None for b in arm.bones}
lower={'Root','Hips','Thigh.L','Shin.L','Foot.L','Thigh.R','Shin.R','Foot.R'}
upper=[b.name for b in arm.bones if b.name not in lower]
assert len(upper)==15 and len(meshes)==13
def use(action):
    rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
def sample(action,frame):
    use(action);scene.frame_set(frame);bpy.context.view_layer.update()
    return {b.name:b.matrix.copy() for b in rig.pose.bones}
idle=sample(bpy.data.actions['Idle'],1)
source=[sample(bpy.data.actions['Attack_OneHand_Thrust'],f) for f in range(1,29)]
def V(v):return Vector(v)
def R(x=0,y=0,z=0):return Euler(tuple(math.radians(v) for v in (x,y,z)),'XYZ').to_matrix()
def T(p,r):
    m=r.to_4x4();m.translation=p;return m
def amplify(reference,rotation,amount):
    q0=reference.to_quaternion();delta=q0.inverted() @ rotation.to_quaternion()
    if delta.w<0:delta.negate()
    axis,angle=delta.to_axis_angle()
    from mathutils import Quaternion
    return (q0 @ Quaternion(axis,angle*amount)).to_matrix()
def pulse(frame):
    # A single smooth impulse; no held intermediate pose.
    t=(frame-1)/27
    return math.sin(math.pi*t)**1.5*math.exp(-1.3*max(0,t-.37))
def local(pose,name):
    p=parents[name];return pose[p].inverted() @ pose[name] if p else pose[name]
def follow(name,d,local_pose):
    d[name]=d[parents[name]] @ local(local_pose,name)
    return d[name]
clamps=[]
def limb(first,second,head,wrist,pole,base):
    l1,l2=arm.bones[first].length,arm.bones[second].length
    axis=wrist-head;dist=axis.length;axis.normalize()
    correction=max(0,dist-(l1+l2)*.99)
    if correction:wrist=head+axis*(l1+l2)*.99;dist=(l1+l2)*.99
    along=(l1*l1-l2*l2+dist*dist)/(2*dist)
    bend=pole-head;bend=(bend-axis*bend.dot(axis)).normalized()
    elbow=head+axis*along+bend*math.sqrt(max(0,l1*l1-along*along))
    def segment(name,a,b):
        q=(base[name].to_3x3() @ V((0,1,0))).rotation_difference((b-a).normalized())
        return T(a,q.to_matrix() @ base[name].to_3x3())
    return segment(first,head,elbow),segment(second,elbow,wrist),wrist,correction

layer=bpy.data.actions.new(NAME);layer.use_fake_user=True;use(layer)
previous={}
for frame,a in enumerate(source,1):
    d={n:m.copy() for n,m in idle.items()}
    p=pulse(frame)
    if frame not in (1,28):
        # Transfer the stationary attack's hip turn to Spine, without pelvis movement.
        spine_pos=(idle['Hips'] @ local(idle,'Spine')).translation
        d['Spine']=T(spine_pos,amplify(idle['Spine'].to_3x3(),a['Spine'].to_3x3(),1.32))
        chest_local=local(a,'Chest')
        chest_local=T(chest_local.translation,amplify(local(idle,'Chest').to_3x3(),chest_local.to_3x3(),1.25))
        d['Chest']=d['Spine'] @ chest_local
        follow('Neck',d,a)
        head_pos=(d['Neck'] @ local(a,'Head')).translation
        d['Head']=T(head_pos,a['Head'].to_3x3())
        follow('Backpack',d,a)
        shift=d['Chest'].translation-a['Chest'].translation
        for side,sign in [('L',1),('R',-1)]:
            follow('Clavicle.'+side,d,a)
            shoulder=(d['Clavicle.'+side] @ local(a,'UpperArm.'+side)).translation
            wrist=a['Hand.'+side].translation+shift
            if side=='R':wrist+=V((-.006,-.020,.005))*p
            else:wrist+=V((.025,.105,-.022))*p
            pole=a['Forearm.'+side].translation+shift+V((sign*.035,.010,0))*p
            first,second,wrist,correction=limb('UpperArm.'+side,'Forearm.'+side,shoulder,wrist,pole,a)
            clamps.append(correction)
            d['UpperArm.'+side]=first;d['Forearm.'+side]=second
            d['Hand.'+side]=T(wrist,a['Hand.'+side].to_3x3())
            follow('HandSocket.'+side,d,a)
    scene.frame_set(frame)
    for n in upper:
        parent=parents[n]
        basis=rest[n].inverted() @ rest[parent] @ d[parent].inverted() @ d[n]
        loc,q,scale=basis.decompose()
        if n in previous and previous[n].dot(q)<0:q.negate()
        previous[n]=q.copy();pb=rig.pose.bones[n]
        pb.location=loc;pb.rotation_quaternion=q;pb.scale=(1,1,1)
        pb.keyframe_insert('location',frame=frame,group=n)
        pb.keyframe_insert('rotation_quaternion',frame=frame,group=n)
for fc in layer.fcurves:
    for k in fc.keyframe_points:k.interpolation='LINEAR'
animated={fc.data_path.split('"')[1] for fc in layer.fcurves}
assert animated==set(upper) and not animated.intersection(lower)
assert max(clamps)<.004, max(clamps)
assert motion_signature()==old_motion and geometry_signature()==old_geometry
layer['layer_mode']='Override with UpperBody mask; keep base locomotion playing'
layer['fps']=30;layer['duration_seconds']=.9;layer['contact_frame']=11
layer['excluded_bones']='Root,Hips,Thigh.L,Shin.L,Foot.L,Thigh.R,Shin.R,Foot.R'
layer['blend_in_frames']=3;layer['blend_out_frames']=8
layer['in_place']=True;layer['unity_connected']=False
for name,frame in [('Start',1),('Drive',7),('Contact',11),('Recover',17),('End',28)]:
    marker=layer.pose_markers.new(name);marker.frame=frame
for label,names in [('Attack_UpperBody',upper),('Locomotion_LowerBody',sorted(lower))]:
    collection=arm.collections.get(label) or arm.collections.new(label)
    for name in names:collection.assign(arm.bones[name])
def bone_path(name):
    chain=[name];parent=parents[name]
    while parent:chain.insert(0,parent);parent=parents[parent]
    return '/'.join(chain)
report={'clip':NAME,'frames':[1,28],'fps':30,'contact_frame':11,'layer_mode':'Override',
    'mask_root':'Spine','included_bones':upper,'excluded_bones':sorted(lower),
    'bone_paths':{n:bone_path(n) for n in upper},'blend_in_seconds':.1,'blend_out_seconds':8/30,
    'base_locomotion_must_continue':True,'restart_locomotion_on_attack':False,
    'model_geometry_unchanged':True,'existing_five_actions_unchanged':True,'max_arm_clamp_m':max(clamps),
    'export_note':'Apply an upper-body transform mask in Unity even if FBX baking adds excluded constant tracks.',
    'unity_connected':False,'game_movement_lock_change_pending':True}
(OUT/'MovementAttackLayer.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
use(bpy.data.actions['Idle']);scene.frame_set(1);scene.frame_start=1;scene.frame_end=91;scene.render.fps=30
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'CompactSurvivor_Combat.fbx'),use_selection=True,
    object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
    bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0,mesh_smooth_type='FACE',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS')
bpy.ops.export_scene.gltf(filepath=str(OUT/'CompactSurvivor_Combat.glb'),use_selection=True,
    export_format='GLB',export_animations=True,export_animation_mode='ACTIONS')
use(bpy.data.actions['Idle']);scene.frame_set(1)
rig['stage']='Six clips; upper-body attack for continuous locomotion. Unity layer connection pending.'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
print('MOVING_ATTACK_LAYER_READY',json.dumps(report),flush=True)
