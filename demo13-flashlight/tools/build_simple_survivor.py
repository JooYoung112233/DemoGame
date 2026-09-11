"""Approved two-head survivor, modular static modeling pass.
Run in Blender background. Existing character/animations are never overwritten.
"""
import bpy, math, bmesh
from pathlib import Path
from mathutils import Vector

OUT=Path(__file__).resolve().parents[1]/'Assets/ChibiSurvivor/SimpleSurvivor'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
COLORS={'skin':(.59,.366,.197),'ear':(.43,.222,.103),'cap':(.175,.139,.080),
        'brim':(.065,.055,.040),'hair':(.035,.023,.013),'brow':(.028,.020,.012),
        'white':(.53,.42,.28),'iris':(.065,.044,.023),'pupil':(.030,.022,.014),
        'lid':(.215,.115,.054),'nose':(.40,.184,.084),'patch':(.27,.197,.127),
        'shirt':(.225,.179,.103),'seam':(.148,.114,.066),'under':(.52,.431,.28),
        'pants':(.044,.044,.039),'cuff':(.075,.072,.061),'boots':(.108,.067,.035),'sole':(.032,.028,.021)}
M={}
for name,c in COLORS.items():
    m=bpy.data.materials.new(name); m.diffuse_color=(*c,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*c,1); bs.inputs['Roughness'].default_value=.87
    M[name]=m

def mesh(name,vs,fs,mat,slot):
    d=bpy.data.meshes.new(name); d.from_pydata(vs,[],fs); d.update()
    bm=bmesh.new(); bm.from_mesh(d); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(d); bm.free()
    o=bpy.data.objects.new(name,d); bpy.context.collection.objects.link(o); d.materials.append(M[mat]); o['slot']=slot
    return o

def rings(name,rows,mat,slot,n=24,p=.82):
    vs=[]
    for z,rx,ry,cy in rows:
        for i in range(n):
            a=math.tau*i/n; x,y=math.sin(a),-math.cos(a)
            vs.append((rx*math.copysign(abs(x)**p,x),cy+ry*math.copysign(abs(y)**p,y),z))
    fs=[tuple(reversed(range(n)))]
    for k in range(len(rows)-1):
        for i in range(n): fs.append((k*n+i,k*n+(i+1)%n,(k+1)*n+(i+1)%n,(k+1)*n+i))
    fs.append(tuple(range((len(rows)-1)*n,len(rows)*n)))
    return mesh(name,vs,fs,mat,slot)

def box(name,pos,size,mat,slot,bevel=.012,segments=2):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos); o=bpy.context.object; o.name=name; o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        b=o.modifiers.new('RoundedEdges','BEVEL'); b.width=bevel; b.segments=segments; bpy.ops.object.modifier_apply(modifier=b.name)
    o.data.materials.append(M[mat]); o['slot']=slot; return o

def oval(name,pos,scale,mat,slot,n=16,r=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=n,ring_count=r,radius=1,location=pos)
    o=bpy.context.object; o.name=name; o.scale=scale; o.data.materials.append(M[mat]); o['slot']=slot
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); return o

face=rings('Face',[(.806,.075,.092,-.004),(.819,.18,.174,-.005),(.844,.278,.235,-.004),
    (.89,.362,.285,0),(.962,.425,.316,.008),(1.045,.456,.338,.016),
    (1.13,.462,.345,.023),(1.23,.454,.348,.032),(1.34,.435,.345,.041),
    (1.46,.389,.312,.05),(1.54,.26,.23,.05),(1.57,.09,.09,.05)],'skin','head',40,.9)
for poly in face.data.polygons: poly.use_smooth=True
bpy.context.view_layer.update()

