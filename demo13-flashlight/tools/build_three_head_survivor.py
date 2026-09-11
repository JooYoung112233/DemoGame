"""Three-head modular survivor: static appearance pass with reproducible parts.

Run with Blender --background --python tools/build_three_head_survivor.py.
The body is newly modeled. Only the already separate face/headwear is reused.
"""
import bpy
import bmesh
import math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ChibiSurvivor/ThreeHeadSurvivor'
OUT.mkdir(parents=True,exist_ok=True)
SOURCE=ROOT/'Assets/ChibiSurvivor/SimpleSurvivor/SimpleSurvivor.blend'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
KEEP={'Survivor_head':'Head','Survivor_face_details':'FaceDetails','Survivor_hair':'Hair','Survivor_cap':'Cap'}
for obj in list(bpy.data.objects):
    if obj.name not in KEEP:
        bpy.data.objects.remove(obj,do_unlink=True)

# Chin-to-cap height = .600; full standing height = 1.800 exactly.
head_objects=[bpy.data.objects[n] for n in KEEP]
head_min=min(v.co.z for v in bpy.data.objects['Survivor_head'].data.vertices)
head_max=max(v.co.z for o in head_objects for v in o.data.vertices)
HEAD_SCALE=.600/(head_max-head_min)
for old,new in KEEP.items():
    obj=bpy.data.objects[old]
    for v in obj.data.vertices:
        v.co.x*=HEAD_SCALE
        v.co.y*=HEAD_SCALE
        v.co.z=1.200+(v.co.z-head_min)*HEAD_SCALE
    obj.name=new
    obj['part']=new
    obj['stage']='static appearance; not rigged'
    obj.data.update()

COLORS={
    'Skin':(.56,.34,.184),'SkinShadow':(.32,.16,.068),
    'Shirt':(.178,.153,.098),'ShirtEdge':(.145,.123,.077),
    'Seam':(.10,.081,.048),'Undershirt':(.42,.35,.231),
    'Trousers':(.038,.039,.035),'TrouserPatch':(.066,.063,.052),
    'TrouserCuff':(.084,.079,.063),'Leather':(.050,.033,.019),
    'Boot':(.079,.049,.026),'BootToe':(.093,.057,.028),'Sole':(.024,.024,.019),
    'Bag':(.060,.058,.043),'BagFlap':(.081,.075,.054),'Repair':(.13,.112,.078),
    'Steel':(.18,.163,.124),'BuckleInset':(.033,.029,.023),
    'WatchDial':(.53,.46,.32),'WatchInk':(.037,.036,.026)
}
M={}
for name,rgb in COLORS.items():
    mat=bpy.data.materials.get('ThreeHead_'+name) or bpy.data.materials.new('ThreeHead_'+name)
    mat.diffuse_color=(*rgb,1); mat.use_nodes=True
    bs=mat.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*rgb,1)
    bs.inputs['Roughness'].default_value=.86
    if name=='Steel': bs.inputs['Metallic'].default_value=.35
    M[name]=mat

def finish(obj,mat,part):
    obj.data.materials.append(M[mat]); obj['part']=part
    return obj

def mesh(name,verts,faces,mat,part,smooth=False):
    data=bpy.data.meshes.new(name); data.from_pydata(verts,[],faces); data.update()
    bm=bmesh.new(); bm.from_mesh(data); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(data); bm.free()
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj)
    for p in data.polygons:p.use_smooth=smooth
    return finish(obj,mat,part)

def box(name,pos,size,mat,part,bevel=.006,rot=(0,0,0),segments=1):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
    o=bpy.context.object; o.name=name; o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        m=o.modifiers.new('EdgeShape','BEVEL'); m.width=bevel; m.segments=segments
        bpy.ops.object.modifier_apply(modifier=m.name)
    o.rotation_euler=rot
    return finish(o,mat,part)

def oval(name,pos,scale,mat,part,segments=14,rings=8,smooth=False):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=pos)
    o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for p in o.data.polygons:p.use_smooth=smooth
    return finish(o,mat,part)

