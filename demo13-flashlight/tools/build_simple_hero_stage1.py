"""Separate, editable stage-one silhouette study. Never modifies production hero."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
OUT.mkdir(parents=True,exist_ok=True);(OUT/'BlenderSource~').mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials):bpy.data.materials.remove(m)
def mat(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=1;p.inputs['Specular IOR Level'].default_value=0
 return m
skin=mat('Study_Skin',(.55,.335,.18));coat=mat('Study_Cloth',(.19,.205,.105));pants=mat('Study_Trousers',(.065,.073,.073));boot=mat('Study_Boots',(.105,.073,.045));hair=mat('Study_Hair',(.055,.043,.032));cap=mat('Study_Cap',(.215,.225,.115));brim=mat('Study_Brim',(.068,.075,.055));patch=mat('Study_Patch',(.39,.31,.19))
parts=[]
def mesh(name,verts,faces,material):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.materials.append(material);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);parts.append(o);return o
# Chamfered rectangle, front is -Y. Broad flat face rather than a sphere.
outline=[(-1,-.62),(-.68,-1),(.68,-1),(1,-.62),(1,.62),(.68,1),(-.68,1),(-1,.62)]
def rounded_outline(radius=.25,steps=4):
 return [(cx+radius*math.cos(math.radians(a+90*i/steps)),cy+radius*math.sin(math.radians(a+90*i/steps))) for cx,cy,a in [(1-radius,-1+radius,-90),(1-radius,1-radius,0),(-1+radius,1-radius,90),(-1+radius,-1+radius,180)] for i in range(steps+1)]
def rings(name,rows,material,shape=None,smooth=False):
 shape=shape or outline;count=len(shape)
 vs=[(cx+x*w/2,cy+y*d/2,z) for z,w,d,cx,cy in rows for x,y in shape]
 fs=[tuple(reversed(range(count)))]
 for j in range(len(rows)-1):
  for i in range(count):fs.append((j*count+i,j*count+(i+1)%count,(j+1)*count+(i+1)%count,(j+1)*count+i))
 fs.append(tuple(range((len(rows)-1)*count,len(rows)*count)))
 o=mesh(name,vs,fs,material)
 if smooth:
  for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
 return o
head=rings('Study_Head',[(1.145,.39,.27,0,-.01),(1.153,.465,.32,0,-.01),(1.175,.55,.365,0,-.01),(1.205,.604,.393,0,-.01),(1.24,.625,.409,0,-.01),(1.29,.632,.415,0,-.01),(1.49,.625,.405,0,-.01),(1.56,.54,.35,0,-.01)],skin,rounded_outline(.38,5),True)
# Face is a real editable UV texture, not floating eye geometry.
import numpy as np
n=512;pixels=np.zeros((n,n,4),np.float32);pixels[:,:,:3]=np.array((.55,.335,.18));pixels[:,:,3]=1
yy,xx=np.mgrid[0:n,0:n];x=xx/(n-1)*.60-.30;z=yy/(n-1)*.48+1.115
ink=np.array((.023,.019,.015))
for cx in [-.103,.103]:
 eye=((x-cx)/.016)**2+(np.maximum(abs(z-1.337)-.024,0)/.016)**2<=1
 brow=(abs(x-cx)<.046)&(abs(z-1.425)<.011)
 pixels[eye|brow,:3]=ink
pixels[:,:,:3]=np.where(pixels[:,:,:3]<=.0031308,pixels[:,:,:3]*12.92,1.055*pixels[:,:,:3]**(1/2.4)-.055)
im=bpy.data.images.new('Study_Face',width=n,height=n,alpha=True);im.pixels.foreach_set(pixels.reshape(-1));im.filepath_raw=str(OUT/'Study_Face.png');im.file_format='PNG';im.save();im=bpy.data.images.load(str(OUT/'Study_Face.png'))
fm=skin.copy();fm.name='Study_Face';node=fm.node_tree.nodes.new('ShaderNodeTexImage');node.image=im;fm.node_tree.links.new(node.outputs['Color'],fm.node_tree.nodes.get('Principled BSDF').inputs['Base Color']);head.data.materials.append(fm)
uv=head.data.uv_layers.new(name='FaceUV')
for poly in head.data.polygons:
 poly.material_index=1 if poly.normal.y<-.8 else 0
 for li in poly.loop_indices:
  p=head.data.vertices[head.data.loops[li].vertex_index].co;uv.data[li].uv=((p.x+.30)/.60,(p.z-1.115)/.48)
rings('Study_Hair',[(1.285,.55,.365,0,.032),(1.32,.63,.43,0,.026),(1.56,.647,.445,0,.015)],hair,rounded_outline(.38,3))
# Hair shell only behind face: omit front-facing quads and end cap; hidden top under hat.
import bmesh
bm=bmesh.new();bm.from_mesh(bpy.data.objects['Study_Hair'].data);bmesh.ops.delete(bm,geom=[f for f in bm.faces if f.normal.y<-.3 or f.normal.z<-.5],context='FACES');bm.to_mesh(bpy.data.objects['Study_Hair'].data);bm.free()
# Small hair silhouettes belong to the hair module and stay under the crown.
for s in [-1,1]:
 mesh('Study_SideHair_'+str(s),[(s*.24,-.178,1.535),(s*.294,-.15,1.51),(s*.296,-.16,1.335),(s*.258,-.203,1.365)],[(0,1,2,3)] if s==1 else [(3,2,1,0)],hair)
crown=rings('Study_Cap',[(1.495,.677,.476,0,0),(1.615,.665,.475,0,.005),(1.70,.60,.44,0,.015),(1.756,.435,.335,0,.023),(1.77,.20,.17,0,.027)],cap,rounded_outline(.45,2))
# Modest cap tilt; retain broad flat panels instead of global subdivision.
for vert in crown.data.vertices:vert.co.z+=.028*vert.co.x
# Flat broad visor with a shallow curve, attached with overlap into crown.
v=[]
for zoff in [0,-.018]:
 for depth in [0,.5,1]:
  for i in range(13):
   t=(i-6)/6
   x=t*.333;y=-.191+depth*(-.20+.052*t*t)
   z=1.537-.038*t*t-.024*depth+.028*x+zoff
   v.append((x,y,z))
f=[]
for d in range(2):
 for i in range(12):
  a=d*13+i;b=a+13
  f.extend([(a,b,b+1,a+1),(39+a,39+a+1,39+b+1,39+b)])
for i in range(12):f.extend([(26+i,65+i,66+i,27+i),(i,i+1,40+i,39+i)])
for d in range(2):
 a=d*13;b=a+13;f.extend([(a,39+a,39+b,b),(a+12,b+12,b+51,a+51)])
mesh('Study_CapBrim',v,f,brim)
mesh('Study_CapPatch',[(-.085,-.239,1.603),(.085,-.239,1.608),(.078,-.220,1.697),(-.078,-.220,1.692)],[(0,1,2,3)],patch)
rings('Study_Neck',[(1.055,.15,.15,0,0),(1.15,.15,.15,0,0)],skin)
rings('Study_Torso',[(.615,.46,.275,0,0),(.65,.475,.285,0,0),(.90,.435,.28,0,0),(1.035,.40,.255,0,0),(1.105,.265,.20,0,0)],coat,rounded_outline(.30,2))
rings('Study_Hips',[(.53,.365,.25,0,.015),(.65,.43,.265,0,.01)],pants)
for s in [-1,1]:
 rings('Study_Sleeve_'+str(s),[(.636,.158,.185,s*.338,-.012),(.69,.17,.205,s*.322,-.006),(.84,.18,.22,s*.29,0),(1.015,.185,.235,s*.24,0),(1.075,.145,.19,s*.208,0),(1.104,.072,.12,s*.178,0)],coat,rounded_outline(.62,3),True)
 bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=(s*.354,-.025,.563));o=bpy.context.object;o.name='Study_Mitten_'+str(s);o.scale=(.083,.074,.093);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(skin);parts.append(o)
 for p in o.data.polygons:p.use_smooth=True
 rings('Study_Leg_'+str(s),[(.165,.17,.19,s*.125,.01),(.24,.19,.215,s*.121,.01),(.59,.20,.235,s*.111,.01)],pants)
 rings('Study_Boot_'+str(s),[(.025,.205,.30,s*.126,-.044),(.085,.21,.305,s*.126,-.044),(.15,.19,.265,s*.126,-.033),(.235,.16,.185,s*.126,.005)],boot)
# Stable source grouping supports later replacement of headwear/clothes and rigging.
root=bpy.data.objects.new('SimpleHero_Stage1',None);bpy.context.collection.objects.link(root)
for o in parts:o.parent=root
root['stage']='01 silhouette revision 2; no accessories or animation';root['height_m']=1.773
for o in parts:
 if not o.data.uv_layers:o.data.uv_layers.new(name='UVMap')
exec(Path(__file__).with_name('author_simple_hero_uv.py').read_text())
parts=author_simple_hero_uv(ROOT,OUT,parts)
# Reference proportion correction; UVs follow the face rather than repainting features.
for o in parts:
 if o.name=='Study_Head':
  for v in o.data.vertices:v.co.x*=.90;v.co.z=1.145+(v.co.z-1.145)*1.10
 elif o.name.startswith('Study_Cap'):
  for v in o.data.vertices:v.co.x*=.92;v.co.z+=.035
 o.data.update()
root['stage']='01 silhouette revision 4; UV atlas and reference face proportions'
root['height_m']=1.808
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=head
bpy.ops.export_scene.fbx(filepath=str(OUT/'SimpleHero_Stage1.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='AUTO')
bpy.ops.export_scene.gltf(filepath=str(OUT/'SimpleHero_Stage1.glb'),use_selection=True,export_format='GLB')
sc=bpy.context.scene;sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=128;sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100
sc.world.color=(.25,.25,.25);sc.view_settings.view_transform='Standard';sc.view_settings.look='Medium High Contrast';sc.view_settings.exposure=0;sc.view_settings.gamma=1
floor=mat('Preview_Ground',(.045,.052,.055));bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.name='Preview_Ground';bpy.context.object.data.materials.append(floor)
def area(name,loc,power,size):
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
area('Preview_Key',(-3,-4,6),450,4);area('Preview_Fill',(4,-1,3),100,5)
bpy.ops.object.camera_add();cam=bpy.context.object;cam.name='Preview_Camera';cam.data.type='ORTHO';cam.data.ortho_scale=2.25;sc.camera=cam
def render(name,loc,scale=2.25):
 cam.location=loc;cam.rotation_euler=(Vector((0,0,.92))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale;sc.render.filepath=str(OUT/(name+'.png'));sc.render.image_settings.file_format='PNG';bpy.ops.render.render(write_still=True)
render('Stage1_Quarter',(2.3,-6,3.1));render('Stage1_Front',(0,-6,1.45));render('Stage1_Back',(2.3,6,3.1));render('Stage1_GameScale',(4,-6,6.5),7.0)
render('Stage1_ReferenceAngle',(2.3,-6,2.0))
cam.location=(2.3,-6,3.1);cam.rotation_euler=(Vector((0,0,.92))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.25
bpy.ops.object.select_all(action='DESELECT');head.select_set(True);bpy.context.view_layer.objects.active=head
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/SimpleHero_Stage1.blend'))
report={'stage':1,'revision':4,'height':max((o.matrix_world@v.co).z for o in parts for v in o.data.vertices),'parts':len(parts),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in parts),'production_hero_untouched':True,'rigged':False,'atlas_size':2048,'materials':1,'head_width_scale':.90,'head_height_scale':1.10,'cap_width_scale':.92}
(OUT/'Stage1Check.json').write_text(json.dumps(report,indent=2));print('STAGE1_READY',report)
