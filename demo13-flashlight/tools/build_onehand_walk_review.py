"""One-handed sword carry over the package guard walk; preserve all other actions."""
import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/ChibiSurvivor/Player/ExplosiveWalkReview'
OUT=ROOT/'Assets/ChibiSurvivor/Player/OneHandWalkReview';OUT.mkdir(parents=True,exist_ok=True)
CACHE=ROOT/'Library/CodexBlender/OneHandWalkReview';CACHE.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'CompactSurvivor_ExplosiveWalk.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig']
original_actions=list(bpy.data.actions);characters=[o for o in scene.objects if o.type=='MESH']
def digest():
 return hashlib.sha256(repr([(a.name,[(c.data_path,c.array_index,[(tuple(k.co),k.interpolation) for k in c.keyframe_points]) for c in a.fcurves]) for a in original_actions]).encode()).hexdigest()
before=digest();source=bpy.data.actions['Walk_Explosive_StrafeForward'];action=source.copy();action.name='Walk_OneHand_Sword';action.use_fake_user=True
rig.animation_data.action=action
if action.slots:rig.animation_data.action_slot=action.slots[0]
count=round(action.frame_range[1]-action.frame_range[0]);end=count+1
modified={'UpperArm.R','Forearm.R','Hand.R'}
def unaffected():
 return [(c.data_path,c.array_index,[(tuple(k.co),k.interpolation) for k in c.keyframe_points]) for c in action.fcurves if not any(c.data_path==f'pose.bones["{n}"].rotation_quaternion' for n in modified)]
protected=repr(unaffected());rest={b.name:b.matrix_local.copy() for b in rig.data.bones}
socket_rel=rest['Hand.R'].inverted()@rest['HandSocket.R'];samples=[]
for f in range(1,end+1):
 scene.frame_set(f);bpy.context.view_layer.update()
 chest=rig.pose.bones['Chest'].matrix.copy();delta=chest.to_quaternion()@rest['Chest'].to_quaternion().inverted()
 wrist=delta.inverted()@(rig.pose.bones['Hand.R'].head-chest.translation)
 samples.append((chest,delta,wrist,rig.pose.bones['UpperArm.R'].head.copy(),{n:rig.pose.bones[n].matrix.to_quaternion().copy() for n in modified}))
mean=sum((s[2] for s in samples[:-1]),Vector())/count
prev={}
def set_world_rotation(name,q,f):
 p=rig.pose.bones[name]
 local=rest[name].to_quaternion().inverted()@rest[p.parent.name].to_quaternion()@p.parent.matrix.to_quaternion().inverted()@q
 if name in prev and prev[name].dot(local)<0:local.negate()
 prev[name]=local.copy();p.rotation_mode='QUATERNION';p.rotation_quaternion=local;p.keyframe_insert('rotation_quaternion',frame=f,group=name);bpy.context.view_layer.update()
for f,(chest,delta,wrist,shoulder,oldq) in enumerate(samples,1):
 scene.frame_set(f);bpy.context.view_layer.update()
 # Editable carry target: hand ahead of right hip, blade outside the face.
 local_wrist=Vector((-.30,-.23,-.045))+(wrist-mean)*.25
 target=chest.translation+delta@local_wrist
 upper=rig.data.bones['UpperArm.R'].length;lower=rig.data.bones['Forearm.R'].length
 line=target-shoulder;distance=line.length;axis=line.normalized()
 assert abs(upper-lower)<distance<upper+lower,('Unreachable wrist',f,distance)
 along=(upper*upper-lower*lower+distance*distance)/(2*distance)
 pole=delta@Vector((-1,.12,-.1));pole=(pole-axis*pole.dot(axis)).normalized()
 elbow=shoulder+axis*along+pole*math.sqrt(max(0,upper*upper-along*along))
 for name,direction in [('UpperArm.R',elbow-shoulder),('Forearm.R',target-elbow)]:
  q=oldq[name];aim=(q@Vector((0,1,0))).rotation_difference(direction)
  set_world_rotation(name,aim@q,f)
 blade_direction=delta@Vector((-.13,-.75,.65)).normalized()
 socket_q=blade_direction.to_track_quat('Y','Z')
 set_world_rotation('Hand.R',socket_q@socket_rel.to_quaternion().inverted(),f)
