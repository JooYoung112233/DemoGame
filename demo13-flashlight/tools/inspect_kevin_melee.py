import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Library/KevinMeleeSource/HumanM_Melee.blend'))
print('OBJECTS',[(o.name,o.type) for o in bpy.context.scene.objects],flush=True)
for r in [o for o in bpy.context.scene.objects if o.type=='ARMATURE']:
 print('RIG',r.name,'matrix',list(map(list,r.matrix_world)),'BONES',[(b.name,b.parent.name if b.parent else None) for b in r.data.bones if b.name.startswith('B-')],flush=True)
print('ACTIONS',[(a.name,list(a.frame_range)) for a in bpy.data.actions],'fps',bpy.context.scene.render.fps,flush=True)
