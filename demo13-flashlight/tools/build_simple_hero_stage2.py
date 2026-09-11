"""Add replaceable silhouette accessories, with all flat detail in UV textures."""
import bpy,math,json,hashlib,sys
import numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
sys.path.insert(0,str(ROOT/'tools'));from character_uv_atlas import author_atlas
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/SimpleHero_Stage1.blend'));bpy.context.preferences.filepaths.save_version=0
body=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('Study_')];root=bpy.data.objects['SimpleHero_Stage1'];root.name='SimpleHero_Stage2'
def body_hash():return hashlib.sha256(repr([(o.name,[tuple(v.co) for v in o.data.vertices],[tuple(u.uv) for u in o.data.uv_layers.active.data]) for o in body]).encode()).hexdigest()
before=body_hash();gear=[]
colors={'Scarf_Cloth':(.285,.078,.044),'Pack_Canvas':(.12,.088,.06),'Pack_Straps':(.064,.05,.035),'Charm_Purple':(.22,.105,.335)}
def material(name):
 m=bpy.data.materials.new(name);m.diffuse_color=(*colors[name],1);return m
scarfmat=material('Scarf_Cloth');packmat=material('Pack_Canvas');strapmat=material('Pack_Straps');charmmat=material('Charm_Purple')
def mesh(name,vs,fs,mat):
 m=bpy.data.meshes.new(name);m.from_pydata(vs,[],fs);m.materials.append(mat);m.update();o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);o.parent=root;gear.append(o)
 import bmesh
 bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free();return o
outline=[(-1,-.62),(-.68,-1),(.68,-1),(1,-.62),(1,.62),(.68,1),(-.68,1),(-1,.62)]
def rings(name,rows,mat):
 vs=[(cx+x*w/2,cy+y*d/2,z) for z,w,d,cx,cy in rows for x,y in outline];fs=[tuple(reversed(range(8)))]
 for j in range(len(rows)-1):
  for i in range(8):fs.append((j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i))
 fs.append(tuple(range((len(rows)-1)*8,len(rows)*8)));return mesh(name,vs,fs,mat)
wrap=rings('Gear_ScarfWrap',[(1.082,.224,.205,0,0),(1.125,.254,.235,0,-.002),(1.143,.225,.209,0,0)],scarfmat)
front=[(-.134,-.079,1.129),(-.154,-.141,1.094),(.003,-.178,.956),(.152,-.143,1.096),(.128,-.077,1.13)]
back=[(x,y+.012,z) for x,y,z in front];vs=front+back
fs=[(0,1,2,3,4),(9,8,7,6,5)]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)]
bib=mesh('Gear_ScarfBib',vs,fs,scarfmat)
pack=rings('Gear_BackpackBody',[(.723,.302,.156,0,.242),(.75,.382,.209,0,.242),(1.075,.382,.209,0,.242),(1.123,.316,.167,0,.242)],packmat)
straps=[]
for s in [-1,1]:
 path=[(s*.198,-.153,.69),(s*.177,-.151,.91),(s*.163,-.132,1.047),(s*.15,-.071,1.094),(s*.15,.015,1.112),(s*.156,.094,1.101),(s*.17,.158,1.063),(s*.181,.15,.77)]
 dense=[]
 for a,b in zip(path[:-1],path[1:]):
  a=Vector(a);b=Vector(b);steps=max(1,math.ceil((b-a).length/.025))
  dense.extend([a.lerp(b,i/steps) for i in range(steps)])
 dense.append(Vector(path[-1]));vs=[];torso=bpy.data.objects['Study_Torso']
 # Five samples across the strap follow the jacket's rounded corner as well as its length.
 for x,y,z in dense:
  for outer in [True,False]:
   for dx in ([-.025,-.0125,0,.0125,.025] if outer else [.025,.0125,0,-.0125,-.025]):
    yy=y+(-.006 if outer else .006)
    if y<-.10:
     surfaces=[]
     for surface in [torso,bpy.data.objects['Study_Sleeve_-1'],bpy.data.objects['Study_Sleeve_1']]:
      hit,point,normal,index=surface.ray_cast(Vector((x+dx,-2,z)),Vector((0,1,0)))
      if hit:surfaces.append(point.y)
     if surfaces:yy=min(surfaces)-(.009 if outer else .005)
    vs.append((x+dx,yy,z))
 fs=[tuple(reversed(range(10)))]
 for i in range(len(dense)-1):
  for j in range(10):fs.append((i*10+j,i*10+(j+1)%10,(i+1)*10+(j+1)%10,(i+1)*10+j))
 fs.append(tuple(range((len(dense)-1)*10,len(dense)*10)))
 straps.append(mesh('Gear_PackStrap_'+str(s),vs,fs,strapmat))
