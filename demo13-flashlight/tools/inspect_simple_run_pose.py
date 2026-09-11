import bpy,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_Stage4_Locomotion.blend'))
rig=bpy.data.objects['SimpleHero_Rig'];a=bpy.data.actions['Run'];rig.animation_data.action=a
if a.slots:rig.animation_data.action_slot=a.slots[0]
bpy.context.scene.frame_set(7);bpy.context.view_layer.update()
for n in ['Hips','Spine','Chest','Head']:
 b=rig.pose.bones[n];q=b.matrix.to_quaternion()@rig.data.bones[n].matrix_local.to_quaternion().inverted();print(n,[round(math.degrees(v),1) for v in q.to_euler()],list(b.head))
