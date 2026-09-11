"""Fast, repeatable surface update from the editable kit; run with Blender --background.
Run build_hideout_textures.py first. The full build_hideout02.py also applies this bedding.
"""
import bpy,bmesh,json,math,sys,random
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from hideout02_bedding import refine_bedding
from hideout02_blender_materials import bind_surfaces

out=Path(__file__).resolve().parents[1]/'Assets/Art/Environments/Hideout02'
bpy.ops.wm.open_mainfile(filepath=str(out/'BlenderSource~/Hideout02.blend'))
bpy.context.preferences.filepaths.save_version=0
manifest=json.loads((out/'KitManifest.json').read_text(encoding='utf-8-sig'))
library=bpy.data.collections['01_Editable_Asset_Library'];library.hide_viewport=False
exports=bpy.data.collections['03_Unity_Batched_Exports'];exports.hide_viewport=False
col=bpy.data.collections['Cot01'];root=next(o for o in col.objects if o.type=='EMPTY' and o.parent is None)
mats={k.removeprefix('Hideout_'):bpy.data.materials[k] for k in manifest['palette'] if k in bpy.data.materials}
refine_bedding(col,root,mats)
rng=random.Random(91127)
for o in [o for o in col.objects if o.type=='MESH']:
    bm=bmesh.new();bm.from_mesh(o.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
    if o.data.uv_layers:continue
    uv=o.data.uv_layers.new(name='Surface_MetreUV');offset=(rng.random(),rng.random())
    dim=[max(v.co[i] for v in o.data.vertices)-min(v.co[i] for v in o.data.vertices) for i in range(3)]
    for face in o.data.polygons:
        major=max(range(3),key=lambda i:abs(face.normal[i]));axes=[i for i in range(3) if i!=major];axes.sort(key=lambda i:dim[i],reverse=True)
        for li in face.loop_indices:
            v=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(v[axes[0]]+offset[0],v[axes[1]]+offset[1])

for o in list(exports.objects):
    if o.name.startswith('Cot01'):bpy.data.objects.remove(o,do_unlink=True)
copies=[]
for o in col.objects:
    if o.type!='MESH':continue
    cp=o.copy();cp.data=o.data.copy();exports.objects.link(cp);cp.parent=None;cp.matrix_world=o.matrix_world.copy();copies.append(cp)
bpy.ops.object.select_all(action='DESELECT')
for o in copies:o.select_set(True)
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();joined=bpy.context.object;joined.name='Cot01'
bpy.ops.export_scene.fbx(filepath=str(out/'Models/Cot01.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
manifest['assets']['Cot01']['triangles']=sum(len(f.vertices)-2 for f in joined.data.polygons)
joined.hide_render=True;joined.hide_set(True)

layout=bpy.data.collections['02_Assembled_Safehouse']
old=next(o for o in layout.objects if o.type=='EMPTY' and o.name.startswith('Cot01') and o.parent)
group=old.parent
for o in list(old.children_recursive)+[old]:bpy.data.objects.remove(o,do_unlink=True)
mapping={}
for o in col.objects:
    cp=o.copy();layout.objects.link(cp);mapping[o]=cp
for o,cp in mapping.items():cp.parent=mapping.get(o.parent,group)
spec=next(p for p in manifest['placements'] if p['asset']=='Cot01')
r=mapping[root];r.location=spec['position'];r.rotation_euler.z=math.radians(spec['rotation_z'])
bind_surfaces(out,manifest)
library.hide_viewport=True;exports.hide_viewport=True
(out/'KitManifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(out/'BlenderSource~/Hideout02.blend'))
print('SURFACE_REFINEMENT',json.dumps({'bedTriangles':manifest['assets']['Cot01']['triangles'],'frameAndAnchorsPreserved':True}))
