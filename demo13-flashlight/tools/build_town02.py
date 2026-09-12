"""Town exterior art. Deterministic editable Blender meshes, metric UVs, split roofs.
Input coordinates are Unity metres (x, height, depth); no reference image is modified.
"""
import bpy, math, json, random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Art/Environments/Town02'
for d in ['Models','BlenderSource~']: (OUT/d).mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0
bpy.context.scene.unit_settings.system='METRIC'
palette={
 'Brick':([.26,.13,.078],'Brick'), 'Plaster':([.38,.35,.27],'Plaster'),
 'Cream':([.46,.43,.34],'Plaster'),'Ochre':([.32,.255,.15],'Plaster'),
 'Concrete':([.26,.265,.23],'Plaster'),'Roof':([.16,.158,.13],'Roof'),
 'Olive':([.16,.185,.125],'PaintedMetal'),'DarkOlive':([.08,.10,.073],'PaintedMetal'),
 'Steel':([.07,.084,.079],'Steel'),'Edge':([.19,.205,.175],'Steel'),
 'Wood':([.25,.17,.092],'Wood'),'WoodDark':([.12,.081,.044],'Wood'),
 'Canvas':([.245,.25,.175],'Cloth'),'Red':([.30,.082,.052],'PaintedMetal'),
 'Glass':([.037,.062,.061],'PaintedMetal'),'Paper':([.55,.50,.37],'Plaster'),
 'Asphalt':([.14,.151,.143],'Asphalt'),'Dirt':([.20,.179,.132],'Asphalt'),
 'RoofWarm':([.185,.163,.12],'Roof'),'RoofClean':([.255,.26,.235],'Roof'),'RoofDark':([.088,.09,.078],'Roof'),
 'Graphite':([.12,.119,.10],'Brick'),'Tile':([.52,.50,.43],'Tile'),'Sage':([.20,.265,.235],'Tile')}
mats={};assets={};parts=[];rng=random.Random(911)
for n,(c,f) in palette.items():
 m=bpy.data.materials.new('Town_'+n);m.diffuse_color=(*c,1);m.use_nodes=True
 p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.84
 mats[n]=m
def box(n,p,s,m,b=.025):
 x,y,z=p;w,h,d=s
 bpy.ops.mesh.primitive_cube_add(size=1,location=(-x,-z,y));o=bpy.context.object;o.name=n;o.dimensions=(w,d,h)
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if b:
  mod=o.modifiers.new('Soft edge','BEVEL');mod.width=min(b,min(s)*.2);mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 o.data.materials.append(mats[m]);parts.append(o);return o
def cyl(n,p,r,h,m):
 x,y,z=p;bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=h,location=(-x,-z,y));o=bpy.context.object;o.name=n
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mats[m]);parts.append(o);return o
def beam(n,a,b,w,m):
 a=Vector((-a[0],-a[2],a[1]));b=Vector((-b[0],-b[2],b[1]))
 bpy.ops.mesh.primitive_cube_add(size=1,location=(a+b)/2);o=bpy.context.object;o.name=n;o.dimensions=(w,w,(b-a).length);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mats[m]);parts.append(o);return o
def export(n):
 global parts
 col=bpy.data.collections.new(n+'_Editable');bpy.context.scene.collection.children.link(col)
 for o in parts:
  for c in list(o.users_collection):c.objects.unlink(o)
  col.objects.link(o);me=o.data
  # Primitive meshes already have normalized UVMap. Replace it, otherwise FBX/Blender
  # samples that first channel and stretches one brick tile across an entire wall.
  for oldUV in list(me.uv_layers):me.uv_layers.remove(oldUV)
  uv=me.uv_layers.new(name='MetreUV');uv.active_render=True
  for f in me.polygons:
   major=max(range(3),key=lambda i:abs(f.normal[i]));axes=[i for i in range(3) if i!=major]
   for li in f.loop_indices:
    v=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(v[axes[0]],v[axes[1]])
 copies=[]
 for o in parts:
  cp=o.copy();cp.data=o.data.copy();bpy.context.scene.collection.objects.link(cp);copies.append(cp)
 bpy.ops.object.select_all(action='DESELECT')
 for o in copies:o.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();joined=bpy.context.object;joined.name=n
 for f in joined.data.polygons:f.use_smooth=True
 mod=joined.modifiers.new('Weighted normals','WEIGHTED_NORMAL');mod.keep_sharp=True;bpy.ops.object.modifier_apply(modifier=mod.name)
 bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/f'{n}.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
 assets[n]={'triangles':sum(len(f.vertices)-2 for f in joined.data.polygons),'sourcePieces':len(parts)}
 bpy.data.objects.remove(joined,do_unlink=True);col.hide_render=True;col.hide_viewport=True;parts=[]
