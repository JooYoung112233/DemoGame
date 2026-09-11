"""Hideout interior art pass, based on GPT/안전구역/주인공집/2026-06-10 concept.
Run Blender --background --python this_file. Source parts remain editable;
Unity exports batch static parts per asset with material submeshes.
"""
import bpy, bmesh, math, json, random, sys
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Art/Environments/Hideout02'
for p in ['Models','BlenderSource~','Previews']: (OUT/p).mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0
sc=bpy.context.scene;sc.name='Hideout02_ReferenceInterior';sc.unit_settings.system='METRIC'
library=bpy.data.collections.new('01_Editable_Asset_Library');sc.collection.children.link(library)
layout=bpy.data.collections.new('02_Assembled_Safehouse');sc.collection.children.link(layout)
studio=bpy.data.collections.new('03_Review_Only');sc.collection.children.link(studio)
rng=random.Random(910);mats={};assets={};placements=[];current=None;parent=None
palette={
 'Olive':(.135,.148,.102),'OliveLight':(.205,.215,.150),'OliveDark':(.075,.088,.061),
 'Steel':(.048,.061,.058),'EdgeMetal':(.13,.15,.14),'Wear':(.255,.225,.153),
 'Wood':(.185,.117,.065),'WoodLight':(.235,.159,.095),'WoodDark':(.117,.078,.047),
 'Canvas':(.18,.185,.136),'Blanket':(.075,.101,.092),'Linen':(.40,.355,.257),
 'Rubber':(.024,.028,.027),'Paper':(.50,.447,.33),'Red':(.24,.07,.041),
 'Glass':(.067,.108,.119),'LampGlow':(.95,.59,.23),'Grey':(.18,.19,.17),
 'Concrete':(.145,.151,.142),'MapLine':(.245,.253,.193),'Ink':(.085,.106,.096)}
for n,c in palette.items():
 m=bpy.data.materials.new('Hideout_'+n);m.diffuse_color=(*c,1);m.use_nodes=True
 p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*c,1)
 p.inputs['Roughness'].default_value=.86;p.inputs['Specular IOR Level'].default_value=.22
 if n in ['Steel','EdgeMetal']:p.inputs['Metallic'].default_value=.4;p.inputs['Roughness'].default_value=.7
 if n=='LampGlow':p.inputs['Emission Color'].default_value=(*c,1);p.inputs['Emission Strength'].default_value=2
 mats[n]=m

def empty(n,col,par=None,loc=(0,0,0)):
 o=bpy.data.objects.new(n,None);col.objects.link(o);o.parent=par;o.location=loc;o.empty_display_size=.10;return o
def asset(n):
 global current,parent
 col=bpy.data.collections.new(n);library.children.link(col);r=empty(n,col)
 assets[n]={'root':r,'col':col,'colliders':[]};current=assets[n];parent=r;return r
def collider(loc,size,par=''):
 current['colliders'].append({'center':loc,'size':size,'parent':par})
def finish(o,n,mat,par=None):
 o.name=n
 for c in list(o.users_collection):c.objects.unlink(o)
 current['col'].objects.link(o);o.parent=par or parent;o.data.materials.append(mats[mat]);return o
def box(n,loc,size,mat,bevel=.012,rot=None,par=None):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size
 if rot:o.rotation_euler=rot
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Single broad edge','BEVEL');mod.width=min(bevel,min(size)*.3);mod.segments=1
  bpy.ops.object.modifier_apply(modifier=mod.name)
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 return finish(o,n,mat,par)
def mesh(n,vs,fs,mat):
 me=bpy.data.meshes.new(n);me.from_pydata(vs,[],fs);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(n,me);current['col'].objects.link(o);o.parent=parent;o.data.materials.append(mats[mat]);return o
def cyl(n,loc,r,depth,mat,axis=(0,0,1),verts=10):
 bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=r,depth=depth,location=loc)
 o=bpy.context.object;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);return finish(o,n,mat)
def beam(n,a,b,width,mat):
 a,b=Vector(a),Vector(b);o=box(n,(a+b)/2,(width,width,(b-a).length),mat,bevel=width*.15,rot=(b-a).to_track_quat('Z','Y').to_euler());return o
def ringmesh(n,rings,mat):
 # Eight-sided cushions, cloth and sculpted large surfaces.
 vs=[]
 for w,d,z,dx,dy in rings:
  c=min(w,d)*.24
  vs.extend([(dx-w+c,dy-d,z),(dx+w-c,dy-d,z),(dx+w,dy-d+c,z),(dx+w,dy+d-c,z),(dx+w-c,dy+d,z),(dx-w+c,dy+d,z),(dx-w,dy+d-c,z),(dx-w,dy-d+c,z)])
 fs=[tuple(reversed(range(8)))]
 for j in range(len(rings)-1):
  for i in range(8):fs.append((j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i))
 fs.append(tuple(range((len(rings)-1)*8,len(rings)*8)));return mesh(n,vs,fs,mat)
def bolts(xs,y,z):
 for x in xs:cyl('Bolt',(x,y,z),.017,.012,'EdgeMetal',(0,1,0),6)
def scuff(loc,size,mat='Wear'):
 x,y,z=loc;w,d=size
 return mesh('Paint_Chip',[(x-w/2,y,z),(x+w*.3,y,z+.003),(x+w/2,y,z+d*.7),(x-w*.25,y,z+d)],[(0,1,2,3)],mat)

