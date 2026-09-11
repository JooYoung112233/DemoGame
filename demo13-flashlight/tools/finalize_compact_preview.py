"""Assemble encoded review clips without applying a second display transform."""
import bpy, json, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated'
if '--round-hands' in sys.argv:OUT=OUT/'RoundHands'
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.render.resolution_x=480;scene.render.resolution_y=600;scene.render.resolution_percentage=100
scene.render.fps=15;scene.frame_step=1
scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
scene.render.image_settings.file_format='FFMPEG'
scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264'
scene.render.ffmpeg.constant_rate_factor='HIGH';scene.render.ffmpeg.audio_codec='NONE'
editor=scene.sequence_editor_create();strips=editor.strips if hasattr(editor,'strips') else editor.sequences
font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
cursor=1;entries=[]
for clip,label,expected in [('Idle','대기 · Idle',45),('Walk','걷기 · Walk',45),('Run','뛰기 · Run',50)]:
    path=OUT/(clip+'.mp4')
    assert path.exists() and path.stat().st_size>1000
    seq=strips.new_movie(clip,str(path),channel=1,frame_start=cursor)
    assert seq.frame_final_duration==expected,(clip,seq.frame_final_duration)
    entries.append({'clip':clip,'start':cursor,'end':seq.frame_final_end-1,'frames':seq.frame_final_duration,'bytes':path.stat().st_size})
    text=strips.new_effect(clip+'_Label',type='TEXT',channel=2,frame_start=cursor,frame_end=seq.frame_final_end)
    text.text=label;text.font=font;text.font_size=26;text.color=(.95,.95,.92,1)
    text.location=(.5,.94);text.use_shadow=True
    cursor=seq.frame_final_end
scene.frame_start=1;scene.frame_end=cursor-1
scene.render.filepath=str(OUT/'LocomotionPreview.mp4');bpy.ops.render.render(animation=True)
combined=OUT/'LocomotionPreview.mp4'
alternates=[p for p in OUT.glob('LocomotionPreview*.mp4') if p!=combined]
if alternates:
    assert len(alternates)==1;alternates[0].replace(combined)
assert combined.exists() and combined.stat().st_size>1000
scene.render.image_settings.file_format='PNG'
for entry in entries:
    scene.frame_set(entry['start']+10)
    scene.render.filepath=str(OUT/('Preview_'+entry['clip']+'.png'))
    bpy.ops.render.render(write_still=True)
info={'fps':15,'resolution':[480,600],'frames':cursor-1,'seconds':(cursor-1)/15,'clips':entries,
      'geometry':'Actual Blender animated model','engine':'EEVEE','preview_translation_only':True,
      'exported_clips':'in-place','combined_display_transform':'Standard; no second AgX pass'}
(OUT/'PreviewCheck.json').write_text(json.dumps(info,indent=2),encoding='utf-8')
print('PREVIEW_VERIFIED',json.dumps(info))
