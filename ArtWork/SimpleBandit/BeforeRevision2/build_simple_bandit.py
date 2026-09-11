"""Simple fixed-outfit bandit, built from the approved survivor rig and shapes."""
import bpy,bmesh,math,json,sys,hashlib,shutil
import numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from character_uv_atlas import author_atlas
OUT=ROOT/'Assets/ChibiSurvivor/Bandit/SimpleBandit';REV=ROOT.parent/'ArtWork/SimpleBandit'
for p in [OUT/'BlenderSource~',OUT/'Textures',REV]:p.mkdir(parents=True,exist_ok=True)
SRC=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_TwoHandBat.blend'
source_hash=hashlib.sha256(SRC.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SRC));bpy.context.preferences.filepaths.save_version=0
sc=bpy.context.scene;rig=bpy.data.objects['SimpleHero_Rig'];rig.data.pose_position='REST';bpy.context.view_layer.update()
for o in list(sc.objects):
 if o.name.startswith('Gear_') or o.name.startswith('Study_Cap'):bpy.data.objects.remove(o,do_unlink=True)
parts=[o for o in sc.objects if o.type=='MESH' and o.name.startswith('Study_')]
bat=bpy.data.objects['Hero_Bat'];bat.name='Bandit_Bat';bat.hide_render=True
# The source bat has closed shells with inward winding; Unity culls their outside.
bat_bm=bmesh.new();bat_bm.from_mesh(bat.data)
bmesh.ops.recalc_face_normals(bat_bm,faces=list(bat_bm.faces))
bat_bm.to_mesh(bat.data);bat_bm.free();bat.data.update()
# Keep rest bones and approved action keys identical for direct motion reuse.
def action_hash():return hashlib.sha256(repr([(a.name,[(f.data_path,f.array_index,[(tuple(k.co),k.interpolation) for k in f.keyframe_points]) for f in a.fcurves]) for a in bpy.data.actions]).encode()).hexdigest()
motion_hash=action_hash()
for o in parts:
 n=o.name
 for v in o.data.vertices:
  x,y,z=v.co
  if n=='Study_Torso':x*=1.10;y*=1.05
  elif n=='Study_Hips':x*=1.09;y*=1.04
  elif n.startswith('Study_Leg'):
   side=1 if x>0 else -1;x=side*.12+(x-side*.12)*1.08;y*=1.035
  elif n.startswith('Study_Sleeve'):
   side=1 if x>0 else -1;center=side*(.345-(z-.64)/.465*.14)
   factor=1.08 if z>.86 else .86
   x=center+(x-center)*factor;y*=factor
  v.co=(x,y,z)
 for mod in list(o.modifiers):
  if mod.type!='ARMATURE':bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
# The fixed mask covers the lower face; remove hidden skin instead of layering it through cloth.
head=bpy.data.objects['Study_Head'];bm=bmesh.new();bm.from_mesh(head.data)
bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=(0,0,1.306),plane_no=(0,0,1),clear_inner=True,clear_outer=False)
boundary=[e for e in bm.edges if e.is_boundary]
if boundary:bmesh.ops.holes_fill(bm,edges=boundary,sides=0)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(head.data);bm.free()
head.vertex_groups.get('Head').add(list(range(len(head.data.vertices))),1,'REPLACE')
colors={'skin':(.49,.30,.165),'jacket':(.115,.076,.05),'pants':(.047,.053,.06),'boots':(.112,.075,.045),'hood':(.052,.054,.070),'mask':(.195,.154,.108)}
def mat(key):
 m=bpy.data.materials.new('SimpleBandit_'+key);m.diffuse_color=(*colors[key],1);return m
hoodmat=mat('hood');maskmat=mat('mask')
def mesh(name,vertices,faces,material,bone):
 me=bpy.data.meshes.new(name);me.from_pydata(vertices,[],faces);me.materials.append(material);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(name,me);sc.collection.objects.link(o);o.parent=rig
 g=o.vertex_groups.new(name=bone);g.add(list(range(len(me.vertices))),1,'REPLACE')
 parts.append(o);return o
