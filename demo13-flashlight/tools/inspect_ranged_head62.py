import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT.parent/'ArtWork/CharacterProportionC/BlenderSource~/Bandit_C.blend'))
o=bpy.data.objects['Bandit_Body']
for name in ['Part_Study_Head','Part_Bandit_Mask']:
 gi=o.vertex_groups[name].index
 print(name,json.dumps([(v.index,list(v.co),[(o.vertex_groups[g.group].name,g.weight) for g in v.groups]) for v in o.data.vertices if any(g.group==gi for g in v.groups)]))
print('BONES',[(b.name,list(b.head_local),list(b.tail_local)) for b in bpy.data.objects['SimpleHero_Rig'].data.bones if 'Arm' in b.name])
