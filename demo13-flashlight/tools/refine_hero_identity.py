"""Hero identity revision: approved proportions, remodeled face and jacket silhouette."""
import bpy,bmesh,math,hashlib,json
from pathlib import Path
from mathutils import Vector,Quaternion
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'Assets/ChibiSurvivor/Player/HeroRefinement/BlenderSource~/HeroRefinement.blend'
OUT=ROOT/'Assets/ChibiSurvivor/Player/HeroIdentityReview';OUT.mkdir(parents=True,exist_ok=True)
(OUT/'BlenderSource~').mkdir(exist_ok=True)
source_hash=hashlib.sha256(SRC.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SRC));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig'];rig.animation_data_clear()
for pb in rig.pose.bones:pb.matrix_basis.identity()
bpy.context.view_layer.update()
scene.render.resolution_x=880;scene.render.resolution_y=1080;scene.render.resolution_percentage=100
scene.cycles.samples=32;scene.cycles.use_denoising=True
cam=scene.camera
def point(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
cam.location=(2.7,-6,2.5);point(cam,(0,0,.84));cam.data.ortho_scale=1.98
if not (OUT/'Before.png').exists():
 scene.render.filepath=str(OUT/'Before.png');bpy.ops.render.render(write_still=True)
def mat(name,color):
 m=bpy.data.materials.new('Identity_'+name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.85;p.inputs['Specular IOR Level'].default_value=.22;return m
M={k:mat(k,c) for k,c in {'jacket':(.068,.09,.075),'panel':(.082,.108,.087),'edge':(.11,.135,.105),'under':(.075,.083,.07),'scarf':(.29,.073,.036),'scarflight':(.36,.104,.052),'scarfshade':(.16,.033,.018),'ink':(.025,.022,.017),'eye':(.47,.40,.28),'iris':(.10,.074,.038),'pupil':(.018,.018,.014),'glint':(.65,.54,.33),'mouth':(.20,.10,.052),'patch':(.23,.165,.105),'metal':(.22,.225,.18),'dark':(.035,.04,.033)}.items()}
def assign(o,m):
 o.data.materials.clear();o.data.materials.append(m)
 for p in o.data.polygons:p.material_index=0
def mesh(name,vs,fs,material,bone):
 d=bpy.data.meshes.new(name);d.from_pydata(vs,[],fs);d.update();bm=bmesh.new();bm.from_mesh(d);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(d);bm.free()
 o=bpy.data.objects.new('Hero_'+name,d);scene.collection.objects.link(o);d.materials.append(material)
 g=o.vertex_groups.new(name=bone);g.add(list(range(len(d.vertices))),1,'REPLACE');m=o.modifiers.new('HeroArmature','ARMATURE');m.object=rig
 return o
def hide(name):
 o=bpy.data.objects.get(name)
 if o:o.hide_render=True;o.hide_set(True)
def lerpmap(z,knots):
 if z<=knots[0][0]:return knots[0][1]
 for (a,x),(b,y) in zip(knots,knots[1:]):
  if z<=b:return x+(y-x)*(z-a)/(b-a)
 return knots[-1][1]
# The existing head remains the base. Broaden lower cheeks and flatten the chin arc.
head=bpy.data.objects['Hero_Head'];head.data=head.data.copy()
adj={v.index:set() for v in head.data.vertices}
for e in head.data.edges:
 a,b=e.vertices;adj[a].add(b);adj[b].add(a)
remaining=set(adj);earverts=set()
while remaining:
 seed=next(iter(remaining));component={seed};todo=[seed];remaining.remove(seed)
 while todo:
  for j in adj[todo.pop()]:
   if j in remaining:remaining.remove(j);component.add(j);todo.append(j)
 meanx=sum(head.data.vertices[i].co.x for i in component)/len(component)
 if abs(meanx)>.24:earverts.update(component)
for v in head.data.vertices:
 x,y,z=v.co
 if v.index not in earverts:
  fac=lerpmap(z,[(1.20,1.32),(1.223,1.18),(1.251,1.11),(1.30,1.01),(1.40,1),(1.67,1)])
  v.co.x*=fac
  v.co.z+=lerpmap(z,[(1.2,.017),(1.251,.010),(1.30,0),(1.67,0)])
  # Slightly flatter front cheek planes, avoiding an inflated spherical muzzle.
  if y<-.11 and z<1.4:v.co.y-=.006*min(1,abs(x)/.18)
 else:
  # Smaller ears stay within the established head silhouette.
  center=Vector((math.copysign(.302,x),.018,1.36));v.co=center+(v.co-center)*.78
head.data.update();bpy.context.view_layer.update()
# Preserve the small original nose; redraw the eyes and mouth on the new surface.
oldface=bpy.data.objects['Hero_FaceDetails']
inds=sorted({i for p in oldface.data.polygons if 'nose' in oldface.data.materials[p.material_index].name.lower() for i in p.vertices})
idx={v:i for i,v in enumerate(inds)}
mesh('IdentityNose',[oldface.data.vertices[i].co.copy() for i in inds],[tuple(idx[i] for i in p.vertices) for p in oldface.data.polygons if all(i in idx for i in p.vertices)],next(m for m in oldface.data.materials if 'nose' in m.name.lower()),'Head')
hide('Hero_FaceDetails')
def decal(name,outline,material,offset=.003):
 center=(sum(x for x,z in outline)/len(outline),sum(z for x,z in outline)/len(outline))
 vs=[(center[0],-1,center[1])]+[(x,-1,z) for x,z in outline];fs=[(0,i+1,(i+1)%len(outline)+1) for i in range(len(outline))]
 o=mesh(name,vs,fs,material,'Head');bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=2,use_grid_fill=True);bm.to_mesh(o.data);bm.free()
 for v in o.data.vertices:
  hit,p,_,_=head.ray_cast(Vector((v.co.x,-1,v.co.z)),Vector((0,1,0)))
  if not hit:raise RuntimeError('Face projection missed '+name)
  v.co=p+Vector((0,-offset,0))
 o.data.update()
 for p in o.data.polygons:
  if p.normal.y>0:p.flip()
 # Subdivision changed indices: reassign the rigid head weights explicitly.
 o.vertex_groups.clear();g=o.vertex_groups.new(name='Head');g.add(list(range(len(o.data.vertices))),1,'REPLACE')
 return o
for sign in [-1,1]:
 cx=sign*.12;dz=.003 if sign==1 else 0
 decal('EyeWhite_'+str(sign),[(cx-.050,1.399+dz),(cx+.051,1.407+dz),(cx+.048,1.357+dz),(cx+.031,1.338+dz),(cx-.026,1.337+dz),(cx-.045,1.35+dz)],M['eye'],.0025)
 decal('Iris_'+str(sign),[(cx-.031,1.40+dz),(cx+.030,1.405+dz),(cx+.030,1.36+dz),(cx+.018,1.346+dz),(cx-.013,1.345+dz),(cx-.030,1.36+dz)],M['iris'],.004)
 decal('Pupil_'+str(sign),[(cx+math.sin(i*math.tau/16)*.017,1.377+dz+math.cos(i*math.tau/16)*.026) for i in range(16)],M['pupil'],.005)
 decal('EyeGlint_'+str(sign),[(cx-.009,1.391+dz),(cx-.002,1.393+dz),(cx-.002,1.385+dz),(cx-.008,1.384+dz)],M['glint'],.006)
 decal('UpperLid_'+str(sign),[(cx-.053,1.399+dz),(cx-.05,1.410+dz),(cx+.05,1.418+dz),(cx+.054,1.408+dz)],M['ink'],.0065)
 # Deliberate slight asymmetry: alert rather than identical sleepy eyes.
 zz=1.445+( .007 if sign<0 else 0)
 decal('Brow_'+str(sign),[(cx-.049,zz-sign*.008),(cx+.047,zz+sign*.008),(cx+.046,zz+.019+sign*.008),(cx-.044,zz+.02-sign*.008)],M['ink'],.004)
decal('Mouth',[(-.032,1.279),(-.008,1.282),(.019,1.278),(.040,1.272),(.036,1.268),(.017,1.274),(-.009,1.277),(-.032,1.276)],M['mouth'],.0035)
decal('CheekRepair',[(.19,1.294),(.235,1.311),(.233,1.337),(.185,1.321)],M['patch'],.004)
# Recut the existing jacket: broader chest, visible waist and flared hem.
body=bpy.data.objects['Hero_JacketBody'];body.data=body.data.copy()
for v in body.data.vertices:
 x,y,z=v.co;fac=lerpmap(z,[(.81,1.10),(.88,.89),(1.0,.96),(1.10,1.13),(1.18,1.08)])
 v.co.x*=fac;v.co.y*=lerpmap(z,[(.81,1.09),(.9,1),(1.08,1.15),(1.18,1.10)])
assign(body,M['jacket'])
for name in ['Hero_JacketOpening','Hero_Lapel_-1','Hero_Lapel_1','Hero_ScarfWrap','Hero_ScarfFront','Hero_ChestPocket','Hero_ChestPocketFlap','Hero_PocketFastener','Hero_JacketHem']:hide(name)
for o in scene.objects:
 if o.name.startswith('Hero_Refined_upper'):assign(o,M['jacket'])
 if o.name.startswith('Hero_RolledSleeve'):assign(o,M['edge'])
mesh('Underlayer',[(-.035,-.146,1.07),(.032,-.146,1.07),(.055,-.146,1.14),(-.055,-.146,1.14)],[(0,1,2,3)],M['under'],'Chest')
# Broad overlapping cloth panels carry the character at gameplay distance.
left=mesh('JacketFront_L',[(-.20,-.102,.835),(-.037,-.147,.842),(-.045,-.154,1.11),(-.17,-.122,1.13),(-.209,-.094,1.02)],[(0,1,2,3,4)],M['panel'],'Chest')
right=mesh('JacketFront_R',[(.033,-.15,.84),(.203,-.103,.82),(.218,-.092,1.04),(.17,-.125,1.135),(.036,-.16,1.08)],[(0,1,2,3,4)],M['jacket'],'Chest')
for o in [left,right]:
 # Cloth conforms to the remodeled torso instead of floating as flat plates.
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=3,use_grid_fill=True);bm.to_mesh(o.data);bm.free()
 body.data.update();bpy.context.view_layer.update()
 for v in o.data.vertices:
  hit,p,_,_=body.ray_cast(Vector((v.co.x,-1,v.co.z)),Vector((0,1,0)))
  if hit:v.co=p+Vector((0,-.004,0))
 o.data.update();o.vertex_groups.clear();g=o.vertex_groups.new(name='Chest');g.add(list(range(len(o.data.vertices))),1,'REPLACE')
 s=o.modifiers.new('ClothThickness','SOLIDIFY');s.thickness=.006
 b=o.modifiers.new('SoftClothEdge','BEVEL');b.width=.003;b.segments=1
