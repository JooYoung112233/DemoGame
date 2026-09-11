"""Offline town interiors: metres, existing footprints, no live Unity mutations.
Editable component collections + reusable FBX + five unlit placement assemblies.
Coordinates of the production helpers are Unity x,height,depth.
"""
from pathlib import Path
HELPER=Path(__file__).with_name('build_town02.py')
head=HELPER.read_text(encoding='utf8').split('def window(')[0]
head=head.replace("OUT=ROOT/'Assets/Art/Environments/Town02'","OUT=ROOT.parent/'ArtWork/TownInteriors62'").replace("'Town_'","'Interior62_'")
exec(compile(head,str(HELPER),'exec'),globals())
from mathutils import Matrix
import bmesh,hashlib
inventory={};layouts={};reuse={};references={}
REF='Assets/GPT/안전구역/ChatGPT Image 2026년 6월 11일 오후 '
specs=[('Pawnshop',16,12,'Brick','Assets/GPT/안전구역/전당포/완전체.png'),('Repair',14,10,'Ochre',REF+'07_37_36.png'),('Medical',18,10,'Tile',REF+'07_40_15.png'),('Furniture',10,14,'Wood',REF+'07_42_47.png'),('BlackMarket',12,14,'Graphite',REF+'07_40_37.png')]
for n,c,f in [('Brass',(.34,.255,.12),'Steel'),('Linen',(.54,.50,.40),'Cloth'),('Upholstery',(.16,.235,.21),'Cloth'),('BlueCloth',(.11,.175,.19),'Cloth'),('Rubber',(.026,.03,.027),'PaintedMetal'),('WoodLight',(.33,.24,.14),'Wood')]:
 palette[n]=(c,f);m=bpy.data.materials.new('Interior62_'+n);m.use_nodes=True;m.diffuse_color=(*c,1);mats[n]=m
palette['WoodLight']=((.275,.187,.103),'Wood')
palette['PawnTile']=((.22,.225,.169),'Tile');m=bpy.data.materials.new('Interior62_PawnTile');m.use_nodes=True;mats['PawnTile']=m
palette['DarkConcrete']=((.105,.115,.105),'Plaster');m=bpy.data.materials.new('Interior62_DarkConcrete');m.use_nodes=True;mats['DarkConcrete']=m

def done(name,reference,foot=None,category='furniture'):
 vs=[v.co for o in parts for v in o.data.vertices]
 lo=[min(v[i] for v in vs) for i in range(3)];hi=[max(v[i] for v in vs) for i in range(3)]
 export(name)
 inventory[name]={**assets[name],'reference':reference,'size':[hi[0]-lo[0],hi[2]-lo[2],hi[1]-lo[1]],'category':category,'footprint':foot or [hi[0]-lo[0],hi[1]-lo[1]]}

def tube(n,a,b,r,m):
 a=Vector((-a[0],-a[2],a[1]));b=Vector((-b[0],-b[2],b[1]))
 bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=(b-a).length,location=(a+b)/2);o=bpy.context.object;o.name=n;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mats[m]);parts.append(o);return o

def torus(n,p,r,t,m,vertical=False):
 x,y,z=p;bpy.ops.mesh.primitive_torus_add(major_segments=20,minor_segments=8,major_radius=r,minor_radius=t,location=(-x,-z,y));o=bpy.context.object;o.name=n
 if vertical:o.rotation_euler.x=math.pi/2
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mats[m]);parts.append(o);return o

def mesh(n,vs,fs,m):
 me=bpy.data.meshes.new(n);me.from_pydata([(-x,-z,y) for x,y,z in vs],[],fs);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(n,me);bpy.context.scene.collection.objects.link(o);o.data.materials.append(mats[m]);parts.append(o);return o

def table(w,d,h,m='Wood'):
 for x in [-w/2+.10,w/2-.10]:
  for z in [-d/2+.1,d/2-.1]:box('Solid legs',(x,h/2,z),(.12,h,.12),m,.018)
 for z in [-d/2+.08,d/2-.08]:box('Apron',(0,h-.13,z),(w-.12,.18,.09),'WoodDark')
 count=max(3,round(d/.20))
 for i in range(count):box('Table plank',(0,h,-d/2+(i+.5)*d/count),(w,.09,d/count-.006),'WoodLight' if i%3==1 else m,.012)

def bottle(x,y,z,tint='Olive',r=.075,h=.25):
 cyl('Bottle body',(x,y+h*.4,z),r,h*.8,tint);cyl('Bottle shoulder',(x,y+h*.82,z),r*.78,h*.06,tint);cyl('Bottle neck',(x,y+h*.94,z),r*.40,h*.20,tint)
 box('Paper label',(x,y+h*.43,z-r-.003),(r*1.3,h*.30,.009),'Paper',.001)
 cyl('Bottle cap',(x,y+h*1.055,z),r*.45,h*.05,'Steel')

