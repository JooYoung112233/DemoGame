"""Offline source validation, not a Unity import or playtest."""
import bpy,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/Art/Environments/TownProps02'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/TownProps02.blend'))
manifest=json.loads((OUT/'KitManifest.json').read_text(encoding='utf8'));report=[]
for name,spec in manifest['assets'].items():
 assert (OUT/'Models'/f'{name}.fbx').stat().st_size>1000,name
 assert (ROOT/spec['reference']).is_file(),spec['reference']
 assert all(math.isfinite(n) and n>0 for n in spec['size']),name
 for collider in spec['colliders']:assert all(n>0 for n in collider['size']),name
 meshes=[o.data for o in bpy.data.collections[name+'_Editable'].objects if o.type=='MESH']
 assert meshes,name
 for mesh in meshes:
  assert len(mesh.uv_layers)==1 and mesh.uv_layers.active.name=='MetreUV',(name,'UV0')
  assert all(math.isfinite(n) for v in mesh.vertices for n in v.co),(name,'vertex')
  assert all(math.isfinite(n) for u in mesh.uv_layers.active.data for n in u.uv),(name,'UV')
  assert all(p.area>1e-10 and p.normal.length>.9 for p in mesh.polygons),(name,'degenerate surface')
 count=sum(len(p.vertices)-2 for me in meshes for p in me.polygons)
 assert count==spec['triangles'],name
 report.append({'name':name,'pieces':len(meshes),'triangles':count,'metricUV0':True,'referenceExists':True})
for name in manifest['palette']:
 spec=manifest['palette'][name]
 for key in ['roughness','roughnessVariation','metallic','normalStrength']:assert math.isfinite(spec[key]) and 0<=spec[key]<=1,(name,key)
 if name.endswith(('Rubber','BagPlastic')):assert spec['metallic']==0,name
 mat=bpy.data.materials[name];images=[n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE']
 assert len(images)==3 and all(im.packed_file for im in images),name
for group,specs in manifest['reviewGroups'].items():
 for name,pos,yaw in specs:assert name in manifest['assets'] and len(pos)==3,(group,name)
result={'assets':report,'totalTriangles':sum(r['triangles'] for r in report),'packedPBRMaterials':len(manifest['palette']),'editableLayout':bpy.data.collections.get('TownProps02_Editable_Layout') is not None,'unityImportPlacementAndPlaytest':'pending: preserve ongoing combat testing'}
(OUT/'ModelValidation.json').write_text(json.dumps(result,indent=2),encoding='utf8');print('TOWN_PROPS_VALIDATED',json.dumps(result))
