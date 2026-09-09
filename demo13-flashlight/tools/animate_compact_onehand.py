"""Add one editable in-place knife slash to the approved round-hand survivor.
Only animation data is changed. The knife lives in a separate preview collection.
Run in Blender 4.5 background; outputs can be regenerated without touching source.
"""
import bpy, math, json, hashlib, sys
from pathlib import Path
from mathutils import Vector, Matrix, Euler

ROOT = Path(__file__).resolve().parents[1]
ARCHIVE = ROOT/'ArtSource/CharacterArchive/2026-09-08'
BASE = ARCHIVE/'Assets/ChibiSurvivor/CompactSurvivor/Animated'
SOURCE = BASE/'RoundHands/CompactSurvivor_Animated.blend'
OUT = BASE/'Combat'
FLUID = '--fluid' in sys.argv
THRUST = '--stab' in sys.argv or FLUID
if THRUST:
    SOURCE = OUT/'CompactSurvivor_Combat.blend'
    OUT = OUT/'Thrust'
    if FLUID: OUT = ROOT/'Assets/ChibiSurvivor/Player'
    # Rebuilding an earlier stage must not replace the later three-attack set.
    if FLUID and ((OUT/'OneHandAttackSet.json').exists() or (OUT/'AttackSet.json').exists()):
        OUT = BASE/'Combat/Thrust/Fluid'
ACTION = 'Attack_OneHand_Thrust' if THRUST else 'Attack_OneHand'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
rig = bpy.data.objects['CompactSurvivor_Rig']
arm = rig.data
meshes = [o for o in scene.objects if o.type == 'MESH']
assert len(meshes) == 13 and len(arm.bones) == 23

def signature(actions):
    data = [(a.name, [(fc.data_path, fc.array_index,
             [(tuple(k.co), k.interpolation) for k in fc.keyframe_points])
             for fc in a.fcurves]) for a in sorted(actions, key=lambda a: a.name)]
    return hashlib.sha256(repr(data).encode()).hexdigest()

def shape_signature(obj):
    data = ([tuple(v.co) for v in obj.data.vertices],
            [tuple(p.vertices) for p in obj.data.polygons],
            [[(g.group, g.weight) for g in v.groups] for v in obj.data.vertices])
    return hashlib.sha256(repr(data).encode()).hexdigest()

old_actions = list(bpy.data.actions)
assert {a.name for a in old_actions} == ({'Idle','Walk','Run','Attack_OneHand'} if THRUST else {'Idle','Walk','Run'})
old_signature = signature(old_actions)
shapes = {o.name: shape_signature(o) for o in meshes}
rest = {b.name: b.matrix_local.copy() for b in arm.bones}
parents = {b.name: b.parent.name if b.parent else None for b in arm.bones}

def use_action(action):
    rig.animation_data.action = action
    if action.slots:
        rig.animation_data.action_slot = action.slots[0]

use_action(bpy.data.actions['Idle'])
scene.frame_set(1)
bpy.context.view_layer.update()
idle = {b.name: b.matrix.copy() for b in rig.pose.bones}
idle_local = {n: idle[parents[n]].inverted() @ m if parents[n] else m.copy()
              for n, m in idle.items()}

def V(v): return Vector(v)
def R(x=0, y=0, z=0):
    return Euler(tuple(math.radians(v) for v in (x, y, z)), 'XYZ').to_matrix()
def T(p, r):
    m = r.to_4x4(); m.translation = p
    return m
def ease(t):
    t = min(1, max(0, t))
    return t*t*(3-2*t)
def mix(a, b, t): return a*(1-t)+b*t

grip_rest = {s: arm.bones['HandSocket.'+s].head_local.copy() for s in ['L', 'R']}
grip_idle = {s: idle['Hand.'+s] @ rest['Hand.'+s].inverted() @ p for s,p in grip_rest.items()}
grip_offset = {s: grip_rest[s]-arm.bones['Hand.'+s].head_local for s in ['L', 'R']}
hand_idle_q = {s: (idle['Hand.'+s].to_3x3() @ rest['Hand.'+s].to_3x3().inverted()).to_quaternion()
               for s in ['L', 'R']}

