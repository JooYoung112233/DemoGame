"""Stage 3: fit existing hero skeleton and retarget only its approved Idle clip."""
import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
SOURCE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend';MODEL=OUT/'BlenderSource~/SimpleHero_Stage2.blend'
hashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in [SOURCE,MODEL]}
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));sc=bpy.context.scene;old=bpy.data.objects['DarkSurvivor_Rig'];old.animation_data.action=bpy.data.actions['Idle']
for track in list(old.animation_data.nla_tracks):old.animation_data.nla_tracks.remove(track)
source_fps=sc.render.fps;frame_range=tuple(map(int,bpy.data.actions['Idle'].frame_range))
bones=[{'name':b.name,'parent':b.parent.name if b.parent else None,'deform':b.use_deform,'head':b.head_local.copy(),'tail':b.tail_local.copy(),'z_axis':b.matrix_local.to_3x3().col[2].copy()} for b in old.data.bones]
samples=[]
for frame in range(frame_range[0],frame_range[1]+1):
 sc.frame_set(frame);bpy.context.view_layer.update();samples.append({b.name:(b.rotation_quaternion.copy(),b.location.copy()) for b in old.pose.bones})
samples[-1]=samples[0]
bpy.ops.wm.open_mainfile(filepath=str(MODEL));sc=bpy.context.scene;bpy.context.preferences.filepaths.save_version=0
parts=[o for o in sc.objects if o.type=='MESH' and o.name.startswith(('Study_','Gear_'))]
original={o.name:([v.co.copy() for v in o.data.vertices],o.matrix_world.copy(),[tuple(u.uv) for u in o.data.uv_layers.active.data]) for o in parts}
for a in list(bpy.data.actions):bpy.data.actions.remove(a)
arm=bpy.data.armatures.new('SimpleHero_Skeleton');rig=bpy.data.objects.new('SimpleHero_Rig',arm);sc.collection.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
fit={'Root':((0,0,0),(0,0,.10)),'Hips':((0,.01,.60),(0,0,.72)),'Spine':((0,0,.72),(0,0,.91)),'Chest':((0,0,.91),(0,0,1.065)),'Neck':((0,0,1.065),(0,0,1.145)),'Head':((0,0,1.145),(0,0,1.49)),'Backpack':((0,.242,1.02),(0,.242,1.12))}
for side,s in [('L',1),('R',-1)]:
 shoulder=(s*.205,0,1.065);elbow=(s*.295,-.003,.835);wrist=(s*.345,-.016,.637)
 fit.update({'Clavicle.'+side:((s*.045,0,1.058),shoulder),'UpperArm.'+side:(shoulder,elbow),'Forearm.'+side:(elbow,wrist),'Hand.'+side:(wrist,(s*.357,-.025,.543)),'HandSocket.'+side:((s*.354,-.025,.566),(s*.354,-.125,.566)),'Thigh.'+side:((s*.117,.01,.60),(s*.123,-.008,.365)),'Shin.'+side:((s*.123,-.008,.365),(s*.126,.005,.175)),'Foot.'+side:((s*.126,.005,.175),(s*.126,-.16,.07))})
for row in bones:
 b=arm.edit_bones.new(row['name']);b.head,b.tail=fit[row['name']];b.use_deform=row['deform'];b.align_roll(row['z_axis'])
 if row['parent']:b.parent=arm.edit_bones[row['parent']]
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True;arm.display_type='OCTAHEDRAL'
rest={b.name:b.matrix_local.copy() for b in arm.bones}
def smooth(a,b,z):
 t=max(0,min(1,(z-a)/(b-a)));return t*t*(3-2*t)
def torso(z):
 if z<.86:
  t=smooth(.65,.82,z);return {'Hips':1-t,'Spine':t}
 t=smooth(.86,1.015,z);return {'Spine':1-t,'Chest':t}
