"""Refine approved hero using Synty arm topology; isolated review, not runtime replacement."""
import bpy,math,bmesh,hashlib,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend'
OUT=ROOT/'Assets/ChibiSurvivor/Player/HeroRefinement';OUT.mkdir(parents=True,exist_ok=True)
(OUT/'BlenderSource~').mkdir(exist_ok=True)
source_hash=hashlib.sha256(SRC.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SRC));bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig']
rig.animation_data_clear()
for p in rig.pose.bones:p.matrix_basis.identity()
for o in scene.objects:
 if 'Sword' in o.name:o.hide_render=True;o.hide_set(True)
bpy.context.view_layer.update()
def point(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
cam=scene.camera;cam.data.type='ORTHO';cam.data.ortho_scale=1.98;cam.location=(2.7,-6,2.5);point(cam,(0,0,.84))
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=800;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
# Stable neutral illumination shared by before/after images.
for o in list(scene.objects):
 if o.type=='LIGHT':bpy.data.objects.remove(o,do_unlink=True)
for name,pos,power,size in [('Key',(-3,-4,5),450,4),('Fill',(4,-2,3),160,3),('Rim',(2,3,4),400,3)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);scene.collection.objects.link(o);o.location=pos;point(o,(0,0,.8))
scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.15,.17,.18,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.3
if not (OUT/'Before.png').exists():
 scene.render.filepath=str(OUT/'Before.png');bpy.ops.render.render(write_still=True)
materials={k:next(m for m in bpy.data.materials if m.name=='Hero_'+k or m.name.startswith('Hero_'+k+'.')) for k in ['skin','jacket','cuff','lapel','metal','leather','patch']}
sources=[]
def arm(side,part):
 sign=1 if side=='L' else -1;code=('11AUPL' if side=='L' else '12AUPR') if part=='upper' else ('13ALWL' if side=='L' else '14ALWR')
 f=ROOT/f'Assets/Synty/SidekickCharacters/Resources/Meshes/Species/Humans/SK_HUMN_BASE_01_{code}_HU01.fbx'
 old=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(f));new=set(bpy.data.objects)-old
 source_rig=next(o for o in new if o.type=='ARMATURE');mesh=next(o for o in new if o.type=='MESH')
 sb='upperarm_'+side.lower() if part=='upper' else 'lowerarm_'+side.lower();end='lowerarm_'+side.lower() if part=='upper' else 'hand_'+side.lower()
 a=source_rig.matrix_world@source_rig.data.bones[sb].head_local;b=source_rig.matrix_world@source_rig.data.bones[end].head_local
 ta=rig.matrix_world@rig.data.bones[('UpperArm.' if part=='upper' else 'Forearm.')+side].head_local
 tb=rig.matrix_world@rig.data.bones[('Forearm.' if part=='upper' else 'Hand.')+side].head_local
 axis=(b-a).normalized();dstaxis=(tb-ta).normalized();front=Vector((0,-1,0));sf=(front-axis*front.dot(axis)).normalized();ss=axis.cross(sf).normalized();df=(front-dstaxis*front.dot(dstaxis)).normalized();ds=dstaxis.cross(df).normalized()
 oldpart=bpy.data.objects[('Hero_Sleeve_' if part=='upper' else 'Hero_Forearm_')+str(sign)]
 mw=mesh.matrix_world.copy();baseverts=mesh.data.shape_keys.key_blocks[0].data if mesh.data.shape_keys else mesh.data.vertices
 vs=[]
 # Source cross-section is scaled to the existing hero's limb, not adult proportions.
 for v in baseverts:
  delta=mw@v.co-a;t=delta.dot(axis)/(b-a).length
  radial=delta-axis*delta.dot(axis)
  u=max(0,min(1,t))
  radius=(.080-.011*u+.005*math.sin(math.pi*u)) if part=='upper' else (.059-.018*u)
  if part=='upper' and t<.18:radius*=math.sqrt(max(.16,1-((t-.18)/.48)**2))
  # Match the approved cuff/wrist sizes. Raw Synty rest meshes are slender here.
  if radial.length>.003:radial=radial.normalized()*radius
  fit_t=t*1.16 if part=='upper' and t>0 else t
  p=ta+(tb-ta)*fit_t+df*radial.dot(sf)*.93+ds*radial.dot(ss)
  vs.append(p)
 faces=[tuple(p.vertices) for p in mesh.data.polygons]
 d=bpy.data.meshes.new('Refined_'+part+'_'+side);d.from_pydata(vs,[],faces);d.update()
 o=bpy.data.objects.new('Hero_Refined_'+part+'_'+side,d);scene.collection.objects.link(o)
 d.materials.append(materials['jacket' if part=='upper' else 'skin'])
 # Rebind the imported topology to the hero's existing limb bones.
 names={}
 for g in mesh.vertex_groups:
  n=g.name;dest=None
  if 'upperarm' in n:dest='UpperArm.'+side
  elif 'lowerarm' in n:dest='Forearm.'+side
  elif 'hand' in n or 'thumb' in n:dest='Hand.'+side
  elif 'clavicle' in n:dest='Clavicle.'+side
  elif 'spine' in n:dest='Chest'
  if dest:names[g.index]=dest
 groups={n:o.vertex_groups.new(name=n) for n in set(names.values())}
 for v in mesh.data.vertices:
  weights={}
  for g in v.groups:
   if g.group in names:weights[names[g.group]]=weights.get(names[g.group],0)+g.weight
  if not weights:weights={('UpperArm.' if part=='upper' else 'Forearm.')+side:1}
  total=sum(weights.values())
  for n,w in weights.items():
   if n not in groups:groups[n]=o.vertex_groups.new(name=n)
   groups[n].add([v.index],w/total,'REPLACE')
 mod=o.modifiers.new('HeroArmature','ARMATURE');mod.object=rig
 if part=='upper':
  # One local subdivision pass rounds the shoulder cap, keeping flat facet normals.
  sub=o.modifiers.new('ShoulderSurface','SUBSURF');sub.levels=1;sub.render_levels=1
 o['source_fbx']=str(f.relative_to(ROOT));o['purpose']='Synty arm topology fitted to approved hero limb proportions'
 oldpart.hide_render=True;oldpart.hide_set(True);oldpart.name='Backup_'+oldpart.name
 sources.append(o['source_fbx'])
 for obj in new:bpy.data.objects.remove(obj,do_unlink=True)
 return o
