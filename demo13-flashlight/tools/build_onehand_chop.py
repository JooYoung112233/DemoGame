"""Add a planted, weighty axe chop, preserving the five approved character clips.
The deferred moving-attack experiment is archived outside Unity's Assets folder.
"""
import bpy, math, json, hashlib, shutil
from pathlib import Path
from mathutils import Vector, Euler

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ChibiSurvivor/Player'
SOURCE=OUT/'CompactSurvivor_Combat.blend'
if (OUT/'AttackSet.json').exists():
    raise RuntimeError('The current character uses two-handed slash/chop. Use build_twohand_attacks.py.')
ARCHIVE=ROOT/'ArtSource/CharacterArchive/2026-09-08/DeferredMovingAttack'
ARCHIVE.mkdir(parents=True,exist_ok=True)
for ext in ('.blend','.fbx','.glb'):
    src=SOURCE.with_suffix(ext);dst=ARCHIVE/src.name
    if src.exists() and not dst.exists():shutil.copy2(src,dst)
# No recursive moves. Verify every source and destination before moving extras.
for name in ('MovementAttackLayer.json','MovementAttackLayer.json.meta',
             'CompactSurvivor_Combat.blend1','CompactSurvivor_Combat.blend1.meta'):
    src=(OUT/name).resolve();dst=(ARCHIVE/name).resolve()
    assert src.parent==OUT.resolve() and dst.parent==ARCHIVE.resolve()
    if src.exists() and not dst.exists():shutil.move(str(src),str(dst))
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];arm=rig.data
meshes=[o for o in scene.objects if o.type=='MESH']
for name in ('Attack_OneHand_Thrust_Upper','Attack_OneHand_Chop'):
    if name in bpy.data.actions:bpy.data.actions.remove(bpy.data.actions[name])
for name in ('Attack_UpperBody','Locomotion_LowerBody'):
    if name in arm.collections:arm.collections.remove(arm.collections[name])
original=list(bpy.data.actions)
assert {a.name for a in original}=={'Idle','Walk','Run','Attack_OneHand','Attack_OneHand_Thrust'}
assert len(meshes)==13 and len(arm.bones)==23
def signature():
    data=[(a.name,[(f.data_path,f.array_index,[(tuple(k.co),k.interpolation) for k in f.keyframe_points]) for f in a.fcurves]) for a in original]
    return hashlib.sha256(repr(data).encode()).hexdigest()
def shapes():
    return hashlib.sha256(repr([(o.name,[tuple(v.co) for v in o.data.vertices],
        [tuple(p.vertices) for p in o.data.polygons],
        [[(g.group,g.weight) for g in v.groups] for v in o.data.vertices]) for o in meshes]).encode()).hexdigest()
old_motion=signature();old_shape=shapes()
rest={b.name:b.matrix_local.copy() for b in arm.bones}
parents={b.name:b.parent.name if b.parent else None for b in arm.bones}
def use(action):
    rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
use(bpy.data.actions['Idle']);scene.frame_set(1);bpy.context.view_layer.update()
idle={b.name:b.matrix.copy() for b in rig.pose.bones}
local={n:idle[parents[n]].inverted()@m if parents[n] else m.copy() for n,m in idle.items()}
def V(v):return Vector(v)
def R(x=0,y=0,z=0):return Euler(tuple(math.radians(v) for v in (x,y,z)),'XYZ').to_matrix()
def T(p,r):
    m=r.to_4x4();m.translation=p;return m
def spline(frame,keys,tangents=None):
    if frame<=keys[0][0]:return keys[0][1].copy()
    if frame>=keys[-1][0]:return keys[-1][1].copy()
    for i,((f0,p0),(f1,p1)) in enumerate(zip(keys,keys[1:])):
        if frame>f1:continue
        def tangent(j):
            if tangents is not None:return tangents[j]
            if j in (0,len(keys)-1):return keys[j][1]*0
            return (keys[j+1][1]-keys[j-1][1])/(keys[j+1][0]-keys[j-1][0])
        h=f1-f0;t=(frame-f0)/h
        return (2*t**3-3*t*t+1)*p0+(t**3-2*t*t+t)*h*tangent(i)+(-2*t**3+3*t*t)*p1+(t**3-t*t)*h*tangent(i+1)
def curve(frame,keys):return spline(frame,[(f,V((v,0,0))) for f,v in keys]).x
def follow(name,d,rotation=None):
    m=d[parents[name]]@local[name]
    if rotation is not None:m=T(m.translation,rotation@m.to_3x3())
    d[name]=m;return m
def segment(name,a,b):
    q=(idle[name].to_3x3()@V((0,1,0))).rotation_difference((b-a).normalized())
    return T(a,q.to_matrix()@idle[name].to_3x3())
