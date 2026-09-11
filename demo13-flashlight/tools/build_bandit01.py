"""Modular Bandit 01, derived from the approved hero proportions and skeleton."""
import bpy, math, json, random, hashlib, bmesh
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend'
OUT=ROOT/'Assets/ChibiSurvivor/Bandit/Bandit01';OUT.mkdir(parents=True,exist_ok=True)
BS=OUT/'BlenderSource~';BS.mkdir(exist_ok=True)
original_hash=hashlib.sha256(SRC.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SRC));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig'];rig.name='Bandit01_Rig';rig.data.name='Bandit01_Skeleton'
rig.animation_data.action=None
for pb in rig.pose.bones:pb.matrix_basis.identity()
bpy.context.view_layer.update()
for o in list(scene.objects):
 if o.name.startswith('Hero_') and any(x in o.name for x in ['Cap','Pack','Backpack','Lantern','Sword','Scarf','Lapel']):bpy.data.objects.remove(o,do_unlink=True)
parts=[o for o in scene.objects if o.type=='MESH' and o.name.startswith('Hero_')]
colors={'hood':(.043,.045,.049),'hood_edge':(.027,.029,.031),'mask':(.16,.127,.091),'jacket':(.080,.051,.032),'cuff':(.095,.064,.043),'under':(.095,.098,.089),'pants':(.027,.029,.029),'leather':(.062,.039,.024),'boots':(.080,.048,.028),'sole':(.019,.021,.02),'wrap':(.20,.177,.139),'metal':(.16,.16,.139)}
M={}
for key,c in colors.items():
 m=bpy.data.materials.new('Bandit_'+key);m.use_nodes=True;m.diffuse_color=(*c,1)
 p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.88;p.inputs['Specular IOR Level'].default_value=.2;M[key]=m
def assign(o,key):
 o.data.materials.clear();o.data.materials.append(M[key])
 for p in o.data.polygons:p.material_index=0
for o in parts:
 n=o.name;o.name=n.replace('Hero_','Bandit_')
 key=None
 if any(x in n for x in ['JacketBody','Sleeve']):key='cuff' if 'Rolled' in n else 'jacket'
 elif 'JacketOpening' in n:
  key='under'
  for v in o.data.vertices:v.co.x*=2.7;v.co.y-=.005
 elif 'Trouser' in n or 'Hips' in n:key='pants'
 elif 'Buckle' in n:key='metal'
 elif 'Belt' in n:key='leather'
 elif 'Sole' in n:key='sole'
 elif 'Boot' in n:key='boots'
 if key:assign(o,key)
 # Copy retained face/skin materials so future edits never affect hero palette.
 else:
  for slot in o.material_slots:
   m=slot.material.copy();m.name=slot.material.name.replace('Hero_','Bandit_');slot.material=m
def skin(o,bone):
 o.parent=rig;g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
 mod=o.modifiers.new('Bandit_Skin','ARMATURE');mod.object=rig;parts.append(o);return o
def mesh(name,vs,fs,key,bone):
 d=bpy.data.meshes.new(name);d.from_pydata(vs,[],fs);d.update()
 bm=bmesh.new();bm.from_mesh(d);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(d);bm.free()
 o=bpy.data.objects.new('Bandit_'+name,d);scene.collection.objects.link(o);d.materials.append(M[key]);return skin(o,bone)
def box(name,pos,size,key,bone,bevel=.006):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name='Bandit_'+name;o.dimensions=size
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 if bevel:
  mod=o.modifiers.new('ClothCorners','BEVEL');mod.width=bevel;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 assign(o,key);return skin(o,bone)
# Small ears stay inside the hood. Remove the hero's nose/mouth/cheek decals under the mask.
head=bpy.data.objects['Bandit_Head']
for vert in head.data.vertices:
 if abs(vert.co.x)>.278:vert.co.x=math.copysign(.278+(abs(vert.co.x)-.278)*.2,vert.co.x)
face=bpy.data.objects['Bandit_FaceDetails'];bm=bmesh.new();bm.from_mesh(face.data)
remove=[f for f in bm.faces if any(k in face.data.materials[f.material_index].name.lower() for k in ['nose','mouth','patch'])]
bmesh.ops.delete(bm,geom=remove,context='FACES');bm.to_mesh(face.data);bm.free()
# Lower the inner brow ends and re-project the decals onto the face surface.
browverts={i for p in face.data.polygons if 'hair' in face.data.materials[p.material_index].name.lower() for i in p.vertices}
for i in browverts:
 vert=face.data.vertices[i];vert.co.z+=(abs(vert.co.x)-.117)*.32-.007
 hit,p,_,_=head.ray_cast(Vector((vert.co.x,-1,vert.co.z)),Vector((0,1,0)))
 if hit:vert.co.y=p.y-.003
