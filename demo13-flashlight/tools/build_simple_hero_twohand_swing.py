"""Stage 5: adapt the existing two-hand slash, keeping approved locomotion keys."""
import bpy,math,json,hashlib,sys
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
sys.path.insert(0,str(ROOT/'tools'))
from character_source_paths import source_path
SOURCE=source_path('Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend')
BASE=source_path('Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_Stage4_Locomotion.blend')
kevin=True
if kevin:
 SOURCE=ROOT/'Library/KevinMeleeSource/Mapped_Attack2H.blend'
 OUT=ROOT.parent/'ArtWork/SimpleHeroMelee';OUT.mkdir(exist_ok=True,parents=True);(OUT/'BlenderSource~').mkdir(exist_ok=True)
hashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in [SOURCE,BASE]}
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));sc=bpy.context.scene;old=bpy.data.objects['DarkSurvivor_Rig']
for t in list(old.animation_data.nla_tracks):old.animation_data.nla_tracks.remove(t)
action=bpy.data.actions['SwordSlash'];old.animation_data.action=action
if action.slots:old.animation_data.action_slot=action.slots[0]
oldrest={b.name:b.matrix_local.copy() for b in old.data.bones}
oldarm=old.data.bones['UpperArm.R'].length+old.data.bones['Forearm.R'].length
samples=[]
source_end=int(action.frame_range[1]);sample_step=.125
for k in range((source_end-1)*8+1):
 f=1+k*sample_step;sc.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update()
 samples.append({b.name:(b.matrix.copy(),b.location.copy()) for b in old.pose.bones})