# Architecture: all panels meet on a 1 m grid. Wall front points -Y.
asset('FloorTile01')
box('Steel_Subframe',(0,0,-.105),(1,1,.16),'Steel',.015)
for i in range(5):
 x=-.4+i*.2;box('Floor_Plank',(x,0,-.023),(.196,.986,.045),['Wood','WoodDark','Wood','WoodLight','Wood'][i],.005)
collider((0,0,-.06),(1,1,.12))

def panel_section(x,width,z,height):
 box('Wall_Sheet',(x,.024,z),(width,.055,height),'Olive',.006)
 count=max(1,round(width/.17))
 for i in range(count):
  xx=x-width/2+(i+.5)*width/count
  box('Corrugation',(xx,-.025,z),(.065,.057,height-.015),'OliveLight',.007)
  box('Outer_Corrugation',(xx,.078,z),(.065,.057,height-.015),'Olive',.007)
def borders():
 for z in [.06,2.61]:box('Panel_Rail',(0,0,z),(1,.15,.12),'Steel',.008)
 for x in [-.48,.48]:box('Panel_Stile',(x,0,1.335),(.04,.09,2.55),'EdgeMetal',.006)
def solidwall():
 panel_section(0,1,1.335,2.55);borders();scuff((-.27,-.055,.3),(.10,.024));scuff((.19,-.055,2.3),(.065,.018))
asset('WallSolid01');solidwall();collider((0,0,1.335),(1,.14,2.67))

asset('WallWindow01')
panel_section(0,1,.56,1.0);panel_section(0,1,2.385,.45)
for x in [-.445,.445]:panel_section(x,.11,1.62,1.12)
borders()
for x in [-.395,.395]:box('Window_Frame',(x,-.065,1.62),(.055,.08,1.16),'Steel',.008)
for z in [1.06,2.18]:box('Window_Frame',(0,-.065,z),(.83,.08,.055),'Steel',.008)
box('Window_DarkPane',(0,.025,1.62),(.74,.015,1.055),'Glass',.002)
box('Window_Crossbar',(0,-.07,1.62),(.035,.055,1.08),'Steel',.004)
box('Window_Sill',(0,-.10,1.035),(.90,.24,.045),'OliveLight',.008)
for x in [-.395,.395]:box('Outer_WindowFrame',(x,.082,1.62),(.055,.08,1.16),'Steel',.008)
for z in [1.06,2.18]:box('Outer_WindowFrame',(0,.082,z),(.83,.08,.055),'Steel',.008)
box('Outer_WindowCrossbar',(0,.086,1.62),(.035,.055,1.08),'Steel',.004)
collider((0,0,1.335),(1,.14,2.67))

asset('WallDoorFrame01')
for x in [-.472,.472]:box('Door_Jamb',(x,0,1.1),(.055,.17,2.2),'Steel',.01);collider((x,0,1.1),(.055,.17,2.2))
panel_section(0,1,2.435,.35);box('Door_Lintel',(0,0,2.23),(1,.17,.08),'Steel',.01)
box('Door_TopRail',(0,0,2.61),(1,.15,.12),'Steel',.008)
box('Threshold',(0,0,.016),(1,.25,.032),'EdgeMetal',.007);collider((0,0,2.445),(1,.17,.45))

asset('Door01')
hinge=empty('Door_Hinge',current['col'],parent,(-.438,0,0));hinge['open_axis']='Blender Z / Unity Y';hinge['open_degrees']=105
parent=hinge
# Geometry is local to the left hinge.
box('Door_Slab',(.438,0,1.08),(.872,.082,2.14),'OliveDark',.016)
for z in [.17,1.93]:box('Door_Reinforcement',(.438,-.055,z),(.79,.036,.11),'OliveLight',.005)
for z in [.33,1.76]:cyl('Hinge_Barrel',(0,0,z),.026,.19,'EdgeMetal')
box('Lock_Plate',(.76,-.059,.98),(.076,.02,.21),'Steel',.007)
beam('Door_Handle',(.70,-.11,1.02),(.80,-.11,1.02),.028,'EdgeMetal')
box('Outer_LockPlate',(.76,.059,.98),(.076,.02,.21),'Steel',.007)
beam('Outer_DoorHandle',(.70,.11,1.02),(.80,.11,1.02),.028,'EdgeMetal')
box('Door_Label',(.44,-.045,1.69),(.20,.008,.105),'Paper',.005)
scuff((.15,-.044,.46),(.085,.023));collider((.438,0,1.08),(.872,.082,2.14),'Door_Hinge')

asset('CornerPost01');box('Square_Post',(0,0,1.335),(.15,.15,2.67),'Steel',.01)
for z in [.06,2.61]:box('Casting',(0,0,z),(.21,.21,.12),'EdgeMetal',.014)
collider((0,0,1.335),(.18,.18,2.67))
asset('RoofTile01');box('Roof_Sheet',(0,0,.035),(1,1,.07),'Olive',.008)
for i in range(5):box('Roof_Rib',(-.4+i*.2,0,.077),(.07,.995,.045),'OliveLight',.007)
collider((0,0,.04),(1,1,.08))
asset('EntryStep01')
box('Step_Base',(0,0,.06),(1.25,.70,.12),'Steel',.015)
for i in range(4):box('Step_Plank',(0,-.27+i*.18,.135),(1.22,.172,.04),'Wood',.005)
collider((0,0,.08),(1.25,.70,.16))

# Workbench: thick planks, offset repair patch, vise and tool rack kept separate.
asset('Workbench01')
for x in [-.71,.71]:
 for y in [-.30,.30]:box('Leg',(x,y,.43),(.075,.075,.86),'Steel',.008);collider((x,y,.43),(.075,.075,.86))
