"""Exactly three attack clips: dagger thrust, two-handed slash, two-handed chop.
Both hands follow one rigid weapon frame with a shared 11 cm grip spacing.
"""
import bpy,math,json,hashlib,shutil,ast
from pathlib import Path
from mathutils import Vector,Matrix,Euler
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ChibiSurvivor/Player'
ARCHIVE=ROOT/'ArtSource/CharacterArchive/2026-09-08/BeforeTwoHandAttacks'
ARCHIVE.mkdir(parents=True,exist_ok=True)
for name in ['CompactSurvivor_Combat.blend','CompactSurvivor_Combat.fbx','CompactSurvivor_Combat.glb',
             'OneHandAttacks_Preview.blend','OneHandAttacks_Preview.mp4','OneHandAttackSet.json',
             'OneHandPreviewCheck.json','OneHandWeaponClearance.json','ChopAnimationCheck.json']:
    src=OUT/name;dst=ARCHIVE/name
    if src.exists() and not dst.exists():shutil.copy2(src,dst)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'CompactSurvivor_Combat.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];arm=rig.data
meshes=[o for o in scene.objects if o.type=='MESH']
keep={'Idle','Walk','Run','Attack_OneHand_Thrust'}
for action in list(bpy.data.actions):
    if action.name not in keep:bpy.data.actions.remove(action)
def signature():
    return hashlib.sha256(repr([(a.name,[(f.data_path,f.array_index,[(tuple(k.co),k.interpolation) for k in f.keyframe_points]) for f in a.fcurves]) for a in bpy.data.actions if a.name in keep]).encode()).hexdigest()
def geometry():
    return hashlib.sha256(repr([(o.name,[tuple(v.co) for v in o.data.vertices],
        [tuple(p.vertices) for p in o.data.polygons],[[tuple((g.group,g.weight)) for g in v.groups] for v in o.data.vertices]) for o in meshes]).encode()).hexdigest()
before_motion=signature();before_geo=geometry()
rest={b.name:b.matrix_local.copy() for b in arm.bones}
parents={b.name:b.parent.name if b.parent else None for b in arm.bones}
def use(action):
    rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
use(bpy.data.actions['Idle']);scene.frame_set(1);bpy.context.view_layer.update()
idle={b.name:b.matrix.copy() for b in rig.pose.bones}
local={n:idle[parents[n]].inverted()@m if parents[n] else m.copy() for n,m in idle.items()}
# Reuse only the pure pose mathematics; never execute the legacy generator's I/O.
source=ast.parse((ROOT/'tools/build_onehand_chop.py').read_text(encoding='utf-8'))
names={'V','R','T','spline','curve','follow','segment','solve'}
functions=[n for n in source.body if isinstance(n,ast.FunctionDef) and n.name in names]
assert len(functions)==len(names)
exec(compile(ast.Module(body=functions,type_ignores=[]),'<shared combat pose math>','exec'))
HAND_GAP=.110
hand_socket={s:rest['Hand.'+s].inverted()@rest['HandSocket.'+s] for s in ('L','R')}
socket_base=idle['HandSocket.R'].to_3x3();axis_base=socket_base@V((0,1,0))
ready=V((-.020,-.274,.810));ready_axis=V((0,-.38,.925)).normalized()
specs={
 'Attack_TwoHand_Slash':{
  'end':40,'contact':16,
  'position':[(1,ready),(6,V((-.066,-.252,.882))),(11,V((-.102,-.241,.977))),
    (13,V((-.081,-.288,.952))),(16,V((-.005,-.350,.828))),
    (20,V((.105,-.267,.723))),(26,V((.077,-.259,.761))),(33,ready+V((.009,-.020,.016))),(40,ready)],
  'axis':[(1,ready_axis),(7,V((-.37,-.43,.82))),(11,V((-.55,-.20,.81))),
    (13,V((-.52,-.67,.52))),(16,V((.36,-.92,-.10))),
    (20,V((.81,-.50,-.30))),(27,V((.58,-.78,.23))),(34,V((.13,-.57,.81))),(40,ready_axis)],
  'drive':[(1,0),(6,-.48),(11,-1),(13,-.55),(16,.73),(19,1),(24,.66),(31,.18),(40,0)],
  'description':'Two-handed diagonal sword slash across the front; pelvis leads hands; fixed grip spacing.'},
 'Attack_TwoHand_Chop':{
  'end':43,'contact':18,
  'position':[(1,ready),(6,V((-.028,-.273,.915))),(11,V((-.034,-.294,1.025))),
    (13,V((-.030,-.305,1.015))),(18,V((.000,-.319,.734))),
    (21,V((.019,-.320,.671))),(28,V((.010,-.279,.735))),
    (36,ready+V((.003,-.015,.016))),(43,ready)],
  'axis':[(1,ready_axis),(7,V((-.15,-.38,.913))),(11,V((-.24,-.15,.959))),
    (13,V((-.16,-.35,.923))),(18,V((.015,-.86,-.51))),
    (21,V((.03,-.70,-.71))),(29,V((.02,-.90,.43))),(37,V((0,-.51,.86))),(43,ready_axis)],
  'drive':[(1,0),(6,-.23),(11,-.44),(13,-.15),(17,1),(21,1.1),(28,.51),(36,.10),(43,0)],
  'description':'Two-handed central axe chop; shared weapon frame, upper-body fold and knee compression.'}}
