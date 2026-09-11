import bpy,json,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Library/CodexBlender/HeroAttachments';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend'))
sc=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig'];rig.data.pose_position='REST';bpy.context.view_layer.update()
pairs={'Hero_PackStrap_1':['Hero_JacketBody','Hero_Backpack'],'Hero_PackStrap_-1':['Hero_JacketBody','Hero_Backpack'],'Hero_BackpackFlap':['Hero_Backpack'],'Hero_BackpackClosure':['Hero_BackpackFlap','Hero_Backpack'],'Hero_Backpack':['Hero_JacketBody'],'Hero_ScarfFront':['Hero_ScarfWrap','Hero_JacketBody'],'Hero_ScarfWrap':['Hero_Neck','Hero_JacketBody'],'Hero_BeltBuckle':['Hero_Belt'],'Hero_LanternLoop':['Hero_LanternBlock','Hero_Belt'],'Hero_LanternAccent':['Hero_LanternBlock'],'Hero_Lapel_1':['Hero_JacketBody'],'Hero_Lapel_-1':['Hero_JacketBody'],'Hero_RolledSleeve_1':['Hero_Sleeve_1','Hero_Forearm_1'],'Hero_RolledSleeve_-1':['Hero_Sleeve_-1','Hero_Forearm_-1']}
def tree(names):
 verts=[];faces=[]
 for name in names:
  o=bpy.data.objects[name];n=len(verts);verts.extend([o.matrix_world@v.co for v in o.data.vertices]);faces.extend([tuple(n+i for i in p.vertices) for p in o.data.polygons])
 return BVHTree.FromPolygons(verts,faces)
rows=[]
for name,targets in pairs.items():
 o=bpy.data.objects[name];bv=tree(targets);points=[o.matrix_world@v.co for v in o.data.vertices];dists=sorted([bv.find_nearest(p)[3] for p in points]);zs=[p.z for p in points]
 row={'part':name,'targets':targets,'nearest_m':dists[0],'closest_quarter_mean_m':sum(dists[:max(1,len(dists)//4)])/max(1,len(dists)//4),'bounds':[(min(p[i] for p in points),max(p[i] for p in points)) for i in range(3)]}
 if 'Strap' in name:row['end_gaps']={label:min(bv.find_nearest(p)[3] for p in points if abs(p.z-z)<.01) for label,z in [('top',max(zs)),('bottom',min(zs))]}
 rows.append(row)
(OUT/'AttachmentAudit.json').write_text(json.dumps(rows,indent=2));print('ATTACHMENTS',json.dumps(rows),flush=True)
rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=24;sc.render.resolution_x=800;sc.render.resolution_y=850;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
for label,position,target,scale in [('Front',(2,-5,2.2),(0,-.03,1),1.5),('Back',(-3,5,2.2),(0,.08,1.04),1.5),('Shoulder',(3,1,4),(0,0,1.15),1.05)]:
 sc.camera.location=position;sc.camera.rotation_euler=(Vector(target)-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=scale;sc.render.filepath=str(OUT/(label+'.png'));bpy.ops.render.render(write_still=True)
