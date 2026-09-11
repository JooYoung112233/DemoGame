import bpy,json,hashlib,shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor';CACHE=ROOT/'Library/CodexBlender/HeroCapJoin';CACHE.mkdir(parents=True,exist_ok=True)
exec(Path(__file__).with_name('fix_hero_cap_join.py').read_text())
files=[(BASE/'BlenderSource~/DarkSurvivor.blend',BASE/'DarkSurvivor.fbx'),(BASE/'SurfaceReview/BlenderSource~/DarkSurvivor_Textured.blend',BASE/'SurfaceReview/DarkSurvivor_Textured.fbx'),(ROOT/'Library/CodexBlender/AxeReview/FittedReview.blend',None),(ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview/BlenderSource~/DarkSurvivor_AxeChop.blend',ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview/DarkSurvivor_AxeChop.fbx')]
reports=[]
for path,fbx in files:
 backup=CACHE/(path.stem+'_Before.blend')
 if not backup.exists():shutil.copy2(path,backup)
 bpy.ops.wm.open_mainfile(filepath=str(path));bpy.context.preferences.filepaths.save_version=0;sc=bpy.context.scene;cap=bpy.data.objects['Hero_Cap']
 def unchanged():return hashlib.sha256(repr(([(o.name,[tuple(v.co) for v in o.data.vertices]) for o in sc.objects if o.type=='MESH' and o!=cap],[(a.name,[(f.data_path,f.array_index,[tuple(k.co) for k in f.keyframe_points]) for f in a.fcurves]) for a in bpy.data.actions],[tuple(u.uv) for u in cap.data.uv_layers.active.data])).encode()).hexdigest()
 before=unchanged()
 if path==files[0][0]:
  rig=bpy.data.objects['DarkSurvivor_Rig'];rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
  sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=32;sc.render.resolution_x=850;sc.render.resolution_y=650;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
  target=cap.matrix_world@Vector((0,-.12,1.56));sc.camera.location=target+Vector((3,-.45,.4));sc.camera.rotation_euler=(target-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=.85;sc.render.filepath=str(CACHE/'Before.png');bpy.ops.render.render(write_still=True)
 report=fix_hero_cap_join(cap);assert unchanged()==before,'Unrelated geometry, animation or UV changed'
 if path==files[0][0]:
  sc.render.filepath=str(CACHE/'After.png');bpy.ops.render.render(write_still=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(path))
 if fbx:
  bpy.ops.object.select_all(action='DESELECT');rig=bpy.data.objects['DarkSurvivor_Rig'];rig.select_set(True)
  for o in sc.objects:
   if o.type=='MESH' and (o.name.startswith('Hero_') or o.name.startswith('Review_Axe_')):
    if 'AxeChop' in str(path) and o.name=='Hero_SwordProxy':continue
    o.hide_set(False);o.select_set(True)
  bpy.context.view_layer.objects.active=rig
  bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
 reports.append({'source':str(path),'result':report,'unrelated_data_preserved':True})
(CACHE/'CapJoinCheck.json').write_text(json.dumps(reports,indent=2));print('CAP_JOIN_FIXED',flush=True)
