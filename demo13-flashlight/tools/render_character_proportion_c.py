"""Player and shared bandit body, before/approved C, same studio camera."""
from pathlib import Path
R=Path(__file__).with_name('render_town_props02.py')
exec(compile(R.read_text(encoding='utf8').split('for group,specs in manifest')[0],str(R),'exec'),globals())
OUT=ROOT.parent/'ArtWork/CharacterProportionC'
sc.render.resolution_x=1400;sc.render.resolution_y=720;sc.cycles.samples=24
sources=[('PlayerBefore',ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_Stage5_Slash.blend'),('PlayerC',OUT/'BlenderSource~/Player_C.blend'),('BanditBefore',ROOT/'Assets/ChibiSurvivor/Bandit/SimpleBandit/BlenderSource~/SimpleBandit.blend'),('BanditC',OUT/'BlenderSource~/Bandit_C.blend')]
frozen=[]
for index,(label,path) in enumerate(sources):
 with bpy.data.libraries.load(str(path),link=False) as (src,dst):
  dst.objects=[n for n in src.objects if n.startswith(('Study_','Gear_')) or n in ['SimpleHero_Rig','Bandit_Body']];dst.actions=['Idle']
 for o in dst.objects:sc.collection.objects.link(o)
 rig=next(o for o in dst.objects if o.type=='ARMATURE');rig.data.pose_position='POSE';rig.animation_data_create();rig.animation_data.action=dst.actions[0]
 if dst.actions[0].slots:rig.animation_data.action_slot=dst.actions[0].slots[0]
 sc.frame_set(1);bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get()
 for o in dst.objects:
  if o.type!='MESH':continue
  me=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);me.transform(o.matrix_world)
  cp=bpy.data.objects.new(label+'_'+o.name,me);sc.collection.objects.link(cp);cp.rotation_euler.z=math.pi;cp.location.x=-(index-1.5)*1.3;frozen.append(cp)
 for o in dst.objects:bpy.data.objects.remove(o,do_unlink=True)
target=Vector((0,0,.82));cam.location=target+Vector((0,12,12));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=6.5
sc.render.filepath=str(OUT/'BeforeAfter.png');bpy.ops.render.render(write_still=True)
for cp in frozen:cp.rotation_euler.z=math.pi+.45
cam.location=target+Vector((0,12,12));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
sc.render.filepath=str(OUT/'QuarterBeforeAfter.png');bpy.ops.render.render(write_still=True)
print('PROPORTION_C_RENDERED')
