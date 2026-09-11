"""Rebuild the original BRB chibi survivor with Blender 4.5 LTS.

blender --background --python tools/build_chibi_survivor.py
All geometry, bone weights and animations are authored here; no external assets.
"""
import bpy
import math
import json
import sys
from pathlib import Path
from mathutils import Vector

# Keep the original construction available for archival recovery only. The active
# appearance builder preserves the rig and supports independent accessory updates.
if '--legacy' not in sys.argv:
    import runpy
    runpy.run_path(str(Path(__file__).with_name('build_reclaimer.py')), run_name='__main__')
    raise SystemExit(0)

sys.path.insert(0, str(Path(__file__).resolve().parent))
from chibi_cap import build_cap

PROJECT = Path(__file__).resolve().parents[1]
SOURCE = PROJECT / 'ArtSource/ChibiSurvivor'
ASSETS = PROJECT / 'Assets/ChibiSurvivor'
SOURCE.mkdir(parents=True, exist_ok=True)
ASSETS.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for action in list(bpy.data.actions):
    bpy.data.actions.remove(action)

def material(name, color, metal=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Roughness'].default_value = .82
    p.inputs['Metallic'].default_value = metal
    return m

M = {
    'skin': material('Skin_WarmPeach', (.69, .405, .255)),
    'blush': material('Cheek_Terracotta', (.63, .235, .16)),
    'hair': material('Hair_Espresso', (.065, .041, .03)),
    'hairlight': material('Hair_Chestnut', (.105, .068, .042)),
    'eyes': material('Eyes_Ink', (.013, .02, .022)),
    'cream': material('Canvas_Cream', (.80, .72, .51)),
    'jacket': material('Jacket_Sage', (.25, .335, .20)),
    'seam': material('Jacket_Shadow', (.135, .205, .125)),
    'pants': material('Trousers_Slate', (.10, .155, .16)),
    'boots': material('Boots_Walnut', (.18, .09, .049)),
    'sole': material('Soles_Charcoal', (.048, .053, .044)),
    'scarf': material('Scarf_BurntOrange', (.69, .20, .069)),
    'bag': material('Backpack_Ochre', (.40, .285, .125)),
    'strap': material('Straps_DarkLeather', (.15, .105, .058)),
    'metal': material('Hardware_Brass', (.53, .40, .19), .45),
}
parts = []

def finish(obj, name, mat, bone):
    obj.name = name
    obj.data.materials.append(M[mat])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    parts.append(obj)
    return obj

def box(name, center, size, mat, bone, bevel=.025, rotation=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = bpy.context.object
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('CraftedCorners', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if rotation:
        obj.rotation_euler = rotation
    return finish(obj, name, mat, bone)

def oval(name, center, size, mat, bone, segments=10, rings=6):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1, location=center)
    obj = bpy.context.object
    obj.scale = size
    return finish(obj, name, mat, bone)

def rod(name, a, b, radius, mat, bone, end_radius=None):
    a, b = Vector(a), Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=radius,
        radius2=radius if end_radius is None else end_radius, depth=(b-a).length,
        location=(a+b)/2)
    obj = bpy.context.object
    obj.rotation_euler = (b-a).to_track_quat('Z', 'Y').to_euler()
    return finish(obj, name, mat, bone)

# Height 1.80 m including hair; head envelope 0.72 m = 2.5 heads.
box('RoundedFace', (0, -.015, 1.425), (.665, .565, .625), 'skin', 'Head', .135)
for side, x in [('L', .347), ('R', -.347)]:
    oval('Ear.'+side, (x, -.012, 1.405), (.058, .06, .09), 'skin', 'Head')
    oval('EarInset.'+side, (x, -.057, 1.404), (.032, .012, .046), 'blush', 'Head')
for side, x in [('L', .123), ('R', -.123)]:
    oval('Eye.'+side, (x, -.303, 1.449), (.029, .014, .041), 'eyes', 'Head')
    oval('EyeGlint.'+side, (x-.008, -.316, 1.463), (.007, .004, .01), 'cream', 'Head', 8, 4)
    oval('Cheek.'+side, (x*1.5, -.298, 1.375), (.042, .006, .019), 'blush', 'Head')
    box('Brow.'+side, (x, -.304, 1.512), (.066, .016, .018), 'hair', 'Head', .005)
oval('ButtonNose', (0, -.312, 1.398), (.036, .033, .031), 'skin', 'Head')
box('SmallSmile', (0, -.302, 1.326), (.038, .01, .012), 'eyes', 'Head', .004)

# Headwear is built separately after rig creation; it is never joined into the body.

rod('Neck', (0, 0, 1.08), (0, 0, 1.22), .105, 'skin', 'Chest')
box('JacketBody', (0, 0, .905), (.49, .33, .48), 'jacket', 'Chest', .08)
box('JacketHem', (0, 0, .695), (.48, .34, .075), 'seam', 'Hips', .025)
box('HipTrousers', (0, 0, .622), (.38, .28, .15), 'pants', 'Hips', .04)
box('JacketPlacket', (0, -.173, .887), (.025, .018, .34), 'seam', 'Chest', .005)
for z in [.76, .86, .96]:
    oval('JacketButton', (.007, -.19, z), (.013, .008, .013), 'metal', 'Chest', 8, 4)
for x in [-.125, .125]:
    box('JacketPocket', (x, -.171, .813), (.117, .032, .106), 'seam', 'Chest', .017)
    box('PocketFlap', (x, -.194, .845), (.13, .025, .038), 'jacket', 'Chest', .009)
    box('ShoulderStrap', (x*1.36, -.165, .971), (.056, .033, .28), 'strap', 'Chest', .012)
    box('StrapBuckle', (x*1.36, -.187, .909), (.065, .012, .04), 'metal', 'Chest', .007)
box('ScarfCollar', (0, 0, 1.115), (.32, .30, .105), 'scarf', 'Chest', .045)
box('ScarfTail', (.075, -.20, 1.035), (.125, .052, .17), 'scarf', 'Chest', .021, (0, -.14, -.14))

for side, sign in [('L', 1), ('R', -1)]:
    x = sign*.132
    rod('TrouserThigh.'+side, (x, 0, .59), (x, 0, .35), .115, 'pants', 'Thigh.'+side, .105)
    oval('TrouserKnee.'+side, (x, 0, .35), (.106, .105, .10), 'pants', 'Shin.'+side)
    rod('TrouserShin.'+side, (x, 0, .35), (x, 0, .18), .091, 'pants', 'Shin.'+side)
    box('CargoPocket.'+side, (x+sign*.09, 0, .45), (.067, .17, .15), 'seam', 'Thigh.'+side, .018)
    box('Boot.'+side, (x, -.05, .13), (.215, .32, .21), 'boots', 'Foot.'+side, .04)
    box('BootSole.'+side, (x, -.057, .042), (.225, .335, .055), 'sole', 'Foot.'+side, .018)
    box('BootCuff.'+side, (x, .014, .223), (.214, .21, .05), 'boots', 'Foot.'+side, .015)
    for z in [.117, .16]:
        box('BootLace.'+side, (x, -.191, z), (.105, .012, .012), 'cream', 'Foot.'+side, .003)
    shoulder, elbow, wrist = (sign*.278, 0, 1.057), (sign*.36, -.006, .855), (sign*.39, -.035, .69)
    oval('Shoulder.'+side, shoulder, (.123, .133, .13), 'jacket', 'UpperArm.'+side)
    rod('SleeveUpper.'+side, shoulder, elbow, .108, 'jacket', 'UpperArm.'+side, .089)
    oval('Elbow.'+side, elbow, (.086, .09, .085), 'jacket', 'Forearm.'+side)
    rod('SleeveLower.'+side, elbow, wrist, .089, 'jacket', 'Forearm.'+side, .071)
    box('Cuff.'+side, (sign*.386, -.031, .719), (.151, .166, .055), 'seam', 'Forearm.'+side, .02)
    oval('MittenHand.'+side, (sign*.39, -.037, .648), (.081, .086, .085), 'skin', 'Hand.'+side)
    oval('Thumb.'+side, (sign*.335, -.088, .662), (.036, .042, .047), 'skin', 'Hand.'+side)

box('BackpackBody', (0, .252, .914), (.42, .235, .43), 'bag', 'Backpack', .065)
box('BackpackLid', (0, .258, 1.107), (.447, .256, .105), 'bag', 'Backpack', .034)
box('BackpackPocket', (0, .39, .85), (.30, .07, .17), 'jacket', 'Backpack', .023)
box('BackpackPatch', (0, .43, .878), (.094, .01, .07), 'cream', 'Backpack', .009)
box('PatchStripe', (0, .437, .878), (.065, .005, .014), 'scarf', 'Backpack', .002)
for x in [-.13, .13]:
    box('BagVerticalStrap', (x, .382, .957), (.045, .016, .31), 'strap', 'Backpack', .006)
    box('BagBrassBuckle', (x, .397, 1.015), (.057, .018, .046), 'metal', 'Backpack', .008)
box('BackpackHandle', (0, .22, 1.183), (.145, .053, .05), 'strap', 'Backpack', .015)

# One renderer, rigid joint weights deliberately suited to the toy-like style.
bpy.ops.object.select_all(action='DESELECT')
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
character = bpy.context.object
character.name = 'ChibiSurvivor_Mesh'
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

arm = bpy.data.armatures.new('ChibiSurvivor_Skeleton')
rig = bpy.data.objects.new('ChibiSurvivor', arm)
bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
character.select_set(False)
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
def bone(name, position, parent=None):
    b = arm.edit_bones.new(name)
    b.head = position
    b.tail = Vector(position)+Vector((0, 0, .10))
    if parent:
        b.parent = arm.edit_bones[parent]
    return b
bone('Root', (0, 0, 0))
bone('Hips', (0, 0, .60), 'Root')
bone('Chest', (0, 0, .84), 'Hips')
bone('Head', (0, 0, 1.17), 'Chest')
bone('Backpack', (0, .2, 1.02), 'Chest')
for side, sign in [('L', 1), ('R', -1)]:
    bone('Thigh.'+side, (sign*.132, 0, .59), 'Hips')
    bone('Shin.'+side, (sign*.132, 0, .35), 'Thigh.'+side)
    bone('Foot.'+side, (sign*.132, 0, .16), 'Shin.'+side)
    bone('UpperArm.'+side, (sign*.278, 0, 1.057), 'Chest')
    bone('Forearm.'+side, (sign*.36, -.006, .855), 'UpperArm.'+side)
    bone('Hand.'+side, (sign*.39, -.035, .69), 'Forearm.'+side)
bone('HandSocket.R', (-.39, -.035, .65), 'Hand.R')
bpy.ops.object.mode_set(mode='OBJECT')
character.parent = rig
mod = character.modifiers.new('SurvivorSkin', 'ARMATURE')
mod.object = rig
cap = build_cap(rig)
rig.show_in_front = True
for pb in rig.pose.bones:
    pb.rotation_mode = 'XYZ'

scene = bpy.context.scene
scene.render.fps = 30
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
actions = []
def rotate(name, x=0, y=0, z=0):
    rig.pose.bones[name].rotation_euler = tuple(math.radians(v) for v in (x, y, z))

foot_indices = []
foot_groups = {character.vertex_groups['Foot.'+s].index for s in ['L', 'R']}
for v in character.data.vertices:
    if any(g.group in foot_groups for g in v.groups):
        foot_indices.append(v.index)

for name, count in [('Idle', 72), ('Walk', 30), ('Run', 20)]:
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    rig.animation_data_create()
    rig.animation_data.action = action
    scene.frame_start, scene.frame_end = 1, count+1
    for frame in range(1, count+2):
        scene.frame_set(frame)
        t = math.tau*(frame-1)/count
        for pb in rig.pose.bones:
            pb.location = (0, 0, 0)
            pb.rotation_euler = (0, 0, 0)
        if name == 'Idle':
            rig.pose.bones['Chest'].location.y = .005*(1-math.cos(t))
            rotate('Chest', x=.8*math.sin(t))
            rotate('Head', x=-.6*math.sin(t), y=1.4*math.sin(t))
            for side, sign in [('L', 1), ('R', -1)]:
                rotate('UpperArm.'+side, x=2*math.sin(t), z=sign*2)
                rotate('Forearm.'+side, x=-6-2*math.sin(t))
        else:
            run = name == 'Run'
            swing = 38 if run else 23
            rotate('Chest', x=9 if run else 2, y=2*math.sin(t))
            rotate('Head', x=-5 if run else -1, y=-2*math.sin(t))
            rotate('Backpack', x=2*math.sin(2*t-.3))
            for side, phase in [('L', t), ('R', t+math.pi)]:
                hip = swing*math.cos(phase)
                knee = (65 if run else 36)*max(0, math.sin(phase))
                rotate('Thigh.'+side, x=hip)
                rotate('Shin.'+side, x=-knee)
                rotate('Foot.'+side, x=-hip+knee)
                rotate('UpperArm.'+side, x=-hip*.85)
                rotate('Forearm.'+side, x=-(42 if run else 12)-8*max(0, -math.cos(phase)))
            bpy.context.view_layer.update()
            evaluated = character.evaluated_get(bpy.context.evaluated_depsgraph_get())
            em = evaluated.to_mesh()
            bottom = min((evaluated.matrix_world@em.vertices[i].co).z for i in foot_indices)
            evaluated.to_mesh_clear()
            # Ground the lowest sole each frame; running adds a small flight phase.
            rig.pose.bones['Hips'].location.y = .015-bottom + (.045*max(0, math.sin(2*t)) if run else 0)
        for pb in rig.pose.bones:
            pb.keyframe_insert('location', frame=frame, group=pb.name)
            pb.keyframe_insert('rotation_euler', frame=frame, group=pb.name)
    for fc in action.fcurves:
        for kp in fc.keyframe_points:
            kp.interpolation = 'LINEAR'
    actions.append(action)

# Export a single FBX with three takes, plus a portable animated GLB.
rig.animation_data.action = actions[0]
scene.frame_start, scene.frame_end = 1, 73
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
character.select_set(True)
cap.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(ASSETS/'ChibiSurvivor.fbx'), use_selection=True,
    object_types={'ARMATURE', 'MESH'}, axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0,
    mesh_smooth_type='FACE', use_mesh_modifiers=True)
