"""Native Blender lighting previews; materials and geometry stay identical.
These are look-development examples, not a verified Unity shader match.
"""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/('Assets/ChibiSurvivor/FaceStudySquare' if '--square-jaw' in sys.argv else 'Assets/ChibiSurvivor/FaceStudySimple')
bpy.ops.wm.open_mainfile(filepath=str(OUT/'FaceStudy.blend'))
scene=bpy.context.scene
scene.name='Neutral_Lighting'
target=Vector((0,0,(934-220)*1.8/868))
cam=scene.camera
cam.location=(2.3,-5,target.z+.12)
cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.ortho_scale=.79
scene.render.resolution_x=900;scene.render.resolution_y=900
scene.cycles.samples=40
for o in list(scene.objects):
    if o.type=='LIGHT':bpy.data.objects.remove(o,do_unlink=True)
def light(name,pos,power,color,size):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=color;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos
    o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
light('Neutral_Key',(-3,-4,4),380,(1,1,1),4)
light('Neutral_Fill',(3,-3,2),110,(1,1,1),3)
scene.world=scene.world.copy()
bg=scene.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.055,.06,.065,1);bg.inputs[1].default_value=.5
scene['lighting_purpose']='Neutral material and silhouette check; not a Unity render'
scene.render.filepath=str(OUT/'FaceNeutral.png');bpy.ops.render.render(write_still=True)

bpy.ops.scene.new(type='FULL_COPY')
scene=bpy.context.scene;scene.name='Atmosphere_Lighting'
for o in list(scene.objects):
    if o.type=='LIGHT':bpy.data.objects.remove(o,do_unlink=True)
scene.world=scene.world.copy()
bg=scene.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.018,.028,.044,1);bg.inputs[1].default_value=.35
light('Shelter_Warm_Key',(-2,-3,3.8),190,(1,.83,.65),2.5)
light('Cool_Ambient_Fill',(3,-3,2),55,(.54,.68,1),3)
light('Cool_Back_Rim',(1.5,1.5,3),170,(.53,.71,1),2)
scene['lighting_purpose']='Example dim warm/cool apocalypse mood; shader/materials and mesh unchanged'
scene.render.filepath=str(OUT/'FaceAtmosphere.png');bpy.ops.render.render(write_still=True)
# Two editable scenes, same character and material values, independent lights.
bpy.context.window.scene=bpy.data.scenes['Neutral_Lighting']
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'FaceLightingStudy.blend'))
print('FACE_LIGHTING_STUDY_COMPLETE',str(OUT))
