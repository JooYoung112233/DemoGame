"""Read-only inspection of the hero source for environment scale review."""
import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
path=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_Stage5_Slash.blend'
bpy.ops.wm.open_mainfile(filepath=str(path))
print('HERO_FIT_SOURCE',json.dumps({'source':str(path),'objects':[{'name':o.name,'type':o.type,'scale':list(o.scale),'dimensions':list(o.dimensions)} for o in bpy.context.scene.objects if o.type in ['MESH','ARMATURE']],'actions':[a.name for a in bpy.data.actions]},ensure_ascii=False))