def solve(first,second,origin,target,pole):
    l1,l2=arm.bones[first].length,arm.bones[second].length
    axis=target-origin;distance=axis.length;axis.normalize()
    limit=(l1+l2)*.995;clamp=max(0,distance-limit)
    if clamp:target=origin+axis*limit;distance=limit
    along=(l1*l1-l2*l2+distance*distance)/(2*distance)
    bend=pole-origin;bend=(bend-axis*bend.dot(axis)).normalized()
    elbow=origin+axis*along+bend*math.sqrt(max(0,l1*l1-along*along))
    return target,elbow,clamp

END=40;CONTACT=17
grip_rest={s:arm.bones['HandSocket.'+s].head_local.copy() for s in ('L','R')}
grip_idle={s:idle['Hand.'+s]@rest['Hand.'+s].inverted()@p for s,p in grip_rest.items()}
offset={s:grip_rest[s]-arm.bones['Hand.'+s].head_local for s in ('L','R')}
q_idle={s:(idle['Hand.'+s].to_3x3()@rest['Hand.'+s].to_3x3().inverted()).to_quaternion() for s in ('L','R')}
right_keys=[(1,grip_idle['R']),(5,V((-.371,-.060,.765))),
    (10,V((-.380,.008,1.170))),(12,V((-.390,-.085,1.145))),
    (17,V((-.284,-.359,.711))),(20,V((-.259,-.349,.634))),
    (26,V((-.318,-.204,.594))),(33,grip_idle['R']+V((-.006,-.044,.021))),(40,grip_idle['R'])]
right_tangents=[V(v) for v in [(0,0,0),(-.016,.005,.065),(-.003,-.012,.008),
    (.010,-.078,-.045),(.014,-.014,-.045),(-.004,.009,-.003),(-.001,.023,-.008),(.002,.012,-.006),(0,0,0)]]
idle_axis=q_idle['R']@V((0,-1,0))
direction_keys=[(1,idle_axis),(5,V((-.18,-.56,.81))),(10,V((-.06,.22,.97))),
    (12,V((-.08,-.15,.985))),(17,V((-.03,-.84,-.54))),(20,V((.05,-.70,-.71))),
    (26,V((.08,-.98,-.14))),(33,V((.04,-.999,.01))),(40,idle_axis)]
clamps=[]
def pose(frame):
    if frame in (1,END):return {n:m.copy() for n,m in idle.items()}
    load=curve(frame,[(1,0),(5,.55),(10,1),(15,.95),(20,.84),(28,.40),(34,.10),(40,0)])
    drive=curve(frame,[(1,0),(5,-.25),(10,-.44),(12,.02),(16,1),(19,.93),(25,.45),(33,.08),(40,0)])
    hip_delta=spline(frame,[(1,V((0,0,0))),(6,V((-.009,.006,-.011))),
        (11,V((-.013,.009,-.018))),(16,V((.009,-.033,-.047))),
        (20,V((.013,-.036,-.051))),(28,V((.001,-.009,-.018))),(40,V((0,0,0)))])
    d={'Root':idle['Root'].copy()}
    d['Hips']=T(idle['Hips'].translation+hip_delta,R(x=3*drive,z=7*drive)@idle['Hips'].to_3x3())
    follow('Spine',d,R(x=7*drive,z=8*drive))
    follow('Chest',d,R(x=5*drive,z=9*drive))
    follow('Neck',d)
    follow('Head',d,R(x=-10*drive,z=-17*drive))
    lag=curve(max(1,frame-2),[(1,0),(10,-.44),(16,1),(25,.45),(40,0)])
    follow('Backpack',d,R(x=-2.8*lag))
    for side,sign in (('L',1),('R',-1)):
        follow('Clavicle.'+side,d)
        shoulder=(d['Clavicle.'+side]@local['UpperArm.'+side]).translation
        if side=='R':
            grip=spline(frame,right_keys,right_tangents)
            direction=spline(frame,direction_keys).normalized()
            q=V((0,-1,0)).rotation_difference(direction)
            # Preserve the exact resting hand roll while the axe leaves/returns.
            blend=max(0,min(1,curve(frame,[(1,0),(5,1),(30,1),(40,0)])))
            q=q_idle[side].slerp(q,blend)
        else:
            grip=spline(frame,[(1,grip_idle[side]),(7,V((.283,-.114,.683))),
                (11,V((.300,-.128,.727))),(17,V((.343,.005,.675))),
                (21,V((.344,.009,.637))),(29,V((.304,-.050,.574))),(40,grip_idle[side])])
            q=q_idle[side].slerp(R(x=-28,y=-8).to_quaternion()@q_idle[side],max(0,min(1,load)))
        wrist=grip-q.to_matrix()@offset[side]
        pole=(d['Chest']@idle['Chest'].inverted()@idle['Forearm.'+side]).translation
        pole=pole.lerp(V((sign*.60,.075,.86 if side=='R' else .70)),max(0,min(1,load)))
        wrist,elbow,clamp=solve('UpperArm.'+side,'Forearm.'+side,shoulder,wrist,pole)
        clamps.append((frame,side,clamp))
        d['UpperArm.'+side]=segment('UpperArm.'+side,shoulder,elbow)
        d['Forearm.'+side]=segment('Forearm.'+side,elbow,wrist)
        d['Hand.'+side]=T(wrist,q.to_matrix()@rest['Hand.'+side].to_3x3())
        follow('HandSocket.'+side,d)
        joint=(d['Hips']@local['Thigh.'+side]).translation
        ankle,knee,clamp=solve('Thigh.'+side,'Shin.'+side,joint,idle['Foot.'+side].translation,
                              idle['Shin.'+side].translation+V((0,-.25,0)))
        assert clamp<1e-5,('leg',frame,side,clamp)
        d['Thigh.'+side]=segment('Thigh.'+side,joint,knee)
        d['Shin.'+side]=segment('Shin.'+side,knee,ankle)
        d['Foot.'+side]=idle['Foot.'+side].copy()
    return d

