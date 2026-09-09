"""Blender-only check that the consolidated current character is self-contained."""
import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ChibiSurvivor/Player'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'CompactSurvivor_Combat.blend'))
scene=bpy.context.scene
meshes=[o for o in scene.objects if o.type=='MESH']
rigs=[o for o in scene.objects if o.type=='ARMATURE']
assert len(meshes)==13 and len(rigs)==1 and len(rigs[0].data.bones)==23
assert {a.name for a in bpy.data.actions}=={'Idle','Walk','Run','Attack_OneHand_Thrust','Attack_TwoHand_Slash','Attack_TwoHand_Chop'}
assert not bpy.data.libraries,'External linked Blender library must be relocated first.'
external=[]
for image in bpy.data.images:
    if image.source=='FILE' and not image.packed_file:
        path=Path(bpy.path.abspath(image.filepath));assert path.is_file(),str(path)
        external.append(str(path))
report={'current_file':str(OUT/'CompactSurvivor_Combat.blend'),'meshes':13,'bones':23,
        'actions':[a.name for a in bpy.data.actions],'external_linked_libraries':0,'external_images':external,
        'unity_validation':False}
(ROOT/'Library/CodexBlender/character-cleanup-model-check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('CONSOLIDATED_HERO_OK',json.dumps(report))
