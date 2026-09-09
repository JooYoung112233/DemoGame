"""Rig the approved compact survivor and bake Idle/Walk/Run at 30 fps.
Analytic leg IK provides planted stance trajectories. Exported clips are in-place.
Equipment remains in its original thirteen mesh slots; source appearance untouched.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix, Euler

ROOT=Path(__file__).resolve().parents[1]
ARCHIVE=ROOT/'ArtSource/CharacterArchive/2026-09-08'
SOURCE=ARCHIVE/'Assets/ChibiSurvivor/CompactSurvivor/CompactSurvivor.blend'
OUT=ARCHIVE/'Assets/ChibiSurvivor/CompactSurvivor/Animated'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene=bpy.context.scene
meshes=[o for o in scene.objects if o.type=='MESH']
assert len(meshes)==13 and not any(o.type=='ARMATURE' for o in scene.objects)
for a in list(bpy.data.actions):bpy.data.actions.remove(a)
original={o.name:[v.co.copy() for v in o.data.vertices] for o in meshes}

def V(p):return Vector(p)
def remap(z):return z*.75 if z<=.768 else .576+(z-.768)*.9 if z<=1.2 else z-.2352
def radians(x):return math.radians(x)
def smooth(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)))
    return t*t*(3-2*t)

arm=bpy.data.armatures.new('CompactSurvivor_Skeleton')
rig=bpy.data.objects.new('CompactSurvivor_Rig',arm);scene.collection.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
def bone(name,head,tail,parent=None,deform=True):
    b=arm.edit_bones.new(name);b.head=head;b.tail=tail;b.use_deform=deform
    if parent:b.parent=arm.edit_bones[parent]
    return b
bone('Root',(0,0,0),(0,0,.10),deform=False)
bone('Hips',(0,0,.552),(0,0,.64),'Root')
bone('Spine',(0,0,.64),(0,0,.80),'Hips')
bone('Chest',(0,0,.80),(0,0,.93),'Spine')
bone('Neck',(0,0,.93),(0,0,.963),'Chest')
bone('Head',(0,0,.963),(0,0,1.235),'Neck')
bone('Backpack',(0,.167,.87),(0,.167,.97),'Chest')
for side,s in [('L',1),('R',-1)]:
    shoulder=(s*.170,0,remap(1.141));elbow=(s*.262,-.004,remap(.923))
    wrist=(s*.290,-.019,remap(.752));hand_end=(s*.296,-.025,.492)
    bone('Clavicle.'+side,(s*.045,0,.913),shoulder,'Chest')
    bone('UpperArm.'+side,shoulder,elbow,'Clavicle.'+side)
    bone('Forearm.'+side,elbow,wrist,'UpperArm.'+side)
    bone('Hand.'+side,wrist,hand_end,'Forearm.'+side)
    bone('HandSocket.'+side,(s*.296,-.03,.535),(s*.296,-.13,.535),'Hand.'+side,False)
    bone('Thigh.'+side,(s*.092,.004,.552),(s*.092,-.008,.3375),'Hips')
    bone('Shin.'+side,(s*.092,-.008,.3375),(s*.092,.003,.129),'Thigh.'+side)
    bone('Foot.'+side,(s*.092,.003,.129),(s*.092,-.125,.046),'Shin.'+side)
bpy.ops.object.mode_set(mode='OBJECT')
rig.show_in_front=True;arm.display_type='OCTAHEDRAL'
for pb in rig.pose.bones:pb.rotation_mode='QUATERNION'
rest={b.name:b.matrix_local.copy() for b in arm.bones}
parent={b.name:b.parent.name if b.parent else None for b in arm.bones}
rig['stage']='Basic locomotion rig; no combat clips or Unity integration yet'
rig['forward']='Blender -Y; FBX exported -Z forward, +Y up'
rig['animation_type']='In-place. Stance speed metadata is in AnimationCheck.json.'
rig['editing']='Anatomical FK bones; leg IK is solved and baked into editable pose keys.'
rig['weapon_sockets']='HandSocket.L / HandSocket.R reserved for later weapon attachments'

def components(data):
    adjacency=[[] for v in data.vertices]
    for e in data.edges:
        a,b=e.vertices;adjacency[a].append(b);adjacency[b].append(a)
    seen=set();groups=[]
    for i in range(len(adjacency)):
        if i in seen:continue
        ids=[];stack=[i];seen.add(i)
        while stack:
            j=stack.pop();ids.append(j)
            for k in adjacency[j]:
                if k not in seen:seen.add(k);stack.append(k)
        groups.append(ids)
    return groups

def torso_weights(z):
    if z<.67:
        t=smooth(.575,.67,z);return {'Hips':1-t,'Spine':t}
    t=smooth(.70,.84,z);return {'Spine':1-t,'Chest':t}

weight_report={}
for obj in meshes:
    slot=obj['equipment_slot']
    groups={b.name:obj.vertex_groups.new(name=b.name) for b in arm.bones if b.use_deform}
    for ids in components(obj.data):
        center=sum((obj.data.vertices[i].co for i in ids),V((0,0,0)))/len(ids)
        side='L' if center.x>=0 else 'R'
        component_min_z=min(obj.data.vertices[i].co.z for i in ids)
        component_max_z=max(obj.data.vertices[i].co.z for i in ids)
        for i in ids:
            x,y,z=obj.data.vertices[i].co
            if slot in {'Head','FaceDetails','Hair','Cap'}:w={'Head':1}
            elif slot in {'Belt','BeltPouch'}:w={'Hips':1}
            elif slot=='Hands':w={'Hand.'+side:1}
            elif slot=='Watch':w={'Forearm.L':1}
            elif slot=='Boots':w={'Foot.'+side:1}
            elif slot=='Backpack':
                w={'Backpack':1} if center.y>.11 else torso_weights(z)
            elif slot=='Body':
                if abs(center.x)<.12:w={'Neck':1}
                else:
                    t=smooth(remap(.894),remap(.931),z)
                    w={'Forearm.'+side:1-t,'UpperArm.'+side:t}
            elif slot=='Shirt':
                if abs(center.x)>.19:
                    # Sleeve and cuff follow upper arm, with a soft shoulder transition.
                    t=smooth(remap(1.090),remap(1.145),z)
                    w={'UpperArm.'+side:1-t,'Chest':t}
                else:w=torso_weights(z)
            elif slot=='Trousers':
                if abs(center.x)<.04:w={'Hips':1}
                elif component_min_z>.50:w={'Hips':1} # belt loops
                elif component_min_z>.31 and component_max_z<.50:
                    # Cargo pockets remain rigid rather than folding through the knee.
                    w={'Thigh.'+side:1}
                else:
                    hip=smooth(.504,.558,z)
                    thigh=smooth(.303,.371,z)*(1-hip)
                    w={'Hips':hip,'Thigh.'+side:thigh,'Shin.'+side:1-hip-thigh}
            else:raise RuntimeError('Unhandled slot '+slot)
            total=sum(w.values())
            for name,value in w.items():
                if value>1e-6:groups[name].add([i],value/total,'REPLACE')
    obj.parent=rig
    mod=obj.modifiers.new('Compact_Skin','ARMATURE');mod.object=rig
    mod.use_deform_preserve_volume=False # match FBX/Unity linear skinning
    obj['stage']='Skinned modular mesh; Idle/Walk/Run'
    assert all(v.groups and abs(sum(g.weight for g in v.groups)-1)<1e-5 for v in obj.data.vertices)
    weight_report[slot]={'vertices':len(obj.data.vertices),'max_influences':max(len(v.groups) for v in obj.data.vertices)}

scene.render.fps=30
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
rig.animation_data_create()
bpy.context.view_layer.update()
deps=bpy.context.evaluated_depsgraph_get()
rest_error=0
for obj in meshes:
    ev=obj.evaluated_get(deps);em=ev.to_mesh()
    rest_error=max(rest_error,max((v.co-original[obj.name][i]).length for i,v in enumerate(em.vertices)))
    ev.to_mesh_clear()
assert rest_error<1e-5, 'Rig changed the approved rest appearance'

def transform(position,rotation):
    m=rotation.to_4x4();m.translation=position;return m
def world_rot(x=0,y=0,z=0):return Euler(tuple(radians(v) for v in (x,y,z)),'XYZ').to_matrix()
def follow(name,desired,rotation=None):
    p=parent[name]
    m=desired[p] @ rest[p].inverted() @ rest[name] if p else rest[name].copy()
    if rotation is not None:m=transform(m.translation,rotation @ m.to_3x3())
    desired[name]=m
    return m
def segment(name,head,tail):
    b=arm.bones[name]
    q=(b.tail_local-b.head_local).normalized().rotation_difference((tail-head).normalized())
    return transform(head,q.to_matrix() @ rest[name].to_3x3())

boot=bpy.data.objects['Compact_Boots']
sole_points={side:[v.co.copy()-arm.bones['Foot.'+side].head_local for v in boot.data.vertices
    if (v.co.x>=0)==(side=='L')] for side in ['L','R']}
lengths={side:(arm.bones['Thigh.'+side].length,arm.bones['Shin.'+side].length) for side in ['L','R']}
CLIPS={'Idle':{'count':90,'stride':0,'duty':1,'speed':0},
       'Walk':{'count':30,'stride':.36,'duty':.60,'lift':.085},
       'Run':{'count':20,'stride':.34,'duty':.36,'lift':.16}}
for name,cfg in CLIPS.items():
    if name!='Idle':cfg['speed']=cfg['stride']/(cfg['duty']*cfg['count']/30)

def foot_goal(name,phase,side):
    cfg=CLIPS[name];phi=(phase+(0 if side=='L' else .5))%1
    pitch=0;lift=0;stance=True;y=.003
    if name!='Idle':
        stride=cfg['stride'];duty=cfg['duty']
        if phi<duty:y=.003-stride/2+stride*phi/duty
        else:
            stance=False;u=(phi-duty)/(1-duty)
            y=.003+stride*.5*math.cos(math.pi*u)
            lift=cfg['lift']*math.sin(math.pi*u)**1.1
            pitch=18*math.sin(math.tau*u) if name=='Run' else -8*math.sin(math.pi*u)
    r=world_rot(x=pitch)
    bottom=min((r @ p).z for p in sole_points[side])
    x=.092 if side=='L' else -.092
    return V((x,y,-bottom+lift)),r,stance,lift,phi

def pose(name,phase):
    t=math.tau*phase;moving=name!='Idle';run=name=='Run'
    goals={s:foot_goal(name,phase,s) for s in ['L','R']}
    if name=='Idle':hip_pos=V((.003*math.sin(t),0,.543+.002*math.cos(t)))
    elif name=='Walk':hip_pos=V((.007*math.sin(t),0,.505-.015*math.cos(2*t)))
    else:hip_pos=V((.008*math.sin(t),0,.504+.022*math.cos(2*t-math.tau*.86)))
    hip_rot=world_rot(y=(1.4 if moving else .5)*math.sin(t),z=(2.5 if moving else .7)*math.sin(t))
    # Limit pelvis height to the reach of the short legs, never stretch the mesh.
    for side in ['L','R']:
        local_hip=arm.bones['Thigh.'+side].head_local-arm.bones['Hips'].head_local
        offset=hip_rot @ local_hip;ankle=goals[side][0]
        reach=sum(lengths[side])*.994
        dx=hip_pos.x+offset.x-ankle.x;dy=hip_pos.y+offset.y-ankle.y
        assert reach*reach>dx*dx+dy*dy
        hip_pos.z=min(hip_pos.z,ankle.z+math.sqrt(reach*reach-dx*dx-dy*dy)-offset.z)
    desired={'Root':rest['Root'].copy(),'Hips':transform(hip_pos,hip_rot @ rest['Hips'].to_3x3())}
    follow('Spine',desired,world_rot(x=6 if run else 1.5 if moving else .35*math.sin(t)))
    follow('Chest',desired,world_rot(x=5 if run else 1 if moving else .3*math.sin(t),z=-(5 if run else 3 if moving else .8)*math.sin(t)))
    follow('Neck',desired)
    follow('Head',desired,world_rot(x=-6 if run else -1.4 if moving else -.4*math.sin(t),z=(2 if moving else 1)*math.sin(t)))
    follow('Backpack',desired,world_rot(x=(1.1 if run else .5 if moving else .15)*math.sin(2*t-.3)))
    for side,s in [('L',1),('R',-1)]:
        phi=phase+(0 if side=='L' else .5);swing=math.cos(math.tau*phi)
        follow('Clavicle.'+side,desired,world_rot(y=s*(1.2 if moving else .4)))
        follow('UpperArm.'+side,desired,world_rot(x=(32 if run else 17)*swing if moving else 1.0*math.sin(t)))
        follow('Forearm.'+side,desired,world_rot(x=(-60+8*swing) if run else (-12+4*swing) if moving else -7+1.2*math.sin(t)))
        hand=follow('Hand.'+side,desired,world_rot(x=-3 if run else 0))
        # Turn palms toward the body instead of presenting palms upward when running.
        desired['Hand.'+side]=hand @ Matrix.Rotation(radians(s*(65 if run else 25 if moving else 15)),4,'Y')
        follow('HandSocket.'+side,desired)
        hip=(desired['Hips'] @ rest['Hips'].inverted() @ rest['Thigh.'+side]).translation
        ankle,foot_r,_,_,_=goals[side]
        axis=ankle-hip;d=axis.length;axis.normalize();l1,l2=lengths[side]
        assert abs(l1-l2)<d<l1+l2
        along=(l1*l1-l2*l2+d*d)/(2*d)
        height=math.sqrt(max(0,l1*l1-along*along))
        forward=V((0,-1,0));bend=(forward-axis*forward.dot(axis)).normalized()
        knee=hip+axis*along+bend*height
        desired['Thigh.'+side]=segment('Thigh.'+side,hip,knee)
        desired['Shin.'+side]=segment('Shin.'+side,knee,ankle)
        desired['Foot.'+side]=transform(ankle,foot_r @ rest['Foot.'+side].to_3x3())
    return desired,goals

actions={}
for name,cfg in CLIPS.items():
    a=bpy.data.actions.new(name);a.use_fake_user=True;rig.animation_data.action=a
    actions[name]=a;a['fps']=30;a['in_place']=True;a['nominal_forward_speed_mps']=cfg['speed']
    a['loop_end_duplicate']=True
    previous={}
    for frame in range(1,cfg['count']+2):
        scene.frame_set(frame)
        desired,goals=pose(name,(frame-1)/cfg['count'])
        for b in arm.bones:
            name_b=b.name;p=parent[name_b]
            basis=rest[name_b].inverted() @ rest[p] @ desired[p].inverted() @ desired[name_b] if p else rest[name_b].inverted() @ desired[name_b]
            loc,q,scale=basis.decompose()
            if name_b in previous and previous[name_b].dot(q)<0:q.negate()
            previous[name_b]=q.copy()
            pb=rig.pose.bones[name_b];pb.location=loc;pb.rotation_quaternion=q;pb.scale=(1,1,1)
            pb.keyframe_insert('location',frame=frame,group=name_b)
            pb.keyframe_insert('rotation_quaternion',frame=frame,group=name_b)
    for fc in a.fcurves:
        for kp in fc.keyframe_points:kp.interpolation='LINEAR'

# Evaluate actual skinned surfaces, including seams and ground contact.
checks={'source':str(SOURCE),'bones':len(arm.bones),'mesh_slots':13,'rest_pose_max_error_m':rest_error,
        'weights':weight_report,'clips':{},'scope':'Blender skin/export verification; Unity validation pending'}
for name,a in actions.items():
    rig.animation_data.action=a;cfg=CLIPS[name];count=cfg['count']
    ends=[];min_sole=100;max_contact_error=0;foot_drift=0;reach_error=0;frames_airborne=0
    last_contacts={}
    for frame in range(1,count+2):
        scene.frame_set(frame);bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
        phase=(frame-1)/count;desired,goals=pose(name,phase)
        snapshot=[]
        for obj in meshes:
            ev=obj.evaluated_get(deps);em=ev.to_mesh()
            if frame in (1,count+1):snapshot.extend(ev.matrix_world @ v.co for v in em.vertices)
            if obj['equipment_slot']=='Boots':
                for side in ['L','R']:
                    idx=[v.index for v in obj.data.vertices if (v.co.x>=0)==(side=='L')]
                    bottom=min((ev.matrix_world @ em.vertices[i].co).z for i in idx)
                    min_sole=min(min_sole,bottom)
                    goal,r,stance,lift,phi=goals[side]
                    if stance:max_contact_error=max(max_contact_error,abs(bottom))
                    actual=rig.pose.bones['Foot.'+side].head
                    reach_error=max(reach_error,(actual-goal).length)
                    virtual_world_y=actual.y-cfg['speed']*(frame-1)/30
                    if stance and side in last_contacts and phi>=last_contacts[side][0]:
                        foot_drift=max(foot_drift,abs(virtual_world_y-last_contacts[side][1]))
                    last_contacts[side]=(phi,virtual_world_y) if stance else (-1,virtual_world_y)
                    if not stance:last_contacts.pop(side,None)
            ev.to_mesh_clear()
        if frame in (1,count+1):ends.append(snapshot)
        if all(not goals[s][2] for s in ['L','R']):frames_airborne+=1
    seam=max((a-b).length for a,b in zip(*ends))
    assert seam<2e-5,(name,'loop seam',seam)
    assert min_sole>-.001,(name,'sole below floor',min_sole)
    assert max_contact_error<.001,(name,'ground contact error',max_contact_error)
    assert foot_drift<.001,(name,'stance drift with matching movement speed',foot_drift)
    assert reach_error<1e-4,(name,'IK bake mismatch',reach_error)
    checks['clips'][name]={'start':1,'end':count+1,'fps':30,'duration_seconds':count/30,
        'nominal_forward_speed_mps':cfg['speed'],'in_place':True,'loop_max_vertex_error_m':seam,
        'lowest_sole_m':min_sole,'max_stance_height_error_m':max_contact_error,
        'max_stance_slide_per_frame_m_at_nominal_speed':foot_drift,
        'max_ik_bake_error_m':reach_error,'airborne_frames':frames_airborne}
(OUT/'AnimationCheck.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')

rig.animation_data.action=actions['Idle'];scene.frame_start=1;scene.frame_end=91;scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for obj in meshes:obj.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'CompactSurvivor_Animated.fbx'),use_selection=True,
    object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
    bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0,mesh_smooth_type='FACE',apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS')
bpy.ops.export_scene.gltf(filepath=str(OUT/'CompactSurvivor_Animated.glb'),use_selection=True,
    export_format='GLB',export_animations=True,export_animation_mode='ACTIONS')
# Give each action an explicit timeline marker and retain Idle as startup clip.
for action in actions.values():
    action.pose_markers.new('LoopStart').frame=1
    action.pose_markers.new('LoopEnd').frame=int(action.frame_range[1])
rig.animation_data.action=actions['Idle'];scene.frame_start=1;scene.frame_end=91;scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CompactSurvivor_Animated.blend'))

# Three actual posed renders for inspecting joints before movie generation.
scene.cycles.samples=24
scene.render.resolution_x=600;scene.render.resolution_y=750
for name,frame in [('Idle',1),('Walk',7),('Run',6)]:
    rig.animation_data.action=actions[name];scene.frame_set(frame)
    scene.render.filepath=str(OUT/(name+'_Pose.png'));bpy.ops.render.render(write_still=True)
print('COMPACT_RIG_COMPLETE',json.dumps(checks))
