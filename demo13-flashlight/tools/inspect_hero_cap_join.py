import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend'))
o=bpy.data.objects['Hero_Cap'];mesh=o.data
for mi,m in enumerate(mesh.materials):
 ids=sorted({i for p in mesh.polygons if p.material_index==mi for i in p.vertices})
 print('CAP_PART',m.name,len(ids),'bounds',[(min(mesh.vertices[i].co[j] for i in ids),max(mesh.vertices[i].co[j] for i in ids)) for j in range(3)])
 if 'brim' in m.name.lower():print('BRIM_VERTICES',json.dumps([(i,list(mesh.vertices[i].co)) for i in ids]))
 print('CROWN_BASE',json.dumps([(i,list(mesh.vertices[i].co)) for i in ids if mesh.vertices[i].co.z<1.58]))