reports={}
for name,spec in specs.items():
    end=spec['end'];chop=name.endswith('Chop');action=bpy.data.actions.new(name)
    action.use_fake_user=True;use(action);previous={};reach=[]
    for frame in range(1,end+1):
        drive=curve(frame,spec['drive'])
        load=math.sin(math.pi*(frame-1)/(end-1))**1.1
        hipdelta=V((.008*drive,-.013*load-.014*max(0,drive),-.012-.019*load-.014*max(0,drive)))
        d={'Root':idle['Root'].copy()}
        d['Hips']=T(idle['Hips'].translation+hipdelta,R(x=2.8*drive,z=(3 if chop else 8)*drive)@idle['Hips'].to_3x3())
        follow('Spine',d,R(x=(7 if chop else 3)*drive,z=(3 if chop else 11)*drive))
        follow('Chest',d,R(x=(6 if chop else 2)*drive,z=(4 if chop else 13)*drive))
        follow('Neck',d);follow('Head',d,R(x=-(9 if chop else 3)*drive,z=-(6 if chop else 23)*drive))
        follow('Backpack',d,R(x=0 if frame in (1,end) else -2*curve(max(1,frame-2),spec['drive'])))
        p=spline(frame,spec['position']);axis=spline(frame,spec['axis']).normalized()
        rot=axis_base.rotation_difference(axis).to_matrix()@socket_base
        weapon=T(p,rot)
        for side,sign in [('R',-1),('L',1)]:
            follow('Clavicle.'+side,d)
            shoulder=(d['Clavicle.'+side]@local['UpperArm.'+side]).translation
            socket=weapon.copy()
            if side=='L':socket.translation-=axis*HAND_GAP
            hand=socket@hand_socket[side].inverted()
            wrist=hand.translation
            pole=V((sign*.48,-.045,.71+.16*max(0,(p.z-.81)/.24)))
            target,elbow,clamp=solve('UpperArm.'+side,'Forearm.'+side,shoulder,wrist,pole)
            reach.append((frame,side,clamp))
            # Clamping one arm independently would detach it from the shared handle.
            assert clamp<1e-5,(name,frame,side,clamp,list(p))
            d['UpperArm.'+side]=segment('UpperArm.'+side,shoulder,elbow)
            d['Forearm.'+side]=segment('Forearm.'+side,elbow,wrist)
            d['Hand.'+side]=hand;d['HandSocket.'+side]=socket
            joint=(d['Hips']@local['Thigh.'+side]).translation
            ankle,knee,clamp=solve('Thigh.'+side,'Shin.'+side,joint,idle['Foot.'+side].translation,
                idle['Shin.'+side].translation+V((0,-.25,0)))
            assert clamp<1e-5,(name,frame,side,'leg')
            d['Thigh.'+side]=segment('Thigh.'+side,joint,knee)
            d['Shin.'+side]=segment('Shin.'+side,knee,ankle);d['Foot.'+side]=idle['Foot.'+side].copy()
        scene.frame_set(frame)
        for bone in arm.bones:
            n=bone.name;parent=parents[n]
            basis=rest[n].inverted()@rest[parent]@d[parent].inverted()@d[n] if parent else rest[n].inverted()@d[n]
            loc,q,scale=basis.decompose()
            if n in previous and previous[n].dot(q)<0:q.negate()
            previous[n]=q.copy();pb=rig.pose.bones[n];pb.location=loc;pb.rotation_quaternion=q;pb.scale=(1,1,1)
            pb.keyframe_insert('location',frame=frame,group=n);pb.keyframe_insert('rotation_quaternion',frame=frame,group=n)
    for fc in action.fcurves:
        for k in fc.keyframe_points:k.interpolation='LINEAR'
    for label,f in [('Ready',1),('Windup',11),('Contact',spec['contact']),('Recover',28),('ReadyAgain',end)]:
        mark=action.pose_markers.new(label);mark.frame=f
    action['description']=spec['description'];action['fps']=30;action['in_place']=True;action['loop']=False
    action['suggested_contact_frame']=spec['contact'];action['hands']=2;action['grip_spacing_m']=HAND_GAP
    action['transition_note']='Starts and ends in two-hand ready pose. Blend from locomotion in the future Unity controller.'
    foot=0;grip=0;rotation=0;snaps={};sole=100
    for frame in range(1,end+1):
        scene.frame_set(frame);bpy.context.view_layer.update()
        right=rig.pose.bones['HandSocket.R'].matrix;left=rig.pose.bones['HandSocket.L'].matrix
        rel=right.inverted()@left
        grip=max(grip,(rel.translation-V((0,-HAND_GAP,0))).length)
        rotation=max(rotation,rel.to_quaternion().angle)
        for side in ('L','R'):foot=max(foot,(rig.pose.bones['Foot.'+side].matrix.translation-idle['Foot.'+side].translation).length)
        if frame in (1,spec['contact'],end):
            dg=bpy.context.evaluated_depsgraph_get();coords=[]
            for o in meshes:
                ev=o.evaluated_get(dg);mesh=ev.to_mesh();coords.extend(v.co.copy() for v in mesh.vertices)
                if o.name=='Compact_Boots':sole=min(sole,min(v.co.z for v in mesh.vertices))
                ev.to_mesh_clear()
            snaps[frame]=coords
    seam=max((a-b).length for a,b in zip(snaps[1],snaps[end]))
    assert grip<1e-5 and foot<1e-5 and seam<1e-5 and sole>-.00001,(name,grip,foot,seam,sole)
    reports[name]={'frames':[1,end],'contact_frame':spec['contact'],'grip_spacing_m':HAND_GAP,
      'max_grip_error_m':grip,'max_grip_rotation_error_rad':rotation,'max_foot_drift_m':foot,
      'ready_pose_seam_m':seam,'min_sole_z_m':sole,'max_arm_clamp_m':max(x[2] for x in reach)}
