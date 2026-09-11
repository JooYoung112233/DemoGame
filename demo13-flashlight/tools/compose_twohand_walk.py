import bpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];CACHE=ROOT/'Library/CodexBlender/TwoHandWalkReview';OUT=ROOT/'Assets/ChibiSurvivor/Player/TwoHandWalkReview'
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene
s.render.resolution_x=960;s.render.resolution_y=600;s.render.resolution_percentage=100;s.render.fps=30
s.view_settings.view_transform='Standard';s.view_settings.look='None'
strips=s.sequence_editor_create().strips
for i,(name,title) in enumerate([('Front','양손검 걷기 · 정면 사선'),('Side','양손검 걷기 · 측면')]):
 for repeat in range(4):
  clip=strips.new_movie(name,str(CACHE/(name+'.mp4')),channel=i+1,frame_start=1+24*repeat)
  assert clip.frame_duration==24,(name,clip.frame_duration)
  clip.blend_type='ALPHA_OVER';clip.transform.offset_x=(i-.5)*480
 label=strips.new_effect('Label',type='TEXT',channel=i+3,frame_start=1,frame_end=97);label.text=title;label.font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf');label.font_size=23;label.location=(.25+i*.5,.96);label.use_shadow=True
s.frame_start=1;s.frame_end=96;s.render.image_settings.file_format='FFMPEG';s.render.ffmpeg.format='MPEG4';s.render.ffmpeg.codec='H264';s.render.ffmpeg.constant_rate_factor='HIGH';s.render.ffmpeg.audio_codec='NONE'
path=OUT/'TwoHandWalkPreview.mp4';s.render.filepath=str(path);bpy.ops.render.render(animation=True)
for candidate in OUT.glob('TwoHandWalkPreview*.mp4'):
 if candidate!=path:candidate.replace(path)
assert path.exists() and path.stat().st_size>1000
s.render.image_settings.file_format='PNG';s.frame_set(7);s.render.filepath=str(CACHE/'VideoCheck.png');bpy.ops.render.render(write_still=True)
print('TWO_HAND_VIDEO_COMPLETE',path,flush=True)
