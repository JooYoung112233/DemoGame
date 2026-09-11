"""Standalone proportion study. No rig or production assets are replaced."""
import bpy, math
from pathlib import Path
from mathutils import Vector

OUT=Path(__file__).resolve().parents[1]/'Assets/ChibiSurvivor/ShapeStudy'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
def mat(name,c):
    m=bpy.data.materials.new(name); m.diffuse_color=(*c,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*c,1); p.inputs['Roughness'].default_value=.88
    return m
M={k:mat(k,c) for k,c in dict(skin=(.48,.30,.17),ear=(.32,.17,.083),cap=(.115,.102,.067),brim=(.047,.041,.028),hair=(.030,.020,.011),eye=(.027,.020,.013),iris=(.087,.063,.034),ivory=(.43,.33,.21),patch=(.20,.143,.092),shirt=(.145,.126,.081),pants=(.036,.036,.032),boots=(.058,.038,.021),sole=(.021,.021,.017)).items()}
def mesh(name,vs,fs,m):
    d=bpy.data.meshes.new(name); d.from_pydata(vs,[],fs); d.update()
    o=bpy.data.objects.new(name,d); bpy.context.collection.objects.link(o); d.materials.append(M[m]); return o
def box(name,pos,size,m,bevel=.02):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos); o=bpy.context.object; o.name=name; o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        b=o.modifiers.new('Form','BEVEL'); b.width=bevel; b.segments=1; bpy.ops.object.modifier_apply(modifier=b.name)
    o.data.materials.append(M[m]); return o
def rings(name,rows,m,n=20,power=1):
    vs=[]
    for z,rx,ry,cy in rows:
        for i in range(n):
            a=math.tau*i/n; x=math.sin(a); y=-math.cos(a)
            vs.append((rx*math.copysign(abs(x)**power,x),cy+ry*math.copysign(abs(y)**power,y),z))
    fs=[tuple(reversed(range(n)))]
    for j in range(len(rows)-1):
        for i in range(n): fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    fs.append(tuple(range((len(rows)-1)*n,len(rows)*n)))
    return mesh(name,vs,fs,m)
# Broad face, flattened front, smaller chin. Total height ~2.0; head+cap .82.
face_rows=[(1.18,.105,.12,-.01),(1.205,.205,.20,-.018),(1.26,.282,.238,-.008),(1.36,.326,.251,0),(1.49,.33,.254,0),(1.61,.32,.25,.008),(1.73,.277,.225,.018),(1.79,.15,.15,.025)]
rings('Face',face_rows,'skin',24,.72)
def front(x,z):
    z=max(face_rows[0][0],min(face_rows[-1][0],z))
    for a,b in zip(face_rows,face_rows[1:]):
        if a[0]<=z<=b[0]:
            t=(z-a[0])/(b[0]-a[0]); rx=a[1]*(1-t)+b[1]*t; ry=a[2]*(1-t)+b[2]*t; cy=a[3]*(1-t)+b[3]*t
            return cy-ry*max(0,1-(abs(x)/rx)**(2/.72))**(.72/2)
def decal(name,points,m,offset=.004):
    # Polygon follows the skin surface; no raised eyeball or surrounding ring.
    cx=sum(x for x,z in points)/len(points); cz=sum(z for x,z in points)/len(points)
    vs=[(cx,front(cx,cz)-offset,cz)]+[(x,front(x,z)-offset,z) for x,z in points]
    return mesh(name,vs,[(0,i+1,(i+1)%len(points)+1) for i in range(len(points))],m)
for s in [-1,1]:
    x=s*.132
    decal('EyeWhite',[(x-.058,1.454),(x-.05,1.379),(x-.032,1.357),(x+.026,1.354),(x+.05,1.379),(x+.055,1.454)],'ivory',.006)
    decal('Iris',[(x+math.sin(i*math.tau/20)*.039,1.41+math.cos(i*math.tau/20)*.053) for i in range(20)],'iris',.008)
    decal('Pupil',[(x+math.sin(i*math.tau/16)*.023,1.419+math.cos(i*math.tau/16)*.042) for i in range(16)],'eye',.01)
    decal('UpperLid',[(x-.065,1.452),(x-.064,1.466),(x+.057,1.465),(x+.063,1.449)],'hair',.012)
    decal('Brow',[(x-.065,1.501),(x-.061,1.522),(x+.058,1.513),(x+.062,1.496)],'hair',.007)
    # Faceted ear, with small inset surface rather than protruding pale rings.
    vs=[(s*.308,-.035,1.43),(s*.355,-.035,1.485),(s*.402,-.029,1.459),(s*.41,-.023,1.386),(s*.371,-.049,1.34),(s*.316,-.063,1.358),(s*.361,-.067,1.411)]
    mesh('Ear',vs,[(6,i,(i+1)%6) for i in range(6)],'skin')
    mesh('EarInset',[(s*.346,-.069,1.428),(s*.377,-.061,1.438),(s*.384,-.055,1.39),(s*.354,-.071,1.372)],[(0,1,2,3)],'ear')