assert signature()==before_motion and geometry()==before_geo
assert len(bpy.data.actions)==6 and len(meshes)==13
report={'clips':reports,'dagger_and_locomotion_unchanged':True,'geometry_and_weights_unchanged':True,
 'attack_clip_count':3,'unity_connected':False}
(OUT/'TwoHandAnimationCheck.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
profiles={'axis_convention':'Weapon-local +Y points toward blade/head; right grip at origin',
 'dagger':{'action':'Attack_OneHand_Thrust','right_grip':[0,0,0],'left_grip':None},
 'sword':{'action':'Attack_TwoHand_Slash','right_grip':[0,0,0],'left_grip':[0,-HAND_GAP,0]},
 'axe':{'action':'Attack_TwoHand_Chop','right_grip':[0,0,0],'left_grip':[0,-HAND_GAP,0]},
 'hammer':{'action':'Attack_TwoHand_Chop','right_grip':[0,0,0],'left_grip':[0,-HAND_GAP,0]},
 'scope':'Blender authoring attachment convention. Runtime weapon swapping and IK are not connected.'}
(OUT/'WeaponGripProfiles.json').write_text(json.dumps(profiles,indent=2),encoding='utf-8')
(OUT/'AttackSet.json').write_text(json.dumps({'attack_mode':'stationary_full_body','attack_clip_count':3,
 'dagger':'Attack_OneHand_Thrust','sword':'Attack_TwoHand_Slash','axe':'Attack_TwoHand_Chop',
 'unity_connected':False},indent=2),encoding='utf-8')
use(bpy.data.actions['Idle']);scene.frame_set(1);scene.frame_start=1;scene.frame_end=91;scene.render.fps=30
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=rig
stage=ROOT/'Library/CodexBlender/TwoHandStaging';stage.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(stage/'CompactSurvivor_Combat.fbx'),use_selection=True,
 object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,
 bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,
 mesh_smooth_type='FACE',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS')
bpy.ops.export_scene.gltf(filepath=str(stage/'CompactSurvivor_Combat.glb'),use_selection=True,
 export_format='GLB',export_animations=True,export_animation_mode='ACTIONS')
use(bpy.data.actions['Idle']);scene.frame_set(1)
rig['stage']='Three attacks: one-hand dagger thrust / two-hand sword slash / two-hand axe chop. Unity pending.'
bpy.ops.wm.save_as_mainfile(filepath=str(stage/'CompactSurvivor_Combat.blend'))
for ext in ('.fbx','.glb','.blend'):
    src=(stage/('CompactSurvivor_Combat'+ext)).resolve();dst=(OUT/src.name).resolve()
    assert src.parent==stage.resolve() and dst.parent==OUT.resolve()
    src.replace(dst)
print('TWOHAND_ATTACKS_READY',json.dumps(report),flush=True)
