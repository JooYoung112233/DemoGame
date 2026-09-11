import bpy,json,hashlib,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];CACHE=ROOT/'Library/CodexBlender/HeroAuthoredUV'
exec(Path(__file__).with_name('apply_hero_surface_to_scene.py').read_text())
targets=[(ROOT/'Library/CodexBlender/AxeReview/FittedReview.blend',None),(ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview/BlenderSource~/DarkSurvivor_AxeChop.blend',ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview/DarkSurvivor_AxeChop.fbx')]
reports=[]
for path,fbx in targets:
 backup=CACHE/(path.stem+'_BeforeUV.blend')
 if not backup.exists():shutil.copy2(path,backup)
 bpy.ops.wm.open_mainfile(filepath=str(path));bpy.context.preferences.filepaths.save_version=0
 rig=bpy.data.objects['DarkSurvivor_Rig']
 def invariant():
  return hashlib.sha256(repr(([(o.name,[tuple(v.co) for v in o.data.vertices], [[(g.group,g.weight) for g in v.groups] for v in o.data.vertices]) for o in bpy.context.scene.objects if o.type=='MESH'],[(b.name,tuple(b.head_local),tuple(b.tail_local)) for b in rig.data.bones],[(a.name,[(f.data_path,f.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right)) for k in f.keyframe_points]) for f in a.fcurves]) for a in bpy.data.actions])).encode()).hexdigest()
 before=invariant();count=apply_hero_surface_to_scene(ROOT);assert before==invariant(),'Axe model or motion changed'
 bpy.ops.wm.save_as_mainfile(filepath=str(path))
 if fbx:
  bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
  for o in bpy.context.scene.objects:
   if o.type=='MESH' and (o.name.startswith('Hero_') or o.name.startswith('Review_Axe_')) and o.name!='Hero_SwordProxy':o.hide_set(False);o.select_set(True)
  bpy.context.view_layer.objects.active=rig
  bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
 reports.append({'source':str(path),'parts':count,'model_weights_motion_preserved':True})
(CACHE/'AxeUVSync.json').write_text(json.dumps(reports,indent=2));print('AXE_UV_SYNC_READY',flush=True)