def case(x,y,z,w=.45):
 box('Supply case',(x,y+.12,z),(w,.24,.28),'Olive',.022)
 for xx in [x-w*.32,x+w*.32]:box('Case straps',(xx,y+.13,z),(w*.055,.26,.29),'DarkOlive',.004)
 box('Case latch',(x,y+.19,z-.147),(.07,.065,.02),'Brass',.004)

# Reuse approved workbench/cloth/crate designs without changing source files.
for kit,names in [('Hideout02',['Workbench01','WoodCrate01','Stool01','SupplyCase02','Radio01','MapBoard01','MedicalBox01','LockerPair02','Rug01']),('TownProps02',['Toolbox02_Editable','UtilitySink02_Editable','Bucket02_Editable','FireExtinguisher02_Editable','CardboardStack02_Editable','FoldedTarp02_Editable'])]:
 source=ROOT/f'Assets/Art/Environments/{kit}/BlenderSource~/{kit}.blend'
 reuse[str(source.relative_to(ROOT))]=hashlib.sha256(source.read_bytes()).hexdigest()
 with bpy.data.libraries.load(str(source),link=False) as (src,dst):dst.collections=names
 for col in dst.collections:
  bpy.context.scene.collection.children.link(col);col.hide_viewport=False;col.hide_render=False;bpy.context.view_layer.update()
  for o in list(col.objects):
   if o.type!='MESH':continue
   cp=o.copy();cp.data=o.data.copy();bpy.context.scene.collection.objects.link(cp);cp.parent=None
   cp.matrix_world=(Matrix.Rotation(math.pi,4,'Z') if kit=='Hideout02' else Matrix.Identity(4))@o.matrix_world
   bpy.ops.object.select_all(action='DESELECT');cp.select_set(True);bpy.context.view_layer.objects.active=cp;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);parts.append(cp)
  name='Reuse_'+col.name.replace('_Editable','')
  done(name,str(source.relative_to(ROOT)))
  col.hide_render=True;col.hide_viewport=True

pawn='Assets/GPT/프랍/전당포프랍1.png'
# Shallow glass display modules: visible velvet tray, actual transparent lid.
box('Cabinet plinth',(0,.09,0),(2.52,.18,.92),'WoodDark')
box('Display base',(0,.40,0),(2.45,.55,.88),'Wood')
for x in [-.8,0,.8]:box('Raised lower panel',(x,.40,-.453),(.73,.39,.035),'WoodDark',.01)
box('Velvet tray',(0,.76,0),(2.33,.10,.78),'DarkOlive')
for z in [-.43,.43]:box('Display brass rail',(0,1.05,z),(2.52,.065,.045),'Brass',.007)
for x in [-1.23,1.23]:
 box('Display brass cross rail',(x,1.05,0),(.05,.065,.88),'Brass',.007)
 for z in [-.43,.43]:box('Corner upright',(x,.91,z),(.045,.29,.045),'Steel',.005)
for x in [-.92,-.58,-.24,.12]:
 torus('Antique ring',(x,.826,-.08),.058,.014,'Brass');box('Price card',(x,.823,.17),(.13,.005,.08),'Paper',.001)
cyl('Pocket watch',(.52,.83,-.08),.125,.026,'Brass');box('Watch face',(.52,.847,-.08),(.13,.007,.13),'Paper',.002)
box('Watch hand',(.52,.853,-.08),(.015,.006,.075),'Steel',.001)
case(.94,.79,.02,.31)
# Independent glass material without noise or opaque painted-metal maps.
gm=bpy.data.materials.new('Interior62_DisplayGlass');gm.use_nodes=True;gp=gm.node_tree.nodes['Principled BSDF'];gp.inputs['Base Color'].default_value=(.80,.90,.85,1);gp.inputs['Transmission Weight'].default_value=.92;gp.inputs['Roughness'].default_value=.17;gp.inputs['IOR'].default_value=1.45;mats['DisplayGlass']=gm
trans=gm.node_tree.nodes.new('ShaderNodeBsdfTransparent');mix=gm.node_tree.nodes.new('ShaderNodeMixShader');mix.inputs[0].default_value=.10
gm.node_tree.links.new(trans.outputs[0],mix.inputs[1]);gm.node_tree.links.new(gp.outputs[0],mix.inputs[2]);gm.node_tree.links.new(mix.outputs[0],gm.node_tree.nodes['Material Output'].inputs['Surface'])
box('Clear display lid',(0,1.025,0),(2.38,.009,.79),'DisplayGlass',.001)
done('PawnDisplay62',pawn,[2.52,.92])