for y in [-.31,.31]:box('Apron',(0,y,.77),(1.52,.055,.12),'OliveDark',.008)
for i in range(4):box('Top_Plank',(0,-.285+i*.19,.88),(1.70,.184,.095),['Wood','WoodLight','Wood','WoodDark'][i],.012)
collider((0,0,.88),(1.7,.76,.095))
for i in range(3):box('Lower_Shelf',(0,-.22+i*.22,.20),(1.43,.21,.055),'WoodDark',.007)
collider((0,0,.20),(1.46,.70,.055))
box('Repair_Sheet',(-.5,-.20,.934),(.35,.27,.012),'EdgeMetal',.004)
box('Vise_Base',(.58,-.14,.976),(.23,.20,.08),'Steel',.012)
for x in [.50,.65]:box('Vise_Jaw',(x,-.14,1.06),(.065,.23,.10),'EdgeMetal',.008)
cyl('Vise_Screw',(.71,-.14,1.00),.020,.24,'Steel',(1,0,0))
beam('Vise_Handle',(.82,-.14,.94),(.82,-.14,1.12),.016,'EdgeMetal')
for x in [-.66,.66]:box('Rack_Post',(x,.31,1.15),(.04,.055,.53),'Steel',.004)
box('Tool_Rack',(0,.34,1.30),(1.43,.035,.28),'WoodDark',.007)
for i in range(4):
 x=-.5+i*.24;box('Tool_Grip',(x,.30,1.27),(.035,.03,.14),'WoodLight',.006)
 box('Tool_Head',(x,.30,1.37),(.085,.036,.045),'EdgeMetal',.004)

asset('Cot01')
for x in [-.43,.43]:
 for y in [-.84,.84]:box('Bed_Leg',(x,y,.21),(.07,.07,.42),'Steel',.009)
for x in [-.46,.46]:box('Bed_SideRail',(x,0,.34),(.07,2.03,.10),'OliveDark',.01)
for y in [-.96,.96]:box('Bed_EndRail',(0,y,.34),(.93,.07,.10),'Steel',.01)
for x in [-.43,.43]:beam('Head_Post',(x,.94,.3),(x,.94,.82),.05,'Steel')
beam('Head_Rail',(-.43,.94,.82),(.43,.94,.82),.05,'Steel')
ringmesh('Mattress',[(.435,.955,.37,0,0),(.458,.97,.43,0,0),(.43,.94,.51,0,0)],'Canvas')
ringmesh('Pillow',[(.31,.18,.51,0,.67),(.335,.19,.55,0,.67),(.28,.16,.615,-.015,.67)],'Linen')
ringmesh('Folded_Blanket',[(.46,.54,.505,0,-.39),(.475,.55,.55,0,-.39),(.44,.53,.575,0,-.39)],'Blanket')
for x in [-.31,.31]:box('Blanket_Seam',(x,-.39,.576),(.018,1.00,.003),'Canvas',.001)
collider((0,0,.285),(.99,2.05,.57))

asset('Shelf01')
for x in [-.53,.53]:
 for y in [-.205,.205]:box('Shelf_Upright',(x,y,.91),(.045,.045,1.82),'Steel',.005);collider((x,y,.91),(.045,.045,1.82))
for z in [.13,.66,1.18,1.70]:
 box('Shelf_Tray',(0,0,z),(1.14,.49,.045),'Olive',.007);collider((0,0,z),(1.14,.49,.045))
 box('Shelf_Lip',(0,-.25,z+.005),(1.14,.025,.055),'OliveLight',.004)
beam('Back_CrossBrace',(-.51,.23,.17),(.51,.23,1.68),.025,'EdgeMetal')
beam('Back_CrossBrace',(.51,.235,.17),(-.51,.235,1.68),.025,'EdgeMetal')

asset('Stool01')
for x,y in [(-.19,-.16),(.19,-.16),(0,.19)]:beam('Stool_Leg',(x*1.2,y*1.2,.025),(x,y,.43),.045,'Steel')
cyl('Stool_Seat',(0,0,.445),.27,.075,'Wood',verts=12);collider((0,0,.25),(.51,.49,.49))

asset('FloorLamp01')
cyl('Heavy_Base',(0,0,.05),.25,.10,'Steel');cyl('Stand',(0,0,.84),.025,1.5,'EdgeMetal')
beam('Angled_Neck',(0,0,1.54),(0,-.20,1.76),.036,'Steel')
bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=.235,radius2=.105,depth=.15,location=(0,-.20,1.69));finish(bpy.context.object,'Lamp_Shade','OliveLight')
cyl('Diffuser',(0,-.20,1.61),.21,.015,'LampGlow');collider((0,0,.82),(.20,.20,1.64))

asset('WallLamp01')
box('Bracket',(0,.025,0),(.16,.055,.24),'Steel',.01)
box('Lamp_Housing',(0,-.08,0),(.23,.15,.32),'OliveDark',.022)
box('Light_Diffuser',(0,-.165,0),(.16,.022,.23),'LampGlow',.022)
for x in [-.055,.055]:beam('Guard',(x,-.188,-.135),(x,-.188,.135),.014,'Steel')
collider((0,-.05,0),(.23,.22,.32))

