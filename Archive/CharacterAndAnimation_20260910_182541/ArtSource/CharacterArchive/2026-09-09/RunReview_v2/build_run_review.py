"""Run-only candidate, based on the approved walk asset; preserves its other actions."""
import bpy, math, json, ast, hashlib
from pathlib import Path
from mathutils import Vector, Matrix, Euler
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Assets/ChibiSurvivor/Player/WalkReview/CompactSurvivor_WalkReview.blend'
OUT=SOURCE.parent.parent/'RunReview'; OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene; rig=bpy.data.objects['CompactSurvivor_Rig']; arm=rig.data
meshes=[o for o in scene.objects if o.type=='MESH']
def digest_motion():
    return hashlib.sha256(repr([(a.name,[(fc.data_path,fc.array_index,[(tuple(k.co),k.interpolation) for k in fc.keyframe_points]) for fc in a.fcurves]) for a in bpy.data.actions if a.name!='Run']).encode()).hexdigest()
def digest_shape():
    return hashlib.sha256(repr([(o.name,[tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons], [[(g.group,g.weight) for g in v.groups] for v in o.data.vertices]) for o in meshes]).encode()).hexdigest()
original_motion=digest_motion(); original_shape=digest_shape()
rest={b.name:b.matrix_local.copy() for b in arm.bones}
parent={b.name:b.parent.name if b.parent else None for b in arm.bones}
tree=ast.parse((ROOT/'tools/rig_compact_survivor.py').read_text(encoding='utf-8'))
funcs=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in {'V','radians','smooth','transform','world_rot','follow','segment'}]
exec(compile(ast.Module(body=funcs,type_ignores=[]),'<run pose math>','exec'))
boot=bpy.data.objects['Compact_Boots']
sole_points={s:[v.co.copy()-arm.bones['Foot.'+s].head_local for v in boot.data.vertices if (v.co.x>=0)==(s=='L')] for s in ('L','R')}
lengths={s:(arm.bones['Thigh.'+s].length,arm.bones['Shin.'+s].length) for s in ('L','R')}
COUNT=24; DUTY=.40; STRIDE=.37; LIFT=.12; SPEED=STRIDE/(DUTY*COUNT/30)
pivots={}
for s,points in sole_points.items():
    bottom=min(p.z for p in points); flat=[p for p in points if p.z<bottom+.0001]
    pivots[s]={'heel':max(flat,key=lambda p:p.y),'toe':min(flat,key=lambda p:p.y)}
def hermite(a,b,da,db,u):
    return (2*u**3-3*u*u+1)*a+(u**3-2*u*u+u)*da+(-2*u**3+3*u*u)*b+(u**3-u*u)*db
def foot_goal(phase,s):
    phi=(phase+(0 if s=='L' else .5))%1
    stance=phi<DUTY; lift=0
    if stance:
        q=phi/DUTY; base=.003-STRIDE/2+STRIDE*q
        pitch=-6*(1-smooth(0,.18,q))+24*smooth(.50,1,q)
    else:
        u=(phi-DUTY)/(1-DUTY); tangent=STRIDE/DUTY*(1-DUTY)
        travel=u+.12*math.sin(math.pi*u)**2
        base=.003+hermite(STRIDE/2,-STRIDE/2,tangent,tangent,travel)
        # Recover the heel behind the pelvis first; lower the foot as it passes
        # forward instead of carrying a high boot under the knee until landing.
        lift=LIFT*math.sin(math.pi*u**.65)**.9
        pitch=24+28*math.sin(math.pi*u)-30*smooth(0,1,u)
    r=world_rot(x=pitch); pivot=pivots[s]['heel' if pitch<0 else 'toe']
    y=base+pivot.y-(r@pivot).y; bottom=min((r@p).z for p in sole_points[s])
    return V((.092 if s=='L' else -.092,y,-bottom+lift)),r,stance,phi,lift
def hip_xy_rot(phase):
    t=math.tau*phase
    return V((.009*math.sin(t),-.045,0)),world_rot(x=5,y=1.4*math.sin(t),z=-6*math.cos(t))
corrections=[]
def pose(phase):
    t=math.tau*phase; goals={s:foot_goal(phase,s) for s in ('L','R')}
    hip,hr=hip_xy_rot(phase)
    # Continuous compression/rebound across both stance and flight, without a
    # discontinuity when support ends. Two rebounds per complete left/right cycle.
    hip.z=.523-.020*math.cos(2*t-1.85)
    ceiling=hip.z
    for s in ('L','R'):
        offset=hr@(arm.bones['Thigh.'+s].head_local-arm.bones['Hips'].head_local)
        ankle=goals[s][0]; reach=sum(lengths[s])*.995
        dx=hip.x+offset.x-ankle.x; dy=hip.y+offset.y-ankle.y
        ceiling=min(ceiling,ankle.z+math.sqrt(reach*reach-dx*dx-dy*dy)-offset.z)
    corrections.append(hip.z-ceiling); hip.z=ceiling
    d={'Root':rest['Root'].copy(),'Hips':transform(hip,hr@rest['Hips'].to_3x3())}
    follow('Spine',d,world_rot(x=6+1.4*math.sin(2*t-.25),y=.7*math.sin(t-.1),z=5*math.cos(t-.10)))
    follow('Chest',d,world_rot(x=4+.7*math.sin(2*t-.45),y=.5*math.sin(t-.25),z=8*math.cos(t-.18)))
    follow('Neck',d)
    follow('Head',d,world_rot(x=-7-1.1*math.sin(2*t-.65),y=-1*math.sin(t-.4),z=-3*math.cos(t-.35)))
    follow('Backpack',d,world_rot(x=2.4*math.sin(2*t-.7),z=1.3*math.sin(t-.35)))
    for s,sign in [('L',1),('R',-1)]:
        swing=math.cos(t+(0 if s=='L' else math.pi))
        follow('Clavicle.'+s,d,world_rot(y=sign*.8))
        follow('UpperArm.'+s,d,world_rot(x=28*swing,y=sign*-2))
        follow('Forearm.'+s,d,world_rot(x=-66+10*swing))
        hand=follow('Hand.'+s,d,world_rot(x=-2))
        d['Hand.'+s]=hand@Matrix.Rotation(radians(sign*25),4,'Y'); follow('HandSocket.'+s,d)
        joint=(d['Hips']@rest['Hips'].inverted()@rest['Thigh.'+s]).translation
        ankle,fr,*_=goals[s]; axis=ankle-joint; dist=axis.length; axis.normalize(); l1,l2=lengths[s]
        assert abs(l1-l2)<dist<l1+l2,(phase,s,dist)
        along=(l1*l1-l2*l2+dist*dist)/(2*dist)
        forward=V((0,-1,0)); bend=(forward-axis*forward.dot(axis)).normalized()
        knee=joint+axis*along+bend*math.sqrt(max(0,l1*l1-along*along))
        d['Thigh.'+s]=segment('Thigh.'+s,joint,knee);d['Shin.'+s]=segment('Shin.'+s,knee,ankle)
        d['Foot.'+s]=transform(ankle,fr@rest['Foot.'+s].to_3x3())
    return d,goals
