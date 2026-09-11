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
 # Replace the inherited long coat, inflated sleeves and short trousers below.
 for mod in list(o.modifiers):
  if mod.type!='ARMATURE':bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
# A small closed face block replaces the hidden skull; no cut-and-capped
# n-gons can cross the visible face when the FBX importer triangulates it.
colors={'skin':(.55,.335,.18),'jacket':(.145,.093,.059),'pants':(.054,.061,.069),'boots':(.13,.081,.045),'hood':(.065,.067,.086),'mask':(.235,.185,.131)}
def mat(key):
 m=bpy.data.materials.new('SimpleBandit_'+key);m.diffuse_color=(*colors[key],1);return m
hoodmat=mat('hood');maskmat=mat('mask')
def mesh(name,vertices,faces,material,bone):
 me=bpy.data.meshes.new(name);me.from_pydata(vertices,[],faces);me.materials.append(material);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(name,me);sc.collection.objects.link(o);o.parent=rig
 g=o.vertex_groups.new(name=bone);g.add(list(range(len(me.vertices))),1,'REPLACE')
 parts.append(o);return o
old_head=bpy.data.objects['Study_Head'];parts.remove(old_head);bpy.data.objects.remove(old_head,do_unlink=True)
face_outline=[(-.278,1.302),(.278,1.302),(.275,1.494),(.221,1.545),(-.221,1.545),(-.275,1.494)]
face_vertices=[(x,-.221,z) for x,z in face_outline]+[(x*.95,-.075,z) for x,z in face_outline]
face_faces=[tuple(range(6)),tuple(reversed(range(6,12)))]+[(i,(i+1)%6,(i+1)%6+6,i+6) for i in range(6)]
head=mesh('Study_Head',face_vertices,face_faces,mat('skin'),'Head');head.modifiers.new('Skin','ARMATURE').object=rig
def smooth(a,b,z):
 t=max(0,min(1,(z-a)/(b-a)));return t*t*(3-2*t)
def reweight(o):
 o.vertex_groups.clear()
 for v in o.data.vertices:
  x,y,z=v.co;side='L' if x>=0 else 'R';n=o.name
  if n.startswith('Study_Sleeve'):
   upper=smooth(.795,.875,z);chest=smooth(1.02,1.10,z)*(1-smooth(.18,.245,abs(x)))
   w={'Forearm.'+side:(1-upper)*(1-chest),'UpperArm.'+side:upper*(1-chest),'Chest':chest}
  elif n.startswith('Study_Leg'):
   hip=smooth(.52,.605,z);thigh=smooth(.325,.405,z)*(1-hip);w={'Hips':hip,'Thigh.'+side:thigh,'Shin.'+side:1-hip-thigh}
  elif n=='Study_Hips':w={'Hips':1}
  elif z<.86:
   t=smooth(.65,.82,z);w={'Hips':1-t,'Spine':t}
  else:
   t=smooth(.86,1.015,z);w={'Spine':1-t,'Chest':t}
  for bone,value in w.items():
   if value>1e-7:(o.vertex_groups.get(bone) or o.vertex_groups.new(name=bone)).add([v.index],value,'REPLACE')
 m=o.modifiers.new('Skin','ARMATURE');m.object=rig
outline=[(-1,-.60),(-.65,-1),(.65,-1),(1,-.60),(1,.60),(.65,1),(-.65,1),(-1,.60)]
def replace_rings(name,rows,key):
 old=bpy.data.objects[name];parts.remove(old);bpy.data.objects.remove(old,do_unlink=True)
 shape=outline
 if name.startswith('Study_Sleeve'):
  radius=.65
  shape=[(cx+radius*math.cos(math.radians(a+90*i/3)),cy+radius*math.sin(math.radians(a+90*i/3))) for cx,cy,a in [(1-radius,-1+radius,-90),(1-radius,1-radius,0),(-1+radius,1-radius,90),(-1+radius,-1+radius,180)] for i in range(4)]
 count=len(shape)
 vs=[(cx+x*w/2,cy+y*d/2,z) for z,w,d,cx,cy in rows for x,y in shape]
 fs=[tuple(reversed(range(count)))]
 for j in range(len(rows)-1):
  for i in range(count):fs.append((j*count+i,j*count+(i+1)%count,(j+1)*count+(i+1)%count,(j+1)*count+i))
 fs.append(tuple(range((len(rows)-1)*count,len(rows)*count)))
 o=mesh(name,vs,fs,mat(key),'Chest');reweight(o);return o
