"""Transfer the purchased/free package's actual joint motion to a review copy.
No motion is synthesized; bind-pose corrections and leg scale adapt the motion.
"""
import bpy,json,math,hashlib,argparse,sys
from pathlib import Path
from mathutils import Vector,Quaternion,Matrix
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser()
p.add_argument('--source',default='Unarmed/Unarmed-Run-Forward');p.add_argument('--base',default='Assets/ChibiSurvivor/Player/WalkReview/CompactSurvivor_WalkReview.blend')
p.add_argument('--out',default='Assets/ChibiSurvivor/Player/ExplosiveRunReview');p.add_argument('--cache',default='Library/CodexBlender/ExplosiveReview')
p.add_argument('--action',default='Run_Explosive_Unarmed');p.add_argument('--filename',default='CompactSurvivor_ExplosiveRun');p.add_argument('--forward-gaze',action='store_true')
args=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
OUT=ROOT/args.out;OUT.mkdir(exist_ok=True,parents=True)
CACHE=ROOT/args.cache;CACHE.mkdir(exist_ok=True,parents=True)
REF=json.loads((ROOT/'Library/CodexBlender/ExplosiveReview/rig-inspection.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(ROOT/args.base))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig']
meshes=[o for o in scene.objects if o.type=='MESH']
def digest():
 return hashlib.sha256(repr([(o.name,[tuple(v.co) for v in o.data.vertices],[(v.index,[(g.group,g.weight) for g in v.groups]) for v in o.data.vertices]) for o in meshes]).encode()).hexdigest()
before_digest=digest()
def existing_actions_digest():
 return hashlib.sha256(repr([(a.name,[(fc.data_path,fc.array_index,[(tuple(k.co),k.interpolation) for k in fc.keyframe_points]) for fc in a.fcurves]) for a in existing_actions]).encode()).hexdigest()
existing_actions=list(bpy.data.actions);before_actions=existing_actions_digest()
before=set(bpy.data.objects)
folder,clip=args.source.split('/')
source_path=ROOT/'Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations'/folder/('RPG-Character@'+clip+'.FBX')
bpy.ops.import_scene.fbx(filepath=str(source_path))
source={o.name:o for o in set(bpy.data.objects)-before}
source_range=source['Motion'].animation_data.action.frame_range
assert abs(source_range[0]-1)<1e-4
count=round(source_range[1]-source_range[0]);end=count+1
mapping={'Hips':'B_Pelvis','Spine':'B_Spine1','Chest':'B_Spine2','Neck':'B_Neck','Head':'B_Head'}
children={}
for s in ['L','R']:
 for t,a in [('Clavicle','Clavicle'),('UpperArm','UpperArm'),('Forearm','Forearm'),('Hand','Hand'),('Thigh','Thigh'),('Shin','Calf'),('Foot','Foot')]:mapping[t+'.'+s]='B_'+s+'_'+a
 for t,a in [('UpperArm','Forearm'),('Forearm','Hand'),('Thigh','Calf'),('Shin','Foot')]:children[t+'.'+s]='B_'+s+'_'+a
rest={k:v.matrix_local.copy() for k,v in rig.data.bones.items()}
corrections={}
for t,s in mapping.items():
 neutral=rest[t].to_quaternion()
 if t in children:
  direction=Vector(REF['model'][children[t]+'.001']['pos'])-Vector(REF['model'][s+'.001']['pos'])
  current=rig.data.bones[t].tail_local-rig.data.bones[t].head_local
  neutral=current.rotation_difference(direction)@neutral
 if t.startswith('Hand.'):
  # Follow the forearm's neutral alignment; round hands need no finger mapping.
  p='Forearm.'+t[-1];ss=mapping[p]
  direction=Vector(REF['model'][s+'.001']['pos'])-Vector(REF['model'][ss+'.001']['pos'])
  current=rig.data.bones[t].tail_local-rig.data.bones[t].head_local
  neutral=current.rotation_difference(direction)@neutral
 corrections[t]=Quaternion(REF['model'][s+'.001']['rot']).inverted()@neutral
samples=[]
for f in range(1,end+1):
 scene.frame_set(f);bpy.context.view_layer.update()
 samples.append({s:source[s].matrix_world.copy() for s in list(mapping.values())+['Motion']})
old_actions=list(bpy.data.actions)
action=bpy.data.actions.new(args.action);action.use_fake_user=True
rig.animation_data.action=action
for p in rig.pose.bones:p.matrix_basis=Matrix.Identity(4)
srcleg=(Vector(REF['model']['B_L_Thigh.001']['pos'])-Vector(REF['model']['B_L_Calf.001']['pos'])).length+(Vector(REF['model']['B_L_Calf.001']['pos'])-Vector(REF['model']['B_L_Foot.001']['pos'])).length
scale=(rig.data.bones['Thigh.L'].length+rig.data.bones['Shin.L'].length)/srcleg
hiprest=Vector(REF['model']['B_Pelvis.001']['pos']);previous={}
for f,data in enumerate(samples,1):
 scene.frame_set(f)
 for p in rig.pose.bones:p.matrix_basis=Matrix.Identity(4)
 for p in rig.pose.bones:
  if p.name not in mapping:continue
  desired=data[mapping[p.name]].to_quaternion()@corrections[p.name]
  parent_rot=p.parent.matrix.to_quaternion() if p.parent else Quaternion()
  parent_rest=p.parent.bone.matrix_local.to_quaternion() if p.parent else Quaternion()
  local=p.bone.matrix_local.to_quaternion().inverted()@parent_rest@parent_rot.inverted()@desired
  if p.name in previous and previous[p.name].dot(local)<0:local.negate()
  previous[p.name]=local.copy();p.rotation_mode='QUATERNION';p.rotation_quaternion=local
  if p.name=='Hips':
   travel=data['Motion'].translation-samples[0]['Motion'].translation
   travel.z=0
   offset=(data['B_Pelvis'].translation-travel-hiprest)*scale
   p.location=rest['Hips'].to_3x3().inverted()@offset
   p.keyframe_insert('location',frame=f,group=p.name)
  p.keyframe_insert('rotation_quaternion',frame=f,group=p.name)
  bpy.context.view_layer.update()
 for p in rig.pose.bones:
  if p.name not in mapping:
   p.keyframe_insert('location',frame=f,group=p.name);p.keyframe_insert('rotation_quaternion',frame=f,group=p.name)
# Calibrate a single floor offset over the cycle, retaining the original hip bounce.
floor=[]
for f in range(1,end):
 scene.frame_set(f);dg=bpy.context.evaluated_depsgraph_get();ev=bpy.data.objects['Compact_Boots'].evaluated_get(dg);m=ev.to_mesh()
 floor.append(min((ev.matrix_world@v.co).z for v in m.vertices));ev.to_mesh_clear()
lift=-min(floor)
for f in range(1,end+1):
 scene.frame_set(f);p=rig.pose.bones['Root'];p.location=rest['Root'].to_3x3().inverted()@Vector((0,0,lift));p.keyframe_insert('location',frame=f,group='Root')
gaze_lift=0
if args.forward_gaze:
 head=rig.pose.bones['Head'];hr=head.bone.matrix_local.to_quaternion();poses=[];angles=[]
 for f in range(1,end+1):
  scene.frame_set(f);bpy.context.view_layer.update();poses.append(head.rotation_quaternion.copy())
  direction=(head.matrix.to_quaternion()@hr.inverted())@Vector((0,-1,0));angles.append(math.degrees(math.asin(max(-1,min(1,direction.z)))))
 mean=sum(angles[:-1])/count
 if mean < -10 or mean > 3:gaze_lift=-6-mean
 for f,q in enumerate(poses,1):
  head.rotation_quaternion=q@Quaternion((1,0,0),-math.radians(gaze_lift));head.keyframe_insert('rotation_quaternion',frame=f,group='Head')
for fc in action.fcurves:
 for k in fc.keyframe_points:k.interpolation='LINEAR'
 fc.modifiers.new('CYCLES')
for o in source.values():bpy.data.objects.remove(o,do_unlink=True)
for a in old_actions:
 if '|Take 001|' in a.name and a.users==0:bpy.data.actions.remove(a)
assert digest()==before_digest,'Character geometry or weights changed'
assert existing_actions_digest()==before_actions,'An existing animation changed'
scene.frame_start=1;scene.frame_end=count;scene.render.fps=30;scene.frame_set(1)
rig['review_source']=f'ExplosiveLLC {clip} (0-{count} original frames)'
rig['review_status']='Pending user approval; Blender retarget, not Unity Humanoid validation'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(args.filename+'.blend')))
(OUT/'RetargetCheck.json').write_text(json.dumps({'source':str(source_path.relative_to(ROOT)),'action':action.name,'frames':[1,end],'fps':30,'seconds':count/30,'mapped_bones':mapping,'leg_scale':scale,'floor_offset_m':lift,'min_sole_height_after_offset':min(floor)+lift,'geometry_and_weights_unchanged':True,'existing_actions_preserved':True,'existing_actions_digest':before_actions,'humanoid_validated':False,'approval':'pending','head_gaze_adjusted':abs(gaze_lift)>.001,'head_pitch_lift_degrees':gaze_lift},indent=2))
scene.camera.location=(-4.8,-6.2,2.3);scene.camera.rotation_euler=(Vector((0,0,.82))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=2.02
scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=24
scene.render.resolution_x=600;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
for f in [1,7,13]:
 scene.frame_set(f);scene.render.filepath=str(CACHE/f'Pose_{f}.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(CACHE/'Render.blend'))
print('EXPLOSIVE_RETARGET_READY',flush=True)
