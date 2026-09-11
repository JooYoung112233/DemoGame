def apply_hero_surface_to_scene(root):
 """Copy surfaces, migrating legacy expression geometry to the textured face."""
 source=root/'Assets/ChibiSurvivor/Player/DarkSurvivor/SurfaceReview/BlenderSource~/DarkSurvivor_Textured.blend'
 if not source.exists():return 0
 with bpy.data.libraries.load(str(source),link=False) as (src,dst):
  textured_face='Hero_Nose' in src.objects
 if textured_face:
  legacy=bpy.data.objects.get('Hero_FaceDetails')
  if legacy:legacy.name='Hero_Nose'
 targets={o.name:o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('Hero_')}
 previous_objects=set(bpy.data.objects);previous_actions=set(bpy.data.actions)
 with bpy.data.libraries.load(str(source),link=False) as (src,dst):
  names=[n for n in src.objects if n in targets];dst.objects=list(names)
 count=0
 for name,ref in zip(names,dst.objects):
  target=targets[name]
  if name=='Hero_Nose':
   target.data=ref.data.copy()
   target.vertex_groups.clear()
   for group in ref.vertex_groups:target.vertex_groups.new(name=group.name)
   target['expression_in_texture_v1']=True
   target['natural_proportions_v1']=True
  if len(target.data.loops)!=len(ref.data.loops) or len(target.data.vertices)!=len(ref.data.vertices):raise RuntimeError('Surface topology mismatch: '+name+str((len(target.data.vertices),len(target.data.loops),len(ref.data.vertices),len(ref.data.loops),[m.name for m in target.data.materials])))
  uv=target.data.uv_layers.active or target.data.uv_layers.new(name='SurfaceUV');uv.name='SurfaceUV'
  for a,b in zip(uv.data,ref.data.uv_layers.active.data):a.uv=b.uv
  target.data.materials.clear()
  for m in ref.data.materials:
   if not m.name.startswith('Surface_'):m.name='Surface_'+m.name.split('.')[0]
   target.data.materials.append(m)
  for a,b in zip(target.data.polygons,ref.data.polygons):a.material_index=b.material_index
  count+=1
 for o in set(bpy.data.objects)-previous_objects:bpy.data.objects.remove(o,do_unlink=True)
 for a in set(bpy.data.actions)-previous_actions:bpy.data.actions.remove(a)
 return count