replace_rings('Study_Torso',[(.697,.485,.271,0,0),(.718,.483,.277,0,0),(.85,.44,.27,0,0),(.98,.428,.263,0,0),(1.055,.37,.242,0,0),(1.105,.25,.185,0,0)],'jacket')
replace_rings('Study_Hips',[(.555,.31,.197,0,.006),(.60,.395,.222,0,.006),(.725,.434,.24,0,.006)],'pants')
for s in [-1,1]:
 replace_rings('Study_Sleeve_'+str(s),[(.636,.15,.162,s*.341,-.013),(.72,.153,.171,s*.323,-.009),(.818,.16,.181,s*.30,-.002),(.853,.169,.19,s*.294,0),(.853,.207,.235,s*.294,0),(.883,.207,.235,s*.284,0),(.906,.192,.219,s*.278,0),(.982,.187,.218,s*.247,0),(1.044,.166,.198,s*.222,0),(1.087,.117,.16,s*.193,0),(1.104,.065,.113,s*.177,0)],'jacket')
 replace_rings('Study_Leg_'+str(s),[(.185,.20,.209,s*.157,.01),(.218,.207,.219,s*.154,.01),(.233,.207,.219,s*.153,.01),(.239,.190,.20,s*.153,.01),(.355,.206,.226,s*.146,.012),(.49,.216,.238,s*.132,.01),(.665,.208,.246,s*.113,.01)],'pants')
 for v in bpy.data.objects['Study_Leg_'+str(s)].data.vertices:v.co.x+=s*.018*max(0,min(1,(.665-v.co.z)/.46))
 for v in bpy.data.objects['Study_Boot_'+str(s)].data.vertices:v.co.x+=s*.048
# Explicit front contours: a narrow crown widening to a draped cheek opening,
# then tapering into the neck. The hood is not a scaled spherical helmet.
outer_half=[(0,1.79),(.145,1.783),(.245,1.714),(.293,1.57),(.358,1.342),(.334,1.185),(.217,1.077),(.10,1.065),(0,1.065)]
inner_half=[(0,1.525),(.14,1.519),(.237,1.476),(.276,1.399),(.282,1.317),(.257,1.205),(.175,1.128),(.082,1.112),(0,1.109)]
def contour(half):return half+[(-x,z) for x,z in half[-2:0:-1]]
outer=contour(outer_half);inner=contour(inner_half);N=len(outer);vs=[]
for layer in range(5):
 for i in range(N):
  x,z=(inner if layer==0 else outer)[i]
  if layer==0:y=-.272+.115*max(0,(1.31-z)/.21)
  elif layer==1:y=-.287+.176*max(0,(z-1.34)/.45)+.13*max(0,(1.25-z)/.19)
  elif layer==2:x*=.95;z=1.414+(z-1.414)*.96;y=.047
  elif layer==3:x*=.81;z=1.426+(z-1.414)*.83;y=.21
  else:x*=.42;z=1.426+(z-1.414)*.48;y=.27
  vs.append((x,y,z))
fs=[]
# Extra broad fold ring breaks up the flat forehead without smoothing the hood.
front_fold=[]
for i in range(N):
 a=Vector(vs[i]);b=Vector(vs[N+i]);p=a.lerp(b,.52)
 p.y-=.045*max(0,min(1,(p.z-1.19)/.20));front_fold.append(tuple(p))
vs=vs[:N]+front_fold+vs[N:]
for j in range(5):
 for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
fs.append(tuple(range(5*N,6*N)))
hood=mesh('Bandit_Hood',vs,fs,hoodmat,'Head');bpy.context.view_layer.objects.active=hood
solid=hood.modifiers.new('HoodThickness','SOLIDIFY');solid.thickness=.013;solid.offset=-1;bpy.ops.object.modifier_apply(modifier=solid.name)
mask_vs=[(-.279,-.232,1.316),(-.196,-.235,1.319),(0,-.243,1.333),(.196,-.235,1.319),(.279,-.232,1.316),(.197,-.221,1.18),(0,-.224,1.035),(-.197,-.221,1.18),(0,-.249,1.23)]
# Broad triangular cloth planes radiate from one soft fold, rather than forming
# the previous horizontal rectangular stripe across the lower face.
mask_fs=[(i,(i+1)%8,8) for i in range(8)]
# Give the cloth a constant depth thickness with explicit, inspectable faces.
mask_vs+= [(x,y+.009,z) for x,y,z in mask_vs.copy()]
mask_fs+= [tuple(i+9 for i in reversed(f)) for f in mask_fs.copy()]
mask_fs+= [(i,(i+1)%8,(i+1)%8+9,i+9) for i in range(8)]
mask=mesh('Bandit_Mask',mask_vs,mask_fs,maskmat,'Head')
for o in [hood,mask]:
 g=o.vertex_groups.get('Head');g.add(list(range(len(o.data.vertices))),1,'REPLACE');m=o.modifiers.new('Skin','ARMATURE');m.object=rig
