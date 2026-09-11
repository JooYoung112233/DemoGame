"""Reuse accepted Walk/Run on the compact rig; keep approved Idle and appearance.
This is the raw retarget stage. The delivered Run is postprocessed by
stabilize_simple_hero_run.py (periodic pelvis path + foot-preserving IK).
Do not deliver this raw floor-fit Run without the stabilization stage.
"""
import bpy,bmesh,math,json,hashlib,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
SOURCE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend';BASE=OUT/'BlenderSource~/SimpleHero_Stage3_Idle.blend'
hashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in [SOURCE,BASE]}
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));sc=bpy.context.scene;old=bpy.data.objects['DarkSurvivor_Rig'];fps=sc.render.fps
for t in list(old.animation_data.nla_tracks):old.animation_data.nla_tracks.remove(t)
oldrest={b.name:b.matrix_local.copy() for b in old.data.bones};samples={}
oldboots=[o for o in sc.objects if o.type=='MESH' and ('Boot' in o.name or 'Sole' in o.name) and o.name.startswith('Hero_')]
assert oldboots
for name in ['Walk','Run']:
 a=bpy.data.actions[name];old.animation_data.action=a;rows=[]
 for f in range(int(a.frame_range[0]),int(a.frame_range[1])+1):
  sc.frame_set(f);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();bottom=100
  for o in oldboots:
   ev=o.evaluated_get(dg);m=ev.to_mesh();bottom=min(bottom,min((ev.matrix_world@v.co).z for v in m.vertices));ev.to_mesh_clear()
  rows.append(({b.name:((b.matrix.to_quaternion()@oldrest[b.name].to_quaternion().inverted()),b.location.copy()) for b in old.pose.bones},max(0,bottom)))
 rows[-1]=rows[0];samples[name]=rows
print('LOCOMOTION_SOURCE',[(n,len(v)) for n,v in samples.items()],flush=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE));bpy.context.preferences.filepaths.save_version=0;sc=bpy.context.scene;rig=bpy.data.objects['SimpleHero_Rig'];arm=rig.data;parts=[o for o in sc.objects if o.type=='MESH' and o.name.startswith(('Study_','Gear_'))]
idle=bpy.data.actions['Idle'];idle.use_fake_user=True
def action_hash(a):return hashlib.sha256(repr([(f.data_path,f.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right)) for k in f.keyframe_points]) for f in a.fcurves]).encode()).hexdigest()
idlehash=action_hash(idle);rig.animation_data.action=None
for b in rig.pose.bones:b.matrix_basis.identity()
arm.pose_position='REST';bpy.context.view_layer.update()
def smooth(a,b,z):
 t=max(0,min(1,(z-a)/(b-a)));return t*t*(3-2*t)
# Add bending loops on the existing surfaces, without sculpting the accepted shape.
loop_report=[]
for o in parts:
 if not o.name.startswith(('Study_Leg','Study_Sleeve')) and o.name!='Study_Torso':continue
 before=len(o.data.vertices);bm=bmesh.new();bm.from_mesh(o.data)
 planes=[.31,.345,.365,.385,.42,.50] if o.name.startswith('Study_Leg') else ([.69,.73,.77,.81,.85,.94,.985] if o.name=='Study_Torso' else [.755,.795,.815,.855,.875,.92])
 for z in planes:bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-6,plane_co=(0,0,z),plane_no=(0,0,1),clear_inner=False,clear_outer=False)
 bm.to_mesh(o.data);bm.free();o.data.update()
 if o.name.startswith('Study_Sleeve'):
  # Slight taper around the elbow; preserve the shoulder and cuff silhouettes.
  for v in o.data.vertices:
   z=v.co.z;s=1 if v.co.x>=0 else -1
   t=max(0,min(1,(z-.69)/(.84-.69))) if z<=.84 else max(0,min(1,(z-.84)/(1.015-.84)))
   center=s*((.322*(1-t)+.29*t) if z<=.84 else (.29*(1-t)+.24*t))
   taper=1-.14*math.exp(-((z-.835)/.07)**2)
   v.co.x=center+(v.co.x-center)*taper;v.co.y*=taper
 for g in o.vertex_groups:g.remove(list(range(len(o.data.vertices))))
 for v in o.data.vertices:
  x,y,z=o.matrix_world@v.co;side='L' if x>=0 else 'R'
  if o.name.startswith('Study_Leg'):
   hip=smooth(.52,.605,z);thigh=smooth(.325,.405,z)*(1-hip);w={'Hips':hip,'Thigh.'+side:thigh,'Shin.'+side:1-hip-thigh}
  elif o.name=='Study_Torso':
   if z<.86:
    t=smooth(.65,.82,z);w={'Hips':1-t,'Spine':t}
   else:
    t=smooth(.86,1.015,z);w={'Spine':1-t,'Chest':t}
  else:
   upper=smooth(.77,.91,z);chest=smooth(1.02,1.10,z)*(1-smooth(.18,.245,abs(x)));w={'Forearm.'+side:(1-upper)*(1-chest),'UpperArm.'+side:upper*(1-chest),'Chest':chest}
  for name,value in w.items():
   if value>1e-7:o.vertex_groups[name].add([v.index],value,'REPLACE')
 loop_report.append({'part':o.name,'before_vertices':before,'after_vertices':len(o.data.vertices)})
