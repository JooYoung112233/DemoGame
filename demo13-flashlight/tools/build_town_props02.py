"""Standalone courtyard props from GPT references. Offline Blender only.
Reuses the tested metric geometry/export helpers and approved low-contrast PBR maps.
"""
from pathlib import Path
HELPERS=Path(__file__).with_name('build_town02.py')
head=HELPERS.read_text(encoding='utf8').split('def rooftop(')[0]
head=head.replace("OUT=ROOT/'Assets/Art/Environments/Town02'","OUT=ROOT/'Assets/Art/Environments/TownProps02'").replace("'Town_'","'TownProps_'")
exec(compile(head,str(HELPERS),'exec'),globals())
for n,c,f in [('Blue',(.12,.185,.19),'PaintedMetal'),('Linen',(.46,.42,.32),'Cloth'),('BlueCloth',(.12,.18,.195),'Cloth'),('Rubber',(.024,.029,.027),'PaintedMetal'),('Rust',(.25,.108,.055),'PaintedMetal'),('Cardboard',(.31,.215,.119),'Wood')]:
 palette[n]=(c,f);m=bpy.data.materials.new('TownProps_'+n);m.diffuse_color=(*c,1);m.use_nodes=True;mats[n]=m
inventory={}
REF='Assets/GPT/안전구역/ChatGPT Image 2026년 6월 11일 오후 '
def finish(name,category,reference,colliders=None):
 vs=[v.co for o in parts for v in o.data.vertices]
 lo=[min(v[i] for v in vs) for i in range(3)];hi=[max(v[i] for v in vs) for i in range(3)]
 export(name)
 inventory[name]={'category':category,'reference':reference,'size':[hi[0]-lo[0],hi[2]-lo[2],hi[1]-lo[1]],'colliders':colliders or [],'interaction':'visual only; no gameplay interaction added'}
def collider(p,s):return {'center':p,'size':s}
def mesh(name,points,faces,material):
 me=bpy.data.meshes.new(name);me.from_pydata([(-x,-z,y) for x,y,z in points],[],faces);me.update()
 import bmesh
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(name,me);bpy.context.scene.collection.objects.link(o);o.data.materials.append(mats[material]);parts.append(o);return o
def tube(name,a,b,r,mat='Steel'):
 a=Vector((-a[0],-a[2],a[1]));b=Vector((-b[0],-b[2],b[1]))
 bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=(b-a).length,location=(a+b)/2);o=bpy.context.object;o.name=name;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mats[mat]);parts.append(o);return o
def line(name,points,r=.012,mat='Canvas'):
 for a,b in zip(points,points[1:]):tube(name,a,b,r,mat)
def ring(name,p,r,thickness,mat,axis='y'):
 x,y,z=p;bpy.ops.mesh.primitive_torus_add(major_segments=32,minor_segments=6,location=(-x,-z,y),major_radius=r,minor_radius=thickness)
 o=bpy.context.object;o.name=name
 if axis=='z':o.rotation_euler.x=math.pi/2
 if axis=='x':o.rotation_euler.y=math.pi/2
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mats[mat]);parts.append(o);return o
def cloth(name,cx,cy,cz,w,ht,mat,shape='towel'):
 # Curved double-sided cloth with distinct sag and folded edges, not a flat billboard.
 vs=[];nx,ny=10,9
 for j in range(ny):
  t=j/(ny-1)
  for i in range(nx):
   u=i/(nx-1);width=w*(1 if shape=='towel' else .78+.28*max(0,1-t*4))
   xx=cx+(u-.5)*width;yy=cy-t*ht-.025*math.sin(u*math.pi);zz=cz+math.sin(u*math.pi*5+.4)*(.012+.035*t)+.035*t*t
   # Split trouser legs with a narrow raised central hem.
   if shape=='trousers' and t>.72 and abs(u-.5)<.12:yy+=ht*.23*(t-.72)/.28
   vs.append((xx,yy,zz))
 fs=[]
 for j in range(ny-1):
  for i in range(nx-1):a=j*nx+i;fs.append((a,a+1,a+1+nx,a+nx))
 o=mesh(name,vs,fs,mat);mod=o.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.008;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
