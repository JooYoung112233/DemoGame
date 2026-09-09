"""Face-only detail pass on the user-approved ProportionStudy (2026-09-08).
Run in Blender background with --python. Never overwrite the approved base.
"""
import bpy, math, hashlib, json, sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / 'Assets/ChibiSurvivor/ProportionStudy'
SQUARE_JAW = '--square-jaw' in sys.argv
SIMPLE = '--simple' in sys.argv or SQUARE_JAW
OUT = ROOT / ('Assets/ChibiSurvivor/FaceStudySquare' if SQUARE_JAW else 'Assets/ChibiSurvivor/FaceStudySimple' if SIMPLE else 'Assets/ChibiSurvivor/FaceStudy')
STAGE = 'Soft square face 03 - pending user review' if SQUARE_JAW else 'Face simplification 02 - pending user review' if SIMPLE else 'Face detail pass 01 - pending user review'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE / 'ProportionStudy.blend'))
scene = bpy.context.scene
K = 1.8 / 868
def X(px): return (px - 297) * K
def Z(py): return (934 - py) * K

def signature(obj):
    payload = [list(v.co) for v in obj.data.vertices]
    payload += [list(row) for row in obj.matrix_world]
    return hashlib.sha256(repr(payload).encode()).hexdigest()

base_objects = [o for o in scene.objects if o.type == 'MESH']
before = {o.name: signature(o) for o in base_objects}
detail = bpy.data.collections.new('Face_Details_Editable')
scene.collection.children.link(detail)
def linear(v): return v/12.92 if v <= .04045 else ((v+.055)/1.055)**2.4
def material(name, rgb):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    c = tuple(linear(v) for v in rgb)
    m.diffuse_color = (*c, 1)
    bs = m.node_tree.nodes['Principled BSDF']
    bs.inputs['Base Color'].default_value = (*c, 1)
    bs.inputs['Roughness'].default_value = .94
    bs.inputs['Specular IOR Level'].default_value = .14
    return m

skin = material('Face_Skin', (.88,.665,.47))
earshade = material('Ear_Inset', (.72,.475,.32))
earlight = material('Ear_Inner_Plane', (.82,.575,.395))
sclera = material('Eye_Warm_Ivory', (.79,.705,.565))
iris = material('Eye_Iris_Brown', (.225,.197,.155) if SIMPLE else (.29,.255,.195))
pupil = material('Eye_Pupil', (.18,.165,.13))
eyeedge = material('Eye_Lower_Edge', (.54,.36,.225))
lid = material('Eye_Upper_Lid', (.26,.185,.12))
brow = material('Brows', (.225,.188,.135))
nosemat = material('Nose_Warm_Plane', (.815,.56,.385) if SIMPLE else (.715,.435,.275))
noseside = material('Nose_Shadow_Plane', (.64,.355,.22))
mouthmat = material('Mouth', (.47,.285,.18))
bandage = material('Bandage_Fabric', (.535,.468,.365))
bandagepad = material('Bandage_Pad', (.57,.50,.393))

head = bpy.data.objects['Head']
if SQUARE_JAW:
    # Local silhouette correction authorized after proportion approval.
    # Retain vertex Z, upper head, maximum width and the whole body.
    # Expand the lower cheek/jaw instead of scaling the complete head.
    # pixel row: old width/depth, new width/depth, depth center
    jaw_rows={
        275:(96,82,100,84,10),
        293:(83,70,96,79,8),
        305:(76,60,91,73,5),
        315:(62,47,80,62,3),
        325:(38,30,62,48,0),
        333:(18,18,38,29,0),
    }
    for v in head.data.vertices:
        row=round(934-v.co.z/K)
        if row not in jaw_rows:continue
        old_w,old_d,new_w,new_d,center=jaw_rows[row]
        v.co.x*=new_w/old_w
        v.co.y=center*K+(v.co.y-center*K)*new_d/old_d
    head.data.update()
    bpy.context.view_layer.update()
    head['shape_revision']='Soft square: fuller lower cheek, wider jaw corners, short flatter chin; overall height/width retained'
