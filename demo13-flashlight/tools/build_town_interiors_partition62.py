"""Keep Town02 exteriors; replace oversized shops with smaller front service rooms.
Offline only. Furniture geometry/materials are reused without scaling.
"""
from pathlib import Path
HELPER=Path(__file__).with_name('build_town02.py')
head=HELPER.read_text(encoding='utf8').split('def window(')[0]
head=head.replace("OUT=ROOT/'Assets/Art/Environments/Town02'", "OUT=ROOT.parent/'ArtWork/TownInteriorsPartition62'")
exec(compile(head,str(HELPER),'exec'),globals())
import hashlib,shutil,copy
from mathutils import Matrix
SOURCE=ROOT.parent/'ArtWork/TownInteriors62'
old=json.loads((SOURCE/'KitManifest.json').read_text(encoding='utf8'))
source=SOURCE/'BlenderSource~/TownInteriors62.blend'
source_hash=hashlib.sha256(source.read_bytes()).hexdigest()
inventory={};layouts={};buildings={};expansions={}
names=[n for n,s in old['assets'].items() if s['category'] not in ['architecture','occlusion','previewOnly']]
with bpy.data.libraries.load(str(source),link=False) as (src,dst):
 dst.collections=[n+'_Editable' for n in names]
for col in dst.collections:
 bpy.context.scene.collection.children.link(col);col.hide_render=True;col.hide_viewport=True
for name in names:
 inventory[name]=copy.deepcopy(old['assets'][name]);shutil.copyfile(SOURCE/'Models'/f'{name}.fbx',OUT/'Models'/f'{name}.fbx')
# Reuse already reviewed material graphs, including packed metre-scale textures.
with bpy.data.libraries.load(str(source),link=False) as (src,dst):
 dst.materials=[n for n in src.materials if n.startswith('Interior62_') and bpy.data.materials.get(n) is None]
for m in bpy.data.materials:
 if m.name.startswith('Interior62_'):mats[m.name[len('Interior62_'):]]=m
roofmat=mats['DarkConcrete'].copy();roofmat.name='Interior62_ClosedRoof'
for node in roofmat.node_tree.nodes:
 if node.type=='MIX_RGB' and node.blend_type=='MULTIPLY':node.inputs[2].default_value=(.035,.044,.040,1)
mats['ClosedRoof']=roofmat

def done(name,ref,category):
 vs=[v.co for o in parts for v in o.data.vertices]
 lo=[min(v[i] for v in vs) for i in range(3)];hi=[max(v[i] for v in vs) for i in range(3)]
 export(name)
 inventory[name]={**assets[name],'reference':ref,'size':[hi[0]-lo[0],hi[2]-lo[2],hi[1]-lo[1]],'category':category,'footprint':[hi[0]-lo[0],hi[1]-lo[1]]}

# Review correction: replace the source drape's grid-cut staircase with a continuous hem.
# Keep the counter dimensions/material and every other reused piece unchanged.
col=bpy.data.collections['MarketCounter62_Editable']
drape=next(o for o in col.objects if o.name.startswith('Counter drape'))
cloth_mats=list(drape.data.materials);vs=[];nx,ny=24,20
for j in range(ny+1):
 t=j/ny
 for i in range(nx+1):
  u=i/nx*(.28+.30*(1-t));x=-1.8+3.6*u
  depth=.38-t*.91;height=1.035+.018*math.sin(u*22+t*3)
  if t>.78:height-=((t-.78)/.22)*(.42+.13*math.sin(u*5));depth=-.40-(t-.78)*.09
  vs.append((-x,-depth,height))
faces=[(j*(nx+1)+i,j*(nx+1)+i+1,(j+1)*(nx+1)+i+1,(j+1)*(nx+1)+i) for j in range(ny) for i in range(nx)]
me=bpy.data.meshes.new('Continuous drape hem');me.from_pydata(vs,[],faces);me.update()
for mat in cloth_mats:me.materials.append(mat)
drape.data=me;col.hide_viewport=False;bpy.context.view_layer.update();bpy.context.view_layer.objects.active=drape
mod=drape.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.004;bpy.ops.object.modifier_apply(modifier=mod.name)
col.name='MarketCounter62_Previous';parts=list(col.objects);export('MarketCounter62');bpy.data.collections.remove(col)
inventory['MarketCounter62'].update(assets['MarketCounter62'])

