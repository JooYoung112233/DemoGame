"""Fit approved modular silhouette to the existing rig and reuse accepted locomotion."""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector, Matrix
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivorReview/Stage02_FacePalette/DarkSurvivor_Stage02.blend'
MOTION=ROOT/'Assets/ChibiSurvivor/Player/ExplosiveRunReview/GazeReview/CompactSurvivor_RunGaze.blend'
SWORD_WALK=ROOT/'Assets/ChibiSurvivor/Player/TwoHandWalkReview/CompactSurvivor_TwoHandWalk.blend'
SWORD_SLASH=ROOT/'Assets/ChibiSurvivor/Player/TwoHandSlashReview/CompactSurvivor_TwoHandSlash.blend'
OUT=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor';OUT.mkdir(parents=True,exist_ok=True)
BLEND_SOURCE=OUT/'BlenderSource~';BLEND_SOURCE.mkdir(exist_ok=True)
CHECK=ROOT/'Library/CodexBlender/DarkSurvivorGame';CHECK.mkdir(parents=True,exist_ok=True)
hashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in (SOURCE,MOTION,SWORD_WALK,SWORD_SLASH)}
bpy.ops.wm.open_mainfile(filepath=str(MOTION));scene=bpy.context.scene
old=bpy.data.objects['CompactSurvivor_Rig']
for track in list(old.animation_data.nla_tracks):old.animation_data.nla_tracks.remove(track)
rest={b.name:b.matrix_local.copy() for b in old.data.bones}
parents={b.name:b.parent.name if b.parent else None for b in old.data.bones}
bone_data={b.name:(b.head_local.copy(),b.tail_local.copy(),b.use_deform) for b in old.data.bones}
samples={}
for dest,src,path in [('Idle','Idle',MOTION),('Walk','Walk',MOTION),('Run','Run_Explosive_Unarmed',MOTION),('SwordWalk','Walk_Explosive_TwoHandSword',SWORD_WALK),('SwordSlash','Attack_Explosive_TwoHandSlash',SWORD_SLASH)]:
 if path!=MOTION:
  bpy.ops.wm.open_mainfile(filepath=str(path));scene=bpy.context.scene;old=bpy.data.objects['CompactSurvivor_Rig']
  for track in list(old.animation_data.nla_tracks):old.animation_data.nla_tracks.remove(track)
 a=bpy.data.actions[src];old.animation_data.action=a
 if a.slots:old.animation_data.action_slot=a.slots[0]
 frames=[]
 for f in range(int(a.frame_range[0]),int(a.frame_range[1])+1):
  scene.frame_set(f);bpy.context.view_layer.update()
  boot=bpy.data.objects['Compact_Boots'].evaluated_get(bpy.context.evaluated_depsgraph_get())
  mesh=boot.to_mesh();bottom=min((boot.matrix_world@v.co).z for v in mesh.vertices);boot.to_mesh_clear()
  frames.append(( {b.name:(b.matrix.copy(),b.location.copy()) for b in old.pose.bones}, bottom))
 # Imported source endpoints can differ slightly; close the loop exactly.
 if dest!='SwordSlash':frames[-1]=frames[0]
 samples[dest]=frames
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));scene=bpy.context.scene
bpy.context.preferences.filepaths.save_version=0
parts=[o for o in scene.objects if o.type=='MESH' and o.name.startswith('Study_')]
for a in list(bpy.data.actions):bpy.data.actions.remove(a)
for o in parts:
 bpy.ops.object.select_all(action='DESELECT');o.hide_set(False);o.hide_render=False;o.select_set(True);bpy.context.view_layer.objects.active=o
 for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 o.vertex_groups.clear();o.name=o.name.replace('Study_','Hero_');o['review_only']=False
original={o.name:[v.co.copy() for v in o.data.vertices] for o in parts}
arm=bpy.data.armatures.new('DarkSurvivor_Skeleton');rig=bpy.data.objects.new('DarkSurvivor_Rig',arm);scene.collection.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
zanchors=[(0,0),(.129,.18),(.3375,.46),(.552,.78),(.64,.865),(.80,1.025),(.93,1.165),(.963,1.198),(1.235,1.47)]
def zfit(z):
 for (a,b),(c,d) in zip(zanchors,zanchors[1:]):
  if z<=c:return b+(z-a)*(d-b)/(c-a)
 return z+.235
