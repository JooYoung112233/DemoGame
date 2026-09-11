"""Stage 02: simplified face and restrained palette on the approved silhouette."""
import bpy,bmesh,math,json,hashlib,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivorReview/Stage01_Silhouette/Revision04_CapHair/DarkSurvivor_Stage01_Rev04.blend'
PRODUCTION=ROOT/'Assets/ChibiSurvivor/Player/CompactSurvivor_Combat.blend'
OUT=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivorReview/Stage02_FacePalette';OUT.mkdir(parents=True,exist_ok=True)
sourcehash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;parts=[o for o in scene.objects if o.type=='MESH' and o.name.startswith('Study_')]
def geom(o):return hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons],list(map(tuple,o.matrix_world)))).encode()).hexdigest()
preserved={o.name:geom(o) for o in parts if o.name!='Study_FaceDetails'}
colors={'skin':(.50,.305,.18),'ear':(.34,.175,.09),'cap':(.105,.109,.073),'brim':(.035,.042,.035),'patch':(.16,.115,.074),'hair':(.024,.018,.013),'ink':(.017,.018,.014),'white':(.46,.40,.30),'iris':(.033,.032,.023),'nose':(.38,.205,.112),'mouth':(.18,.082,.043),'jacket':(.059,.071,.053),'cuff':(.083,.09,.069),'lapel':(.067,.078,.057),'scarf':(.205,.068,.043),'trousers':(.027,.034,.036),'trouser_cuff':(.044,.05,.047),'boots':(.061,.038,.025),'sole':(.022,.025,.023),'leather':(.039,.027,.019),'pack':(.067,.072,.054),'flap':(.087,.089,.065),'metal':(.17,.175,.154),'lamp':(.039,.043,.045),'under':(.20,.185,.145)}
M={}
colors['iris']=(.068,.055,.035)
colors['pupil']=(.018,.020,.017)
for name,c in colors.items():
    m=bpy.data.materials.new('Hero_'+name);m.use_nodes=True;m.diffuse_color=(*c,1)
    p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.83;p.inputs['Specular IOR Level'].default_value=.25
    if name=='metal':p.inputs['Metallic'].default_value=.4;p.inputs['Roughness'].default_value=.6
    M[name]=m
def assign(o,key):
    o.data.materials.clear();o.data.materials.append(M[key])
    for slot in o.material_slots:slot.link='DATA'
    for p in o.data.polygons:p.material_index=0
# Recover the original polygon-to-material mapping lost during the clay study.
with bpy.data.libraries.load(str(PRODUCTION),link=False) as (src,dst):dst.objects=['Compact_Head','Compact_Cap']
for ref,name in zip(dst.objects,['Study_Head','Study_Cap']):
    obj=bpy.data.objects[name];assert len(obj.data.polygons)==len(ref.data.polygons)
    indices=[p.material_index for p in ref.data.polygons];names=[m.name.split('.')[0] for m in ref.data.materials]
    obj.data.materials.clear()
    for n in names:obj.data.materials.append(M[n])
    for p,index in zip(obj.data.polygons,indices):p.material_index=index
    bpy.data.objects.remove(ref,do_unlink=True)
for o in parts:
    n=o.name
    if n in ['Study_Head','Study_Cap','Study_FaceDetails']:continue
    key='jacket'
    if n=='Study_Hair':key='hair'
    elif any(t in n for t in ['Forearm','Hands','Neck']):key='skin'
    elif 'Scarf' in n:key='scarf'
    elif 'RolledSleeve' in n:key='cuff'
    elif 'Lapel' in n:key='lapel'
    elif n=='Study_JacketOpening':key='under'
    elif n=='Study_Hips' or 'TrouserLeg' in n:key='trousers'
    elif 'TrouserCuff' in n:key='trouser_cuff'
    elif 'Boot' in n:key='boots'
    elif 'Sole' in n:key='sole'
    elif 'Buckle' in n:key='metal'
    elif n=='Study_Belt' or 'PackStrap' in n or 'Closure' in n:key='leather'
    elif n=='Study_Backpack':key='pack'
    elif n=='Study_BackpackFlap':key='flap'
    elif 'Lantern' in n:key='lamp'
    assign(o,key)

# Few flush shapes convey expression; there are no protruding eye rims.
head=bpy.data.objects['Study_Head'];f=bpy.data.objects['Study_FaceDetails']
faceparts=[]
def decal(name,outline,key,offset):
    cx=sum(x for x,z in outline)/len(outline);cz=sum(z for x,z in outline)/len(outline)
    vs=[(cx,-1,cz)]+[(x,-1,z) for x,z in outline];fs=[(0,i+1,(i+1)%len(outline)+1) for i in range(len(outline))]
    d=bpy.data.meshes.new(name);d.from_pydata(vs,[],fs);d.update()
    bm=bmesh.new();bm.from_mesh(d);bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=2,use_grid_fill=True);bm.to_mesh(d);bm.free()
    for v in d.vertices:
        hit,p,_,_=head.ray_cast(Vector((v.co.x,-1,v.co.z)),Vector((0,1,0)));assert hit,(name,tuple(v.co))
        v.co=p+Vector((0,-offset,0))
    d.update()
    for p in d.polygons:
        if p.normal.y>0:p.flip()
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);d.materials.append(M[key]);faceparts.append(o)

