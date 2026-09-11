"""Head-area silhouette refinement. Body and facial geometry are immutable."""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector,Matrix

ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivorReview/Stage01_Silhouette'
SOURCE=BASE/'Revision02/DarkSurvivor_Stage01_Rev02.blend'
OUT=BASE/'Revision03_Head';OUT.mkdir(parents=True,exist_ok=True)
sourcehash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene
parts=[o for o in scene.objects if o.type=='MESH' and o.name.startswith('Study_')]
originals=[(o,o.data.copy(),o.matrix_world.copy(),[(m.type,getattr(m,'thickness',None)) for m in o.modifiers]) for o in parts]
EDIT={'Study_Cap','Study_Hair','Study_ScarfWrap','Study_ScarfFront'}
def signature(o):
    return hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons],list(map(tuple,o.matrix_world)))).encode()).hexdigest()
unchanged={o.name:signature(o) for o in parts if o.name not in EDIT}

def replace(name,vs,fs):
    o=bpy.data.objects[name];mats=list(o.data.materials)
    d=bpy.data.meshes.new(name+'_Rev03');d.from_pydata(vs,[],fs);d.update();o.data=d;o.matrix_world=Matrix.Identity(4)
    for m in mats:d.materials.append(m)
    for mod in list(o.modifiers):o.modifiers.remove(mod)
    return o

# A fitted six-panel cap shape, with a forward-biased crown instead of a hemisphere.
N=32
rows=[(1.529,.287,.229,.031),(1.58,.287,.229,.032),(1.643,.26,.213,.035),(1.687,.207,.181,.03),(1.708,.129,.123,.019),(1.714,.022,.024,.013)]
vs=[]
for row,(z,rx,ry,cy) in enumerate(rows):
    for i in range(N):
        a=math.tau*i/N;c,s=math.cos(a),math.sin(a)
        # Rear edge slightly lower, avoiding a horizontal helmet rim.
        zz=z-(.021*(1-c)/2)*(1-row/(len(rows)-1))
        vs.append((rx*s,cy-ry*c,zz))
fs=[]
for j in range(len(rows)-1):
    for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
fs.append(tuple(range((len(rows)-1)*N,len(rows)*N)))
# Narrow folded lower rim, open underneath.
start=len(vs)
for i in range(N):
    p=Vector(vs[i]);p.x*=.98;p.y=.031+(p.y-.031)*.98;p.z+=.005;vs.append(p)
for i in range(N):fs.append((i,start+i,start+(i+1)%N,(i+1)%N))
# Curved brim, three rows across its depth, thin at the leading edge.
S=21;start=len(vs)
for layer in [0,1]:
    for j in range(3):
        t=j/2
        for i in range(S):
            u=-1+2*i/(S-1);x=u*.26
            c=math.sqrt(max(0,1-(x/.287)**2))
            back=.031-.229*c+.004;front=-.402+.119*u*u
            inner_z=1.529-.021*(1-c)/2+.001
            front_z=1.488-.025*u*u
            z=inner_z*(1-t)+front_z*t+.006*math.sin(math.pi*t)-(.008 if layer else 0)
            vs.append((x,back*(1-t)+front*t,z))
for layer in range(2):
    for j in range(2):
        for i in range(S-1):
            a=start+layer*3*S+j*S+i;q=(a,a+1,a+1+S,a+S);fs.append(q if layer==0 else tuple(reversed(q)))
for i in range(S-1):
    a=start+2*S+i;fs.append((a,a+3*S,a+1+3*S,a+1))
for side in [0,S-1]:
    for j in range(2):
        a=start+j*S+side;fs.append((a,a+S,a+S+3*S,a+3*S))
# Plain front patch projected onto the actual crown, not a guessed flat plane.
cap=replace('Study_Cap',vs,fs);bpy.context.view_layer.update()
start=len(vs)
patch=[]
for x,z in [(-.061,1.603),(.061,1.603),(.058,1.669),(-.058,1.669)]:
    hit,p,n,_=cap.ray_cast(Vector((x,-1,z)),Vector((0,1,0)));assert hit
    patch.append(p+Vector((0,-.003,0)))
