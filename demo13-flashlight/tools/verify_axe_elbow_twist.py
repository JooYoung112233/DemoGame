import bpy,math,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];CACHE=ROOT/'Library/CodexBlender/AxeReview';OUT=ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview'
bpy.ops.wm.open_mainfile(filepath=str(CACHE/'FittedReview.blend'));rig=bpy.data.objects['DarkSurvivor_Rig'];sc=bpy.context.scene
upper=rig.data.bones['UpperArm.L'];fore=rig.data.bones['Forearm.L'];normal=(upper.tail_local-upper.head_local).cross(fore.tail_local-fore.head_local).normalized();rows=[]
for i in range(145):
 f=1+i/4;sc.frame_set(int(f),subframe=f%1);bpy.context.view_layer.update()
 un=rig.pose.bones[upper.name].matrix.to_quaternion()@upper.matrix_local.to_quaternion().inverted()@normal
 fn=rig.pose.bones[fore.name].matrix.to_quaternion()@fore.matrix_local.to_quaternion().inverted()@normal
 rows.append({'frame':f,'hinge_misalignment':math.degrees(un.angle(fn))})
report={'max_hinge_misalignment_degrees':max(r['hinge_misalignment'] for r in rows),'samples':rows};(OUT/'ElbowHingeCheck.json').write_text(json.dumps(report,indent=2));print('ELBOW_HINGE',report['max_hinge_misalignment_degrees'],flush=True)
