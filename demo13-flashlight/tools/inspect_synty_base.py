import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
base=root/'Assets/Synty/SidekickCharacters/Resources/Meshes'
bpy.ops.wm.read_factory_settings(use_empty=True)
for pattern in ['Species/Humans/*01HEAD*','Species/Humans/*01_10TORS*','Outfits/Starter/*09_10TORS*']:
 for f in base.glob(pattern+'.fbx'):
  bpy.ops.wm.read_factory_settings(use_empty=True)
  bpy.ops.import_scene.fbx(filepath=str(f))
  print('SOURCE',f.name)
  for o in bpy.context.scene.objects:
   if o.type=='MESH':
    pts=[o.matrix_world@v.co for v in o.data.vertices]
    print(json.dumps(dict(name=o.name,verts=len(pts),bounds=[[min(p[i] for p in pts),max(p[i] for p in pts)] for i in range(3)],matrix=[list(r) for r in o.matrix_world],shapes=[k.name for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else [],materials=[m.name for m in o.data.materials])))
   if o.type=='ARMATURE':
    print('BONES',[(b.name,tuple(round(c,3) for c in o.matrix_world@b.head_local)) for b in o.data.bones if any(k in b.name.lower() for k in ['head','neck','arm','hips','spine'])])