for j,z in enumerate([.905,.975,1.045]):
 hit,p,_,_=body.ray_cast(Vector((.006,-1,z)),Vector((0,1,0)))
 if hit:
  vs=[(p.x,p.y-.011,p.z)]+[(p.x+.008*math.sin(i*math.tau/8),p.y-.007,p.z+.008*math.cos(i*math.tau/8)) for i in range(8)]
  mesh('JacketButton_'+str(j),vs,[(0,i+1,(i+1)%8+1) for i in range(8)],M['metal'],'Chest')
for sign in [-1,1]:
 o=mesh('RaisedCollar_'+str(sign),[(sign*.045,-.126,1.08),(sign*.132,-.108,1.14),(sign*.113,-.041,1.194),(sign*.063,-.03,1.182)],[(0,1,2,3)],M['edge'],'Chest')
 s=o.modifiers.new('CollarThickness','SOLIDIFY');s.thickness=.008
 # Tuck the raised collar behind the scarf.
 for v in o.data.vertices:v.co.y+=.035;v.co.z-=.015
# Scarf wrap and a broad side tail replace the small triangular bib.
vs=[];n=16
for row in range(3):
 for i in range(n):
  a=math.tau*i/n;x=math.sin(a)*(.125 if row==1 else .116);y=-math.cos(a)*(.108 if row==1 else .099);z=[1.135,1.162,1.19][row]+x*.08
  vs.append((x,y,z))
