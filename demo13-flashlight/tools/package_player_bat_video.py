import bpy,json
from pathlib import Path
out=Path('D:/Demo/ArtWork/SimpleHeroMelee')
bpy.ops.wm.read_factory_settings(use_empty=True)
s=bpy.context.scene;s.render.resolution_x=800;s.render.resolution_y=800;s.render.resolution_percentage=100;s.render.fps=30
ed=s.sequence_editor_create();strips=ed.strips if hasattr(ed,'strips') else ed.sequences
for i in range(3):
 clip=strips.new_movie('Two-hand single swing - repeat '+str(i+1),str(out/'BatSwing.mp4'),channel=1,frame_start=1+i*48)
 clip.frame_final_duration=48
 assert clip.elements and clip.elements[0].orig_width==800
s.frame_start=1;s.frame_end=144;s.render.use_sequencer=True
s.render.image_settings.file_format='FFMPEG';s.render.ffmpeg.format='MPEG4';s.render.ffmpeg.codec='H264';s.render.ffmpeg.constant_rate_factor='HIGH';s.render.filepath=str(out/'BatSwing_Review.mp4')
bpy.ops.render.render(animation=True)
print('REVIEW_VIDEO_READY',flush=True)