def surface(name,outline,mat,target,slot,offset=.0015):
    # Subdivide BEFORE ray-projecting: patches follow the actual faceted surface.
    cx=sum(x for x,z in outline)/len(outline); cz=sum(z for x,z in outline)/len(outline)
    vs=[(cx,-2,cz)]+[(x,-2,z) for x,z in outline]
    fs=[(0,i+1,(i+1)%len(outline)+1) for i in range(len(outline))]
    o=mesh(name,vs,fs,mat,slot)
    bm=bmesh.new(); bm.from_mesh(o.data); bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=3,use_grid_fill=True); bm.to_mesh(o.data); bm.free()
    for v in o.data.vertices:
        hit,loc,normal,_=target.ray_cast(Vector((v.co.x,-2,v.co.z)),Vector((0,1,0)))
        if not hit: raise RuntimeError('Surface projection missed '+name+str(v.co))
        v.co=loc+Vector((0,-offset,0))
    for poly in o.data.polygons:
        if poly.normal.y>0: poly.flip()
    o.data.update(); return o

for s in [-1,1]:
    x=s*.192
    # Half-lidded eyes; flush color surfaces have no visible globe or socket ring.
    outline=[(x-.068,1.12),(x-.066,1.07),(x-.055,1.017),(x-.029,.997),(x+.025,.997),(x+.054,1.017),(x+.067,1.07),(x+.067,1.12)]
    surface('EyeWhite',outline,'white',face,'face_details',.0018)
    iris=[(x-.051,1.12),(x-.051,1.063),(x-.04,1.022),(x-.018,1.006),(x+.018,1.006),(x+.039,1.022),(x+.048,1.063),(x+.048,1.12)]
    surface('Iris',iris,'iris',face,'face_details',.0032)
    surface('Pupil',[(x+math.sin(i*math.tau/20)*.028,1.077+math.cos(i*math.tau/20)*.044) for i in range(20)],'pupil',face,'face_details',.0045)
    surface('Eyelid',[(x-.079,1.116),(x-.073,1.133),(x-.045,1.14),(x+.043,1.138),(x+.074,1.127),(x+.078,1.114)],'lid',face,'face_details',.0055)
    surface('Lash',[(x-.078,1.115),(x-.077,1.122),(x-.01,1.126),(x+.076,1.12),(x+.078,1.113),(x,1.117)],'brow',face,'face_details',.006)
    surface('Brow',[(x-.082,1.177),(x-.074,1.20),(x+.068,1.197),(x+.075,1.174)],'brow',face,'face_details',.003)
    # Closed ears with broad, shallow inner facets.
    outline=[(.42,1.102),(.462,1.151),(.526,1.146),(.559,1.108),(.559,1.035),(.526,.996),(.458,.991),(.426,1.023)]
    vs=[(s*.49,-.123,1.07)]+[(s*x,-.075,z) for x,z in outline]+[(s*x,.035,z) for x,z in outline]
    fs=[(0,i+1,(i+1)%8+1) for i in range(8)]+[(i+1,(i+1)%8+1,(i+1)%8+9,i+9) for i in range(8)]+[tuple(range(9,17))]
    oval('Ear',(s*.474,-.015,1.067),(.082,.070,.105),'skin','head',12,8)
    oval('InnerEar',(s*.485,-.077,1.068),(.047,.008,.063),'ear','head',12,8)

# Small angular nose; mouth and bandage conform tightly to the cheeks.
mesh('Nose',[(0,-.363,1.019),(-.037,-.316,1.012),(0,-.327,1.06),(.037,-.316,1.012),(0,-.325,.997)],[(0,1,2),(0,2,3),(0,3,4),(0,4,1),(1,4,3,2)],'nose','face_details')
surface('Mouth',[(-.057,.909),(-.028,.914),(0,.916),(.032,.913),(.058,.907),(.057,.904),(0,.911),(-.055,.905)],'lid',face,'face_details',.002)
surface('Bandage',[(.29,.922),(.371,.949),(.367,.989),(.283,.96)],'patch',face,'face_details',.0025)

# Hair cap is cropped away from the face, with two restrained pointed side locks.
vs=[]; rows=[(.86,.28,.25),(.95,.415,.32),(1.10,.458,.36),(1.29,.465,.37),(1.43,.4,.33)]
for z,rx,ry in rows:
    for i in range(19):
        a=math.pi*.43+(math.pi*1.14)*i/18
        vs.append((rx*math.sin(a),.03-ry*math.cos(a),z))
