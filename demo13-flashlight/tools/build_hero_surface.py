"""Bake editable stylized surface layers; preserve mesh topology, rig and actions."""
import bpy,math,json,hashlib,numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor'
OUT=BASE/'SurfaceReview';TEX=OUT/'Textures';CACHE=ROOT/'Library/CodexBlender/HeroSurface'
for p in (OUT,TEX,CACHE,OUT/'BlenderSource~'):p.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'BlenderSource~/DarkSurvivor.blend'))
bpy.context.preferences.filepaths.save_version=0
sc=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig']
parts=[o for o in sc.objects if o.type=='MESH' and o.name.startswith('Hero_')]
def invariants():
 return {'geometry':hashlib.sha256(repr([(o.name,[tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons],[(v.index,[(g.group,g.weight) for g in v.groups]) for v in o.data.vertices]) for o in parts]).encode()).hexdigest(),'rig':hashlib.sha256(repr([(b.name,tuple(b.head_local),tuple(b.tail_local),b.parent.name if b.parent else None) for b in rig.data.bones]).encode()).hexdigest(),'actions':hashlib.sha256(repr([(a.name,[(f.data_path,f.array_index,[tuple(k.co) for k in f.keyframe_points]) for f in a.fcurves]) for a in bpy.data.actions]).encode()).hexdigest()}