def hollow(name,cx,cz,rings,mat,verts=16):
 vs=[]
 for y,rx,rz in rings:
  for i in range(verts):a=i*math.tau/verts;vs.append((cx+math.cos(a)*rx,y,cz+math.sin(a)*rz))
 fs=[]
 for j in range(len(rings)-1):
  for i in range(verts):fs.append((j*verts+i,j*verts+(i+1)%verts,(j+1)*verts+(i+1)%verts,(j+1)*verts+i))
 fs.append(tuple(range((len(rings)-1)*verts,len(rings)*verts)))
 return mesh(name,vs,fs,mat)

# Domestic yard: drying, clean-up, water and storage.
for x in [-2.05,2.05]:
 cyl('Clothes post',(x,1.05,0),.065,2.1,'WoodDark');ring('Post binding',(x,1.98,0),.071,.012,'Canvas')
points=[(-2.05+4.1*i/28,2.02-.23*math.sin(i/28*math.pi),0) for i in range(29)];line('Sagging clothes rope',points,.012,'Canvas')
for x,w,ht,mat,shape in [(-1.48,.63,.71,'Linen','towel'),(-.66,.63,.86,'BlueCloth','trousers'),(.15,.66,.68,'Linen','shirt'),(1,.73,.88,'Canvas','towel')]:
 yy=2.02-.23*math.sin((x+2.05)/4.1*math.pi);cloth('Hanging '+shape,x,yy,-.006,w,ht,mat,shape)
 for dx in [-w*.35,w*.35]:box('Wooden peg',(x+dx,yy+.027,0),(.025,.10,.035),'Wood',.003)
finish('Clothesline02','Living',REF+'07_21_31.png',[collider([-2.05,1.05,0],[.15,2.1,.15]),collider([2.05,1.05,0],[.15,2.1,.15])])

for x in [-.66,.66]:
 for z in [-.28,.28]:box('Sink leg',(x,.43,z),(.055,.86,.055),'Steel')
for z in [-.29,.29]:box('Sink frame',(0,.70,z),(1.43,.06,.055),'Steel')
box('Sink back splash',(0,1.02,.32),(1.5,.23,.055),'Edge')
# Rectangular basin rims with rounded corners are built as concentric rings.
for x in [-.38,.38]:
 coords=[(-1,-.65),(-.85,-1),(.85,-1),(1,-.65),(1,.65),(.85,1),(-.85,1),(-1,.65)]
 levels=[(.90,.355,.305),(.87,.30,.25),(.68,.22,.18)];vs=[(x+xx*sx,yy,zz*sz) for yy,sx,sz in levels for xx,zz in coords]
 fs=[]
 for j in range(2):
  for i in range(8):fs.append((j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i))
 fs.append(tuple(range(16,24)));mesh('Inset wash basin',vs,fs,'Edge');cyl('Drain',(x,.686,0),.041,.012,'Steel')
line('Tap spout',[(0,.92,.28),(0,1.20,.28),(0,1.28,.20),(0,1.28,.08),(0,1.19,.03)],.025,'Edge')
for x in [-.11,.11]:cyl('Tap valve',(x,.95,.26),.04,.075,'Steel')
line('Waste pipe',[(-.38,.68,0),(-.38,.40,0),(-.30,.33,0),(-.17,.33,0),(-.11,.40,0),(-.11,.56,.20)],.035,'Steel')
finish('UtilitySink02','Living',REF+'07_21_31.png',[collider([0,.45,0],[1.5,.9,.67])])

for x in [-.34,.34]:
 for z in [-.34,.34]:box('Water stand leg',(x,.29,z),(.075,.58,.075),'WoodDark')
box('Water stand platform',(0,.60,0),(.86,.10,.86),'Wood');cyl('Water drum',(0,1.03,0),.38,.76,'Blue')
for y in [.69,1.05,1.39]:ring('Drum hoop',(0,y,0),.385,.018,'Edge')
cyl('Water lid',(0,1.43,0),.39,.045,'Blue');cyl('Filling cap',(.12,1.46,.1),.065,.035,'Steel')
line('Water tap',[(0,.79,-.36),(0,.79,-.48),(0,.70,-.48)],.023,'Edge');box('Tap handle',(0,.85,-.42),(.12,.025,.035),'Red',.003)
finish('WaterBarrelStand02','Living',REF+'07_24_05.png',[collider([0,.74,0],[.87,1.48,.97])])