bpy.ops.wm.open_mainfile(filepath=str(BASE));bpy.context.preferences.filepaths.save_version=0
sc=bpy.context.scene;rig=bpy.data.objects['SimpleHero_Rig'];arm=rig.data
parts=[o for o in sc.objects if o.type=='MESH' and o.name.startswith(('Study_','Gear_'))]
rest={b.name:b.matrix_local.copy() for b in arm.bones}
def digest(a):return hashlib.sha256(repr([(c.data_path,c.array_index,[(tuple(k.co),k.interpolation) for k in c.keyframe_points]) for c in a.fcurves]).encode()).hexdigest()
preserved={a.name:digest(a) for a in bpy.data.actions}
for a in bpy.data.actions:a.use_fake_user=True
action=bpy.data.actions.new('SwordSlash');action.use_fake_user=True;rig.animation_data.action=action
scale=(arm.bones['UpperArm.R'].length+arm.bones['Forearm.R'].length)/oldarm
legscale=(arm.bones['Thigh.R'].length+arm.bones['Shin.R'].length)/((oldrest['Shin.R'].translation-oldrest['Thigh.R'].translation).length+(oldrest['Foot.R'].translation-oldrest['Shin.R'].translation).length)
gap=.15;errors=[];reaches=[];previous={}
def fit(pose):
 axis=(pose['HandSocket.R'][0].to_quaternion()@Vector((0,1,0))).normalized()
 # Source roll stays continuous when the blade crosses the vertical direction.
 q=pose['HandSocket.R'][0].to_quaternion();socketq=(q@Vector((0,1,0))).rotation_difference(axis)@q
 headq=rig.pose.bones['Head'].matrix.to_quaternion()@rest['Head'].to_quaternion().inverted()
 forward=headq@Vector((0,-1,0));forward.z=0;forward.normalize()
 chestq=rig.pose.bones['Chest'].matrix.to_quaternion()@rest['Chest'].to_quaternion().inverted()
 chestforward=chestq@Vector((0,-1,0));chestforward.z=0;chestforward.normalize()
 anchor=rig.pose.bones['Chest'].head+(pose['HandSocket.R'][0].translation-pose['Chest'][0].translation)*scale
 # Keep the handle ahead of the jacket while retaining lateral swing travel.
 relative=chestq.inverted()@(anchor-rig.pose.bones['Chest'].head)
 relative.y=min(relative.y,-.29)
 anchor=rig.pose.bones['Chest'].head+chestq@relative

 rels={s:rest['Hand.'+s].inverted()@rest['HandSocket.'+s] for s in ['R','L']}
 dg=bpy.context.evaluated_depsgraph_get();headpoints=[]
 for obj in parts:
  if obj.name not in ['Study_Head','Study_Cap','Study_CapBrim']:continue
  ev=obj.evaluated_get(dg);me=ev.to_mesh()
  headpoints.extend(headq.inverted()@(ev.matrix_world@v.co) for v in me.vertices);ev.to_mesh_clear()
 lo=Vector(tuple(min(p[i] for p in headpoints) for i in range(3)));hi=Vector(tuple(max(p[i] for p in headpoints) for i in range(3)))
 headcenter=headq@((lo+hi)*.5);radii=(hi-lo)*.62+Vector((.075,.075,.075))
 for avoidance in range(24):
  handqs={s:socketq@rels[s].to_quaternion().inverted() for s in ['R','L']}
  for s in ['R','L']:
   off=-(handqs[s]@rels[s].translation);off-=axis*off.dot(axis)
   toward=rig.pose.bones['UpperArm.'+s].head-anchor;toward-=axis*toward.dot(axis)
   if off.length>.001 and toward.length>.001:
    off.normalize();toward.normalize();angle=math.atan2(axis.dot(off.cross(toward)),off.dot(toward))
    handqs[s]=Quaternion(axis,angle)@handqs[s]
  original=anchor.copy()
  for _ in range(60):
   for s in ['R','L']:
    off=(axis*gap if s=='L' else Vector())+handqs[s]@rels[s].translation
    center=rig.pose.bones['UpperArm.'+s].head+off;d=anchor-center
    limit=(arm.bones['UpperArm.'+s].length+arm.bones['Forearm.'+s].length)*.97
    if d.length>limit:anchor=center+d.normalized()*limit
  reaches.append((anchor-original).length)
  # Closest point on the blade segment in a head-aligned safety ellipsoid.
  local=headq.inverted()@(anchor-headcenter);direction=headq.inverted()@axis
  scaled=Vector(tuple(local[i]/radii[i] for i in range(3)))
  ray=Vector(tuple(direction[i]/radii[i] for i in range(3)))
  t=max(.125,min(.87,-scaled.dot(ray)/ray.length_squared))
  near=scaled+ray*t;distance=near.length
  if distance>=1.06:break
  gradient=headq@Vector(tuple(near[i]/radii[i] for i in range(3)))
  gradient.normalize()
  clearaxis=(axis+gradient*(1.06-distance)*1.8).normalized()
  socketq=axis.rotation_difference(clearaxis)@socketq;axis=clearaxis
 for s in ['R','L']:
  upper=rig.pose.bones['UpperArm.'+s];fore=rig.pose.bones['Forearm.'+s];hand=rig.pose.bones['Hand.'+s]
  shoulder=upper.head.copy();wrist=anchor-(axis*gap if s=='L' else Vector())-handqs[s]@rels[s].translation
  delta=wrist-shoulder;dist=delta.length;direction=delta.normalized();a=upper.bone.length;b=fore.bone.length
  assert abs(a-b)<dist<a+b,(s,dist,a+b)
  along=(a*a-b*b+dist*dist)/(2*dist)
  chestq=rig.pose.bones['Chest'].matrix.to_quaternion()@rest['Chest'].to_quaternion().inverted()
  pole=chestq@Vector((.7 if s=='L' else -.7,.35,-.6));pole-=direction*pole.dot(direction)
  if pole.length<.001:pole=Vector((1 if s=='L' else -1,0,0));pole-=direction*pole.dot(direction)
  elbow=shoulder+direction*along+pole.normalized()*math.sqrt(max(0,a*a-along*along))
  for pb,h,t in [(upper,shoulder,elbow),(fore,elbow,wrist)]:
   q=chestq@rest[pb.name].to_quaternion();q=(q@Vector((0,1,0))).rotation_difference(t-h)@q
   m=q.to_matrix().to_4x4();m.translation=h;pb.matrix=m;bpy.context.view_layer.update()
  m=handqs[s].to_matrix().to_4x4();m.translation=wrist;hand.matrix=m
  rig.pose.bones['HandSocket.'+s].matrix_basis=Matrix.Identity(4);bpy.context.view_layer.update()
 errors.append((rig.pose.bones['HandSocket.L'].head-(rig.pose.bones['HandSocket.R'].head-axis*gap)).length)