# Explicit authored phases at 30 fps: anticipation / slash / follow-through / recover.
WINDUP = 7 if FLUID else 9 if THRUST else 11
FOLLOW = 11 if FLUID else 14 if THRUST else 18
END = 28 if FLUID else 34 if THRUST else 40
CONTACT = 11 if FLUID else 14 if THRUST else 15
wind_grip = V((-.400, -.080, .930))
follow_grip = V((.080, -.270, .665))
wind_q = V((0, -1, 0)).rotation_difference(V((-.40, -.10, .91)).normalized())
follow_q = V((0, -1, 0)).rotation_difference(V((.78, -.55, -.30)).normalized())
if THRUST:
    wind_grip = V((-.245,-.155,.760))
    follow_grip = V((-.215,-.435,.800))
    wind_q = V((0,-1,0)).rotation_difference(V((0,-1,.05)).normalized())
    follow_q = wind_q.copy()

def spline(frame, keys, tangents=None):
    """C1 Hermite curve: pass through interior poses without ease-to-stop segments.
    Tangents are per frame; automatic tangents use the neighboring timed values.
    """
    if frame <= keys[0][0]: return keys[0][1].copy()
    if frame >= keys[-1][0]: return keys[-1][1].copy()
    for i in range(len(keys)-1):
        f0,p0 = keys[i]; f1,p1 = keys[i+1]
        if frame > f1: continue
        def tangent(j):
            if tangents is not None: return tangents[j]
            if j in (0,len(keys)-1): return keys[j][1]*0
            return (keys[j+1][1]-keys[j-1][1])/(keys[j+1][0]-keys[j-1][0])
        h=f1-f0; t=(frame-f0)/h
        return ((2*t**3-3*t*t+1)*p0+(t**3-2*t*t+t)*h*tangent(i)
                +(-2*t**3+3*t*t)*p1+(t**3-t*t)*h*tangent(i+1))

def scalar_curve(frame, keys):
    return spline(frame,[(f,V((v,0,0))) for f,v in keys]).x

FLOW_HAND_KEYS = [(1,grip_idle['R']), (4,V((-.308,-.100,.647))),
                 (7,V((-.262,-.215,.758))), (11,V((-.215,-.422,.785))),
                 (14,V((-.252,-.319,.721))), (18,V((-.306,-.174,.606))),
                 (23,grip_idle['R']+V((-.002,-.020,.014))), (28,grip_idle['R'])]
FLOW_HAND_TANGENTS = [V(v) for v in [(0,0,0),(.009,-.025,.042),(.013,-.067,.025),
    (-.002,0,-.008),(-.015,.054,-.030),(-.005,.028,-.020),(0,.009,-.006),(0,0,0)]]

def flow_load(frame):
    return scalar_curve(frame,[(1,0),(4,.50),(7,1),(12,.90),(18,.40),(23,.08),(28,0)])

def flow_drive(frame):
    # Pelvis/chest accelerate before the hand; recover while the arm is still out.
    return scalar_curve(frame,[(1,0),(3,-.25),(5,-.18),(8,.85),(9,1),
                               (13,.52),(18,.08),(23,-.02),(28,0)])

def authored_fluid(frame):
    orient = scalar_curve(frame,[(1,0),(4,.40),(7,1),(12,1),(16,.80),(21,.25),(28,0)])
    q = hand_idle_q['R'].slerp(wind_q,min(1,max(0,orient)))
    return flow_load(frame),flow_drive(frame),spline(frame,FLOW_HAND_KEYS,FLOW_HAND_TANGENTS),q

def authored_thrust(frame):
    if frame <= WINDUP:
        t = ease((frame-1)/(WINDUP-1))
        return (t,0,grip_idle['R'].lerp(wind_grip,t),hand_idle_q['R'].slerp(wind_q,t))
    if frame <= FOLLOW:
        t = ease((frame-WINDUP)/(FOLLOW-WINDUP))
        return (1,t,wind_grip.lerp(follow_grip,t),wind_q.copy())
    if frame <= 16:
        return (1,1,follow_grip.copy(),wind_q.copy())
    if frame <= 22:
        t = ease((frame-16)/6)
        return (1,1-t,follow_grip.lerp(wind_grip,t),wind_q.copy())
    t = ease((frame-22)/(END-22))
    clearance = V((-.035,-.055,.006))*math.sin(math.pi*t)
    return (1-t,0,wind_grip.lerp(grip_idle['R'],t)+clearance,wind_q.slerp(hand_idle_q['R'],t))