N=16;vs=[]
for layer in range(5):
 for i in range(N):
  t=i*math.tau/N;sn=math.sin(t);cs=math.cos(t);xx=math.copysign(abs(sn)**.72,sn);zz=math.copysign(abs(cs)**.8,cs)
  if layer==0:x=.292*xx;z=1.382+(.175 if zz>0 else .222)*zz;y=-.246+.088*max(0,-zz)
  elif layer==1:x=.345*xx;z=1.405+(.353 if zz>0 else .282)*zz;y=-.224+.10*max(0,-zz)
  elif layer==2:x=.344*xx;z=1.411+(.323 if zz>0 else .267)*zz;y=.063
  elif layer==3:x=.294*xx;z=1.415+.274*zz;y=.234
  else:x=.17*xx;z=1.411+.185*zz;y=.318
  vs.append((x,y,z))
fs=[]
for j in range(4):
 for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
fs.append(tuple(range(4*N,5*N)))
hood=mesh('Bandit_Hood',vs,fs,hoodmat,'Head');bpy.context.view_layer.objects.active=hood
solid=hood.modifiers.new('HoodThickness','SOLIDIFY');solid.thickness=.013;solid.offset=-1;bpy.ops.object.modifier_apply(modifier=solid.name)
mask=mesh('Bandit_Mask',[(-.294,-.08,1.318),(-.205,-.236,1.31),(0,-.251,1.307),(.205,-.236,1.31),(.294,-.08,1.318),(-.246,-.14,1.195),(0,-.25,1.076),(.246,-.14,1.195)],[(0,1,5),(1,2,6,5),(2,3,7,6),(3,4,7)],maskmat,'Head')
bpy.context.view_layer.objects.active=mask;solid=mask.modifiers.new('MaskThickness','SOLIDIFY');solid.thickness=.012;bpy.ops.object.modifier_apply(modifier=solid.name)
for o in [hood,mask]:
 g=o.vertex_groups.get('Head');g.add(list(range(len(o.data.vertices))),1,'REPLACE');m=o.modifiers.new('Skin','ARMATURE');m.object=rig
regions={'HEAD_HOOD':(.02,.51,.55,.47),'JACKET':(.59,.51,.39,.47),'LEGS_BOOTS':(.02,.02,.55,.47),'SKIN':(.59,.02,.39,.47)}
def bucket(n):
 if n in ['Study_Head','Bandit_Hood','Bandit_Mask']:return 'HEAD_HOOD'
 if n=='Study_Torso' or n.startswith('Study_Sleeve'):return 'JACKET'
 if n=='Study_Hips' or n.startswith(('Study_Leg','Study_Boot')):return 'LEGS_BOOTS'
 return 'SKIN'
def paint(name,material,p):
 x,y,z=p.T;n=len(p);key='skin'
 if name=='Study_Torso' or name.startswith('Study_Sleeve'):key='jacket'
 if name=='Study_Hips' or name.startswith('Study_Leg'):key='pants'
 if name.startswith('Study_Boot'):key='boots'
 if name=='Bandit_Hood':key='hood'
 if name=='Bandit_Mask':key='mask'
 c=np.tile(colors[key],(n,1));h=np.zeros(n);rough=np.full(n,.91)
 def fill(mask,col):c[mask]=col
 if name=='Study_Head':
  front=y<-.16
  for center in [-.103,.103]:
   eye=((x-center)/.015)**2+(np.maximum(abs(z-1.349)-.021,0)/.015)**2<=1
   brow=(abs(x-center)<.045)&(abs(z-(1.438+(abs(x)-.103)*.19))<.010)
   fill(front&(eye|brow),(.02,.016,.013))
 elif name=='Study_Torso':
  front=y<-.065;fill(front&(abs(x)<.065),(.155,.155,.141));fill(front&(abs(abs(x)-.073)<.005),(.068,.044,.03))
  fill((z<.648),(.088,.057,.038));fill(front&(abs(abs(x)-.206)<.003)&(z<.90),(.085,.053,.034))
 elif name.startswith('Study_Sleeve'):
  fill(z<.835,colors['skin']);fill((z>=.835)&(z<.867),(.14,.092,.057));fill((z>.835)&(z<.842),(.07,.044,.028))
  if name.endswith('_1'):
   band=(z>.679)&(z<.786);fill(band,(.275,.243,.192))
   lines=band&(np.mod(z+x*.10+y*.11,.033)<.003);fill(lines,(.203,.173,.132))
 elif name.startswith('Study_Leg'):
  fill(z<.222,(.066,.070,.072));fill((z>.219)&(z<.228),(.028,.033,.036))
  if name.endswith('_-1'):
   patch=(y<-.077)&(abs(x+.135)<.031)&(abs(z-.353)<.025)
   fill(patch,(.238,.17,.106))
 elif name.startswith('Study_Boot'):
  fill(z<.068,(.028,.025,.024));rough[:]=.82
 elif name=='Bandit_Hood':
  fill((y<-.215)&(z<1.57),(.041,.043,.055))
 elif name=='Bandit_Mask':
  fill((z<1.265)&(x>0),(.174,.135,.09))
 return c,h,0,rough
