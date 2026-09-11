"""Grouped, non-overlapping UV atlas for the simple hero, preserving its silhouette."""
def author_simple_hero_uv(root, out, parts):
 import bpy, numpy as np, sys, json, hashlib
 sys.path.insert(0,str(root/'tools'))
 from character_uv_atlas import author_atlas
 # These surfaces do not define the silhouette and are painted onto head/cap.
 removable={'Study_CapPatch','Study_Hair','Study_SideHair_-1','Study_SideHair_1'}
 keep=[o for o in parts if o.name not in removable]
 def invariant():
  return hashlib.sha256(repr([(o.name,[tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons],list(o.matrix_world)) for o in keep]).encode()).hexdigest()
 before=invariant()
 for o in parts:
  if o.name in removable:bpy.data.objects.remove(o,do_unlink=True)
 colors={'Study_Skin':(.55,.335,.18),'Study_Face':(.55,.335,.18),'Study_Cloth':(.19,.205,.105),'Study_Trousers':(.065,.073,.073),'Study_Boots':(.105,.073,.045),'Study_Cap':(.215,.225,.115),'Study_Brim':(.068,.075,.055)}
 regions={'HEAD':(.02,.54,.48,.44),'CAP':(.52,.62,.46,.36),'JACKET':(.02,.26,.48,.26),'TROUSERS':(.52,.29,.46,.31),'BOOTS':(.52,.02,.46,.25),'HANDS_NECK':(.02,.02,.48,.22)}
 def bucket(n):
  if n=='Study_Head':return 'HEAD'
  if n.startswith('Study_Cap'):return 'CAP'
  if n=='Study_Torso' or n.startswith('Study_Sleeve'):return 'JACKET'
  if n=='Study_Hips' or n.startswith('Study_Leg'):return 'TROUSERS'
  if n.startswith('Study_Boot'):return 'BOOTS'
  return 'HANDS_NECK'
 def paint(name,material,p):
  x,y,z=p.T;count=len(p);color=np.tile(colors[material.name.split('.')[0]],(count,1));height=np.zeros(count);rough=np.full(count,.9)
  def fill(mask,c):color[mask]=c
  def triangle(points):
   a,b,c=np.array(points);v0=b-a;v1=c-a;d=v0[0]*v1[1]-v0[1]*v1[0];u=((x-a[0])*v1[1]-(z-a[1])*v1[0])/d;v=(v0[0]*(z-a[1])-(v0[1])*(x-a[0]))/d
   return (u>=0)&(v>=0)&(u+v<=1)
  if name=='Study_Head':
   front=y<-.16
   for cx in [-.103,.103]:
    eye=((x-cx)/.016)**2+(np.maximum(abs(z-1.337)-.024,0)/.016)**2<=1
    brow=(abs(x-cx)<.046)&(abs(z-1.425)<.011)
    fill(front&(eye|brow),(.023,.019,.015))
   # Hair cap at the rear, with short tapered temples. No geometry/decal layers.
   hair=(y>-.07)&(z>1.305+.015*np.cos(x*12))
   temples=(abs(x)>.265)&(y>-.17)&(z>1.345+.19*np.maximum(0,.30-abs(x)))
   fill(hair|temples,(.055,.043,.032));rough[hair|temples]=.95
  elif name=='Study_Cap':
   zz=z-.028*x
   patch=(abs(x)<.085)&(zz>1.603)&(zz<1.695)&(y<-.17)
   fill(patch,(.39,.31,.19))
   border=patch&((abs(x)>.081)|(zz<1.607)|(zz>1.691));fill(border,(.285,.226,.14))
  elif name=='Study_Torso':
   front=y<-.075
   seam=front&(abs(x)<.0035)&(z<1.025);fill(seam,(.105,.12,.065))
   # Broad collar triangles, drawn directly into the shirt UV island.
   for s in [-1,1]:
    collar=front&triangle([(s*.045,1.091),(s*.145,1.048),(s*.07,.998)])
    fill(collar,(.235,.249,.139))
   opening=front&triangle([(-.043,1.091),(.043,1.091),(0,1.025)]);fill(opening,(.105,.118,.064))
   hem=(z>.628)&(z<.635);fill(hem,(.158,.173,.09))
  elif name.startswith('Study_Sleeve'):
   fill((z>.647)&(z<.654),(.145,.16,.08))
  elif name=='Study_Hips':
   fill((y<-.09)&(abs(x)<.003)&(z>.56),(.044,.049,.049))
  elif name.startswith('Study_Leg'):
   fill((z>.21)&(z<.216),(.053,.06,.06))
  elif name.startswith('Study_Boot'):
   rough[:]=.78;fill(z<.057,(.034,.03,.024));rough[z<.057]=.95
  elif name.startswith('Study_Mitten') or name=='Study_Neck':rough[:]=.88
  return color,height,0,rough
 tex=out/'Textures';images,report=author_atlas(keep,regions,bucket,paint,tex,'SimpleHero',2048)
 # A real grayscale roughness texture: white=rough, black=smooth.
 mask=images['Mask'];a=np.empty(2048*2048*4,np.float32);mask.pixels.foreach_get(a);a=a.reshape((-1,4));a[:,:3]=1-a[:,3:4];a[:,3]=1
 rough=bpy.data.images.new('SimpleHero_Roughness',width=2048,height=2048,alpha=True);rough.colorspace_settings.name='Non-Color';rough.pixels.foreach_set(a.reshape(-1));rough.filepath_raw=str(tex/'SimpleHero_Roughness.png');rough.file_format='PNG';rough.save()
 mat=bpy.data.materials.new('SimpleHero_Surface');mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear();bs=nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Specular IOR Level'].default_value=0;bs.inputs['Metallic'].default_value=0
 outnode=nodes.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(bs.outputs[0],outnode.inputs['Surface'])
 for im,label,socket in [(images['BaseColor'],'Editable_BaseColor','Base Color'),(rough,'Material_Roughness','Roughness')]:
  node=nodes.new('ShaderNodeTexImage');node.name=label;node.image=im;mat.node_tree.links.new(node.outputs['Color'],bs.inputs[socket])
 for o in keep:
  o.data.materials.clear();o.data.materials.append(mat)
  for p in o.data.polygons:p.material_index=0
  o['uv_surface_v1']=True
 assert invariant()==before,'Remaining silhouette geometry changed during UV authoring'
 # The helper produces flat normal/mask intermediates. They are not attached as detail maps.
 report.update({'removed_detail_meshes':sorted(removable),'remaining_geometry_preserved':True,'materials':1,'normal_map_used':False,'painted_details':['eyes','eyebrows','hair','cap patch','collar','shirt seam','cuff lines','boot sole color']})
 (out/'UVSurfaceCheck.json').write_text(json.dumps(report,indent=2))
 return keep
