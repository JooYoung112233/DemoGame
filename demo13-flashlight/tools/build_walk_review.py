"""One walk revision for approval. The current Player asset is never overwritten."""
import bpy,math,json,ast,hashlib,shutil
from pathlib import Path
from mathutils import Vector,Matrix,Euler
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Assets/ChibiSurvivor/Player/CompactSurvivor_Combat.blend'
OUT=SOURCE.parent/'WalkReview';OUT.mkdir(exist_ok=True)
ARCHIVE=ROOT/'ArtSource/CharacterArchive/2026-09-09/WalkReview_v1';ARCHIVE.mkdir(parents=True,exist_ok=True)
for name in ['CompactSurvivor_WalkReview.blend','WalkCheck.json','WalkComparison.mp4','WalkPoses.png']:
    src=OUT/name;dst=ARCHIVE/name
    if src.exists() and not dst.exists():shutil.copy2(src,dst)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];arm=rig.data
meshes=[o for o in scene.objects if o.type=='MESH']
def digest_motion():
    return hashlib.sha256(repr([(a.name,[(fc.data_path,fc.array_index,[(tuple(k.co),k.interpolation) for k in fc.keyframe_points]) for fc in a.fcurves]) for a in bpy.data.actions if a.name!='Walk']).encode()).hexdigest()
def digest_shape():
    return hashlib.sha256(repr([(o.name,[tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons],
      [[(g.group,g.weight) for g in v.groups] for v in o.data.vertices]) for o in meshes]).encode()).hexdigest()
original_motion=digest_motion();original_shape=digest_shape()
rest={b.name:b.matrix_local.copy() for b in arm.bones}
parent={b.name:b.parent.name if b.parent else None for b in arm.bones}
# Pure shared math, without loading or running the original rig/asset generator.
tree=ast.parse((ROOT/'tools/rig_compact_survivor.py').read_text(encoding='utf-8'))
names={'V','radians','smooth','transform','world_rot','follow','segment'}
funcs=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in names]
exec(compile(ast.Module(body=funcs,type_ignores=[]),'<walk pose math>','exec'))
boot=bpy.data.objects['Compact_Boots']
sole_points={s:[v.co.copy()-arm.bones['Foot.'+s].head_local for v in boot.data.vertices if (v.co.x>=0)==(s=='L')] for s in ('L','R')}
lengths={s:(arm.bones['Thigh.'+s].length,arm.bones['Shin.'+s].length) for s in ('L','R')}
COUNT=36;DUTY=.60;STRIDE=.34;LIFT=.035;SPEED=STRIDE/(DUTY*COUNT/30)
pivots={}
for s,points in sole_points.items():
    bottom=min(p.z for p in points);flat=[p for p in points if p.z<bottom+.0001]
    pivots[s]={'heel':max(flat,key=lambda p:p.y),'toe':min(flat,key=lambda p:p.y)}
def hermite(a,b,da,db,u):
    return (2*u**3-3*u*u+1)*a+(u**3-2*u*u+u)*da+(-2*u**3+3*u*u)*b+(u**3-u*u)*db
def foot_goal(phase,s):
    phi=(phase+(0 if s=='L' else .5))%1
    stance=phi<DUTY;lift=0
    if stance:
        q=phi/DUTY;base=.003-STRIDE/2+STRIDE*q
        pitch=-14*(1-smooth(0,.22,q))+28*smooth(.68,1,q)
    else:
        u=(phi-DUTY)/(1-DUTY)
        tangent=STRIDE/DUTY*(1-DUTY)
        base=.003+hermite(STRIDE/2,-STRIDE/2,tangent,tangent,u)
        lift=LIFT*math.sin(math.pi*u)**1.45
        pitch=28-42*smooth(0,1,u)
    r=world_rot(x=pitch)
    pivot=pivots[s]['heel' if pitch<0 else 'toe']
    # Roll about a stationary heel/toe during contact; translation removes treadmill travel.
    y=base+pivot.y-(r@pivot).y
    bottom=min((r@p).z for p in sole_points[s])
    return V((.092 if s=='L' else -.092,y,-bottom+lift)),r,stance,phi,lift