fs=[(r*n+i,r*n+(i+1)%n,(r+1)*n+(i+1)%n,(r+1)*n+i) for r in range(2) for i in range(n)]
o=mesh('ScarfWrap_New',vs,fs,M['scarf'],'Neck');s=o.modifiers.new('ScarfThickness','SOLIDIFY');s.thickness=.006
mesh('ScarfFrontFold',[(-.118,-.098,1.173),(-.079,-.149,1.121),(.033,-.171,1.109),(.129,-.117,1.169),(.030,-.154,1.17)],[(0,1,4),(1,2,4),(2,3,4)],M['scarflight'],'Chest')
tail=mesh('ScarfSideTail',[(.096,-.093,1.172),(.17,-.103,1.16),(.25,-.063,1.055),(.24,-.054,1.001),(.193,-.08,1.026),(.139,-.124,1.1)],[(0,1,5),(1,2,5),(2,3,4,5)],M['scarf'],'Chest');s=tail.modifiers.new('ScarfThickness','SOLIDIFY');s.thickness=.006
mesh('ScarfTailFold',[(.146,-.125,1.12),(.185,-.106,1.075),(.24,-.068,1.007),(.209,-.096,1.068)],[(0,1,3),(1,2,3)],M['scarfshade'],'Chest')
# Cap tilt changes the outer silhouette without altering head proportions.
cap=bpy.data.objects['Hero_Cap'];cap.data=cap.data.copy();q=Quaternion(Vector((0,1,0)),math.radians(-4));pivot=Vector((0,0,1.55))
for v in cap.data.vertices:v.co=pivot+q@(v.co-pivot)+Vector((.012,0,-.009))
hair=bpy.data.objects['Hero_Hair'];hair.data=hair.data.copy()
for v in hair.data.vertices:
 t=max(0,min(1,(v.co.z-1.43)/.10));target=pivot+q@(v.co-pivot)+Vector((.012,0,-.009));v.co=v.co.lerp(target,t)
bpy.context.view_layer.update()
for name,pos,target,scale in [('Preview',(2.7,-6,2.5),(0,0,.84),1.98),('Front',(0,-6,1.4),(0,0,.84),1.98),('Side',(6,0,1.4),(0,0,.84),1.98),('Face',(1,-4,1.65),(0,-.03,1.37),.78)]:
 cam.location=pos;point(cam,target);cam.data.ortho_scale=scale;scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
cam.location=(2.7,-6,2.5);point(cam,(0,0,.84));cam.data.ortho_scale=1.98
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/HeroIdentity.blend'))
assert hashlib.sha256(SRC.read_bytes()).hexdigest()==source_hash
print('IDENTITY_REVIEW_COMPLETE source preserved; gameplay not replaced')
