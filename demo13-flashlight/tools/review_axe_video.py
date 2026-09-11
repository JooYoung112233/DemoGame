import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview/Previews';CACHE=ROOT/'Library/CodexBlender/AxeReview'
bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;sc.render.resolution_x=600;sc.render.resolution_y=720;sc.render.resolution_percentage=100;sc.render.fps=30
sc.view_settings.view_transform='Standard';sc.view_settings.look='None';ed=sc.sequence_editor_create();cursor=1;report=[]
font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
for name,label in [('AxeChop01','내려찍기 · 단일 공격')]:
 path=OUT/(name+'.mp4');seq=ed.strips.new_movie(name,str(path),channel=1,frame_start=cursor)
 text=ed.strips.new_effect(name+'_Label',type='TEXT',channel=2,frame_start=cursor,frame_end=seq.frame_final_end);text.text=label;text.font=font;text.font_size=24;text.color=(.95,.95,.9,1);text.location=(.5,.94);text.use_shadow=True
 report.append({'name':name,'start':cursor,'end':seq.frame_final_end-1,'seconds':seq.frame_final_duration/30});cursor=seq.frame_final_end
sc.frame_start=1;sc.frame_end=cursor-1;sc.render.image_settings.file_format='FFMPEG';sc.render.ffmpeg.format='MPEG4';sc.render.ffmpeg.codec='H264';sc.render.ffmpeg.constant_rate_factor='HIGH';sc.render.filepath=str(OUT/'AxeMotionReview.mp4');bpy.ops.render.render(animation=True)
dest=OUT/'AxeMotionReview.mp4'
for p in OUT.glob('AxeMotionReview*.mp4'):
 if p!=dest:p.replace(dest)
sc.render.image_settings.file_format='PNG'
for f in [23,28,33,43]:
 sc.frame_set(report[-1]['start']+f-1);sc.render.filepath=str(CACHE/f'Combo_{f}.png');bpy.ops.render.render(write_still=True)
(OUT/'VideoCheck.json').write_text(json.dumps(report,indent=2));print('VIDEO_CHECK',report)
