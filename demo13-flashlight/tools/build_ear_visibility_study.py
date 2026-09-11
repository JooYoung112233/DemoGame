"""Reversible ear visibility comparison; no geometry or facial changes."""
import bpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/ChibiSurvivor/FaceStudySquare'
OUT=ROOT/'Assets/ChibiSurvivor/EarVisibilityStudy'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'FaceLightingStudy.blend'))
for source_name,new_name,filename in [
    ('Neutral_Lighting','NoEars_Neutral','NoEarsNeutral.png'),
    ('Atmosphere_Lighting','NoEars_Atmosphere','NoEarsAtmosphere.png'),
]:
    bpy.context.window.scene=bpy.data.scenes[source_name]
    bpy.ops.scene.new(type='FULL_COPY')
    scene=bpy.context.scene;scene.name=new_name
    ears=[o for o in scene.objects if o.type=='MESH' and o.name.split('.')[0]=='Ear']
    assert len(ears)==2, 'Expected exactly two ear objects'
    for ear in ears:
        ear.hide_render=True;ear.hide_viewport=True
        ear['study_visibility']='Temporarily hidden for user comparison; geometry retained'
    scene['study']='Ear visibility only; unchanged head/body/material/camera/lighting'
    scene['approval']='Experiment only; ear removal is not a confirmed design decision'
    scene.render.filepath=str(OUT/filename)
    bpy.ops.render.render(write_still=True)
bpy.context.window.scene=bpy.data.scenes['NoEars_Neutral']
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'EarStudy.blend'))
print('EAR_VISIBILITY_STUDY_COMPLETE',str(OUT),'ears preserved; visibility only')