hollow('Open metal bucket',0,0,[(.015,.12,.12),(.30,.17,.17),(.31,.154,.154),(.045,.105,.105)],'Edge')
ring('Bucket lip',(0,.305,0),.162,.011,'Steel')
line('Bucket handle',[(math.cos(a)*.163,.28+math.sin(a)*.20,0) for a in [i*math.pi/16 for i in range(17)]],.008,'Steel')
finish('Bucket02','Living',REF+'07_24_05.png')

box('Basket base',(0,.035,0),(.58,.05,.43),'Canvas')
for z in [-.22,.22]:
 for i in range(10):box('Basket lattice',(-.275+i*.061,.18,z),(.022,.29,.025),'Canvas',.005)
 for y in [.095,.22,.325]:box('Basket rail',(0,y,z),(.61,.025,.025),'Canvas',.005)
for x in [-.30,.30]:
 for i in range(7):box('Basket lattice',(x,.18,-.20+i*.067),(.025,.29,.025),'Canvas',.005)
 box('Basket rim',(x,.325,0),(.035,.035,.46),'Canvas',.005)
for i in range(3):
 box('Folded washing',(-.13+i*.125,.09+i*.049,.025*math.sin(i)),(.24,.07,.31),['Linen','BlueCloth','Canvas'][i],.023)
finish('LaundryBasket02','Living',REF+'07_24_05.png')

for i in range(4):box('Folded canvas',(0,.025+i*.033,0),(.66-i*.008,.028,.46),'Canvas',.014)
for x in [-.26,.26]:ring('Tarp eyelet',(x,.139,-.17),.020,.007,'Edge')
finish('FoldedTarp02','Supplies',REF+'07_24_05.png')

points=[]
for i in range(129):
 t=i/128;angle=t*math.tau*4;rad=.13+t*.15;points.append(Vector((math.cos(angle)*rad,.027+math.sin(angle*2)*.003,math.sin(angle)*rad)))