for name in ['Head','Ear','Ear.001','Neck']:
    bpy.data.objects[name].data.materials.clear()
    bpy.data.objects[name].data.materials.append(skin)

# Subtle broad planes without subdivision or decimation.
head.data.update()
normals = []
for p in head.data.polygons:
    for li in p.loop_indices:
        vn = head.data.vertices[head.data.loops[li].vertex_index].normal
        normals.append((vn*.80+p.normal*.20).normalized())
head.data.normals_split_custom_set(normals)

def surface(px, py, offset=.0005, target=head):
    world = Vector((X(px), -3, Z(py)))
    inv = target.matrix_world.inverted()
    origin = inv @ world
    direction = (inv.to_3x3() @ Vector((0,1,0))).normalized()
    hit, loc, normal, idx = target.ray_cast(origin, direction)
    if not hit:
        raise RuntimeError('Projection missed %s at %.2f, %.2f' % (target.name,px,py))
    return (target.matrix_world @ loc) + Vector((0,-offset,0))

def mesh(name, verts, faces, mat, smooth=True):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    detail.objects.link(obj)
    data.materials.append(mat)
    for p in data.polygons: p.use_smooth = smooth
    obj['stage'] = STAGE
    obj['attachment'] = 'Head; static detail study, not yet rigged'
    return obj

def patch(name, outline, mat, offset=.0008, target=head, center=None):
    # Resample polygon edges before projection: long edges otherwise cut
    # through the base surface and create jagged missing patches.
    dense=[]
    for i,p in enumerate(outline):
        q=outline[(i+1)%len(outline)]
        steps=max(1,math.ceil(math.dist(p,q)/1.25))
        for j in range(steps):
            u=j/steps
            dense.append((p[0]+(q[0]-p[0])*u,p[1]+(q[1]-p[1])*u))
    outline=dense
    offset+=.001
    if center is None:
        center = (sum(p[0] for p in outline)/len(outline), sum(p[1] for p in outline)/len(outline))
    vs = [surface(*center,offset,target)]
    fs = []
    N = len(outline)
    rings = max(6,math.ceil(max(math.dist(p,center) for p in outline)/1.25))
    for r in range(1,rings+1):
        u = r/rings
        for p in outline:
            vs.append(surface(center[0]+(p[0]-center[0])*u, center[1]+(p[1]-center[1])*u,offset,target))
    for i in range(N): fs.append((0,1+i,1+(i+1)%N))
    for r in range(rings-1):
        a=1+r*N; b=a+N
        for i in range(N):
            fs.append((a+i,b+i,b+(i+1)%N))
            fs.append((a+i,b+(i+1)%N,a+(i+1)%N))
    # Counterclockwise in image coordinates faces -Y in Blender.
    return mesh(name,vs,[tuple(reversed(f)) for f in fs],mat)

def ellipse(cx,cy,rx,ry,clip_top=None,N=64):
    pts=[]
    for i in range(N):
        a=math.tau*i/N
        y=cy+ry*math.sin(a)
        if clip_top is not None:y=max(y,clip_top)
        pts.append((cx+rx*math.cos(a),y))
    return pts

def curve(name, coords, mat, radius, offset=.0012, target=head):
    data=bpy.data.curves.new(name,'CURVE'); data.dimensions='3D'
    data.bevel_depth=radius*K; data.bevel_resolution=1; data.resolution_u=10
    sp=data.splines.new('BEZIER'); sp.bezier_points.add(len(coords)-1)
    for b,p in zip(sp.bezier_points,coords):
        b.co=surface(*p,offset,target)
        b.handle_left_type='AUTO'; b.handle_right_type='AUTO'
    obj=bpy.data.objects.new(name,data); detail.objects.link(obj); data.materials.append(mat)
    return obj

