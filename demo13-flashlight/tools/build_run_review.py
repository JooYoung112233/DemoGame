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
COUNT=24; DUTY=.34; STRIDE=.58; LIFT=.16; SPEED=STRIDE/(DUTY*COUNT/30)
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
        q=phi/DUTY; base=.075-STRIDE/2+STRIDE*q
        pitch=-10*(1-smooth(0,.22,q))+52*smooth(.32,1,q)
    else:
        u=(phi-DUTY)/(1-DUTY); tangent=STRIDE/DUTY*(1-DUTY)
        travel=u+.08*math.sin(math.pi*u)**2
        base=.075+hermite(STRIDE/2,-STRIDE/2,tangent,tangent,travel)
        # Recover the heel behind the pelvis first; lower the foot as it passes
        # forward instead of carrying a high boot under the knee until landing.
        lift=LIFT*math.sin(math.pi*u**.75)*(1-.45*smooth(.55,.85,u))
        pitch=52+32*math.sin(math.pi*u)-62*smooth(0,1,u)
    r=world_rot(x=pitch); pivot=pivots[s]['heel' if pitch<0 else 'toe']
    y=base+pivot.y-(r@pivot).y; bottom=min((r@p).z for p in sole_points[s])
    return V((.092 if s=='L' else -.092,y,-bottom+lift)),r,stance,phi,lift
def hip_xy_rot(phase):
    t=math.tau*phase
    return V((.009*math.sin(t),-.060,0)),world_rot(x=16,y=1.3*math.sin(t),z=-6*math.cos(t))
corrections=[]

def body_height(phase):
    keys=[(0,.533),(.10,.486),(.23,.499),(.34,.522),(.40,.538),(.50,.533)]
    q=phase%.5
    for i in range(len(keys)-1):
        a,za=keys[i];b,zb=keys[i+1]
        if a<=q<b:
            pa,pz=keys[i-1] if i else (-.10,.538)
            nb,nz=keys[i+2] if i+2<len(keys) else (.60,.486)
            da=0 if (za-pz)*(zb-za)<=0 else (zb-pz)/(b-pa)
            db=0 if (zb-za)*(nz-zb)<=0 else (nz-za)/(nb-a)
            return hermite(za,zb,da*(b-a),db*(b-a),(q-a)/(b-a))
    return keys[0][1]
def pose(phase):
    t=math.tau*phase; goals={s:foot_goal(phase,s) for s in ('L','R')}
    hip,hr=hip_xy_rot(phase)
    # Continuous compression/rebound across both stance and flight, without a
    # discontinuity when support ends. Two rebounds per complete left/right cycle.
    hip.z=body_height(phase)
    # Keep the airborne reaching leg just short of full extension. This is a
    # swing-foot target adjustment, never a change to stance contact or bone length.
    for s in ('L','R'):
        if not goals[s][2]:
            offset=hr@(arm.bones['Thigh.'+s].head_local-arm.bones['Hips'].head_local)
            ankle=goals[s][0];dx=hip.x+offset.x-ankle.x;dy=hip.y+offset.y-ankle.y
            reach=sum(lengths[s])*.985
            ankle.z=max(ankle.z,hip.z+offset.z-math.sqrt(reach*reach-dx*dx-dy*dy))
    ceiling=hip.z
    for s in ('L','R'):
        offset=hr@(arm.bones['Thigh.'+s].head_local-arm.bones['Hips'].head_local)
        ankle=goals[s][0]; reach=sum(lengths[s])*.995
        dx=hip.x+offset.x-ankle.x; dy=hip.y+offset.y-ankle.y
        ceiling=min(ceiling,ankle.z+math.sqrt(reach*reach-dx*dx-dy*dy)-offset.z)
    corrections.append(hip.z-ceiling); hip.z=ceiling
    d={'Root':rest['Root'].copy(),'Hips':transform(hip,hr@rest['Hips'].to_3x3())}
    follow('Spine',d,world_rot(x=4+1.0*math.sin(2*t-.25),y=.7*math.sin(t-.1),z=5*math.cos(t-.10)))
    follow('Chest',d,world_rot(x=1+.6*math.sin(2*t-.45),y=.5*math.sin(t-.25),z=8*math.cos(t-.18)))
    follow('Neck',d)
    follow('Head',d,world_rot(x=-11-.8*math.sin(2*t-.65),y=-1*math.sin(t-.4),z=-3*math.cos(t-.35)))
    follow('Backpack',d,world_rot(x=2.4*math.sin(2*t-.7),z=1.3*math.sin(t-.35)))
    for s,sign in [('L',1),('R',-1)]:
        swing=math.cos(t+(0 if s=='L' else math.pi))
        follow('Clavicle.'+s,d,world_rot(y=sign*.8))
        # Run arm planes are authored anatomically instead of retaining the
        # original relaxed rest-pose splay. Elbows stay beside the rib cage.
        shoulder=(d['Clavicle.'+s]@rest['Clavicle.'+s].inverted()@rest['UpperArm.'+s]).translation
        upper_angle=radians(32*swing+4)
        bend=radians(64-12*swing)
        yaw=world_rot(z=3*math.cos(t-.18))
        upper_dir=(yaw@V((sign*.25,math.sin(upper_angle),-math.cos(upper_angle)))).normalized()
        lower_dir=(yaw@V((sign*.15,math.sin(upper_angle-bend),-math.cos(upper_angle-bend)))).normalized()
        elbow=shoulder+upper_dir*arm.bones['UpperArm.'+s].length
        wrist=elbow+lower_dir*arm.bones['Forearm.'+s].length
        d['UpperArm.'+s]=segment('UpperArm.'+s,shoulder,elbow)
        d['Forearm.'+s]=segment('Forearm.'+s,elbow,wrist)
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
report={'revision':3,'frames':[1,COUNT+1],'fps':30,'cycle_seconds':COUNT/30,'nominal_forward_speed_mps':SPEED,
 'run_only_changed':True,'approved_walk_and_other_four_actions_unchanged':True,'geometry_and_weights_unchanged':True,
 'loop_seam_m':seam,'minimum_sole_z_m':bottom,'max_contact_height_error_m':contact_error,
 'max_same_support_vertex_slide_per_frame_m':drift,'max_pelvis_reach_correction_m':max(corrections),
 'pelvis_vertical_range_m':max(hips)-min(hips),'airborne_frames':airborne,'approval':'pending','unity_connected':False,
 'reference':'Full source video reattached; continuous 10.875-12.750 seconds studied, interpretation not motion capture'}
(OUT/'RunCheck.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene.render.fps=30;scene.frame_start=1;scene.frame_end=COUNT+1;scene.frame_set(1)
rig['stage']='Run candidate based on approved Walk v2; production asset unchanged'
staged=ROOT/'Library/CodexBlender/RunCandidate_v3.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(staged))
staged.replace(OUT/'CompactSurvivor_RunReview_v3.blend')
print('RUN_REVIEW_READY',json.dumps(report),flush=True)