# Distinct merchandise for the three adjoining display modules.
for variant in ['Watches','Electronics']:
 for src in bpy.data.collections['PawnDisplay62_Editable'].objects:
  if src.name.startswith(('Antique ring','Pocket watch','Watch face','Watch hand','Supply case','Case straps','Case latch')):continue
  cp=src.copy();cp.data=src.data.copy();bpy.context.scene.collection.objects.link(cp);parts.append(cp)
 if variant=='Watches':
  for x in [-.82,-.24,.34,.90]:
   box('Watch leather strap',(x,.824,0),(.13,.026,.41),'WoodDark',.012)
   cyl('Watch brass body',(x,.85,0),.105,.035,'Brass');cyl('Watch paper face',(x,.872,0),.085,.007,'Paper')
   box('Watch hand',(x,.879,0),(.009,.006,.06),'Steel',.001)
 else:
  for x in [-.70,.34]:
   box('Salvaged camera body',(x,.87,0),(.42,.13,.26),'Steel',.022)
   tube('Camera lens',(x,.87,-.14),(x,.87,-.24),.075,'Edge')
   tube('Camera lens glass',(x,.87,-.245),(x,.87,-.25),.056,'Glass')
   box('Shutter button',(x-.13,.948,.02),(.045,.025,.035),'Brass',.005)
  tube('Flashlight',(.91,.86,-.18),(.91,.86,.19),.057,'Olive');tube('Flashlight lens',(.91,.86,-.185),(.91,.86,-.20),.066,'Paper')
 done('PawnDisplay_'+variant+'62',pawn,[2.52,.92])

# Register with sloped keyboard, cash drawer and paper roll.
box('Register base',(0,.08,0),(.46,.16,.43),'Steel',.028)
box('Cash drawer',(0,.07,-.226),(.40,.09,.03),'Edge',.01)
box('Receipt housing',(0,.28,.08),(.43,.25,.25),'Olive',.027)
box('Receipt strip',(.11,.405,.07),(.14,.008,.16),'Paper',.003)
for i in range(4):
 for j in range(4):box('Register key',(-.145+i*.095,.178+j*.012,-.145+j*.058),(.058,.024,.035),'Paper',.006)
box('Amount display',(0,.31,-.053),(.24,.06,.013),'Glass',.003)
done('CashRegister62',pawn,category='tabletop')

# Safe with actual hinges, inset door and radial wheel.
for x in [-.54,.54]:
 for z in [-.36,.36]:box('Safe feet',(x,.10,z),(.16,.20,.16),'Steel')
box('Safe case',(0,1.02,0),(1.38,1.84,1.0),'Steel',.06)
box('Door recess',(0,.96,-.51),(1.21,1.55,.05),'Rubber',.022)
box('Safe door',(.01,.96,-.55),(1.12,1.45,.08),'Edge',.025)
for y in [.40,1.45]:tube('Door hinge',(-.57,y-.12,-.61),(-.57,y+.12,-.61),.055,'Steel')
torus('Safe wheel',(.20,1.08,-.63),.18,.021,'Steel',True)
for a in [0,2.094,4.188]:beam('Wheel spoke',(.20,1.08,-.66),(.20+.18*math.cos(a),1.08+.18*math.sin(a),-.66),.026,'Steel')
tube('Safe dial',(-.24,.86,-.60),(-.24,.86,-.66),.075,'Brass')
box('Safe label',(0,1.66,-.506),(.25,.08,.009),'Brass',.004)
done('Safe62',pawn,[1.38,1.20])

# Open shelves containing mixed salvage, not repeated blank blocks.
for x in [-1.26,1.26]:box('Shelf sides',(x,1.05,0),(.10,2.1,.65),'Wood')
box('Shelf back',(0,1.05,.30),(2.5,2.05,.045),'WoodDark',.004)
for y in [.18,.70,1.23,1.77,2.10]:
 box('Shelf plank',(0,y,0),(2.62,.075,.69),'WoodLight',.012)
 if y>1.8:continue
 for i,x in enumerate([-.99,-.45,.15,.83]):
  if i%2==0:case(x,y+.04,0,.40 if i==0 else .47)
  else:bottle(x,y+.04,0,'Olive' if y<1 else 'Brass',r=.11,h=.32)
case(-.82,2.14,0,.52);bottle(-.13,2.14,.01,'Olive',r=.14,h=.35)
for i in range(4):box('Books on shelf crown',(.40+i*.13,2.33,.02),(.10,.36,.24),'Wood' if i%2 else 'Olive',.009)
done('SalvageShelf62',pawn,[2.62,.69])

# Antique clock and open ledger add recognizable trade objects to the upper surfaces.
box('Clock timber body',(0,.33,0),(.48,.66,.19),'WoodDark',.065)
tube('Clock brass rim',(0,.38,-.11),(0,.38,-.13),.193,'Brass');tube('Clock dial',(0,.38,-.132),(0,.38,-.14),.172,'Paper')
beam('Clock long hand',(0,.38,-.148),(0,.50,-.148),.016,'Steel');beam('Clock short hand',(0,.38,-.148),(.09,.34,-.148),.018,'Steel')
for a in range(12):
 angle=a*math.pi/6;box('Clock hour marks',(.14*math.sin(angle),.38+.14*math.cos(angle),-.149),(.014,.021,.006),'Steel',.001)
