"""Remove floor-fit chatter from Run using a periodic pelvis path and leg IK.
Only the Run action changes; geometry, textures and other clips are preserved.
"""
import bpy,math,json,hashlib,shutil
from pathlib import Path
from mathutils import Vector,Quaternion
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
BACKUP=ROOT/'Library/SimpleHeroRunJitterBefore';BACKUP.mkdir(exist_ok=True)
BASE=OUT/'BlenderSource~/SimpleHero_Stage4_Locomotion.blend'
for p in [BASE,OUT/'BlenderSource~/SimpleHero_Stage5_Slash.blend',OUT/'Stage4_Run.mp4']:
 dst=BACKUP/p.name
 if not dst.exists():shutil.copy2(p,dst)
def action_hash(a):return hashlib.sha256(repr([(c.data_path,c.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right),k.interpolation) for k in c.keyframe_points]) for c in a.fcurves]).encode()).hexdigest()
def geometry_hash(parts):return hashlib.sha256(repr([(o.name,[tuple(v.co) for v in o.data.vertices],[[(g.group,g.weight) for g in v.groups] for v in o.data.vertices],[tuple(u.uv) for u in o.data.uv_layers.active.data]) for o in parts]).encode()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(BACKUP/BASE.name));bpy.context.preferences.filepaths.save_version=0;sc=bpy.context.scene;r=bpy.data.objects['SimpleHero_Rig'];old=bpy.data.actions['Run'];r.animation_data.action=old
# The backup lives outside Assets. Resolve its relative textures against the
# original blend directory before saving, so Blender cannot rebase them to Library.
for image in bpy.data.images:
 if image.source=='FILE' and image.filepath:
  if image.filepath.startswith('//'):image.filepath=str((BASE.parent/image.filepath[2:]).resolve())
  assert Path(bpy.path.abspath(image.filepath)).exists(),image.filepath
if old.slots:r.animation_data.action_slot=old.slots[0]
parts=[o for o in sc.objects if o.type=='MESH' and o.name.startswith(('Study_','Gear_'))];gh=geometry_hash(parts)
preserved={a.name:action_hash(a) for a in bpy.data.actions if a!=old};rest={b.name:b.matrix_local.copy() for b in r.data.bones}
period=24;step=.25;poses=[];hip_positions=[]
for k in range(period*4+1):
 f=1+k*step;sc.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();poses.append({p.name:p.matrix.copy() for p in r.pose.bones})
 if k%4==0 and k<period*4:hip_positions.append(r.pose.bones['Hips'].head.copy())
def harmonic(t):
 p=sum(hip_positions,Vector())/period
 for h in [1,2]:
  ac=sum((v*math.cos(2*math.pi*h*i/period) for i,v in enumerate(hip_positions)),Vector())*(2/period)
  bc=sum((v*math.sin(2*math.pi*h*i/period) for i,v in enumerate(hip_positions)),Vector())*(2/period)
  p+=ac*math.cos(2*math.pi*h*t/period)+bc*math.sin(2*math.pi*h*t/period)
 return p
hip_quats=[poses[i*4]['Hips'].to_quaternion() for i in range(period)]
for i in range(1,period):
 if hip_quats[i].dot(hip_quats[i-1])<0:hip_quats[i].negate()
def hip_rotation(t):
 values=[]
 for j in range(4):
  v=sum(q[j] for q in hip_quats)/period
  for h in [1,2,3]:
   ac=sum(q[j]*math.cos(2*math.pi*h*i/period) for i,q in enumerate(hip_quats))*2/period
   bc=sum(q[j]*math.sin(2*math.pi*h*i/period) for i,q in enumerate(hip_quats))*2/period
   v+=ac*math.cos(2*math.pi*h*t/period)+bc*math.sin(2*math.pi*h*t/period)
  values.append(v)
 return Quaternion(values).normalized()
def pelvis_matrix(t,drop):
 m=hip_rotation(t).to_matrix().to_4x4();m.translation=harmonic(t)-Vector((0,0,drop));return m
# A single constant vertical adjustment ensures every preserved ankle stays
# reachable; never add a per-frame pelvis clamp which would recreate chatter.
def reach_max(drop):
 maximum=0
 for k,pose in enumerate(poses[:-1]):
  change=pelvis_matrix(k*step,drop)@pose['Hips'].inverted()
  for s in ['L','R']:
   shoulder=change@pose['Thigh.'+s].translation;target=pose['Foot.'+s].translation
   maximum=max(maximum,(target-shoulder).length/(r.data.bones['Thigh.'+s].length+r.data.bones['Shin.'+s].length))
 return maximum
