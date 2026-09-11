import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
folder=ROOT/'Assets/ChibiSurvivor/Bandit/SimpleBandit'
bpy.ops.wm.open_mainfile(filepath=str(folder/'BlenderSource~/SimpleBandit_Combat.blend'))
bpy.context.preferences.filepaths.save_version=0
rig=bpy.data.objects['SimpleHero_Rig'];club=bpy.data.objects['Bandit_Club']
rest=rig.data.bones['HandSocket.R'].matrix_local;inv=rest.inverted()
local=[inv@v.co for v in club.data.vertices]
center=Vector(((min(p.x for p in local)+max(p.x for p in local))/2,0,(min(p.z for p in local)+max(p.z for p in local))/2))
shift=Vector((-center.x,0,-.003-center.z))
for v in club.data.vertices:v.co=rest@((inv@v.co)+shift)
club.data.update()
bpy.ops.wm.save_as_mainfile(filepath=str(folder/'BlenderSource~/SimpleBandit_Combat.blend'))
bpy.ops.object.select_all(action='DESELECT');club.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(folder/'Weapons/BanditClub.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
print('GRIP_MESH_CENTERED',list(shift),flush=True)