done('AntiqueClock62','Assets/GPT/프랍/전당포프랍2.png',category='tabletop')
table(1.6,.78,.84)
box('Open ledger cover',(-.30,.906,-.08),(.55,.04,.35),'WoodDark',.01)
for x in [-.44,-.16]:
 box('Ledger pages',(x,.931,-.08),(.25,.018,.31),'Paper',.004)
 for j in range(5):box('Ledger ruled lines',(x,.942,-.19+j*.046),(.20,.002,.003),'WoodDark',.001)
done('AntiqueDesk62','Assets/GPT/프랍/전당포프랍2.png',[1.6,.78])

# Wall tools: holders remain visible above the repair bench.
box('Tool board',(0,.70,0),(2.20,1.25,.085),'WoodDark')
for x in [-.88,-.50,-.10,.30,.70]:
 tube('Tool shaft',(x,.34,-.085),(x,1.06,-.085),.027,'Edge')
 for dx in [-.055,.055]:box('Open wrench jaws',(x+dx,1.11,-.085),(.036,.13,.045),'Edge',.004)
 box('Peg',(x,1.15,-.05),(.035,.025,.09),'Steel',.003)
for y in [.17,1.28]:box('Board border',(0,y,-.012),(2.28,.06,.12),'WoodLight',.008)
done('ToolBoard62',REF+'07_37_36.png',category='wall')

# Welding trolley, hoses and small cylinder valve.
box('Trolley base',(0,.14,0),(.77,.12,.66),'Steel')
for x in [-.30,.30]:
 torus('Trolley wheel',(x,.15,.19),.115,.042,'Rubber',True)
 beam('Trolley handles',(x,.12,.26),(x,1.54,.26),.045,'Red')
beam('Trolley top rail',(-.30,1.54,.26),(.30,1.54,.26),.045,'Red')
cyl('Gas cylinder',(0,.80,0),.22,1.18,'Olive');cyl('Cylinder shoulder',(0,1.42,0),.155,.10,'Olive');cyl('Gas valve',(0,1.52,0),.045,.13,'Brass')
for y in [.40,1.09]:box('Cylinder clamp',(0,y,-.212),(.52,.055,.06),'Steel',.007)
for k in range(3):torus('Coiled hose',(.02,.55+k*.03,-.30),.23,.023,'Rubber',True)
done('WeldingTrolley62',REF+'07_37_36.png',[.82,.85])
for y in [.14,.42,.70]:
 torus('Used tire',(0,y,0),.33,.125,'Rubber');torus('Tire bead',(0,y+.07,0),.224,.023,'Steel')
done('TireStack62',REF+'07_37_36.png',[.92,.92])
box('Creeper cushion',(0,.17,0),(.58,.12,1.3),'Red',.07)
box('Creeper headrest',(0,.25,.46),(.52,.09,.30),'Rubber',.04)
for x in [-.29,.29]:
 for z in [-.48,.48]:cyl('Caster',(x,.08,z),.065,.08,'Steel')
done('MechanicCreeper62',REF+'07_37_36.png',[.72,1.3])

# Treatment bed with segmented raised cushion and foot-side storage drawer.
for x in [-.48,.48]:
 for z in [-.90,.90]:box('Bed leg',(x,.33,z),(.085,.66,.085),'Cream',.012)
box('Medical bed frame',(0,.65,0),(1.16,.14,2.24),'Cream',.025)
box('Bed main cushion',(0,.78,-.27),(1.08,.18,1.62),'Upholstery',.075)
o=box('Raised bed head',(0,.98,.80),(1.08,.20,.70),'Upholstery',.065)
center=Vector((0,-.8,.98))
for v in o.data.vertices:v.co=center+Matrix.Rotation(math.radians(-20),3,'X')@(v.co-center)
box('Bed drawer',(0,.38,-.62),(.84,.23,.48),'Cream',.018);box('Drawer handle',(0,.40,-.878),(.25,.04,.045),'Steel',.006)
done('TreatmentBed62',REF+'07_40_15.png',[1.16,2.3])

for x in [-.70,.70]:box('Medicine cabinet side',(x,1.2,0),(.08,2.4,.55),'Cream')
box('Medicine cabinet back',(0,1.2,.245),(1.40,2.4,.055),'Sage')
for y in [.14,.76,1.30,1.83,2.40]:box('Medicine shelf',(0,y,0),(1.46,.07,.59),'Cream',.012)
for x in [-.34,.34]:
 box('Lower cupboard door',(x,.45,-.29),(.63,.53,.045),'Cream',.014);box('Cabinet handle',(x+.19,.47,-.33),(.035,.16,.04),'Steel',.005)
for y in [.80,1.34,1.87]:
 for i in range(5):bottle(-.53+i*.26,y,0,'Olive' if i%2 else 'Paper',h=.26)
done('MedicineCabinet62',REF+'07_40_15.png',[1.48,.65])

for x in [-.34,.34]:
 for z in [-.25,.25]:box('Cart leg',(x,.43,z),(.045,.78,.045),'Cream',.007);cyl('Cart caster',(x,.06,z),.065,.08,'Rubber')