mesh('HairBack',vs,[(j*19+i,j*19+i+1,(j+1)*19+i+1,(j+1)*19+i) for j in range(4) for i in range(18)],'hair','hair')
for s in [-1,1]:
    mesh('HairTemple',[(s*.331,-.20,1.345),(s*.443,-.12,1.308),(s*.433,-.19,1.048),(s*.386,-.264,1.064),(s*.363,-.28,1.211),(s*.29,-.289,1.27)],[(0,1,2,3,4,5)],'hair','hair')

# Rounded cap panels and a bent bill whose centre arches upward at the forehead.
cap=rings('CapCrown',[(1.286,.477,.384,.046),(1.416,.489,.391,.054),(1.56,.444,.365,.066),
    (1.682,.34,.287,.08),(1.752,.185,.167,.088),(1.775,.055,.055,.09)],'cap','cap',24,.9)
vs=[]; N=17
for row in range(3):
    u=row/2
    for i in range(N):
        t=-1+2*i/(N-1); x=t*(.431-.014*u)
        y=(-.335+.095*t*t)*(1-u)+(-.624+.175*t*t)*u
        z=1.39-.092*t*t-.049*u
        vs.append((x,y,z))
cnt=len(vs); vs += [(x,y,z-.017) for x,y,z in vs]
fs=[]
for r in range(2):
    for i in range(N-1):
        a=r*N+i; fs.extend([(a,a+1,a+1+N,a+N),(a+cnt,a+cnt+N,a+cnt+N+1,a+cnt+1)])
border=list(range(N))+[N+N-1,3*N-1]+list(reversed(range(2*N,3*N-1)))+[N]
for a,b in zip(border,border[1:]+border[:1]): fs.append((a,b,b+cnt,a+cnt))
mesh('CapBill',vs,fs,'brim','cap')
bpy.context.view_layer.update()
surface('CapPatch',[(-.135,1.472),(.135,1.472),(.126,1.629),(-.126,1.629)],'patch',cap,'cap',.009)
box('TopButton',(0,.09,1.779),(.083,.073,.020),'brim','cap',.009)

# Compact shirt, small folded collar and ONE shallow pocket.
shirt=rings('Shirt',[(.451,.25,.163,0),(.50,.262,.173,0),(.65,.242,.164,0),(.734,.198,.147,0),(.79,.127,.113,0)],'shirt','shirt',20,.8)
oval('Neck',(0,0,.793),(.115,.101,.073),'skin','body')
bpy.context.view_layer.update()
surface('Undershirt',[(-.07,.772),(0,.68),(.07,.772)],'under',shirt,'shirt',.002)
surface('Placket',[(-.006,.452),(.006,.452),(.006,.69),(-.006,.69)],'seam',shirt,'shirt',.0015)
for s in [-1,1]:
    mesh('Collar',[(s*.035,-.124,.784),(s*.108,-.141,.772),(s*.155,-.159,.697),(s*.093,-.184,.72),(s*.041,-.175,.687)],[(0,1,2,3,4)],'shirt','shirt')
surface('Pocket',[(.077,.541),(.157,.541),(.163,.551),(.163,.616),(.073,.616),(.073,.551)],'shirt',shirt,'shirt',.004)
surface('PocketLip',[(.073,.613),(.163,.613),(.163,.619),(.073,.619)],'seam',shirt,'shirt',.005)