sizes={'Pawnshop':(9,6),'Repair':(7.5,6),'Medical':(8,6),'Furniture':(7,7),'BlackMarket':(7,6)}
walls={'Pawnshop':'Brick','Repair':'Ochre','Medical':'Cream','Furniture':'Wood','BlackMarket':'Graphite'}
door_x={'Pawnshop':3.05,'Repair':2.5,'Medical':2.65,'Furniture':0,'BlackMarket':2.5}
side_doors={'Pawnshop':(.25,1.9),'Repair':(-1.9,1.65),'Medical':(-.2,-1.0),'Furniture':(.5,1.1),'BlackMarket':(-1.1,.2)}
# Shared leaf: local origin is the floor-height hinge; Unity Y is its rotation axis.
box('Door leaf',(.55,1.1,0),(1.10,2.2,.065),'DarkOlive',.014)
box('Door inset',(.55,1.16,-.037),(.87,1.80,.016),'Olive',.012)
box('Storage label',(.55,1.74,-.052),(.36,.15,.012),'Paper',.004)
box('Lock case',(.96,1.0,-.055),(.085,.16,.05),'Edge',.005)
box('Latch handle',(.89,1.04,-.092),(.20,.038,.035),'Steel',.005)
for y in [.30,1.9]:cyl('Door hinge',(.01,y,0),.045,.18,'Steel')
done('StorageDoorLeaf62',old['buildings']['Pawnshop']['reference'],'hingedDoor')

def wall_with_door(name,length,centre,wall,ref,origin,yaw):
 # Model in a wall-local frame; rotate to actual side/rear before export.
 z=0;gap=.60
 for a,b in [(-length/2,centre-gap),(centre+gap,length/2)]:
  if b<=a:continue
  x=(a+b)/2
  box('Partition pier',(x,1.5,z),(b-a,3,.16),wall,.008)
  box('Lower wainscot',(x,.45,-.095),(b-a,.90,.035),'Sage' if wall=='Cream' else 'WoodDark',.003)
  box('Chair rail',(x,.93,-.12),(b-a,.065,.04),'Sage' if wall=='Cream' else 'Wood',.005)
 box('Over door wall',(centre,2.63,0),(1.2,.74,.16),wall,.008)
 for x in [centre-.615,centre+.615]:box('Storage jamb',(x,1.13,-.015),(.065,2.26,.19),'Steel')
 box('Storage lintel',(centre,2.26,-.015),(1.30,.075,.19),'Steel')
 box('Flush door threshold',(centre,.005,0),(1.22,.02,.62),'Concrete',.002)
 mat=Matrix.Translation((-origin[0],-origin[1],0))@Matrix.Rotation(-math.radians(yaw),4,'Z')
 for o in parts:o.data.transform(mat);o.data.update()
 done(name,ref,'fixedPartitionWithDoorway')
 # Unity positive yaw rotates X toward -Z.
 a=math.radians(yaw);cx=origin[0]+centre*math.cos(a);cz=origin[1]-centre*math.sin(a)
 hx=origin[0]+(centre-.55)*math.cos(a);hz=origin[1]-(centre-.55)*math.sin(a)
 return {'hingeLocal':[hx,0,hz],'yaw':yaw,'openingCenter':[cx,1.13,cz],'openingWidth':1.2,'openingHeight':2.26,
         'swingAxis':'local Y','openAngleDegrees':-90,'leafAsset':'StorageDoorLeaf62','frameAsset':name}