clamps=[]
def pose(phase):
    t=math.tau*phase;goals={s:foot_goal(phase,s) for s in ('L','R')}
    hip=V((.014*math.sin(t),-.006,1))
    hr=world_rot(y=1.5*math.sin(t),z=-6.5*math.cos(t))
    # Derive body height from the supporting leg instead of prescribing a low hip.
    # Long support leg, a brief yielding knee after contact, then a heel-raised push-off.
    heights=[]
    for s in ('L','R'):
        offset=hr@(arm.bones['Thigh.'+s].head_local-arm.bones['Hips'].head_local)
        ankle=goals[s][0];stance=goals[s][2];phi=goals[s][3]
        if stance:
            q=phi/DUTY
            bend=11+10*math.exp(-((q-.20)/.13)**2)+6*smooth(.70,1,q)
            l1,l2=lengths[s];reach=math.sqrt(l1*l1+l2*l2+2*l1*l2*math.cos(radians(bend)))
        else:reach=sum(lengths[s])*.997
        dx=hip.x+offset.x-ankle.x;dy=hip.y+offset.y-ankle.y
        heights.append(ankle.z+math.sqrt(reach*reach-dx*dx-dy*dy)-offset.z)
    # A smooth minimum avoids a snap when support transfers between the two feet.
    delta=heights[0]-heights[1]
    hip.z=(heights[0]+heights[1]-math.sqrt(delta*delta+.003**2))/2
    clamps.append(0)
    d={'Root':rest['Root'].copy(),'Hips':transform(hip,hr@rest['Hips'].to_3x3())}
    follow('Spine',d,world_rot(x=1.8+.7*math.sin(2*t-.20),y=.8*math.sin(t-.12),z=4.5*math.cos(t-.10)))
    follow('Chest',d,world_rot(x=.8+.5*math.sin(2*t-.40),y=.5*math.sin(t-.22),z=9*math.cos(t-.18)))
    follow('Neck',d)
    follow('Head',d,world_rot(x=-1.8-.6*math.sin(2*t-.60),y=-1.0*math.sin(t-.40),z=-2.5*math.cos(t-.35)))
    follow('Backpack',d,world_rot(x=1.1*math.sin(2*t-.60),z=.8*math.sin(t-.35)))
    for s,sign in [('L',1),('R',-1)]:
        swing=math.cos(t+(0 if s=='L' else math.pi))
        follow('Clavicle.'+s,d,world_rot(y=sign*.6))
        follow('UpperArm.'+s,d,world_rot(x=30*swing,y=sign*8))
        follow('Forearm.'+s,d,world_rot(x=-8-5*max(0,-swing)))
        hand=follow('Hand.'+s,d,world_rot(x=-1))
        d['Hand.'+s]=hand@Matrix.Rotation(radians(sign*25),4,'Y')
        follow('HandSocket.'+s,d)
        joint=(d['Hips']@rest['Hips'].inverted()@rest['Thigh.'+s]).translation
        ankle,fr,*_=goals[s];axis=ankle-joint;dist=axis.length;axis.normalize();l1,l2=lengths[s]
        assert abs(l1-l2)<dist<l1+l2
        along=(l1*l1-l2*l2+dist*dist)/(2*dist)
        forward=V((0,-1,0));bend=(forward-axis*forward.dot(axis)).normalized()
        knee=joint+axis*along+bend*math.sqrt(max(0,l1*l1-along*along))
        d['Thigh.'+s]=segment('Thigh.'+s,joint,knee)
        d['Shin.'+s]=segment('Shin.'+s,knee,ankle)
        d['Foot.'+s]=transform(ankle,fr@rest['Foot.'+s].to_3x3())
    return d,goals
bpy.data.actions.remove(bpy.data.actions['Walk'])
walk=bpy.data.actions.new('Walk');walk.use_fake_user=True;rig.animation_data.action=walk
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
for fc in walk.fcurves:
    for k in fc.keyframe_points:k.interpolation='LINEAR'
walk['fps']=30;walk['in_place']=True;walk['nominal_forward_speed_mps']=SPEED
walk['loop_end_duplicate']=True;walk['approval']='Review pending; only Walk changed'
walk['reference']='2026-09-09 second supplied walk reference: latter segment, full-body sway and delayed shoulders/head'
for label,f in [('LeftHeel',1),('LeftSupport',10),('RightHeel',19),('RightSupport',28),('Loop',37)]:
    mark=walk.pose_markers.new(label);mark.frame=f
if walk.slots:rig.animation_data.action_slot=walk.slots[0]
snaps={};bottom=100;contact_error=0;drift=0;airborne=0;last={}
for frame in range(1,COUNT+2):
    scene.frame_set(frame);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
    _,goals=pose((frame-1)/COUNT);coords=[]
    for o in meshes:
        ev=o.evaluated_get(dg);mesh=ev.to_mesh()
        if frame in (1,COUNT+1):coords.extend(v.co.copy() for v in mesh.vertices)
        if o==boot:
            for s in ('L','R'):
                ids=[v.index for v in o.data.vertices if (v.co.x>=0)==(s=='L')]
                support=min(ids,key=lambda i:mesh.vertices[i].co.z)
                low=mesh.vertices[support].co.z;bottom=min(bottom,low)
                stance=goals[s][2];phi=goals[s][3]
                if stance:
                    contact_error=max(contact_error,abs(low))
                    y=mesh.vertices[support].co.y-SPEED*(frame-1)/30
                    if s in last and last[s][0]==support and phi>last[s][2]:drift=max(drift,abs(y-last[s][1]))
                    last[s]=(support,y,phi)
                else:last.pop(s,None)
        ev.to_mesh_clear()
    if coords:snaps[frame]=coords
    if all(not goals[s][2] for s in ('L','R')):airborne+=1
seam=max((a-b).length for a,b in zip(snaps[1],snaps[COUNT+1]))
assert seam<1e-5 and bottom>-.00001 and contact_error<1e-5 and drift<.001 and airborne==0,(seam,bottom,contact_error,drift,airborne)
assert digest_motion()==original_motion and digest_shape()==original_shape
report={'frames':[1,37],'fps':30,'cycle_seconds':1.2,'nominal_forward_speed_mps':SPEED,
 'revision':2,'revision_note':'Support-leg-driven pelvis; visible opposite chest rotation, lateral body transfer, delayed head/backpack, toe push-off',
 'reference_file':'뭘만든거야_걷기부터_만들어달라니까.mp4','reference_section':'latter walking segment despite retained Run caption',
 'reference_cadence_note':'Interpreted from the reference, not a motion-capture reconstruction',
 'walk_only_changed':True,'other_five_actions_unchanged':True,'geometry_and_weights_unchanged':True,
 'loop_seam_m':seam,'minimum_sole_z_m':bottom,'max_contact_height_error_m':contact_error,
 'max_same_support_vertex_slide_per_frame_m':drift,'max_pelvis_reach_correction_m':max(clamps),'airborne_frames':airborne,
 'approval':'pending','unity_connected':False}
(OUT/'WalkCheck.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene.render.fps=30;scene.frame_start=1;scene.frame_end=37;scene.frame_set(1)
rig['stage']='Walk revision for approval; original Player production file unchanged.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CompactSurvivor_WalkReview.blend'))
print('WALK_REVIEW_READY',json.dumps(report),flush=True)