for sample_index,pose in enumerate(samples):
 frame=1+sample_index*sample_step;sc.frame_set(int(frame),subframe=frame-int(frame))
 for pb in rig.pose.bones:
  n=pb.name;p=pb.parent
  follow=p.matrix@rest[p.name].inverted()@rest[n] if p else rest[n].copy()
  follow.translation+=follow.to_3x3()@pose[n][1]*legscale
  deformation=pose[n][0].to_quaternion()@oldrest[n].to_quaternion().inverted()
  def pitch_limit(q,degrees):
   d=q@Vector((0,-1,0));limit=-math.sin(math.radians(degrees))
   if d.z>=limit:return q
   flat=Vector((d.x,d.y,0)).normalized()*math.cos(math.radians(degrees));flat.z=limit
   return d.rotation_difference(flat)@q
  if n=='Chest':deformation=pitch_limit(deformation,28)
  if n=='Head':deformation=pitch_limit(deformation,10)
  if n in ['Hips','Spine']:
   h=pose['Hips'][0].to_quaternion()@oldrest['Hips'].to_quaternion().inverted()
   c=pitch_limit(pose['Chest'][0].to_quaternion()@oldrest['Chest'].to_quaternion().inverted(),28)
   deformation=h.slerp(c,.12 if n=='Hips' else .5)
  q=deformation@rest[n].to_quaternion();m=q.to_matrix().to_4x4();m.translation=follow.translation
  pb.matrix=m;bpy.context.view_layer.update()
 fit(pose)
 dg=bpy.context.evaluated_depsgraph_get();bottom=100
 for o in parts:
  if not o.name.startswith('Study_Boot'):continue
  ev=o.evaluated_get(dg);me=ev.to_mesh();bottom=min(bottom,min((ev.matrix_world@v.co).z for v in me.vertices));ev.to_mesh_clear()
 hip=rig.pose.bones['Hips'];m=hip.matrix.copy();m.translation.z-=bottom;hip.matrix=m;bpy.context.view_layer.update()
 for pb in rig.pose.bones:
  if pb.name in previous and pb.rotation_quaternion.dot(previous[pb.name])<0:pb.rotation_quaternion=-pb.rotation_quaternion
  previous[pb.name]=pb.rotation_quaternion.copy()
  for prop in ['location','rotation_quaternion','scale']:pb.keyframe_insert(prop,frame=frame,group=pb.name)
for fc in action.fcurves:
 for k in fc.keyframe_points:k.interpolation='LINEAR'
assert max(errors)<.001
assert all(digest(bpy.data.actions[n])==h for n,h in preserved.items())
# Removable review weapon; character FBX deliberately exports only the character.
def material(name,col,metal=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*col,1);p.inputs['Roughness'].default_value=.72;p.inputs['Metallic'].default_value=metal;return m
steel=material('Review_SwordSteel',(.22,.24,.25),.4);leather=material('Review_SwordLeather',(.065,.04,.022))
proxy=[]
for label,pos,size,mat in [('Grip',(0,-.07,0),(.043,.255,.043),leather),('Guard',(0,.14,0),(.12,.025,.04),steel),('Blade',(0,.507,0),(.065,.70,.022),steel)]:
 bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.name='Review_Sword_'+label;o.location=pos;o.dimensions=size
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mat)
 for v in o.data.vertices:v.co=rest['HandSocket.R']@v.co
 g=o.vertex_groups.new(name='HandSocket.R');g.add(list(range(len(o.data.vertices))),1,'REPLACE')
 o.parent=rig;mod=o.modifiers.new('WeaponSocket','ARMATURE');mod.object=rig;proxy.append(o)
