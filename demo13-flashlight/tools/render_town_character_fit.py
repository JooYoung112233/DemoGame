"""Frozen hero/environment comparison. Never writes a runtime character or scene.
All cameras and lighting are identical. A/B isolate ground scale; B/C isolate
proportion changes. C is an unrigged design study, not an animation-ready model.
"""
from pathlib import Path
RENDER=Path(__file__).with_name('render_town_props02.py')
exec(compile(RENDER.read_text(encoding='utf8').split('for group,specs in manifest')[0],str(RENDER),'exec'),globals())
import hashlib
OUT=ROOT.parent/'ArtWork/TownCharacterFit';OUT.mkdir(parents=True,exist_ok=True)
source=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_Stage5_Slash.blend'
digest=hashlib.sha256(source.read_bytes()).hexdigest()
with bpy.data.libraries.load(str(source),link=False) as (src,dst):
 dst.objects=[n for n in src.objects if n.startswith(('Study_','Gear_')) or n=='SimpleHero_Rig'];dst.actions=['Idle']
for o in dst.objects:sc.collection.objects.link(o)
rig=bpy.data.objects['SimpleHero_Rig'];rig.data.pose_position='POSE';rig.animation_data_create();rig.animation_data.action=dst.actions[0]
if dst.actions[0].slots:rig.animation_data.action_slot=dst.actions[0].slots[0]
sc.frame_set(1);bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();frozen=[]
for o in dst.objects:
 if o.type=='MESH':
  me=bpy.data.meshes.new_from_object(o.evaluated_get(deps),depsgraph=deps);me.transform(o.matrix_world);frozen.append((o.name,me))
for o in dst.objects:o.hide_render=True
verts=[v.co for n,me in frozen for v in me.vertices];base=min(v.z for v in verts);height=max(v.z for v in verts)-base
head=[v.co for n,me in frozen if n=='Study_Head' for v in me.vertices];pivot=Vector([(min(v[i] for v in head)+max(v[i] for v in head))/2 for i in range(3)])
for name,pos in [('UtilitySink02',(-1.35,0,.25)),('WaterBarrelStand02',(1.3,0,.45)),('Toolbox02',(.72,0,-.65))]:
 for o in bpy.data.collections[name+'_Editable'].objects:
  cp=o.copy();sc.collection.objects.link(cp);cp.location=(-pos[0],-pos[2],pos[1])
cam.location=Vector((0,0,.75))+Vector((-10,10,math.sqrt(200)));cam.rotation_euler=(Vector((0,0,.75))-cam.location).to_track_quat('-Z','Y').to_euler();data.ortho_scale=5.3
sc.render.resolution_x=960;sc.render.resolution_y=850;sc.cycles.samples=20
def material(name,color):
 m=bpy.data.materials.new(name);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.94;return m
groundA=[material('Large tile '+str(i),(.105+i*.002,.11+i*.002,.10+i*.002)) for i in range(4)]
groundB=[material('Small paving '+str(i),(.096+i*.004,.103+i*.004,.094+i*.004)) for i in range(4)]
originalFloor=plane.data.materials[0]
for label,step,gap,proposed in [('A_LargeTiles',2.0,.035,False),('B_HumanScaleGround',.80,.009,False),('C_ProportionStudy',.80,.009,True)]:
 objects=[]
 plane.data.materials[0]=material('Joint '+label,(.024,.027,.022) if label.startswith('A') else (.072,.079,.071))
 for ix in range(-5,6):
  for iy in range(-5,6):
   bpy.ops.mesh.primitive_plane_add(size=1,location=(ix*step,iy*step,-.029));o=bpy.context.object;o.scale=(step-gap,step-gap,1);o.data.materials.append((groundA if label.startswith('A') else groundB)[(ix*7+iy*3)%4]);objects.append(o)
 for name,me in frozen:
  copy=me.copy();o=bpy.data.objects.new(label+'_'+name,copy);sc.collection.objects.link(o);objects.append(o)
  for v in copy.vertices:
   if proposed:
    if name in ['Study_Head','Study_Cap','Study_CapBrim']:
     v.co=pivot+(v.co-pivot)*.90;v.co.z+=(pivot.z-base)*.08
    else:
     v.co.z=base+(v.co.z-base)*1.08
     if name in ['Study_Torso','Study_Hips']:v.co.y*=1.12
   v.co.z-=base
  # Source hero faces Blender -Y; turn towards the front of the prop study.
  o.rotation_euler.z=math.pi
 sc.render.filepath=str(OUT/f'{label}.png');bpy.ops.render.render(write_still=True)
 for o in objects:bpy.data.objects.remove(o,do_unlink=True)
assert hashlib.sha256(source.read_bytes()).hexdigest()==digest
report={'source':str(source.relative_to(ROOT)),'sourceSHA256':digest,'sourceUnchanged':True,'sourceHeightMetres':height,'sourceHeadWidth':max(v.x for v in head)-min(v.x for v in head),'cameraPitchYaw':[45,45],'variants':{'A':'Source proportions, illustrative 2m high-contrast joints; NOT a reconstruction of screenshot tile size','B':'Same hero, 0.8m lower-contrast paving','C':'B ground; head/cap 90%, body height 108%, torso depth 112%; frozen design proposal only'},'runtimeChanges':False,'unityLightingMatch':False}
(OUT/'Comparison.json').write_text(json.dumps(report,indent=2),encoding='utf8');print('CHARACTER_FIT_COMPLETE',json.dumps(report))