before=invariants();rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
for o in parts:
 if o.name=='Hero_SwordProxy':o.hide_render=True
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=32
sc.render.resolution_x=900;sc.render.resolution_y=1000;sc.render.resolution_percentage=100
sc.camera.location=(-3.4,-6,2.5);sc.camera.rotation_euler=(Vector((0,0,.94))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=2.25
sc.render.image_settings.file_format='PNG';sc.render.filepath=str(OUT/'Before.png');bpy.ops.render.render(write_still=True)
rig.data.pose_position='REST';bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.012,area_weight=.25,correct_aspect=True,scale_to_bounds=True)
bpy.ops.object.mode_set(mode='OBJECT')
for o in parts:o.data.uv_layers.active.name='SurfaceUV'
materials=list({m.name:m for o in parts for m in o.data.materials}.values())
cloth={'jacket','lapel','cap','brim','cuff','trousers','trouser_cuff','pack','flap','scarf','under','patch'}
leather={'boots','leather','sole'}
outputs={};sources={};profiles=[]
for m in materials:
 p=m.node_tree.nodes.get('Principled BSDF');nodes=m.node_tree.nodes;links=m.node_tree.links;out=next(n for n in nodes if n.type=='OUTPUT_MATERIAL')
 base=list(p.inputs['Base Color'].default_value);rough=p.inputs['Roughness'].default_value;metal=p.inputs['Metallic'].default_value
 key=m.name.removeprefix('Hero_');active=key in cloth|leather
 def node(kind):return nodes.new(kind)
 geo=node('ShaderNodeNewGeometry');noise=node('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=9 if key in cloth else 15;noise.inputs['Detail'].default_value=1.2;noise.inputs['Roughness'].default_value=.5;links.new(geo.outputs['Position'],noise.inputs['Vector'])
 ramp=node('ShaderNodeValToRGB');ramp.color_ramp.elements.remove(ramp.color_ramp.elements[1]);ramp.color_ramp.interpolation='EASE'
 stops=[(.27,.76),(.48,.95),(.64,1.12),(.78,1.20)] if active else [(0,1),(1,1)]
 for i,(pos,factor) in enumerate(stops):
  e=ramp.color_ramp.elements[0] if i==0 else ramp.color_ramp.elements.new(pos);e.position=pos;e.color=tuple(min(1,c*factor) for c in base[:3])+(1,)
 links.new(noise.outputs['Fac'],ramp.inputs[0]);color=ramp.outputs['Color']
 if active:
  # Convex borders pick up a restrained warm abrasion tint; broad islands keep
  # the surface readable at the actual quarter-view camera distance.
  wear=node('ShaderNodeMapRange');links.new(geo.outputs['Pointiness'],wear.inputs['Value']);wear.inputs['From Min'].default_value=.50;wear.inputs['From Max'].default_value=.56;wear.inputs['To Max'].default_value=.28
  mix=node('ShaderNodeMixRGB');links.new(wear.outputs['Result'],mix.inputs[0]);links.new(color,mix.inputs[1]);mix.inputs[2].default_value=tuple(min(1,c*1.4+.014) for c in base[:3])+(1,);color=mix.outputs[0]
  if key in {'boots','sole','trousers','trouser_cuff'}:
   xyz=node('ShaderNodeSeparateXYZ');links.new(geo.outputs['Position'],xyz.inputs[0]);dust=node('ShaderNodeMapRange');links.new(xyz.outputs['Z'],dust.inputs['Value']);dust.inputs['From Min'].default_value=.12;dust.inputs['From Max'].default_value=.65;dust.inputs['To Min'].default_value=.48;dust.inputs['To Max'].default_value=0
   variation=node('ShaderNodeMath');variation.operation='MULTIPLY';links.new(dust.outputs[0],variation.inputs[0]);links.new(noise.outputs['Fac'],variation.inputs[1])
   mix=node('ShaderNodeMixRGB');links.new(variation.outputs[0],mix.inputs[0]);links.new(color,mix.inputs[1]);mix.inputs[2].default_value=(.18,.13,.08,1);color=mix.outputs[0]
 links.new(color,p.inputs['Base Color'])
 roughnode=node('ShaderNodeMapRange');links.new(noise.outputs['Fac'],roughnode.inputs['Value']);roughnode.inputs['To Min'].default_value=max(.15,rough-.09) if active else rough;roughnode.inputs['To Max'].default_value=min(.98,rough+.06) if active else rough;links.new(roughnode.outputs['Result'],p.inputs['Roughness'])
 if active and key!='sole':
  mapping=node('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=(1,1,3.2 if key in cloth else 1)
  folds=node('ShaderNodeTexNoise');folds.inputs['Scale'].default_value=13 if key in cloth else 20;folds.inputs['Detail'].default_value=0;links.new(geo.outputs['Position'],mapping.inputs[0]);links.new(mapping.outputs[0],folds.inputs['Vector'])
  bump=node('ShaderNodeBump');bump.inputs['Strength'].default_value=.16 if key in cloth else .10;bump.inputs['Distance'].default_value=.004 if key in cloth else .002;links.new(folds.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],p.inputs['Normal'])
 ao=node('ShaderNodeAmbientOcclusion');ao.inputs['Distance'].default_value=.065;ao.samples=16
 smooth=node('ShaderNodeMath');smooth.operation='SUBTRACT';smooth.inputs[0].default_value=1;links.new(roughnode.outputs['Result'],smooth.inputs[1])
 pack=node('ShaderNodeCombineXYZ');pack.inputs[0].default_value=metal;links.new(ao.outputs['AO'],pack.inputs[1]);links.new(smooth.outputs[0],pack.inputs[2])
 outputs[m.name]=out;sources[m.name]=(p,color,pack.outputs[0]);profiles.append({'material':m.name,'surface':'cloth' if key in cloth else 'leather' if key in leather else 'plain','base_color':base,'roughness':rough,'metallic':metal})
# Join disposable copies for one shared, non-overlapping atlas bake.
bpy.ops.object.select_all(action='DESELECT');copies=[]
for o in parts:
 c=o.copy();c.data=o.data.copy();sc.collection.objects.link(c);c.parent=None;c.matrix_world=o.matrix_world.copy()
 for mod in list(c.modifiers):c.modifiers.remove(mod)
 c.hide_render=False;c.select_set(True);copies.append(c);o.hide_render=True
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();bake=bpy.context.object;bake.name='SurfaceBakeTemporary'
sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.device='CPU';sc.render.bake.margin=12;sc.render.bake.use_selected_to_active=False
images={}
for label,kind in [('BaseColor','EMIT'),('Normal','NORMAL'),('Mask','EMIT')]:
 image=bpy.data.images.new('Hero_'+label,width=2048,height=2048,alpha=True);image.colorspace_settings.name='sRGB' if label=='BaseColor' else 'Non-Color'
 for m in materials:
  nodes=m.node_tree.nodes;links=m.node_tree.links;p,color,packed=sources[m.name];out=outputs[m.name]
  if label=='Normal':links.new(p.outputs['BSDF'],out.inputs['Surface'])
  else:
   emission=nodes.new('ShaderNodeEmission');links.new(color if label=='BaseColor' else packed,emission.inputs['Color']);links.new(emission.outputs[0],out.inputs['Surface'])
  target=nodes.new('ShaderNodeTexImage');target.image=image;nodes.active=target
 bpy.ops.object.bake(type=kind)
 if label=='Mask':
  values=np.empty(2048*2048*4,dtype=np.float32);image.pixels.foreach_get(values);v=values.reshape(-1,4);v[:,3]=v[:,2];v[:,2]=0;image.pixels.foreach_set(values)
 image.filepath_raw=str(TEX/('Hero_'+label+'.png'));image.file_format='PNG';image.save();images[label]=image
 print('SURFACE_BAKED',label,flush=True)
bpy.data.objects.remove(bake,do_unlink=True)
for o in parts:o.hide_render=o.name=='Hero_SwordProxy'
for m in materials:
 nodes=m.node_tree.nodes;links=m.node_tree.links;p,_,_=sources[m.name];links.new(p.outputs['BSDF'],outputs[m.name].inputs['Surface'])
 for socket in ['Base Color','Roughness','Normal']:
  for link in list(p.inputs[socket].links):links.remove(link)
 texnodes={}
 for label in images:
  n=nodes.new('ShaderNodeTexImage');n.name='Baked_'+label;n.image=images[label];texnodes[label]=n
 links.new(texnodes['BaseColor'].outputs['Color'],p.inputs['Base Color'])
 norm=nodes.new('ShaderNodeNormalMap');links.new(texnodes['Normal'].outputs['Color'],norm.inputs['Color']);links.new(norm.outputs[0],p.inputs['Normal'])
 inv=nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;links.new(texnodes['Mask'].outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],p.inputs['Roughness'])
rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
assert invariants()==before,'Surface authoring changed model or animation'
sc.render.engine='BLENDER_EEVEE_NEXT';sc.render.filepath=str(OUT/'After.png');bpy.ops.render.render(write_still=True)
for label,loc in [('Back',(3.4,6,2.6)),('Close',(-2,-5,2.7))]:
 sc.camera.location=loc;target=Vector((0,0,.95 if label=='Back' else 1.12));sc.camera.rotation_euler=(target-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=2.25 if label=='Back' else 1.35;sc.render.filepath=str(OUT/(label+'.png'));bpy.ops.render.render(write_still=True)
sc.camera.data.ortho_scale=2.25
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/DarkSurvivor_Textured.blend'))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in parts:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'DarkSurvivor_Textured.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
(OUT/'SurfaceCheck.json').write_text(json.dumps({'preserved':before,'parts':len(parts),'uv_mapped':all(len(o.data.uv_layers)>0 for o in parts),'textures':{k:Path(v.filepath_raw).name for k,v in images.items()},'mask_channels':{'R':'metallic','G':'occlusion','B':'unused','A':'smoothness'},'profiles':profiles},indent=2))
print('SURFACE_READY',flush=True)
