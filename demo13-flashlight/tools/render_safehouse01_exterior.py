import bpy
from pathlib import Path
from mathutils import Vector
out=Path(__file__).resolve().parents[1]/'Assets/Art/Environments/Safehouse01'
bpy.ops.wm.open_mainfile(filepath=str(out/'BlenderSource~/Safehouse01.blend'))
sc=bpy.context.scene;sc.cycles.samples=24;sc.render.filepath=str(out/'Previews/Overview.png')
bpy.ops.render.render(write_still=True)
for name in ['HiddenWalls','Roof']:
 for o in bpy.data.objects[name].children_recursive:o.hide_render=False;o.hide_set(False)
sc=bpy.context.scene;cam=sc.camera
cam.location=(8,-11,8);cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=9.3
sc.cycles.samples=24;sc.render.filepath=str(out/'Previews/Exterior.png')
bpy.ops.render.render(write_still=True)
