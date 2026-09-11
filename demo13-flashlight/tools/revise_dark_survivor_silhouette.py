"""Second static shape pass; keep v1 and production assets intact."""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivorReview/Stage01_Silhouette'
SOURCE=BASE/'DarkSurvivor_Stage01.blend'
OUT=BASE/'Revision02';OUT.mkdir(parents=True,exist_ok=True)
signature=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
parts=[o for o in scene.objects if o.name.startswith('Study_') and o.type=='MESH']
originals=[(o,o.data.copy(),o.matrix_world.copy()) for o in parts]
face_signature=hashlib.sha256(repr([tuple(v.co) for v in bpy.data.objects['Study_FaceDetails'].data.vertices]).encode()).hexdigest()

def edit_world(o,func):
    inv=o.matrix_world.inverted()
    for v in o.data.vertices:v.co=inv@func(o.matrix_world@v.co)
    o.data.update()

def loft_replace(name,rows,n=20,power=.85):
    o=bpy.data.objects[name];materials=list(o.data.materials);vs=[]
    for z,rx,ry,cy,cx in rows:
        for i in range(n):
            a=math.tau*i/n;s,c=math.sin(a),-math.cos(a)
            vs.append((cx+rx*math.copysign(abs(s)**power,s),cy+ry*math.copysign(abs(c)**power,c),z))
    fs=[tuple(reversed(range(n)))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(rows)-1) for i in range(n)]+[tuple(range((len(rows)-1)*n,len(rows)*n))]
    d=bpy.data.meshes.new(name+'_Reshaped');d.from_pydata(vs,[],fs);d.update();o.data=d;o.matrix_world.identity()
    for m in materials:d.materials.append(m)
    return o

# Lower the cap crown without altering the face/skull. Preserve a plausible brim.
def cap(p):
    x,y,z=p
    if z>1.52:z=1.52+(z-1.52)*.85
    if y<-.23 and z<1.56:
        t=max(0,min(1,(-y-.23)/.18));y-=.026*t;z-=.010*t*(1-min(1,abs(x)/.30))
    return Vector((x,y,z))
edit_world(bpy.data.objects['Study_Cap'],cap)

# Waist taper and a slightly shorter hem; broad changes only, no micro wrinkles.
loft_replace('Study_JacketBody',[(.822,.177,.117,0,0),(.85,.179,.118,0,0),(.94,.171,.111,0,0),(1.04,.188,.121,0,0),(1.105,.197,.116,0,0),(1.15,.177,.102,0,0),(1.18,.119,.081,0,0),(1.19,.075,.065,0,0)],24,.8)
edit_world(bpy.data.objects['Study_JacketOpening'],lambda p:Vector((p.x,p.y,max(.824,p.z))))
for s in [-1,1]:
    loft_replace('Study_Sleeve_'+str(s),[(1.15,.044,.062,0,s*.171),(1.128,.064,.078,0,s*.197),(1.098,.08,.085,0,s*.22),(1.052,.079,.084,.002,s*.241),(.994,.070,.076,.003,s*.265),(.947,.061,.068,.001,s*.277)],20,.94)
    loft_replace('Study_RolledSleeve_'+str(s),[(.933,.067,.074,.001,s*.28),(.94,.076,.082,.001,s*.278),(.973,.077,.083,.002,s*.272),(.982,.069,.076,.002,s*.27)],20,.9)
    loft_replace('Study_Forearm_'+str(s),[(.748,.043,.045,-.101,s*.315),(.785,.047,.049,-.088,s*.312),(.853,.05,.052,-.056,s*.302),(.911,.055,.055,-.019,s*.289),(.957,.054,.054,.002,s*.276),(.981,.048,.05,.004,s*.272)],20,1)
    x=s*.103
    loft_replace('Study_TrouserLeg_'+str(s),[(.18,.068,.073,.006,x),(.265,.069,.076,.006,x),(.32,.073,.079,.007,x+s*.003),(.395,.076,.081,-.001,x+s*.005),(.455,.078,.083,-.010,x+s*.005),(.485,.081,.086,-.010,x+s*.004),(.57,.094,.10,.002,x+s*.004),(.66,.101,.11,.005,x),(.735,.096,.109,.004,x),(.785,.092,.108,.004,x)],20,.86)
    # Less rigid trouser turn-up follows a slightly slimmer ankle.
    cuff=bpy.data.objects['Study_TrouserCuff_'+str(s)]
    edit_world(cuff,lambda p:Vector((x+(p.x-x)*.91,p.y*.94,p.z+.003*math.sin(p.x*24))))
    strap=bpy.data.objects['Study_PackStrap_'+str(s)]
    edit_world(strap,lambda p:Vector((p.x*(.91 if p.z<1.0 else 1),p.y,p.z)))
def hand(p):
    s=1 if p.x>0 else -1
    return p+Vector((-s*.009,-.082,0))