for side in ['L','R']:
 arm(side,'upper');arm(side,'lower')
def attach(o,bone,material):
 o.data.materials.clear();o.data.materials.append(material)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE');m=o.modifiers.new('HeroArmature','ARMATURE');m.object=rig
 return o
def box(name,loc,size,material,bone,bevel=.003):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name='Hero_'+name;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('ClothEdges','BEVEL');m.width=bevel;m.segments=1;bpy.ops.object.modifier_apply(modifier=m.name)
 return attach(o,bone,material)
# Restrained asymmetric clothing details, placed inside the established silhouette.
box('ChestPocket',(.095,-.128,1.015),(.083,.014,.088),materials['jacket'],'Chest',.004)
box('ChestPocketFlap',(.095,-.14,1.051),(.087,.012,.022),materials['cuff'],'Chest',.003)
box('PocketFastener',(.095,-.149,1.05),(.013,.004,.008),materials['metal'],'Chest',.001)
box('JacketHem',(0,-.118,.828),(.30,.012,.012),materials['cuff'],'Spine',.003)
box('RepairPatch',(-.133,-.09,.526),(.048,.008,.057),materials['patch'],'Thigh.R',.003)
for i in range(3):box('RepairStitch_'+str(i),(-.15+i*.017,-.095,.552),(.004,.003,.012),materials['leather'],'Thigh.R',.001)
# Give flat lapels thickness without inflating their outlines.
for o in scene.objects:
 if o.name.startswith('Hero_Lapel_'):
  s=o.modifiers.new('ClothThickness','SOLIDIFY');s.thickness=.004
  b=o.modifiers.new('LapelEdge','BEVEL');b.width=.0015;b.segments=1
bpy.context.view_layer.update()
for name,pos,target,scale in [('Preview',(2.7,-6,2.5),(0,0,.84),1.98),('Front',(0,-6,1.4),(0,0,.84),1.98),('Side',(6,0,1.4),(0,0,.84),1.98),('Face',(1,-4,1.65),(0,-.03,1.37),.78)]:
 cam.location=pos;point(cam,target);cam.data.ortho_scale=scale;scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
cam.location=(2.7,-6,2.5);point(cam,(0,0,.84));cam.data.ortho_scale=1.98
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/HeroRefinement.blend'))
assert hashlib.sha256(SRC.read_bytes()).hexdigest()==source_hash
print('REFINEMENT_COMPLETE',json.dumps({'unchanged_source':True,'synty_parts':sources,'runtime_replaced':False}))
