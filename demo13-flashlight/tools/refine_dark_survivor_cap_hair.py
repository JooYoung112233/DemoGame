"""Restore a curved baseball cap and taper temple hair; preserve approved body/scarf."""
import bpy,math,json,hashlib,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivorReview/Stage01_Silhouette'
SOURCE=BASE/'Revision03_Head/DarkSurvivor_Stage01_Rev03.blend'
CAP_SOURCE=BASE/'Revision02/DarkSurvivor_Stage01_Rev02.blend'
OUT=BASE/'Revision04_CapHair';OUT.mkdir(parents=True,exist_ok=True)
source_hash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;parts=[o for o in scene.objects if o.type=='MESH' and o.name.startswith('Study_')]
def signature(o):
    return hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons],list(map(tuple,o.matrix_world)),[(m.type,getattr(m,'thickness',0),getattr(m,'offset',0)) for m in o.modifiers])).encode()).hexdigest()
preserved={o.name:signature(o) for o in parts if o.name not in ['Study_Cap','Study_Hair']}
old=[(o,o.data.copy(),o.matrix_world.copy()) for o in parts]
with bpy.data.libraries.load(str(CAP_SOURCE),link=False) as (src,dst):dst.objects=['Study_Cap']
reference=dst.objects[0];cap=bpy.data.objects['Study_Cap'];cap.data=reference.data.copy();bpy.data.objects.remove(reference,do_unlink=True)
# Retain original rounded brim and reduce only the upper crown very slightly.
for v in cap.data.vertices:
    if v.co.z>1.55:v.co.z=1.55+(v.co.z-1.55)*.96
cap.data.update()

head=bpy.data.objects['Study_Head'];d=head.data
sd=bpy.data.meshes.new('TemporaryScalp');sd.from_pydata([v.co for v in d.vertices[:480]],[],[tuple(p.vertices) for p in d.polygons if max(p.vertices)<480]);sd.update()
scalp=bpy.data.objects.new('TemporaryScalp',sd);scene.collection.objects.link(scalp);bpy.context.view_layer.update()
vs=[];fs=[];C=65;R=11
for row in range(R):
    t=row/(R-1)
    for col in range(C):
        u=col/(C-1)
        # Narrow front ends into pointed sideburns; upper hair remains tucked under cap.
        edge=math.pi*(.35-.05*t)
        angle=edge+(math.tau-2*edge)*u
        bottom=1.286+.112*abs(2*u-1)**2.5+.010*math.cos(math.tau*5*u+.4)
        z=bottom+(1.577-bottom)*t
        out=Vector((math.sin(angle),-math.cos(angle),0))
        hit,p,_,_=scalp.ray_cast(out*2+Vector((0,0,z)),-out);assert hit
        p+=out*(.006+.004*math.sin(math.pi*t))
        if z>1.515:
            hit,cp,_,_=cap.ray_cast(out*2+Vector((0,0,z)),-out)
            if hit and p.dot(out)>cp.dot(out)-.006:p-=out*(p.dot(out)-cp.dot(out)+.006)
        vs.append(p)
for row in range(R-1):
    for i in range(C-1):fs.append((row*C+i,row*C+i+1,(row+1)*C+i+1,(row+1)*C+i))
hair=bpy.data.objects['Study_Hair'];mats=list(hair.data.materials)
mesh=bpy.data.meshes.new('TaperedTempleHair');mesh.from_pydata(vs,[],fs);mesh.update();hair.data=mesh
for m in mats:mesh.materials.append(m)
for m in list(hair.modifiers):hair.modifiers.remove(m)
m=hair.modifiers.new('Hair edge thickness','SOLIDIFY');m.thickness=.004;m.offset=-1
bpy.data.objects.remove(scalp,do_unlink=True);bpy.data.meshes.remove(sd)
assert all(signature(bpy.data.objects[n])==s for n,s in preserved.items()),'An approved part changed'
scene['stage']='01 revision 04: curved baseball cap and tapered temple hair; body, face and scarf preserved'
zs=[(o.matrix_world@v.co).z for o in parts for v in o.data.vertices]
hz=[(o.matrix_world@v.co).z for o in parts if o.name in ['Study_Head','Study_Hair','Study_Cap'] for v in o.data.vertices]
scene['head_units']=f'{(max(zs)-min(zs))/(max(hz)-min(hz)):.2f} including cap'
scene.cycles.samples=32;cam=scene.camera
def camera(pos,target,scale):
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
camera((3,-6,2.05),(0,0,.9),2.12);scene.render.resolution_x=800;scene.render.resolution_y=1000
scene.render.filepath=str(OUT/'Quarter.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'DarkSurvivor_Stage01_Rev04.blend'))
bpy.ops.render.render(write_still=True)
report={'source_unchanged':hashlib.sha256(SOURCE.read_bytes()).hexdigest()==source_hash,'body_face_scarf_unchanged':True,'edited_parts':['Study_Cap','Study_Hair'],'static_review_only':True}
(OUT/'ShapeCheck.json').write_text(json.dumps(report,indent=2))
if '--full-render' in sys.argv:
    def dup(items,x,angle,label):
        parent=bpy.data.objects.new(label,None);scene.collection.objects.link(parent);objects=[parent]
        for src,data,matrix in items:
            o=src.copy();o.data=data;scene.collection.objects.link(o);o.parent=parent;o.matrix_local=matrix;o.hide_render=False;o.hide_set(False);objects.append(o)
        parent.location.x=x;parent.rotation_euler.z=angle;return objects
    current=[(o,o.data,o.matrix_world.copy()) for o in parts]
    for o in parts:o.hide_render=True;o.hide_set(True)
    copies=dup(old,-.40,-math.radians(22),'Previous')+dup(current,.40,-math.radians(22),'Revised')
    camera((0,-8,2.4),(0,0,1.42),1.58);scene.render.resolution_x=1440;scene.render.resolution_y=960
    scene.render.filepath=str(OUT/'HeadBeforeAfter.png');bpy.ops.render.render(write_still=True)
    for o in copies:bpy.data.objects.remove(o,do_unlink=True)
    for x,angle in [(-1.15,0),(0,-math.pi/2),(1.15,math.pi)]:dup(current,x,angle,'Turnaround')
    camera((0,-8,1.28),(0,0,.90),3.8);scene.render.resolution_x=1680;scene.render.resolution_y=1000
    scene.render.filepath=str(OUT/'FrontSideBack.png');bpy.ops.render.render(write_still=True)
print('CAP_HAIR_REV04_READY',str(OUT),flush=True)
