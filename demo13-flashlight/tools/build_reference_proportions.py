"""Reference-measured sizing pass only. No outfit detail or game integration.
Pixel landmarks are read from the front figure of the user's 1536x1024 image.
The image is a perspective concept sheet, so depths remain estimates from its side view.
"""
import bpy, bmesh, math, shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ChibiSurvivor/ProportionStudy'
OUT.mkdir(parents=True,exist_ok=True)
REFERENCE=Path('C:/Users/admin/AppData/Local/Temp/codex-clipboard-94efe687-e4b1-4384-a7de-b87a97cf5792.png')
if not (OUT/'Reference.png').exists():shutil.copy2(REFERENCE,OUT/'Reference.png')
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
TOP=66; FLOOR=934; CHIN=333; WAIST=530; KNEE=744; CX=297
K=1.8/(FLOOR-TOP)
def Z(pixel):return (FLOOR-pixel)*K
def X(pixel):return (pixel-CX)*K
M={}
for name,color in {'head':(.36,.38,.40),'cap':(.22,.24,.25),'torso':(.25,.28,.30),'limbs':(.32,.34,.36),'pants':(.16,.18,.20),'boots':(.22,.24,.26)}.items():
    m=bpy.data.materials.new('Study_'+name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes['Principled BSDF']; bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=.95
    M[name]=m
def mesh(name,vs,fs,mat):
    d=bpy.data.meshes.new(name); d.from_pydata(vs,[],fs); d.update()
    bm=bmesh.new(); bm.from_mesh(d); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(d); bm.free()
    o=bpy.data.objects.new(name,d); bpy.context.collection.objects.link(o); d.materials.append(M[mat]); o['stage']='Sizing only'
    for p in d.polygons:p.use_smooth=True
    return o
def shape(name,rows,mat,n=32,power=.86):
    # pixel row, pixel half-width, pixel half-depth, pixel x center, pixel y center
    rows=sorted(rows,key=lambda r:r[0],reverse=True); vs=[]
    for py,rx,ry,cx,cy in rows:
        for i in range(n):
            a=math.tau*i/n; sx,sy=math.sin(a),-math.cos(a)
            vs.append((X(cx)+rx*K*math.copysign(abs(sx)**power,sx),cy*K+ry*K*math.copysign(abs(sy)**power,sy),Z(py)))
    fs=[tuple(reversed(range(n)))]+[(r*n+i,r*n+(i+1)%n,(r+1)*n+(i+1)%n,(r+1)*n+i) for r in range(len(rows)-1) for i in range(n)]
    fs.append(tuple(range((len(rows)-1)*n,len(rows)*n)))
    return mesh(name,vs,fs,mat)
def oval(name,pixelx,pixely,width,height,depth,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,radius=1,location=(X(pixelx),0,Z(pixely)))
    o=bpy.context.object; o.name=name; o.scale=(width*K,depth*K,height*K)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(M[mat]); o['stage']='Sizing only'
    for p in o.data.polygons:p.use_smooth=True
    return o

shape('Head',[(196,89,80,CX,10),(221,97,85,CX,10),(249,101,86,CX,10),(275,96,82,CX,10),
    (293,83,70,CX,8),(305,76,60,CX,5),(315,62,47,CX,3),(325,38,30,CX,0),(CHIN,18,18,CX,0)],'head')
shape('Cap',[(TOP,9,9,CX,18),(76,43,39,CX,17),(93,79,71,CX,15),(120,108,88,CX,12),
    (153,122,97,CX,9),(184,124,99,CX,7),(214,118,94,CX,5)],'cap',power=.95)
# Thin curved visor, the basic projected contour from the front concept.
vs=[]; N=25
for row in range(3):
    u=row/2
    for i in range(N):
        t=-1+2*i/(N-1)
        vs.append((t*111*K,(-86+36*t*t-u*(57-16*t*t))*K,Z(182+33*t*t+14*u)))
visor=mesh('Visor',vs,[(j*N+i,j*N+i+1,(j+1)*N+i+1,(j+1)*N+i) for j in range(2) for i in range(N-1)],'cap')
sol=visor.modifiers.new('VisorThickness','SOLIDIFY'); sol.thickness=4*K
bpy.context.view_layer.objects.active=visor; bpy.ops.object.modifier_apply(modifier=sol.name)
for s in [-1,1]:oval('Ear',CX+s*114,265,17,25,12,'head')
oval('Neck',CX,341,27,20,24,'limbs')
shape('Torso',[(347,46,34,CX,0),(367,75,45,CX,0),(395,90,53,CX,0),(454,92,56,CX,0),
    (509,97,58,CX,0),(WAIST,92,56,CX,0)],'torso')
shape('Hips',[(524,92,56,CX,0),(556,95,57,CX,0),(588,86,52,CX,0),(608,69,46,CX,0)],'pants')
for s in [-1,1]:
    shape('Sleeve',[(368,19,30,CX+s*78,0),(386,29,32,CX+s*93,0),(425,29,31,CX+s*107,0),
        (475,27,30,CX+s*117,-1),(499,30,32,CX+s*120,-2)],'torso')
    shape('Forearm',[(500,22,24,CX+s*121,-1),(529,22,23,CX+s*125,-3),(578,18,20,CX+s*134,-5),
        (597,17,19,CX+s*136,-6)],'limbs')
    oval('Hand',CX+s*137,619,23,31,20,'limbs')
    shape('Leg',[(583,43,49,CX+s*50,0),(628,45,48,CX+s*54,1),(689,44,45,CX+s*59,3),
        (KNEE,41,43,CX+s*64,-4),(794,43,43,CX+s*67,2),(838,44,44,CX+s*68,3)],'pants')
    shape('Boot',[(835,41,43,CX+s*69,3),(866,46,56,CX+s*71,-8),(892,57,76,CX+s*75,-24),
        (918,60,78,CX+s*77,-27),(FLOOR,59,77,CX+s*77,-27)],'boots',power=.73)
character=[o for o in bpy.context.scene.objects if o.type=='MESH']
for o in character:
    o['source_image']='Reference.png, front figure'
    o['purpose']='Proportion study; face/clothing details deliberately omitted'
scene=bpy.context.scene
scene['reference_landmarks_px']=str(dict(top=TOP,chin=CHIN,waist=WAIST,knee=KNEE,floor=FLOOR))
scene['head_fraction']=(CHIN-TOP)/(FLOOR-TOP)
scene['torso_fraction']=(WAIST-CHIN)/(FLOOR-TOP)
scene['lower_fraction']=(FLOOR-WAIST)/(FLOOR-TOP)
scene['depth_note']='Depth inferred from side concept; not a calibrated orthographic reference'
scene.render.engine='CYCLES'; scene.cycles.samples=32; scene.cycles.use_denoising=True
scene.world.use_nodes=True; bg=scene.world.node_tree.nodes['Background']; bg.inputs[0].default_value=(.04,.043,.047,1); bg.inputs[1].default_value=.45
scene.view_settings.view_transform='Standard'; scene.view_settings.look='None'
def area(pos,power,size):
    bpy.ops.object.light_add(type='AREA',location=pos); o=bpy.context.object; o.data.energy=power; o.data.size=size
    o.rotation_euler=(Vector((0,0,.9))-o.location).to_track_quat('-Z','Y').to_euler()
area((-3,-4,5),350,4); area((3,-3,2),90,3)
bpy.ops.object.camera_add(location=(0,-7,.9)); scene.camera=bpy.context.object
scene.camera.rotation_euler=(math.pi/2,0,0); scene.camera.data.type='ORTHO'; scene.camera.data.ortho_scale=2.04
scene.render.resolution_x=850; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ProportionStudy.blend'))
scene.render.filepath=str(OUT/'Front.png'); bpy.ops.render.render(write_still=True)

# Native Blender sizing board: reference texture plane and actual model share one scale.
LEFT=-.49; RIGHT=.49
for o in character:o.location.x+=RIGHT
im=bpy.data.images.load(str(OUT/'Reference.png')); im.pack()
crop=(124,458,TOP,FLOOR)
w=(crop[1]-crop[0])*K; h=1.8
d=bpy.data.meshes.new('ReferencePlane'); d.from_pydata([(LEFT-w/2,.3,0),(LEFT+w/2,.3,0),(LEFT+w/2,.3,h),(LEFT-w/2,.3,h)],[],[(0,1,2,3)]); d.update()
o=bpy.data.objects.new('ReferencePlane',d); bpy.context.collection.objects.link(o)
uv=d.uv_layers.new(); coords=[(crop[0]/1536,(1024-FLOOR)/1024),(crop[1]/1536,(1024-FLOOR)/1024),(crop[1]/1536,(1024-TOP)/1024),(crop[0]/1536,(1024-TOP)/1024)]
for loop in d.loops:uv.data[loop.index].uv=coords[loop.vertex_index]
m=bpy.data.materials.new('ReferenceImage'); m.use_nodes=True; m.node_tree.nodes.clear()
tex=m.node_tree.nodes.new('ShaderNodeTexImage'); tex.image=im; emission=m.node_tree.nodes.new('ShaderNodeEmission'); output=m.node_tree.nodes.new('ShaderNodeOutputMaterial')
m.node_tree.links.new(tex.outputs['Color'],emission.inputs['Color']); m.node_tree.links.new(emission.outputs[0],output.inputs['Surface']); d.materials.append(m)
def emissive(name,c):
    m=bpy.data.materials.new(name); m.use_nodes=True; m.node_tree.nodes.clear()
    e=m.node_tree.nodes.new('ShaderNodeEmission'); e.inputs[0].default_value=(*c,1); out=m.node_tree.nodes.new('ShaderNodeOutputMaterial'); m.node_tree.links.new(e.outputs[0],out.inputs[0]); return m
line_mat=emissive('Guide',(.38,.57,.58)); text_mat=emissive('Labels',(.83,.87,.88))
font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
def text_obj(text,pos,size=.03):
    data=bpy.data.curves.new('Label','FONT'); data.body=text; data.size=size; data.font=font
    obj=bpy.data.objects.new('Label',data); bpy.context.collection.objects.link(obj); obj.location=pos; obj.rotation_euler=(math.pi/2,0,0); data.materials.append(text_mat)
for label,pixel in [('모자 끝',TOP),('턱',CHIN),('허리',WAIST),('무릎',KNEE),('발바닥',FLOOR)]:
    z=Z(pixel)
    # Sparse dashed guides keep the underlying silhouette visible.
    for i in range(43):
        x=-.87+i*.042
        data=bpy.data.curves.new('Guide','CURVE'); data.dimensions='3D'; data.bevel_depth=.0007; data.bevel_resolution=0
        sp=data.splines.new('POLY'); sp.points.add(1); sp.points[0].co=(x,-.5,z,1); sp.points[1].co=(x+.022,-.5,z,1)
        obj=bpy.data.objects.new('Guide',data); bpy.context.collection.objects.link(obj); data.materials.append(line_mat)
    text_obj(label,(.89,-.51,z-.008),.027)
text_obj('원본 정면',(-.69,-.51,1.88),.035)
text_obj('비율 확인용 Blender',(.20,-.51,1.88),.035)
text_obj('머리 약 31%  |  턱~허리 약 23%  |  허리 아래 약 46%',(-.76,-.51,-.11),.028)
scene.camera.location=(.07,-7,.91); scene.camera.data.ortho_scale=2.32
scene.render.resolution_x=1600; scene.render.resolution_y=1500
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ProportionComparison.blend'))
scene.render.filepath=str(OUT/'Comparison.png'); bpy.ops.render.render(write_still=True)
print('PROPORTION_STUDY_CREATED',str(OUT),'HEAD_UNITS',(FLOOR-TOP)/(CHIN-TOP))