vs.extend(patch);vs.extend([p+Vector((0,-.004,0)) for p in patch])
fs.extend([tuple(start+i for i in q) for q in [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]])
cap=replace('Study_Cap',vs,fs)

# Hair hugs the scalp, with a few broad, offset ends rather than a straight shelf.
head=bpy.data.objects['Study_Head'];d=head.data
scalp_data=bpy.data.meshes.new('ScalpGuide');scalp_data.from_pydata([v.co for v in d.vertices[:480]],[],[tuple(p.vertices) for p in d.polygons if max(p.vertices)<480]);scalp_data.update()
scalp=bpy.data.objects.new('ScalpGuide',scalp_data);scene.collection.objects.link(scalp);bpy.context.view_layer.update()
vs=[];fs=[];columns=65;levels=9
for row in range(levels):
    t=row/(levels-1)
    for col in range(columns):
        u=col/(columns-1);a=math.pi*.27+math.pi*1.46*u
        # Five broad locks, with small asymmetric tips; not many fine spikes.
        wave=.018*math.cos(u*math.tau*5+.4)+.007*math.sin(u*math.tau*2)
        bottom=1.299+.070*abs(2*u-1)**1.4+wave
        z=bottom+(1.585-bottom)*t
        outward=Vector((math.sin(a),-math.cos(a),0))
        hit,p,n,_=scalp.ray_cast(outward*2+Vector((0,0,z)),-outward)
        assert hit,('Scalp miss',u,z)
        offset=.006+.006*math.sin(math.pi*t)
        hp=p+outward*offset
        if z>1.515:
            cap_hit,cp,_,_=cap.ray_cast(outward*2+Vector((0,0,z)),-outward)
            if cap_hit and hp.dot(outward)>cp.dot(outward)-.006:
                hp-=outward*(hp.dot(outward)-cp.dot(outward)+.006)
        vs.append(hp)
for j in range(levels-1):
    for i in range(columns-1):fs.append((j*columns+i,j*columns+i+1,(j+1)*columns+i+1,(j+1)*columns+i))
# Two quiet forehead tufts connect the temples to the brim; facial features unchanged.
for outline in [[(-.237,1.502),(-.197,1.548),(-.10,1.555),(-.142,1.504),(-.209,1.458)],[(.11,1.555),(.206,1.548),(.244,1.497),(.222,1.477),(.17,1.511)]]:
    start=len(vs)
    for x,z in outline:
        hit,p,n,_=scalp.ray_cast(Vector((x,-1,z)),Vector((0,1,0)));assert hit
        hp=p+Vector((0,-.009,0))
        if z>1.526:
            cap_hit,cp,_,_=cap.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
            if cap_hit:hp.y=max(hp.y,cp.y+.006)
        vs.append(hp)
    fs.append(tuple(range(start,len(vs))))
hair=replace('Study_Hair',vs,fs);solid=hair.modifiers.new('Hair edge thickness','SOLIDIFY');solid.thickness=.004;solid.offset=-1
bpy.data.objects.remove(scalp,do_unlink=True);bpy.data.meshes.remove(scalp_data)

# A short draped neckerchief, curved against the jacket instead of a triangular plate.
vs=[];fs=[];N=32
for j,(z,rx,ry) in enumerate([(1.155,.078,.073),(1.166,.088,.083),(1.188,.082,.076),(1.202,.074,.067)]):
    for i in range(N):
        a=math.tau*i/N
        vs.append((rx*math.sin(a),-ry*math.cos(a),z+.003*math.sin(2*a+.5)))
for j in range(3):
    for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
