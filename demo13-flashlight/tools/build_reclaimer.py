"""Modular BRB recovery worker. Reuses the existing skeleton and all action keys.

blender --background --python tools/build_reclaimer.py
Append -- --part watch|kit|cap to regenerate just that accessory.
Appearance settings and the accessory builders are deliberately independent.
"""
import bpy
import math
import sys
import shutil
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT/'ArtSource/ChibiSurvivor'
ASSETS = ROOT/'Assets/ChibiSurvivor'
sys.path.insert(0, str(Path(__file__).resolve().parent))
from chibi_cap import build_cap

LOOK = {
    'head_scale': (1.16, 1.12, 1.30),
    'skin': (.48, .34, .225), 'coat': (.155, .17, .12),
    'patch': (.245, .245, .175), 'cloth': (.115, .125, .115),
    'leather': (.11, .075, .044), 'metal': (.19, .18, .135),
    'dial': (.60, .54, .36), 'ink': (.018, .025, .021),
    'bag': (.22, .205, .14), 'cap': (.095, .11, .095),
}
part = sys.argv[sys.argv.index('--part')+1] if '--part' in sys.argv else 'all'
baseline = SOURCE/'ReclaimerBase.blend'
if not baseline.exists():
    shutil.copy2(SOURCE/'ChibiSurvivor.blend', baseline)
bpy.ops.wm.open_mainfile(filepath=str(baseline if part == 'all' else SOURCE/'ChibiSurvivor.blend'))
rig = bpy.data.objects['ChibiSurvivor']

def action_signature():
    return [(a.name, [(c.data_path, c.array_index, [tuple(k.co) for k in c.keyframe_points])
                     for c in a.fcurves]) for a in bpy.data.actions]

original_keys = action_signature()
original_bones = [(b.name, tuple(b.head_local), tuple(b.tail_local)) for b in rig.data.bones]

def material(name, rgb):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*rgb, 1)
    p.inputs['Roughness'].default_value = .91
    return m

M = {key: material('Recovery_'+key, value) for key,value in LOOK.items() if key != 'head_scale'}

def skin(obj, bone):
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    obj.parent = rig
    mod = obj.modifiers.new('SharedRig', 'ARMATURE')
    mod.object = rig
    return obj

