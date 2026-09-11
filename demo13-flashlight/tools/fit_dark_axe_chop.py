"""Fit the free two-hand slash's original hand spacing to the shorter character."""
import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview';CACHE=ROOT/'Library/CodexBlender/AxeReview'
bpy.ops.wm.open_mainfile(filepath=str(CACHE/'Render.blend'));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig'];action=rig.animation_data.action
characters=[o for o in scene.objects if o.type=='MESH' and o.name.startswith('Hero_')];end=scene.frame_end
other=[a for a in bpy.data.actions if a!=action]
def digest():return hashlib.sha256(repr([(a.name,[(c.data_path,c.array_index,[tuple(k.co) for k in c.keyframe_points]) for c in a.fcurves]) for a in other]).encode()).hexdigest()
before_digest=digest();rest={b.name:b.matrix_local.copy() for b in rig.data.bones}
before=set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/2Hand-Sword/RPG-Character@2Hand-Sword-Attack5.FBX'))
src={o.name:o for o in set(bpy.data.objects)-before}
ref=json.loads((ROOT/'Library/CodexBlender/ExplosiveReview/rig-inspection.json').read_text())['model']
source_arm=(Vector(ref['B_R_UpperArm.001']['pos'])-Vector(ref['B_R_Forearm.001']['pos'])).length+(Vector(ref['B_R_Forearm.001']['pos'])-Vector(ref['B_R_Hand.001']['pos'])).length
scale=(rig.data.bones['UpperArm.R'].length+rig.data.bones['Forearm.R'].length)/source_arm
samples=[]
for f in range(1,end+1):
 scene.frame_set(f);bpy.context.view_layer.update()
 r=src['B_R_Hand'].matrix_world.translation;l=src['B_L_Hand'].matrix_world.translation;c=src['B_Spine2'].matrix_world.translation
 socket_axis=sum((rig.pose.bones['HandSocket.'+s].matrix.to_quaternion()@Vector((0,1,0)) for s in ['R','L']),Vector()).normalized()
 samples.append({'axis':socket_axis,'gap':(r-l).length*scale,'anchor':rig.pose.bones['Chest'].head+(r-c)*scale+Vector((.025,-.05,0)),
  'bones':{n:rig.pose.bones[n].matrix.copy() for n in ['UpperArm.R','Forearm.R','Hand.R','HandSocket.R','UpperArm.L','Forearm.L','Hand.L','HandSocket.L']}})
gap=.16;previous={}
def rotate(n,q,f):
 p=rig.pose.bones[n];local=rest[n].to_quaternion().inverted()@rest[p.parent.name].to_quaternion()@p.parent.matrix.to_quaternion().inverted()@q
 if n in previous and previous[n].dot(local)<0:local.negate()
 previous[n]=local.copy();p.rotation_quaternion=local;p.keyframe_insert('rotation_quaternion',frame=f,group=n);bpy.context.view_layer.update()
reach_corrections=[]
for f,s in enumerate(samples,1):
 scene.frame_set(f);bpy.context.view_layer.update();socketq=s['axis'].to_track_quat('Y','Z')
 # Move a shared anchor into both reachable arm spheres, preserving hand spacing.
 original_anchor=s['anchor'].copy()
 for iteration in range(32):
  changed=False
  for side in ['R','L']:
   upper='UpperArm.'+side;fore='Forearm.'+side;hand='Hand.'+side
   rel=s['bones'][hand].inverted()@s['bones']['HandSocket.'+side]
   handq=socketq@rel.to_quaternion().inverted()
   offset=(s['axis']*gap if side=='L' else Vector())+handq@rel.translation
   center=rig.pose.bones[upper].head+offset
   delta=s['anchor']-center;limit=(rig.data.bones[upper].length+rig.data.bones[fore].length)*.98
   if delta.length>limit:
    s['anchor']=center+delta.normalized()*limit;changed=True
  if not changed:break
 reach_corrections.append((s['anchor']-original_anchor).length)
 for side in ['R','L']:
  upper='UpperArm.'+side;fore='Forearm.'+side;hand='Hand.'+side
  rel=s['bones'][hand].inverted()@s['bones']['HandSocket.'+side]
  handq=socketq@rel.to_quaternion().inverted();anchor=s['anchor']-(s['axis']*gap if side=='L' else Vector())
  wrist=anchor-handq@rel.translation;shoulder=rig.pose.bones[upper].head.copy()
  direction=wrist-shoulder;d=direction.length;axis=direction.normalized();a=rig.data.bones[upper].length;b=rig.data.bones[fore].length
  assert abs(a-b)<d<a+b,('Unreachable source hand',f,side,d,a+b)
  along=(a*a-b*b+d*d)/(2*d);pole=s['bones'][fore].translation-shoulder;pole=(pole-axis*pole.dot(axis)).normalized()
  elbow=shoulder+axis*along+pole*math.sqrt(max(0,a*a-along*along))
  for name,aim in [(upper,elbow-shoulder),(fore,wrist-elbow)]:
   old=s['bones'][name].to_quaternion();rotate(name,(old@Vector((0,1,0))).rotation_difference(aim)@old,f)
  rotate(hand,handq,f)
for o in src.values():bpy.data.objects.remove(o,do_unlink=True)
for c in action.fcurves:
 for k in c.keyframe_points:k.interpolation='LINEAR'
assert digest()==before_digest,'Another motion changed'

exec(Path(__file__).with_name('finish_dark_axe_chop.py').read_text(encoding='utf8'))