M['hair']=bpy.data.materials['Bandit_hair'] if 'Bandit_hair' in bpy.data.materials else bpy.data.objects['Bandit_Hair'].data.materials[0]
for k,outline in enumerate([[(-.21,1.57),(-.09,1.625),(-.10,1.475)], [(-.10,1.615),(.055,1.616),(.098,1.474),(-.005,1.53)],[(.05,1.605),(.18,1.575),(.208,1.49),(.155,1.515)]]):
 v=[]
 for x,z in outline:
  hit,p,_,_=head.ray_cast(Vector((x,-1,z)),Vector((0,1,0)));v.append((x,(p.y if hit else -.14)-.015,z))
 fringe=mesh('Fringe_'+str(k),v,[tuple(range(len(v)))],'hair','Head')
 bm=bmesh.new();bm.from_mesh(fringe.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=5,use_grid_fill=True);bm.to_mesh(fringe.data);bm.free()
 for vert in fringe.data.vertices:
  hit,p,_,_=head.ray_cast(Vector((vert.co.x,-1,vert.co.z)),Vector((0,1,0)))
  if hit:vert.co.y=p.y-.009
 fringe.vertex_groups[0].add(list(range(len(fringe.data.vertices))),1,'REPLACE')
 sol=fringe.modifiers.new('HairDepth','SOLIDIFY');sol.thickness=.004
# Hood is an open, thick shell: opening rim, broad crown facets, closed back.
N=16;vs=[]
for layer in range(5):
 for i in range(N):
  t=i*math.tau/N;c=math.cos(t);s=math.sin(t)
  if layer==0:x=.320*s;z=1.389+(.209 if c>0 else .249)*c;y=-.214+.095*max(0,-c)
  elif layer==1:x=.340*s;z=1.432+.303*c;y=-.10+.10*max(0,-c)
  elif layer==2:x=.341*s;z=1.431+.297*c;y=.082
  elif layer==3:x=.282*s;z=1.425+.264*c;y=.257
  else:x=.154*s;z=1.408+.170*c;y=.325
  # Cloth hangs toward the nape; shallow side folds interrupt the helmet-like rings.
  fold=math.sin(t*3+.4)*max(0,1-abs(c))
  if layer in (0,1,2):x+=.010*fold;y+=.012*fold
  z+=.009*s*(1 if layer<3 else .4)
  vs.append((x,y,z))
fs=[]
for j in range(4):
 for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
vs.append((0,.35,1.403));tip=len(vs)-1
for i in range(N):fs.append((4*N+i,4*N+(i+1)%N,tip))
hood=mesh('Hood',vs,fs,'hood','Head')
sol=hood.modifiers.new('FabricThickness','SOLIDIFY');sol.thickness=.012
# Separate dark inner rim gives the opening depth without a smooth balloon surface.
rimvs=vs[:N]+[(x*.965,y+.008,1.405+(z-1.405)*.966) for x,y,z in vs[:N]]
mesh('HoodOpening',rimvs,[(i,(i+1)%N,N+(i+1)%N,N+i) for i in range(N)],'hood_edge','Head')
# Face mask fitted to the existing face; the lower edge forms a cloth point.
head=bpy.data.objects['Bandit_Head'];vs=[]
for row in range(4):
 for i in range(9):
  x=(i-4)*.058;u=abs(x)/.232
  top=1.354-.024*u;bottom=1.163+.090*u;z=top+(bottom-top)*row/3
  hit,p,_,_=head.ray_cast(Vector((x,-1,max(z,1.215))),Vector((0,1,0)))
  y=(p.y-.014 if hit else -.13)-.037*math.sin(math.pi*row/4)*(1-u*.65)
  # A shallow, off-centre fold retains the broad, simple cloth shape.
  y-=.009*math.sin(i*math.pi/4+row*.5)*math.sin(math.pi*row/3)
  vs.append((x,y,z))
fs=[]
for r in range(3):
 for i in range(8):
  a=r*9+i;fs.extend([(a,a+1,a+10),(a,a+10,a+9)])
