import bpy
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview/Previews'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Library/CodexBlender/AxeReview/FittedReview.blend'))
sc=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig'];rig.animation_data.action=bpy.data.actions['AxeChop01'];sc.frame_set(1)
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=24;sc.render.resolution_x=720;sc.render.resolution_y=800;sc.render.resolution_percentage=100
sc.camera.location=(3,-5,2.5);sc.camera.rotation_euler=(Vector((0,-.05,1.08))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=1.9
sc.render.fps=24;sc.frame_start=1;sc.frame_end=52;sc.render.image_settings.file_format='FFMPEG';sc.render.ffmpeg.format='MPEG4';sc.render.ffmpeg.codec='H264';sc.render.ffmpeg.constant_rate_factor='HIGH';sc.render.ffmpeg.audio_codec='NONE'
stem='AxeChop01_ElbowAxis_Close';sc.render.filepath=str(OUT/(stem+'.mp4'));bpy.ops.render.render(animation=True)
for path in OUT.glob(stem+'*.mp4'):
 if path.name!=stem+'.mp4':path.replace(OUT/(stem+'.mp4'))
print('ELBOW_VIDEO_READY',flush=True)