action=bpy.data.actions.new('Attack_OneHand_Chop');action.use_fake_user=True;use(action)
previous={}
for frame in range(1,END+1):
    scene.frame_set(frame);desired=pose(frame)
    for b in arm.bones:
        n=b.name;p=parents[n]
        basis=rest[n].inverted()@rest[p]@desired[p].inverted()@desired[n] if p else rest[n].inverted()@desired[n]
        loc,q,scale=basis.decompose()
        if n in previous and previous[n].dot(q)<0:q.negate()
        previous[n]=q.copy();pb=rig.pose.bones[n]
        pb.location=loc;pb.rotation_quaternion=q;pb.scale=(1,1,1)
        pb.keyframe_insert('location',frame=frame,group=n)
        pb.keyframe_insert('rotation_quaternion',frame=frame,group=n)
for fc in action.fcurves:
    for k in fc.keyframe_points:k.interpolation='LINEAR'
assert max(c[2] for c in clamps)<.008,sorted(clamps,key=lambda c:c[2])[-5:]
assert signature()==old_motion and shapes()==old_shape
for name,frame in [('Start',1),('Raised',10),('Downstroke',13),('Contact',17),('FollowThrough',20),('Recovered',40)]:
    marker=action.pose_markers.new(name);marker.frame=frame
action['fps']=30;action['loop']=False;action['in_place']=True
action['suggested_contact_frame']=17;action['weapon_hand']='R'
action['description']='Planted axe chop; shoulder lift, accelerated downswing, knee compression and delayed recovery.'
action['timing_note']='Visual contact marker only; no gameplay hit event connected.'
checks={'clip':action.name,'fps':30,'frames':[1,END],'duration_seconds':(END-1)/30,
    'suggested_contact_frame':CONTACT,'model_geometry_and_weights_unchanged':True,
    'existing_five_actions_unchanged':True,'max_arm_clamp_m':max(c[2] for c in clamps),'unity_checked':False}
foot_drift=0;sole=100;snaps={};trajectory=[]
for f in range(1,END+1):
    scene.frame_set(f);bpy.context.view_layer.update()
    for s in ('L','R'):
        foot_drift=max(foot_drift,(rig.pose.bones['Foot.'+s].matrix.translation-idle['Foot.'+s].translation).length)
    trajectory.append(list(rig.pose.bones['HandSocket.R'].matrix.translation))
    dg=bpy.context.evaluated_depsgraph_get();coords=[]
    for o in meshes:
        ev=o.evaluated_get(dg);mesh=ev.to_mesh()
        if o.name=='Compact_Boots':sole=min(sole,min(v.co.z for v in mesh.vertices))
        if f in (1,CONTACT,END):coords.extend(v.co.copy() for v in mesh.vertices)
        ev.to_mesh_clear()
    if coords:snaps[f]=coords
checks.update(max_foot_drift_m=foot_drift,min_sole_z_m=sole,
    return_pose_error_m=max((a-b).length for a,b in zip(snaps[1],snaps[END])),right_hand_trajectory=trajectory)
assert foot_drift<1e-5 and sole>-.00001 and checks['return_pose_error_m']<1e-5
(OUT/'ChopAnimationCheck.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
mapping={'attack_mode':'stationary_full_body','user_decision_date':'2026-09-08',
    'sword':'Attack_OneHand','dagger':'Attack_OneHand_Thrust','axe':'Attack_OneHand_Chop',
    'moving_attack_deferred':True,'unity_connected':False}
(OUT/'OneHandAttackSet.json').write_text(json.dumps(mapping,indent=2),encoding='utf-8')
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
rig['stage']='Idle/Walk/Run + stationary sword slash, dagger thrust, axe chop. Unity connection pending.'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
print('ONEHAND_CHOP_READY',json.dumps({k:v for k,v in checks.items() if k!='right_hand_trajectory'}),flush=True)