# Continuous six-sided rope: no hidden cylinder caps at every short segment.
vs=[];fs=[]
for i,p in enumerate(points):
 tangent=(points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized();side=tangent.cross(Vector((0,1,0))).normalized();up=side.cross(tangent).normalized()
 for j in range(6):
  a=j*math.tau/6;vs.append(tuple(p+.018*(math.cos(a)*side+math.sin(a)*up)))
for i in range(len(points)-1):
 for j in range(6):fs.append((i*6+j,i*6+(j+1)%6,(i+1)*6+(j+1)%6,(i+1)*6+j))
fs.extend([tuple(reversed(range(6))),tuple(range((len(points)-1)*6,len(points)*6))])
mesh('Coiled rope',vs,fs,'Canvas');line('Loose rope end',[(.28,.027,0),(.36,.028,.09),(.43,.027,.14)],.018,'Canvas')
finish('RopeCoil02','Supplies',REF+'07_24_05.png')

# Workshop and service equipment.
for x in [-.25,.25]:box('Ladder stile',(x,1.17,0),(.065,2.34,.075),'WoodDark')
for i in range(8):box('Ladder rung',(0,.20+i*.28,-.005),(.51,.055,.08),'Wood')
finish('Ladder02','Workshop',REF+'07_24_05.png',[collider([0,1.17,0],[.58,2.34,.09])])

box('Meter box',(0,.44,0),(.56,.86,.22),'Edge');box('Meter door',(0,.44,-.125),(.49,.75,.04),'Olive')
box('Meter window',(0,.63,-.151),(.25,.16,.015),'Glass',.008)
for i in range(4):box('Vent slit',(0,.20+i*.035,-.151),(.25,.014,.016),'Steel',.002)
for x in [-.17,.17]:line('Cable conduit',[(x,.06,0),(x,-.13,0),(x+.06,-.19,0)],.026,'Steel')
box('Identification plate',(.07,.40,-.153),(.19,.075,.012),'Paper',.003)
finish('JunctionBox02','Workshop',REF+'07_24_05.png')

box('Toolbox case',(0,.13,0),(.63,.24,.30),'Rust');box('Toolbox lid',(0,.27,0),(.65,.075,.32),'Red')
for x in [-.24,.24]:box('Toolbox latch',(x,.245,-.172),(.06,.09,.023),'Edge',.006)
line('Toolbox grip',[(-.12,.31,0),(-.12,.39,0),(.12,.39,0),(.12,.31,0)],.014,'Steel')
box('Worn label',(.10,.14,-.154),(.17,.066,.012),'Paper',.005)
finish('Toolbox02','Workshop',REF+'07_24_05.png')

cyl('Extinguisher body',(0,.275,0),.12,.50,'Red');cyl('Extinguisher shoulder',(0,.54,0),.082,.075,'Red');cyl('Bottle valve',(0,.60,0),.035,.065,'Edge')
ring('Extinguisher base',(0,.04,0),.123,.018,'Steel');box('Safety lever',(.025,.65,0),(.17,.035,.05),'Steel',.008)
line('Extinguisher hose',[(0,.61,.035),(.14,.62,.02),(.20,.51,0),(.20,.20,0),(.14,.12,-.02)],.017,'Rubber')
box('Instruction label',(0,.31,-.121),(.13,.15,.012),'Paper',.004)
finish('FireExtinguisher02','Workshop',REF+'07_24_05.png')

for x in [-.46,.46]:
 for z in [-.26,.26]:box('Stove leg',(x,.43,z),(.045,.86,.045),'Steel')
box('Stove shelf',(0,.22,0),(.95,.05,.54),'Steel');box('Stove enamel top',(0,.89,0),(1.03,.10,.64),'Edge')
for x in [-.25,.25]:
 cyl('Burner recess',(x,.946,0),.18,.018,'Steel');ring('Burner ring',(x,.972,0),.11,.025,'Steel')
 for a in [0,math.pi/2]:beam('Pan support',(x-math.cos(a)*.19,.992,-math.sin(a)*.19),(x+math.cos(a)*.19,.992,math.sin(a)*.19),.025,'Steel')
for x in [-.24,.24]:box('Stove knob',(x,.85,-.345),(.07,.065,.045),'Rubber',.01)
finish('OutdoorStove02','Workshop',REF+'07_21_31.png',[collider([0,.47,0],[1.05,.94,.68])])

# Alley accumulation: hollow dumpster, soft bags, cardboard and patched partitions.
box('Dumpster floor',(0,.12,0),(1.39,.08,.82),'DarkOlive')
for x in [-.71,.71]:box('Dumpster side',(x,.63,0),(.08,1.05,.88),'Olive')
for z in [-.43,.43]:box('Dumpster wall',(0,.63,z),(1.48,1.05,.065),'Olive')
box('Hinged bin lid',(0,1.18,0),(1.56,.11,.98),'Steel')
for i in range(6):box('Lid rib',(-.64+i*.256,1.25,0),(.05,.06,.79),'Rubber',.008)
for x in [-.55,.55]:
 for z in [-.30,.30]:cyl('Caster stem',(x,.07,z),.045,.14,'Steel')
for x in [-.77,.77]:line('Bin lifting handle',[(x,.84,-.16),(x*1.09,.84,-.16),(x*1.09,.84,.16),(x,.84,.16)],.026,'Edge')
box('Municipal label',(.40,.88,-.466),(.26,.13,.01),'Paper',.004)
for x,y in [(-.42,.43),(.20,.30),(.52,.69)]:box('Local paint chip',(x,y,-.467),(.09,.025,.013),'Rust',.003)
finish('Dumpster02','Alley','Assets/GPT/프랍/안전구역프랍.png',[collider([0,.65,0],[1.67,1.3,1.0])])

for j,(x,z,r) in enumerate([(-.24,0,.23),(.18,.06,.20),(0,-.26,.17)]):
 levels=[(.015,r*.55),(.10,r),(.32,r*.87),(.46,r*.35),(.50,r*.13),(.56,r*.27)]
 vs=[];segments=12
 for k,(y,rad) in enumerate(levels):
  for i in range(segments):a=i*math.tau/segments;rr=rad*(1+.13*math.sin(i*3+k));vs.append((x+math.cos(a)*rr,y*(1-j*.12),z+math.sin(a)*rr))
 fs=[tuple(reversed(range(segments)))]
 for k in range(len(levels)-1):
  for i in range(segments):fs.append((k*segments+i,k*segments+(i+1)%segments,(k+1)*segments+(i+1)%segments,(k+1)*segments+i))
 fs.append(tuple(range((len(levels)-1)*segments,len(levels)*segments)));mesh('Tied refuse bag',vs,fs,'Rubber');ring('Bag tie',(x,.49*(1-j*.12),z),r*.15,.008,'Canvas')
finish('GarbageBags02','Alley','Assets/GPT/프랍/안전구역프랍.png')

for x,y,z,w,h,d in [(-.23,.22,0,.53,.43,.51),(.31,.18,.05,.47,.35,.43),(-.20,.61,.01,.42,.32,.43)]:
 box('Cardboard carton',(x,y,z),(w,h,d),'Cardboard',.007);box('Carton tape top',(x,y+h/2+.002,z),(.09,.006,d+.008),'Linen',.001)
 box('Carton tape front',(x,y,z-d/2-.004),(.09,h,.006),'Linen',.001);box('Shipping label',(x+w*.20,y+.01,z-d/2-.008),(.12,.09,.006),'Paper',.001)
finish('CardboardStack02','Supplies','Assets/GPT/프랍/안전구역프랍.png',[collider([.01,.39,.01],[1.02,.80,.57])])

for x in [-1.25,0,1.25]:box('Partition post',(x,1.02,0),(.13,2.04,.13),'WoodDark')
for i in range(20):
 x=-1.25+(i+.5)*2.5/20;height=1.54+.12*math.sin(i*3)
 box('Salvaged corrugated sheet',(x,1.04,.026),(.12,height,.036),'Edge' if i%3 else 'Olive',.005)
 box('Sheet fold',(x,1.04,0),(.045,height,.06),'Steel',.006)
for y in [.36,1.55]:box('Partition crossbar',(0,y,-.072),(2.65,.12,.12),'Wood')
beam('Crooked plank',(-1.15,.64,-.15),(1.17,1.19,-.15),.14,'Wood')
cloth('Partition tied canvas',.10,1.64,-.16,1.53,.70,'Linen')
line('Lashing',[(-1.25,1.79,-.10),(-.7,1.57,-.20),(.76,1.57,-.20),(1.25,1.79,-.10)],.013,'Canvas')
finish('PatchBarricade02','Alley','Assets/GPT/프랍/안전구역프랍9.png',[collider([0,1.02,0],[2.7,2.04,.38])])

for x,z,a in [(-.40,.12,.4),(.18,.21,-.3),(.20,-.29,.2)]:
 for i in range(6):
  xx=x+(i-2.5)*.085;box('Discarded sheet',(xx,.08+.04*math.sin(i*math.pi/2),z),(.075,.035,.64),'Edge',.004)
for j in range(4):
 y=.12+j*.053;line('Scrap tube',[(-.45+j*.09,y,-.40),(.28+j*.07,y,.32)],.032,'Steel')
for i in range(5):beam('Broken timber',(-.62+i*.16,.10+i*.027,-.14),(.15+i*.13,.12+i*.029,.23),.075,'WoodDark')
ring('Discarded tire',(.31,.24,.10),.24,.065,'Rubber')
box('Scrap electrical panel',(-.24,.29,.15),(.39,.09,.34),'Olive')
finish('ScrapHeap02','Alley','Assets/GPT/프랍/안전구역프랍8.png')

# Simple salvage bicycle: open spoke wheels and tube frame rather than a solid silhouette.
for x in [-.57,.57]:
 ring('Bicycle tire',(x,.34,0),.31,.029,'Rubber','z');ring('Bicycle wheel rim',(x,.34,0),.282,.009,'Edge','z')
 for i in range(16):a=i*math.tau/16;tube('Wheel spoke',(x,.34,0),(x+math.cos(a)*.278,.34+math.sin(a)*.278,0),.0035,'Edge')
 tube('Wheel axle',(x,.34,-.06),(x,.34,.06),.022,'Steel')
A=(-.57,.34,0);B=(-.10,.26,0);C=(-.19,.76,0);D=(.36,.80,0);E=(.57,.34,0)
for a,b in [(A,B),(A,C),(B,C),(C,D),(B,D),(D,E)]:tube('Cycle frame',a,b,.024,'Rust')
tube('Saddle post',C,(-.21,.91,0),.017,'Edge');box('Bicycle saddle',(-.22,.925,0),(.22,.075,.13),'Rubber')
line('Handlebar stem',[D,(.31,1.02,0),(.36,1.10,0)],.016,'Steel');tube('Handlebar',(.36,1.10,-.24),(.36,1.10,.24),.017,'Edge')
for z in [-.23,.23]:tube('Handlebar grip',(.36,1.10,z-.045),(.36,1.10,z+.045),.024,'Rubber')
ring('Chainring',(-.10,.26,-.05),.092,.012,'Steel','z');tube('Crank',(-.10,.26,-.07),(.04,.18,-.07),.011,'Edge')
box('Pedal',(.05,.18,-.105),(.09,.025,.07),'Rubber',.004)
finish('Bicycle02','Alley','Assets/GPT/프랍/안전구역프랍4.png',[collider([0,.60,0],[1.82,1.19,.50])])

# Publish compact metadata and pack the same maps used by the approved town/hideout.
used={m.name for n in assets for o in bpy.data.collections[n+'_Editable'].objects for m in o.data.materials}
pal={}
for name,(color,family) in palette.items():
 mat=mats[name]
 if mat.name not in used:continue
 nodes=mat.node_tree.nodes;links=mat.node_tree.links;bsdf=nodes.get('Principled BSDF')
 folder='Town02' if family in ['Brick','Plaster','Asphalt','Tile','Roof'] else 'Hideout02'
 textureRoot=ROOT/f'Assets/Art/Environments/{folder}/Textures';maps={}
 for kind in ['Base','Normal','Mask']:
  t=nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(textureRoot/f'{family}_{kind}.png'),check_existing=True);t.image.colorspace_settings.name='sRGB' if kind=='Base' else 'Non-Color';t.image.pack();maps[kind]=t
 tint=nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1;tint.inputs[2].default_value=(*color,1);links.new(maps['Base'].outputs['Color'],tint.inputs[1]);links.new(tint.outputs[0],bsdf.inputs['Base Color'])
 norm=nodes.new('ShaderNodeNormalMap');norm.inputs['Strength'].default_value=.28;links.new(maps['Normal'].outputs['Color'],norm.inputs['Color']);links.new(norm.outputs[0],bsdf.inputs['Normal'])
 inv=nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;links.new(maps['Mask'].outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],bsdf.inputs['Roughness'])
 split=nodes.new('ShaderNodeSeparateColor');links.new(maps['Mask'].outputs['Color'],split.inputs[0]);links.new(split.outputs[0],bsdf.inputs['Metallic'])
 pal[mat.name]={'color':list(color),'family':family,'textureFolder':f'Assets/Art/Environments/{folder}/Textures'}
