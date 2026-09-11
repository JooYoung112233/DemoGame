"""Fit the free two-hand walk's original hand spacing to the shorter character."""
import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/TwoHandWalkReview';CACHE=ROOT/'Library/CodexBlender/TwoHandWalkReview'
bpy.ops.wm.open_mainfile(filepath=str(CACHE/'Render.blend'));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];action=rig.animation_data.action
characters=[o for o in scene.objects if o.type=='MESH'];end=25
other=[a for a in bpy.data.actions if a!=action]
def digest():return hashlib.sha256(repr([(a.name,[(c.data_path,c.array_index,[tuple(k.co) for k in c.keyframe_points]) for c in a.fcurves]) for a in other]).encode()).hexdigest()
before_digest=digest();rest={b.name:b.matrix_local.copy() for b in rig.data.bones}
# Remove remaining horizontal cycle displacement; keep vertical weight transfer.
hip=rig.pose.bones['Hips'];scene.frame_set(1);start=hip.location.copy();scene.frame_set(end);finish=hip.location.copy()
drift=rest['Hips'].to_3x3()@(finish-start);drift.z=0;drift=rest['Hips'].to_3x3().inverted()@drift
hips=[]
for f in range(1,end+1):scene.frame_set(f);hips.append(hip.location.copy()-drift*((f-1)/(end-1)))
for f,position in enumerate(hips,1):hip.location=position;hip.keyframe_insert('location',frame=f,group='Hips')
before=set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/2Hand-Sword/RPG-Character@2Hand-Sword-Walk.FBX'))
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
gap=.14;previous={}
def rotate(n,q,f):
 p=rig.pose.bones[n];local=rest[n].to_quaternion().inverted()@rest[p.parent.name].to_quaternion()@p.parent.matrix.to_quaternion().inverted()@q
 if n in previous and previous[n].dot(local)<0:local.negate()
 previous[n]=local.copy();p.rotation_quaternion=local;p.keyframe_insert('rotation_quaternion',frame=f,group=n);bpy.context.view_layer.update()
for f,s in enumerate(samples,1):
 scene.frame_set(f);bpy.context.view_layer.update();socketq=s['axis'].to_track_quat('Y','Z')
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
col=bpy.data.collections.new('PREVIEW_ONLY_TwoHandSword');scene.collection.children.link(col)
grip=bpy.data.objects.new('Preview_TwoHand_Grip',None);col.objects.link(grip);constraint=grip.constraints.new('COPY_TRANSFORMS');constraint.target=rig;constraint.subtarget='HandSocket.R'
grip['purpose']='Replaceable sword silhouette; source animation is the free 2Hand-Sword-Walk'
def mat(name,color,metal=0):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.72;p.inputs['Metallic'].default_value=metal;return m
steel=mat('Review_Steel',(.28,.31,.30),.4);wood=mat('Review_Grip',(.075,.04,.02))
def box(name,loc,size,material):
 bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.name=name;o.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 for c in list(o.users_collection):c.objects.unlink(o)
 col.objects.link(o);o.parent=grip;o.location=loc;o.data.materials.append(material);return o
box('Preview_TwoHand_Handle',(0,-gap*.5,0),(.03,gap+.08,.03),wood)
guard=box('Preview_TwoHand_Guard',(0,.065,0),(.065,.012,.018),steel)
outline=[(-.030,.073),(.030,.073),(.025,.59),(0,.68),(-.025,.59)];verts=[(x,y,z) for z in [-.005,.005] for x,y in outline];faces=[(4,3,2,1,0),(5,6,7,8,9)]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)]
m=bpy.data.meshes.new('ReviewBlade');m.from_pydata(verts,[],faces);m.update();blade=bpy.data.objects.new('Preview_TwoHand_Blade',m);col.objects.link(blade);blade.parent=grip;m.materials.append(steel)
def tree(o,dg):
 ev=o.evaluated_get(dg);m=ev.to_mesh();t=BVHTree.FromPolygons([ev.matrix_world@v.co for v in m.vertices],[tuple(p.vertices) for p in m.polygons]);ev.to_mesh_clear();return t
hits=[];errors=[]
for f in range(1,end+1):
 scene.frame_set(f);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
 errors.append((rig.pose.bones['HandSocket.L'].head-grip.matrix_world@Vector((0,-gap,0))).length)
 bt=tree(blade,dg);gt=tree(guard,dg)
 for o in characters:
  t=tree(o,dg)
  if bt.overlap(t):hits.append([f,o.name,'blade'])
  if o.name!='Compact_Hands' and gt.overlap(t):hits.append([f,o.name,'guard'])
assert max(errors)<.001,('Hands detached',max(errors))
if hits:
 scene.frame_set(1);scene.camera.location=(-4.8,-7,1.9);scene.camera.rotation_euler=(Vector((0,0,.79))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=2.2
 scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=8;scene.cycles.use_denoising=True;scene.render.resolution_x=600;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(CACHE/'GripDiagnosis.png');bpy.ops.render.render(write_still=True)
assert not hits,('Blade/guard overlap',hits[:8])
stats=json.loads((OUT/'RetargetCheck.json').read_text());stats.update({'source_clip':'2Hand-Sword-Walk','two_hand_grip_spacing_m':gap,'max_support_hand_error_m':max(errors),'weapon_surface_intersections':hits,'other_actions_unchanged':True,'preview_description':'Free 2Hand-Sword-Walk, retargeted to character proportions with shared grip correction, in-place cycle correction and a removable sword proxy.'})
(OUT/'RetargetCheck.json').write_text(json.dumps(stats,indent=2));scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CompactSurvivor_TwoHandWalk.blend'))
print('FREE_TWO_HAND_WALK_READY',json.dumps({'grip_spacing':gap,'grip_error':max(errors),'intersections':hits}),flush=True)