tag=rings('Gear_AnomalyCharm',[(.52,.046,.021,-.231,-.142),(.529,.055,.023,-.231,-.142),(.604,.055,.023,-.231,-.142),(.615,.043,.021,-.231,-.142)],charmmat)
loop=mesh('Gear_CharmLoop',[(-.241,-.147,.605),(-.224,-.147,.605),(-.224,-.147,.65),(-.241,-.147,.65)],[(0,1,2,3)],strapmat)
def join(objects,name):
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=objects[0];o.name=name;return o
scarf=join([wrap,bib],'Gear_Scarf');backpack=join([pack]+straps,'Gear_Backpack');charm=join([tag,loop],'Gear_Charm');gear=[scarf,backpack,charm]
for v in charm.data.vertices:v.co.x+=.025;v.co.y-=.012
regions={'SCARF':(.02,.57,.48,.41),'BACKPACK':(.52,.02,.46,.96),'CHARM':(.02,.02,.48,.52)}
def bucket(n):return {'Gear_Scarf':'SCARF','Gear_Backpack':'BACKPACK','Gear_Charm':'CHARM'}[n]
def paint(name,mat,p):
 x,y,z=p.T;n=len(p);c=np.tile(colors[mat.name],(n,1));rough=np.full(n,.93)
 if mat.name=='Pack_Canvas':
  rear=y>.32;flap=rear&(z>1.004);c[flap]=(.145,.108,.073)
  c[rear&(z>1.001)&(z<1.008)]=(.062,.045,.031)
  center=rear&(abs(x)<.025)&(z>.848)&(z<1.025);c[center]=(.048,.037,.025)
  buckle=rear&(abs(x)<.033)&(z>.895)&(z<.928);c[buckle]=(.17,.133,.087)
  c[buckle&(abs(x)<.023)&(z>.903)&(z<.921)]=(.048,.037,.025)
 elif mat.name=='Pack_Straps':
  if name=='Gear_Backpack':
   adjuster=(y<-.10)&(z>.864)&(z<.89);c[adjuster]=(.135,.113,.08)
   c[adjuster&(z>.87)&(z<.884)]=(.064,.05,.035)
  rough[:]=.85
 elif mat.name=='Scarf_Cloth':
  # One broad fold tone, no mesh folds, stitches or cloth noise.
  c[(z>1.1)&(y<-.06)]=(.33,.096,.052)
 elif mat.name=='Charm_Purple':rough[:]=.82
 return c,np.zeros(n),0,rough
images,uvreport=author_atlas(gear,regions,bucket,paint,OUT/'Textures','SimpleHero_Gear',1024)
mat=bpy.data.materials.new('SimpleHero_Gear');mat.use_nodes=True;nodes=mat.node_tree.nodes;bs=nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.92;bs.inputs['Specular IOR Level'].default_value=0
tex=nodes.new('ShaderNodeTexImage');tex.image=images['BaseColor'];mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
for o in gear:
 o.data.materials.clear();o.data.materials.append(mat)
 for poly in o.data.polygons:poly.material_index=0
 o['replacement_slot']=o.name.removeprefix('Gear_');o['flat_details_in_uv']=True
assert body_hash()==before,'Approved body or UV changed'
root['stage']='02 simple accessories; approved stage-one body preserved';root['rigged']=False
parts=body+gear;bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=body[0]
bpy.ops.export_scene.fbx(filepath=str(OUT/'SimpleHero_Stage2.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='AUTO')
bpy.ops.export_scene.gltf(filepath=str(OUT/'SimpleHero_Stage2.glb'),use_selection=True,export_format='GLB')
sc=bpy.context.scene;cam=sc.camera
def render(name,loc,scale=2.25):
 cam.location=loc;cam.rotation_euler=(Vector((0,0,.92))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale;sc.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
render('Stage2_Quarter',(2.3,-6,3.1));render('Stage2_ReferenceAngle',(2.3,-6,2))
# Rotate the studio key for a readable rear inspection; restore it immediately.
lights=[o for o in sc.objects if o.type=='LIGHT'];saved=[(o,o.location.copy(),o.rotation_euler.copy()) for o in lights]
for o in lights:o.location.y*=-1;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
render('Stage2_Back',(2.3,6,2.7))
for o,loc,rot in saved:o.location=loc;o.rotation_euler=rot
render('Stage2_Side',(6,-.8,2));render('Stage2_GameScale',(4,-6,6.5),7)
cam.location=(2.3,-6,2);cam.rotation_euler=(Vector((0,0,.92))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.25
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/SimpleHero_Stage2.blend'))
report={'stage':2,'approved_body_uv_preserved':True,'parts':len(parts),'materials':2,'accessory_slots':[o.name for o in gear],'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in parts),'rigged':False,'gameplay_applied':False}
(OUT/'Stage2Check.json').write_text(json.dumps(report,indent=2));print('STAGE2_READY',report)
