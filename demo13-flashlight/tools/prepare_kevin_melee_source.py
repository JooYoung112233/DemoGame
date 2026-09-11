"""Map the supplied native Blender action onto the existing 23-bone rig."""
import bpy,math,sys
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Library/KevinMeleeSource'
sys.path.insert(0,str(ROOT/'tools'))
from character_source_paths import source_path
bpy.ops.wm.open_mainfile(filepath=str(OUT/'HumanM_Melee.blend'));sc=bpy.context.scene;src=bpy.data.objects['Rig'];src.animation_data.action=bpy.data.actions['HumanM@Attack2H01']
for t in list(src.animation_data.nla_tracks):src.animation_data.nla_tracks.remove(t)
mapping={'Root':'B-root','Hips':'B-hips','Spine':'B-spine','Chest':'B-chest','Neck':'B-neck','Head':'B-head','Backpack':'B-chest'}
for side in ['L','R']:
 for n,s in [('Clavicle','shoulder'),('UpperArm','upperArm'),('Forearm','forearm'),('Hand','hand'),('HandSocket','handProp'),('Thigh','thigh'),('Shin','shin'),('Foot','foot')]:mapping[n+'.'+side]='B-'+s+'.'+side
rest={n:src.data.bones[s].matrix_local.copy() for n,s in mapping.items()};rows=[]
for f in range(1,50):
 sc.frame_set(f);bpy.context.view_layer.update();rows.append({n:src.pose.bones[s].matrix.copy() for n,s in mapping.items()})
bpy.ops.wm.open_mainfile(filepath=str(source_path('Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_Stage4_Locomotion.blend')));sc=bpy.context.scene;r=bpy.data.objects['SimpleHero_Rig'];r.name='DarkSurvivor_Rig';newrest={b.name:b.matrix_local.copy() for b in r.data.bones}
for a in list(bpy.data.actions):bpy.data.actions.remove(a)
a=bpy.data.actions.new('SwordSlash');a.use_fake_user=True;r.animation_data.action=a
legscale=(r.data.bones['Thigh.R'].length+r.data.bones['Shin.R'].length)/((rest['Shin.R'].translation-rest['Thigh.R'].translation).length+(rest['Foot.R'].translation-rest['Shin.R'].translation).length)
armscale=(r.data.bones['UpperArm.R'].length+r.data.bones['Forearm.R'].length)/((rest['Forearm.R'].translation-rest['UpperArm.R'].translation).length+(rest['Hand.R'].translation-rest['Forearm.R'].translation).length)
previous={}
for f,pose in enumerate(rows,1):
 sc.frame_set(f)
 for pb in r.pose.bones:
  n=pb.name;p=pb.parent;follow=p.matrix@newrest[p.name].inverted()@newrest[n] if p else newrest[n].copy()
  neutral=newrest[n].to_quaternion()
  if n.startswith(('UpperArm','Forearm','Hand.','Thigh','Shin','Foot.','Clavicle')):
   neutral=(neutral@Vector((0,1,0))).rotation_difference(rest[n].to_quaternion()@Vector((0,1,0)))@neutral
  q=pose[n].to_quaternion()@rest[n].to_quaternion().inverted()@neutral
  if n.startswith('HandSocket'):q=pose[n].to_quaternion()
  m=q.to_matrix().to_4x4();m.translation=follow.translation
  if n=='Hips':m.translation=newrest[n].translation+(pose[n].translation-rest[n].translation)*legscale
  # Source grip anchors are needed by the shared two-hand fitting pass.
  if n.startswith('HandSocket'):m.translation=r.pose.bones['Chest'].head+(pose[n].translation-pose['Chest'].translation)*armscale
  pb.matrix=m;bpy.context.view_layer.update()
  if n in previous and pb.rotation_quaternion.dot(previous[n])<0:pb.rotation_quaternion=-pb.rotation_quaternion
  previous[n]=pb.rotation_quaternion.copy()
  for prop in ['location','rotation_quaternion','scale']:pb.keyframe_insert(prop,frame=f,group=n)
for fc in a.fcurves:
 for k in fc.keyframe_points:k.interpolation='LINEAR'
sc.frame_start=1;sc.frame_end=49;sc.render.fps=30;sc.frame_set(1);bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Mapped_Attack2H.blend'));print('KEVIN_SOURCE_MAPPED',legscale,armscale,flush=True)
