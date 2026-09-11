"""Compare the actual two rigged bodies at the user's 62 degree pitch / yaw 0."""
from pathlib import Path
R=Path(__file__).with_name('render_town_props02.py')
exec(compile(R.read_text(encoding='utf8').split('for group,specs in manifest')[0],str(R),'exec'),globals())
OUT=ROOT.parent/'ArtWork/BanditRoles62';sc.render.resolution_x=1200;sc.render.resolution_y=760;sc.cycles.samples=24
sources=[ROOT.parent/'ArtWork/CharacterProportionC/BlenderSource~/Bandit_C.blend',OUT/'BlenderSource~/Bandit_Ranged_C.blend']
target=Vector((0,0,.86));cam.location=target+Vector((0,10,10*math.tan(math.radians(62))));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=3.7
for label,actionName,frame,angle in [('Roles62','Idle',1,math.pi),('Movement62','Walk',12,math.pi+.3),('Rear62','Idle',1,0),('Side62','Idle',1,math.pi*1.5),('Run62','Run',8,math.pi+.3)]:
 if requested and label not in requested:continue
 frozen=[]
 for index,path in enumerate(sources):
  with bpy.data.libraries.load(str(path),link=False) as (src,dst):dst.objects=[n for n in src.objects if n in ['SimpleHero_Rig','Bandit_Body']];dst.actions=[actionName]
  for o in dst.objects:sc.collection.objects.link(o)
  rig=next(o for o in dst.objects if o.type=='ARMATURE');rig.data.pose_position='POSE';rig.animation_data_create();rig.animation_data.action=dst.actions[0]
  if dst.actions[0].slots:rig.animation_data.action_slot=dst.actions[0].slots[0]
  sc.frame_set(frame);bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
  for o in dst.objects:
   if o.type!='MESH':continue
   me=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);me.transform(o.matrix_world);cp=bpy.data.objects.new(label,me);sc.collection.objects.link(cp);cp.rotation_euler.z=angle;cp.location.x=(.5-index)*1.36;frozen.append(cp)
  for o in dst.objects:bpy.data.objects.remove(o,do_unlink=True)
 sc.render.filepath=str(OUT/f'{label}.png');bpy.ops.render.render(write_still=True)
 for o in frozen:bpy.data.objects.remove(o,do_unlink=True)
print('BANDIT_ROLES62_RENDERED')
