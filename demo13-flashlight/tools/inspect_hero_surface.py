import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend'))
print('SURFACE_INSPECT',json.dumps({'objects':[{'name':o.name,'vertices':len(o.data.vertices),'uv':len(o.data.uv_layers),'materials':[m.name for m in o.data.materials],'modifiers':[m.type for m in o.modifiers]} for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('Hero_')],'actions':[(a.name,list(a.frame_range)) for a in bpy.data.actions]}))