arm.pose_position='POSE';rest={b.name:b.matrix_local.copy() for b in arm.bones};sc.render.fps=fps
boots=[o for o in parts if o.name.startswith('Study_Boot')]
legscale=(arm.bones['Thigh.L'].length+arm.bones['Shin.L'].length)/((oldrest['Shin.L'].translation-oldrest['Thigh.L'].translation).length+(oldrest['Foot.L'].translation-oldrest['Shin.L'].translation).length)
reports=[]
def evaluated():
 bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();result={}
 for o in parts:
  ev=o.evaluated_get(dg);m=ev.to_mesh();result[o.name]=[ev.matrix_world@v.co for v in m.vertices];ev.to_mesh_clear()
 return result
for name,rows in samples.items():
 a=bpy.data.actions.new(name);a.use_fake_user=True;rig.animation_data.action=a;previous={};first=None;minimum=100;maximum=0;ground_adjust=[]
 for frame,(pose,source_bottom) in enumerate(rows,1):
  sc.frame_set(frame)
  for pb in rig.pose.bones:
   n=pb.name;p=pb.parent
   follow=(p.matrix@rest[p.name].inverted()@rest[n]) if p else rest[n].copy()
   follow.translation+=follow.to_3x3()@pose[n][1]*legscale
   deformation=pose[n][0]
   if name=='Run' and n in ['Hips','Spine']:
    # Distribute the source's strong waist twist through the compact torso.
    # Keep the chest lean, arm swing and head direction of the original run.
    hip_q=pose['Hips'][0].slerp(pose['Chest'][0],.15)
    deformation=hip_q if n=='Hips' else hip_q.slerp(pose['Chest'][0],.5)
   q=deformation@rest[n].to_quaternion();m=q.to_matrix().to_4x4();m.translation=follow.translation;pb.matrix=m;bpy.context.view_layer.update()
  dg=bpy.context.evaluated_depsgraph_get();bottom=100
  for o in boots:
   ev=o.evaluated_get(dg);m=ev.to_mesh();bottom=min(bottom,min((ev.matrix_world@v.co).z for v in m.vertices));ev.to_mesh_clear()
  target=source_bottom*legscale*(.60 if name=='Run' else 1);shift=target-bottom;hip=rig.pose.bones['Hips'];m=hip.matrix.copy();m.translation.z+=shift;hip.matrix=m;bpy.context.view_layer.update();ground_adjust.append(shift)
  for pb in rig.pose.bones:
   if pb.name in previous and pb.rotation_quaternion.dot(previous[pb.name])<0:pb.rotation_quaternion=-pb.rotation_quaternion
   previous[pb.name]=pb.rotation_quaternion.copy()
   for prop in ['location','rotation_quaternion','scale']:pb.keyframe_insert(prop,frame=frame,group=pb.name)
  ps=evaluated()
  assert all(all(math.isfinite(c) for c in p) and p.length<5 for vv in ps.values() for p in vv)
  if first is None:first=ps
  footmin=min(p.z for o in boots for p in ps[o.name]);minimum=min(minimum,footmin);maximum=max(maximum,footmin)
 loop=max((p-q).length for n in ps for p,q in zip(ps[n],first[n]));assert loop<1e-4,(name,loop)
 for fc in a.fcurves:
  for k in fc.keyframe_points:k.interpolation='LINEAR'
  fc.modifiers.new('CYCLES')
 reports.append({'clip':name,'frames':len(rows),'seconds':(len(rows)-1)/fps,'min_sole_z':minimum,'max_lowest_sole_z':maximum,'loop_error_m':loop,'max_vertical_fit_m':max(map(abs,ground_adjust))})
 assert minimum>-.001
assert action_hash(idle)==idlehash
rig.animation_data.action=idle;sc.frame_set(1);sc.frame_start=1;sc.frame_end=91
rig['motion_stage']='Idle, Walk, Run; combat not yet retargeted';rig['locomotion_source']=str(SOURCE);rig['locomotion_in_place']=True
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/SimpleHero_Stage4_Locomotion.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'SimpleHero_Stage4_Locomotion.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest()==h for p,h in hashes.items())
report={'stage':4,'existing_walk_run_reused':True,'idle_keys_preserved':True,'leg_length_scale':legscale,'bend_loops':loop_report,'clips':reports,'gameplay_applied':False}
(OUT/'Stage4MotionCheck.json').write_text(json.dumps(report,indent=2));print('LOCOMOTION_READY',report,flush=True)
if '--no-render' in sys.argv:sys.exit(0)
sc.render.image_settings.file_format='PNG';sc.render.resolution_x=850;sc.render.resolution_y=1000;sc.eevee.taa_render_samples=64
cam=sc.camera
for name in ['Walk','Run']:
 rig.animation_data.action=bpy.data.actions[name];sc.frame_set(1+int((len(samples[name])-1)*.25));sc.render.filepath=str(OUT/('Stage4_'+name+'.png'));bpy.ops.render.render(write_still=True)
 oldcam=cam.matrix_world.copy();cam.location=(6,-.8,2.4);cam.rotation_euler=(Vector((0,0,.9))-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/('Stage4_'+name+'_Side.png'));bpy.ops.render.render(write_still=True);cam.matrix_world=oldcam
sc.render.resolution_x=640;sc.render.resolution_y=720;sc.eevee.taa_render_samples=40;sc.render.image_settings.file_format='FFMPEG';sc.render.ffmpeg.format='MPEG4';sc.render.ffmpeg.codec='H264';sc.render.ffmpeg.constant_rate_factor='HIGH'
for name in ['Walk','Run']:
 rig.animation_data.action=bpy.data.actions[name];sc.frame_start=1;sc.frame_end=(len(samples[name])-1)*3;sc.render.filepath=str(OUT/('Stage4_'+name+'.mp4'));bpy.ops.render.render(animation=True);print(name+'_VIDEO_READY',flush=True)