def sleeve(s):
    # One tapered sleeve with overlapping shoulder envelope, no detached ball joints.
    a=Vector((s*.188,0,.733)); b=Vector((s*.307,-.003,.53)); q=(b-a).to_track_quat('Z','Y')
    vs=[]
    for t,r in [(0,.05),(.12,.084),(.78,.092),(1,.088)]:
        for i in range(12):
            v=a+(b-a)*t+q@Vector((math.cos(i*math.tau/12)*r,math.sin(i*math.tau/12)*r,0)); vs.append(v)
    fs=[tuple(reversed(range(12)))]+[(j*12+i,j*12+(i+1)%12,(j+1)*12+(i+1)%12,(j+1)*12+i) for j in range(3) for i in range(12)]+[tuple(range(36,48))]
    o=mesh('Sleeve',vs,fs,'shirt','shirt')
    for poly in o.data.polygons: poly.use_smooth=True
    cuff=box('RolledSleeve',b,(.193,.198,.060),'shirt','shirt',.011); cuff.rotation_euler=q.to_euler()
    oval('Forearm',(s*.322,-.006,.48),(.065,.065,.098),'skin','body')
    oval('Mitten',(s*.343,-.012,.405),(.075,.072,.082),'skin','hands',16,10)
    oval('Thumb',(s*.296,-.061,.412),(.032,.036,.042),'skin','hands',12,8)
for s in [-1,1]: sleeve(s)
rings('Hip',[(.365,.203,.134,.006),(.415,.229,.145,.006),(.48,.235,.15,.006)],'pants','trousers',20,.85)
for s in [-1,1]:
    leg=rings('TrouserLeg',[(.212,.104,.113,.005),(.28,.109,.121,.005),(.444,.112,.127,.005)],'pants','trousers',12,.78); leg.location.x=s*.126
    box('TrouserCuff',(s*.126,.005,.225),(.225,.244,.061),'cuff','trousers',.012)
    oval('BootUpper',(s*.126,-.015,.139),(.101,.116,.077),'boots','boots',12,8)
    box('BootToe',(s*.126,-.062,.084),(.215,.29,.132),'boots','boots',.04,2)
    box('Sole',(s*.126,-.069,.025),(.228,.301,.04),'sole','boots',.014,2)

# Keep slots as separate mesh objects; source remains editable and unrigged.
for slot in ['head','face_details','hair','cap','body','hands','shirt','trousers','boots']:
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.get('slot')==slot]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]; bpy.ops.object.join()
    o=bpy.context.object; o.name='Survivor_'+slot; o['equipment_slot']=slot
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)

scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=48
scene.world.use_nodes=True; scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.11,.115,.12,1); scene.world.node_tree.nodes['Background'].inputs[1].default_value=.45
scene.view_settings.view_transform='AgX'; scene.view_settings.look='AgX - Medium High Contrast'; scene.view_settings.exposure=0
def light(pos,power,size):
    bpy.ops.object.light_add(type='AREA',location=pos); o=bpy.context.object; o.data.energy=power; o.data.shape='DISK'; o.data.size=size; o.rotation_euler=(Vector((0,0,.95))-o.location).to_track_quat('-Z','Y').to_euler()
light((-3,-4,5),420,4); light((3,-4,2.5),170,3); light((1,3,4),220,3)
bpy.ops.object.camera_add(); scene.camera=bpy.context.object; scene.camera.data.type='ORTHO'; scene.camera.data.ortho_scale=2.04
def cam(pos):
    scene.camera.location=pos; scene.camera.rotation_euler=(Vector((0,0,.90))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
cam((0,-6,1.25))
scene.render.resolution_x=880; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene['stage']='Static appearance approval; no rig or animation yet'
bpy.ops.object.select_all(action='DESELECT')
for o in scene.objects:
    if o.type=='MESH':o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'SimpleSurvivor.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
bpy.ops.export_scene.gltf(filepath=str(OUT/'SimpleSurvivor.glb'),use_selection=True,export_format='GLB')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SimpleSurvivor.blend'))
for name,pos in [('Front',(0,-6,1.25)),('Quarter',(3,-6,1.8)),('Side',(6,-.25,1.25))]:
    cam(pos); scene.render.filepath=str(OUT/(name+'.png')); bpy.ops.render.render(write_still=True)
print('SIMPLE_SURVIVOR_CREATED',OUT)
