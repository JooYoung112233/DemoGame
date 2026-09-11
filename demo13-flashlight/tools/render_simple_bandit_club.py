import bpy,math
from pathlib import Path
from mathutils import Vector,Matrix
root=Path(__file__).resolve().parents[1];out=root.parent/'ArtWork/SimpleBandit/CombatReview'
bpy.ops.wm.open_mainfile(filepath=str(root/'Assets/ChibiSurvivor/Bandit/SimpleBandit/BlenderSource~/SimpleBandit_Combat.blend'))
s=bpy.context.scene;r=bpy.data.objects['SimpleHero_Rig'];club=bpy.data.objects['Bandit_Club'];inv=r.data.bones['HandSocket.R'].matrix_local.inverted()
bpy.data.objects['Bandit_Body'].hide_render=True
for mod in list(club.modifiers):club.modifiers.remove(mod)
for v in club.data.vertices:v.co=inv@v.co;v.co.x-=.1;v.co.z+=.025
club.parent=None;club.matrix_world=Matrix.Identity(4);club.rotation_euler=(math.pi/2,0,0);club.location.z=.198
s.camera.location=(1.1,-2,1);s.camera.rotation_euler=(Vector((0,0,.41))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.ortho_scale=1.04
s.render.resolution_x=800;s.render.resolution_y=800;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.filepath=str(out/'BanditClub.png');bpy.ops.render.render(write_still=True)
