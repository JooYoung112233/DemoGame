import bpy,math,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];CACHE=ROOT/'Library/CodexBlender/AxeReview'
bpy.ops.wm.open_mainfile(filepath=str(CACHE/'FittedReview.blend'));sc=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig'];rig.animation_data.action=bpy.data.actions['AxeChop01'];rows=[]
for f in range(1,38):
 sc.frame_set(f);bpy.context.view_layer.update();p=rig.pose.bones
 fore=(p['Forearm.L'].tail-p['Forearm.L'].head).normalized();hand=(p['Hand.L'].tail-p['Hand.L'].head).normalized()
 rows.append({'frame':f,'wrist_bend':math.degrees(fore.angle(hand)),'elbow':list(p['Forearm.L'].head),'wrist':list(p['Hand.L'].head),'hand_end':list(p['Hand.L'].tail),'socket':list(p['HandSocket.L'].head)})
(CACHE/'LeftHandInspection.json').write_text(json.dumps(rows,indent=2));print('WORST',sorted(rows,key=lambda r:r['wrist_bend'],reverse=True)[:6])
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=16;sc.render.resolution_x=800;sc.render.resolution_y=750;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
for f in [9,13,16,23,30]:
 sc.frame_set(f);sc.camera.location=(3,-5,2.1);target=rig.pose.bones['Chest'].head+Vector((0,-.13,.05));sc.camera.rotation_euler=(target-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=1.5
 sc.render.filepath=str(CACHE/f'LeftClose_{f}.png');bpy.ops.render.render(write_still=True)
