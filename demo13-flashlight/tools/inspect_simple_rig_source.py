import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
rig.animation_data.action=bpy.data.actions['Idle'];bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
print('RIG_SOURCE',json.dumps({'rig':rig.name,'matrix':list(map(list,rig.matrix_world)),'fps':bpy.context.scene.render.fps,'frames':list(bpy.data.actions['Idle'].frame_range),'bones':[{'name':b.name,'head':list(b.head_local),'tail':list(b.tail_local),'parent':b.parent.name if b.parent else None,'rotation':list(rig.pose.bones[b.name].rotation_quaternion),'location':list(rig.pose.bones[b.name].location)} for b in rig.data.bones]}))