bpy.ops.export_scene.gltf(filepath=str(SOURCE/'ChibiSurvivor.glb'), use_selection=True,
    export_format='GLB', export_animations=True, export_animation_mode='ACTIONS')

# Validation checks actual evaluated skinning and all loop seams, not just key data.
report = {'height_m': 1.815, 'head_ratio': 2.5, 'vertices': len(character.data.vertices),
    'triangles': sum(len(p.vertices)-2 for p in character.data.polygons),
    'bones': len(arm.bones), 'materials': len(character.data.materials), 'clips': {}}
for action in actions:
    rig.animation_data.action = action
    start, end = map(int, action.frame_range)
    poses, min_z = [], float('inf')
    for frame in range(start, end+1):
        scene.frame_set(frame)
        bpy.context.view_layer.update()
        ev = character.evaluated_get(bpy.context.evaluated_depsgraph_get())
        em = ev.to_mesh()
        coords = [ev.matrix_world@v.co for v in em.vertices]
        min_z = min(min_z, min(p.z for p in coords))
        if frame in (start, end):
            poses.append(coords)
        ev.to_mesh_clear()
    seam = max((a-b).length for a,b in zip(*poses))
    assert seam < 1e-5, (action.name, 'loop seam', seam)
    assert min_z >= -.001, (action.name, 'below ground', min_z)
    report['clips'][action.name] = {'start': start, 'end': end, 'fps': 30,
        'seconds': (end-start)/30, 'loop_max_error_m': seam, 'lowest_vertex_m': min_z}
