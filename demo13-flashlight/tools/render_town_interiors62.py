"""Production model review at the requested 62-degree camera; not Unity."""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT.parent/'ArtWork/TownInteriors62'
partition='--partition' in sys.argv
if partition:OUT=ROOT.parent/'ArtWork/TownInteriorsPartition62'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/TownInteriors62.blend'))
manifest=json.loads((OUT/'KitManifest.json').read_text(encoding='utf8'))
sc=bpy.context.scene;sc.render.engine='CYCLES';sc.cycles.device='CPU';sc.cycles.samples=24;sc.cycles.use_denoising=True
sc.render.resolution_x=1320;sc.render.resolution_y=1060;sc.render.resolution_percentage=100
sc.view_settings.view_transform='AgX';sc.view_settings.exposure=.55
world=bpy.data.worlds.new('Interior review ambient');sc.world=world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.38,.42,.47,1);world.node_tree.nodes['Background'].inputs[1].default_value=.6
review=bpy.data.collections.new('REVIEW_ONLY');sc.collection.children.link(review)
def link(o):review.objects.link(o);return o
for name,pos,power,size,color in [('Window fill',(-8,4,18),2200,10,(1,.89,.72)),('Skylight',(9,1,17),1600,12,(.73,.82,1)),('Rear bounce',(0,-9,12),900,8,(1,.87,.70))]:
 ld=bpy.data.lights.new(name,'AREA');ld.energy=power;ld.shape='DISK';ld.size=size;ld.color=color
 o=link(bpy.data.objects.new(name,ld));o.location=pos;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200);plane=bpy.context.object;plane.location.z=-.20
ground=bpy.data.materials.new('Review background');ground.use_nodes=True;p=ground.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(.04,.046,.045,1);p.inputs['Roughness'].default_value=.95;plane.data.materials.append(ground)
data=bpy.data.cameras.new('62 degree top-down');cam=link(bpy.data.objects.new('62 degree top-down',data));data.type='ORTHO';sc.camera=cam
# Include the exact approved C player as a scale witness, with original textures/rig.
source=ROOT.parent/'ArtWork/CharacterProportionC/BlenderSource~/Player_C.blend'
if source.exists():
 with bpy.data.libraries.load(str(source),link=False) as (src,dst):
  dst.objects=[n for n in src.objects if n.startswith(('Study_','Gear_')) or n=='SimpleHero_Rig'];dst.actions=['Idle']
 for o in dst.objects:
  if o and o.name not in sc.objects:sc.collection.objects.link(o)
 rig=next((o for o in dst.objects if o and o.type=='ARMATURE'),None)
 if rig:
  idle=dst.actions[0]
  if idle:rig.animation_data_create();rig.animation_data.action=idle
  if idle and idle.slots:rig.animation_data.action_slot=idle.slots[0]
  sc.frame_set(1);bpy.context.view_layer.update()
  deps=bpy.context.evaluated_depsgraph_get()
  for o in list(dst.objects):
   if not o or o.type!='MESH':continue
   me=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);me.transform(o.matrix_world)
   cp=link(bpy.data.objects.new('C_player_scale_witness',me))
   cp.location=(0,2.5,0);cp.rotation_euler.z=math.pi
  for o in dst.objects:
   if o:o.hide_render=True
requested=set(sys.argv[sys.argv.index('--')+1:])-{'--partition','--service','--same-scale'} if '--' in sys.argv else None
detail=bool(requested and 'DETAIL' in requested)
for name,spec in manifest['buildings'].items():
 if requested and name not in requested:continue
 col=bpy.data.collections[name+'_Assembly62'];col.hide_render=False;col.hide_viewport=False
 for o in review.objects:
  if o.name.startswith('C_player_scale_witness'):
   x,z=spec.get('previewPlayer',[0,-2.5]);o.location=(-x,-z,.025)
 if '--service' in sys.argv:
  for o in col.objects:
   if str(o.get('asset','')).endswith(('ClosureRoof62','ClosedFloor62','OuterOutline62','ClosedFrontReturn62','RoomDividers62')):o.hide_render=True
 target=Vector((0,0,.25));angle=math.radians(62);cam.location=target+Vector((0,20*math.cos(angle),20*math.sin(angle)));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
 bpy.context.view_layer.update();pts=[o.matrix_world@Vector(c) for o in col.objects if not o.hide_render for c in o.bound_box]
 view=cam.rotation_euler.to_matrix().transposed();pp=[view@(p-target) for p in pts]
 w=max(p.x for p in pp)-min(p.x for p in pp);h=max(p.y for p in pp)-min(p.y for p in pp)
 data.ortho_scale=max(w,h*sc.render.resolution_x/sc.render.resolution_y)*1.10
 if '--same-scale' in sys.argv:data.ortho_scale=21
 centre=Vector(((max(p.x for p in pp)+min(p.x for p in pp))/2,(max(p.y for p in pp)+min(p.y for p in pp))/2,0))
 cam.location+=cam.rotation_euler.to_matrix()@centre
 if detail:
  target=Vector((0,-.6,.35));cam.location=target+Vector((0,20*math.cos(angle),20*math.sin(angle)));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=10.5
 suffix='Service' if '--service' in sys.argv else 'SameScale' if '--same-scale' in sys.argv else 'Detail' if detail else ''
 sc.render.filepath=str(OUT/f'{name}{suffix}62.png');bpy.ops.render.render(write_still=True)
 col.hide_render=True;col.hide_viewport=True
print('TOWN_INTERIORS62_RENDERED')
