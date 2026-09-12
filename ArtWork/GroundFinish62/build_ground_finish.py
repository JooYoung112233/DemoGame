"""Offline Blender ground dressing, 6 reusable metric modules. No Unity calls."""
import bpy, bmesh, math, random, json
from pathlib import Path
from mathutils import Vector

OUT = Path(__file__).resolve().parent
REPO = OUT.parents[1]
TOWN = REPO/'demo13-flashlight/Assets/Art/Environments/Town02'
for name in ('Models', 'BlenderSource~', 'Review'):
    (OUT/name).mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version = 0
sc = bpy.context.scene
sc.unit_settings.system = 'METRIC'
rng = random.Random(621209)
materials = {}

def material(name, color, family='Asphalt', normal=.25):
    m = bpy.data.materials.new('Ground62_'+name)
    m.use_nodes = True
    m.diffuse_color = (*color, 1)
    n, links = m.node_tree.nodes, m.node_tree.links
    bs = n.get('Principled BSDF')
    bs.inputs['Roughness'].default_value = .94
    bs.inputs['Metallic'].default_value = 0
    bs.inputs['Base Color'].default_value = (*color, 1)
    if family:
        maps = {}
        for kind in ('Base', 'Normal', 'Mask'):
            node = n.new('ShaderNodeTexImage')
            node.image = bpy.data.images.load(str(TOWN/'Textures'/f'{family}_{kind}.png'), check_existing=True)
            node.image.colorspace_settings.name = 'sRGB' if kind == 'Base' else 'Non-Color'
            node.image.pack()
            maps[kind] = node
        mix = n.new('ShaderNodeMixRGB'); mix.blend_type = 'MULTIPLY'
        mix.inputs[0].default_value = 1; mix.inputs[2].default_value = (*color, 1)
        links.new(maps['Base'].outputs['Color'], mix.inputs[1]); links.new(mix.outputs[0], bs.inputs['Base Color'])
        nm = n.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value = normal
        links.new(maps['Normal'].outputs['Color'], nm.inputs['Color']); links.new(nm.outputs[0], bs.inputs['Normal'])
        inv = n.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1
        links.new(maps['Mask'].outputs['Alpha'], inv.inputs[1]); links.new(inv.outputs[0], bs.inputs['Roughness'])
    materials[name] = m
    return m

material('Asphalt', (.145,.15,.132))
material('Soil', (.20,.168,.119))
material('DryDust', (.255,.225,.166))
material('Crack', (.066,.065,.056), normal=.10)
material('CrackLip', (.12,.124,.108))
material('Concrete', (.335,.323,.265), 'Plaster', .24)
material('ConcreteDark', (.278,.274,.225), 'Plaster', .24)
material('Stone', (.29,.278,.225), 'Plaster', .2)
material('Grass', (.155,.175,.087), None)
material('DryGrass', (.28,.246,.142), None)

# Feathered soil uses a portable vertex alpha mask, not hard-edged opaque disks.
for name in ('Soil','DryDust'):
    m=materials[name];nodes=m.node_tree.nodes;links=m.node_tree.links
    attr=nodes.new('ShaderNodeVertexColor');attr.layer_name='GroundFade'
    links.new(attr.outputs['Alpha'],nodes.get('Principled BSDF').inputs['Alpha'])
    m.surface_render_method='DITHERED'

def collection(name):
    c = bpy.data.collections.new(name); sc.collection.children.link(c); return c

def mesh(name, vs, fs, mat, col):
    me = bpy.data.meshes.new(name)
    me.from_pydata(vs, [], fs); me.update()
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces)); bm.to_mesh(me); bm.free()
    uv = me.uv_layers.new(name='MetreUV')
    for face in me.polygons:
        axis = max(range(3), key=lambda a: abs(face.normal[a]))
        axes = [a for a in range(3) if a != axis]
        for li in face.loop_indices:
            p = me.vertices[me.loops[li].vertex_index].co
            uv.data[li].uv = (p[axes[0]], p[axes[1]])
    obj = bpy.data.objects.new(name, me); col.objects.link(obj)
    me.materials.append(materials[mat]); return obj