asset('MapBoard01')
box('Board',(0,0,0),(1.24,.05,.79),'WoodDark',.014)
box('Canvas_Pinboard',(0,-.03,0),(1.13,.018,.68),'Canvas',.006)
box('Map_Paper',(-.12,-.044,.03),(.68,.006,.49),'Paper',.004,rot=(0,math.radians(-3),0))
# Abstract route strokes: editable, no tiny generated words.
for a,b in [((-.39,-.05,-.08),(.03,-.05,.12)),((-.24,-.05,.23),(-.15,-.05,-.17)),((-.42,-.05,.13),(.09,-.05,.01))]:beam('Map_Route',a,b,.012,'MapLine')
for x,z in [(-.25,.1),(-.1,.025),(.05,.02)]:cyl('Map_Pin',(x,-.06,z),.015,.012,'Red',(0,1,0),8)
for z in [-.14,.12]:box('Note',(.39,-.045,z),(.18,.007,.15),'Linen',.002)
collider((0,0,0),(1.24,.07,.79))

asset('Generator01')
for x in [-.35,.35]:
 for y in [-.235,.235]:box('Rubber_Foot',(x,y,.035),(.11,.10,.07),'Rubber',.01)
for y in [-.25,.25]:
 for x in [-.36,.36]:beam('Cage_Post',(x,y,.055),(x,y,.59),.04,'Steel')
 beam('Cage_Top',(-.36,y,.59),(.36,y,.59),.04,'Steel')
for x in [-.36,.36]:beam('Cage_Handle',(x,-.25,.59),(x,.25,.59),.04,'Steel')
box('Engine',(0,0,.27),(.56,.39,.33),'Steel',.035)
box('Fuel_Tank',(0,0,.485),(.55,.41,.135),'Olive',.024)
cyl('Fuel_Cap',(.17,0,.575),.045,.028,'EdgeMetal')
box('Control_Panel',(0,-.215,.29),(.30,.035,.19),'OliveLight',.007)
for x in [-.07,.06]:cyl('Socket',(x,-.24,.27),.026,.02,'Rubber',(0,1,0))
for i in range(5):box('Cooling_Slat',(.293,-.10+i*.045,.25),(.012,.015,.20),'EdgeMetal',.003)
cyl('Starter_Hub',(-.30,0,.24),.135,.07,'EdgeMetal',(1,0,0),12)
collider((0,0,.31),(.77,.55,.62))

asset('Radio01')
box('Radio_Case',(0,0,.16),(.43,.17,.30),'OliveDark',.025)
box('Speaker',( -.105,-.092,.17),(.17,.014,.19),'Rubber',.014)
for i in range(6):box('Speaker_Grille',(-.105,-.105,.095+i*.027),(.143,.006,.006),'EdgeMetal',.001)
box('Tuner_Scale',(.105,-.095,.215),(.135,.014,.055),'Paper',.004)
for x in [.06,.15]:cyl('Dial',(x,-.106,.12),.025,.025,'EdgeMetal',(0,1,0))
beam('Antenna',(.16,.03,.30),(.23,.03,.62),.009,'EdgeMetal')
for x in [-.13,.13]:beam('Handle_Support',(x,0,.29),(x,0,.37),.019,'Steel')
beam('Carry_Handle',(-.13,0,.37),(.13,0,.37),.022,'Steel');collider((0,0,.16),(.43,.18,.32))

asset('WoodCrate01')
for y in [-.24,.24]:
 for i in range(3):box('Crate_Board',(0,y,.08+i*.13),(.64,.042,.122),['Wood','WoodLight','Wood'][i],.006)
for x in [-.30,.30]:
 for i in range(3):box('End_Board',(x,0,.08+i*.13),(.04,.46,.122),'WoodDark',.006)
for x in [-.235,.235]:
 for y in [-.266,.266]:box('Crate_Brace',(x,y,.205),(.055,.025,.40),'WoodLight',.004)
box('Crate_Bottom',(0,0,.029),(.6,.46,.058),'WoodDark',.004)
for i in range(4):box('Lid_Plank',(-.24+i*.16,0,.423),(.153,.53,.044),'Wood',.006)
collider((0,0,.225),(.66,.56,.45))

asset('Jerrycan01')
box('Can_Body',(0,0,.235),(.31,.19,.45),'Olive',.026)
box('Top_Neck',(-.025,0,.48),(.25,.16,.08),'OliveLight',.015)
for x in [-.105,.05]:beam('Handle_Support',(x,0,.5),(x,0,.61),.036,'OliveDark')
beam('Can_Handle',(-.105,0,.61),(.05,0,.61),.04,'OliveDark')
cyl('Can_Cap',(.10,0,.52),.038,.035,'Steel')
beam('Stamped_Rib_A',(-.09,-.099,.10),(.09,-.099,.35),.015,'OliveLight')
beam('Stamped_Rib_B',(.09,-.10,.10),(-.09,-.10,.35),.015,'OliveLight')
collider((0,0,.27),(.31,.20,.54))

asset('MedicalBox01')
box('Medical_Case',(0,0,.17),(.49,.27,.33),'Linen',.024)
box('Medical_Lid',(0,0,.35),(.50,.28,.055),'Canvas',.008)
box('Cross_Horizontal',(0,-.142,.19),(.17,.009,.045),'Red',.003)
box('Cross_Vertical',(0,-.145,.19),(.045,.009,.17),'Red',.003)
for x in [-.13,.13]:box('Case_Clasp',(x,-.15,.30),(.028,.018,.07),'Steel',.004)
beam('Case_Handle',(-.10,0,.41),(.10,0,.41),.025,'Steel')
for x in [-.1,.1]:beam('Case_HandleEnd',(x,0,.36),(x,0,.41),.02,'Steel')
collider((0,0,.20),(.51,.30,.40))