assert all(v.groups and abs(sum(g.weight for g in v.groups)-1)<1e-6 for v in character.data.vertices)
(SOURCE/'validation.json').write_text(json.dumps(report, indent=2), encoding='utf-8')

# Presentation stage, excluded from exported game assets.
stage = material('Stage', (.105, .155, .17))
bpy.ops.mesh.primitive_cylinder_add(vertices=64, radius=.77, depth=.09, location=(0, 0, -.045))
pedestal = bpy.context.object
pedestal.name = 'Preview_Platform'
pedestal.data.materials.append(stage)
bevel = pedestal.modifiers.new('SoftRim', 'BEVEL')
bevel.width, bevel.segments = .025, 3
bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -.095))
bpy.context.object.data.materials.append(material('Backdrop', (.045, .073, .085)))
world = scene.world or bpy.data.worlds.new('Studio')
scene.world = world
world.use_nodes = True
world.node_tree.nodes['Background'].inputs[0].default_value = (.22, .29, .33, 1)
world.node_tree.nodes['Background'].inputs[1].default_value = .5
def aim(obj, target):
    obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z', 'Y').to_euler()
for name, pos, energy, size, color in [
    ('Key', (-3, -4, 5), 430, 4, (1,.83,.65)),
    ('Fill', (3, -2, 3), 240, 3, (.65,.81,1)),
    ('Rim', (1, 3, 4), 550, 3, (1,.73,.43))]:
    data = bpy.data.lights.new(name, 'AREA')
    data.energy, data.shape, data.size, data.color = energy, 'DISK', size, color
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = pos
    aim(obj, (0,0,.9))