def patch(name, x, y, rx, ry, z, mat, col, sides=9):
    vs = [(x,y,z)];angles=[]
    for i in range(sides):
        a = i*math.tau/sides; f = rng.uniform(.72,1.12)
        angles.append((math.cos(a)*rx*f,math.sin(a)*ry*f))
    for factor in (.5,1.4):
        for dx,dy in angles:vs.append((x+dx*factor,y+dy*factor,z))
    fs=[(0,1+i,1+(i+1)%sides) for i in range(sides)]
    fs += [(1+i,1+sides+i,1+sides+(i+1)%sides,1+(i+1)%sides) for i in range(sides)]
    obj=mesh(name,vs,fs,mat,col)
    colors=obj.data.color_attributes.new(name='GroundFade',type='FLOAT_COLOR',domain='CORNER')
    for li,loop in enumerate(obj.data.loops):
        idx=loop.vertex_index;alpha=.50 if idx==0 else .30 if idx<=sides else 0
        colors.data[li].color=(1,1,1,alpha)
    obj.visible_shadow=False
    return obj

def stone(x,y,r,h,col,mat='Stone'):
    sides = rng.choice((5,6,7)); ang = rng.random()*math.tau
    ring = [(x+math.cos(ang+i*math.tau/sides)*r*rng.uniform(.8,1.1),
             y+math.sin(ang+i*math.tau/sides)*r*rng.uniform(.65,1), .003) for i in range(sides)]
    vs = ring + [(x+(p[0]-x)*.72,y+(p[1]-y)*.72,h) for p in ring]
    fs = [tuple(reversed(range(sides))),tuple(range(sides,sides*2))]
    fs += [(i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides)]
    return mesh('Loose aggregate',vs,fs,mat,col)

def ribbon(points, width, z, mat, col, label):
    vs=[]
    for i, p in enumerate(points):
        tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])
        tangent.normalize(); normal=Vector((-tangent.y,tangent.x))
        taper=max(.13,math.sin(math.pi*(i+.18)/(len(points)-.65)))
        w=width*taper*rng.uniform(.82,1.18)
        for sign in (-1,1):
            v=Vector(p)+normal*w*sign; vs.append((v.x,v.y,z))
    obj=mesh(label,vs,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(points)-1)],mat,col)
    obj.visible_shadow=False
    return obj

def crack(col, bent=False):
    points=[]
    for i in range(17):
        x=-1.35+i*.17
        y=.16*math.sin(i*1.29)+(.42*math.sin(i*.23) if bent else .04*i)
        points.append((x,y-.25))
    paths=[points]
    for at, sign in ((4,-1),(9,1),(13,-1)):
        x,y=points[at]
        paths.append([(x+i*.073+math.sin(i*2.4)*.045,y+sign*i*.105) for i in range(6)])
    for path in paths:
        ribbon(path,.019,.001,'CrackLip',col,'Chipped fracture rim')
        ribbon(path,.009,.0018,'Crack',col,'Fine fracture')
    for i in range(17):
        p=rng.choice(points); stone(p[0]+rng.uniform(-.09,.09),p[1]+rng.uniform(-.075,.075),rng.uniform(.009,.024),.011,col)

def chipped_slab(cx, cy, w, d, col, broken=False):
    # Straight rear joins; chipped, uneven front edge. Actual thickness, not paper cutouts.
    coords=[(-w/2,d/2),(w/2,d/2),(w/2,-d*.36)]
    for j in range(6):
        x=w/2-j*w/5
        depth=rng.uniform(.10,.31) if broken and j in (1,2,3) else rng.uniform(0,.065)
        coords.append((x,-d/2+depth))
    coords.append((-w/2,d*.22))
    vs=[(cx+x,cy+y,.008) for x,y in coords]+[(cx+x,cy+y,.072+rng.uniform(-.001,.001)) for x,y in coords]
    n=len(coords);fs=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    o=mesh('Chipped paving block',vs,fs,rng.choice(['Concrete','ConcreteDark']),col)
    bevel=o.modifiers.new('Worn arris','BEVEL');bevel.width=.008;bevel.segments=1
    bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.modifier_apply(modifier=bevel.name);o.select_set(False)

def edge(col,broken=False):
    for i in range(5):
        x=(i-2)*.72
        chipped_slab(x,.06,.70,.92,col,broken and i in (1,2,4))
        patch('Soil in exposed edge',x,-.44,.44,.15,.002,'Soil',col)
    for i in range(48):
        x=rng.uniform(-1.77,1.77);y=rng.uniform(-.72,-.35)
        stone(x,y,rng.uniform(.018,.07),rng.uniform(.018,.044),col)

def dirt(col):
    for i in range(48):
        x=rng.uniform(-1.7,1.7);y=rng.gauss(0,.25)
        if abs(y)>.67: continue
        patch('Dust deposit',x,y,rng.uniform(.07,.22),rng.uniform(.03,.12),.002+i*.000035,
              'Soil' if i%3 else 'DryDust',col)
    for i in range(86):
        x=rng.uniform(-1.9,1.9);y=rng.gauss(0,.3)
        if abs(y)>.8: continue
        stone(x,y,rng.uniform(.009,.049),rng.uniform(.007,.028),col)