asset('Rug01')
box('Worn_Mat',(0,0,.008),(1.18,.74,.016),'Blanket',.01)
for y in [-.33,.33]:box('Border_Stitch',(0,y,.017),(1.10,.015,.003),'Canvas',.001)

# Reference-specific furnishings. Keep the source Safehouse01 kit untouched.
def replace_asset(name):
 a=assets.pop(name)
 for o in list(a['col'].objects):bpy.data.objects.remove(o,do_unlink=True)
 bpy.data.collections.remove(a['col'])
 return asset(name)

palette.update({'Wood':(.14,.075,.032),'WoodLight':(.21,.125,.060),
 'WoodDark':(.085,.043,.019),'Grey':(.125,.135,.143),
 'Canvas':(.23,.215,.17),'Blanket':(.11,.13,.075),'Linen':(.60,.54,.42),'RugFabric':(.12,.075,.041)})
for n,c in palette.items():
 if n not in mats:mats[n]=mats['Canvas'].copy();mats[n].name='Hideout_'+n
 mats[n].diffuse_color=(*c,1);mats[n].node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*c,1)

replace_asset('Cot01')
for x in [-.61,.61]:
 for y in [-.98,.98]:box('Oak_Bedpost',(x,y,.40),(.105,.105,.8),'WoodLight',.018)
for x in [-.6,.6]:box('Thick_Side_Rail',(x,0,.34),(.085,2.02,.19),'Wood',.013)
for y in [-.97,.97]:
 for z in [.30,.53,.74]:box('End_Board',(0,y,z),(1.15,.07,.15),'Wood',.01)
for i in range(9):box('Bed_Slat',(0,-.82+i*.2,.35),(1.15,.15,.04),'WoodDark',.003)
ringmesh('Padded_Mattress',[(.565,.94,.38,0,0),(.585,.955,.48,0,0),(.56,.93,.55,0,0)],'Linen')
ringmesh('Soft_Pillow',[(.41,.22,.55,0,.66),(.45,.24,.62,0,.66),(.37,.19,.70,-.02,.65)],'Linen')
# Rounded folds and overhang read at the game camera, not only in a close-up.
ringmesh('Olive_Quilt',[(.607,.655,.48,0,-.27),(.603,.657,.565,0,-.27),(.56,.63,.602,0,-.27)],'Blanket')
for x in [-.52,.52]:box('Quilt_Edge_Seam',(x,-.28,.605),(.018,1.22,.006),'Canvas',.002)
box('Mended_Patch',(.29,-.53,.608),(.18,.20,.007),'OliveDark',.007,rot=(0,0,.17))
for i in range(6):
 for x in [.195,.39]:box('Repair_Stitch',(x,-.61+i*.031,.615),(.031,.007,.004),'Canvas',.001,rot=(0,0,.15))
collider((0,0,.35),(1.3,2.15,.70))

def table(name,w=1.35,d=.65,h=.83):
 asset(name)
 for x in [-w/2+.09,w/2-.09]:
  for y in [-d/2+.06,d/2-.06]:box('Timber_Leg',(x,y,h/2),(.085,.085,h),'Wood',.012)
 for z in [.21,h]:
  for i in range(4):box('Plank',(0,-d/2+d/8+i*d/4,z),(w,d/4-.008,.065),'WoodLight' if i==1 else 'Wood',.009)
 for y in [-d/2+.04,d/2-.04]:box('Apron',(0,y,h-.12),(w-.08,.05,.15),'WoodDark',.008)
 collider((0,0,h*.5),(w,d,h))

replace_asset('Shelf01')
for x in [-.67,.67]:
 for y in [-.27,.27]:box('Wooden_Upright',(x,y,.72),(.075,.075,1.44),'Wood',.012)
for z in [.18,.67,1.19]:
 for i in range(3):box('Wooden_Shelf',(0,-.22+i*.22,z),(1.43,.21,.065),'WoodLight' if i==1 else 'Wood',.009)
 box('Back_Rail',(0,.30,z+.17),(1.38,.045,.20),'WoodDark',.008)
collider((0,0,.72),(1.46,.67,1.44))

replace_asset('Stool01')
for x in [-.16,.16]:
 for y in [-.16,.16]:beam('Splayed_Wood_Leg',(x*1.25,y*1.25,.02),(x,y,.44),.063,'Wood')
for i in range(3):box('Seat_Board',(-.16+i*.16,0,.46),(.154,.47,.07),'WoodLight',.015)
for y in [-.17,.17]:beam('Stool_Brace',(-.17,y,.18),(.17,y,.18),.034,'WoodDark')
collider((0,0,.24),(.5,.5,.49))

# Workbench: wooden legs, recognizable loose tools, retain the existing vise.
current=assets['Workbench01'];parent=current['root']
for o in list(current['col'].objects):
 if o.name.startswith(('Rack_','Tool_','Repair_Sheet')):bpy.data.objects.remove(o,do_unlink=True)
 elif o.type=='MESH' and o.name.startswith(('Leg','Apron')):o.data.materials.clear();o.data.materials.append(mats['Wood'])
