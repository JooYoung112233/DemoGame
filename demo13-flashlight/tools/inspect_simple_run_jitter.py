import bpy,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
for path,rigname in [(ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend','DarkSurvivor_Rig'),(OUT/'BlenderSource~/SimpleHero_Stage4_Locomotion.blend','SimpleHero_Rig')]:
 bpy.ops.wm.open_mainfile(filepath=str(path));s=bpy.context.scene;r=bpy.data.objects[rigname];a=bpy.data.actions['Run'];r.animation_data.action=a
 if a.slots:r.animation_data.action_slot=a.slots[0]
 rows=[]
 for f in range(1,25):
  s.frame_set(f);bpy.context.view_layer.update();rows.append({n:(r.pose.bones[n].head.copy(),r.pose.bones[n].matrix.to_quaternion()) for n in ['Hips','Chest','Head','Hand.R','Foot.R']})
 print('FILE',path.name,'render',s.render.engine,s.eevee.taa_render_samples,flush=True)
 print('HIP_Z',[round(x['Hips'][0].z,4) for x in rows],flush=True)
 for n in rows[0]:
  accelerations=[((rows[(i+1)%24][n][0]-rows[i][n][0])-(rows[i][n][0]-rows[(i-1)%24][n][0])).length for i in range(24)]
  speeds=[math.degrees(rows[i][n][1].rotation_difference(rows[(i+1)%24][n][1]).angle) for i in range(24)]
  print(n,'max_accel',max(accelerations),'frame',accelerations.index(max(accelerations))+1,'rot_steps',[round(min(v,360-v),2) for v in speeds],flush=True)
 print('HIP_LOC_KEYS',[(c.array_index,[round(k.co.y,4) for k in c.keyframe_points]) for c in a.fcurves if c.data_path=='pose.bones["Hips"].location'],flush=True)