mask=mesh('FaceWrap',vs,fs,'mask','Head');sol=mask.modifiers.new('ClothThickness','SOLIDIFY');sol.thickness=.006
mesh('NeckKerchief',[(-.095,-.085,1.205),(.095,-.085,1.205),(.085,-.143,1.157),(0,-.155,1.073),(-.085,-.143,1.157),(0,-.159,1.167)],[(0,1,5),(1,2,5),(2,3,5),(3,4,5),(4,0,5)],'mask','Chest')
# Jacket has a broad exposed shirt, two lapels and simple low pockets.
for s in [-1,1]:
 mesh('Lapel_'+str(s),[(s*.071,-.145,.835),(s*.103,-.142,1.063),(s*.143,-.125,1.128),(s*.099,-.145,1.168),(s*.074,-.154,1.10),(s*.074,-.148,.835)],[(0,1,4,5),(1,2,3,4)],'jacket','Chest')
 box('JacketPocket_'+str(s),(s*.141,-.126,.889),(.068,.015,.082),'cuff','Spine',.004)
box('LootPouch',(-.203,-.10,.735),(.112,.085,.16),'leather','Hips',.013)
box('PouchFlap',(-.203,-.15,.786),(.108,.018,.065),'jacket','Hips',.008)
box('PouchTab',(-.203,-.163,.755),(.023,.011,.035),'metal','Hips',.003)
# Wrist bandage is wrapped around the forearm, not a replacement for its skin.
arm=bpy.data.objects['Bandit_Forearm_-1'];vs=[];fs=[]
start=Vector((-.293,-.055,.839));end=Vector((-.316,-.103,.757));axis=(end-start).normalized();u=axis.cross(Vector((0,1,0))).normalized();v=axis.cross(u)
for j in range(5):
 center=start.lerp(end,j/4);radius=.066-j*.0025
 for i in range(10):
  t=i*math.tau/10;p=center+radius*(u*math.cos(t)+v*math.sin(t));vs.append(tuple(p))
for j in range(4):
 for i in range(10):fs.append((j*10+i,j*10+(i+1)%10,(j+1)*10+(i+1)%10,(j+1)*10+i))
mesh('WristWrap',vs,fs,'wrap','Forearm.R')
# Restrained material variations emphasize planes, avoiding both plastic gloss and visual noise.
random.seed(19)
for o in [hood,mask]+[o for o in parts if any(k in o.name for k in ['JacketBody','Sleeve','TrouserLeg'])]:
 base=o.data.materials[0];p=base.node_tree.nodes['Principled BSDF'];c=list(p.inputs['Base Color'].default_value)
 for f in [.93,1.055]:
  m=base.copy();m.name=base.name+('_Shade' if f<1 else '_Light');m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*(k*f for k in c[:3]),1);o.data.materials.append(m)
 for poly in o.data.polygons:poly.use_smooth=False;poly.material_index=random.choices([0,1,2],[.70,.15,.15])[0]
for o in parts:o['module']=o.name.replace('Bandit_','');o['editable_separately']=True
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1);bpy.context.view_layer.update()
# Save clean body plus compatible existing clips; no weapon or gameplay enemy is added.
scene.render.resolution_x=720;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.cycles.samples=24
scene.camera.location=(2.7,-6,2.6);target=Vector((0,0,.88));scene.camera.rotation_euler=(target-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.type='ORTHO';scene.camera.data.ortho_scale=2.06
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(BS/'Bandit01.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'Bandit01.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
palette=[]
for m in set(m for o in parts for m in o.data.materials):
 p=m.node_tree.nodes.get('Principled BSDF');palette.append({'name':m.name,'color':list(p.inputs['Base Color'].default_value),'roughness':p.inputs['Roughness'].default_value})
(OUT/'Palette.json').write_text(json.dumps({'materials':palette},indent=2))
bad=[]
for o in parts:
 for v in o.data.vertices:
  if abs(sum(g.weight for g in v.groups)-1)>.001:bad.append((o.name,v.index))
assert not bad,bad[:10]
assert hashlib.sha256(SRC.read_bytes()).hexdigest()==original_hash
report={'mesh_modules':len(parts),'triangles':sum(len(p.vertices)-2 for o in parts for p in o.data.polygons),'bones':len(rig.data.bones),'source_hero_unchanged':True,'unweighted_vertices':len(bad),'clips':[a.name for a in bpy.data.actions]}
(OUT/'BuildCheck.json').write_text(json.dumps(report,indent=2));print('BANDIT_BUILD',json.dumps(report),flush=True)
for name,pos in [('Preview',(2.7,-6,2.6)),('Front',(0,-6,1.65)),('Back',(2.4,6,2.5))]:
 scene.camera.location=pos;scene.camera.rotation_euler=(target-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