for c in action.fcurves:
 if any(c.data_path==f'pose.bones["{n}"].rotation_quaternion' for n in modified):
  for k in c.keyframe_points:k.interpolation='LINEAR'
assert before==digest(),'Existing action changed'
assert protected==repr(unaffected()),'A channel outside the right arm changed'
# Separate, replaceable sword proxy. Its only attachment is the existing socket.
collection=bpy.data.collections.new('PREVIEW_ONLY_OneHandSword');scene.collection.children.link(collection)
grip=bpy.data.objects.new('Preview_Sword_Grip',None);collection.objects.link(grip)
c=grip.constraints.new('COPY_TRANSFORMS');c.target=rig;c.subtarget='HandSocket.R'
grip['purpose']='Removable sword proxy for animation approval; not a production weapon asset'
def material(name,color,metal=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;s=m.node_tree.nodes['Principled BSDF'];s.inputs['Base Color'].default_value=(*color,1);s.inputs['Roughness'].default_value=.7;s.inputs['Metallic'].default_value=metal;return m
steel=material('Review_Steel',(.27,.30,.29),.45);edge=material('Review_Edge',(.44,.47,.46),.45);wood=material('Review_Grip',(.075,.040,.021))
def box(name,location,size,mat):
 bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.name=name;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for col in list(o.users_collection):col.objects.unlink(o)
 collection.objects.link(o);o.parent=grip;o.location=location;o.data.materials.append(mat);return o
box('Preview_Sword_Handle',(0,0,0),(.03,.095,.027),wood)
guard=box('Preview_Sword_Guard',(0,.057,0),(.095,.012,.025),steel)
outline=[(-.025,.065),(.025,.065),(.022,.375),(0,.435),(-.022,.375)];n=len(outline)
verts=[(x,y,z) for z in [-.004,.004] for x,y in outline];faces=[tuple(reversed(range(n))),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
mesh=bpy.data.meshes.new('ReviewSword');mesh.from_pydata(verts,[],faces);mesh.update();blade=bpy.data.objects.new('Preview_Sword_Blade',mesh);collection.objects.link(blade);blade.parent=grip;mesh.materials.append(steel);mesh.materials.append(edge)
for face in mesh.polygons:
 if face.index>1:face.material_index=1
def tree(o,dg):
 ev=o.evaluated_get(dg);m=ev.to_mesh();b=BVHTree.FromPolygons([ev.matrix_world@v.co for v in m.vertices],[tuple(p.vertices) for p in m.polygons]);ev.to_mesh_clear();return b
hits=[];minblade=100
for f in range(1,end+1):
 scene.frame_set(f);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
 minblade=min(minblade,min((blade.matrix_world@v.co).z for v in blade.data.vertices))
 bt=tree(blade,dg);gt=tree(guard,dg)
 for o in characters:
  ct=tree(o,dg)
  if bt.overlap(ct) or (o.name!='Compact_Hands' and gt.overlap(ct)):hits.append([f,o.name])
assert not hits,('Weapon intersects character',hits)
assert minblade>0,('Blade intersects floor',minblade)
stats={'source':'ExplosiveLLC Unarmed-Strafe-Forward with authored right-arm sword carry','action':action.name,'frames':[1,end],'fps':30,'seconds':count/30,'head_gaze_adjusted':True,'geometry_and_weights_unchanged':True,'existing_actions_preserved':True,'unchanged_actions_digest':before,'lower_body_and_left_arm_unchanged':True,'changed_bones':sorted(modified),'weapon_surface_intersections':hits,'min_blade_height_m':minblade,'approval':'pending','humanoid_validated':False,'preview_description':'Blender guard-walk adaptation: source gait with authored right-arm sword carry and removable sword proxy.'}
(OUT/'RetargetCheck.json').write_text(json.dumps(stats,indent=2))
scene.frame_start=1;scene.frame_end=count;scene.frame_set(1)
rig['review_status']='One-hand sword walk pending approval; approved unarmed motions preserved'
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CompactSurvivor_OneHandWalk.blend'))
scene.camera.location=(-4.8,-7,1.9);scene.camera.rotation_euler=(Vector((0,0,.79))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=1.98
scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=24;scene.render.resolution_x=480;scene.render.resolution_y=600;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
scene.frame_set(7);scene.render.filepath=str(CACHE/'Pose.png');bpy.ops.render.render(write_still=True)
print('ONE_HAND_WALK_READY',json.dumps(stats),flush=True)
