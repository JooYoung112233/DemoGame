"""Render the actual rigged NPC assets; studio review, not Unity screenshots."""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parents[1]
specs=json.loads((OUT/'BuildValidation.json').read_text(encoding='utf8'))
mode=sys.argv[-1] if '--' in sys.argv else 'stills'
bpy.ops.wm.read_factory_settings(use_empty=True);sc=bpy.context.scene;bpy.context.preferences.filepaths.save_version=0
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=48 if mode=='stills' else 16
sc.render.resolution_x=1440 if mode=='stills' else 1080;sc.render.resolution_y=760 if mode=='stills' else 600;sc.render.resolution_percentage=100
sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast';sc.view_settings.exposure=.45
world=bpy.data.worlds.new('Neutral review');world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.35,.39,.42,1);world.node_tree.nodes['Background'].inputs[1].default_value=.55;sc.world=world
def material(name,col):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*col,1);p.inputs['Roughness'].default_value=.94;return m
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='Review ground';floor.location.z=.020;floor.data.materials.append(material('Ground',(.085,.092,.087)))
for name,pos,power,size,color in [('Warm key',(-3,-4,7),650,5,(1,.88,.72)),('Sky fill',(4,-1,5),350,5,(.74,.84,1)),('Rim',(0,4,6),550,4,(1,.91,.77))]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color;o=bpy.data.objects.new(name,data);sc.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.8))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add();cam=bpy.context.object;sc.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=6.15
rigs=[]
for i,s in enumerate(specs):
 with bpy.data.libraries.load(str(OUT/'BlenderSource~'/(s['name']+'.blend')),link=False) as (src,dst):dst.objects=['SimpleHero_Rig','NPC_Body'];dst.actions=[s['motion']]
 for o in dst.objects:sc.collection.objects.link(o)
 r=next(o for o in dst.objects if o.type=='ARMATURE');r.animation_data_create();r.animation_data.action=dst.actions[0]
 if dst.actions[0].slots:r.animation_data.action_slot=dst.actions[0].slots[0]
 r.location.x=(i-1.5)*1.43;rigs.append(r)
def camera(pitch):
 target=Vector((0,0,.88));cam.location=target+Vector((0,-8,8*math.tan(math.radians(pitch))));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
if mode in ['stills','all']:
 sc.eevee.taa_render_samples=48;sc.render.resolution_x=1440;sc.render.resolution_y=760
 sc.render.image_settings.file_format='PNG'
 for label,pitch,angle,frame in [('Front',18,0,1),('Top62',62,0,1),('Rear62',62,math.pi,46),('Quarter',32,math.radians(-25),23),('Side',22,math.pi/2,68),('End',18,0,91)]:
  camera(pitch)
  for r in rigs:r.rotation_euler.z=angle
  sc.frame_set(frame);sc.render.filepath=str(OUT/'Review'/(label+'.png'));bpy.ops.render.render(write_still=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'Review'/'LineupReview.blend'),compress=True)
if mode!='stills':
 sc.eevee.taa_render_samples=24;sc.render.resolution_x=1080;sc.render.resolution_y=600
 for r in rigs:r.rotation_euler.z=0
 for pitch,name in ([(62,'Idle62'),(26,'IdleFront')] if mode=='all' else [(62 if mode=='video62' else 26,'Idle62' if mode=='video62' else 'IdleFront')]):
  camera(pitch)
  sc.frame_start=1;sc.frame_end=89;sc.frame_step=2;sc.render.fps=15
  sc.render.image_settings.file_format='FFMPEG';sc.render.ffmpeg.format='MPEG4';sc.render.ffmpeg.codec='H264';sc.render.ffmpeg.constant_rate_factor='HIGH';sc.render.filepath=str(OUT/(name+'.mp4'))
  bpy.ops.render.render(animation=True)
print('NPC_REVIEW_RENDERED',mode,flush=True)