for side,cx in [('R',249),('L',344)]:
    # The upper eyelid cuts across a large dark iris. No spherical eyeball.
    if not SIMPLE:
        patch('Eye_Socket_'+side,ellipse(cx,261,20.5,20.1,245),eyeedge,.0006)
    patch('Eye_White_'+side,ellipse(cx,261,20.1,19.5,247),sclera,.0010)
    patch('Eye_Iris_'+side,ellipse(cx+1,260.6,15.6,18.0,247.2),iris,.0015)
    if not SIMPLE:
        patch('Eye_Pupil_'+side,ellipse(cx+1.5,258,9.0,12.3,246),pupil,.0019)
    eyelid=[(cx-23,246.8),(cx-17,242.1),(cx-4,241.0),(cx+11,242.0),(cx+22,246.2),(cx+21,251.2),(cx+8,249.4),(cx-6,249.0),(cx-22,251.1)]
    if SIMPLE:
        eyelid=[(x,248+(y-248)*.60) for x,y in eyelid]
    patch('Eye_Lid_'+side,eyelid,lid,.0025)
    # Narrow skin plane just above the lash, rather than a thick tube.
    if not SIMPLE:
        patch('Eye_Fold_'+side,[(cx-21,240.7),(cx-12,237.5),(cx+5,237.6),(cx+19,242),(cx+13,242),(cx-4,239.6)],earlight,.0010)

patch('Brow_R',[(227,226),(268,223),(269,229),(227,232)] if SIMPLE else [(221,227),(269,220),(271,229),(221,233)],brow,.0013)
patch('Brow_L',[(324,223),(363,226),(364,232),(324,229)] if SIMPLE else [(323,220),(367,226),(369,232),(322,229)],brow,.0013)

# A small faceted nose, with shallow bridge; depth is local, head unchanged.
nosepx=[(293,274),(300,274),(307,286),(305,290),(289,289),(288,285)]
if SIMPLE:
    nosepx=[(297+(x-297)*.70,284+(y-284)*.72) for x,y in nosepx]
vs=[surface(x,y,.0009) for x,y in nosepx]
tip=surface(297,284,.0009); tip.y-=(4.5 if SIMPLE else 9)*K
vs.append(tip)
nose=mesh('Nose',vs,[(i,(i+1)%6,6) for i in range(6)],nosemat,False)
nose.data.materials.append(noseside)
for p in nose.data.polygons:
    if not SIMPLE and p.index in (2,3):p.material_index=1
curve('Mouth',[(282,309),(290,307.5),(299,307.4),(310,310)],mouthmat,.60)

for side,objname,cx,sign in ([] if SIMPLE else [('R','Ear',183,-1),('L','Ear.001',411,1)]):
    ear=bpy.data.objects[objname]
    outline=[(cx-7,250),(cx+4,247),(cx+11,254),(cx+10,271),(cx+1,279),(cx-8,272),(cx-10,260)]
    patch('Ear_Concha_'+side,outline,earshade,.0008,ear)
    patch('Ear_Inner_'+side,[(cx-6,254),(cx+3,252),(cx+6,258),(cx+3,269),(cx-4,270)],earlight,.0012,ear)

# Familiar cheek bandage, separate replaceable mesh, without stitch clutter.
bandage_outline=[(346,293),(369,285),(374,299),(349,308)]
if SIMPLE:
    bandage_outline=[(360+(x-360)*.85,297+(y-297)*.85) for x,y in bandage_outline]
patch('Cheek_Bandage',bandage_outline,bandage,.0015)
if not SIMPLE:
    patch('Cheek_Bandage_Pad',[(352,292.8),(364,288.7),(368,300),(356,304)],bandagepad,.0020)

after={o.name:signature(o) for o in base_objects}
changed=[name for name in before if before[name]!=after[name]]
assert changed==(['Head'] if SQUARE_JAW else []), 'Unexpected change outside authorized face-shape scope'
report={'approved_base':str(BASE/'ProportionStudy.blend'),'base_geometry_unchanged':before==after,
        'changed_base_objects':changed,'body_geometry_unchanged':all(before[n]==after[n] for n in before if n!='Head'),
        'objects_checked':len(before),'signatures':before,'stage':STAGE,
        'scope':'User-requested local jaw/cheek correction; original cap/body/limbs preserved. No rig or runtime changes.' if SQUARE_JAW else 'Face only; original cap/body/limbs geometry preserved. No rig or runtime changes.'}
