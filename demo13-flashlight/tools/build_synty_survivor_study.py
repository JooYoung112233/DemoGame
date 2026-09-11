"""Non-destructive Synty-derived proportion study; preserves source topology and keys."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector,Quaternion
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/Synty/SidekickCharacters'
OUT=ROOT/'Assets/ChibiSurvivor/Player/SyntySurvivorStudy'
OUT.mkdir(parents=True,exist_ok=True)
(OUT/'BlenderSource~').mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
rig=None;parts=[];sources=[]
def mat(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.85;p.inputs['Specular IOR Level'].default_value=.2
 return m
skin=mat('Survivor_Skin',(.43,.265,.145));olive=mat('Survivor_Olive',(.11,.105,.062));dark=mat('Survivor_Charcoal',(.035,.042,.043));leather=mat('Survivor_Leather',(.075,.044,.025));hair=mat('Survivor_Hair',(.025,.019,.015));cream=mat('Survivor_Linen',(.38,.34,.245))
eyes=mat('Source_EyePalette',(1,1,1));nt=eyes.node_tree;im=nt.nodes.new('ShaderNodeTexImage');im.image=bpy.data.images.load(str(BASE/'Characters/HumanSpecies/HumanSpecies_01/Textures/T_HumanSpecies_01ColorMap.png'));im.interpolation='Closest';nt.links.new(im.outputs['Color'],nt.nodes['Principled BSDF'].inputs['Base Color'])
def imp(stem,material):
 global rig
 f=next((BASE/'Resources/Meshes').rglob(stem+'.fbx'));old=set(bpy.data.objects)
 bpy.ops.import_scene.fbx(filepath=str(f));new=set(bpy.data.objects)-old
 arm=next(o for o in new if o.type=='ARMATURE')
 if rig is None:rig=arm;rig.name='SyntySurvivor_Rig'
 for o in new:
  if o.type!='MESH':continue
  mw=o.matrix_world.copy();o.parent=rig;o.matrix_world=mw
  for m in o.modifiers:
   if m.type=='ARMATURE':m.object=rig
  o.data.materials.clear();o.data.materials.append(material)
  for p in o.data.polygons:p.material_index=0
  parts.append(o);o['source_fbx']=str(f.relative_to(ROOT));sources.append(o['source_fbx'])
 for o in new:
  if o.type!='MESH' and o!=rig:bpy.data.objects.remove(o,do_unlink=True)
 return parts[-1]
for code in ['01HEAD','03EBRL','04EBRR','05EYEL','06EYER','07EARL','08EARR','35NOSE','36TETH','37TONG']:
 imp('SK_HUMN_BASE_01_'+code+'_HU01',eyes if code in ['05EYEL','06EYER'] else hair if code in ['03EBRL','04EBRR'] else cream if code=='36TETH' else skin)
imp('SK_HUMN_BASE_08_02HAIR_HU01',hair)
for code in ['10TORS','11AUPL','12AUPR','13ALWL','14ALWR','17HIPS','18LEGL','19LEGR','20FOTL','21FOTR']:
 imp(('SK_SCFI_CIVL_09_' if code[:2] in ['20','21'] else 'SK_HUMN_BASE_01_')+code+'_HU01',olive if code[:2] in ['10','11','12'] else skin if code[:2] in ['13','14'] else leather if code[:2] in ['20','21'] else dark)
for code in ['15HNDL','16HNDR']:imp('SK_HUMN_BASE_01_'+code+'_HU01',skin)
# One continuous deformation applies to every mesh/key and the rest skeleton.
# Head ~0.50 m; neck-to-floor ~1.12 m. T-pose arms compressed separately.
def warp(p):
 x,y,z=p
 t=max(0,min(1,(z-1.37)/.13));t=t*t*(3-2*t)
 if z<1.50:t*=max(0,min(1,(.22-abs(x))/.08))
 xx=x*(1+.95*t);yy=y*(1+.26*t)
 if abs(x)>.205 and z<1.49:xx=math.copysign(.205+(abs(x)-.205)*.80,x)
 zz=z*.8 if z<.93 else .744+(z-.93)*.78 if z<1.416 else 1.12308+(z-1.416)*1.50
 # Lift the chin region while preserving the shared neck boundary.
 if 1.49<z<1.63:zz+=.042*math.sin(math.pi*(z-1.49)/.14)
 return Vector((xx,yy,zz))
for o in parts:
 mw=o.matrix_world.copy();inv=mw.inverted()
 groups=[k.data for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else [o.data.vertices]
 for group in groups:
  for v in group:
   p=mw@v.co
   if any(c in o.name for c in ['10TORS','11AUPL','12AUPR']):
    p.y*=1.12
    if '10TORS' in o.name:p.x*=1.06
   if any(c in o.name for c in ['17HIPS','18LEGL','19LEGR']):
    p.y*=1.30
    if '17HIPS' in o.name and p.y<-.085:p.y=-.115
    if '18LEGL' in o.name or '19LEGR' in o.name:
     center=.10 if p.x>0 else -.10;p.x=center+(p.x-center)*1.20
   v.co=inv@warp(p)
 # Keys are retained; restrained expression instead of new sculpted facial detail.
 if o.data.shape_keys:
  for k in o.data.shape_keys.key_blocks:
   if k.name in ['browFrownLeft','browFrownRight']:k.value=.15
   if k.name in ['eyeWideUpperLeft','eyeWideUpperRight']:k.value=.25
 o['study_stage']='Proportion and palette; animation not validated'
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT');mw=rig.matrix_world.copy();inv=mw.inverted()
for b in rig.data.edit_bones:
 h=warp(mw@b.head);t=warp(mw@b.tail);b.head=inv@h;b.tail=inv@t
bpy.ops.object.mode_set(mode='OBJECT')
# Separate cap, pockets, satchel and mitten hands remain replaceable objects.
def attach(o,bone,material):
 o.data.materials.clear();o.data.materials.append(material)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 vg=o.vertex_groups.new(name=bone);vg.add(list(range(len(o.data.vertices))),1,'REPLACE')
 mod=o.modifiers.new('Skin','ARMATURE');mod.object=rig;parts.append(o)
 return o
def box(name,loc,size,material,bone,bevel=.008):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('SoftCorners','BEVEL');m.width=bevel;m.segments=1;bpy.ops.object.modifier_apply(modifier=m.name)
 return attach(o,bone,material)
box('Survivor_ShirtPocket_L',(.115,-.135,1.002),(.105,.025,.12),olive,'spine_03')
box('Survivor_ShirtPocket_R',(-.115,-.135,1.002),(.105,.025,.12),olive,'spine_03')
box('Survivor_ShirtPlacket',(0,-.155,.969),(.018,.014,.25),olive,'spine_03',.003)
box('Survivor_Belt',(0,0,.754),(.37,.285,.047),leather,'pelvis')
box('Survivor_Buckle',(0,-.153,.754),(.046,.018,.035),cream,'pelvis',.003)
box('Survivor_Pouch',(-.17,-.12,.713),(.125,.09,.135),leather,'pelvis')
box('Survivor_Backpack',(0,.205,1.015),(.31,.17,.34),dark,'spine_03',.025)
for sign in [-1,1]:box('Survivor_ShoulderStrap',(.155*sign,-.132,1.055),(.034,.03,.24),leather,'spine_03')
for code,bone in [('15HNDL','hand_l'),('16HNDR','hand_r')]:
 o=next(o for o in parts if code in o.name);center=rig.matrix_world@rig.data.bones[bone].head_local+Vector((.027 if bone.endswith('l') else -.027,0,0))
 o.hide_render=True;o.hide_set(True)
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,location=center);m=bpy.context.object;m.name='Survivor_Mitten_'+bone;m.scale=(.069,.048,.045);attach(m,bone,skin)
 # Keep a thumb silhouette without separate fingers.
 bpy.ops.mesh.primitive_uv_sphere_add(segments=10,ring_count=6,location=center+Vector((-.025 if bone.endswith('l') else .025,-.038,0)));m=bpy.context.object;m.name='Survivor_Thumb_'+bone;m.scale=(.034,.025,.027);attach(m,bone,skin)
# Faceted cap crown with a genuinely projecting bill, sized to the source head.
verts=[];faces=[];n=16
for z,rx,ry in [(1.465,.224,.174),(1.57,.228,.179),(1.665,.185,.153),(1.72,.102,.094),(1.735,.025,.025)]:
 for i in range(n):a=2*math.pi*i/n;verts.append((rx*math.cos(a),.005+ry*math.sin(a),z))
for r in range(4):
 for i in range(n):j=(i+1)%n;faces.append((r*n+i,r*n+j,(r+1)*n+j,(r+1)*n+i))
faces.append(tuple(range(4*n,5*n)))
mesh=bpy.data.meshes.new('CapCrown');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Survivor_CapCrown',mesh);scene.collection.objects.link(o);attach(o,'head',olive)
vs=[(-.20,-.10,1.477),(-.11,-.164,1.49),(0,-.18,1.495),(.11,-.164,1.49),(.20,-.10,1.477),(-.21,-.25,1.435),(-.12,-.31,1.45),(0,-.33,1.455),(.12,-.31,1.45),(.21,-.25,1.435)]
fs=[(i,i+1,i+6,i+5) for i in range(4)];d=bpy.data.meshes.new('CapBill');d.from_pydata(vs,[],fs);d.update();o=bpy.data.objects.new('Survivor_CapBill',d);scene.collection.objects.link(o);attach(o,'head',dark)
box('Survivor_CapPatch',(0,-.169,1.606),(.096,.014,.061),leather,'head',.003)
for o in parts:
 if 'Cap' in o.name:
  for v in o.data.vertices:v.co.z=1.525+(v.co.z-1.465)*.75
 if '02HAIR' in o.name:
  o.hide_render=True;o.hide_set(True)
for side,sign in [('l',1),('r',-1)]:
 foot=rig.matrix_world@rig.data.bones['foot_'+side].head_local
 box('Survivor_Boot_'+side,(foot.x,-.055,.092),(.15,.285,.165),leather,'foot_'+side,.025)
 box('Survivor_Sole_'+side,(foot.x,-.06,.014),(.158,.293,.035),dark,'foot_'+side,.009)
for o in parts:
 if '20FOTL' in o.name or '21FOTR' in o.name:o.hide_render=True;o.hide_set(True)
for side,sign in [('l',1),('r',-1)]:
 pb=rig.pose.bones.get('upperarm_'+side)
 axis=(rig.matrix_world.to_3x3()@pb.bone.matrix_local.to_3x3()).inverted()@Vector((0,1,0))
 pb.rotation_mode='QUATERNION';pb.rotation_quaternion=Quaternion(axis.normalized(),math.radians(sign*67))
bpy.context.view_layer.update()
# Studio scene is separate from the model collection.
model=bpy.data.collections.new('CHARACTER - editable Synty parts');scene.collection.children.link(model)
for o in [rig]+parts:
 for c in list(o.users_collection):c.objects.unlink(o)
 model.objects.link(o)
def point(o,target):o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='StudioFloor';floor.location.z=-.015;floor.data.materials.append(mat('Studio',(.038,.043,.041)))
for name,pos,power,size in [('Key',(-3,-4,6),450,4),('Fill',(4,-2,3),180,3),('Rim',(1,3,4),500,3)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos;point(o,(0,0,.9))
scene.world=bpy.data.worlds.new('StudioWorld');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.14,.16,.18,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
camd=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',camd);scene.collection.objects.link(cam);scene.camera=cam;camd.type='ORTHO';camd.ortho_scale=2.05
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=800;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
for name,pos in [('Preview',(3,-6,2.7)),('Front',(0,-7,1.45)),('Side',(7,0,1.45))]:
 cam.location=pos;point(cam,(0,0,.85));scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
cam.location=(3,-6,2.7);point(cam,(0,0,.85))
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/SyntySurvivor_Study.blend'))
print('STUDY_COMPLETE',len(parts),'parts; source FBX files untouched')