def fit(p):return Vector((p.x*.103/.092,p.y,zfit(p.z)))
for name,(h,t,deform) in bone_data.items():
 b=arm.edit_bones.new(name);b.head=fit(h);b.tail=fit(t);b.use_deform=deform
 if parents[name]:b.parent=arm.edit_bones[parents[name]]
for side,s in [('L',1),('R',-1)]:
 shoulder=Vector((s*.178,0,1.145));elbow=Vector((s*.276,.002,.957));wrist=Vector((s*.315,-.101,.748));end=Vector((s*.321,-.108,.671))
 for name,h,t in [('Clavicle',Vector((s*.05,0,1.148)),shoulder),('UpperArm',shoulder,elbow),('Forearm',elbow,wrist),('Hand',wrist,end),('HandSocket',Vector((s*.321,-.105,.715)),Vector((s*.321,-.205,.715)))]:
  b=arm.edit_bones[name+'.'+side];b.head=h;b.tail=t
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True
newrest={b.name:b.matrix_local.copy() for b in arm.bones}
arm.bones['HandSocket.R'].use_deform=True
def smooth(a,b,z):
 t=max(0,min(1,(z-a)/(b-a)));return t*t*(3-2*t)
def torso(z):
 if z<.93:
  t=smooth(.79,.92,z);return {'Hips':1-t,'Spine':t}
 t=smooth(.94,1.08,z);return {'Spine':1-t,'Chest':t}
for o in parts:
 groups={b.name:o.vertex_groups.new(name=b.name) for b in arm.bones if b.use_deform}
 n=o.name
 for v in o.data.vertices:
  x,y,z=v.co;side='L' if x>0 else 'R'
  if n in ['Hero_Head','Hero_Cap','Hero_Hair','Hero_FaceDetails']:w={'Head':1}
  elif 'Hands' in n:w={'Hand.'+side:1}
  elif 'Neck' in n:w={'Neck':1}
  elif any(q in n for q in ['Belt','Lantern','Hips']):w={'Hips':1}
  elif any(q in n for q in ['Boot','Sole']):w={'Foot.'+side:1}
  elif 'TrouserCuff' in n:w={'Shin.'+side:1}
  elif 'TrouserLeg' in n:
   hip=smooth(.69,.80,z);thigh=smooth(.405,.515,z)*(1-hip)
   w={'Hips':hip,'Thigh.'+side:thigh,'Shin.'+side:1-hip-thigh}
  elif 'Forearm' in n:
   t=smooth(.93,.982,z);w={'Forearm.'+side:1-t,'UpperArm.'+side:t}
  elif 'RolledSleeve' in n:w={'UpperArm.'+side:1}
  elif 'Sleeve' in n:
   t=smooth(1.10,1.15,z)* (1-smooth(.15,.235,abs(x)))
   w={'UpperArm.'+side:1-t,'Chest':t}
  elif 'Backpack' in n:w={'Backpack':1}
  else:w=torso(z)
  for b,value in w.items():
   if value>1e-7:groups[b].add([v.index],value,'REPLACE')
 o.parent=rig;mod=o.modifiers.new('Hero_Skin','ARMATURE');mod.object=rig
 assert all(abs(sum(g.weight for g in v.groups)-1)<1e-5 for v in o.data.vertices)
rig.animation_data_create();scene.render.fps=30
for pb in rig.pose.bones:pb.rotation_mode='QUATERNION'
bpy.context.view_layer.update()
error=0
for o in parts:
 ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());em=ev.to_mesh()
 error=max(error,max((v.co-original[o.name][v.index]).length for v in em.vertices));ev.to_mesh_clear()