def box(name, center, size, mat, bone, bevel=.012, rot=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('SoftEdges','BEVEL')
        mod.width, mod.segments = bevel, 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.rotation_euler = rot
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.data.materials.append(M[mat])
    return skin(obj, bone)

def rod(name, a, b, radius, mat, bone, sides=8):
    a,b = Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cylinder_add(vertices=sides, radius=radius, depth=(b-a).length, location=(a+b)/2)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = (b-a).to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.data.materials.append(M[mat])
    return skin(obj,bone)

def combine(prefix, name):
    objects = [o for o in rig.children if o.type == 'MESH' and o.name.startswith(prefix)]
    if not objects: return
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects: obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    bpy.context.object.name = name
    bpy.context.object['part'] = name

def clear_part(name):
    obj = bpy.data.objects.get(name)
    if obj: bpy.data.objects.remove(obj, do_unlink=True)

def head_position(co):
    x,y,z = co
    sx,sy,sz = LOOK['head_scale']
    return (x*sx, y*sy, 1.17+(z-1.17)*sz)

def remodel_body():
    obj = bpy.data.objects['ChibiSurvivor_Mesh']
    data = obj.data
    bone_names = {g.index:g.name for g in obj.vertex_groups}
    buckets = {}
    for face in data.polygons:
        verts = [data.vertices[i] for i in face.vertices]
        bone = bone_names[verts[0].groups[0].group]
        mat = data.materials[face.material_index]
        matname = mat.name
        # Remove blush, glossy eye dots, the bright scarf and branded backpack badge.
        if matname == 'Scarf_BurntOrange': continue
        if bone == 'Head' and matname == 'Canvas_Cream': continue
        if bone == 'Head' and matname == 'Cheek_Terracotta' and all(v.co.y < -.20 for v in verts): continue
        if bone == 'Backpack' and matname == 'Canvas_Cream': continue
        if bone == 'Head': category = 'Head'
        elif bone == 'Backpack': category = 'Backpack'
        elif bone.startswith('Hand.'): category = 'Hands'
        elif bone.startswith('Foot.'): category = 'Boots'
        elif bone.startswith(('Thigh.','Shin.')): category = 'Trousers'
        else: category = 'Jacket'
        buckets.setdefault(category, []).append(face)
    for category, polygons in buckets.items():
        indices = sorted({i for f in polygons for i in f.vertices})
        remap = {old:new for new,old in enumerate(indices)}
        coords = []
        for i in indices:
            co = data.vertices[i].co.copy()
            if category == 'Head': co = Vector(head_position(co))
            if category == 'Jacket': co.x *= 1.045
            if category == 'Backpack':
                co.y = .2+(co.y-.2)*1.25
                co.z -= .04 + .05*(co.x+.22)
            coords.append(co)
        mesh = bpy.data.meshes.new('Recovery_'+category)
        mesh.from_pydata(coords, [], [tuple(remap[i] for i in f.vertices) for f in polygons])
        used_materials = sorted({f.material_index for f in polygons})
        for slot in used_materials: mesh.materials.append(data.materials[slot])
        for face, original in zip(mesh.polygons, polygons):
            face.material_index = used_materials.index(original.material_index)
        name = 'ChibiSurvivor_Mesh' if category == 'Jacket' else 'Recovery_'+category
        child = bpy.data.objects.new(name+'_New', mesh)
        bpy.context.collection.objects.link(child)
        child.parent = rig
        for bone in set(bone_names[g.group] for i in indices for g in data.vertices[i].groups):
            group = child.vertex_groups.new(name=bone)
            for old in indices:
                for g in data.vertices[old].groups:
                    if bone_names[g.group] == bone: group.add([remap[old]], g.weight, 'REPLACE')
        mod = child.modifiers.new('SharedRig','ARMATURE')
        mod.object = rig
        child['part'] = category.lower()
        child['final_name'] = name
    bpy.data.objects.remove(obj, do_unlink=True)
    for child in rig.children:
        if 'final_name' in child:
            child.name = child['final_name']
    palette = {
        'Skin_WarmPeach': LOOK['skin'], 'Cheek_Terracotta': (.33,.20,.135),
        'Eyes_Ink': LOOK['ink'], 'Jacket_Sage': LOOK['coat'],
        'Jacket_Shadow': (.10,.115,.078), 'Trousers_Slate': (.10,.115,.105),
        'Boots_Walnut': LOOK['leather'], 'Soles_Charcoal': (.04,.048,.036),
        'Canvas_Cream': (.33,.32,.23), 'Backpack_Ochre': LOOK['bag'],
        'Straps_DarkLeather': (.10,.076,.048), 'Hardware_Brass': LOOK['metal'],
    }
    for name,color in palette.items():
        if bpy.data.materials.get(name): material(name,color)

def build_clothes():
    # Dust collar, asymmetrical repairs and restrained broad wear marks, no noisy texture.
    box('Clothes_Collar', (0,-.018,1.108), (.345,.30,.125), 'cloth','Chest', .045)
    box('Clothes_CollarFold', (.065,-.18,1.072), (.21,.034,.068), 'cloth','Chest', .015, (0,.1,.06))
    box('Clothes_ChestRepair', (-.125,-.20,.983), (.14,.015,.10), 'patch','Chest', .008, (0,-.09,0))
    box('Clothes_LowerRepair', (.15,-.19,.759), (.11,.018,.083), 'cloth','Chest', .007, (0,.12,0))
    for x,z in [(-.185,.955),(-.065,.955),(-.185,1.01),(-.065,1.01)]:
        box('Clothes_Stitch', (x,-.215,z), (.007,.008,.027), 'dial','Chest', .001)
    box('Clothes_KneePatch', (.132,-.105,.39), (.145,.019,.13), 'patch','Shin.L', .012, (0,.10,0))
    box('Clothes_KneePatch2', (-.132,-.105,.39), (.139,.016,.095), 'cloth','Shin.R', .006)
    box('Clothes_ElbowRepair', (-.373,.027,.845), (.042,.168,.095), 'patch','Forearm.R', .012)
    for side,sign in [('L',1),('R',-1)]:
        box('Clothes_BootScuff', (sign*.132,-.213,.074), (.14,.006,.026), 'patch','Foot.'+side,.002)
    # One old facial cut; eyes remain matte, without blush or sparkles.
    a,b = head_position((-.205,-.301,1.46)), head_position((-.182,-.302,1.413))
    rod('Clothes_OldScar',a,b,.006,'leather','Head',6)
    combine('Clothes_','Recovery_Repairs')

def build_watch():
    clear_part('Recovery_Wristwatch')
    bone = 'Forearm.L'
    box('Watch_Strap', (.389,-.033,.740), (.166,.183,.050), 'leather',bone,.012)
    rod('Watch_Case',(.39,-.125,.741),(.39,-.151,.741),.063,'metal',bone,12)
    rod('Watch_Face',(.39,-.152,.741),(.39,-.156,.741),.049,'dial',bone,12)
    rod('Watch_Minute',(.39,-.159,.741),(.39,-.159,.777),.004,'ink',bone,6)
    rod('Watch_Hour',(.39,-.160,.741),(.414,-.160,.733),.005,'ink',bone,6)
    for dx,dz in [(0,.043),(.043,0),(0,-.043),(-.043,0)]:
        box('Watch_Mark',(.39+dx,-.160,.741+dz),(.005,.004,.006),'ink',bone,.001)
    rod('Watch_Crown',(.448,-.139,.741),(.464,-.139,.741),.012,'metal',bone)
    combine('Watch_','Recovery_Wristwatch')

def build_kit():
    clear_part('Recovery_FieldKit')
    bone = 'Backpack'
    rod('Kit_Bedroll',(-.25,.39,1.14),(.23,.39,1.14),.115,'cloth',bone,10)
    for x in [-.16,.13]:
        rod('Kit_RollStrap',(x-.015,.39,1.14),(x+.015,.39,1.14),.12,'leather',bone,10)
    box('Kit_BagRepair',(-.072,.465,.834),(.14,.015,.13),'patch',bone,.006, (0,.1,.14))
    for x in [-.12,-.025]:
        for z in [.785,.879]: box('Kit_BagStitch',(x,.477,z),(.008,.006,.026),'dial',bone,.001)
    box('Kit_RecoveryCase',(.277,.28,.88),(.115,.20,.25),'metal',bone,.018)
    box('Kit_CaseLatch',(.341,.273,.94),(.022,.074,.047),'leather',bone,.005)
    rod('Kit_Prybar',(-.265,.37,.70),(-.295,.37,1.15),.022,'metal',bone)
    rod('Kit_PrybarHook',(-.295,.37,1.15),(-.24,.37,1.18),.022,'metal',bone)
    rod('Kit_PrybarGrip',(-.265,.37,.70),(-.275,.37,.84),.029,'leather',bone)
    combine('Kit_','Recovery_FieldKit')

def build_headwear():
    clear_part('ChibiSurvivor_Cap')
    cap = build_cap(rig)
    for vertex in cap.data.vertices:
        vertex.co = head_position(vertex.co)
    material('Cap_DeepTeal', LOOK['cap'])
    material('Cap_Underbrim', (.042,.052,.037))
    material('Cap_Badge', (.285,.275,.20))
    material('Cap_BadgeMark', (.14,.15,.102))

if part == 'all':
    remodel_body()
    build_clothes()
    build_watch()
    build_kit()
    build_headwear()
elif part == 'watch': build_watch()
elif part == 'kit': build_kit()
elif part == 'cap': build_headwear()
else: raise ValueError('Unknown part: '+part)

assert action_signature() == original_keys, 'Animation keys changed'
assert original_bones == [(b.name, tuple(b.head_local), tuple(b.tail_local)) for b in rig.data.bones]
meshes = [obj for obj in rig.children if obj.type == 'MESH']
for obj in meshes:
    assert all(v.groups and abs(sum(g.weight for g in v.groups)-1)<1e-5 for v in obj.data.vertices), obj.name

scene = bpy.context.scene
rig.animation_data.action = bpy.data.actions['Idle']
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
for obj in meshes: obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(ASSETS/'ChibiSurvivor.fbx'), use_selection=True,
    object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0, mesh_smooth_type='FACE', use_mesh_modifiers=True)
bpy.ops.export_scene.gltf(filepath=str(SOURCE/'ChibiSurvivor.glb'), use_selection=True,
    export_format='GLB', export_animations=True, export_animation_mode='ACTIONS')
material('Backdrop',(.045,.05,.043))
material('Stage',(.13,.14,.12))
scene.camera.data.ortho_scale = 2.68
target = Vector((0,0,.97))
scene.camera.rotation_euler = (target-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x, scene.render.resolution_y = 800, 900
scene.cycles.samples = 24
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ChibiSurvivor.blend'))
scene.render.filepath = str(ASSETS/'RecoveryPreview.png')
bpy.ops.render.render(write_still=True)
scene.camera.location = (-2.8,4.5,3)
scene.camera.rotation_euler = (target-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath = str(ASSETS/'RecoveryBack.png')
bpy.ops.render.render(write_still=True)
print('RECLAIMER_PASS:', len(meshes), 'independently skinned parts;',
      sum(len(o.data.vertices) for o in meshes), 'vertices; original bones and action keys preserved')
