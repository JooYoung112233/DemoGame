"""Compare the reattached reference's actual frames with the Blender run at real speed."""
import bpy,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ChibiSurvivor/Player/RunReview'
CACHE=ROOT/'Library/CodexBlender/RunReview_v3'
REF=ROOT/'Library/CodexBlender/RunReferenceFull'
assert (CACHE/'Revised.mp4').stat().st_size>1000
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene;scene.render.resolution_x=1024;scene.render.resolution_y=640;scene.render.resolution_percentage=100
scene.render.fps=30;scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
ed=scene.sequence_editor_create();strips=ed.strips if hasattr(ed,'strips') else ed.sequences
COUNT=174
# 46 consecutive source frames, 10.875 through 12.750 seconds, native 24 fps.
# Repeat only this run excerpt, not the initial old Blender locomotion in the video.
for f in range(1,COUNT+1):
    index=5+(int((f-1)*24/30)%46)
    seq=strips.new_image('Reference',str(REF/f'reference_{index:02d}.png'),channel=1,frame_start=f)
    seq.frame_final_duration=1;seq.blend_type='ALPHA_OVER'
    seq.transform.scale_x=.63;seq.transform.scale_y=.63;seq.transform.offset_x=-256
for start in [1,121]:
    seq=strips.new_movie('Blender Run',str(CACHE/'Revised.mp4'),channel=2,frame_start=start)
    assert seq.frame_final_duration==120
    seq.frame_final_end=min(start+120,COUNT+1)
    seq.blend_type='ALPHA_OVER';seq.transform.scale_x=1;seq.transform.scale_y=1;seq.transform.offset_x=256
font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
for col,title in enumerate(['참고 영상','Blender 수정안']):
    seq=strips.new_effect('Label',type='TEXT',channel=3+col,frame_start=1,frame_end=COUNT+1)
    seq.text=title;seq.font=font;seq.font_size=24;seq.location=(.25+.5*col,.95);seq.use_shadow=True
scene.frame_start=1;scene.frame_end=COUNT
scene.render.image_settings.file_format='FFMPEG';scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264'
scene.render.ffmpeg.constant_rate_factor='HIGH';scene.render.ffmpeg.ffmpeg_preset='GOOD';scene.render.ffmpeg.audio_codec='NONE'
target=OUT/'RunReferenceComparison_v3.mp4';scene.render.filepath=str(target)
bpy.ops.render.render(animation=True)
if not target.exists():
    candidates=list(OUT.glob('RunReferenceComparison_v3*.mp4'));assert len(candidates)==1;candidates[0].replace(target)
assert target.stat().st_size>1000
shutil.copy2(CACHE/'Revised.mp4',OUT/'RunPreview_v3.mp4')
scene.render.image_settings.file_format='PNG'
for f in [1,10,22,35]:
    scene.frame_set(f);scene.render.filepath=str(CACHE/f'ReferencePair_{f}.png');bpy.ops.render.render(write_still=True)
report={'left':'Original user video, 10.875-12.750 seconds, repeated at native 24 fps',
 'right':'Actual Blender Run v3, 30 fps, repeated 0.8 second animation',
 'output_fps':30,'output_frames':COUNT,'source_in_project':'ArtSource/MotionReferences/LocomotionAndCombat_2026-09-09.mp4',
 'retimed_for_matching_poses':False,'approval':'pending','unity_connected':False}
(OUT/'ReferenceComparisonCheck_v3.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('REFERENCE_COMPARISON_READY',str(target),flush=True)