for name,(w,d) in sizes.items():
 b=copy.deepcopy(old['buildings'][name]);W,D=b['width'],b['depth'];cz=-(D-d)/2;ref=b['reference'];wall=walls[name]
 b.update(serviceWidth=w,serviceDepth=d,serviceCenterZ=cz,serviceArea=w*d,closedArea=W*D-w*d,closedDoorInteractive=False)
 buildings[name]=b
 box('Service floor slab',(0,-.08,0),(w-.34,.16,d-.34),'DarkConcrete' if name=='BlackMarket' else 'Concrete',.015)
 if name in ['Furniture','Pawnshop']:
  front=-d/2+.19 if name=='Furniture' else .60
  span=d/2-.19-front;rows=math.ceil(span/.27)
  for j in range(rows):
   z=front+(j+.5)*span/rows
   for k in range(4):
    ww=(w-.38)/4;x=-(w-.38)/2+(k+.5)*ww
    box('Floor boards',(x,.012,z),(ww-.006,.025,span/rows-.005),'WoodLight' if (j+k)%7==0 else 'Wood',.002)
 if name in ['Medical','Pawnshop']:
  back=d/2-.18 if name=='Medical' else .58
  box('Tile floor',(0,.012,(-d/2+.18+back)/2),(w-.36,.025,back+d/2-.18),'Tile' if name=='Medical' else 'PawnTile',.002)
 done(name+'_Floor62',ref,'architecture')
 doors={}
 doors['Rear']=wall_with_door(name+'_RearLining62',w,door_x[name],wall,ref,(0,d/2-.175),0)
 for side,label,dc in [(-1,'Left',side_doors[name][0]),(1,'Right',side_doors[name][1])]:
  # Negative side faces the service room, and leaves swing outward into storage.
  yaw=-90 if side<0 else 90
  doors[label]=wall_with_door(name+'_'+label+'Partition62',d-.36,dc if side<0 else -dc,wall,ref,(side*(w/2-.175),0),yaw)
 for side in [-1,1]:box('Front interior lining',(side*(w+1.6)/4,1.2,-d/2+.175),((w-1.6)/2-.19,2.4,.035),wall,.002)
 done(name+'_TallLining62',ref,'occlusion')
 for s,dc in [(-1,side_doors[name][0]),(1,side_doors[name][1])]:
  for a,b in [(-d/2+.18,dc-.65),(dc+.65,d/2-.18)]:
   if b>a:box('Cutaway partition',(s*(w/2-.175),.18,(a+b)/2),(.16,.36,b-a),'Concrete')
 for s in [-1,1]:box('Cutaway front',(s*(w+1.6)/4,.18,-d/2+.175),((w-1.6)/2-.18,.36,.16),'Concrete')
 done(name+'_PreviewCutaway62',ref,'previewOnly')
 # Coordinates of closure volumes are outer-building local, not service-room local.
 # Retain these lids when hiding the original exterior roof; do not reveal empty storage.
 edge=d/2+cz;top=3.04
 regions=[('Rear',0,(D/2+edge)/2,W-.30,D/2-edge)]
 for s,label in [(-1,'Left'),(1,'Right')]:regions.append((label,s*(W+w)/4,cz,(W-w)/2-.15,d-.15))
 expansions[name]=[]
 for label,x,z,rw,rd in regions:
  box('Closed ceiling slab',(x,top,z),(rw,.12,rd),'ClosedRoof',.012)
  # A few construction joints keep the broad inaccessible surfaces quiet.
  for j in range(1,max(1,math.ceil(rw/2.8))):
   xx=x-rw/2+j*rw/math.ceil(rw/2.8)
   box('Ceiling joint',(xx,top+.061,z),(.014,.003,rd-.04),'Graphite',.001)
  roof=name+'_'+label+'ClosureRoof62';done(roof,ref,'lockedZoneRoof')
  box('Closed room subfloor',(x,-.08,z),(rw,.16,rd),'Concrete',.008)
  floor=name+'_'+label+'ClosedFloor62';done(floor,ref,'futureRoomFloor')
  door=doors[label];door['hingeLocal'][2]+=cz;door['openingCenter'][2]+=cz
  # Rear, left, right room bounds are independent. Walls stay when the door opens.
  expansions[name].append({'id':name.lower()+'.'+label.lower(),'initialState':'locked','unlockCondition':None,
   'boundsCenter':[x,1.5,z],'boundsSize':[rw,3,rd],'door':door,'floorAsset':floor,'roofAsset':roof,
   'onUnlock':{'unlockDoor':True,'keepPartition':True,'roofVisibility':'hide while room occupied','releaseZoneBlocker':True},
   'requiredBeforeUnlock':['add intended unlocked function','wire door interaction','refresh colliders and navigation','save unlocked state'],
   'newInteractionsImplemented':False})
 # Opaque front returns prevent a visual hole beneath the locked-side ceilings.
 for s in [-1,1]:box('Locked front return',(s*(W+w)/4,1.5,-D/2+.175),((W-w)/2-.12,3,.16),wall,.008)
 done(name+'_ClosedFrontReturn62',ref,'permanentClosure')
 for s in [-1,1]:box('Independent locked room divider',(s*(W+w)/4,1.5,edge),((W-w)/2,3,.16),wall,.008)
 done(name+'_RoomDividers62',ref,'fixedRoomDivider')
 for x in [-W/2+.15,W/2-.15]:box('Outer cutaway wall',(x,.16,0),(.18,.32,D-.18),'Concrete')
 box('Outer rear cutaway',(0,.16,D/2-.15),(W-.18,.32,.18),'Concrete')
 for s in [-1,1]:box('Outer front cutaway',(s*(W+1.6)/4,.16,-D/2+.15),((W-1.6)/2-.18,.32,.18),'Concrete')
 done(name+'_OuterOutline62',ref,'previewOnly')

