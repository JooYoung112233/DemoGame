import bpy
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];BASE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor';OUT=BASE/'SurfaceReview'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Library/CodexBlender/HeroProportions/DarkSurvivor_Before.blend'))
sc=bpy.context.scene;old=[o for o in sc.objects if o.type=='MESH' and o.name.startswith('Hero_') and o.name!='Hero_SwordProxy'];rig=bpy.data.objects['DarkSurvivor_Rig'];rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
with bpy.data.libraries.load(str(BASE/'BlenderSource~/DarkSurvivor.blend'),link=False) as (src,dst):
 dst.objects=[n for n in src.objects if n=='DarkSurvivor_Rig' or (n.startswith('Hero_') and n!='Hero_SwordProxy')]
new=[]
for o in dst.objects:
 sc.collection.objects.link(o)
 if o.type=='MESH':new.append(o)
sc.frame_set(1);bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();frozen=[]
for group,shift in [(old,-.70),(new,.70)]:
 for o in group:
  mesh=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);c=bpy.data.objects.new('Compare',mesh);sc.collection.objects.link(c);c.matrix_world=o.matrix_world.copy();c.location.x+=shift;frozen.append(c)
for o in sc.objects:
 if o.type=='MESH' and (o.name.startswith('Hero_')):o.hide_render=True
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=64;sc.render.resolution_x=1500;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.camera.data.type='ORTHO';sc.camera.data.ortho_scale=3.15;sc.camera.location=(0,-6,1.65);sc.camera.rotation_euler=(Vector((0,0,.88))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/'Proportion_Comparison.png');bpy.ops.render.render(write_still=True)
