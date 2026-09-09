import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/ChibiSurvivor/Player/WalkReview/CompactSurvivor_WalkReview.blend'))
target=bpy.data.objects['CompactSurvivor_Rig']
before=set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/Unarmed/RPG-Character@Unarmed-Run-Forward.FBX'),force_connect_children=False, automatic_bone_orientation=False)
print('IMPORTED_OBJECTS',[(o.name,o.type) for o in set(bpy.data.objects)-before],flush=True)
print('IMPORTED_ACTIONS',[(a.name,list(a.frame_range)) for a in bpy.data.actions],flush=True)
source=list(set(bpy.data.objects)-before)
out={}
for key,rig in [('target',target)]:
 out[key]={'name':rig.name,'matrix':[list(r) for r in rig.matrix_world], 'bones':[{ 'name':b.name,'parent':b.parent.name if b.parent else None,'head':list(rig.matrix_world@b.head_local),'tail':list(rig.matrix_world@b.tail_local),'rotation':list((rig.matrix_world@b.matrix_local).to_quaternion())} for b in rig.data.bones]}
out['actions']=[{'name':a.name,'range':list(a.frame_range)} for a in bpy.data.actions]
out['fps']=bpy.context.scene.render.fps
out['source']={o.name:{'parent':o.parent.name if o.parent else None,'pos':list(o.matrix_world.translation),'rot':list(o.matrix_world.to_quaternion())} for o in source}
out['samples']={}
for f in [1,7,13,19,25]:
 bpy.context.scene.frame_set(f);bpy.context.view_layer.update()
 out['samples'][f]={o.name:{'pos':list(o.matrix_world.translation),'rot':list(o.matrix_world.to_quaternion())} for o in source if 'Finger' not in o.name}
before=set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Models/Characters/RPG-Character.FBX'),use_anim=False)
out['model']={o.name:{'type':o.type,'pos':list(o.matrix_world.translation),'rot':list(o.matrix_world.to_quaternion())} for o in set(bpy.data.objects)-before if 'Finger' not in o.name}
cache=ROOT/'Library/CodexBlender/ExplosiveReview';cache.mkdir(exist_ok=True)
(cache/'rig-inspection.json').write_text(json.dumps(out,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(cache/'Inspection.blend'))
print('RIG_INSPECTION_READY',flush=True)