def components(m):
 adj=[set() for _ in m.vertices]
 for e in m.edges:a,b=e.vertices;adj[a].add(b);adj[b].add(a)
 pending=set(range(len(adj)));result=[]
 while pending:
  i=pending.pop();stack=[i];ids=[i]
  while stack:
   for j in adj[stack.pop()]&pending:pending.remove(j);ids.append(j);stack.append(j)
  result.append(ids)
 return result
weights={}
for o in parts:
 wm=o.matrix_world.copy();o.vertex_groups.clear();groups={b.name:o.vertex_groups.new(name=b.name) for b in arm.bones if b.use_deform}
 pack_ids=set()
 if o.name=='Gear_Backpack':
  for ids in components(o.data):
   if max((wm@o.data.vertices[i].co).y for i in ids)>.30:pack_ids.update(ids)
 for v in o.data.vertices:
  x,y,z=wm@v.co;side='L' if x>=0 else 'R';n=o.name
  if n=='Study_Head' or n.startswith('Study_Cap'):w={'Head':1}
  elif n.startswith('Study_Mitten'):w={'Hand.'+side:1}
  elif n.startswith('Study_Boot'):w={'Foot.'+side:1}
  elif n in ['Study_Hips','Gear_Charm']:w={'Hips':1}
  elif n=='Gear_Scarf':w={'Chest':1}
  elif n=='Study_Neck':
   t=smooth(1.075,1.13,z);w={'Chest':1-t,'Neck':t}
  elif n=='Gear_Backpack':w={'Backpack':1} if v.index in pack_ids else torso(z)
  elif n.startswith('Study_Sleeve'):
   upper=smooth(.795,.875,z);chest=smooth(1.02,1.10,z)*(1-smooth(.18,.245,abs(x)))
   w={'Forearm.'+side:(1-upper)*(1-chest),'UpperArm.'+side:upper*(1-chest),'Chest':chest}
  elif n.startswith('Study_Leg'):
   hip=smooth(.52,.605,z);thigh=smooth(.325,.405,z)*(1-hip);w={'Hips':hip,'Thigh.'+side:thigh,'Shin.'+side:1-hip-thigh}
  else:w=torso(z)
  total=sum(w.values())
  for name,value in w.items():
   if value>1e-7:groups[name].add([v.index],value/total,'REPLACE')
 o.parent=rig;o.matrix_world=wm;mod=o.modifiers.new('SimpleHero_Skin','ARMATURE');mod.object=rig;mod.use_deform_preserve_volume=False
 assert all(v.groups and abs(sum(g.weight for g in v.groups)-1)<1e-5 for v in o.data.vertices)
 weights[o.name]={'vertices':len(o.data.vertices),'max_influences':max(len(v.groups) for v in o.data.vertices)}
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();rest_error=0
for o in parts:
 ev=o.evaluated_get(deps);m=ev.to_mesh();rest_error=max(rest_error,max((v.co-original[o.name][0][i]).length for i,v in enumerate(m.vertices)));ev.to_mesh_clear()
 assert [tuple(u.uv) for u in o.data.uv_layers.active.data]==original[o.name][2]
assert rest_error<1e-5,rest_error
rig.animation_data_create();act=bpy.data.actions.new('Idle');rig.animation_data.action=act
for b in rig.pose.bones:b.rotation_mode='QUATERNION'
def segment(name,h,t):
 b=arm.bones[name];q=(b.tail_local-b.head_local).rotation_difference(t-h);m=q.to_matrix().to_4x4()@rest[name];m.translation=h;rig.pose.bones[name].matrix=m;bpy.context.view_layer.update()
def planted_leg(side):
 thigh=arm.bones['Thigh.'+side];shin=arm.bones['Shin.'+side];h=rig.pose.bones[thigh.name].head.copy();target=arm.bones['Foot.'+side].head_local.copy()
 vec=target-h;d=vec.length;axis=vec.normalized();l1=thigh.length;l2=shin.length
 assert d<l1+l2+1e-4,'Leg cannot reach floor'
 along=(l1*l1-l2*l2+d*d)/(2*d);height=math.sqrt(max(0,l1*l1-along*along));pole=Vector((0,-1,0));pole=(pole-axis*pole.dot(axis)).normalized();knee=h+axis*along+pole*height
 segment(thigh.name,h,knee);segment(shin.name,knee,target);rig.pose.bones['Foot.'+side].matrix=rest['Foot.'+side];bpy.context.view_layer.update()