mesh('Nose',[(0,-.285,1.365),(-.021,-.253,1.36),(0,-.255,1.397),(.021,-.253,1.36),(0,-.253,1.347)],[(0,1,2),(0,2,3),(0,3,4),(0,4,1)],'ear')
decal('Mouth',[(-.037,1.286),(0,1.291),(.038,1.286),(.037,1.282),(0,1.286),(-.037,1.282)],'ear')
decal('CheekPatch',[(.225,1.315),(.269,1.338),(.263,1.367),(.217,1.344)],'patch',.007)
# Dark cropped hair at temples and back. Front is open for the face.
for s in [-1,1]:
    mesh('TempleHair',[(s*.255,-.22,1.622),(s*.322,-.15,1.62),(s*.328,-.11,1.40),(s*.285,-.175,1.44)],[(0,1,2,3)],'hair')
rings('BackHair',[(1.30,.20,.09,.15),(1.39,.31,.14,.13),(1.57,.33,.17,.085),(1.72,.29,.20,.035)],'hair',20)
# Cap hugs the skull; elongated curved bill is a separate mesh.
rings('CapCrown',[(1.589,.345,.283,.008),(1.69,.34,.277,.016),(1.82,.291,.247,.025),(1.921,.20,.177,.033),(1.962,.072,.065,.035)],'cap',20,.72)
vs=[]; N=13
for outer in [False,True]:
    for i in range(N):
        t=-1+2*i/(N-1); x=t*(.307 if outer else .302)
        y=(-.48+.105*t*t) if outer else (-.261+.076*t*t)
        z=1.597-.029*(1-t*t)-( .025 if outer else 0)
        vs.append((x,y,z))
vs += [(x,y,z-.016) for x,y,z in vs]
fs=[]
for i in range(N-1):
    fs.extend([(i,i+1,N+i+1,N+i),(2*N+i,3*N+i,3*N+i+1,2*N+i+1),(N+i,N+i+1,3*N+i+1,3*N+i)])
mesh('CapBill',vs,fs,'brim')
box('CapPatch',(0,-.258,1.778),(.13,.012,.106),'patch',.005)
for x in [-.063,.063]:
    for z in [1.746,1.806]: box('CapStitch',(x,-.267,z),(.018,.006,.005),'brim',.001)
box('CapButton',(0,.035,1.964),(.054,.049,.016),'brim',.006)
# Deliberately plain clothing volumes for silhouette approval.
rings('Shirt',[(.66,.217,.137,0),(.76,.232,.147,0),(1.05,.212,.142,0),(1.175,.14,.103,0)],'shirt',12,.7)
box('Neck',(0,0,1.16),(.16,.15,.12),'skin',.03)
box('Hips',(0,0,.644),(.366,.257,.155),'pants',.035)
def limb(name,a,b,r1,r2,m):
    av,bv=Vector(a),Vector(b); bpy.ops.mesh.primitive_cone_add(vertices=10,radius1=r1,radius2=r2,depth=(bv-av).length,location=(av+bv)/2)
    o=bpy.context.object; o.name=name; o.rotation_euler=(bv-av).to_track_quat('Z','Y').to_euler(); o.data.materials.append(M[m])
for s in [-1,1]:
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=1,location=(s*.219,0,1.065))
    shoulder=bpy.context.object; shoulder.name='Shoulder'; shoulder.scale=(.106,.098,.092); shoulder.data.materials.append(M['shirt'])
    limb('Sleeve',(s*.21,0,1.074),(s*.302,0,.83),.095,.106,'shirt')
    limb('ForeSleeve',(s*.302,0,.84),(s*.328,-.016,.756),.086,.094,'shirt')
    box('Cuff',(s*.323,-.012,.768),(.17,.182,.066),'shirt',.009)
    limb('Wrist',(s*.328,-.01,.755),(s*.339,-.015,.677),.053,.063,'skin')
    box('Hand',(s*.339,-.015,.632),(.12,.123,.131),'skin',.027)
    limb('TrouserLeg',(s*.113,0,.639),(s*.113,0,.20),.10,.109,'pants')
    box('Boot',(s*.113,-.036,.113),(.19,.274,.179),'boots',.031)
    box('Sole',(s*.113,-.041,.035),(.202,.28,.037),'sole',.009)
    box('TrouserCuff',(s*.113,0,.23),(.207,.21,.064),'pants',.01)

scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=32
scene.world.color=(.10,.10,.10)
scene.view_settings.view_transform='AgX'; scene.view_settings.look='AgX - Medium High Contrast'
scene.view_settings.exposure=-.4
def area(pos,power,size):
    bpy.ops.object.light_add(type='AREA',location=pos); o=bpy.context.object; o.data.energy=power; o.data.shape='DISK'; o.data.size=size; o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
area((-3,-4,5),350,4); area((3,-2,2),90,3); area((1,3,4),180,3)
bpy.ops.object.camera_add(location=(0,-6,1.38)); scene.camera=bpy.context.object
def camera(pos):
    scene.camera.location=pos; scene.camera.rotation_euler=(Vector((0,0,1))-scene.camera.location).to_track_quat('-Z','Y').to_euler(); scene.camera.data.type='ORTHO'; scene.camera.data.ortho_scale=2.23
camera((0,-6,1.38))
scene.render.resolution_x=800; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SurvivorShapeStudy.blend'))
for name,pos in [('Front',(0,-6,1.38)),('Quarter',(3,-6,2.05))]:
    camera(pos); scene.render.filepath=str(OUT/(name+'.png')); bpy.ops.render.render(write_still=True)
print('SHAPE_STUDY_CREATED',OUT)