for y in [.24,.84]:box('Instrument tray',(0,y,0),(.78,.055,.62),'Cream',.013)
for x in [-.35,.35]:beam('Cart guard',(x,.86,-.27),(x,.86,.27),.025,'Steel')
for x in [-.23,0,.23]:bottle(x,.873,.10,'Paper',r=.058,h=.19)
box('Folded gauze',(0,.92,-.12),(.31,.085,.15),'Linen',.02)
done('InstrumentCart62',REF+'07_40_15.png',[.80,.65])

# IV stand and a cloth screen, visibly thin with irregular hanging folds.
for a in range(5):
 ang=a*2*math.pi/5;beam('IV base',(0,.08,0),(.37*math.cos(ang),.08,.37*math.sin(ang)),.045,'Steel')
tube('IV pole',(0,.1,0),(0,2.05,0),.022,'Edge');beam('IV hooks',(-.28,2.02,0),(.28,2.02,0),.025,'Edge')
box('Fluid bag',(.20,1.72,0),(.16,.32,.055),'Paper',.04);box('Fluid level',(.20,1.70,-.033),(.12,.16,.008),'Sage',.004)
for a,b in zip([(.20,1.57,0),(.27,1.10,0),(.23,.69,0)],[(.27,1.10,0),(.23,.69,0),(.30,.45,0)]):tube('IV line',a,b,.008,'Sage')
done('IVStand62',REF+'07_40_15.png',[.8,.8])

for x in [-1.12,1.12]:
 beam('Screen pole',(x,.10,0),(x,1.92,0),.045,'Cream');beam('Screen foot',(x,.07,-.32),(x,.07,.32),.05,'Cream')
beam('Screen rail',(-1.12,1.91,0),(1.12,1.91,0),.045,'Cream')
vs=[];nx,nz=36,12
for j in range(nz+1):
 t=j/nz
 for i in range(nx+1):
  u=i/nx;vs.append((-1.07+2.14*u,.36+1.5*t+.035*math.sin(u*18)*(1-t),.05*math.sin(u*46)+.014*math.sin(u*17+t*2)))
fs=[(j*(nx+1)+i,j*(nx+1)+i+1,(j+1)*(nx+1)+i+1,(j+1)*(nx+1)+i) for j in range(nz) for i in range(nx)]
o=mesh('Curtain folds',vs,fs,'Upholstery');mod=o.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.004;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
done('PrivacyScreen62',REF+'07_40_15.png',[2.30,.7])

# Reception desk, monitor and record folder.
box('Reception base',(0,.49,0),(2.7,.96,.80),'Cream',.045)
box('Reception front inset',(0,.49,-.416),(2.34,.61,.032),'Sage',.016)
box('Reception top',(0,1.02,0),(2.83,.095,.96),'Sage',.035)
box('Monitor foot',(.23,1.09,.14),(.31,.055,.20),'Steel',.015);box('Monitor stem',(.23,1.22,.18),(.065,.25,.06),'Steel')
box('Monitor back',(.23,1.38,.16),(.50,.33,.065),'Steel',.022)
box('Patient folder',(-.78,1.079,-.08),(.38,.018,.32),'WoodLight',.008);box('Record paper',(-.78,1.091,-.08),(.33,.008,.27),'Paper',.002)
done('Reception62',REF+'07_40_15.png',[2.83,.96])

# Furniture shop: cabinet, table, spindle chairs and upholstered armchairs.
box('Wardrobe carcass',(0,1.18,0),(1.82,2.3,.74),'WoodDark')
for x in [-.44,.44]:
 box('Panelled cabinet door',(x,1.16,-.398),(.82,2.09,.065),'Wood',.02)
 for y in [.60,1.61]:box('Door field',(x,y,-.436),(.60,.77,.025),'WoodLight',.012)
 box('Wardrobe pull',(x+(.26 if x<0 else -.26),1.17,-.481),(.04,.15,.035),'Brass',.005)
for y in [.10,2.32]:box('Cabinet cornice',(0,y,0),(1.95,.12,.82),'Wood',.018)
done('Wardrobe62',REF+'07_42_47.png',[1.95,.87])
table(2.4,1.12,.84);done('DiningTable62',REF+'07_42_47.png',[2.4,1.12])
table(.56,.57,.47)
for x in [-.24,.24]:beam('Chair back uprights',(x,.46,.23),(x,1.07,.29),.055,'Wood')
for y in [.73,1.02]:box('Back slat',(0,y,.27),(.52,.10,.05),'WoodLight',.009)
done('WoodChair62',REF+'07_42_47.png',[.62,.66])
for color in ['Upholstery','BlueCloth','Linen']:
 for x in [-.34,.34]:
  for z in [-.32,.32]:box('Armchair feet',(x,.13,z),(.09,.26,.09),'Wood')
 box('Seat frame',(0,.32,0),(.87,.18,.85),'WoodDark',.025)
 box('Seat cushion',(0,.48,-.04),(.70,.23,.67),color,.085)
 box('Back cushion',(0,.87,.32),(.75,.74,.23),color,.085)
 for x in [-.43,.43]:box('Padded arm',(x,.67,0),(.19,.31,.82),color,.065)
 done('Armchair_'+color+'62',REF+'07_42_47.png',[1.05,.92])

