import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[1];out=root.parent/'ArtWork/SimpleHeroMelee'
bpy.ops.wm.open_mainfile(filepath=str(root/'Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_TwoHandBat.blend'));s=bpy.context.scene;r=bpy.data.objects['SimpleHero_Rig'];a=bpy.data.actions['BatSwing'];r.animation_data.action=a
for track in list(r.animation_data.nla_tracks):r.animation_data.nla_tracks.remove(track)
for fc in a.fcurves:
 for key in fc.keyframe_points:
  for v in [key.co,key.handle_left,key.handle_right]:v.x=1+(v.x-1)*8
s.render.fps=240;s.frame_start=1;s.frame_end=385;s.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');r.select_set(True);bpy.context.view_layer.objects.active=r
for o in s.objects:
 if o.type=='MESH' and o.name.startswith(('Study_','Gear_','Hero_Bat')):o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out/'Player_BatMotion.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_step=1,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
print('BAT_MOTION_240FPS_EXPORTED',flush=True)
