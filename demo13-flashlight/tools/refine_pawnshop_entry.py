"""Remove the pawnshop canopy in its editable Blender source and re-export only its shell.
Preserve original UVs/material names and all other town models. Unity imports via MCP.
"""
import bpy, json, math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
KIT = ROOT / 'Assets/Art/Environments/Town02'
REVIEW = ROOT.parent / 'ArtWork/PawnshopEntryReview'
SOURCE = KIT / 'BlenderSource~/Town02.blend'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
bpy.context.preferences.filepaths.save_version = 0
col = bpy.data.collections['Pawnshop_Shell_Editable']
layout = bpy.data.collections.get('Town02_Editable_Layout')
awnings = [o for o in col.objects if o.name.startswith(('Awning slat', 'Awning bracket'))]
removed_names = [o.name for o in awnings]
removed_data = {o.data for o in awnings}
for o in list(bpy.data.objects):
    if o in awnings or (layout and o.name in layout.objects and o.data in removed_data):
        bpy.data.objects.remove(o, do_unlink=True)

# Flat visual inset points at the open doorway; never export it as a physics step.
old = next((o for o in col.objects if o.name.startswith('Entry stone inset')), None)
if old is None:
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 6.42, .049))
    inset = bpy.context.object
    inset.name = 'Entry stone inset'
    inset.dimensions = (1.56, 1.12, .012)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = inset.modifiers.new('Soft edge', 'BEVEL'); bevel.width = .0024; bevel.segments = 1
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    inset.data.materials.append(bpy.data.materials['Town_Concrete'])
    for c in list(inset.users_collection): c.objects.unlink(inset)
    col.objects.link(inset)
    for uv in list(inset.data.uv_layers): inset.data.uv_layers.remove(uv)
    uv = inset.data.uv_layers.new(name='MetreUV'); uv.active_render = True
    for face in inset.data.polygons:
        major = max(range(3), key=lambda i: abs(face.normal[i]))
        axes = [i for i in range(3) if i != major]
        for li in face.loop_indices:
            v = inset.data.vertices[inset.data.loops[li].vertex_index].co
            uv.data[li].uv = (v[axes[0]], v[axes[1]])
    if layout:
        copy = inset.copy(); layout.objects.link(copy); copy.location = (6, -16, 0)

# Keep the inset 1 cm above the existing 4.5 cm pavement overlay.
inset = next(o for o in col.objects if o.name.startswith('Entry stone inset'))
center_z = (min(v.co.z for v in inset.data.vertices) + max(v.co.z for v in inset.data.vertices)) / 2
for v in inset.data.vertices: v.co.z += .049 - center_z
if not removed_names and (REVIEW/'BlenderValidation.json').exists():
    removed_names = json.loads((REVIEW/'BlenderValidation.json').read_text()).get('removedCanopyPieces', [])

assert not any(o.name.startswith(('Awning slat', 'Awning bracket')) for o in col.objects)
copies = []
col.hide_viewport = False
bpy.ops.object.select_all(action='DESELECT')
for o in col.objects:
    copy = o.copy(); copy.data = o.data.copy(); bpy.context.scene.collection.objects.link(copy)
    copies.append(copy)
for o in copies: o.select_set(True)
bpy.context.view_layer.objects.active = copies[0]
bpy.ops.object.join()
joined = bpy.context.object; joined.name = 'Pawnshop_Shell'
for face in joined.data.polygons: face.use_smooth = True
normal = joined.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL'); normal.keep_sharp = True
bpy.ops.object.modifier_apply(modifier=normal.name)
mesh = joined.data
assert len(mesh.uv_layers) == 1
assert all(math.isfinite(v) for vertex in mesh.vertices for v in vertex.co)
assert all(face.area > 0 for face in mesh.polygons)
report = dict(removedCanopyPieces=removed_names, sourcePieces=len(col.objects),
              triangles=sum(len(p.vertices)-2 for p in mesh.polygons), uvLayers=len(mesh.uv_layers),
              materialNames=[m.name for m in mesh.materials], floorInsetTop=.055,
              finiteVertices=True, positiveFaceAreas=True)
bpy.ops.export_scene.fbx(filepath=str(KIT/'Models/Pawnshop_Shell.fbx'), use_selection=True,
                        object_types={'MESH'}, axis_forward='-Z', axis_up='Y', bake_anim=False, add_leaf_bones=False)
bpy.data.objects.remove(joined, do_unlink=True)
col.hide_viewport = True
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE))
manifest_path = KIT/'KitManifest.json'
manifest = json.loads(manifest_path.read_text())
manifest['assets']['Pawnshop_Shell'] = dict(triangles=report['triangles'], sourcePieces=report['sourcePieces'])
manifest_path.write_text(json.dumps(manifest, indent=2))
REVIEW.mkdir(parents=True, exist_ok=True)
(REVIEW/'BlenderValidation.json').write_text(json.dumps(report, indent=2))
print('PAWNSHOP_ENTRY_COMPLETE', json.dumps(report))