for s in [-1,1]:
    cx=s*.117
    decal('EyeWhite',[(cx-.042,1.396),(cx+.042,1.396),(cx+.039,1.350),(cx+.029,1.327),(cx+.014,1.32),(cx-.017,1.32),(cx-.033,1.333),(cx-.04,1.352)],'white',.002)
    decal('EyeDark',[(cx-.029,1.393),(cx+.029,1.393),(cx+.028,1.351),(cx+.019,1.333),(cx,1.327),(cx-.02,1.335),(cx-.028,1.354)],'iris',.0036)
    decal('Pupil',[(cx+math.sin(i*math.tau/16)*.0145,1.369+math.cos(i*math.tau/16)*.026) for i in range(16)],'pupil',.0042)
    decal('UpperLid',[(cx-.046,1.392),(cx-.043,1.399),(cx,1.401),(cx+.043,1.398),(cx+.046,1.391),(cx,1.395)],'ink',.0048)
    outline=[]
    for dx,dz in [(-.044,0),(.043,0),(.042,.014),(-.042,.017)]:outline.append((cx+dx,1.435+s*dx*.09+dz))
    decal('Brow',outline,'hair',.0025)
# Preserve the nose geometry as a small warm facet; simplify the mouth and cheek mark.
with bpy.data.libraries.load(str(PRODUCTION),link=False) as (src,dst):dst.objects=['Compact_FaceDetails']
ref=dst.objects[0];indices=sorted({i for p in ref.data.polygons if p.material_index==5 for i in p.vertices});indexmap={v:i for i,v in enumerate(indices)}
vs=[ref.data.vertices[i].co+Vector((0,0,.2352)) for i in indices];fs=[tuple(indexmap[i] for i in p.vertices) for p in ref.data.polygons if p.material_index==5]
d=bpy.data.meshes.new('SmallNose');d.from_pydata(vs,[],fs);d.update();o=bpy.data.objects.new('SmallNose',d);scene.collection.objects.link(o);d.materials.append(M['nose']);faceparts.append(o)
bpy.data.objects.remove(ref,do_unlink=True)
decal('QuietMouth',[(-.033,1.266),(-.015,1.269),(.01,1.268),(.033,1.265),(.032,1.262),(.01,1.265),(-.015,1.266),(-.033,1.264)],'mouth',.0022)
decal('CheekPatch',[(.175,1.271),(.222,1.286),(.22,1.309),(.173,1.294)],'patch',.0028)
bpy.ops.object.select_all(action='DESELECT')
for o in faceparts:o.select_set(True)
bpy.context.view_layer.objects.active=faceparts[0];bpy.ops.object.join();newface=bpy.context.object
f.data=newface.data.copy();bpy.data.objects.remove(newface,do_unlink=True)
f['face_design']='flat two-tone eyes, fine upper lid, steady eyebrows and small closed mouth'

# A flat coloured inset previews the future lantern light without adding finished prop detail.
lampmat=bpy.data.materials.new('Hero_AnomalyAccent');lampmat.use_nodes=True
p=lampmat.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(.13,.034,.24,1);p.inputs['Emission Color'].default_value=(.27,.055,.56,1);p.inputs['Emission Strength'].default_value=1.2;p.inputs['Roughness'].default_value=.7
bpy.ops.mesh.primitive_cube_add(size=1,location=(-.215,-.118,.715));accent=bpy.context.object;accent.name='Study_LanternAccent';accent.dimensions=(.05,.003,.084)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);accent.data.materials.append(lampmat);accent['purpose']='Stage02 colour/light swatch; final lantern modelling deferred';parts.append(accent)
assert all(geom(bpy.data.objects[n])==v for n,v in preserved.items()),'Approved silhouette changed'
cam=scene.camera
def camera(pos,target,scale):
    cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
camera((3,-6,2.05),(0,0,.9),2.12)
scene.cycles.samples=32;scene.render.resolution_x=800;scene.render.resolution_y=1000
scene['stage']='02 face and palette review. Solid colours; final wear textures, rig and game hookup pending.'
scene['face_palette']='muted olive charcoal, rust neckerchief, warm face and small violet anomaly accent'
scene.render.filepath=str(OUT/'NeutralQuarter.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'DarkSurvivor_Stage02.blend'));bpy.ops.render.render(write_still=True)
if '--full-render' in sys.argv:
    camera((2,-6,2.0),(0,0,1.4),.97);scene.render.resolution_x=900;scene.render.resolution_y=900
    scene.render.filepath=str(OUT/'Face.png');bpy.ops.render.render(write_still=True)
    camera((3,-6,2.05),(0,0,.9),2.12);scene.render.resolution_x=800;scene.render.resolution_y=1000
    settings={n:(bpy.data.objects[n].data.energy,tuple(bpy.data.objects[n].data.color)) for n in ['Key','Fill','Rim']}
    world=scene.world.node_tree.nodes['Background'];world.inputs[0].default_value=(.11,.15,.22,1);world.inputs[1].default_value=.27
    for name,energy,color in [('Key',210,(.7,.8,1)),('Fill',115,(.70,.72,.87)),('Rim',240,(.54,.63,1))]:
        light=bpy.data.objects[name];light.data.energy=energy;light.data.color=color
    scene.render.filepath=str(OUT/'DarkQuarter.png');bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'DarkSurvivor_Stage02_DarkLight.blend'))
report={'source_unchanged':hashlib.sha256(SOURCE.read_bytes()).hexdigest()==sourcehash,'approved_silhouette_unchanged':True,'face_simplified':True,'palette_linear_rgb':colors,'static_review_only':True,'final_textures':False,'lighting_check':'neutral and cool dark studio' if '--full-render' in sys.argv else 'neutral studio'}
(OUT/'PaletteCheck.json').write_text(json.dumps(report,indent=2))
print('FACE_PALETTE_READY',str(OUT),flush=True)