def weeds(col):
    for i in range(27):
        x=rng.uniform(-1.45,1.45);y=rng.uniform(-.24,.21)
        patch('Wall dust',x,y,.14,.065,.002+i*.00008,'Soil',col)
        if i%3: continue
        for j in range(rng.randint(4,7)):
            ang=rng.random()*math.tau;h=rng.uniform(.105,.24);w=rng.uniform(.009,.019)
            dx,dy=math.cos(ang),math.sin(ang)
            vs=[(x-dy*w,y+dx*w,.006),(x+dy*w,y-dx*w,.006),
                (x+dx*h*.22+dy*w*.4,y+dy*h*.22-dx*w*.4,h*.62),
                (x+dx*h*.66,y+dy*h*.66,h),
                (x+dx*h*.22-dy*w*.4,y+dy*h*.22+dx*w*.4,h*.62)]
            # Real thin leaf shell: coincident reversed faces can disappear on FBX reimport.
            obj=mesh('Bent grass blade',vs,[(0,1,2,4),(4,2,3)],rng.choice(['Grass','Grass','DryGrass']),col)
            mod=obj.modifiers.new('Leaf thickness','SOLIDIFY');mod.thickness=.0015;mod.offset=0
            bpy.context.view_layer.objects.active=obj;bpy.ops.object.modifier_apply(modifier=mod.name)
    for i in range(18): stone(rng.uniform(-1.5,1.5),rng.uniform(-.24,.25),rng.uniform(.012,.035),.016,col)

assets={}
for name,fn in [('CrackBranch_A',lambda c:crack(c)),('CrackBranch_B',lambda c:crack(c,True)),
                ('BrokenPaving_A',lambda c:edge(c)),('BrokenPaving_B',lambda c:edge(c,True)),
                ('DirtGravel_Transition',dirt),('WallWeeds_Dust',weeds)]:
    col=collection(name+'_Editable');fn(col);assets[name]=col

def export_assets():
    report={}
    for name,col in assets.items():
        bpy.ops.object.select_all(action='DESELECT');copies=[]
        for o in col.objects:
            cp=o.copy();cp.data=o.data.copy();sc.collection.objects.link(cp);copies.append(cp);cp.select_set(True)
        bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();o=bpy.context.object;o.name=name
        me=o.data
        bm=bmesh.new();bm.from_mesh(me);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
        me.calc_loop_triangles();bad=sum(t.area<1e-11 for t in me.loop_triangles)
        if bad:raise RuntimeError(f'{name}: {bad} degenerate triangles')
        if len(me.uv_layers)!=1:raise RuntimeError('UV0 missing')
        me.calc_tangents(uvmap=me.uv_layers[0].name)
        bounds=[(min(v.co[a] for v in me.vertices),max(v.co[a] for v in me.vertices)) for a in range(3)]
        bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/f'{name}.fbx'),use_selection=True,
            object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,use_tspace=True)
        report[name]={'triangles':len(me.loop_triangles),'editablePieces':len(col.objects),'boundsXYZ':bounds,
                      'UV0':True,'tangents':True,'degenerateTriangles':bad,'rig':'none: static ground dressing'}
        bpy.data.objects.remove(o,do_unlink=True);col.hide_render=True;col.hide_viewport=True
    return report

report=export_assets()
gallery=collection('Review_KitOverview')
context=collection('Review_EntranceExample')
def instance(col,dest,loc=(0,0,0),angle=0):
    o=bpy.data.objects.new(col.name+'_Instance',None);dest.objects.link(o)
    o.instance_type='COLLECTION';o.instance_collection=col;o.location=loc;o.rotation_euler.z=angle
    # Source visibility controls collection instances too: move source collections off scene instead.
    return o

# Asset collections are data blocks referenced only through instances, no duplicate geometry in the view.
for col in assets.values():
    col.hide_render=False;col.hide_viewport=False;sc.collection.children.unlink(col)
for name,col in assets.items():
    own=bpy.data.scenes.new('Asset_'+name);own.collection.children.link(col);own.unit_settings.system='METRIC'