def wrench(x,y,angle=0):
 beam('Wrench_Handle',(x,y-.12,.956),(x,y+.10,.956),.025,'EdgeMetal')
 for dx in [-.035,.035]:box('Open_Jaw',(x+dx,y+.13,.956),(.022,.07,.023),'EdgeMetal',.004)
 beam('Jaw_Bridge',(x-.035,y+.1,.956),(x+.035,y+.1,.956),.027,'EdgeMetal')
 cyl('Ring_End',(x,y-.13,.956),.040,.018,'EdgeMetal',verts=16)
 cyl('Ring_Hole',(x,y-.13,.967),.021,.003,'Steel',verts=12)
wrench(-.43,0);wrench(-.13,.08)
beam('Hammer_Wood_Handle',(.12,-.2,.955),(.30,.02,.955),.035,'WoodLight')
box('Hammer_Head',(.30,.02,.961),(.18,.06,.065),'EdgeMetal',.008,rot=(0,0,-.65))
beam('Driver_Shaft',(-.5,-.24,.95),(-.27,-.24,.95),.012,'EdgeMetal')
box('Driver_Grip',(-.56,-.24,.96),(.12,.041,.04),'OliveDark',.009)
box('Tool_Tray',(.33,.22,.96),(.25,.21,.055),'WoodDark',.007)

table('CookingStation02',1.45,.72)
box('Camp_Stove',(-.38,0,.92),(.48,.49,.13),'OliveDark',.018)
cyl('Burner',(-.38,0,1.0),.18,.035,'Steel',verts=24)
cyl('Cooking_Pot',(-.38,0,1.12),.165,.24,'Grey',verts=24)
cyl('Pot_Lid',(-.38,0,1.248),.176,.025,'EdgeMetal',verts=24)
cyl('Lid_Knob',(-.38,0,1.278),.035,.036,'Rubber')
for x in [-.60,-.16]:box('Pot_Handle',(x,0,1.15),(.10,.055,.035),'Steel',.01)
box('Chopping_Board',(.40,-.10,.88),(.36,.27,.028),'WoodLight',.015)
box('Knife_Blade',(.39,-.10,.902),(.16,.027,.006),'EdgeMetal',.001,rot=(0,0,.35))
beam('Knife_Handle',(.48,-.075,.91),(.57,-.04,.91),.026,'WoodDark')
cyl('Mug',(.12,.12,.955),.070,.19,'Linen',verts=20)
cyl('Mug_Coffee',(.12,.12,1.052),.058,.003,'WoodDark',verts=20)
beam('Mug_Handle',(.185,.12,.91),(.185,.12,1.0),.022,'Linen')

table('MedicalStation02',1.25,.72)
for x,y in [(.12,.12),(.39,.18)]:
 cyl('Medicine_Bottle',(x,y,.975),.070,.26,'WoodLight',verts=20)
 cyl('Bottle_Label',(x,y,.98),.072,.13,'Linen',verts=20)
 cyl('Bottle_Cap',(x,y,1.12),.051,.04,'Grey',verts=16)
for x in [-.4,-.10,.25]:
 cyl('Rolled_Bandage',(x,-.20,.94),.082,.18,'Linen',axis=(0,1,0),verts=20)
 cyl('Bandage_Core',(x,-.296,.94),.031,.005,'Canvas',axis=(0,1,0),verts=16)

table('RadioTable02',.8,.64,.72)
table('BedsideTable02',.48,.48,.57)
box('Drawer', (0,-.04,.43),(.36,.42,.17),'WoodLight',.009)
cyl('Drawer_Knob',(0,-.27,.43),.018,.025,'EdgeMetal',(0,1,0))
cyl('Lantern_Base',(0,0,.655),.105,.10,'Steel',verts=20)
cyl('Lantern_Glass',(0,0,.82),.075,.24,'LampGlow',verts=20)
cyl('Lantern_Cap',(0,0,.96),.10,.05,'Steel',verts=20)
for x in [-.085,.085]:beam('Lantern_Guard',(x,0,.68),(x,0,.96),.014,'EdgeMetal')
beam('Carry_Bail',(-.085,0,.96),(-.07,0,1.08),.013,'Steel')
beam('Carry_Bail',(-.07,0,1.08),(.07,0,1.08),.013,'Steel')
beam('Carry_Bail',(.07,0,1.08),(.085,0,.96),.013,'Steel')

asset('LockerPair02')
for x,mat in [(-.30,'Grey'),(.30,'Linen')]:
 box('Locker_Body',(x,0,.65),(.55,.52,1.3),mat,.025)
 box('Door_Inset',(x,-.27,.66),(.47,.02,1.18),'Steel' if mat=='Grey' else 'Canvas',.015)
 box('Door_Panel',(x,-.285,.66),(.435,.021,1.14),mat,.014)
 for z in [.95,1.01,1.07]:box('Vent',(x,-.303,z),(.20,.008,.016),'Steel',.002)
 beam('Door_Handle',(x+.13,-.335,.5),(x+.13,-.335,.66),.022,'EdgeMetal')
 for z in [.18,1.08]:box('Hinge',(x-.22,-.30,z),(.035,.025,.085),'EdgeMetal',.004)
collider((0,0,.65),(1.15,.60,1.3))

asset('SupplyCase02')
box('Military_Case',(0,0,.16),(.55,.36,.30),'OliveDark',.025)
box('Case_Lid',(0,0,.32),(.57,.38,.075),'Olive',.015)
for x in [-.17,.17]:box('Brass_Latch',(x,-.204,.28),(.046,.024,.085),'Wear',.005)
beam('Carry_Handle',(-.07,-.22,.20),(.07,-.22,.20),.022,'Steel')
collider((0,0,.18),(.58,.4,.36))
asset('Tin02')
cyl('Tin_Can',(0,0,.095),.067,.19,'Grey',verts=20)
for z in [.012,.18]:cyl('Rim',(0,0,z),.071,.012,'EdgeMetal',verts=20)
cyl('Paper_Label',(0,0,.093),.068,.11,'Canvas',verts=20)