wrap=replace('Study_ScarfWrap',vs,fs);mod=wrap.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.005
jacket=bpy.data.objects['Study_JacketBody'];vs=[];fs=[];cols=25;rows=11
for j in range(rows):
    t=j/(rows-1)
    for i in range(cols):
        u=-1+2*i/(cols-1);x=.096*u
        top=1.188-.026*abs(u);bottom=1.09+.052*abs(u)**.85+.005*u
        z=top*(1-t)+bottom*t
        hit,p,n,_=jacket.ray_cast(Vector((x,-1,z)),Vector((0,1,0)));assert hit,('Scarf projection',x,z)
        y=p.y-.009-.014*math.sin(math.pi*t)-.004*math.sin(u*math.pi*2+.5)*math.sin(math.pi*t)
        vs.append((x,y,z))
for j in range(rows-1):
    for i in range(cols-1):fs.append((j*cols+i,j*cols+i+1,(j+1)*cols+i+1,(j+1)*cols+i))
scarf=replace('Study_ScarfFront',vs,fs);mod=scarf.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.006;mod.offset=-.2

assert all(signature(bpy.data.objects[n])==sig for n,sig in unchanged.items()),'Body or face changed'
scene['stage']='01 revision 03: cap, hair and neckerchief only; face and body unchanged'
body_z=[(o.matrix_world@v.co).z for o in parts for v in o.data.vertices]
head_z=[(o.matrix_world@v.co).z for o in parts if o.name in ['Study_Head','Study_Hair','Study_Cap'] for v in o.data.vertices]
scene['head_units']=f'{(max(body_z)-min(body_z))/(max(head_z)-min(head_z)):.2f} including lowered cap; anatomical head and body unchanged'
scene.cycles.samples=32
cam=scene.camera
def camera(pos,target,scale):
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
camera((3,-6,2.05),(0,0,.9),2.12);scene.render.resolution_x=800;scene.render.resolution_y=1000
scene.render.filepath=str(OUT/'Quarter.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'DarkSurvivor_Stage01_Rev03.blend'))
bpy.ops.render.render(write_still=True)

def dup(items,x,angle,name):
    parent=bpy.data.objects.new(name,None);scene.collection.objects.link(parent);all_objects=[parent]
    for src,data,matrix,mods in items:
        o=src.copy();o.data=data;scene.collection.objects.link(o);o.parent=parent;o.matrix_local=matrix;o.hide_render=False;o.hide_set(False)
        for m in list(o.modifiers):o.modifiers.remove(m)
        for kind,thickness in mods:
            m=o.modifiers.new('Review '+kind,kind)
            if thickness is not None:m.thickness=thickness
        all_objects.append(o)
    parent.location.x=x;parent.rotation_euler.z=angle;return all_objects
current=[(o,o.data,o.matrix_world.copy(),[(m.type,getattr(m,'thickness',None)) for m in o.modifiers]) for o in parts]
for o in parts:o.hide_render=True;o.hide_set(True)
copies=dup(originals,-.40,-math.radians(22),'Before')+dup(current,.40,-math.radians(22),'After')
camera((0,-8,2.4),(0,0,1.42),1.58);scene.render.resolution_x=1440;scene.render.resolution_y=960
scene.render.filepath=str(OUT/'HeadBeforeAfter.png');bpy.ops.render.render(write_still=True)
for o in copies:bpy.data.objects.remove(o,do_unlink=True)
copies=[]
for x,angle in [(-1.15,0),(0,-math.pi/2),(1.15,math.pi)]:copies+=dup(current,x,angle,'Turnaround')
camera((0,-8,1.28),(0,0,.90),3.8);scene.render.resolution_x=1680;scene.render.resolution_y=1000
scene.render.filepath=str(OUT/'FrontSideBack.png');bpy.ops.render.render(write_still=True)
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==sourcehash
(OUT/'ShapeCheck.json').write_text(json.dumps({'source_unchanged':True,'body_and_face_geometry_unchanged':True,'edited_parts':sorted(EDIT),'static_review_only':True,'comparison':'left revision02, right revision03, same camera and scale'},indent=2))
print('HEAD_REV03_READY',str(OUT),flush=True)