arm.bones['HandSocket.R'].use_deform=True
def tree(o,dg):
 ev=o.evaluated_get(dg);me=ev.to_mesh();t=BVHTree.FromPolygons([ev.matrix_world@v.co for v in me.vertices],[tuple(p.vertices) for p in me.polygons]);ev.to_mesh_clear();return t
hits=[];suberrors=[]
for k in range((source_end-1)*16+1):
 f=1+k*.0625;sc.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
 suberrors.append((rig.pose.bones['HandSocket.L'].head-rig.pose.bones['HandSocket.R'].matrix@Vector((0,-gap,0))).length)
 for weapon in proxy[1:]:
  wt=tree(weapon,dg)
  for o in parts:
   if o.name.startswith('Study_Mitten'):continue
   if wt.overlap(tree(o,dg)):hits.append([f,weapon.name,o.name])
report={'source':str(SOURCE),'action':'SwordSlash','frames':source_end,'bake_step':sample_step,'seconds':(source_end-1)/30,'loop':False,'grip_spacing_m':gap,'max_grip_error_m':max(errors),'max_subframe_grip_error_m':max(suberrors),'max_reach_adjustment_m':max(reaches),'weapon_intersections':hits,'previous_action_keys_preserved':True,'gameplay_applied':False}
(OUT/'Stage5SlashCheck.json').write_text(json.dumps(report,indent=2));print('SLASH_CHECK', {**report, 'weapon_intersections':hits[:10], 'intersection_count':len(hits)},flush=True)
assert max(suberrors)<.008,('Interpolated grip detached',max(suberrors))
if '--draft' not in sys.argv:assert not hits,('Weapon intersects character',hits[:8])
if '--check-only' in sys.argv:raise SystemExit(0)
sc.render.fps=30;sc.frame_start=1;sc.frame_end=source_end;sc.frame_set(1)
rig['motion_stage']='Idle, Walk, Run, SwordSlash; review only'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/SimpleHero_Stage5_Slash.blend'))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'SimpleHero_Stage5_Slash.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_step=sample_step,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest()==h for p,h in hashes.items())
sc.camera.data.ortho_scale=3.1;sc.camera.location=(-4.8,-7,2.7);sc.camera.rotation_euler=(Vector((0,-.1,.9))-sc.camera.location).to_track_quat('-Z','Y').to_euler()
sc.render.resolution_x=800;sc.render.resolution_y=800;sc.render.resolution_percentage=100;sc.eevee.taa_render_samples=64;sc.render.image_settings.file_format='PNG'
for f in [1,9,17,21,29,41]:
 sc.frame_set(f);sc.render.filepath=str(OUT/f'Stage5_Slash_{f:03}.png');bpy.ops.render.render(write_still=True)
print('SLASH_READY',flush=True)
if '--video' in sys.argv:
 sc.camera.location=(4.8,-7,2.7);sc.camera.rotation_euler=(Vector((0,-.1,.9))-sc.camera.location).to_track_quat('-Z','Y').to_euler()
 sc.render.image_settings.file_format='FFMPEG';sc.render.ffmpeg.format='MPEG4';sc.render.ffmpeg.codec='H264';sc.render.ffmpeg.constant_rate_factor='HIGH'
 sc.render.filepath=str(OUT/'Kevin_Attack2H01.mp4');sc.frame_start=1;sc.frame_end=source_end;bpy.ops.render.render(animation=True);print('KEVIN_REVIEW_READY',flush=True)