def authored(frame):
    if FLUID: return authored_fluid(frame)
    if THRUST: return authored_thrust(frame)
    if frame <= WINDUP:
        t = ease((frame-1)/(WINDUP-1))
        return (t, 0, grip_idle['R'].lerp(wind_grip, t), hand_idle_q['R'].slerp(wind_q, t))
    if frame <= FOLLOW:
        t = ease((frame-WINDUP)/(FOLLOW-WINDUP))
        # A continuous Bezier arc keeps the strike moving through its contact point.
        p0 = wind_grip; p1 = V((-.435, -.330, .960))
        p2 = V((-.115, -.395, .730)); p3 = follow_grip
        grip = (1-t)**3*p0+3*(1-t)**2*t*p1+3*(1-t)*t*t*p2+t**3*p3
        return (1, t, grip, wind_q.slerp(follow_q, t))
    t = ease((frame-FOLLOW)/(END-FOLLOW))
    # Recover around the belt pouch, keeping the blade in front of the equipment.
    clearance = V((-.040, -.075, .008))*math.sin(math.pi*t)
    return (1-t, 1, follow_grip.lerp(grip_idle['R'], t)+clearance, follow_q.slerp(hand_idle_q['R'], t))

reach_corrections = []

def follow(name, desired, rotation=None):
    p = parents[name]
    m = desired[p] @ idle_local[name] if p else idle[name].copy()
    if rotation is not None:
        m = T(m.translation, rotation @ m.to_3x3())
    desired[name] = m
    return m

def segment(name, head, tail):
    base_axis = idle[name].to_3x3() @ V((0, 1, 0))
    q = base_axis.rotation_difference((tail-head).normalized())
    return T(head, q.to_matrix() @ idle[name].to_3x3())

def solve_limb(first, second, origin, target, pole):
    l1, l2 = arm.bones[first].length, arm.bones[second].length
    axis = target-origin
    d = axis.length
    limit = (l1+l2)*.995
    correction = max(0, d-limit)
    if correction:
        target = origin+axis.normalized()*limit
        d = limit
    axis.normalize()
    along = (l1*l1-l2*l2+d*d)/(2*d)
    height = math.sqrt(max(0, l1*l1-along*along))
    bend = pole-origin
    bend = (bend-axis*bend.dot(axis)).normalized()
    mid = origin+axis*along+bend*height
    return target, mid, correction