assert error<1e-5
grip_errors=[]
def fit_sword_grip(pose):
 axis=sum((pose['HandSocket.'+s][0].to_quaternion()@Vector((0,1,0)) for s in ['R','L']),Vector()).normalized()
 socketq=axis.to_track_quat('Y','Z');gap=.16
 scale=(arm.bones['UpperArm.R'].length+arm.bones['Forearm.R'].length)/((bone_data['UpperArm.R'][1]-bone_data['UpperArm.R'][0]).length+(bone_data['Forearm.R'][1]-bone_data['Forearm.R'][0]).length)
 anchor=rig.pose.bones['Chest'].head+(pose['HandSocket.R'][0].translation-pose['Chest'][0].translation)*scale
 rels={s:newrest['Hand.'+s].inverted()@newrest['HandSocket.'+s] for s in ['R','L']}
 handqs={s:socketq@rels[s].to_quaternion().inverted() for s in ['R','L']}
 for _ in range(40):
  for s in ['R','L']:
   off=(axis*gap if s=='L' else Vector())+handqs[s]@rels[s].translation
   center=rig.pose.bones['UpperArm.'+s].head+off;d=anchor-center
   limit=(arm.bones['UpperArm.'+s].length+arm.bones['Forearm.'+s].length)*.98
   if d.length>limit:anchor=center+d.normalized()*limit
 for s in ['R','L']:
  upper=rig.pose.bones['UpperArm.'+s];fore=rig.pose.bones['Forearm.'+s];hand=rig.pose.bones['Hand.'+s]
  shoulder=upper.head.copy();wrist=anchor-(axis*gap if s=='L' else Vector())-handqs[s]@rels[s].translation
  delta=wrist-shoulder;dist=delta.length;direction=delta.normalized();a=upper.bone.length;b=fore.bone.length
  along=(a*a-b*b+dist*dist)/(2*dist);pole=fore.head-shoulder;pole-=direction*pole.dot(direction)
  if pole.length<.001:pole=Vector((1 if s=='L' else -1,0,0));pole-=direction*pole.dot(direction)
  elbow=shoulder+direction*along+pole.normalized()*math.sqrt(max(0,a*a-along*along))
  for pb,h,t in [(upper,shoulder,elbow),(fore,elbow,wrist)]:
   q=pb.matrix.to_quaternion();q=(q@Vector((0,1,0))).rotation_difference(t-h)@q
   m=q.to_matrix().to_4x4();m.translation=h;pb.matrix=m;bpy.context.view_layer.update()
  m=handqs[s].to_matrix().to_4x4();m.translation=wrist;hand.matrix=m
  rig.pose.bones['HandSocket.'+s].matrix_basis=Matrix.Identity(4);bpy.context.view_layer.update()
 grip_errors.append((rig.pose.bones['HandSocket.L'].head-(rig.pose.bones['HandSocket.R'].head-axis*gap)).length)
clips=[]
for name,frames in samples.items():
 a=bpy.data.actions.new(name);a.use_fake_user=True;rig.animation_data.action=a
 previous={};footmin=[]
 for frame,(pose,oldbottom) in enumerate(frames,1):
  scene.frame_set(frame)
  for pb in rig.pose.bones:
   n=pb.name;p=parents[n]
   follow=(rig.pose.bones[p].matrix@newrest[p].inverted()@newrest[n]) if p else newrest[n].copy()
   follow.translation+=follow.to_3x3()@pose[n][1]*1.3
   q=(pose[n][0].to_quaternion()@rest[n].to_quaternion().inverted())@newrest[n].to_quaternion()
   m=q.to_matrix().to_4x4();m.translation=follow.translation;pb.matrix=m
   bpy.context.view_layer.update()
  if name.startswith('Sword'):fit_sword_grip(pose)
  bpy.context.view_layer.update()
  bottom=100
  for o in parts:
   if 'Sole' not in o.name:continue
   ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());em=ev.to_mesh();bottom=min(bottom,min((ev.matrix_world@v.co).z for v in em.vertices));ev.to_mesh_clear()
  h=rig.pose.bones['Hips'];m=h.matrix.copy();m.translation.z+=max(0,oldbottom)*1.3-bottom;h.matrix=m
  bpy.context.view_layer.update()
  for pb in rig.pose.bones:
   q=pb.rotation_quaternion
   if pb.name in previous and q.dot(previous[pb.name])<0:pb.rotation_quaternion=-q
   previous[pb.name]=pb.rotation_quaternion.copy()
   pb.keyframe_insert('location',frame=frame,group=pb.name);pb.keyframe_insert('rotation_quaternion',frame=frame,group=pb.name);pb.keyframe_insert('scale',frame=frame,group=pb.name)
 for fc in a.fcurves:
  for k in fc.keyframe_points:k.interpolation='LINEAR'
 clips.append({'name':name,'frames':len(frames),'seconds':(len(frames)-1)/30,'loop':name!='SwordSlash'})