bpy.data.actions.remove(bpy.data.actions['Run'])
run=bpy.data.actions.new('Run');run.use_fake_user=True;rig.animation_data.action=run
previous={}
for frame in range(1,COUNT+2):
    scene.frame_set(frame);d,goals=pose((frame-1)/COUNT)
    for bone in arm.bones:
        n=bone.name;p=parent[n]
        basis=rest[n].inverted()@rest[p]@d[p].inverted()@d[n] if p else rest[n].inverted()@d[n]
        loc,q,scale=basis.decompose()
        if n in previous and previous[n].dot(q)<0:q.negate()
        previous[n]=q.copy();pb=rig.pose.bones[n];pb.location=loc;pb.rotation_quaternion=q;pb.scale=(1,1,1)
        pb.keyframe_insert('location',frame=frame,group=n);pb.keyframe_insert('rotation_quaternion',frame=frame,group=n)
for fc in run.fcurves:
    for k in fc.keyframe_points:k.interpolation='LINEAR'
run['fps']=30;run['in_place']=True;run['nominal_forward_speed_mps']=SPEED
run['loop_end_duplicate']=True;run['approval']='pending'
for label,f in [('LeftLand',1),('LeftCompression',5),('LeftPush',10),('Flight',12),('RightLand',13),('Loop',25)]:
    mark=run.pose_markers.new(label);mark.frame=f
if run.slots:rig.animation_data.action_slot=run.slots[0]
snaps={};bottom=100;contact_error=0;drift=0;airborne=0;last={};hips=[]
for frame in range(1,COUNT+2):
    scene.frame_set(frame);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
    _,goals=pose((frame-1)/COUNT);coords=[];hips.append(rig.pose.bones['Hips'].matrix.translation.z)
    for o in meshes:
        ev=o.evaluated_get(dg);mesh=ev.to_mesh()
        if frame in (1,COUNT+1):coords.extend(v.co.copy() for v in mesh.vertices)
        if o==boot:
            for s in ('L','R'):
                ids=[v.index for v in o.data.vertices if (v.co.x>=0)==(s=='L')]
                support=min(ids,key=lambda i:mesh.vertices[i].co.z);low=mesh.vertices[support].co.z;bottom=min(bottom,low)
                stance=goals[s][2];phi=goals[s][3]
                if stance:
                    contact_error=max(contact_error,abs(low));y=mesh.vertices[support].co.y-SPEED*(frame-1)/30
                    if s in last and last[s][0]==support and phi>last[s][2]:drift=max(drift,abs(y-last[s][1]))
                    last[s]=(support,y,phi)
                else:last.pop(s,None)
        ev.to_mesh_clear()
    if coords:snaps[frame]=coords
    if all(not goals[s][2] for s in ('L','R')):airborne+=1
seam=max((a-b).length for a,b in zip(snaps[1],snaps[COUNT+1]))
assert seam<1e-5 and bottom>-.00001 and contact_error<1e-5 and drift<.001 and airborne>0,(seam,bottom,contact_error,drift,airborne)
assert digest_motion()==original_motion and digest_shape()==original_shape
report={'revision':2,'frames':[1,COUNT+1],'fps':30,'cycle_seconds':COUNT/30,'nominal_forward_speed_mps':SPEED,
 'run_only_changed':True,'approved_walk_and_other_four_actions_unchanged':True,'geometry_and_weights_unchanged':True,
 'loop_seam_m':seam,'minimum_sole_z_m':bottom,'max_contact_height_error_m':contact_error,
 'max_same_support_vertex_slide_per_frame_m':drift,'max_pelvis_reach_correction_m':max(corrections),
 'pelvis_vertical_range_m':max(hips)-min(hips),'airborne_frames':airborne,'approval':'pending','unity_connected':False,
 'reference':'Cached video frames at 11.208, 11.833 and 12.458 seconds; interpretation, not motion capture'}
(OUT/'RunCheck.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene.render.fps=30;scene.frame_start=1;scene.frame_end=COUNT+1;scene.frame_set(1)
rig['stage']='Run candidate based on approved Walk v2; production asset unchanged'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CompactSurvivor_RunReview.blend'))
print('RUN_REVIEW_READY',json.dumps(report),flush=True)
