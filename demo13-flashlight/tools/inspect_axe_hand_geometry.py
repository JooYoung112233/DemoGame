import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];CACHE=ROOT/'Library/CodexBlender/AxeReview'
bpy.ops.wm.open_mainfile(filepath=str(CACHE/'FittedReview.blend'));sc=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig'];rows=[]
hands=bpy.data.objects['Hero_Hands'];group=hands.vertex_groups['Hand.L'].index;ids=[v.index for v in hands.data.vertices if any(g.group==group and g.weight>.5 for g in v.groups)]
print('LEFT_REST',list(rig.data.bones['Hand.L'].head_local),list(rig.data.bones['HandSocket.L'].head_local))
print('LEFT_MESH_REST',list(sum((hands.data.vertices[i].co for i in ids),Vector())/len(ids)))
for f in range(1,38):
 sc.frame_set(f);bpy.context.view_layer.update();ev=hands.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh();ps=[ev.matrix_world@mesh.vertices[i].co for i in ids];center=sum(ps,Vector())/len(ps);ev.to_mesh_clear()
 socket=rig.pose.bones['HandSocket.R'].matrix;shaft=(socket.to_quaternion()@Vector((0,1,0))).normalized();d=center-socket.translation;distance=(d-shaft*d.dot(shaft)).length
 rows.append({'frame':f,'center_shaft_distance':distance,'center_socket_distance':(center-rig.pose.bones['HandSocket.L'].head).length,'center':list(center),'elbow':list(rig.pose.bones['Forearm.L'].head)})
(CACHE/'HandGeometryCheck.json').write_text(json.dumps(rows,indent=2));print('HAND_GEOMETRY',json.dumps(rows))
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=16;sc.render.resolution_x=650;sc.render.resolution_y=700;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
for f in [1,11,13,16,30]:
 sc.frame_set(f);sc.camera.location=(3,-5,2);target=rig.pose.bones['Chest'].head+Vector((0,-.13,.08));sc.camera.rotation_euler=(target-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=1.25;sc.render.filepath=str(CACHE/f'HandAnatomy_{f}.png');bpy.ops.render.render(write_still=True)