for i,(name,col) in enumerate(assets.items()):
    x=(.5-i%2)*4.6;y=(i//2-1)*2.6
    instance(col,gallery,(x,y,.015))
    cu=bpy.data.curves.new(name+'_Label','FONT');cu.body=f'{i+1:02d}  '+name.replace('_',' ')
    cu.size=.17;cu.align_x='CENTER';cu.extrude=0
    txt=bpy.data.objects.new(name+'_Label',cu);gallery.objects.link(txt);txt.location=(x,y+1.13,.012);txt.rotation_euler.z=math.pi
    txt.data.materials.append(materials['Concrete'])

def cube(name,loc,size,mat,col):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(o.users_collection):c.objects.unlink(o)
    col.objects.link(o);o.data.materials.append(materials[mat])
    uv=o.data.uv_layers.active
    for face in o.data.polygons:
        axis=max(range(3),key=lambda a:abs(face.normal[a]));axes=[a for a in range(3) if a!=axis]
        for li in face.loop_indices:
            v=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(v[axes[0]],v[axes[1]])
    return o

ground=collection('Review_GroundLightingCamera')
cube('Review asphalt only',(0,0,-.05),(200,200,.1),'Asphalt',ground)
# Existing repair facade is context only, not exported as a newly made building.
with bpy.data.libraries.load(str(TOWN/'BlenderSource~/Town02.blend'),link=False) as (src,dst):
    dst.collections=[n for n in ['Repair_Shell_Editable','Repair_Roof_Editable'] if n in src.collections]
for col in dst.collections:
    col.hide_render=False;col.hide_viewport=False;instance(col,context,(0,-7,0))

cube('Existing pavement context',(0,-1.15,.003),(14,1.7,.065),'ConcreteDark',context)
for x in (-5.2,-1.65,1.9,5.45):
    instance(assets['BrokenPaving_B' if x<0 else 'BrokenPaving_A'],context,(x,.09,0),math.pi)
    instance(assets['DirtGravel_Transition'],context,(x,.71,.005),rng.uniform(-.10,.10))
for x,y,angle,name in [(-4.7,1.65,-.3,'CrackBranch_A'),(3.4,2.0,.7,'CrackBranch_B'),
                        (-2.3,3.2,.2,'CrackBranch_B'),(5.6,3.9,-.5,'CrackBranch_A')]:
    instance(assets[name],context,(x,y,0),angle)
for x in (-5.2,4.8):instance(assets['WallWeeds_Dust'],context,(x,-1.65,.041))

sc.render.engine='CYCLES';sc.cycles.samples=24;sc.cycles.use_denoising=True
sc.render.resolution_x=1500;sc.render.resolution_y=1050;sc.render.resolution_percentage=100
sc.world=bpy.data.worlds.new('Overcast town review');sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.44,.47,.51,1)
sc.world.node_tree.nodes['Background'].inputs[1].default_value=.65
sc.view_settings.view_transform='AgX';sc.view_settings.exposure=.5
data=bpy.data.lights.new('Large soft daylight','AREA');data.energy=2500;data.shape='DISK';data.size=10
light=bpy.data.objects.new('Large soft daylight',data);ground.objects.link(light);light.location=(-7,2,15)
light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Review 62 degrees');cam=bpy.data.objects.new('Review 62 degrees',data);ground.objects.link(cam)
data.type='ORTHO';sc.camera=cam
def capture(name,target,scale,pitch=62):
    target=Vector(target);cam.location=target+Vector((0,math.cos(math.radians(pitch))*24,math.sin(math.radians(pitch))*24))
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
    sc.render.filepath=str(OUT/'Review'/f'{name}.png');bpy.ops.render.render(write_still=True)

context.hide_render=True;context.hide_viewport=True
capture('KitOverview62',(0,-.25,0),10.5)
gallery.hide_render=True;gallery.hide_viewport=True;context.hide_render=False;context.hide_viewport=False
capture('EntranceExample62',(0,-.6,.15),17)
capture('EdgeDetail62',(-3.6,.35,.04),6.8)
capture('SurfaceLowAngle',(-3.6,.45,.04),6.8,25)
# Save the useful assembled 62 degree view as the startup view.
target=Vector((0,-.6,.15));cam.location=target+Vector((0,math.cos(math.radians(62))*24,math.sin(math.radians(62))*24))
cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=17
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/GroundFinish62.blend'))
(OUT/'ModelValidation.json').write_text(json.dumps({'units':'metres','cameraPitch':62,'unityImported':False,
    'newAssets':report,'totalTriangles':sum(v['triangles'] for v in report.values()),
    'reusedContext':'Town02 Repair shell and roof; excluded from six FBX files',
    'surfaces':'Existing Town02 Base/Normal/Mask packed; nonmetallic; no emissive materials',
    'futureUnityIntegration':'Soil/DryDust require GroundFade vertex alpha in the destination material; FBX alone does not reproduce Blender nodes. Ground overlays must receive shadows but not cast offset shadows.'},indent=2))
print('GROUND_FINISH_COMPLETE',json.dumps(report))