def pose(frame):
    if frame in (1, END):
        return {n:m.copy() for n,m in idle.items()}
    weight, sweep, right_grip, right_q = authored(frame)
    twist = mix(-1.0, 1.0, sweep)*weight
    d = {'Root': idle['Root'].copy()}
    hip = idle['Hips'].translation+V((.005*twist, -.012*weight if THRUST else -.008*weight, -.022*weight))
    d['Hips'] = T(hip, R(x=1.5*weight,z=(3 if THRUST else 7)*twist) @ idle['Hips'].to_3x3())
    follow('Spine', d, R(x=(1+5*sweep)*weight if THRUST else 2*weight,z=(3 if THRUST else 10)*twist))
    follow('Chest', d, R(x=(1+3*sweep)*weight if THRUST else 2*weight,z=(5 if THRUST else 15)*twist))
    follow('Neck', d)
    follow('Head', d, R(x=-(2+5*sweep)*weight if THRUST else -4*weight,z=(-8 if THRUST else -24)*twist))
    follow('Backpack', d, R(x=-1.5*weight))
    if FLUID:
        hip_delta = spline(frame,[(1,V((0,0,0))), (3,V((-.004,.006,-.004))),
            (6,V((-.006,-.004,-.017))), (9,V((.003,-.020,-.026))),
            (13,V((.006,-.011,-.021))), (18,V((.001,-.001,-.008))),
            (23,V((0,.001,-.001))), (28,V((0,0,0)))])
        drive=flow_drive(frame)
        d['Hips']=T(idle['Hips'].translation+hip_delta,R(x=2*drive,z=4*drive-2*weight) @ idle['Hips'].to_3x3())
        follow('Spine',d,R(x=5*drive,z=4*drive-2*weight))
        follow('Chest',d,R(x=3.5*drive,z=6*drive-2*weight))
        follow('Neck',d)
        follow('Head',d,R(x=-6*drive,z=-8*drive+4*weight))
        follow('Backpack',d,R(x=-1.6*flow_drive(max(1,frame-2))))

    for side, sign in [('L',1), ('R',-1)]:
        follow('Clavicle.'+side, d)
        shoulder = (d['Clavicle.'+side] @ idle_local['UpperArm.'+side]).translation
        if side == 'R':
            grip = right_grip; q = right_q
        else:
            guard = V((mix(.275,.315,sweep), mix(-.205,-.150,sweep), .725))
            if THRUST: guard = V((mix(.255,.265,sweep),mix(-.145,-.170,sweep),.675))
            grip = grip_idle['L'].lerp(guard, weight)
            q = hand_idle_q['L'].slerp((R(x=-35,y=-5).to_quaternion() @ hand_idle_q['L']), min(1,max(0,weight)))
            if FLUID:
                grip=spline(frame,[(1,grip_idle['L']),(4,V((.279,-.057,.565))),
                    (8,V((.249,-.137,.655))),(12,V((.250,-.145,.670))),
                    (17,V((.275,-.106,.610))),(23,grip_idle['L']+V((0,-.010,.015))),
                    (28,grip_idle['L'])])
                left_load=max(0,min(1,flow_load(max(1,frame-1))))
                q=hand_idle_q['L'].slerp(R(x=-25,y=-5).to_quaternion() @ hand_idle_q['L'],left_load)
        world_r = q.to_matrix()
        wrist = grip-world_r @ grip_offset[side]
        idle_elbow = (d['Chest'] @ idle['Chest'].inverted() @ idle['Forearm.'+side]).translation
        pole = idle_elbow.lerp(V((sign*.60,.025,.71 if side=='L' else .77)), weight)
        if THRUST:
            pole = idle_elbow.lerp(V((sign*(.38 if side=='L' else .45),.020,.72 if side=='L' else .76)),weight)
        wrist, elbow, correction = solve_limb('UpperArm.'+side,'Forearm.'+side,shoulder,wrist,pole)
        reach_corrections.append((frame,side,correction))
        d['UpperArm.'+side] = segment('UpperArm.'+side,shoulder,elbow)
        d['Forearm.'+side] = segment('Forearm.'+side,elbow,wrist)
        d['Hand.'+side] = T(wrist, world_r @ rest['Hand.'+side].to_3x3())
        follow('HandSocket.'+side,d)

        hip_joint = (d['Hips'] @ idle_local['Thigh.'+side]).translation
        ankle = idle['Foot.'+side].translation.copy()
        pole = idle['Shin.'+side].translation+V((0,-.2,0))
        ankle,knee,correction = solve_limb('Thigh.'+side,'Shin.'+side,hip_joint,ankle,pole)
        assert correction < 1e-5, ('Leg reach',frame,side,correction)
        d['Thigh.'+side] = segment('Thigh.'+side,hip_joint,knee)
        d['Shin.'+side] = segment('Shin.'+side,knee,ankle)
        d['Foot.'+side] = idle['Foot.'+side].copy()
    return d

attack = bpy.data.actions.new(ACTION)
attack.use_fake_user = True
rig.animation_data.action = attack
attack['fps'] = 30
attack['in_place'] = True
attack['loop'] = False
attack['weapon_hand'] = 'R'
attack['description'] = ('Short straight knife thrust with a retract before lowering the arm.' if THRUST else 'One diagonal knife slash.')+' Returns to the existing Idle frame 1.'
if FLUID: attack['description']='Continuous knife thrust: overlapping body lead, a moving windup, no extension hold, curved recoil and delayed settling.'
attack['suggested_contact_frame'] = CONTACT
attack['timing_note'] = 'Animation phase only; no gameplay damage/hit event is installed.'
markers = [('Start',1),('Anticipation',WINDUP),('Thrust',CONTACT),('Retract',16),('Chambered',22),('Recovered',END)] if THRUST else [('Start',1),('Anticipation',WINDUP),('Slash',CONTACT),('FollowThrough',FOLLOW),('Recovered',END)]
if FLUID: markers=[('Start',1),('PassingWindup',7),('Thrust',11),('Recoil',14),('Settled',28)]
for name,frame in markers:
    marker = attack.pose_markers.new(name); marker.frame = frame
previous = {}
for frame in range(1,END+1):
    scene.frame_set(frame)
    desired = pose(frame)
    for b in arm.bones:
        n = b.name; p = parents[n]
        basis = rest[n].inverted() @ rest[p] @ desired[p].inverted() @ desired[n] if p else rest[n].inverted() @ desired[n]
        loc,q,scale = basis.decompose()
        if n in previous and previous[n].dot(q)<0: q.negate()
        previous[n] = q.copy()
        pb = rig.pose.bones[n]
        pb.location = loc; pb.rotation_quaternion = q; pb.scale = (1,1,1)
        pb.keyframe_insert('location',frame=frame,group=n)
        pb.keyframe_insert('rotation_quaternion',frame=frame,group=n)