def place(room,asset,x,z,y=0,yaw=0,scale=1,kind='obstacle',outer=False):
 if not outer:z+=buildings[room]['serviceCenterZ']
 layouts.setdefault(room,[]).append({'asset':asset,'position':[x,y,z],'yaw':yaw,'scale':scale,'kind':kind,'footprint':inventory[asset]['footprint']})
for n in buildings:
 for suffix in ['Floor62','RearLining62','TallLining62','PreviewCutaway62']:place(n,n+'_'+suffix,0,0,kind='architecture')
 for side in ['Left','Right']:place(n,n+'_'+side+'Partition62',0,0,kind='expandablePartition')
 for side in ['Rear','Left','Right']:place(n,n+'_'+side+'ClosureRoof62',0,0,kind='lockedZoneRoof',outer=True)
 for side in ['Rear','Left','Right']:place(n,n+'_'+side+'ClosedFloor62',0,0,kind='futureRoomFloor',outer=True)
 for zone in expansions[n]:
  door=zone['door'];x,y,z=door['hingeLocal'];place(n,door['leafAsset'],x,z,y,yaw=door['yaw'],kind='lockedDoor',outer=True)
  layouts[n][-1]['zoneId']=zone['id'];door['placementIndex']=len(layouts[n])-1
 place(n,n+'_ClosedFrontReturn62',0,0,kind='permanentClosure',outer=True)
 place(n,n+'_RoomDividers62',0,0,kind='fixedRoomDivider',outer=True)
 place(n,n+'_OuterOutline62',0,0,kind='previewOnly',outer=True)

N='Pawnshop'
place(N,'PawnDisplay62',-1.32,-.10);place(N,'PawnDisplay_Electronics62',1.32,-.10)
place(N,'CashRegister62',-1.3,-.04,1.07,kind='tabletop')
place(N,'SalvageShelf62',-2.75,2.30);place(N,'Safe62',.25,2.1)
place(N,'AntiqueClock62',-2.2,2.22,2.14,kind='tabletop')
place(N,'AntiqueDesk62',3.5,.4,yaw=-90)
place(N,'Reuse_Radio01',3.5,.35,.90,yaw=-90,kind='tabletop')
place(N,'Armchair_Upholstery62',3.3,-1.8,yaw=-20)
place(N,'Reuse_WoodCrate01',-3.5,-1.65);place(N,'Reuse_CardboardStack02',-3.2,-2.35)
place(N,'Reuse_Rug01',0,-1.75,scale=.70,kind='floorDressing')
N='Repair'
place(N,'Reuse_Workbench01',-.9,1.98);place(N,'ToolBoard62',-.9,2.72,1.02,kind='wall')
place(N,'Reuse_Toolbox02',-.95,1.92,.96,kind='tabletop')
place(N,'SalvageShelf62',-2.85,1.2,yaw=90);place(N,'Reuse_LockerPair02',.95,2.2)
place(N,'WeldingTrolley62',-2.65,-.90);place(N,'TireStack62',2.55,-1.4)
place(N,'Reuse_UtilitySink02',2.95,.35,yaw=-90)
place(N,'MechanicCreeper62',1,-.50,yaw=-18)
place(N,'Reuse_FireExtinguisher02',1.3,-2.45)
N='Medical'
place(N,'Reception62',0,-1.1);place(N,'TreatmentBed62',-1.3,1.75,yaw=90)
place(N,'MedicineCabinet62',-3.28,1.6,yaw=90);place(N,'IVStand62',.4,2.2)
place(N,'InstrumentCart62',-.9,.55);place(N,'PrivacyScreen62',1.3,1.55,yaw=90)
place(N,'Reuse_UtilitySink02',2.95,.55,yaw=-90)
place(N,'Armchair_BlueCloth62',-2.8,-1.85);place(N,'Reuse_Stool01',2.1,-2.0)
N='Furniture'
place(N,'SalvageShelf62',-1.85,2.7);place(N,'Wardrobe62',1.7,2.7)
place(N,'DiningTable62',-.65,-.25)
for x in [-1.3,0]:
 place(N,'WoodChair62',x,.80);place(N,'WoodChair62',x,-1.30,yaw=180)