replace_asset('Rug01')
box('Woven_Rug',(0,0,.012),(1.9,2.45,.022),'RugFabric',.005)
for x in [-.90,.90]:box('Woven_Border',(x,0,.025),(.055,2.39,.005),'Canvas',.002)
for y in [-1.17,1.17]:box('Woven_Border',(0,y,.026),(1.85,.055,.005),'Canvas',.002)
for x in [-.9+i*.06 for i in range(31)]:
 for y in [-1.25,1.25]:beam('Rug_Fringe',(x,y-.03,.012),(x,y+.03,.012),.009,'Canvas')

current=assets['MedicalBox01'];parent=current['root']
box('Lid_Cross_Horizontal',(0,-.035,.381),(.21,.055,.008),'Red',.003)
box('Lid_Cross_Vertical',(0,-.035,.386),(.055,.19,.008),'Red',.003)

asset('RoomShell02')
box('Foundation',(0,0,-.13),(6.12,6.12,.22),'Steel',.02)
collider((0,0,-.11),(6,6,.20))
# Real staggered plank ends; grain runs along each board's long axis.
for row in range(24):
 y=-2.875+row*.25
 cuts=sorted(set([-3,3]+[v for v in [-3+(row%3)*.61+k*1.83 for k in range(5)] if -2.99<v<2.99]))
 for a,b in zip(cuts,cuts[1:]):
  box('Staggered_Floorboard',((a+b)/2,y,-.02),(b-a-.009,.243,.035),rng.choice(['Wood','Wood','WoodDark','WoodLight']),.004)
  for xx in [a+.045,b-.045]:
   for yy in [y-.075,y+.075]:cyl('Recessed_Nail',(xx,yy,-.001),.007,.003,'Steel',verts=6)
# Rear wall retained for silhouette; other walls cut low for unobstructed facilities.
for x in [-2.9+i*.145 for i in range(41)]:
 box('Corrugated_Back_Sheet',(x,3,1.05),(.146,.065,2.10),'Grey',.004)
 box('Raised_Vertical_Rib',(x,2.947,1.05),(.060,.06,2.08),'Grey',.009)
 for z in [.11,1.99]:cyl('Wall_Rivet',(x,2.91,z),.012,.012,'EdgeMetal',(0,1,0),8)
for z in [.055,2.12]:box('Rear_Frame',(0,3,z),(6.13,.16,.13),'Steel',.012)
for x in [-3,3]:
 box('Cutaway_Side',(x,0,.115),(.15,6,.23),'Grey',.009)
 box('Cutaway_Cap',(x,0,.24),(.19,6,.035),'EdgeMetal',.005)
 collider((x,0,1.2),(.16,6,2.4))
for x in [-1.87,1.87]:box('Cutaway_Front',(x,-3,.14),(2.26,.16,.28),'Grey',.01)
for x in [-3,3]:
 for y in [-3,3]:
  h=2.21 if y>0 else .30
  box('Corner_Post',(x,y,h/2),(.21,.21,h),'Steel',.017)
  box('Corner_Plate',(x,y,h),(.27,.27,.06),'EdgeMetal',.012)
  cyl('Corner_Bolt',(x,y,h+.033),.035,.007,'Steel',verts=6)
for x in [-.36,.36]:
 box('Entry_Door_Cutaway',(x,-3,.19),(.70,.16,.38),'OliveDark',.013)
 box('Door_Top_Edge',(x,-3,.39),(.71,.19,.035),'Olive',.005)
 for xx in [x-.25,x+.25]:box('Door_Strap',(xx,-3.10,.21),(.042,.023,.25),'Wear',.004)
box('Threshold',(0,-2.96,.017),(1.48,.29,.035),'EdgeMetal',.005)
collider((0,3,1.2),(6,.16,2.4))
for x in [-1.88,1.88]:collider((x,-3,1.2),(2.24,.16,2.4))

# Assembly positions are in Blender coordinates: +Y is the rear wall.
placements=[]
def p(name,x,y,z=0,rz=0,group='Furniture',key=None,stand=None,scale=1):
 placements.append(dict(asset=name,position=[x,y,z],rotation_z=rz,group=group,scale=scale,key=key,stand=stand))
