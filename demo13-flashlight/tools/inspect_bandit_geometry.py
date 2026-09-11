import bpy,bmesh,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Assets/ChibiSurvivor/Bandit/SimpleBandit/BlenderSource~/SimpleBandit.blend'))
o=bpy.data.objects['Bandit_Body'];g=o.vertex_groups['Part_Bandit_Mask'].index
ids={v.index for v in o.data.vertices if any(w.group==g for w in v.groups)}
print('OBJECTS',[(o.name,o.hide_render) for o in bpy.context.scene.objects if o.type=='MESH'])
print('MASK_VERTS',[(i,tuple(o.data.vertices[i].co)) for i in sorted(ids)])
print('MASK_FACES',[(tuple(f.vertices),tuple(f.normal)) for f in o.data.polygons if set(f.vertices)<=ids])
print('MODIFIERS',[(m.name,m.type) for m in o.modifiers])