def loft(name,rows,mat,part,n=16,power=.78,smooth=False):
    # rows: z, half-width, half-depth, y-center, x-center
    vs=[]
    for z,rx,ry,cy,cx in rows:
        for i in range(n):
            t=math.tau*i/n; sx,sy=math.sin(t),-math.cos(t)
            vs.append((cx+rx*math.copysign(abs(sx)**power,sx),cy+ry*math.copysign(abs(sy)**power,sy),z))
    fs=[tuple(reversed(range(n)))]
    fs += [(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(rows)-1) for i in range(n)]
    fs.append(tuple(range((len(rows)-1)*n,len(rows)*n)))
    return mesh(name,vs,fs,mat,part,smooth)

def tube(name,a,b,r1,r2,mat,part,n=12):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=n,radius1=r1,radius2=r2,depth=(b-a).length,location=(a+b)/2)
    o=bpy.context.object; o.name=name; o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return finish(o,mat,part)

def patch(name,outline,target,mat,part,offset=.004):
    x=sum(v[0] for v in outline)/len(outline); z=sum(v[1] for v in outline)/len(outline)
    verts=[(x,-1,z)]+[(x,-1,z) for x,z in outline]
    obj=mesh(name,verts,[(0,i+1,(i+1)%len(outline)+1) for i in range(len(outline))],mat,part)
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=2,use_grid_fill=True)
    bm.to_mesh(obj.data); bm.free()
    for v in obj.data.vertices:
        hit,point,_,_=target.ray_cast(Vector((v.co.x,-1,v.co.z)),Vector((0,1,0)))
        if not hit: raise RuntimeError('Patch projection missed: '+name)
        v.co=point+Vector((0,-offset,0))
    for p in obj.data.polygons:
        if p.normal.y>0:p.flip()
    obj.data.update(); return obj

def ribbon(name,points,width,mat,part):
    vs=[]
    for x,y,z in points:vs.extend([(x-width/2,y,z),(x+width/2,y,z)])
    return mesh(name,vs,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(points)-1)],mat,part)

# Fit the hair to the actual scalp, excluding ears from the ray-cast surface.
bpy.data.objects.remove(bpy.data.objects['Hair'],do_unlink=True)
hd=bpy.data.objects['Head'].data
scalp=mesh('ScalpProjection',[v.co.copy() for v in hd.vertices],
    [tuple(p.vertices) for p in hd.polygons if p.use_smooth],'Skin','_helper')
bpy.context.view_layer.update()
vs=[]; columns=57; levels=13
for row in range(levels):
    t=row/(levels-1)
    for col in range(columns):
        u=col/(columns-1); a=math.pi*.31+math.pi*1.38*u
        bottom=1.245+.12*abs(2*u-1)**1.6
        z=bottom+(1.57-bottom)*t
        outward=Vector((math.sin(a),-math.cos(a),0))
        hit,point,_,_=scalp.ray_cast(outward*2+Vector((0,0,z)),-outward)
        if not hit:raise RuntimeError('Hair scalp projection missed')
        vs.append(point+outward*.006)
hair=mesh('CroppedHair',vs,[(j*columns+i,j*columns+i+1,(j+1)*columns+i+1,(j+1)*columns+i)
    for j in range(levels-1) for i in range(columns-1)],'Leather','Hair')
hair.data.materials.clear(); hair.data.materials.append(bpy.data.materials['hair'])
bpy.data.objects.remove(scalp,do_unlink=True)

# Short neck is covered by the collar, shoulders and shirt are shaped together.
oval('Neck',(0,0,1.196),(.068,.064,.057),'Skin','Body',smooth=True)
shirt=loft('WorkShirt',[(.751,.173,.107,0,0),(.78,.185,.112,0,0),(.88,.175,.11,0,0),
    (1.028,.177,.112,0,0),(1.112,.176,.105,0,0),(1.162,.139,.082,0,0),(1.188,.070,.058,0,0)],'Shirt','Shirt',n=24,power=.74)