for fc in attack.fcurves:
    for k in fc.keyframe_points: k.interpolation = 'LINEAR'
assert max(c[2] for c in reach_corrections)<.008, sorted(reach_corrections,key=lambda c:c[2])[-5:]
if FLUID: assert max(c[2] for c in reach_corrections)<1e-5, sorted(reach_corrections,key=lambda c:c[2])[-5:]
assert signature(old_actions) == old_signature
assert all(shape_signature(o)==shapes[o.name] for o in meshes)

# Evaluate the actual skinned surfaces, contact points, sockets and return pose.
checks = {'source':str(SOURCE),'mesh_slots':13,'bones':23,
          'existing_meshes_and_weights_unchanged':True,'existing_actions_unchanged':True,
          'existing_actions_sha256':old_signature,'clip':ACTION,'fps':30,
          'frames':[1,END],'duration_seconds':(END-1)/30,'loop':False,'in_place':True,
          'suggested_contact_frame':CONTACT,'max_arm_goal_clamp_m':max(c[2] for c in reach_corrections),
          'unity_checked':False}
snapshots = {}; min_sole = 100; foot_drift = 0; socket_error = 0; trajectory = []
use_action(attack)
for frame in range(1,END+1):
    scene.frame_set(frame); bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    coords = []
    for o in meshes:
        ev = o.evaluated_get(dg); em = ev.to_mesh()
        if o.name == 'Compact_Boots': min_sole = min(min_sole,min(v.co.z for v in em.vertices))
        if frame in (1,CONTACT,END): coords.extend(v.co.copy() for v in em.vertices)
        ev.to_mesh_clear()
    if coords: snapshots[frame] = coords
    for side in ['L','R']:
        foot_drift = max(foot_drift,(rig.pose.bones['Foot.'+side].matrix.translation-idle['Foot.'+side].translation).length)
        grip = rig.pose.bones['Hand.'+side].matrix @ rest['Hand.'+side].inverted() @ grip_rest[side]
        socket_error = max(socket_error,(grip-rig.pose.bones['HandSocket.'+side].matrix.translation).length)
    trajectory.append(list(rig.pose.bones['HandSocket.R'].matrix.translation))
checks.update(min_boot_sole_z_m=min_sole,max_foot_drift_m=foot_drift,max_socket_detachment_m=socket_error,
              return_pose_error_m=max((a-b).length for a,b in zip(snapshots[1],snapshots[END])),
              maximum_pose_motion_m=max((a-b).length for a,b in zip(snapshots[1],snapshots[CONTACT])),
              right_hand_trajectory=trajectory)
if FLUID:
    step_speeds=[(V(b)-V(a)).length*30 for a,b in zip(trajectory,trajectory[1:])]
    checks['motion_revision']='Continuous Hermite path; torso leads arm; no interior pose holds'
    checks['hand_speeds_mps']=step_speeds
    checks['internal_stationary_intervals']=[i+1 for i,v in enumerate(step_speeds) if 3<=i+1<=21 and v<.02]
    assert not checks['internal_stationary_intervals'],checks['internal_stationary_intervals']
assert min_sole>-.00001 and foot_drift<1e-5 and socket_error<1e-5
assert checks['return_pose_error_m']<1e-5 and checks['maximum_pose_motion_m']>.20
(OUT/'AnimationCheck.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')

scene.render.fps = 30; scene.frame_start = 1; scene.frame_end = END
scene.frame_set(1)
rig['stage'] = ' / '.join(a.name for a in old_actions)+' / '+ACTION+'; round hands; Unity integration pending'
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True)
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'CompactSurvivor_Combat.fbx'),use_selection=True,
    object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
    bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0,mesh_smooth_type='FACE',apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS')
bpy.ops.export_scene.gltf(filepath=str(OUT/'CompactSurvivor_Combat.glb'),use_selection=True,
    export_format='GLB',export_animations=True,export_animation_mode='ACTIONS')
use_action(attack); scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CompactSurvivor_Combat.blend'))
print('ONEHAND_ANIMATION_COMPLETE', json.dumps({k:v for k,v in checks.items() if k!='right_hand_trajectory'}),flush=True)
