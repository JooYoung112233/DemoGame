import bpy,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
mode=sys.argv[-1];slash=mode=='SwordSlash'
source='SimpleHero_Stage5_Slash.blend' if slash else 'SimpleHero_Stage4_Locomotion.blend'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~'/source));sc=bpy.context.scene;rig=bpy.data.objects['SimpleHero_Rig']
action=bpy.data.actions[mode];rig.animation_data.action=action
if action.slots:rig.animation_data.action_slot=action.slots[0]
sc.render.resolution_x=640;sc.render.resolution_y=720;sc.render.resolution_percentage=100;sc.eevee.taa_render_samples=96 if mode=='Run' else 24
if slash:
 sc.camera.data.ortho_scale=2.65;sc.camera.location=(4.8,-7,2.7);sc.camera.rotation_euler=(Vector((0,-.1,.9))-sc.camera.location).to_track_quat('-Z','Y').to_euler()
 # Add holds only to an unsaved review copy, not the exported attack action.
 action=action.copy();rig.animation_data.action=action
 for fc in action.fcurves:
  for k in fc.keyframe_points:k.co.x+=12;k.handle_left.x+=12;k.handle_right.x+=12
 sc.frame_start=1;sc.frame_end=61
else:sc.frame_start=1;sc.frame_end=72 if mode=='Run' else 108
sc.render.image_settings.file_format='PNG';sc.frame_set(25 if slash else 7);sc.render.filepath=str(OUT/('Stage5_Slash.png' if slash else 'Stage4_'+mode+'.png'));bpy.ops.render.render(write_still=True)
if mode=='Run':
 old=sc.camera.matrix_world.copy();sc.camera.location=(6,-.8,2.4);sc.camera.rotation_euler=(Vector((0,0,.9))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/'Stage4_Run_Side.png');bpy.ops.render.render(write_still=True);sc.camera.matrix_world=old
sc.render.fps=30;sc.render.image_settings.file_format='FFMPEG';sc.render.ffmpeg.format='MPEG4';sc.render.ffmpeg.codec='H264';sc.render.ffmpeg.constant_rate_factor='HIGH'
sc.render.filepath=str(OUT/('Stage5_Slash.mp4' if slash else 'Stage4_'+mode+'.mp4'));bpy.ops.render.render(animation=True)
print('REVIEW_VIDEO_READY',mode,flush=True)