def window(x,z,w=2.1):
 box('Recess',(x,2.1,z),(w,.98,.14),'WoodDark')
 box('Glass',(x,2.1,z-.09),(w-.18,.78,.06),'Glass',.01)
 for xx in [x-w/2,x+w/2]:box('Window jamb',(xx,2.1,z-.15),(.09,1.15,.16),'Olive')
 for yy in [1.54,2.66]:box('Window trim',(x,yy,z-.15),(w+.12,.09,.16),'Olive')
 box('Mullion',(x,2.1,z-.18),(.055,1,.09),'Edge',.005)
 box('Sill',(x,1.51,z-.26),(w+.32,.12,.46),'Concrete')
def awning(x,z,w,mat):
 for i in range(round(w/.28)):
  xx=x-w/2+(i+.5)*w/round(w/.28)
  box('Awning slat',(xx,2.94,z-.55),(w/round(w/.28)-.015,.12,1.12),mat,.018)
 for xx in [x-w/2+.10,x+w/2-.10]:box('Awning bracket',(xx,2.66,z-.25),(.065,.49,.44),'Steel')
def rooftop(n,w,d,h=4.5,metal=False):
 import sys
 sys.path.insert(0,str(Path(__file__).resolve().parent))
 from town02_roofs import build_roof
 build_roof(n,w,d,h,metal,box,cyl,beam,export,parts,mats)

