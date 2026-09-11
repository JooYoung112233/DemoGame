"""Decode actual MP4 files with Blender VSE, without re-rendering the models."""
import bpy,json
from pathlib import Path
OUT=Path(__file__).resolve().parents[1];report=[]
for label in ['Idle62','IdleFront']:
 bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;sc.render.fps=15
 editor=sc.sequence_editor_create();movie=editor.sequences.new_movie(label,str(OUT/(label+'.mp4')),channel=1,frame_start=1)
 assert movie.frame_duration==45,(label,movie.frame_duration)
 sc.render.resolution_x=1080;sc.render.resolution_y=600;sc.render.resolution_percentage=100;sc.render.use_sequencer=True
 sc.view_settings.view_transform='Standard';sc.view_settings.look='None';sc.view_settings.exposure=0;sc.view_settings.gamma=1
 sc.render.image_settings.file_format='PNG';sc.render.film_transparent=False
 for frame in [1,12,24,35,45]:
  sc.frame_set(frame);sc.render.filepath=str(OUT/'Review'/(label+'_Decoded_'+str(frame)+'.png'));bpy.ops.render.render(write_still=True)
 report.append(dict(file=label+'.mp4',frames=movie.frame_duration,fps=15,seconds=movie.frame_duration/15,decodedFrames=[1,12,24,35,45]))
(OUT/'VideoValidation.json').write_text(json.dumps(report,indent=2))
print('NPC_VIDEOS_DECODED',flush=True)