p('RoomShell02',0,0,group='Architecture')
p('Shelf01',-2.03,2.50)
p('Cot01',.18,2.06,rz=90,key='bed',stand=[.1,1.08,0])
p('BedsideTable02',-1.21,2.32)
p('MapBoard01',2.04,2.88,1.48,key='dispatch',stand=[1.73,1.91,0])
p('LockerPair02',-2.32,1.28)
p('Workbench01',2.45,.75,rz=-90,key='workbench',stand=[1.62,.75,0])
p('Stool01',1.38,.09,rz=-14,key='idle',stand=[.75,.02,0])
p('MedicalStation02',2.43,-1.46,rz=-90,key='medical',stand=[1.6,-1.46,0])
p('MedicalBox01',2.50,-1.14,.865,rz=-90,group='SmallProps',scale=.76)
p('RadioTable02',-2.45,-.16,rz=90,key='radio',stand=[-1.66,-.16,0])
p('Radio01',-2.45,-.16,.757,rz=45,group='SmallProps',scale=1.2)
p('Generator01',-2.42,-1.10,rz=90,key='generator',stand=[-1.66,-1.10,0])
p('CookingStation02',-2.04,-2.47,rz=180,key='cooking',stand=[-1.75,-1.75,0])
p('Rug01',0,-.73,group='Dressing')
p('StorageChest01',-1.10,.65,rz=180,key='stash',stand=[-1.05,-.04,0])
for x,y,z in [(-2.40,2.49,.218),(-1.68,2.5,.218),(-2.05,2.50,1.228),(.62,2.16,0),(2.42,-1.56,.25)]:p('SupplyCase02',x,y,z,group='Dressing')
for x,y,z in [(-2.38,2.5,.708),(-2.17,2.5,.708),(-1.96,2.5,.708),(-1.67,2.51,1.23),(-2.43,-2.4,.26),(-2.19,-2.4,.26),(2.44,-1.02,.25),(2.45,-1.26,.25)]:p('Tin02',x,y,z,group='Dressing')
for x,y,z,rz in [(2.45,-2.47,0,-4),(-.40,2.20,0,3),(-2,-2.46,.25,0)]:p('WoodCrate01',x,y,z,rz,group='Dressing',scale=.75)

# Only used assets are exported. Each has metric UV0; UV1 is a unique lightmap unwrap.
used={v['asset'] for v in placements}-{'StorageChest01'}
texture_family={n:('Wood' if n.startswith('Wood') else 'Cloth' if n in ['Canvas','Blanket','Linen'] else 'Steel' if n in ['Steel','EdgeMetal'] else 'Plaster' if n in ['Concrete','Paper'] else 'PaintedMetal') for n in palette}
texture_family['WoodDark']='Wood'
texture_family['RugFabric']='Cloth'
for name,a in assets.items():
 if name not in used:continue
 for o in [o for o in a['col'].objects if o.type=='MESH']:
  me=o.data;uv=me.uv_layers.new(name='Surface_MetreUV')
  # Dominant-plane projection avoids smart-project stretching and arbitrary texel scales.
  offset=Vector((rng.random(),rng.random()))
  dim=[max(v.co[i] for v in me.vertices)-min(v.co[i] for v in me.vertices) for i in range(3)]
  for face in me.polygons:
   major=max(range(3),key=lambda i:abs(face.normal[i]));axes=[i for i in range(3) if i!=major]
   axes.sort(key=lambda i:dim[i],reverse=True)
   for li in face.loop_indices:
    v=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(v[axes[0]]+offset.x,v[axes[1]]+offset.y)
  o['uv_scale']='1 UV unit per metre, long axis follows grain'

stats={}
export_collection=bpy.data.collections.new('03_Unity_Batched_Exports');sc.collection.children.link(export_collection)
for name in sorted(used):
 a=assets[name];copies=[]
 for o in a['col'].objects:
  if o.type!='MESH':continue
  cp=o.copy();cp.data=o.data.copy();export_collection.objects.link(cp);cp.parent=None;cp.matrix_world=o.matrix_world.copy();copies.append(cp)
 bpy.ops.object.select_all(action='DESELECT')
 for o in copies:o.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();joined=bpy.context.object;joined.name=name
 # Smooth bevel corners with weighted face normals while keeping intentional hard surfaces.
 for poly in joined.data.polygons:poly.use_smooth=True
 mod=joined.modifiers.new('Weighted_Face_Normals','WEIGHTED_NORMAL');mod.keep_sharp=True;mod.weight=50
 bpy.ops.object.modifier_apply(modifier=mod.name)
 bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 stats[name]={'meshes':1,'triangles':sum(len(f.vertices)-2 for f in joined.data.polygons),'uv_mapped':len(joined.data.uv_layers)>0,'colliders':a['colliders']}
 joined.hide_render=True;joined.hide_set(True)

# Editable Blender assembly mirrors the prefab. Source library remains preserved.
groups={n:empty(n,layout) for n in ['Architecture','Furniture','SmallProps','Dressing']}
for spec in placements:
 if spec['asset']=='StorageChest01':continue
 a=assets[spec['asset']];mapping={}
 for o in a['col'].objects:
  cp=o.copy();layout.objects.link(cp);mapping[o]=cp
 for o,cp in mapping.items():cp.parent=mapping.get(o.parent,groups[spec['group']])
 r=mapping[a['root']];r.location=spec['position'];r.rotation_euler.z=math.radians(spec['rotation_z']);r.scale=(spec['scale'],)*3
library.hide_render=True;library.hide_viewport=True;export_collection.hide_render=True;export_collection.hide_viewport=True
(OUT/'KitManifest.json').write_text(json.dumps({'units':'metres','room_size':[6,6,2.4],'assets':stats,'placements':placements,'palette':{('Hideout_'+n):{'color':list(c),'family':texture_family[n],'metallic':.7 if n in ['Steel','EdgeMetal'] else .15,'smoothness':.25,'emission':1.5 if n=='LampGlow' else 0} for n,c in palette.items()}},indent=2),encoding='utf8')
sys.path.insert(0,str(Path(__file__).resolve().parent))
from hideout02_blender_materials import bind_surfaces,append_approved_chest
manifest=json.loads((OUT/'KitManifest.json').read_text(encoding='utf8'))
bind_surfaces(OUT,manifest);append_approved_chest(OUT,manifest)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/Hideout02.blend'))
print('HIDEOUT02_COMPLETE',json.dumps({'assets':len(stats),'triangles':sum(a['triangles'] for a in stats.values()),'placements':len(placements)}))