specs=[('Pawnshop',16,12,'Brick',False),('Repair',14,10,'Ochre',False),('Medical',18,10,'Tile',False),('Furniture',10,14,'Wood',True),('BlackMarket',12,14,'Graphite',True)]
for name,w,d,wall,east in specs:
 z=-d/2
 for x in [-1,1]:box('Front wall',(x*(w+1.6)/4,2.25,z),((w-1.6)/2,4.5,.30),wall)
 box('Door lintel',(0,3.46,z),(1.6,2.08,.30),wall)
 box('Rear wall',(0,2.25,d/2),(w+.3,4.5,.30),wall)
 for x in [-w/2,w/2]:box('Side wall',(x,2.25,0),(.3,4.5,d+.3),wall)
 for x in [-1,1]:box('Plinth',(x*(w+1.6)/4,.30,z-.19),((w-1.6)/2,.60,.18),'Sage' if name=='Medical' else 'Concrete')
 for xx in [-w/2,w/2]:box('Corner pier',(xx,2.25,z-.15),(.44,4.5,.48),'Brick' if wall=='Brick' else 'Concrete')
 for xx in [-.9,.9]:box('Door jamb',(xx,1.25,z-.20),(.16,2.5,.24),'WoodDark' if wall=='Brick' else 'Steel')
 box('Door cap',(0,2.5,z-.20),(1.96,.17,.24),'WoodDark')
 for xx in [-w*.31,w*.31]:
  window(xx,z-.24,2.5 if name=='Medical' else 2.15)
  if name in ['Furniture','BlackMarket']:
   for yy,tilt in [(1.85,.16),(2.26,-.13)]:beam('Boarded window',(xx-1,yy,z-.48),(xx+1,yy+tilt,z-.48),.16,'WoodDark' if name=='BlackMarket' else 'Wood')
 if name not in ['BlackMarket','Pawnshop']:awning(0,z-.2,3.8,'Sage' if name=='Medical' else 'Olive')
 if name=='Pawnshop':box('Entry stone inset',(0,.049,z-.42),(1.56,.012,1.12),'Concrete',.004)
 sw=6 if name=='Pawnshop' else 1.4 if name=='BlackMarket' else 4.2
 box('Sign backing',(0,3.78,z-.28),(sw,1.05,.22),'WoodDark')
 box('Sign face',(0,3.78,z-.415),(sw-.22,.82,.08),'Paper' if name=='Medical' else 'Wood' if name in ['Pawnshop','Furniture'] else 'Graphite' if name=='BlackMarket' else 'Olive')
 for xx in [-sw/2+.15,sw/2-.15]:box('Sign straps',(xx,3.78,z-.47),(.06,1,.045),'Edge',.005)
 if name=='Medical':
  box('Medical cross vertical',(0,3.78,z-.48),(.17,.65,.05),'Sage');box('Medical cross horizontal',(0,3.78,z-.485),(.60,.17,.05),'Sage')
 elif name=='Pawnshop':
  for xx in [-.60,0,.60]:
   box('Trade emblem stem',(xx,3.8,z-.49),(.04,.50,.05),'Paper',.002)
   box('Trade emblem plate',(xx,3.60,z-.49),(.27,.21,.06),'Paper')
 elif name=='Repair':
  box('Repair emblem',(0,3.78,z-.49),(.75,.13,.055),'Paper');box('Repair emblem head',(-.35,3.78,z-.49),(.15,.46,.055),'Paper')
 elif name=='Furniture':
  box('Furniture emblem',(0,3.75,z-.49),(.65,.11,.055),'Paper')
  for xx in [-.27,.27]:box('Furniture emblem leg',(xx,3.60,z-.49),(.065,.25,.055),'Paper')
 else:
  for xx in [-.26,0,.26]:box('Market tally',(xx,3.78,z-.49),(.07,.48,.06),'Paper')
 # Vertical drainage, meter box and local repair marks; not blanket surface noise.
 cyl('Drain pipe',(w/2-.48,2.19,z-.38),.09,4.15,'Steel')
 box('Meter cabinet',(-w/2+.8,1.15,z-.25),(.46,.72,.25),'Edge')
 for i in range(3):box('Local plaster repair',(-w*.27+i*.31,.8+i*.13,z-.162),(.42,.16,.02),'Concrete',.005)
 if name=='Furniture':
  for xx in [-w/2+.4,-1.15,1.15,w/2-.4]:box('Timber shopfront post',(xx,2.10,z-.24),(.19,4.12,.20),'WoodDark')
  for yy in [.67,3.22]:
   for side in [-1,1]:box('Timber shopfront rail',(side*(w+1.6)/4,yy,z-.25),((w-1.6)/2,.13,.18),'WoodDark')
 if name=='Repair':
  # The wide workshop shutter is a closed side bay, not the functional central door.
  xx=-w*.31
  box('Workshop bay recess',(xx,1.36,z-.29),(3.35,2.62,.20),'Steel')
  for i in range(17):box('Workshop roller slat',(xx,.13+i*.15,z-.43),(3.15,.13,.06),'DarkOlive',.01)
  awning(xx,z-.18,3.55,'Olive')
 export(name+'_Shell');rooftop(name,w,d)
# Home container: built around the original footprint; west-facing entrance is separate gameplay object.
w,d,h=14,9,2.9
for z in [-d/2,d/2]:
 box('Container sheet',(0,h/2,z),(w,h,.14),'Olive')
 for i in range(45):box('Corrugation',(-w/2+(i+.5)*w/45,h/2,z),(.085,h-.18,.25),'Olive',.014)
for x in [-w/2,w/2]:box('Container end',(x,h/2,0),(.14,h,d),'DarkOlive')
for x in [-w/2,w/2]:
 for z in [-d/2,d/2]:box('Corner casting',(x,h/2,z),(.28,h,.28),'Edge')
for z in [-d/2,d/2]:
 for y in [.13,h-.11]:box('Container rail',(0,y,z),(w,.22,.27),'Steel')
