# Geometry source used by build_village01.py; axes and helpers match Safehouse01.
asset('StreetLamp01')
box('Concrete_Foot',(0,0,.10),(.42,.42,.20),'Concrete',.035)
cyl('Post',(0,0,1.8),.055,3.4,'Steel',verts=8)
beam('Bent_Arm',(0,0,3.48),(0,-.55,3.68),.07,'Steel')
beam('Lamp_Hanger',(0,-.55,3.68),(0,-.55,3.38),.04,'EdgeMetal')
cyl('Shade',(0,-.55,3.35),.24,.10,'OliveDark',verts=8)
cyl('Warm_Lens',(0,-.55,3.25),.12,.14,'LampGlow',verts=8)
cyl('Lamp_Base',(0,-.55,3.15),.15,.045,'Steel',verts=8)
for x in [-.12,.12]:beam('Lens_Guard',(x,-.55,3.16),(x,-.55,3.32),.018,'Steel')
box('Service_Box',(0,.07,1.12),(.17,.14,.29),'Olive',.01)
collider((0,0,1.75),(.22,.22,3.5))

asset('Bench01')
for x in [-.63,.63]:
 for y in [-.2,.2]:beam('Leg',(x,y,.025),(x,y*.8,.48),.065,'Steel')
 beam('Back_Post',(x,.2,.25),(x,.30,1.02),.055,'Steel')
 beam('Seat_Rail',(x,-.27,.42),(x,.26,.42),.06,'Steel')
for i in range(4):box('Seat_Plank',(0,-.21+i*.14,.48),(1.7,.13,.065),['Wood','WoodLight','Wood','WoodDark'][i],.008)
for z in [.72,.91]:box('Back_Plank',(0,.26,z),(1.7,.065,.15),'Wood',.012,rot=(math.radians(-8),0,0))
collider((0,0,.49),(1.72,.65,.98))

asset('OilDrum01')
cyl('Drum',(0,0,.45),.31,.88,'Olive',verts=12)
for z in [.035,.29,.64,.885]:cyl('Rolled_Rim',(0,0,z),.323,.036,'Steel',verts=12)
cyl('Lid',(0,0,.898),.294,.018,'OliveLight',verts=12)
cyl('Bung',(.15,0,.914),.043,.02,'Steel',verts=8)
box('Faded_Label',(0,-.309,.49),(.17,.009,.13),'Linen',.002)
scuff((-.1,-.30,.17),(.12,.09));collider((0,0,.46),(.65,.65,.92))

asset('Pallet01')
for x in [-.48,0,.48]:box('Runner',(x,0,.08),(.12,.9,.16),'WoodDark',.008)
for i in range(6):box('Deck_Plank',(-.50+i*.20,0,.19),(.18,.96,.06),['Wood','WoodLight','Wood','Wood','WoodDark','Wood'][i],.008)
collider((0,0,.11),(1.18,.96,.22))

asset('Sandbags01')
for level in range(3):
 for i in range(3 if level<2 else 2):
  x=(i-1)*.60+(.28 if level==2 else .06*(level%2));y=.015*(i%2);z=level*.22
  ringmesh('Canvas_Bag',[(.26,.24,z+.02,x,y),(.32,.28,z+.11,x,y),(.27,.24,z+.235,x+.015,y)],'Canvas' if (i+level)%2 else 'OliveLight')
  box('Bag_Seam',(x,-.27,z+.115),(.47,.013,.018),'WoodDark',.003)
collider((0,0,.34),(1.88,.57,.68))

asset('LowFence01')
for x in [-.94,.94]:box('Fence_Post',(x,0,.61),(.11,.13,1.22),'Steel',.015)
for z in [.25,.98]:box('Fence_Rail',(0,0,z),(2,.06,.065),'EdgeMetal',.008)
for i in range(9):box('Picket',(-.8+i*.2,0,.65),(.035,.04,.78),'Steel',.004)
box('Repair_Plate',(.52,-.04,.63),(.35,.025,.48),'Olive',.008)
collider((0,0,.6),(2,.15,1.2))

asset('Window01')
box('Recess',(0,0,0),(1.50,.065,1.38),'Steel',.012)
box('Dark_Glass',(0,-.047,0),(1.33,.025,1.21),'Glass',.004)
for x in [-.71,0,.71]:box('Window_Upright',(x,-.075,0),(.055,.055,1.38),'OliveLight',.006)
for z in [-.65,.12,.65]:box('Window_Rail',(0,-.078,z),(1.48,.055,.055),'OliveLight',.006)
box('Deep_Sill',(0,-.16,-.73),(1.64,.40,.13),'Concrete',.018)
beam('Boarded_Corner',(.2,-.125,-.56),(.68,-.125,-.10),.11,'WoodDark')

asset('Awning01')
for x in [-1.65,1.65]:
 beam('Wall_Bracket',(x,0,-.50),(x,-1.25,-.22),.045,'Steel')
 beam('Roof_Rib',(x,0,0),(x,-1.35,-.23),.045,'Steel')
for i in range(10):
 x=-1.8+i*.4
 box('Canvas_Roof',(x,-.66,-.115),(.395,1.38,.045),'Olive' if i%3 else 'Canvas',.004,rot=(math.radians(10),0,0))
 box('Canvas_Valance',(x,-1.34,-.32),(.395,.04,.19),'OliveDark' if i%3 else 'Canvas',.01)
beam('Front_Rail',(-1.99,-1.34,-.24),(1.99,-1.34,-.24),.05,'Steel')

