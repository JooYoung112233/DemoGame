"""Offline model review; does not connect to or interrupt the Unity Editor.
This is a Blender studio preview, never labelled as an in-game screenshot.
"""
import bpy,math,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
requested=set(sys.argv[sys.argv.index('--')+1:]) if '--' in sys.argv else None
OUT=ROOT.parent/'ArtWork/TownReview'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/Art/Environments/Town02/BlenderSource~/Town02.blend'))
if bpy.data.collections.get('Town02_Editable_Layout'):bpy.data.collections['Town02_Editable_Layout'].hide_render=True
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.device='CPU';sc.cycles.samples=24;sc.cycles.use_denoising=True
sc.render.resolution_x=960;sc.render.resolution_y=800;sc.render.resolution_percentage=100
sc.view_settings.view_transform='AgX';sc.view_settings.exposure=.65
if sc.world is None:sc.world=bpy.data.worlds.new('Town review world')
sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.40,.43,.47,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.7
studio=bpy.data.collections.new('Review lighting');sc.collection.children.link(studio)
def move(o):
 for c in list(o.users_collection):c.objects.unlink(o)
 studio.objects.link(o)
bpy.ops.mesh.primitive_plane_add(size=200);plane=bpy.context.object;plane.location.z=-.08;move(plane)
m=bpy.data.materials.new('Studio ground');m.diffuse_color=(.085,.092,.09,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=m.diffuse_color;m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.94;plane.data.materials.append(m)
for name,pos,power,size,color in [('Warm key',(-8,5,20),2600,12,(1,.87,.69)),('Sky fill',(10,5,15),1500,15,(.70,.80,1)),('Back light',(0,-12,12),1600,10,(1,.9,.76))]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color;o=bpy.data.objects.new(name,data);studio.objects.link(o);o.location=pos;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Review camera');cam=bpy.data.objects.new('Review camera',data);studio.objects.link(cam);sc.camera=cam;data.type='ORTHO'
def camera(size,target=(0,0,2)):
 cam.location=Vector(target)+Vector((-12,20,25));cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=size
for name in ['Pawnshop','Repair','Medical','Furniture','BlackMarket','Container_Home']:
 if requested and name not in requested:continue
 cols=[bpy.data.collections[name+'_'+k+'_Editable'] for k in ['Shell','Roof']]
 for col in cols:col.hide_render=False
 camera(24 if name in ['Medical','Pawnshop'] else 21)
 if name=='Container_Home':
  cam.location=Vector((0,0,2))+Vector((12,20,25));cam.rotation_euler=(Vector((0,0,2))-cam.location).to_track_quat('-Z','Y').to_euler()
 sc.render.filepath=str(OUT/f'Model_{name}.png');bpy.ops.render.render(write_still=True)
 for col in cols:col.hide_render=True
# Present the new small props together at their relative physical scale.
for name,pos in [('SpoolTable02',(-1.9,-.4,0)),('YardChair02',(-2.5,1,0)),('Planter02',(1.4,-1.6,0)),('MerchantCart02',(1.2,.8,0))]:
 col=bpy.data.collections[name+'_Editable'];col.hide_render=False
 for o in col.objects:o.location+=Vector(pos)
if not requested or 'CourtyardProps' in requested:
 camera(7.8,(0,0,.6));sc.render.filepath=str(OUT/'Model_CourtyardProps.png');bpy.ops.render.render(write_still=True)
print('TOWN_OFFLINE_PREVIEWS_COMPLETE')
