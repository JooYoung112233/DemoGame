"""Offline story NPC kit. Reads the approved C rig; never writes Unity Assets.
Run with Blender 4.5 background. Four NPCs, one authored idle each.
"""
import bpy, bmesh, math, json, hashlib, sys
import numpy as np
from pathlib import Path
from mathutils import Vector, Matrix, Euler

OUT=Path(__file__).resolve().parents[1]
ROOT=OUT.parents[1]/'demo13-flashlight'
SOURCE=OUT.parent/'CharacterProportionC/BlenderSource~/Player_C.blend'
sys.path.insert(0,str(ROOT/'tools'))
from character_uv_atlas import author_atlas
for d in ['BlenderSource~','Models','Textures','Review']: (OUT/d).mkdir(parents=True,exist_ok=True)
source_hash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
SPECS=[
 dict(id='pawnshop',name='Pawnshop',label='전당포 주인',cloth=(.185,.070,.050),pants=(.066,.053,.042),hair=(.090,.077,.060),skin=(.49,.30,.175),width=1.10,motion='Idle_Observe'),
 dict(id='veteran_scavenger',name='VeteranScavenger',label='베테랑 회수꾼',cloth=(.128,.145,.067),pants=(.060,.064,.059),hair=(.060,.050,.040),skin=(.45,.285,.165),width=1.0,motion='Idle_Crouch'),
 dict(id='district_warden',name='DistrictWarden',label='구역 관리인',cloth=(.124,.092,.060),pants=(.065,.073,.074),hair=(.055,.046,.037),skin=(.51,.315,.19),width=.97,motion='Idle_Ledger'),
 dict(id='wandering_merchant',name='WanderingMerchant',label='떠돌이 상인',cloth=(.205,.145,.072),pants=(.094,.080,.053),hair=(.052,.039,.027),skin=(.50,.31,.18),width=1.05,motion='Idle_Pack'),
]

def smooth(a,b,v):
 t=max(0,min(1,(v-a)/(b-a)));return t*t*(3-2*t)
def skel_sig(r):
 return [(b.name,b.parent.name if b.parent else None,tuple(tuple(row) for row in b.matrix_local),b.use_deform) for b in r.data.bones]

