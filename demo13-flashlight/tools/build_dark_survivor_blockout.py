"""Stage 1: modular, static clay silhouette study. Does not replace the player."""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Assets/ChibiSurvivor/Player/CompactSurvivor_Combat.blend'
OUT=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivorReview/Stage01_Silhouette'
OUT.mkdir(parents=True,exist_ok=True)
source_hash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
KEEP={'Compact_Head','Compact_FaceDetails','Compact_Cap','Compact_Hair','Compact_Hands'}
for o in list(bpy.data.objects):
    if o.name not in KEEP:bpy.data.objects.remove(o,do_unlink=True)
for a in list(bpy.data.actions):bpy.data.actions.remove(a)

def material(name,value):
    m=bpy.data.materials.new(name);m.diffuse_color=(value,value,value,1);m.use_nodes=True
    p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(value,value,value,1);p.inputs['Roughness'].default_value=.85
    return m
M={k:material('Clay_'+k,v) for k,v in {'skin':.40,'cloth':.26,'edge':.21,'dark':.12,'face':.085,'eye':.31,'floor':.055}.items()}
modules=[]
for o in list(scene.objects):
    # Use unposed mesh coordinates, not an attack frame or an evaluated armature.
    o.parent=None;o.animation_data_clear()
    for mod in list(o.modifiers):o.modifiers.remove(mod)
    for v in o.data.vertices:
        if o.name=='Compact_Hands':
            s=1 if v.co.x>0 else -1
            v.co=Vector((s*.324,-.022,.714))+(v.co-Vector((s*.296,-.022,.536)))*1.06
        else:v.co.z+=.2352
    original_mats=list(o.data.materials)
    o.data.materials.clear()
    for m in original_mats:
        n=m.name.lower()
        key='cloth' if o.name=='Compact_Cap' else 'dark' if o.name=='Compact_Hair' else 'skin'
        if o.name=='Compact_FaceDetails':key='eye' if any(t in n for t in ['white','ivory','patch']) else 'face'
        o.data.materials.append(M[key])
    o.name=o.name.replace('Compact_','Study_');modules.append(o)
    o['stage']='01 silhouette; original face retained, expression work deferred'

def mesh(name,vs,fs,key,slot):
    d=bpy.data.meshes.new(name);d.from_pydata(vs,[],fs);d.update()
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);d.materials.append(M[key])
    o['equipment_slot']=slot;o['stage']='01 silhouette, static unrigged review';modules.append(o);return o

def box(name,pos,size,key,slot,bevel=.01):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        m=o.modifiers.new('Broad edges','BEVEL');m.width=bevel;m.segments=1;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=m.name)
    o.data.materials.append(M[key]);o['equipment_slot']=slot;modules.append(o);return o

def loft(name,rows,key,slot,n=16,power=.8):
    vs=[]
    for z,rx,ry,cy,cx in rows:
        for i in range(n):
            t=math.tau*i/n;s,c=math.sin(t),-math.cos(t)
            vs.append((cx+rx*math.copysign(abs(s)**power,s),cy+ry*math.copysign(abs(c)**power,c),z))
    fs=[tuple(reversed(range(n)))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(rows)-1) for i in range(n)]+[tuple(range((len(rows)-1)*n,len(rows)*n))]
    return mesh(name,vs,fs,key,slot)

def tube(name,a,b,r1,r2,key,slot,n=12):
    a,b=Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=n,radius1=r1,radius2=r2,depth=(b-a).length,location=(a+b)/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();o.data.materials.append(M[key]);o['equipment_slot']=slot;modules.append(o);return o

