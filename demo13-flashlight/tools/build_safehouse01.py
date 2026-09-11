"""Editable safehouse kit. Metres, Z up; every asset and every moving part separate."""
import bpy, bmesh, math, json, random, sys
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Art/Environments/Safehouse01'
for p in ['Models','BlenderSource~','Previews']: (OUT/p).mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0
sc=bpy.context.scene;sc.name='Safehouse01_Assembly';sc.unit_settings.system='METRIC'
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
 m=bpy.data.materials.new('Safehouse_'+n);m.diffuse_color=(*c,1);m.use_nodes=True
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

# UV unwrap once per original mesh; layout copies share meshes.
stats={}
for name,a in assets.items():
 meshes=[o for o in a['col'].objects if o.type=='MESH']
 for o in meshes:
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(70),island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
  o['editable_part']=True
 bpy.ops.object.select_all(action='DESELECT')
 for o in a['col'].objects:o.select_set(True)
 bpy.context.view_layer.objects.active=a['root']
 bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/(name+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 stats[name]={'meshes':len(meshes),'triangles':sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),'uv_mapped':all(len(o.data.uv_layers)>0 for o in meshes),'colliders':a['colliders']}

groups={n:empty(n,layout) for n in ['Floor','VisibleWalls','Furniture','SmallProps','HiddenWalls','Roof','Outside']}
def place(name,loc,rz=0,group='Furniture',scale=1):
 a=assets[name];mapping={}
 for o in a['col'].objects:
  cp=o.copy();layout.objects.link(cp);mapping[o]=cp
 for o,cp in mapping.items():cp.parent=mapping.get(o.parent,groups[group])
 r=mapping[a['root']];r.location=loc;r.rotation_euler.z=math.radians(rz);r.scale=(scale,)*3
 placements.append({'asset':name,'position':loc,'rotation_z':rz,'group':group,'scale':scale})
 return r
for x in range(6):
 for y in range(4):
  place('FloorTile01',(x-2.5,y-1.5,0),group='Floor')
  place('RoofTile01',(x-2.5,y-1.5,2.67),group='Roof')
for x in range(6):
 place('WallWindow01' if x in [0,4] else 'WallSolid01',(x-2.5,2,0),group='VisibleWalls')
 place('WallDoorFrame01' if x==4 else 'WallSolid01',(x-2.5,-2,0),180,'HiddenWalls')
for y in range(4):
 place('WallSolid01',(-3,y-1.5,0),90,'VisibleWalls')
 place('WallWindow01' if y==2 else 'WallSolid01',(3,y-1.5,0),-90,'HiddenWalls')
for x in [-3,3]:
 for y in [-2,2]:place('CornerPost01',(x,y,0),group='VisibleWalls' if y==2 or x==-3 else 'HiddenWalls')
place('Door01',(1.5,-2,0),180,'HiddenWalls')
place('EntryStep01',(1.5,-2.4,-.19),group='Outside')
place('Workbench01',(.25,1.50,0));place('Cot01',(-2.15,.37,0))
place('Shelf01',(2.13,1.65,0));place('Stool01',(.16,.56,0),-12)
place('FloorLamp01',(-1.30,.99,0),-25)
place('MapBoard01',(.24,1.89,1.94),group='Furniture')
place('WallLamp01',(-2.91,.95,1.98),90,'Furniture')
place('Generator01',(2.43,-.72,0),-90)
place('Radio01',(-.27,1.46,.934),group='SmallProps')
place('MedicalBox01',(2.10,1.63,1.202),group='SmallProps')
place('WoodCrate01',(2.10,1.65,.684),group='SmallProps')
place('WoodCrate01',(-2.28,-1.21,0),-6,'SmallProps')
place('Jerrycan01',(1.78,1.65,.153),group='SmallProps')
place('Jerrycan01',(2.16,1.65,.153),group='SmallProps')
place('Jerrycan01',(2.15,-1.30,0),-14,'SmallProps')
place('Rug01',(-.80,-.55,.002),-3,'Furniture')
# Reuse the approved chest, including hierarchy and animation source, without altering it.
chestpath=ROOT/'Assets/Art/Props/StorageChest01/BlenderSource~/StorageChest01.blend'
with bpy.data.libraries.load(str(chestpath),link=False) as (src,dst):dst.collections=[n for n in src.collections if n.startswith('StorageChest01 -')]
chestcol=dst.collections[0];layout.children.link(chestcol)
chestroot=next(o for o in chestcol.objects if o.name=='StorageChest01')
chestroot.parent=groups['Furniture'];chestroot.location=(-.73,-1.32,0);chestroot.rotation_euler.z=math.radians(-4)
for o in chestcol.objects:o.animation_data_clear()
placements.append({'asset':'StorageChest01','position':[-.73,-1.32,0],'rotation_z':-4,'group':'Furniture','scale':1})
for o in layout.objects:
 if o.parent and o.parent.name in ['HiddenWalls','Roof']:o.hide_render=True