assert max(grip_errors)<.001, max(grip_errors)
# Removable proxy sword uses the already established simple handle/guard/blade design.
steel=bpy.data.materials.new('Hero_WeaponSteel');steel.use_nodes=True
steel.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.23,.25,.25,1)
steel.node_tree.nodes['Principled BSDF'].inputs['Metallic'].default_value=.65
steel.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.6
pieces=[]
for label,pos,size,mat in [('Handle',(0,-.08,0),(.035,.24,.035),bpy.data.materials['Hero_leather']),('Guard',(0,.065,0),(.12,.016,.025),steel),('Blade',(0,.35,0),(.055,.55,.016),steel)]:
 bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.name=label
 o.location=pos;o.dimensions=size;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 o.data.materials.append(mat);pieces.append(o)
bpy.ops.object.select_all(action='DESELECT')
for o in pieces:o.select_set(True)
bpy.context.view_layer.objects.active=pieces[0];bpy.ops.object.join();sword=bpy.context.object;sword.name='Hero_SwordProxy'
for v in sword.data.vertices:v.co=newrest['HandSocket.R']@v.co
group=sword.vertex_groups.new(name='HandSocket.R');group.add(list(range(len(sword.data.vertices))),1,'REPLACE')
sword.parent=rig;mod=sword.modifiers.new('SwordSocket','ARMATURE');mod.object=rig;parts.append(sword)
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_start=1;scene.frame_end=len(samples['Idle']);scene.frame_set(1)
rig['animation_source']=str(MOTION);rig['equipment_editing']='Separate skinned meshes; shared editable material palette.'
materials=[]
for o in parts:
 for m in o.data.materials:
  if not m or any(x['name']==m.name for x in materials):continue
  p=m.node_tree.nodes.get('Principled BSDF') if m.use_nodes else None
  materials.append({'name':m.name,'color':list(p.inputs['Base Color'].default_value if p else m.diffuse_color),'roughness':p.inputs['Roughness'].default_value if p else .8,'metallic':p.inputs['Metallic'].default_value if p else 0,'emission':list(p.inputs['Emission Color'].default_value) if p else [0,0,0,1],'emissionStrength':p.inputs['Emission Strength'].default_value if p else 0})
(OUT/'Palette.json').write_text(json.dumps({'materials':materials},indent=2))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=rig
# Only the character and its skeleton go into the game FBX; studio assets remain in Blender.
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_SOURCE/'DarkSurvivor.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'DarkSurvivor.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest()==h for p,h in hashes.items())
report={'rest_shape_error':error,'source_unchanged':True,'mesh_parts':len(parts),'bones':len(arm.bones),'clips':clips,'max_sword_support_hand_error':max(grip_errors),'triangles':sum(len(p.vertices)-2 for o in parts for p in o.data.polygons)}
(CHECK/'ExportCheck.json').write_text(json.dumps(report,indent=2));print('GAME_EXPORT',json.dumps(report),flush=True)
sword.hide_render=True
scene.render.resolution_x=640;scene.render.resolution_y=800;scene.cycles.samples=12;scene.render.filepath=str(CHECK/'Idle.png');bpy.ops.render.render(write_still=True)
rig.animation_data.action=bpy.data.actions['Run'];scene.frame_set(7);scene.render.filepath=str(CHECK/'Run.png');bpy.ops.render.render(write_still=True)
sword.hide_render=False
for name,frame in [('SwordWalk',6),('SwordSlash',18)]:
 rig.animation_data.action=bpy.data.actions[name];scene.frame_set(frame);scene.render.filepath=str(CHECK/(name+'.png'));bpy.ops.render.render(write_still=True)