bpy.context.view_layer.update()
patch('Undershirt',[(-.044,1.175),(0,1.09),(.044,1.175)],shirt,'Undershirt','Shirt',.004)
patch('ButtonPlacket',[(-.010,.775),(.010,.775),(.010,1.094),(-.010,1.094)],shirt,'ShirtEdge','Shirt',.002)
for z in [.80,.91,1.022]:
    oval('Button',(0,-.119,z),(.009,.005,.009),'Steel','Shirt',segments=8,rings=4)
for s in [-1,1]:
    mesh('CollarFold',[(s*.016,-.065,1.188),(s*.068,-.070,1.185),(s*.097,-.10,1.125),
        (s*.065,-.116,1.141),(s*.028,-.120,1.100)],[(0,1,2,3,4)],'Shirt','Shirt')
    x=s*.078
    patch('ChestPocket',[(x-.038,.963),(x+.038,.963),(x+.043,.973),(x+.043,1.057),(x-.043,1.057),(x-.043,.973)],shirt,'Shirt','Shirt',.006)
    patch('PocketFlap',[(x-.045,1.047),(x,1.039),(x+.045,1.047),(x+.045,1.070),(x-.045,1.070)],shirt,'ShirtEdge','Shirt',.008)
    oval('PocketButton',(x,-.119,1.052),(.006,.004,.006),'Steel','Shirt',8,4)
    # Sleeve rings run along the bent arm; shoulder cap penetrates shirt subtly.
    rows=[(1.141,.044,.071,0,s*.161),(1.114,.075,.078,0,s*.185),
          (1.048,.074,.079,0,s*.220),(.973,.063,.071,0,s*.244),
          (.918,.062,.067,-.003,s*.262)]
    loft('Sleeve',rows,'Shirt','Shirt',n=14,power=.9,smooth=True)
    cuff=box('RolledCuff',(s*.262,-.003,.923),(.147,.151,.051),'ShirtEdge','Shirt',.009,rot=(0,-s*.13,0))
    loft('Forearm',[(.752,.040,.042,-.019,s*.29),(.793,.043,.045,-.013,s*.282),(.902,.052,.05,-.006,s*.267),(.931,.051,.05,-.004,s*.263)],'Skin','Body',n=14,power=1,smooth=True)
    box('Palm',(s*.296,-.018,.724),(.083,.068,.081),'Skin','Hands',.021,segments=2)
    # Compact separated finger tips make gripping more plausible than mitten balls.
    for j in range(4):
        fx=s*(.265+j*.020)
        box('Finger',(fx,-.026,.683+abs(j-1.5)*.005),(.023,.045,.055),'Skin','Hands',.009,segments=2)
    oval('Thumb',(s*.250,-.043,.719),(.022,.025,.040),'Skin','Hands',12,8,smooth=True)
box('SleeveRepair',(-.299,-.039,.984),(.025,.085,.042),'TrouserPatch','Shirt',.003)