for x in [-w/2-.12]:
 box('Home door',(x,1.19,0),(.14,2.32,1.7),'Olive')
 for zz in [-.91,.91]:box('Home door trim',(x-.10,1.23,zz),(.17,2.46,.12),'Edge')
 box('Home door lintel',(x-.10,2.46,0),(.17,.12,1.94),'Edge')
 box('Handle',(x-.22,1.15,.59),(.10,.27,.08),'Steel')
for xx in [-3.4,3.4]:window(xx,-d/2-.14,1.6)
export('Container_Home_Shell');rooftop('Container_Home',w,d,h,True)
# Courtyard objects from the 07_19_02 / 07_21_31 reference sheets.
cyl('Spool foot',(0,.09,0),.82,.16,'WoodDark');cyl('Spool core',(0,.43,0),.37,.60,'Wood');cyl('Spool tabletop',(0,.78,0),.85,.13,'Wood')
cyl('Axle hole',(0,.852,0),.115,.012,'Steel')
for x in [-.55,-.28,0,.28,.55]:
 length=2*math.sqrt(.82**2-x*x);box('Plank joint',(x,.849,0),(.009,.008,length),'WoodDark',.002)
for x,z in [(-.48,-.48),(-.48,.48),(.48,-.48),(.48,.48)]:cyl('Tabletop bolt',(x,.86,z),.035,.014,'Steel')
export('SpoolTable02')
for x in [-.24,.24]:
 for z in [-.24,.24]:beam('Chair leg',(x*1.18,0,z*1.18),(x,.49,z),.04,'Steel')
box('Seat',(0,.49,0),(.57,.07,.55),'Olive')
for x in [-.25,.25]:beam('Backrest frame',(x,.47,.24),(x,1.12,.32),.045,'Edge')
for yy in [.77,.91,1.05]:box('Backrest slat',(0,yy,.29),(.53,.105,.047),'Olive')
export('YardChair02')
# Raised planter and simple three-dimensional low foliage. No billboard texture shadows.
for x in [-1.12,1.12]:box('Planter end',(x,.37,0),(.11,.69,1.04),'WoodDark')
for z in [-.5,.5]:
 for yy in [.16,.36,.56]:box('Planter plank',(0,yy,z),(2.32,.18,.08),'Wood')
box('Soil',(0,.50,0),(2.16,.05,.9),'Dirt')
for x in [-.77,-.25,.27,.77]:
 for z in [-.22,.23]:
  cyl('Plant stalk',(x,.66,z),.02,.29,'DarkOlive')
  for i in range(5):
   a=i*math.tau/5;beam('Plant leaf',(x,.72,z),(x+math.cos(a)*.16,.61,z+math.sin(a)*.16),.065,'Olive')
export('Planter02')
# Covered two-axle merchant cart. The canvas is a curved mesh, separate from its wood frame.
box('Cart chassis',(0,.46,0),(2.55,.19,1.68),'WoodDark')
for i in range(8):box('Cart deck',(-1.12+i*.32,.59,0),(.30,.12,1.65),'Wood')
for x in [-.88,.88]:
 for z in [-.93,.93]:
  bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=.39,depth=.16,location=(-x,-z,.42),rotation=(math.pi/2,0,0));o=bpy.context.object;o.name='Wheel';bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mats['Steel']);parts.append(o)
for x in [-1.1,1.1]:
 for z in [-.70,.70]:box('Canopy upright',(x,1.20,z),(.075,1.45,.075),'WoodDark')
for x in [-1.1,1.1]:
 for z in [-.70,.70]:beam('Canopy brace',(x,1.9,z),(x,2.13,0),.055,'WoodDark')
vs=[]
for x in [-1.34,-.67,0,.67,1.34]:
 for i in range(9):
  z=-.89+i*.2225;y=1.94+.24*math.cos(z/.89*math.pi/2)+.014*math.sin(x*4+i)
  vs.append((-x,-z,y))
faces=[]
for j in range(4):
 for i in range(8):a=j*9+i;faces.append((a,a+9,a+10,a+1))