reports=[]
for spec in SPECS:
 bpy.ops.wm.open_mainfile(filepath=str(SOURCE));bpy.context.preferences.filepaths.save_version=0
 sc=bpy.context.scene;rig=bpy.data.objects['SimpleHero_Rig'];signature=skel_sig(rig)
 rig.animation_data_clear()
 for a in list(bpy.data.actions):bpy.data.actions.remove(a)
 rig.data.pose_position='REST';bpy.context.view_layer.update()
 keep=['Study_Mitten_-1','Study_Mitten_1','Study_Boot_-1','Study_Boot_1']
 for o in list(sc.objects):
  if o!=rig and o.name not in keep:bpy.data.objects.remove(o,do_unlink=True)
 parts=[];colors={};materials={}
 def mat(key,color,rough=.9,metal=0):
  if key in materials:return materials[key]
  m=bpy.data.materials.new('NPC_'+key);m.diffuse_color=(*color,1)
  m['roughness']=rough;m['metallic']=metal;materials[key]=m;colors[key]=color;return m
 skin=mat('skin',spec['skin'],.88);cloth=mat('cloth',spec['cloth']);pants=mat('pants',spec['pants']);hair=mat('hair',spec['hair'],.94)
 leather=mat('leather',(.105,.068,.037),.84);dark=mat('dark',(.030,.033,.030),.91)
 canvas=mat('canvas',(.275,.225,.138),.96);hem=mat('hem',tuple(v*.79 for v in spec['cloth']));metal=mat('metal',(.23,.215,.16),.69,.24)
 lining=mat('lining',(.25,.221,.16));paper=mat('paper',(.48,.423,.30))
 def weights(o,mode):
  o.vertex_groups.clear()
  for v in o.data.vertices:
   x,y,z=v.co;s='L' if x>=0 else 'R'
   if mode=='torso':
    if z<.86:t=smooth(.65,.82,z);w={'Hips':1-t,'Spine':t}
    else:t=smooth(.86,1.015,z);w={'Spine':1-t,'Chest':t}
   elif mode=='arm':
    u=smooth(.79,.885,z);w={'Forearm.'+s:1-u,'UpperArm.'+s:u}
   elif mode=='leg':
    h=smooth(.54,.625,z);t=smooth(.315,.405,z)*(1-h);w={'Hips':h,'Thigh.'+s:t,'Shin.'+s:1-h-t}
   else:w={mode:1}
   for n,value in w.items():
    if value>1e-7:(o.vertex_groups.get(n) or o.vertex_groups.new(name=n)).add([v.index],value,'REPLACE')
 def mesh(name,vs,fs,material,bone='Chest',smooth_faces=False):
  me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
  bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
  o=bpy.data.objects.new(name,me);sc.collection.objects.link(o);o.parent=rig;me.materials.append(material)
  weights(o,bone);o.modifiers.new('Skin','ARMATURE').object=rig
  if smooth_faces:
   for f in me.polygons:f.use_smooth=True
  parts.append(o);return o
 def box(name,position,size,material,bone='Chest',bevel=.008,rotation=None):
  bpy.ops.mesh.primitive_cube_add(size=1,location=position);tmp=bpy.context.object;tmp.dimensions=size
  if rotation:tmp.rotation_euler=rotation
  bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  if bevel:
   mod=tmp.modifiers.new('Soft edges','BEVEL');mod.width=min(bevel,min(size)*.25);mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
  vs=[tuple(v.co) for v in tmp.data.vertices];fs=[tuple(f.vertices) for f in tmp.data.polygons];bpy.data.objects.remove(tmp,do_unlink=True)
  return mesh(name,vs,fs,material,bone)
 def ellipsoid(name,p,size,material,bone='Head',segments=16,rings=8):
  bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=p);tmp=bpy.context.object;tmp.scale=size
  bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
  vs=[tuple(v.co) for v in tmp.data.vertices];fs=[tuple(f.vertices) for f in tmp.data.polygons];bpy.data.objects.remove(tmp,do_unlink=True)
  return mesh(name,vs,fs,material,bone,True)
 def rings(name,rows,material,bone='torso',shape='rounded',cap=True):
  if shape=='round':outline=[(math.sin(i*math.tau/24),-math.cos(i*math.tau/24)) for i in range(24)]
  else:
   r=.43;outline=[(cx+r*math.cos(math.radians(a+90*i/3)),cy+r*math.sin(math.radians(a+90*i/3))) for cx,cy,a in [(1-r,-1+r,-90),(1-r,1-r,0),(-1+r,1-r,90),(-1+r,-1+r,180)] for i in range(4)]
  n=len(outline);vs=[(cx+x*w/2,cy+y*d/2,z) for z,w,d,cx,cy in rows for x,y in outline]
  fs=[]
  if cap:fs.append(tuple(reversed(range(n))))
  for k in range(len(rows)-1):
   for i in range(n):fs.append((k*n+i,k*n+(i+1)%n,(k+1)*n+(i+1)%n,(k+1)*n+i))
  if cap:fs.append(tuple(range((len(rows)-1)*n,len(rows)*n)))
  return mesh(name,vs,fs,material,bone,shape=='round')
 # Reuse rounded hands and boots, with exactly the approved rest skeleton/scale.
 for name in keep:
  o=bpy.data.objects[name];transform=rig.matrix_world.inverted()@o.matrix_world;o.data.transform(transform);o.matrix_basis=Matrix.Identity(4)
  o.data.materials.clear();o.data.materials.append(skin if 'Mitten' in name else leather)
  for f in o.data.polygons:f.material_index=0
  parts.append(o)
 # A complete rounded skull replaces the cap-hidden source top; face retains C width.
 head_rows=[(1.183,.33,.242,0,-.010),(1.20,.414,.290,0,-.010),(1.24,.487,.348,0,-.008),(1.31,.512,.374,0,-.010),(1.49,.508,.370,0,-.008),(1.565,.455,.334,0,.006),(1.622,.335,.265,0,.015),(1.654,.17,.145,0,.020)]
 head=rings('Head',head_rows,skin,'Head',cap=True)
 for f in head.data.polygons:f.use_smooth=True
 rings('Neck',[(1.055,.155,.15,0,0),(1.235,.16,.16,0,0)],skin,'Neck')
 width=spec['width']
 rings('Torso',[(.655,.462*width,.288,0,.003),(.70,.473*width,.310,0,0),(.84,.449*width,.305,0,0),(.98,.418*width,.278,0,0),(1.065,.36,.239,0,0),(1.105,.254,.190,0,0)],cloth)
 rings('Hips',[(.55,.315,.210,0,.009),(.60,.392,.248,0,.007),(.715,.431*width,.267,0,0)],pants,'Hips')
 for s in [-1,1]:
  rings('Sleeve_'+str(s),[(.634,.15,.169,s*.341,-.013),(.715,.155,.178,s*.325,-.007),(.805,.16,.19,s*.30,0),(.86,.179,.209,s*.288,0),(.98,.176,.209,s*.245,0),(1.05,.14,.184,s*.208,0),(1.087,.087,.13,s*.184,0)],cloth,'arm','round')
  rings('Leg_'+str(s),[(.19,.18,.20,s*.126,.01),(.24,.192,.218,s*.125,.01),(.35,.195,.225,s*.123,.008),(.48,.208,.235,s*.119,.008),(.625,.21,.25,s*.114,.01)],pants,'leg')
  box('Boot cuff '+str(s),(s*.126,.008,.214),(.169,.19,.055),leather,'Foot.'+('L' if s>0 else 'R'))
 # Hair shell follows the real skull contour. Top-down visibility is not forced.
 r=.43;profile=[(cx+r*math.cos(math.radians(a+90*i/3)),cy+r*math.sin(math.radians(a+90*i/3))) for cx,cy,a in [(1-r,-1+r,-90),(1-r,1-r,0),(-1+r,1-r,90),(-1+r,-1+r,180)] for i in range(4)]
 def scalp_point(x,y,z,offset=.014):
  if z>head_rows[-1][0]:w,d,cy=.07,.064,.020
  else:
   for a,b in zip(head_rows,head_rows[1:]):
    if a[0]<=z<=b[0]:
     t=(z-a[0])/(b[0]-a[0]);w=a[1]+t*(b[1]-a[1]);d=a[2]+t*(b[2]-a[2]);cy=a[4]+t*(b[4]-a[4]);break
  return (x*(w/2+offset),cy+y*(d/2+offset),z)
 if spec['id']=='pawnshop':
  outline=profile[2:14];n=len(outline);vs=[]
  for z in [1.31,1.40,1.49,1.565]:
   vs.extend(scalp_point(x,y,z) for x,y in outline)
  fs=[(k*n+i,k*n+i+1,(k+1)*n+i+1,(k+1)*n+i) for k in range(3) for i in range(n-1)]
  h=mesh('Receding side hair',vs,fs,hair,'Head',True);mod=h.modifiers.new('Hair thickness','SOLIDIFY');mod.thickness=.008
 else:
  n=len(profile);vs=[]
  for k in range(5):
   for x,y in profile:
    bottom=1.31+.23*max(0,-y)**3
    z=[bottom,max(bottom+.018,1.49),1.565,1.622,1.671][k]
    vs.append(scalp_point(x,y,z))
  fs=[(k*n+i,k*n+(i+1)%n,(k+1)*n+(i+1)%n,(k+1)*n+i) for k in range(4) for i in range(n)];fs.append(tuple(range(4*n,5*n)))
  mesh('Hair cap',vs,fs,hair,'Head',True)
 if spec['id']=='district_warden':
  ellipsoid('Tied hair bun',(0,.11,1.691),(.115,.112,.105),hair)
  rings('Hair tie',[(1.665,.17,.13,0,.11),(1.691,.155,.13,0,.11)],dark,'Head','round')
 # Beards are painted onto the head; no floating chin wedges.
 # Belt and tailored collar, shared occupational clothing language.
 rings('Belt',[(.682,.477*width,.319,0,0),(.724,.478*width,.319,0,0)],leather,'Hips')
 box('Belt buckle',(0,-.17,.703),(.061,.018,.047),metal,'Hips')
 for s in [-1,1]:
  box('Collar '+str(s),(s*.078,-.112,1.055),(.082,.04,.113),lining,'Chest',.008,(0,s*math.radians(27),s*math.radians(-8)))
 if spec['id']=='pawnshop':
  apron=rings('Apron',[(.535,.40,.043,0,-.165),(.565,.47,.043,0,-.181),(.75,.481,.044,0,-.181),(.92,.334,.040,0,-.171),(1.03,.258,.035,0,-.139)],canvas)
  box('Apron pocket',(0,-.209,.715),(.255,.016,.118),canvas,'Spine')
  for s in [-1,1]:
   box('Apron strap '+str(s),(s*.124,-.116,1.038),(.03,.035,.15),leather,'Chest',rotation=(math.radians(-25),0,0))
   box('Apron rivet '+str(s),(s*.116,-.169,1.015),(.019,.013,.017),metal,'Chest')
 elif spec['id']=='veteran_scavenger':
  rings('Headband',[(1.482,.555,.421,0,-.006),(1.548,.528,.401,0,.004)],dark,'Head',cap=False)
  box('Headband knot',(.235,.079,1.493),(.07,.07,.07),dark,'Head')
  box('Headband tail',(.255,.117,1.402),(.06,.021,.17),dark,'Head',rotation=(.15,-.2,.1))
  for s in [-1,1]:
   box('Jacket chest pocket '+str(s),(s*.128,-.158,.927),(.108,.027,.107),hem,'Chest')
   box('Jacket pocket flap '+str(s),(s*.128,-.176,.966),(.112,.015,.033),cloth,'Chest')
  box('Hip utility pouch',(-.252,.026,.671),(.10,.135,.145),leather,'Hips')
 elif spec['id']=='district_warden':
  box('Ledger pages',(.30,-.185,.91),(.19,.065,.285),paper,'Chest')
  for y in [-.224,-.145]:box('Ledger cover',(.30,y,.91),(.21,.014,.30),leather,'Chest')
  box('Ledger spine',(.399,-.185,.91),(.014,.087,.30),hem,'Chest')
  box('Ledger label',(.30,-.235,.976),(.09,.008,.05),canvas,'Chest')
  box('Key ring',(-.214,-.15,.67),(.047,.02,.055),metal,'Hips')
  for x,z in [(-.23,.604),(-.20,.594),(-.18,.62)]:
   box('Key stem',(x,-.162,z),(.009,.012,.066),metal,'Hips',.002)
   box('Key tooth',(x+.007,-.162,z-.022),(.018,.014,.012),metal,'Hips',.002)
 else:
  hat=mat('hat',(.185,.12,.055),.93)
  rings('Hat brim',[(1.546,.72,.60,0,.012),(1.564,.725,.60,0,.012)],hat,'Head','round')
  rings('Hat crown',[(1.555,.59,.49,0,.012),(1.63,.55,.45,0,.025),(1.731,.407,.33,0,.032),(1.76,.26,.24,0,.025)],hat,'Head','round')
  rings('Hat band',[(1.57,.59,.49,0,.015),(1.607,.571,.471,0,.020)],leather,'Head','round',False)
  # Short poncho with a front point and a broad back, weighted to upper torso.
  poncho_mat=mat('poncho',(.222,.171,.096),.96)
  poncho=rings('Poncho',[(.815,.55,.405,0,0),(.98,.61,.36,0,0),(1.085,.43,.30,0,0),(1.14,.21,.20,0,0)],poncho_mat,'Chest','round',cap=False)
  for v in poncho.data.vertices:
   if v.co.z<.83:v.co.z-=.15*max(0,1-abs(v.co.x)/.275)*max(0,-v.co.y/.2025)
  mod=poncho.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.014
  box('Packed rucksack',(0,.30,.93),(.47,.29,.49),leather,'Backpack',.04)
  box('Pack canvas lid',(0,.35,1.18),(.48,.28,.09),canvas,'Backpack',.025)
  roll=ellipsoid('Bedroll',(0,.32,1.25),(.31,.103,.10),hem,'Backpack')
  for s in [-1,1]:
   box('Roll tie '+str(s),(s*.20,.32,1.25),(.035,.209,.204),leather,'Backpack',.008)
   box('Shoulder strap '+str(s),(s*.14,-.14,1.02),(.038,.037,.25),leather,'Chest',rotation=(.15,s*.15,0))
  box('Side satchel',(-.27,.018,.66),(.13,.155,.20),leather,'Hips',.02)
  for x,col in [(.23,(.095,.18,.19)),(.31,(.23,.10,.06))]:
   bottle=mat('bottle'+str(x),col,.72)
   box('Trade bottle',(x,-.095,.69),(.052,.052,.112),bottle,'Hips',.01)
   box('Bottle cap',(x,-.095,.755),(.032,.032,.025),metal,'Hips')

 # Apply authored thickness before charting; triangulation is identical in FBX.
 for o in parts:
  bpy.context.view_layer.objects.active=o
  for mod in list(o.modifiers):
   if mod.type!='ARMATURE':bpy.ops.object.modifier_apply(modifier=mod.name)
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
 # Atlas paints the actual complete surface and quietly varies cloth roughness.
 # Coordinates supplied by the atlas author are world coordinates, including C scale.
 def paint(name,material,p):
  p=p.copy();p[:,2]/=1.08;x,y,z=p.T;n=len(p)
  c=np.tile(tuple(material.diffuse_color)[:3],(n,1));rough=np.full(n,float(material.get('roughness',.9)));height=np.zeros(n)
  def fill(mask,col):c[mask]=col
  if name=='Head':
   front=y<-.145
   if spec['id']!='district_warden':
    beard=(y<-.07)&(z<1.282+.073*np.clip((abs(x)-.08)/.12,0,1))
    fill(beard,tuple(v*1.28 for v in spec['hair']))
   for center in [-.086,.086]:
    eye=((x-center)/.012)**2+(np.maximum(abs(z-1.387)-.016,0)/.012)**2<=1
    brow=(abs(x-center)<.037)&(abs(z-(1.45+.08*(abs(x)-.086)))<.008)
    fill(front&(eye|brow),(.024,.020,.016))
   fill(front&(abs(x)<.029)&(abs(z-1.30)<.005),(.16,.091,.047))
  if name=='Torso':
   front=y<-.09
   if spec['id'] in ['veteran_scavenger','district_warden']:
    fill(front&(abs(x)<.061),(.066,.067,.054) if spec['id']=='veteran_scavenger' else (.23,.211,.167))
    fill(front&(abs(abs(x)-.066)<.005),tuple(v*.68 for v in spec['cloth']))
   else:fill(front&(abs(x)<.004),tuple(v*.64 for v in spec['cloth']))
  if name.startswith('Sleeve'):
   if spec['id']=='pawnshop':fill(z<.827,spec['skin']);fill((z>.827)&(z<.878),(.27,.213,.142))
   elif spec['id']=='veteran_scavenger':fill(z<.73,spec['skin']);fill((z>.73)&(z<.78),(.205,.203,.128))
   else:fill(z<.685,tuple(v*.74 for v in spec['cloth']))
  if name.startswith('Study_Mitten') and spec['id']=='veteran_scavenger':fill(z>.535,(.05,.047,.04))
  if name.startswith('Study_Boot'):fill(z<.060,(.025,.024,.02))
  if name=='Apron':
   stain=((x+.14)/.054)**2+((z-.78)/.036)**2<1
   fill((y<-.17)&stain,(.223,.18,.111))
   fill((z<.562),(.232,.185,.113))
  if name.startswith('Leg') and spec['id']=='veteran_scavenger':
   fill((y<-.08)&(abs(z-.365)<.035)&(abs(abs(x)-.126)<.05),(.15,.121,.075))
  # Fine restrained fabric relief, never a baked light/shadow gradient.
  if material.get('roughness',.9)>=.9 and name!='Head':
   weave=np.sin(x*780+y*510)*np.sin(z*680+y*430)
   c*=1+.014*weave[:,None];height=.028*weave;rough=np.clip(rough+.018*weave,0,1)
  return np.clip(c,0,1),height,float(material.get('metallic',0)),rough
 regions={'SURFACE':(.015,.015,.97,.97)}
 images,uvreport=author_atlas(parts,regions,lambda n:'SURFACE',paint,OUT/'Textures'/spec['name'],spec['name'],2048)
 material=bpy.data.materials.new(spec['name']+'_Surface');material.use_nodes=True;nodes=material.node_tree.nodes;links=material.node_tree.links;bs=nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.9;bs.inputs['Specular IOR Level'].default_value=.18
 tex=nodes.new('ShaderNodeTexImage');tex.image=images['BaseColor'];links.new(tex.outputs['Color'],bs.inputs['Base Color'])
 mask=nodes.new('ShaderNodeTexImage');mask.image=images['Mask'];inv=nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;links.new(mask.outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],bs.inputs['Roughness'])
 normal=nodes.new('ShaderNodeTexImage');normal.image=images['Normal'];nm=nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.35;links.new(normal.outputs['Color'],nm.inputs['Color']);links.new(nm.outputs['Normal'],bs.inputs['Normal'])
 for o in parts:
  o.data.materials.clear();o.data.materials.append(material)
  for f in o.data.polygons:f.material_index=0
  vg=o.vertex_groups.new(name='Part_'+o.name);vg.add(list(range(len(o.data.vertices))),1,'REPLACE')
  for v in o.data.vertices:assert abs(sum(g.weight for g in v.groups if o.vertex_groups[g.group].name in rig.data.bones)-1)<1e-5,(spec['name'],o.name,v.index)
 # One render mesh, retained part groups for editable subcomponents.
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.hide_set(False);o.select_set(True)
 bpy.context.view_layer.objects.active=head;bpy.ops.object.join();body=bpy.context.object;body.name='NPC_Body';parts=[body]
 body.data.materials.clear();body.data.materials.append(material)
 for f in body.data.polygons:f.material_index=0
 rig.data.pose_position='POSE';rig.animation_data_create();action=bpy.data.actions.new(spec['motion']);rig.animation_data.action=action
 rest={b.name:b.matrix_local.copy() for b in rig.data.bones}
 for pb in rig.pose.bones:pb.rotation_mode='QUATERNION'
 def segment(name,h,t):
  b=rig.data.bones[name];q=(b.tail_local-b.head_local).rotation_difference(t-h);m=q.to_matrix().to_4x4()@rest[name];m.translation=h;rig.pose.bones[name].matrix=m;bpy.context.view_layer.update()
 def limb(first,second,end,target,pole,foot=False):
  a=rig.data.bones[first];b=rig.data.bones[second];h=rig.pose.bones[first].head.copy();v=target-h;d=v.length;axis=v.normalized();l1=a.length;l2=b.length
  assert abs(l1-l2)+1e-5<d<l1+l2+.0001,(spec['name'],first,d,l1+l2)
  along=(l1*l1-l2*l2+d*d)/(2*d);height=math.sqrt(max(0,l1*l1-along*along));pol=(pole-axis*pole.dot(axis)).normalized();joint=h+axis*along+pol*height
  segment(first,h,joint);segment(second,joint,target)
  if foot:m=rest[end].copy();m.translation=target;rig.pose.bones[end].matrix=m
  else:segment(end,target,target+(target-joint).normalized()*rig.data.bones[end].length)
  bpy.context.view_layer.update()
 def rot(name,angles):rig.pose.bones[name].rotation_quaternion=Euler(tuple(math.radians(v) for v in angles),'XYZ').to_quaternion()
 first_vertices=None;loop_error=0;max_foot_drift=0;first_feet=None;previous={};motion_extent=0;bounds=[]
 for frame in range(1,92):
  t=(frame-1)/90;wave=math.sin(math.tau*t);breath=1-math.cos(math.tau*t)
  for pb in rig.pose.bones:pb.matrix_basis=Matrix.Identity(4)
  hips=rest['Hips'].copy()
  if spec['id']=='veteran_scavenger':hips.translation=Vector((0,.125,.285));hips=hips@Euler((math.radians(11),0,0),'XYZ').to_matrix().to_4x4()
  else:hips.translation.z-=.018
  rig.pose.bones['Hips'].matrix=hips;bpy.context.view_layer.update()
  head_yaw={'pawnshop':2.2,'veteran_scavenger':1.4,'district_warden':1.7,'wandering_merchant':3.5}[spec['id']]
  rot('Spine',(1.2*breath+(7 if spec['id']=='veteran_scavenger' else 0),0,.35*wave));rot('Chest',(.5*breath,0,0));rot('Head',(.65*breath,0,head_yaw*wave))
  bpy.context.view_layer.update()
  for s,side in [(1,'L'),(-1,'R')]:
   target=rig.data.bones['Foot.'+side].head_local.copy()
   if spec['id']=='veteran_scavenger':target.x+=s*.035
   limb('Thigh.'+side,'Shin.'+side,'Foot.'+side,target,Vector((s*.15,-1,0)),True)
  chest_skin=rig.pose.bones['Chest'].matrix@rest['Chest'].inverted()
  for s,side in [(1,'L'),(-1,'R')]:
   if spec['id']=='veteran_scavenger':
    knee=rig.pose.bones['Shin.'+side].head.copy();target=knee+Vector((s*.055,-.015,.10));pole=Vector((s*.8,-1,0))
   elif spec['id']=='district_warden' and s==1:target=chest_skin@Vector((.38,-.26,.80));pole=Vector((1,.1,-.4))
   elif spec['id']=='wandering_merchant' and s==1:target=chest_skin@Vector((.24,-.25,1.015));pole=Vector((1,.15,-.3))
   else:
    shoulder=rig.pose.bones['UpperArm.'+side].head.copy();target=shoulder+Vector((s*.16,-.045,-.386));pole=Vector((s*.5,-1,-.3))
   limb('UpperArm.'+side,'Forearm.'+side,'Hand.'+side,target,pole)
   if spec['id']=='district_warden' and s==1:segment('Hand.L',target,target+chest_skin.to_3x3()@Vector((-.09,.0,.02)))
   if spec['id']=='wandering_merchant' and s==1:segment('Hand.L',target,target+chest_skin.to_3x3()@Vector((-.065,.06,.018)))
  for pb in rig.pose.bones:
   if pb.name in previous and pb.rotation_quaternion.dot(previous[pb.name])<0:pb.rotation_quaternion=-pb.rotation_quaternion
   previous[pb.name]=pb.rotation_quaternion.copy()
   for path in ['location','rotation_quaternion','scale']:pb.keyframe_insert(path,frame=frame,group=pb.name)
  sc.frame_set(frame);bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();ev=body.evaluated_get(deps);me=ev.to_mesh();vs=[ev.matrix_world@v.co for v in me.vertices];ev.to_mesh_clear()
  assert all(math.isfinite(c) for v in vs for c in v)
  if first_vertices is None:first_vertices=vs
  loop_error=max((a-b).length for a,b in zip(first_vertices,vs));motion_extent=max(motion_extent,loop_error)
  feet=[rig.pose.bones['Foot.'+s].matrix.copy() for s in ['L','R']]
  if first_feet is None:first_feet=feet
  max_foot_drift=max(max_foot_drift,max(abs(a[i][j]-b[i][j]) for a,b in zip(first_feet,feet) for i in range(4) for j in range(4)))
  bounds.append([[min(v[i] for v in vs),max(v[i] for v in vs)] for i in range(3)])
 assert loop_error<1e-5 and max_foot_drift<1e-5,(loop_error,max_foot_drift)
 assert skel_sig(rig)==signature
 for fc in action.fcurves:
  for k in fc.keyframe_points:k.interpolation='LINEAR'
 action.use_fake_user=True;sc.frame_start=1;sc.frame_end=91;sc.render.fps=30;sc.frame_set(1)
 rig['npcId']=spec['id'];rig['motion']=spec['motion'];rig['integration']='Offline staging only; NPC data, gameplay and Unity unchanged'
 for im in images.values():im.pack()
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~'/(spec['name']+'.blend')))
 bpy.ops.object.select_all(action='DESELECT');body.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/(spec['name']+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)
 reports.append(dict(spec,bones=len(rig.data.bones),skeletonUnchanged=True,rootScale=list(rig.scale),triangles=len(body.data.polygons),vertices=len(body.data.vertices),actionCount=len(bpy.data.actions),frames=91,seconds=3,loopVertexError=loop_error,footMatrixDrift=max_foot_drift,maxVertexMotion=motion_extent,bounds=bounds[0],uvOverlapPixels=uvreport['overlapping_pixels'],uvCharts=uvreport['charts']))
 (OUT/'BuildValidation.json').write_text(json.dumps(reports,indent=2,ensure_ascii=False),encoding='utf8')
 print('NPC_COMPLETE',spec['name'],flush=True)
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==source_hash
(OUT/'SourcePreservation.json').write_text(json.dumps({'source':str(SOURCE),'sha256':source_hash,'unchanged':True},indent=2))
print('STORY_NPC_KIT_COMPLETE',flush=True)