# Structural silhouette controls. Heights in metres, cap-inclusive head unit .6.
WAIST=.78;CHIN=1.20;HEIGHT=1.80
loft('Study_Neck',[(1.14,.065,.06,0,0),(1.23,.065,.06,0,0)],'skin','Body')
loft('Study_JacketBody',[(.798,.19,.12,0,0),(.845,.196,.124,0,0),(1.025,.181,.116,0,0),(1.13,.198,.113,0,0),(1.175,.153,.09,0,0),(1.19,.075,.065,0,0)],'cloth','Jacket',24,.75)
# One opening and two broad lapels: silhouette-scale construction, no buttons/pockets.
mesh('Study_JacketOpening',[(-.025,-.126,.80),(.025,-.126,.80),(.031,-.122,1.10),(0,-.10,1.18),(-.031,-.122,1.10)],[(0,1,2,3,4)],'dark','Jacket')
for s in [-1,1]:
    mesh('Study_Lapel_'+str(s),[(s*.028,-.087,1.184),(s*.086,-.086,1.18),(s*.121,-.121,1.10),(s*.070,-.13,1.13),(s*.032,-.133,1.067)],[(0,1,2,3,4)],'edge','Jacket')
    loft('Study_Sleeve_'+str(s),[(1.145,.057,.077,0,s*.178),(1.11,.084,.087,0,s*.216),(1.028,.075,.082,0,s*.25),(.982,.067,.074,-.003,s*.272)],'cloth','Jacket',16,.88)
    cuff=box('Study_RolledSleeve_'+str(s),(s*.272,-.003,.982),(.151,.162,.055),'edge','Jacket',.009);cuff.rotation_euler.y=-s*.15
    loft('Study_Forearm_'+str(s),[(.749,.043,.046,-.019,s*.318),(.815,.047,.05,-.014,s*.309),(.932,.056,.054,-.006,s*.284),(.995,.054,.052,-.003,s*.273)],'skin','Body',16,1)
    x=s*.103
    loft('Study_TrouserLeg_'+str(s),[(.18,.073,.075,.006,x),(.25,.076,.081,.006,x),(.39,.079,.084,-.004,x),(.46,.082,.088,-.009,x),(.58,.09,.096,.003,x),(.72,.094,.104,.004,x),(.785,.092,.108,.004,x)],'dark','Trousers',16,.8)
    loft('Study_TrouserCuff_'+str(s),[(.188,.081,.083,.006,x),(.202,.087,.091,.006,x),(.247,.086,.091,.006,x),(.26,.080,.084,.006,x)],'edge','Trousers',16,.75)
    loft('Study_BootShaft_'+str(s),[(.085,.073,.08,.005,x),(.213,.07,.079,.004,x)],'edge','Boots',12,.8)
    loft('Study_Boot_'+str(s),[(.028,.088,.139,-.05,x),(.06,.09,.144,-.05,x),(.11,.085,.134,-.042,x),(.16,.068,.087,.004,x)],'edge','Boots',16,.65)
    loft('Study_Sole_'+str(s),[(0,.086,.137,-.05,x),(.012,.092,.145,-.05,x),(.036,.092,.145,-.05,x)],'dark','Boots',16,.65)
loft('Study_Hips',[(.67,.16,.10,0,0),(.735,.18,.114,0,0),(.805,.185,.117,0,0)],'dark','Trousers',20,.75)
loft('Study_Belt',[(.763,.191,.126,0,0),(.802,.191,.126,0,0)],'edge','Belt',24,.75)
box('Study_BeltBuckle',(0,-.132,.782),(.052,.016,.037),'cloth','Belt',.004)

# Neckwear and small square pack are separable large forms, not surface detail.
loft('Study_ScarfWrap',[(1.154,.077,.07,0,0),(1.188,.083,.077,0,0),(1.205,.078,.073,0,0)],'edge','Scarf',16,.8)
scarf=mesh('Study_ScarfFront',[(-.08,-.076,1.189),(.082,-.079,1.184),(.10,-.112,1.135),(.017,-.147,1.034),(-.085,-.12,1.118),(0,-.146,1.143)],[(0,1,5),(1,2,5),(2,3,5),(3,4,5),(4,0,5)],'edge','Scarf')
mod=scarf.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.008
box('Study_Backpack',(0,.198,1.015),(.295,.183,.315),'edge','Backpack',.026)
box('Study_BackpackFlap',(0,.297,1.072),(.286,.025,.162),'cloth','Backpack',.018)
box('Study_BackpackClosure',(0,.315,1.00),(.037,.012,.135),'dark','Backpack',.004)
for s in [-1,1]:
    points=[(s*.14,-.14,.862),(s*.16,-.127,1.072),(s*.142,-.088,1.158),(s*.121,.01,1.187),(s*.137,.122,1.151),(s*.14,.189,1.09)]
    vs=[]
    for x,y,z in points:vs.extend([(x-.018,y,z),(x+.018,y,z)])
    strap=mesh('Study_PackStrap_'+str(s),vs,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(points)-1)],'dark','Backpack')
    mod=strap.modifiers.new('Strap thickness','SOLIDIFY');mod.thickness=.009
