import bpy,json
from pathlib import Path
r=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(r/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend'))
rows=[]
for o in bpy.context.scene.objects:
 if o.type=='MESH' and o.name.startswith('Hero_'):
  vs=[o.matrix_world@v.co for v in o.data.vertices]
  rows.append(dict(name=o.name,n=len(vs),lo=[min(v[i] for v in vs) for i in range(3)],hi=[max(v[i] for v in vs) for i in range(3)],materials=[m.name for m in o.data.materials]))
(r/'Library/hero_uv_source.json').write_text(json.dumps(rows,indent=2))