# Contraband counter: partly draped cloth, supply packets, ammo boxes; no opaque lid.
table(3.6,.94,.96,'WoodDark')
box('Market cabinet',(0,.43,0),(3.34,.70,.78),'WoodDark')
for x in [-1.10,0,1.10]:box('Market inset',(x,.43,-.405),(1.00,.52,.02),'Wood')
for x in [-1.18,-.65]:case(x,1.01,.02,.44)
for x in [.30,.66,1.05]:box('Wrapped packet',(x,1.09,.05),(.28,.16,.38),'Linen',.05)
vs=[];nx,ny=38,20
for j in range(ny+1):
 t=j/ny
 for i in range(nx+1):
  u=i/nx;x=-1.80+3.6*u
  depth=.38-t*.91;height=1.035+.018*math.sin(u*22+t*3)
  if t>.78:height-=((t-.78)/.22)*(.42+.13*math.sin(u*5));depth=-.40-(t-.78)*.09
  # The left cloth hangs diagonally across the top and front edge, leaving the right goods visible.
  vs.append((x,height,depth))
fs=[]
for j in range(ny):
 for i in range(nx):
  if i/nx>.28+.30*(1-j/ny):continue
  fs.append((j*(nx+1)+i,j*(nx+1)+i+1,(j+1)*(nx+1)+i+1,(j+1)*(nx+1)+i))
o=mesh('Counter drape',vs,fs,'Canvas');mod=o.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.004;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
done('MarketCounter62',REF+'07_40_37.png',[3.6,1.05])

# Room architecture: lining fits inside Town02 shells; cutaway pieces are preview-only.
for name,w,d,wall,ref in specs:
 references[name]=ref
 box('Floor slab',(0,-.08,0),(w-.34,.16,d-.34),'DarkConcrete' if name=='BlackMarket' else 'Concrete',.015)
 if name in ['Furniture','Pawnshop']:
  front=-d/2+.19 if name=='Furniture' else .60
  span=d/2-.19-front;rows=math.ceil(span/.27)
  for j in range(rows):
   z=front+(j+.5)*span/rows
   for k in range(5):
    ww=(w-.38)/5;x=-(w-.38)/2+(k+.5)*ww
    box('Floor boards',(x,.012,z),(ww-.006,.025,span/rows-.005),'WoodLight' if (j+k)%7==0 else 'Wood',.002)
 if name in ['Medical','Pawnshop']:
  back=d/2-.18 if name=='Medical' else .58
  box('Tile floor',(0,.012,(-d/2+.18+back)/2),(w-.36,.025,back+d/2-.18),'Tile' if name=='Medical' else 'PawnTile',.002)
 # All indoor walking surface is at zero (+ 2.5cm finish); no raised threshold.
 done(name+'_Floor62',ref,category='architecture')
 # Rear wall is the only full-height preview wall. Tall lining remains a separate object.
 box('Rear interior plaster',(0,1.7,d/2-.175),(w-.36,3.4,.035),'Cream' if name=='Medical' else wall,.002)
 box('Rear wall lower trim',(0,.50,d/2-.208),(w-.39,1.0,.032),'Sage' if name=='Medical' else 'WoodDark' if name=='Furniture' else 'Concrete',.003)
 box('Rear chair rail',(0,1.02,d/2-.235),(w-.40,.06,.035),'Sage' if name=='Medical' else 'Wood',.005)
 done(name+'_RearLining62',ref,category='architecture')
 for x in [-w/2+.175,w/2-.175]:box('Side interior lining',(x,1.7,0),(.035,3.4,d-.36),'Cream' if name=='Medical' else wall,.002)
 for side in [-1,1]:box('Front interior lining',(side*(w+1.6)/4,1.2,-d/2+.175),((w-1.6)/2-.19,2.4,.035),wall,.002)
 done(name+'_TallLining62',ref,category='occlusion')
 for x in [-w/2+.17,w/2-.17]:box('Cutaway side',(x,.18,0),(.16,.36,d-.18),'Concrete')
 for side in [-1,1]:box('Cutaway front',(side*(w+1.6)/4,.18,-d/2+.17),((w-1.6)/2-.18,.36,.16),'Concrete')
 done(name+'_PreviewCutaway62',ref,category='previewOnly')

