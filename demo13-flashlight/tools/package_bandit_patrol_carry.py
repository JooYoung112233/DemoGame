import bpy
from pathlib import Path
OUT=Path('D:/Demo/ArtWork/SimpleBandit/PatrolCarry')
bpy.ops.wm.read_factory_settings(use_empty=True);s=bpy.context.scene
s.render.resolution_x=1200;s.render.resolution_y=720;s.render.resolution_percentage=100;s.render.fps=30
s.view_settings.view_transform='Standard';s.view_settings.look='None';s.view_settings.exposure=0
editor=s.sequence_editor_create();strips=editor.strips
files=sorted((OUT/'Frames').glob('*.png'));assert len(files)==300,len(files)
strip=strips.new_image('Actual Unity prefab capture',str(files[0]),channel=1,frame_start=1)
for f in files[1:]:strip.elements.append(f.name)
strip.frame_final_duration=300
font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf');text_channel=1
def text(name,value,start,end,x,y,size=26):
 global text_channel
 text_channel+=1
 t=strips.new_effect(name,type='TEXT',channel=text_channel,frame_start=start,frame_end=end)
 t.text=value;t.font=font;t.font_size=size;t.location=(x,y);t.color=(.9,.88,.83,1);t.use_shadow=True;t.shadow_color=(0,0,0,.8)
text('Title','밴딧 · 평상시 한손 / 전투 시 양손',1,301,.5,.95,30)
text('Left angle','측면 위주',1,301,.25,.85,22)
text('Right angle','정면 위주',1,301,.75,.85,22)
for a,b,label in [(1,46,'평상시 · 한손으로 내려 들기'),(46,91,'순찰 · 한손으로 들고 걷기'),(91,101,'적 발견 · 양손으로 고쳐 잡기'),(101,139,'전투 · 양손 준비 자세로 추격'),(139,158,'멈춤 · 공격 준비'),(158,214,'방망이 1타 → 전투 자세 복귀'),(214,226,'경계'),(226,301,'경계 해제 · 한손 순찰로 복귀')]:text('Phase '+str(a),label,a,b,.5,.075,27)
s.frame_start=1;s.frame_end=300;s.render.use_sequencer=True
s.render.image_settings.file_format='FFMPEG';s.render.ffmpeg.format='MPEG4';s.render.ffmpeg.codec='H264';s.render.ffmpeg.constant_rate_factor='HIGH';s.render.filepath=str(OUT/'Bandit_Combat_Review.mp4')
bpy.ops.render.render(animation=True);print('BANDIT_REVIEW_VIDEO_READY',flush=True)
s.render.image_settings.file_format='PNG'
for frame in [1,91,174]:
 s.frame_set(frame);s.render.filepath=str(OUT/('ReviewFrame_'+str(frame)+'.png'));bpy.ops.render.render(write_still=True)

