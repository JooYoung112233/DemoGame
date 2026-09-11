import bpy,math,re
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview';CACHE=ROOT/'Library/CodexBlender/AxeReview'
bpy.ops.wm.open_mainfile(filepath=str(CACHE/'FittedReview.blend'))
sc=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig'];preview=OUT/'Previews';preview.mkdir(exist_ok=True)
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=16;sc.render.resolution_x=600;sc.render.resolution_y=720;sc.render.resolution_percentage=100
sc.camera.location=(-3.7,-6,2.8);sc.camera.rotation_euler=(Vector((0,-.15,1.05))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=2.75
sc.render.fps=30;sc.frame_step=1
# Hold the final frame for a readable pause without modifying delivered actions.
sc.render.image_settings.file_format='FFMPEG';sc.render.ffmpeg.format='MPEG4';sc.render.ffmpeg.codec='H264';sc.render.ffmpeg.constant_rate_factor='HIGH';sc.render.ffmpeg.audio_codec='NONE'
for name in ['AxeChop01']:
 a=bpy.data.actions[name];rig.animation_data.action=a
 sc.frame_start=1;sc.frame_end=int(a.frame_range[1])+15;sc.frame_set(1)
 sc.render.filepath=str(preview/(name+'.mp4'));bpy.ops.render.render(animation=True)
 path=preview/(name+'.mp4')
 for candidate in preview.glob(name+'*.mp4'):
  if re.fullmatch(re.escape(name)+r'(?:\.mp4)?\d{4}-\d{4}\.mp4',candidate.name):candidate.replace(path)
 if not path.exists():raise RuntimeError('Rendered movie was not found')
 print('VIDEO_READY',name,flush=True)