(OUT/'GeometryCheck.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
scene['stage']=STAGE
if SIMPLE:
    scene['simplification']='Plain ears, single dark iris, no eye rim or fold accents, thinner eyelids/brows, smaller nose and single-piece bandage; calm survivor expression'
scene['approved_proportion_base']=str(BASE/'ProportionStudy.blend')
scene.cycles.samples=48
scene.render.resolution_percentage=100
bpy.ops.object.light_add(type='AREA', location=(0,-4,1.6))
fill=bpy.context.object;fill.name='Face_Soft_Fill';fill.data.energy=65;fill.data.size=3
fill.rotation_euler=(Vector((0,0,Z(265)))-fill.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'FaceStudy.blend'))
scene.render.filepath=str(OUT/'FullBody.png')
bpy.ops.render.render(write_still=True)

# Close-up front and 3/4 renders from the actual Blender geometry.
cam=scene.camera
cam.location=(0,-6,Z(220)); cam.rotation_euler=(math.pi/2,0,0)
cam.data.ortho_scale=.79
scene.render.resolution_x=1000; scene.render.resolution_y=1000
scene.render.filepath=str(OUT/'FaceFront.png'); bpy.ops.render.render(write_still=True)
cam.location=(2.3,-5,Z(216)+.12)
cam.rotation_euler=(Vector((0,0,Z(220)))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(OUT/'FaceQuarter.png'); bpy.ops.render.render(write_still=True)
if SIMPLE:
    print('FACE_STUDY_COMPLETE',str(OUT),'CHANGED_BASE_OBJECTS',changed,'BODY_GEOMETRY_UNCHANGED',report['body_geometry_unchanged'])
    sys.exit(0)

# Original face crop + Blender front, rendered together with matching scale.
for o in [*base_objects,*list(detail.objects)]:o.location.x+=.38
im=bpy.data.images.load(str(BASE/'Reference.png')); im.pack()
crop=(153,435,62,346)
left=-.36; w=(crop[1]-crop[0])*K; h=(crop[3]-crop[2])*K
z0=Z(crop[3])
data=bpy.data.meshes.new('Reference_Crop')
data.from_pydata([(left-w/2,.35,z0),(left+w/2,.35,z0),(left+w/2,.35,z0+h),(left-w/2,.35,z0+h)],[],[(0,1,2,3)])
data.update(); obj=bpy.data.objects.new('Reference_Crop',data);scene.collection.objects.link(obj)
uv=data.uv_layers.new()
coords=[(crop[0]/1536,1-crop[3]/1024),(crop[1]/1536,1-crop[3]/1024),(crop[1]/1536,1-crop[2]/1024),(crop[0]/1536,1-crop[2]/1024)]
for l in data.loops:uv.data[l.index].uv=coords[l.vertex_index]
m=bpy.data.materials.new('Reference_Emission');m.use_nodes=True;m.node_tree.nodes.clear()
t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=im
e=m.node_tree.nodes.new('ShaderNodeEmission');out=m.node_tree.nodes.new('ShaderNodeOutputMaterial')
m.node_tree.links.new(t.outputs['Color'],e.inputs['Color']);m.node_tree.links.new(e.outputs[0],out.inputs[0]);data.materials.append(m)
font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
def label(body,x):
    d=bpy.data.curves.new('Label','FONT');d.body=body;d.font=font;d.size=.026
    o=bpy.data.objects.new('Label',d);scene.collection.objects.link(o)
    o.location=(x,-.55,Z(46));o.rotation_euler=(math.pi/2,0,0)
label('원본 시안',-.50);label('Blender 얼굴 1차',.18)
cam.location=(0,-7,Z(208));cam.rotation_euler=(math.pi/2,0,0);cam.data.ortho_scale=1.46
scene.render.resolution_x=1600;scene.render.resolution_y=850
scene.render.filepath=str(OUT/'FaceComparison.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'FaceComparison.blend'))
bpy.ops.render.render(write_still=True)
print('FACE_STUDY_COMPLETE',str(OUT),'BASE_GEOMETRY_UNCHANGED',before==after)