# Bind low-contrast approved maps to new surfaces; no global material mutations.
material_specs={}
for name,(color,family) in palette.items():
 m=mats[name];p=m.node_tree.nodes.get('Principled BSDF');nodes=m.node_tree.nodes;links=m.node_tree.links
 textureRoot=ROOT/'Assets/Art/Environments'/('Town02' if family in ['Brick','Plaster','Asphalt','Tile','Roof'] else 'Hideout02')/'Textures'
 maps={}
 for kind in ['Base','Normal','Mask']:
  t=nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(textureRoot/f'{family}_{kind}.png'),check_existing=True);t.image.colorspace_settings.name='sRGB' if kind=='Base' else 'Non-Color';t.image.pack();maps[kind]=t
 tint=nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1;tint.inputs[2].default_value=(*color,1);links.new(maps['Base'].outputs['Color'],tint.inputs[1]);links.new(tint.outputs[0],p.inputs['Base Color'])
 rough=.52 if name=='Brass' else .73 if name in ['Steel','Edge'] else .90 if family in ['Cloth','Wood'] else .82
 metallic=.52 if name in ['Steel','Edge','Brass'] else 0
 p.inputs['Metallic'].default_value=metallic;p.inputs['Roughness'].default_value=rough;p.inputs['Specular IOR Level'].default_value=.26
 # Small masked variation around material-specific roughness, never identical metal/cloth.
 sep=nodes.new('ShaderNodeSeparateColor');links.new(maps['Mask'].outputs[0],sep.inputs[0])
 vary=nodes.new('ShaderNodeMath');vary.operation='MULTIPLY_ADD';vary.inputs[1].default_value=.08;vary.inputs[2].default_value=rough-.04;links.new(sep.outputs[1],vary.inputs[0]);links.new(vary.outputs[0],p.inputs['Roughness'])
 normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.25 if family=='Brick' else .12;links.new(maps['Normal'].outputs[0],normal.inputs['Color']);links.new(normal.outputs[0],p.inputs['Normal'])
 material_specs[m.name]={'color':color,'family':family,'roughness':rough,'metallic':metallic,'normalStrength':normal.inputs['Strength'].default_value}

# Placements stay in Unity metres. Furniture and BlackMarket inherit the east-facing exterior yaw.
def place(room,asset,x,z,y=0,yaw=0,scale=1,kind='obstacle'):
 layouts.setdefault(room,[]).append({'asset':asset,'position':[x,y,z],'yaw':yaw,'scale':scale,'kind':kind})
for name,w,d,wall,ref in specs:
 for suffix in ['Floor62','RearLining62','TallLining62','PreviewCutaway62']:place(name,name+'_'+suffix,0,0,kind='architecture')

N='Pawnshop'
for x,a in [(-2.66,'PawnDisplay62'),(0,'PawnDisplay_Watches62'),(2.66,'PawnDisplay_Electronics62')]:place(N,a,x,1.5)
place(N,'CashRegister62',-2.6,1.62,1.07,kind='tabletop')
for x in [-5.45,5.45]:place(N,'SalvageShelf62',x,4.60)
place(N,'Safe62',2.1,4.75);place(N,'ToolBoard62',-1.1,5.78,1.02,kind='wall')
for z in [-2.0,1.5]:place(N,'SalvageShelf62',-6.7,z,yaw=90)
place(N,'Reuse_Radio01',5.4,4.55,2.16,kind='tabletop');place(N,'Reuse_WoodCrate01',-6.2,-3.4);place(N,'Reuse_CardboardStack02',-5.2,-4.2)
place(N,'Armchair_Upholstery62',5.8,-3.3,yaw=-30);place(N,'Reuse_Stool01',4.3,-3.6);place(N,'Reuse_Rug01',0,-3.8,scale=1.4,kind='floorDressing')
place(N,'AntiqueDesk62',5.7,-.45,yaw=-90);place(N,'AntiqueClock62',5.8,-.05,.90,yaw=-90,kind='tabletop')
place(N,'Reuse_Radio01',5.7,-.90,.90,yaw=-90,kind='tabletop');place(N,'Reuse_MapBoard01',5.2,5.79,2.2,kind='wall')
place(N,'AntiqueClock62',-5.1,4.52,2.14,kind='tabletop')
N='Repair'
for x in [-2.5,0,2.5]:place(N,'Reuse_Workbench01',x,3.65)
for x in [-2.5,0]:place(N,'ToolBoard62',x,4.76,1.02,kind='wall')
place(N,'SalvageShelf62',-5.1,3.75);place(N,'Reuse_LockerPair02',5.10,3.58)
place(N,'WeldingTrolley62',-4.7,.9);place(N,'TireStack62',5.3,.3);place(N,'TireStack62',5.3,-.85)
place(N,'Reuse_UtilitySink02',-5,-2.3,yaw=90);place(N,'Reuse_Toolbox02',-2.6,3.45,.96,kind='tabletop');place(N,'MechanicCreeper62',2.8,-1.8,yaw=-22)
place(N,'Reuse_WoodCrate01',4.8,-3.2);place(N,'Reuse_Bucket02',-4,-3.35);place(N,'Reuse_FireExtinguisher02',-6.25,-3.2)
N='Medical'
place(N,'Reception62',0,-1.65)
for x in [-2.6,1.0]:place(N,'TreatmentBed62',x,2.6,yaw=90)
place(N,'IVStand62',-4.0,3.5);place(N,'IVStand62',-.4,3.5);place(N,'InstrumentCart62',-2.6,.9);place(N,'InstrumentCart62',1,.9)
place(N,'PrivacyScreen62',-1.1,2.5,yaw=90)
for x in [-6.1,6.15]:place(N,'MedicineCabinet62',x,3.88)
place(N,'MedicineCabinet62',7.6,-1.6,yaw=-90);place(N,'Reuse_UtilitySink02',-7.15,1.3,yaw=90)
for x in [-6.9,-5.5,-4.1]:place(N,'Armchair_BlueCloth62',x,-3.45)
place(N,'Reuse_MedicalBox01',2.7,3.0,kind='obstacle');place(N,'Reuse_MapBoard01',3.8,4.79,1.8,kind='wall')
N='Furniture'
place(N,'Wardrobe62',2.95,5.95);place(N,'SalvageShelf62',-2.85,5.95)
place(N,'Armchair_Upholstery62',-.30,5.55)
place(N,'DiningTable62',-.40,1.55)
for x in [-1.16,.36]:
 place(N,'WoodChair62',x,2.55);place(N,'WoodChair62',x,.55,yaw=180)