# Lantern is only a solid proxy in stage 1, with no glass or emissive shader.
box('Study_LanternBlock',(-.215,-.074,.715),(.085,.084,.14),'edge','Lantern',.008)
tube('Study_LanternLoop',(-.215,-.074,.786),(-.215,-.074,.815),.012,.012,'dark','Lantern',8)

for o in modules:
    o['review_only']=True
    if o.type=='MESH':
        for v in o.data.vertices:assert all(math.isfinite(c) for c in v.co)
scene.world=bpy.data.worlds.new('Neutral studio');scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.16,.16,.16,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.4
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=0
def light(name,pos,power,size):
    bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,.9))-o.location).to_track_quat('-Z','Y').to_euler()
light('Key',(-3,-4,5),450,4);light('Fill',(4,-2,3),230,4);light('Rim',(1,3,4),350,3)
bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;ground.name='StudioFloor';ground.data.materials.append(M['floor']);ground.location.z=-.008
bpy.ops.object.camera_add(location=(3,-6,2.1));cam=bpy.context.object;cam.name='ReviewCamera';scene.camera=cam
cam.data.type='ORTHO';cam.data.ortho_scale=2.12
def camera(pos,target,scale):
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
scene.render.image_settings.file_format='PNG';scene.render.resolution_percentage=100
scene.render.resolution_x=800;scene.render.resolution_y=1000
camera((3,-6,2.05),(0,0,.9),2.12)
scene['stage']='01 silhouette only; approval required before face, palette and texture work'
scene['head_units']='3.0 measured including cap; head retained from approved character'
scene['source_asset']=str(SOURCE)
bpy.ops.object.select_all(action='DESELECT')
for o in modules:o.select_set(True)
bpy.context.view_layer.objects.active=modules[0]
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'DarkSurvivor_Stage01.blend'))
scene.render.filepath=str(OUT/'Quarter.png');bpy.ops.render.render(write_still=True)

# Native 3D turntable sheet, all views at equal scale; no image compositing.
for index,(x,angle) in enumerate([(-1.15,0),(0,math.radians(-90)),(1.15,math.pi)]):
    empty=bpy.data.objects.new('View_'+str(index),None);scene.collection.objects.link(empty)
    for src in modules:
        o=src.copy();o.data=src.data;scene.collection.objects.link(o);o.parent=empty
    empty.location.x=x;empty.rotation_euler.z=angle
for o in modules:o.hide_render=True;o.hide_set(True)
camera((0,-8,1.28),(0,0,.90),3.8)
scene.render.resolution_x=1680;scene.render.resolution_y=1000
scene.render.filepath=str(OUT/'FrontSideBack.png');bpy.ops.render.render(write_still=True)
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==source_hash,'Production source changed'
report={'stage':1,'static_review_only':True,'source_unchanged':True,'total_height_m':HEIGHT,'cap_inclusive_head_height_m':.6,'head_units':3.0,'waist_height_m':WAIST,'chin_height_m':CHIN,'separate_slots':sorted(set(o.get('equipment_slot','') for o in modules)),'animation_integration':False,'face_redesign':False,'final_materials':False}
(OUT/'ShapeCheck.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('DARK_SURVIVOR_STAGE01_READY',str(OUT),flush=True)