me=bpy.data.meshes.new('Canvas canopy');me.from_pydata(vs,[],faces);me.update();o=bpy.data.objects.new('Canvas canopy',me);bpy.context.scene.collection.objects.link(o);o.data.materials.append(mats['Canvas']);parts.append(o)
mod=o.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.025;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
for x in [-.78,0,.78]:box('Supplies crate',(x,.91,0),(.62,.52,.75),'Wood');box('Crate strap',(x,1.18,0),(.055,.02,.76),'Steel')
for z in [-.54,.54]:beam('Tow handle',(1.22,.5,z),(2.05,.82,z),.085,'WoodDark')
export('MerchantCart02')
# Standalone perimeter modules are ready for the next placement pass; original gameplay wall colliders remain authoritative.
for row in range(8):
 for i in range(4):box('Concrete masonry',(-.75+i*.5,(row+.5)*.5,0),(.487,.485,.48),'Concrete',.028)
box('Wall coping',(0,4.05,0),(2.08,.15,.63),'Cream')
export('PerimeterWall02')
box('Perimeter pillar',(0,2.03,0),(.82,4.06,.82),'Concrete',.04)
box('Pillar cap',(0,4.16,0),(1,.25,1),'Cream',.045)
box('Cap recess',(0,4.291,0),(.32,.014,.32),'Steel',.002)
export('PerimeterPillar02')
(OUT/'KitManifest.json').write_text(json.dumps({'assets':assets,'palette':{'Town_'+n:{'color':c,'family':f} for n,(c,f) in palette.items()},'buildings':[{'name':n,'east':e} for n,w,d,m,e in specs]},indent=2))
# Save linked PBR surface nodes into the editable source too. Original GPT images are references only.
for name,(color,family) in palette.items():
 mat=mats[name];nodes=mat.node_tree.nodes;links=mat.node_tree.links;bsdf=nodes.get('Principled BSDF')
 textureRoot=OUT/'Textures' if family in ['Brick','Plaster','Asphalt','Tile','Roof'] else ROOT/'Assets/Art/Environments/Hideout02/Textures'
 maps={}
 for kind in ['Base','Normal','Mask']:
  t=nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(textureRoot/f'{family}_{kind}.png'),check_existing=True);t.image.colorspace_settings.name='sRGB' if kind=='Base' else 'Non-Color';t.image.pack();maps[kind]=t
 tint=nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1;tint.inputs[2].default_value=(*color,1);links.new(maps['Base'].outputs['Color'],tint.inputs[1]);links.new(tint.outputs[0],bsdf.inputs['Base Color'])
 nm=nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.65 if family=='Brick' else .28;links.new(maps['Normal'].outputs['Color'],nm.inputs['Color']);links.new(nm.outputs[0],bsdf.inputs['Normal'])
 invert=nodes.new('ShaderNodeMath');invert.operation='SUBTRACT';invert.inputs[0].default_value=1;links.new(maps['Mask'].outputs['Alpha'],invert.inputs[1]);links.new(invert.outputs[0],bsdf.inputs['Roughness'])
 separate=nodes.new('ShaderNodeSeparateColor');links.new(maps['Mask'].outputs['Color'],separate.inputs[0]);links.new(separate.outputs[0],bsdf.inputs['Metallic'])
assembly=bpy.data.collections.new('Town02_Editable_Layout');bpy.context.scene.collection.children.link(assembly)
placements=[('Pawnshop',(34,0,44),0),('Repair',(12,0,44),0),('Medical',(64,0,44),0),('Furniture',(12,0,27),90),('BlackMarket',(12,0,10),90),('Container_Home',(64,0,12),0)]
placements += [('SpoolTable02',(44,0,29),0),('YardChair02',(42.6,0,29),90),('YardChair02',(45.4,0,29),-90),('MerchantCart02',(35,0,17.5),15),('Planter02',(14,0,18),0)]
for name,pos,rz in placements:
 names=[name+'_'+part for part in ['Shell','Roof']] if name in [p[0] for p in placements[:6]] else [name]
 for key in names:
  for o in bpy.data.collections[key+'_Editable'].objects:
   cp=o.copy();assembly.objects.link(cp);cp.location=(-pos[0]+40,-pos[2]+28,pos[1]);cp.rotation_euler.z=math.radians(rz)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/Town02.blend'))
print('TOWN02_COMPLETE',json.dumps(assets))