place(N,'Armchair_BlueCloth62',-3.1,1.0,yaw=90);place(N,'Armchair_Linen62',-3.1,-2.35,yaw=90)
place(N,'Reuse_Rug01',-.35,1.6,scale=1.9,kind='floorDressing')
place(N,'DiningTable62',-2.4,-4.8)
for z in [-3.45,-4.8]:place(N,'WoodChair62',2.1,z,yaw=-90)
place(N,'Reuse_WoodCrate01',-3.9,3.8);place(N,'Reuse_CardboardStack02',3.85,2.5)
N='BlackMarket'
place(N,'MarketCounter62',0,-2.1);place(N,'Safe62',3.90,5.85)
for x in [-3.7,-1.8]:place(N,'Reuse_WoodCrate01',x,5.25,scale=1.25)
place(N,'Reuse_WoodCrate01',-3.7,5.25,.99,scale=.9)
place(N,'SalvageShelf62',-4.9,1.8,yaw=90);place(N,'SalvageShelf62',4.9,1.8,yaw=-90)
for z in [.3,3.7]:place(N,'Reuse_WoodCrate01',4.4,z)
place(N,'Reuse_LockerPair02',1.35,5.8)
place(N,'Reuse_FoldedTarp02',-1.8,5.25,inventory['Reuse_WoodCrate01']['size'][1]*1.25,scale=.70,kind='tabletop')
place(N,'Reuse_Radio01',-1.18,-2.08,1.25,kind='tabletop')
place(N,'Reuse_CardboardStack02',-4.5,-4.8);place(N,'Reuse_Rug01',0,-4.6,scale=1.4,kind='floorDressing')

assemblies={}
for name,placements in layouts.items():
 col=bpy.data.collections.new(name+'_Assembly62');bpy.context.scene.collection.children.link(col);assemblies[name]=col
 for i,p in enumerate(placements):
  a=p['asset'];x,y,z=p['position'];mat=Matrix.Translation((-x,-z,y))@Matrix.Rotation(-math.radians(p['yaw']),4,'Z')@Matrix.Scale(p['scale'],4)
  for o in bpy.data.collections[a+'_Editable'].objects:
   cp=o.copy();col.objects.link(cp);cp.name=f'{name}_{i:02d}_{o.name}';cp.matrix_world=mat@o.matrix_world
   cp['asset']=a;cp['placement_index']=i
   if a.endswith('TallLining62'):cp.hide_render=True
  p['footprint']=inventory[a]['footprint']
 col.hide_render=True;col.hide_viewport=True
# Pack all inherited textures as well; preserve portable source.
for im in bpy.data.images:
 if im.source=='FILE' and not im.packed_file:
  try:im.pack()
  except RuntimeError:pass
worlds={'Pawnshop':[34,0,44,0],'Repair':[12,0,44,0],'Medical':[64,0,44,0],'Furniture':[12,0,27,90],'BlackMarket':[12,0,10,90]}
manifest={'units':'metres','coordinateConvention':'Unity local x,height,depth; front -depth. Blender (-x,-depth,height).','camera':[62,0],'assets':inventory,'materials':material_specs,'layouts':layouts,'buildings':{n:{'width':w,'depth':d,'doorWidth':1.6,'worldPlacementXYZYaw':worlds[n],'reference':ref} for n,w,d,wall,ref in specs},'sourceHashes':reuse,'pawnshopNpcKeepout':{'center':[0,0,2.4],'radius':.45},'status':'offline art; no Unity import, NPC rewiring, roof handling or collision applied'}
(OUT/'KitManifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/TownInteriors62.blend'))
print('TOWN_INTERIORS62_BUILT',len(inventory),'models',sum(len(p) for p in layouts.values()),'placements')