previous={};boot_positions=None;max_boot_drift=0;max_motion=0;first_positions=None
for i,sample in enumerate(samples,1):
 for b in rig.pose.bones:
  q,loc=sample[b.name];b.rotation_quaternion=q;b.location=(0,0,0);b.scale=(1,1,1)
  if b.name=='Hips':b.location=loc*(.60/.78)
 bpy.context.view_layer.update()
 for side in ['L','R']:planted_leg(side)
 for b in rig.pose.bones:
  if b.name in previous and b.rotation_quaternion.dot(previous[b.name])<0:b.rotation_quaternion=-b.rotation_quaternion
  previous[b.name]=b.rotation_quaternion.copy();b.keyframe_insert('rotation_quaternion',frame=i,group=b.name);b.keyframe_insert('location',frame=i,group=b.name);b.keyframe_insert('scale',frame=i,group=b.name)
 deps=bpy.context.evaluated_depsgraph_get();positions={}
 for o in parts:
  ev=o.evaluated_get(deps);m=ev.to_mesh();positions[o.name]=[ev.matrix_world@v.co for v in m.vertices];ev.to_mesh_clear()
  assert all(all(math.isfinite(c) for c in p) for p in positions[o.name])
 if first_positions is None:first_positions=positions
 for name,ps in positions.items():
  drift=max((a-b).length for a,b in zip(ps,first_positions[name]));max_motion=max(max_motion,drift)
  if name.startswith('Study_Boot'):max_boot_drift=max(max_boot_drift,drift)
loop_error=max((a-b).length for n in positions for a,b in zip(positions[n],first_positions[n]))
assert max_boot_drift<1e-4 and loop_error<1e-4,(max_boot_drift,loop_error)
for fc in act.fcurves:
 for k in fc.keyframe_points:k.interpolation='LINEAR'
sc.render.fps=source_fps;sc.frame_start=1;sc.frame_end=len(samples);sc.frame_set(1)
rig['source_rig']=str(SOURCE);rig['motion_stage']='Idle only; walk and combat not yet retargeted';rig['sockets']='HandSocket.L / HandSocket.R';rig['rest_shape_preserved']=True
parent=bpy.data.objects.get('SimpleHero_Stage2')
if parent and not parent.children:bpy.data.objects.remove(parent,do_unlink=True)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/SimpleHero_Stage3_Idle.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'SimpleHero_Stage3_Idle.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
report={'stage':3,'source_idle_reused':True,'clips':['Idle'],'seconds':(len(samples)-1)/source_fps,'bones':len(arm.bones),'meshes':len(parts),'rest_shape_error':rest_error,'uv_preserved':True,'max_boot_drift_m':max_boot_drift,'loop_error_m':loop_error,'max_motion_m':max_motion,'weights':weights,'gameplay_applied':False}
assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest()==h for p,h in hashes.items())
(OUT/'Stage3RigCheck.json').write_text(json.dumps(report,indent=2));print('IDLE_RIG_READY',report,flush=True)
sc.render.filepath=str(OUT/'Stage3_Idle.png');bpy.ops.render.render(write_still=True)
sc.render.resolution_x=640;sc.render.resolution_y=720;sc.eevee.taa_render_samples=48
sc.render.image_settings.file_format='FFMPEG';sc.render.ffmpeg.format='MPEG4';sc.render.ffmpeg.codec='H264';sc.render.ffmpeg.constant_rate_factor='HIGH';sc.render.filepath=str(OUT/'Stage3_Idle.mp4');sc.frame_end=90
bpy.ops.render.render(animation=True);print('IDLE_VIDEO_READY',flush=True)
