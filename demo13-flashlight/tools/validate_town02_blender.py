"""Validate the editable model kit without accessing the live Unity Editor."""
import bpy,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/Art/Environments/Town02'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/Town02.blend'))
manifest=json.loads((OUT/'KitManifest.json').read_text());report=[]
for name in manifest['assets']:
 meshes=[o.data for o in bpy.data.collections[name+'_Editable'].objects if o.type=='MESH']
 assert meshes,name
 for mesh in meshes:
  assert len(mesh.uv_layers)==1 and mesh.uv_layers.active.name=='MetreUV',(name,'UV0 must be metric, not primitive UVMap')
  assert all(math.isfinite(n) for v in mesh.vertices for n in v.co),(name,'non-finite vertex')
  assert all(math.isfinite(n) for u in mesh.uv_layers.active.data for n in u.uv),(name,'non-finite UV')
  assert all(p.area>1e-10 and p.normal.length>.9 for p in mesh.polygons),(name,'degenerate surface')
 report.append({'name':name,'pieces':len(meshes),'triangles':sum(len(p.vertices)-2 for me in meshes for p in me.polygons),'metricUV0':True})
for mat in [m for m in bpy.data.materials if m.name.startswith('Town_')]:
 images=[n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE'];assert len(images)==3 and all(im.packed_file for im in images),mat.name
result={'assets':report,'totalTriangles':sum(r['triangles'] for r in report),'packedPBRMaterials':len(manifest['palette']),'editableLayout':bpy.data.collections.get('Town02_Editable_Layout') is not None,'unityFinalImportAndPlaytest':'pending: user is running combat testing; keep Unity play mode untouched'}
(OUT/'ModelValidation.json').write_text(json.dumps(result,indent=2));print(json.dumps(result))