place(N,'Armchair_BlueCloth62',2.2,-.15,yaw=-90);place(N,'Armchair_Linen62',2.2,-1.80,yaw=-90)
place(N,'AntiqueDesk62',-1.9,-2.62)
place(N,'Reuse_Rug01',-.65,-.25,scale=1.20,kind='floorDressing')
N='BlackMarket'
place(N,'MarketCounter62',0,-.65);place(N,'SalvageShelf62',-1.65,2.25)
place(N,'Safe62',.70,2.15);place(N,'Reuse_LockerPair02',-2.65,.20,yaw=90)
place(N,'Reuse_WoodCrate01',2.55,1.5)
place(N,'Reuse_FoldedTarp02',2.55,1.5,.445,kind='tabletop')
place(N,'Reuse_Radio01',-1.18,-.63,1.25,kind='tabletop')
place(N,'Reuse_CardboardStack02',-2.45,-2.35)
place(N,'Reuse_Rug01',0,-2.0,scale=.65,kind='floorDressing')

local_targets={'Pawnshop':[(0,-1.1),(0,.9)],'Repair':[(0,-1.6),(0,.7)],'Medical':[(0,-2.1),(0,0)],'Furniture':[(.9,-2.5),(1.8,1.35)],'BlackMarket':[(0,-1.7),(0,.6)]}
for n,b in buildings.items():
 b['approachTargets']=[[x,z+b['serviceCenterZ']] for x,z in local_targets[n]]
 for zone in expansions[n]:
  door=zone['door'];a=math.radians(door['yaw']);x,_,z=door['openingCenter']
  door['serviceApproach']=[x-.82*math.sin(a),z-.82*math.cos(a)]
  b['approachTargets'].append(door['serviceApproach'])
 b['previewPlayer']=[0,-sizes[n][1]/2+.9+b['serviceCenterZ']]
 col=bpy.data.collections.new(n+'_Assembly62');bpy.context.scene.collection.children.link(col)
 for i,p in enumerate(layouts[n]):
  x,y,z=p['position'];mat=Matrix.Translation((-x,-z,y))@Matrix.Rotation(-math.radians(p['yaw']),4,'Z')@Matrix.Scale(p['scale'],4)
  for o in bpy.data.collections[p['asset']+'_Editable'].objects:
   cp=o.copy();col.objects.link(cp);cp.matrix_world=mat@o.matrix_world
   cp.name=f'{n}_{i:02d}_{o.name}';cp['asset']=p['asset'];cp['placement_index']=i
   if p['asset'].endswith('TallLining62'):cp.hide_render=True
   if p['asset'].endswith(('LeftPartition62','RightPartition62')) and not o.name.startswith(('Storage jamb','Storage lintel','Flush door threshold')):cp.hide_render=True
 col.hide_render=True;col.hide_viewport=True
for im in bpy.data.images:
 if im.source=='FILE' and not im.packed_file:
  try:im.pack()
  except RuntimeError:pass
assert hashlib.sha256(source.read_bytes()).hexdigest()==source_hash
manifest={'units':'metres','coordinateConvention':old['coordinateConvention'],'camera':[62,0],
 'assets':inventory,'materials':{**old['materials'],'Interior62_ClosedRoof':{'color':[.035,.044,.040],'family':'Plaster','roughness':.82,'metallic':0,'normalStrength':.12}},'layouts':layouts,'buildings':buildings,'expansionZones':expansions,
 'sourceHashes':{**old['sourceHashes'],'../ArtWork/TownInteriors62/BlenderSource~/TownInteriors62.blend':source_hash},
 'furnitureScalePolicy':'original dimensions; only rugs scaled','status':'offline partition proposal; original exterior footprint/door/world placement retained. Unity NPC approach points must be relocated; not yet applied.'}
(OUT/'KitManifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/TownInteriors62.blend'))
print('PARTITION62_BUILT',len(inventory),'models',sum(len(v) for v in layouts.values()),'placements')
