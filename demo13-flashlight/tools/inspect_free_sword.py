import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];CACHE=ROOT/'Library/CodexBlender/TwoHandWalkReview'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/ChibiSurvivor/Player/TwoHandWalkReview/CompactSurvivor_TwoHandWalk.blend'))
rig=bpy.data.objects['CompactSurvivor_Rig'];out={}
for f in [1,7,13,19,25]:
 bpy.context.scene.frame_set(f);bpy.context.view_layer.update()
 out[f]={n:{'head':list(rig.pose.bones[n].head),'rot':list(rig.pose.bones[n].matrix.to_quaternion()),'axisY':list(rig.pose.bones[n].matrix.to_quaternion()@Vector((0,1,0)))} for n in ['Hand.R','Hand.L','HandSocket.R','HandSocket.L']}
before=set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Models/Weapons/2Hand-Sword.FBX'),use_anim=False)
out['weapon']=[{'name':o.name,'type':o.type,'bounds':[list(o.matrix_world@Vector(v)) for v in o.bound_box] if o.type=='MESH' else None,'matrix':[list(r) for r in o.matrix_world],'vertices':[list(v.co) for v in o.data.vertices][:8] if o.type=='MESH' else None} for o in set(bpy.data.objects)-before]
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/2Hand-Sword/RPG-Character@2Hand-Sword-Walk.FBX'))
bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
out['source']={n:list(bpy.data.objects[n].matrix_world.translation) for n in ['B_Spine2','B_R_Hand','B_L_Hand','B_R_UpperArm','B_L_UpperArm']}
(CACHE/'sword-inspection.json').write_text(json.dumps(out,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(CACHE/'SwordInspection.blend'))
print('SWORD_INSPECTION',json.dumps(out),flush=True)
