import bpy,json,hashlib,shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor';OUT=BASE/'SurfaceReview';CACHE=ROOT/'Library/CodexBlender/HeroProportions';CACHE.mkdir(parents=True,exist_ok=True)
exec(Path(__file__).with_name('refine_hero_proportions.py').read_text())
files=[(BASE/'BlenderSource~/DarkSurvivor.blend',BASE/'DarkSurvivor.fbx'),(OUT/'BlenderSource~/DarkSurvivor_Textured.blend',OUT/'DarkSurvivor_Textured.fbx'),(ROOT/'Library/CodexBlender/AxeReview/FittedReview.blend',None),(ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview/BlenderSource~/DarkSurvivor_AxeChop.blend',ROOT/'Assets/ChibiSurvivor/Player/AxeChopReview/DarkSurvivor_AxeChop.fbx')]
reports=[]
for index,(path,fbx) in enumerate(files):
 backup=CACHE/(path.stem+'_Before.blend')
 if not backup.exists():shutil.copy2(path,backup)
 bpy.ops.wm.open_mainfile(filepath=str(path));bpy.context.preferences.filepaths.save_version=0;sc=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig'];parts=[o for o in sc.objects if o.type=='MESH' and o.name.startswith('Hero_')]
 def invariant():
  return hashlib.sha256(repr(([(o.name,[tuple(p.vertices) for p in o.data.polygons], [[(g.group,g.weight) for g in v.groups] for v in o.data.vertices],[tuple(u.uv) for u in o.data.uv_layers.active.data]) for o in parts],[(b.name,tuple(b.head_local),tuple(b.tail_local),b.parent.name if b.parent else None) for b in rig.data.bones],[(a.name,[(f.data_path,f.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right)) for k in f.keyframe_points]) for f in a.fcurves]) for a in bpy.data.actions])).encode()).hexdigest()
 before=invariant();rig.data.pose_position='REST';bpy.context.view_layer.update()
 old_bounds={o.name:[min(v.co[i] for v in o.data.vertices) for i in range(3)]+[max(v.co[i] for v in o.data.vertices) for i in range(3)] for o in parts if o.name in ['Hero_Head','Hero_Cap','Hero_JacketBody']}
 snapshots=[]
 def snapshot(label):
  bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();items=[]
  for o in parts:
   if o.name=='Hero_SwordProxy':continue
   d=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);copy=bpy.data.objects.new('Compare_'+label+'_'+o.name,d);sc.collection.objects.link(copy);copy.matrix_world=o.matrix_world.copy();copy.hide_render=True;items.append(copy)
  return items
 def render(name,loc=(2,-6,2.2)):
  sc.camera.location=loc;sc.camera.rotation_euler=(Vector((0,0,.89))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=2.15;sc.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
 if index==0:
  rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1);sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=48;sc.render.resolution_x=850;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
  for o in parts:o.hide_render=o.name=='Hero_SwordProxy'
  render('Proportion_Before');snapshots=snapshot('Before')
 rig.data.pose_position='REST';bpy.context.view_layer.update();changed=refine_hero_proportions();assert invariant()==before,'Topology/UV/weights/skeleton/motion changed'
 if index==0:
  rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1);render('Proportion_After');render('Proportion_Side',(5,-.5,1.8))
  after_snapshot=snapshot('After')
  for o in parts:o.hide_render=True
  for o in snapshots:o.hide_render=False;o.location.x-=.70
  for o in after_snapshot:o.hide_render=False;o.location.x+=.70
  sc.render.resolution_x=1500;render('Proportion_Comparison',(0,-6,1.7))
  for o in snapshots+after_snapshot:
   mesh=o.data;bpy.data.objects.remove(o,do_unlink=True);bpy.data.meshes.remove(mesh)
  sc.render.resolution_x=850
  for o in parts:o.hide_render=o.name=='Hero_SwordProxy'
  for action,frame in [('Run',7),('SwordSlash',16)]:rig.animation_data.action=bpy.data.actions[action];sc.frame_set(frame);render('Proportion_'+action)
  rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
 else:rig.data.pose_position='POSE'
 bpy.ops.wm.save_as_mainfile(filepath=str(path))
 if fbx:
  bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
  for o in sc.objects:
   if o.type=='MESH' and (o.name.startswith('Hero_') or o.name.startswith('Review_Axe_')):
    if 'AxeChop' in str(path) and o.name=='Hero_SwordProxy':continue
    o.hide_set(False);o.select_set(True)
  bpy.context.view_layer.objects.active=rig
  bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
 reports.append({'source':str(path),'changed':changed,'uv_topology_weights_skeleton_motion_preserved':True,'before_bounds':old_bounds,'after_bounds':{o.name:[min(v.co[i] for v in o.data.vertices) for i in range(3)]+[max(v.co[i] for v in o.data.vertices) for i in range(3)] for o in parts if o.name in old_bounds}})
(OUT/'ProportionCheck.json').write_text(json.dumps(reports,indent=2));print('HERO_PROPORTIONS_READY',flush=True)