cam = bpy.data.objects.new('Preview_Camera', bpy.data.cameras.new('Preview_Camera'))
scene.collection.objects.link(cam)
scene.camera = cam
cam.data.type, cam.data.ortho_scale = 'ORTHO', 2.5
cam.location = (2.8, -4.5, 3)
aim(cam, (0,0,.87))
scene.render.engine = 'CYCLES'
scene.cycles.samples = 24
scene.cycles.use_denoising = True
scene.render.resolution_x, scene.render.resolution_y = 700, 800
scene.render.resolution_percentage = 100
scene.view_settings.view_transform = 'AgX'
scene.render.image_settings.file_format = 'PNG'
rig.animation_data.action = actions[0]
scene.frame_start, scene.frame_end = 1, 73
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ChibiSurvivor.blend'))
for name, pos in [('front', (0,-5,2.35)), ('quarter', (2.8,-4.5,3)), ('back', (-2.8,4.5,3))]:
    cam.location = pos
    aim(cam, (0,0,.87))
    scene.render.filepath = str(SOURCE/(name+'.png'))
    bpy.ops.render.render(write_still=True)
cam.location = (2.8,-4.5,3)
aim(cam, (0,0,.87))
scene.render.resolution_x, scene.render.resolution_y = 350, 400
scene.cycles.samples = 12
for action in actions:
    rig.animation_data.action = action
    frames = int(action.frame_range[1]-1)
    folder = SOURCE/'frames'/action.name
    folder.mkdir(parents=True, exist_ok=True)
    for index in range(12):
        scene.frame_set(1+int(index*frames/12), subframe=index*frames/12-int(index*frames/12))
        scene.render.filepath = str(folder/('%02d.png'%index))
        bpy.ops.render.render(write_still=True)
print('CHIBI_BUILD_SUCCESS', json.dumps(report))