# Trouser legs include knee and ankle shape; cuffs don't resemble rigid armor.
loft('TrouserHip',[(.655,.148,.099,0,0),(.724,.172,.107,0,0),(.783,.174,.108,0,0)],'Trousers','Trousers',n=20,power=.75)
for s in [-1,1]:
    x=s*.092
    leg=loft('TrouserLeg',[(.204,.068,.069,.009,x),(.256,.073,.075,.006,x),(.354,.074,.078,.012,x),
        (.427,.080,.085,-.007,x),(.47,.082,.086,-.011,x),(.542,.084,.087,.005,x),(.675,.087,.10,.005,x),(.742,.085,.10,.004,x)],'Trousers','Trousers',n=14,power=.85)
    loft('TrouserCuff',[(.219,.078,.08,.006,x),(.23,.081,.085,.006,x),(.268,.081,.085,.006,x),(.279,.076,.080,.006,x)],'TrouserCuff','Trousers',n=14,power=.76)
    # Cargo pocket is shallow and follows the outside of the thigh.
    box('CargoPocket',(s*.158,-.018,.563),(.050,.121,.131),'TrouserPatch','Trousers',.008,rot=(0,0,-s*.12))
    box('CargoFlap',(s*.164,-.021,.625),(.052,.127,.029),'Trousers','Trousers',.004)
    loft('BootShaft',[(.106,.066,.071,.005,x),(.172,.065,.073,.003,x),(.234,.065,.071,.003,x)],'Boot','Boots',n=12,power=.8)
    loft('BootFoot',[(.032,.08,.126,-.042,x),(.061,.085,.132,-.046,x),(.096,.082,.128,-.047,x),
        (.121,.072,.112,-.033,x),(.161,.056,.072,.007,x)],'BootToe','Boots',n=16,power=.65)
    loft('BootSole',[(0,.076,.121,-.043,x),(.012,.085,.132,-.045,x),(.035,.085,.132,-.045,x),(.040,.080,.127,-.044,x)],'Sole','Boots',n=16,power=.67)
    # Three broad laces, no fine noise.
    for z,y in [(.109,-.13),(.132,-.098),(.152,-.074)]:
        box('BootLace',(x,y,z),(.073,.011,.009),'Leather','Boots',.002)
bpy.context.view_layer.update()
patch('KneeRepair',[(.052,.395),(.13,.395),(.13,.478),(.052,.478)],leg,'Repair','Trousers',.006)

# Belt and equipment retain their own parts, including shoulder straps.
loft('Belt',[(.743,.181,.116,0,0),(.792,.181,.116,0,0)],'Leather','Belt',n=24,power=.72)
box('BeltBuckle',(0,-.121,.768),(.054,.015,.043),'Steel','Belt',.004)
box('BuckleOpening',(0,-.130,.768),(.034,.004,.025),'BuckleInset','Belt',.001)
for x in [-.11,.11]:box('BeltLoop',(x,-.118,.769),(.019,.016,.067),'Trousers','Trousers',.003)
box('Pouch',(-.125,-.132,.724),(.100,.067,.114),'Leather','BeltPouch',.012)
box('PouchFlap',(-.125,-.171,.762),(.108,.012,.055),'Boot','BeltPouch',.007)
box('PouchClasp',(-.125,-.181,.735),(.023,.009,.026),'Steel','BeltPouch',.002)

box('PackBody',(0,.167,1.010),(.29,.157,.326),'Bag','Backpack',.033,segments=2)
box('PackFlap',(0,.255,1.09),(.279,.031,.182),'BagFlap','Backpack',.021,segments=2)
box('PackFrontPocket',(0,.255,.898),(.24,.030,.09),'Bag','Backpack',.01)
box('PackFastener',(0,.274,.984),(.031,.014,.133),'Leather','Backpack',.004)
box('PackBuckle',(0,.285,1.016),(.045,.011,.031),'Steel','Backpack',.003)
box('PackRepair',(.065,.277,1.079),(.066,.008,.063),'Repair','Backpack',.003,rot=(0,.26,0))
for s in [-1,1]:
    box('PackSidePocket',(s*.147,.171,.93),(.045,.126,.095),'BagFlap','Backpack',.01)
    ribbon('ShoulderStrap',[(s*.127,-.116,.959),(s*.14,-.115,1.055),(s*.137,-.104,1.132),
        (s*.112,-.056,1.175),(s*.107,.026,1.181),(s*.112,.105,1.149),(s*.118,.153,1.087)],.032,'Leather','Backpack')
    box('StrapBuckle',(s*.14,-.125,1.042),(.041,.010,.029),'Steel','Backpack',.003)
    box('StrapInset',(s*.14,-.131,1.042),(.026,.003,.015),'Leather','Backpack',.001)
    tube('PackHandle',(s*.05,.164,1.17),(s*.037,.164,1.21),.009,.009,'Leather','Backpack',8)
