import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
paths=['Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_Stage5_Slash.blend','Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_TwoHandBat.blend','Assets/ChibiSurvivor/Bandit/SimpleBandit/BlenderSource~/SimpleBandit.blend','Assets/ChibiSurvivor/Bandit/SimpleBandit/BlenderSource~/SimpleBandit_Combat.blend']
result=[]
for path in paths:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/path));rig=bpy.data.objects['SimpleHero_Rig']
 result.append({'path':path,'rigScale':list(rig.scale),'bones':[{'name':b.name,'parent':b.parent.name if b.parent else None,'head':list(b.head_local),'tail':list(b.tail_local)} for b in rig.data.bones],'parts':[{'name':o.name,'parent':o.parent.name if o.parent else None,'matrix':[list(row) for row in o.matrix_basis],'constraints':len(o.constraints)} for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith(('Study_','Gear_','Bandit_','Hero_'))],'actions':[{'name':a.name,'range':list(a.frame_range),'channels':len(a.fcurves)} for a in bpy.data.actions],'constraints':[(b.name,c.type) for b in rig.pose.bones for c in b.constraints]})
out=ROOT/'Library/TownProps02/c-sources.json';out.write_text(json.dumps(result,indent=2),encoding='utf8');print('C_SOURCES',str(out))
