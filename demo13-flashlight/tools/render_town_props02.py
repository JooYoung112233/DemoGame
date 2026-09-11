"""Blender studio previews only. Never connects to the live Unity Editor."""
import bpy,json,math,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];KIT=ROOT/'Assets/Art/Environments/TownProps02';OUT=ROOT.parent/'ArtWork/TownPropsReview';OUT.mkdir(parents=True,exist_ok=True)
requested=set(sys.argv[sys.argv.index('--')+1:]) if '--' in sys.argv else None
bpy.ops.wm.open_mainfile(filepath=str(KIT/'BlenderSource~/TownProps02.blend'))
bpy.data.collections['TownProps02_Editable_Layout'].hide_render=True
manifest=json.loads((KIT/'KitManifest.json').read_text(encoding='utf8'))
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.device='CPU';sc.cycles.samples=24;sc.cycles.use_denoising=True
sc.render.resolution_x=1100;sc.render.resolution_y=850;sc.render.resolution_percentage=100
sc.view_settings.view_transform='AgX';sc.view_settings.exposure=.65
if sc.world is None:sc.world=bpy.data.worlds.new('Props review world')
sc.world.use_nodes=True;bg=sc.world.node_tree.nodes['Background'];bg.inputs[0].default_value=(.4,.43,.47,1);bg.inputs[1].default_value=.7
bpy.ops.mesh.primitive_plane_add(size=200);plane=bpy.context.object;plane.location.z=-.035
m=bpy.data.materials.new('Review ground');m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(.085,.092,.09,1);p.inputs['Roughness'].default_value=.94;plane.data.materials.append(m)
for name,pos,power,size,color in [('Warm key',(-8,5,20),2600,12,(1,.87,.69)),('Sky fill',(10,5,15),1500,15,(.70,.80,1)),('Back light',(0,-12,12),1600,10,(1,.9,.76))]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color;o=bpy.data.objects.new(name,data);sc.collection.objects.link(o);o.location=pos;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('Quarter view');cam=bpy.data.objects.new('Quarter view',data);sc.collection.objects.link(cam);sc.camera=cam;data.type='ORTHO'
for group,specs in manifest['reviewGroups'].items():
 if requested and group not in requested:continue
 copies=[]
 for name,pos,yaw in specs:
  for o in bpy.data.collections[name+'_Editable'].objects:
   cp=o.copy();sc.collection.objects.link(cp);cp.location=(-pos[0],-pos[2],pos[1]);cp.rotation_euler.z=-math.radians(yaw);copies.append(cp)
 bpy.context.view_layer.update()
 points=[cp.matrix_world@Vector(corner) for cp in copies for corner in cp.bound_box]
 target=Vector([(min(p[i] for p in points)+max(p[i] for p in points))/2 for i in range(3)])
 cam.location=target+Vector((-6,10,12));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
 view=cam.rotation_euler.to_matrix().transposed();projected=[view@(p-target) for p in points]
 w=max(p.x for p in projected)-min(p.x for p in projected);h=max(p.y for p in projected)-min(p.y for p in projected)
 data.ortho_scale=max(w,h*sc.render.resolution_x/sc.render.resolution_y)*1.20
 sc.render.filepath=str(OUT/f'{group}.png');bpy.ops.render.render(write_still=True)
 for o in copies:bpy.data.objects.remove(o,do_unlink=True)
print('TOWN_PROPS_PREVIEWS_COMPLETE')