tube('PackHandle',(-.037,.164,1.21),(.037,.164,1.21),.009,.009,'Leather','Backpack',8)

# Watch on the character's left forearm, dial faces forward for quarter-view readability.
box('WatchStrap',(.286,-.01,.807),(.104,.107,.029),'Leather','Watch',.012)
tube('WatchCase',(.286,-.063,.807),(.286,-.078,.807),.038,.038,'Steel','Watch',16)
tube('WatchDial',(.286,-.079,.807),(.286,-.081,.807),.031,.031,'WatchDial','Watch',16)
for i in range(12):
    a=math.tau*i/12; dx,dz=math.sin(a),math.cos(a)
    tube('WatchTick',(.286+dx*.024,-.083,.807+dz*.024),(.286+dx*.028,-.083,.807+dz*.028),.0014,.0014,'WatchInk','Watch',5)
tube('WatchMinute',(.286,-.084,.807),(.274,-.084,.831),.0019,.0019,'WatchInk','Watch',6)
tube('WatchHour',(.286,-.085,.807),(.300,-.085,.819),.0023,.0023,'WatchInk','Watch',6)

# Merge only within slots; every piece remains individually editable in Edit Mode.
parts=['Head','FaceDetails','Hair','Cap','Body','Hands','Shirt','Trousers','Boots','Belt','BeltPouch','Backpack','Watch']
for part in parts:
    objs=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.get('part')==part]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
    if len(objs)>1:bpy.ops.object.join()
    obj=bpy.context.object; obj.name='ThreeHead_'+part
    obj['equipment_slot']=part; obj['stage']='Static model; rig/animation pending'
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
character=[o for o in bpy.context.scene.objects if o.type=='MESH']
scene=bpy.context.scene
scene['proportion']='3.0 cap-inclusive head units; chin 1.2, crown 1.8, floor 0'
scene['stage']='Static appearance approval; no animation or Unity hookup'
scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.cycles.use_denoising=True
scene.world.use_nodes=True
background=scene.world.node_tree.nodes.get('Background')
background.inputs[0].default_value=(.065,.069,.075,1); background.inputs[1].default_value=.45
scene.view_settings.view_transform='AgX'; scene.view_settings.look='AgX - Medium High Contrast'; scene.view_settings.exposure=0
def light(pos,power,size):
    bpy.ops.object.light_add(type='AREA',location=pos); o=bpy.context.object
    o.data.energy=power; o.data.shape='DISK'; o.data.size=size
    o.rotation_euler=(Vector((0,0,.9))-o.location).to_track_quat('-Z','Y').to_euler()
light((-3,-4,4.5),390,4); light((3,-4,2.4),150,3); light((1,3,4),230,3)
bpy.ops.object.camera_add(); scene.camera=bpy.context.object; scene.camera.data.type='ORTHO'; scene.camera.data.ortho_scale=2.04
def camera(pos):
    scene.camera.location=pos
    scene.camera.rotation_euler=(Vector((0,0,.9))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
camera((0,-6,1.20))
scene.render.resolution_x=880; scene.render.resolution_y=1100; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
bpy.ops.object.select_all(action='DESELECT')
for o in character:o.select_set(True)
bpy.context.view_layer.objects.active=character[0]
bpy.ops.export_scene.fbx(filepath=str(OUT/'ThreeHeadSurvivor.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
bpy.ops.export_scene.gltf(filepath=str(OUT/'ThreeHeadSurvivor.glb'),use_selection=True,export_format='GLB')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ThreeHeadSurvivor.blend'))
for name,pos in [('Front',(0,-6,1.20)),('Quarter',(3,-6,1.65)),('Side',(6,-.15,1.20)),('Back',(-3,6,1.65))]:
    camera(pos); scene.render.filepath=str(OUT/(name+'.png')); bpy.ops.render.render(write_still=True)
print('THREE_HEAD_SURVIVOR_CREATED',OUT,'parts',len(character),'vertices',sum(len(o.data.vertices) for o in character))