edit_world(bpy.data.objects['Study_Hands'],hand)

loft_replace('Study_Hips',[(.67,.16,.10,0,0),(.735,.18,.114,0,0),(.805,.18,.116,0,0),(.846,.168,.107,0,0)],20,.78)

# A filled cloth bag: rounded lower mass, narrowed/slumped upper edge.
loft_replace('Study_Backpack',[(.85,.097,.059,.193,0),(.868,.129,.079,.194,0),(.93,.145,.095,.194,0),(1.045,.14,.091,.188,0),(1.108,.13,.076,.18,0),(1.15,.111,.060,.17,0)],20,.7)
def flap(p):
    x,y,z=p;t=max(0,min(1,(z-1.0)/.16))
    x*=1-.14*t;y-=.032*t+.015
    z-=.008+.018*t+.010*(1-min(1,abs(x)/.143))
    y+=.008*(1-min(1,(x/.143)**2))
    return Vector((x,y,z))
edit_world(bpy.data.objects['Study_BackpackFlap'],flap)
edit_world(bpy.data.objects['Study_BackpackClosure'],lambda p:Vector((p.x,p.y-.024,p.z-.012)))
for s in [-1,1]:
    # Attach the rear ends of the straps to the lowered bag crown.
    def strap_back(p):
        if p.y>.10:
            p.z-=.022*min(1,(p.y-.10)/.09);p.y-=.015*min(1,(p.y-.10)/.09)
        return p
    edit_world(bpy.data.objects['Study_PackStrap_'+str(s)],strap_back)

assert face_signature==hashlib.sha256(repr([tuple(v.co) for v in bpy.data.objects['Study_FaceDetails'].data.vertices]).encode()).hexdigest()
for o in parts:
    o['revision']='02: cap, shoulders, relaxed arms, tapered clothes and soft pack'
    for v in o.data.vertices:assert all(math.isfinite(c) for c in v.co)
bpy.context.view_layer.update()
zs=[(o.matrix_world@v.co).z for o in parts for v in o.data.vertices]
headparts=[o for o in parts if o.name in ['Study_Head','Study_Cap','Study_Hair']]
hz=[(o.matrix_world@v.co).z for o in headparts for v in o.data.vertices]
ratio=(max(zs)-min(zs))/(max(hz)-min(hz))
scene['head_units']=f'{ratio:.2f} cap-inclusive after lowering crown; unchanged anatomical head/body'
scene['stage']='01 silhouette revision 02; static modular study, no animation changes'
scene.cycles.samples=32
cam=scene.camera
def camera(pos,target,scale):
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
camera((3,-6,2.05),(0,0,.9),2.12)
scene.render.resolution_x=800;scene.render.resolution_y=1000;scene.render.filepath=str(OUT/'Quarter.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'DarkSurvivor_Stage01_Rev02.blend'))
bpy.ops.render.render(write_still=True)

def duplicate(objects,x,angle,name):
    parent=bpy.data.objects.new(name,None);scene.collection.objects.link(parent);dupes=[parent]
    for src,data,matrix in objects:
        o=src.copy();o.data=data;scene.collection.objects.link(o);o.parent=parent;o.matrix_local=matrix;dupes.append(o)
    parent.location.x=x;parent.rotation_euler.z=angle
    return dupes
current=[(o,o.data,o.matrix_world.copy()) for o in parts]
views=[]
for x,angle in [(-1.15,0),(0,-math.pi/2),(1.15,math.pi)]:views+=duplicate(current,x,angle,'RevisionView')
for o in parts:o.hide_render=True;o.hide_set(True)
camera((0,-8,1.28),(0,0,.90),3.8)
scene.render.resolution_x=1680;scene.render.resolution_y=1000;scene.render.filepath=str(OUT/'FrontSideBack.png')
bpy.ops.render.render(write_still=True)
for o in views:bpy.data.objects.remove(o,do_unlink=True)
# Compare at the exact same angle, common scale and shared studio lighting.
for x,items in [(-.58,originals),(.58,current)]:
    dupes=duplicate(items,x,math.radians(26.565),'Comparison')
    for o in dupes:o.hide_render=False;o.hide_set(False)
camera((0,-8,2.27),(0,0,.9),2.52)
scene.render.resolution_x=1200;scene.render.resolution_y=1000;scene.render.filepath=str(OUT/'BeforeAfter.png')
bpy.ops.render.render(write_still=True)
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==signature
(OUT/'ShapeCheck.json').write_text(json.dumps({'source_unchanged':True,'face_unchanged':True,'static_review_only':True,'height_m':max(zs)-min(zs),'head_units_with_cap':ratio,'anatomical_head_and_leg_lengths_unchanged':True,'separate_parts':len(parts),'comparison':'left revision 01, right revision 02; same scale, camera and lighting'},indent=2))
print('SILHOUETTE_REV02_READY',str(OUT),flush=True)
