import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
for rel in ['Assets/ChibiSurvivor/Player/CompactSurvivor_Combat.blend','Assets/ChibiSurvivor/Player/DarkSurvivorReview/Stage02_FacePalette/DarkSurvivor_Stage02.blend']:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/rel))
 print('INSPECT',rel)
 for o in bpy.context.scene.objects:
  if o.type=='ARMATURE':
   print('RIG',o.name,[(b.name,tuple(b.head_local),tuple(b.tail_local)) for b in o.data.bones])
  elif o.type=='MESH' and o.name.startswith('Study_'):
   ps=[o.matrix_world@v.co for v in o.data.vertices]
   print('PART',o.name,tuple(min(p[i] for p in ps) for i in range(3)),tuple(max(p[i] for p in ps) for i in range(3)))
 print('ACTIONS',[(a.name,tuple(a.frame_range)) for a in bpy.data.actions])