regions={'HEAD_HOOD':(.02,.51,.55,.47),'JACKET':(.59,.51,.39,.47),'LEGS_BOOTS':(.02,.02,.55,.47),'SKIN':(.59,.02,.39,.47)}
def bucket(n):
 if n in ['Study_Head','Bandit_Hood','Bandit_Mask','Bandit_MaskBib']:return 'HEAD_HOOD'
 if n=='Study_Torso' or n.startswith('Study_Sleeve'):return 'JACKET'
 if n=='Study_Hips' or n.startswith(('Study_Leg','Study_Boot')):return 'LEGS_BOOTS'
 return 'SKIN'
def paint(name,material,p):
 x,y,z=p.T;n=len(p);key='skin'
 if name=='Study_Torso' or name.startswith('Study_Sleeve'):key='jacket'
 if name=='Study_Hips' or name.startswith('Study_Leg'):key='pants'
 if name.startswith('Study_Boot'):key='boots'
 if name=='Bandit_Hood':key='hood'
 if name.startswith('Bandit_Mask'):key='mask'
 c=np.tile(colors[key],(n,1));h=np.zeros(n);rough=np.full(n,.91)
 def fill(mask,col):c[mask]=col
 if name=='Study_Head':
  front=y<-.16
  for center in [-.103,.103]:
   eye=((x-center)/.015)**2+(np.maximum(abs(z-1.375)-.021,0)/.015)**2<=1
   brow=(abs(x-center)<.045)&(abs(z-(1.456+(abs(x)-.103)*.19))<.010)
   fill(front&(eye|brow),(.02,.016,.013))
 elif name=='Study_Torso':
  front=y<-.065;opening=.069+.018*np.clip((z-.74)/.36,0,1)
  fill(front&(abs(x)<opening),(.16,.16,.15));fill(front&(abs(abs(x)-opening)<.004),(.078,.049,.031))
  fill((z<.718)&(abs(x)>.071),(.12,.073,.044));fill(front&(abs(abs(x)-.206)<.003)&(z<.86),(.101,.062,.037))
 elif name.startswith('Study_Sleeve'):
  fill(z<.852,colors['skin']);fill((z>=.852)&(z<.886),(.169,.109,.068));fill((z>.852)&(z<.86),(.085,.051,.03))
  if name.endswith('_1'):
   band=(z>.679)&(z<.786);fill(band,(.275,.243,.192))
   lines=band&(np.mod(z+x*.10+y*.11,.033)<.003);fill(lines,(.203,.173,.132))
 elif name.startswith('Study_Leg'):
  fill(z<.238,(.069,.076,.085));fill((z>.234)&(z<.241),(.032,.038,.045))
  if name.endswith('_-1'):
   edge=.029+.009*np.sin((z-.353)*390)
   patch=(y<-.077)&(abs(x+.15)<edge)&(abs(z-.353)<.021+.008*np.sin(x*420))
   fill(patch,(.238,.17,.106))
 elif name.startswith('Study_Boot'):
  fill(z<.068,(.028,.025,.024));rough[:]=.82
 elif name=='Bandit_Hood':
  fill((y<-.245)&(z<1.51),(.048,.049,.064))
 elif name=='Bandit_Mask':
  fill((z<1.23)&(x>0),(.222,.173,.121))
 elif name=='Bandit_MaskBib':c[:]=(.215,.163,.114)
 return c,h,0,rough
# Author UVs on the exact triangles exported to Unity. Cut-and-capped facial
# n-gons otherwise admit different triangulations in Blender and the FBX importer.
for o in parts:
 bm=bmesh.new();bm.from_mesh(o.data)
 bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED',ngon_method='EAR_CLIP')
 bm.to_mesh(o.data);bm.free();o.data.update()
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
sc.view_settings.exposure=.2
bpy.data.objects['Preview_Fill'].data.energy=155
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