asset('Shutter01')
box('Shutter_Back',(0,0,0),(1.46,.06,1.85),'Steel',.009)
for i in range(12):box('Shutter_Slat',(0,-.044,-.84+i*.15),(1.34,.055,.14),'OliveDark' if i%4 else 'Olive',.008)
for x in [-.72,.72]:box('Side_Track',(x,-.07,0),(.065,.09,1.93),'EdgeMetal',.005)
box('Pull_Handle',(0,-.10,-.62),(.22,.035,.04),'Steel',.004)

asset('NoticeBoard01')
for x in [-1.05,1.05]:box('Board_Post',(x,0,1.10),(.13,.16,2.2),'WoodDark',.012)
box('Board_Back',(0,0,1.48),(2.30,.10,1.40),'OliveDark',.015)
for x in [-1.18,1.18]:box('Side_Frame',(x,-.025,1.48),(.11,.14,1.5),'Wood',.012)
for z in [.74,2.22]:box('Board_Frame',(0,-.025,z),(2.46,.14,.10),'Wood',.012)
box('Rain_Hood',(0,-.12,2.31),(2.65,.59,.09),'Steel',.015,rot=(math.radians(8),0,0))
for x,z,w,h,ang in [(-.66,1.67,.53,.59,-4),(-.03,1.54,.56,.74,3),(.61,1.80,.47,.38,-6),(.59,1.19,.52,.35,2)]:
 box('Pinned_Paper',(x,-.062,z),(w,.008,h),'Paper' if x<.1 else 'Linen',.002,rot=(0,math.radians(ang),0))
 for j in range(3):box('Paper_Mark',(x,-.069,z+.08-j*.08),(w*.68,.003,.012),'MapLine',.001)
 cyl('Pin',(x,-.073,z+h*.40),.018,.015,'Red',(0,1,0),6)
collider((0,0,1.17),(2.5,.30,2.34))

asset('PavingSlab01')
box('Weathered_Slab',(0,0,-.012),(1.98,1.98,.09),'Concrete',.023)
mesh('Broken_Corner',[(-.99,-.99,.034),(-.69,-.99,.034),(-.99,-.75,.034)],[(0,1,2)],'Grey')
beam('Surface_Crack',(.67,.98,.034),(.42,.52,.034),.009,'Steel')

asset('Curb01')
for x in [-.5,.5]:box('Curb_Stone',(x,0,.05),(.985,.23,.16),'Grey',.018)

asset('Drain01')
box('Drain_Recess',(0,0,.008),(.52,1.02,.025),'Steel',.005)
for i in range(9):box('Grate_Slat',(0,-.44+i*.11,.03),(.47,.026,.032),'EdgeMetal',.004)
for x in [-.24,.24]:box('Frame',(x,0,.03),(.026,1,.032),'EdgeMetal',.003)

asset('Tires01')
for level in range(3):
 bpy.ops.mesh.primitive_torus_add(major_segments=12,minor_segments=6,location=(.03*(level%2),0,.14+level*.22),major_radius=.235,minor_radius=.105)
 o=bpy.context.object;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);finish(o,'Used_Tire','Rubber')
 for i in range(12):
  a=i*math.tau/12;box('Tread',(.333*math.cos(a)+.03*(level%2),.333*math.sin(a),.14+level*.22),(.055,.026,.12),'Steel',.006,rot=(0,0,a-math.pi/2))
collider((0,0,.37),(.72,.72,.74))

asset('Rubble01')
for i in range(9):
 x=rng.uniform(-.65,.65);y=rng.uniform(-.38,.38);h=rng.uniform(.08,.22)
 box('Broken_Masonry',(x,y,h*.5),(.18+rng.random()*.32,.14+rng.random()*.23,h),'Concrete' if i%3 else 'WoodDark',.04,rot=(0,0,rng.random()*3))
beam('Bent_Rebar',(-.6,.18,.09),(.6,.08,.12),.018,'Steel')

asset('Brazier01')
for x in [-.23,.23]:
 for y in [-.23,.23]:beam('Foot',(x,y,0),(x*.8,y*.8,.34),.045,'Steel')
cyl('Fire_Bowl',(0,0,.37),.40,.23,'Steel',verts=10)
cyl('Coal_Bed',(0,0,.494),.35,.022,'Rubber',verts=10)
for i in range(5):
 a=i*1.8;box('Charred_Log',(.14*math.cos(a),.14*math.sin(a),.54),(.39,.10,.08),'WoodDark',.017,rot=(0,0,a))
 cyl('Ember',(.15*math.sin(a),.15*math.cos(a),.54),.045,.04,'LampGlow',verts=6)
collider((0,0,.3),(.82,.82,.6))

asset('ShopSign01')
for x in [-1.05,1.05]:beam('Mount',(x,0,0),(x,-.22,0),.05,'Steel')
box('Sign_Frame',(0,-.20,0),(3.20,.12,.73),'WoodDark',.015)
box('Painted_Face',(0,-.269,0),(3.02,.025,.56),'OliveDark',.006)
for x in [-1.48,1.48]:
 for z in [-.22,.22]:cyl('Screw',(x,-.29,z),.024,.018,'EdgeMetal',(0,1,0),6)

asset('Bollard01')
box('Base',(0,0,.05),(.26,.26,.1),'Concrete',.016)
cyl('Bollard',(0,0,.46),.074,.82,'Steel',verts=8)
cyl('Reflector',(0,0,.72),.08,.10,'Wear',verts=8)
collider((0,0,.45),(.19,.19,.9))