# Hide entire root trees for the cutaway. Per-object flags are explicit for Blender.
for g in ['HiddenWalls','Roof']:
 for o in groups[g].children_recursive:o.hide_render=True;o.hide_set(True)
library.hide_render=True;library.hide_viewport=True

def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
def light(n,loc,power,color,size=2,kind='AREA'):
 d=bpy.data.lights.new(n,kind);d.energy=power;d.color=color
 if kind=='AREA':d.shape='DISK';d.size=size
 else:d.shadow_soft_size=size
 o=bpy.data.objects.new(n,d);studio.objects.link(o);o.location=loc;aim(o,(0,0,0));return o
light('Warm_Practical',(-1.22,.84,1.54),28,(1,.72,.40),.30,'POINT')
light('Wall_Practical',(-2.69,.95,1.97),18,(1,.68,.36),.22,'POINT')
light('Soft_Daylight',(1,-4,7),1250,(.83,.89,1),7)
light('Cool_Window',(-1,4,5),800,(.66,.78,1),5)
light('Warm_Fill',(-4,-1,4),500,(1,.82,.59),4)
sc.world=bpy.data.worlds.new('Safehouse_Studio');sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.12,.15,.17,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.4
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.201));floor=bpy.context.object
for c in list(floor.users_collection):c.objects.unlink(floor)
studio.objects.link(floor);floor.name='Review_Ground';floor.data.materials.append(mats['Concrete'])
cd=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',cd);studio.objects.link(cam);sc.camera=cam;cd.type='ORTHO'
sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=True
sc.render.resolution_x=1500;sc.render.resolution_y=1150;sc.render.resolution_percentage=100;sc.view_settings.view_transform='AgX'
views=[('Overview',(8,-11,9),(0,0,1),9.3),('Interior',(6,-10,7),(-.4,.6,1.0),6.7),('TopDown',(0,-.01,15),(0,0,0),7.1)]
for name,pos,target,scale in views:
 cam.location=pos;aim(cam,target);cd.ortho_scale=scale;sc.render.filepath=str(OUT/'Previews'/(name+'.png'))
 if '--no-render' not in sys.argv:bpy.ops.render.render(write_still=True)
cam.location=views[0][1];aim(cam,views[0][2]);cd.ortho_scale=views[0][3]
# Persist assembly in Blender space. Unity's FBX import maps X,Y,Z to -X,Z,-Y.
(OUT/'KitManifest.json').write_text(json.dumps({'units':'metres','room_size':[6,4,2.67],'assets':stats,'placements':placements,'palette':{('Safehouse_'+n):{'color':list(c),'metallic':.4 if n in ['Steel','EdgeMetal'] else 0,'smoothness':.3 if n in ['Steel','EdgeMetal'] else .14,'emission':2 if n=='LampGlow' else 0} for n,c in palette.items()}},indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/Safehouse01.blend'))
print('SAFEHOUSE_COMPLETE',json.dumps({'assets':len(assets),'triangles':sum(a['triangles'] for a in stats.values()),'placements':len(placements)}))