drop=0
while reach_max(drop)>.995 and drop<.1:drop+=.001
assert reach_max(drop)<=.995,('IK unreachable',drop,reach_max(drop))
old.name='Run_Unstable_Temporary';a=bpy.data.actions.new('Run');a.use_fake_user=True;r.animation_data.action=a;previous={};errors=[]
for k,pose in enumerate(poses):
 f=1+k*step;sc.frame_set(int(f),subframe=f-int(f))
 delta=harmonic(k*step)-pose['Hips'].translation;delta.z-=drop
 for pb in r.pose.bones:
  m=pose[pb.name].copy()
  if pb.name=='Hips':m=pelvis_matrix(k*step,drop)
  elif pb.parent:m.translation=(pb.parent.matrix@pose[pb.parent.name].inverted()@pose[pb.name]).translation
  pb.matrix=m;bpy.context.view_layer.update()
 for s in ['L','R']:
  upper=r.pose.bones['Thigh.'+s];lower=r.pose.bones['Shin.'+s];foot=r.pose.bones['Foot.'+s]
  hip=upper.head.copy();ankle=pose[foot.name].translation;d=ankle-hip;distance=d.length;direction=d.normalized();la=upper.bone.length;lb=lower.bone.length
  along=(la*la-lb*lb+distance*distance)/(2*distance)
  pole=pose[lower.name].translation+delta-hip;pole-=direction*pole.dot(direction)
  if pole.length<.001:pole=Vector((0,-1,0));pole-=direction*pole.dot(direction)
  knee=hip+direction*along+pole.normalized()*math.sqrt(max(0,la*la-along*along))
  for pb,h,t in [(upper,hip,knee),(lower,knee,ankle)]:
   q=pose[pb.name].to_quaternion();q=(q@Vector((0,1,0))).rotation_difference(t-h)@q;m=q.to_matrix().to_4x4();m.translation=h;pb.matrix=m;bpy.context.view_layer.update()
  foot.matrix=pose[foot.name];bpy.context.view_layer.update();errors.append((foot.head-ankle).length)
 for pb in r.pose.bones:
  if pb.name in previous and pb.rotation_quaternion.dot(previous[pb.name])<0:pb.rotation_quaternion=-pb.rotation_quaternion
  previous[pb.name]=pb.rotation_quaternion.copy()
  for prop in ['location','rotation_quaternion','scale']:pb.keyframe_insert(prop,frame=f,group=pb.name)
for c in a.fcurves:
 for key in c.keyframe_points:key.interpolation='LINEAR'
 c.modifiers.new('CYCLES')
bpy.data.actions.remove(old)
assert all(action_hash(bpy.data.actions[n])==h for n,h in preserved.items()) and gh==geometry_hash(parts)
def max_accel(values):return max((values[(i+1)%len(values)]-2*v+values[(i-1)%len(values)]).length for i,v in enumerate(values))
before=max_accel(hip_positions);after=max_accel([harmonic(i) for i in range(period)])
assert after<before*.65 and max(errors)<1e-5,(before,after,max(errors))
head_positions=[]
for f in range(1,25):sc.frame_set(f);bpy.context.view_layer.update();head_positions.append(r.pose.bones['Head'].head.copy())
head_before=max_accel([poses[i*4]['Head'].translation for i in range(period)]);head_after=max_accel(head_positions)
assert head_after<head_before*.65,(head_before,head_after)
report={'cause':'per-frame sole fitting and abrupt pelvis rotation introduced upper-body chatter','period_seconds':.8,'position_harmonics':2,'rotation_harmonics':3,'constant_hip_drop_m':drop,'before_pelvis_second_difference_m':before,'after_pelvis_second_difference_m':after,'before_head_second_difference_m':head_before,'after_head_second_difference_m':head_after,'ankle_target_error_m':max(errors),'geometry_uv_weights_unchanged':True,'other_actions_unchanged':True,'gameplay_applied':False}
def save_export(stage,filename):
 r.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
 bpy.ops.object.select_all(action='DESELECT');r.select_set(True)
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=r
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~'/(filename+'.blend')))
 bpy.ops.export_scene.fbx(filepath=str(OUT/(filename+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_step=.125 if stage==5 else .25,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
save_export(4,'SimpleHero_Stage4_Locomotion')
# Update the delivered combat file too without regenerating its accepted slash.
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/SimpleHero_Stage5_Slash.blend'));sc=bpy.context.scene;r=bpy.data.objects['SimpleHero_Rig']
parts=[o for o in sc.objects if o.type=='MESH' and o.name.startswith(('Study_','Gear_'))];gh=geometry_hash(parts)
preserved={x.name:action_hash(x) for x in bpy.data.actions if x.name!='Run'};old=bpy.data.actions['Run'];old.name='Run_Unstable_Temporary'
with bpy.data.libraries.load(str(BASE),link=False) as (src,dst):dst.actions=['Run']
new=dst.actions[0];new.name='Run';new.use_fake_user=True
if r.animation_data.action==old:r.animation_data.action=new
bpy.data.actions.remove(old)
assert all(action_hash(bpy.data.actions[n])==h for n,h in preserved.items()) and gh==geometry_hash(parts)
save_export(5,'SimpleHero_Stage5_Slash')
(OUT/'RunStabilityCheck.json').write_text(json.dumps(report,indent=2));print('RUN_STABILIZED',report,flush=True)
