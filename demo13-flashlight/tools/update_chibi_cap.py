"""Patch the existing .blend, preserving the body, skeleton and all action keys.

blender --background --python tools/update_chibi_cap.py
"""
import bpy
import bmesh
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))
from chibi_cap import build_cap

source = ROOT/'ArtSource/ChibiSurvivor'
assets = ROOT/'Assets/ChibiSurvivor'
if (source/'ReclaimerBase.blend').exists():
    import runpy
    sys.argv.extend(['--part', 'cap'])
    runpy.run_path(str(Path(__file__).with_name('build_reclaimer.py')), run_name='__main__')
    raise SystemExit(0)
bpy.ops.wm.open_mainfile(filepath=str(source/'ChibiSurvivor.blend'))
rig = bpy.data.objects['ChibiSurvivor']
body = bpy.data.objects['ChibiSurvivor_Mesh']

def keys():
    return [(a.name, [(f.data_path, f.array_index, [(tuple(k.co), k.interpolation) for k in f.keyframe_points])
                     for f in a.fcurves]) for a in bpy.data.actions]

original_keys = keys()
original_bones = [(b.name, tuple(b.head_local), tuple(b.tail_local)) for b in rig.data.bones]
bm = bmesh.new()
bm.from_mesh(body.data)
hair_slots = {i for i,m in enumerate(body.data.materials) if m and m.name.startswith('Hair_')}
visited, remove = set(), []
for vertex in bm.verts:
    if vertex in visited:
        continue
    component, pending = [], [vertex]
    visited.add(vertex)
    while pending:
        current = pending.pop()
        component.append(current)
        for edge in current.link_edges:
            other = edge.other_vert(current)
            if other not in visited:
                visited.add(other)
                pending.append(other)
    faces = {face for v in component for face in v.link_faces}
    if faces and all(f.material_index in hair_slots for f in faces) and max(v.co.z for v in component) > 1.53:
        remove.extend(component)
bmesh.ops.delete(bm, geom=remove, context='VERTS')
bm.to_mesh(body.data)
bm.free()
old = bpy.data.objects.get('ChibiSurvivor_Cap')
if old:
    bpy.data.objects.remove(old, do_unlink=True)
cap = build_cap(rig)
assert keys() == original_keys, 'Animation keys changed'
assert original_bones == [(b.name, tuple(b.head_local), tuple(b.tail_local)) for b in rig.data.bones]
assert all(len(v.groups) == 1 and v.groups[0].weight == 1 for v in cap.data.vertices)

scene = bpy.context.scene
rig.animation_data.action = bpy.data.actions['Idle']
scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
for obj in (rig, body, cap):
    obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(assets/'ChibiSurvivor.fbx'), use_selection=True,
    object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0, mesh_smooth_type='FACE', use_mesh_modifiers=True)
bpy.ops.export_scene.gltf(filepath=str(source/'ChibiSurvivor.glb'), use_selection=True,
    export_format='GLB', export_animations=True, export_animation_mode='ACTIONS')
bpy.ops.wm.save_as_mainfile(filepath=str(source/'ChibiSurvivor.blend'))
scene.render.filepath = str(assets/'CapPreview.png')
bpy.ops.render.render(write_still=True)
print('CAP_UPDATE_PASS: separate headwear mesh; removed hair vertices:', len(remove),
      '; unchanged skeleton and animation keys; cap vertices:', len(cap.data.vertices))
