import bpy,json,shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor';OUT=BASE/'SurfaceReview';BACKUP=ROOT/'Library/CodexBlender/HeroSurface/BeforeSurface';BACKUP.mkdir(parents=True,exist_ok=True)
for rel in ['DarkSurvivor.fbx','BlenderSource~/DarkSurvivor.blend','DarkSurvivor.prefab']:
 src=BASE/rel;dst=BACKUP/src.name
 if not dst.exists():shutil.copy2(src,dst)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/DarkSurvivor_Textured.blend'));bpy.context.preferences.filepaths.save_version=0
rig=bpy.data.objects['DarkSurvivor_Rig'];exec(Path(__file__).with_name('clear_hero_elbow_cuffs.py').read_text());changed=clear_hero_elbow_cuffs(rig)
sc=bpy.context.scene;rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
sc.camera.location=(-3.4,-6,2.5);sc.camera.rotation_euler=(Vector((0,0,.94))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=2.25
sc.render.filepath=str(OUT/'Final.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'BlenderSource~/DarkSurvivor.blend'))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in sc.objects:
 if o.type=='MESH' and o.name.startswith('Hero_'):o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(BASE/'DarkSurvivor.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
print('HERO_SURFACE_PROMOTED',changed,flush=True)