groups={
 'Living':[('Clothesline02',(0,0,1.25),0),('UtilitySink02',(-1.30,0,-.40),0),('WaterBarrelStand02',(1.28,0,-.18),0),('Bucket02',(.73,0,-.89),0),('LaundryBasket02',(-.23,0,.26),0)],
 'Workshop':[('Ladder02',(-1.30,0,.60),0),('JunctionBox02',(-.48,.35,.66),0),('OutdoorStove02',(.78,0,.26),0),('Toolbox02',(-.80,0,-.56),-10),('FireExtinguisher02',(.03,0,-.37),0),('RopeCoil02',(.44,0,-.83),0),('FoldedTarp02',(-.07,0,-.97),0)],
 'Alley':[('PatchBarricade02',(0,0,1.04),0),('Dumpster02',(-1.15,0,-.14),0),('GarbageBags02',(-1.52,0,-1.04),0),('CardboardStack02',(.10,0,-.04),0),('ScrapHeap02',(.37,0,-1.08),0),('Bicycle02',(1.6,0,-.20),-25)]}
layout=bpy.data.collections.new('TownProps02_Editable_Layout');bpy.context.scene.collection.children.link(layout)
for j,(group,specs) in enumerate(groups.items()):
 for name,pos,rz in specs:
  for o in bpy.data.collections[name+'_Editable'].objects:
   cp=o.copy();layout.objects.link(cp);cp.location=(-pos[0]+(j-1)*6,-pos[2],pos[1]);cp.rotation_euler.z=-math.radians(rz)
manifest={'units':'metres','assets':{n:{**a,**inventory[n]} for n,a in assets.items()},'palette':pal,'reviewGroups':groups,'reuse':['Village01: Bench01, StreetLamp01, NoticeBoard01, OilDrum01, Pallet01, Tires01, Sandbags01, Brazier01, LowFence01','Safehouse01: WoodCrate01, Jerrycan01, Generator01','Town02: SpoolTable02, YardChair02, Planter02, MerchantCart02'],'status':'Offline model kit; Unity import, placement and interaction validation pending.'}
(OUT/'KitManifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/TownProps02.blend'))
print('TOWN_PROPS_COMPLETE',json.dumps({'models':len(assets),'triangles':sum(v['triangles'] for v in assets.values()),'materials':len(pal)}))