images,uvreport=author_atlas(parts,regions,bucket,paint,OUT/'Textures','SimpleBandit',2048)
material=bpy.data.materials.new('SimpleBandit_Surface');material.use_nodes=True;bs=material.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.9;bs.inputs['Specular IOR Level'].default_value=0
node=material.node_tree.nodes.new('ShaderNodeTexImage');node.image=images['BaseColor'];material.node_tree.links.new(node.outputs['Color'],bs.inputs['Base Color'])
for o in parts:
 o.data.materials.clear();o.data.materials.append(material)
 for f in o.data.polygons:f.material_index=0
 g=o.vertex_groups.new(name='Part_'+o.name);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
 # Ensure every exported vertex belongs to a deform bone, including solidified cloth.
 for v in o.data.vertices:
  weights=[(o.vertex_groups[g.group].name,g.weight) for g in v.groups if o.vertex_groups[g.group].name in rig.data.bones]
  assert weights and abs(sum(w for _,w in weights)-1)<.0001,(o.name,v.index,weights)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();body=bpy.context.object;body.name='Bandit_Body'
# Joining same shared material can preserve duplicate slots; consolidate to one.
body.data.materials.clear();body.data.materials.append(material)
for f in body.data.polygons:f.material_index=0
rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1);bpy.context.view_layer.update()
assert action_hash()==motion_hash
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=64;sc.render.resolution_x=800;sc.render.resolution_y=900;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.fps=30
sc.world.color=(.08,.08,.08)
def render(name,loc,action='Idle',frame=1,armed=False):
 rig.animation_data.action=bpy.data.actions[action];sc.frame_set(frame);bat.hide_render=not armed;sc.camera.location=loc;sc.camera.rotation_euler=(Vector((0,0,.88))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=2.6 if armed else 2.18;sc.render.filepath=str(REV/(name+'.png'));bpy.ops.render.render(write_still=True)
render('Front',(0,-6,1.95));render('Quarter',(3,-6,2.1))
lights=[o for o in sc.objects if o.type=='LIGHT'];saved_lights=[(o,o.location.copy(),o.rotation_euler.copy()) for o in lights]
for o in lights:o.location.y*=-1;o.rotation_euler=(Vector((0,0,.9))-o.location).to_track_quat('-Z','Y').to_euler()
render('Back',(2,6,2.1))
for o,loc,rot in saved_lights:o.location=loc;o.rotation_euler=rot
render('BatHold',(3,-6,2.1),'BatSwing',1,True);render('BatSwing',(3,-6,2.1),'BatSwing',19,True)
rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1);bat.hide_render=True;sc.camera.location=(3,-6,2.1);sc.camera.rotation_euler=(Vector((0,0,.88))-sc.camera.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/SimpleBandit.blend'))
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);bat.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'SimpleBandit.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_step=.125,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
assert hashlib.sha256(SRC.read_bytes()).hexdigest()==source_hash
report={'body_renderers':1,'body_materials':1,'held_weapon_separate':True,'bones':len(rig.data.bones),'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'body_vertices':len(body.data.vertices),'actions_preserved':True,'uv_overlapping_pixels':uvreport['overlapping_pixels'],'gameplay_applied':False,'source_preserved':True}
(REV/'BuildCheck.json').write_text(json.dumps(report,indent=2));print('SIMPLE_BANDIT_READY',report,flush=True)
