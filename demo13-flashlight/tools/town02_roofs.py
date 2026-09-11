"""Building-specific roofs, informed by Assets/GPT/안전구역 exterior references.
Shared construction helpers do not dictate equipment locations or silhouettes.
"""
import bpy,math
from mathutils import Vector

def build_roof(n,w,d,h,metal,box,cyl,beam,export,parts,mats):
 def tube(name,a,b,r=.10,mat='Steel'):
  a=Vector((-a[0],-a[2],a[1]));b=Vector((-b[0],-b[2],b[1]))
  bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=(b-a).length,location=(a+b)/2)
  o=bpy.context.object;o.name=name;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
  bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mats[mat]);parts.append(o)
 def pipepath(name,points,r=.10,mat='Steel'):
  for a,b in zip(points,points[1:]):tube(name,a,b,r,mat)
 def fan(x,z,s=1,height=.78):
  y=h+.23
  box('Equipment curb',(x,y+.10,z),(1.6*s,.20,1.5*s),'Steel')
  box('Weathered fan housing',(x,y+.20+height/2,z),(1.5*s,height,1.4*s),'Edge')
  top=y+.20+height
  box('Fan housing rim',(x,top+.045,z),(1.64*s,.09,1.54*s),'Steel')
  cyl('Dark circular fan recess',(x,top+.10,z),.54*s,.04,'DarkOlive')
  for i in range(-4,5):
   a=i*.112*s;length=2*math.sqrt(max(.001,(.52*s)**2-a*a))
   box('Fan guard',(x+a,top+.133,z),(.023,.018,length),'Edge',.002)
   box('Fan guard',(x,top+.137,z+a),(length,.018,.023),'Edge',.002)
  cyl('Fan hub',(x,top+.15,z),.085*s,.045,'Steel')
  for i in range(5):box('Cooling grille',(x,y+.31+i*height/6,z-.71*s),(1.25*s,.025,.04),'Steel',.003)
 def vent(x,z,height=.65):
  cyl('Vent flashing',(x,h+.28,z),.24,.10,'Steel');cyl('Roof vent',(x,h+.26+height/2,z),.105,height,'Edge');cyl('Vent rain cap',(x,h+.29+height,z),.20,.07,'Steel')
 def coping(side,raiseBy,mat,cap,segments=True):
  alongX=side in ['front','back'];length=w if alongX else d
  p=[0,h+.22+raiseBy/2,(-d/2 if side=='front' else d/2)] if alongX else [(-w/2 if side=='left' else w/2),h+.22+raiseBy/2,0]
  box('Parapet '+side,p,(length+.32,raiseBy,.28) if alongX else (.28,raiseBy,length+.32),mat)
  count=math.ceil(length/.95) if segments else 1
  for i in range(count):
   pp=list(p);pp[1]=h+.22+raiseBy+.055;pp[0 if alongX else 2]=-length/2+(i+.5)*length/count
   # A missing cap exposes the dark masonry, not a repeated perfect bright rectangle.
   if n=='BlackMarket' and side=='front' and i in [2,5]:continue
   box('Individual coping block',pp,(length/count-.018,.11,.43) if alongX else (.43,.11,length/count-.018),cap,.024)
 roofMat={'Pawnshop':'RoofWarm','Repair':'RoofWarm','Medical':'RoofClean','Furniture':'RoofWarm','BlackMarket':'RoofDark','Container_Home':'Olive'}[n]
 box('Roof deck',(0,h+.11,0),(w+.35,.22,d+.35),roofMat)
 if metal:
  # Container corner castings and low steel rails: no masonry parapet.
  for z in [-d/2,d/2]:box('Container roof rail',(0,h+.28,z),(w+.34,.16,.20),'Edge')
  for x in [-w/2,w/2]:box('Container roof end',(x,h+.28,0),(.20,.16,d+.34),'Steel')
  for i in range(round(w/.32)):box('Folded roof rib',(-w/2+(i+.5)*w/round(w/.32),h+.26,0),(.065,.09,d-.15),'Olive',.013)
  for x in [-w/2,w/2]:
   for z in [-d/2,d/2]:
    box('ISO corner casting',(x,h+.33,z),(.38,.23,.38),'Edge')
    box('Lifting socket',(x,h+.451,z),(.17,.012,.14),'Steel',.002)
  fan(-w*.29,d*.26,.70,.46);vent(w*.34,d*.29,.38)
  pipepath('External return pipe',[(-w/2-.14,h+.32,d*.25),(-w/2-.14,h+.75,d*.25),(-w*.29-.6,h+.75,d*.25)],.065)
  for i in range(2):box('Sheet repair',(.65+i*.82,h+.315,-d*.19),(.72,.018,1.18),'DarkOlive',.003)
 elif n=='Pawnshop':
  coping('left',.78,'Brick','Brick');coping('right',.78,'Brick','Brick')
  coping('back',.31,'Concrete','Concrete');coping('front',.36,'Brick','WoodDark',False)
  for x in [-w/2,w/2]:
   for z in [-d/2,d/2]:box('Raised brick pier',(x,h+.65,z),(.55,.92,.55),'Brick')
  fan(-w*.28,d*.18,.90,.71);vent(w*.27,-d*.22,.72)
  # Two dark roofing rolls overlap at a repair, not the shared pale rectangle.
  for i in range(2):box('Bitumen patch',(w*.23+i*.4,h+.23+i*.016,d*.24),(1.65,.014,1.05),'RoofDark',.002)
  for i in range(4):box('Drain cover',(w/2-.45,h+.244,-d/2+.45+i*.06),(.30,.02,.028),'Steel',.002)
 elif n=='Repair':
  for side in ['front','back','left','right']:coping(side,.32,'Concrete','Concrete')
  # Elevated tank / service pipes / turbine follow the dedicated workshop concept.
  x,z=-w*.29,d*.25
  for dx in [-.57,.57]:
   for dz in [-.57,.57]:box('Tank stand',(x+dx,h+.83,z+dz),(.10,1.20,.10),'WoodDark')
  beam('Tank stand diagonal',(x-.57,h+.30,z-.57),(x+.57,h+1.35,z-.57),.09,'Wood')
  beam('Tank stand diagonal',(x+.57,h+.30,z-.57),(x-.57,h+1.35,z-.57),.09,'Wood')
  box('Tank platform',(x,h+1.39,z),(1.48,.12,1.48),'WoodDark')
  cyl('Utility water tank',(x,h+2.02,z),.73,1.18,'Olive')
  for yy in [h+1.48,h+2.55]:cyl('Tank reinforcing band',(x,yy,z),.765,.075,'Edge')
  cyl('Tank inspection lid',(x,h+2.64,z),.32,.07,'Steel')
  pipepath('Tank outflow',[(x+.73,h+1.8,z),(x+1.1,h+1.8,z),(x+1.1,h+.46,z),(x+2.35,h+.46,z)],.12)
  for dx in [0,.47]:
   x=w*.20+dx
   pipepath('Paired workshop pipe',[(x,h+.29,d*.37),(x,h+.56,d*.29),(x,h+.56,-d*.03),(x+.32,h+.56,-d*.09),(x+.32,h+.3,-d*.14)],.15)
   for zz in [d*.24,.4]:tube('Pipe coupling',(x,h+.56,zz-.07),(x,h+.56,zz+.07),.19,'Edge')
  x,z=-.4,.3;cyl('Turbine curb',(x,h+.30,z),.59,.16,'Steel');cyl('Turbine barrel',(x,h+.64,z),.46,.56,'Edge')
  for i in range(14):
   a=i*math.tau/14;box('Turbine blade',(x+math.cos(a)*.47,h+.65,z+math.sin(a)*.47),(.055,.43,.055),'Steel',.006)
  cyl('Turbine top',(x,h+.96,z),.51,.08,'Steel');fan(w*.32,d*.27,.60,.54)
  vent(-w*.26,-d*.29,.92)
 elif n=='Medical':
  for side in ['front','back','left','right']:coping(side,.24,'Cream','Cream')
  # Clean, low service roof with a compact twin air handler and clear maintenance lane.
  for x in [-w/2,w/2]:
   for z in [-d/2,d/2]:box('Clinic cap',(x,h+.52,z),(.54,.32,.54),'Cream')
  for x in [-1,1]:fan(-w*.17+x*.63,d*.22,.62,.48)
  box('Air handler duct',(-w*.17,h+.55,d*.22-1.15),(1.7,.43,1.15),'Sage')
  for i in range(5):box('Service stepping tile',(-w*.17,h+.231,-d*.27+i*.58),(.77,.025,.53),'Tile',.006)
  vent(w*.29,-d*.20,.47)
 elif n=='Furniture':
  for side in ['left','right','back']:coping(side,.45,'Concrete','Concrete')
  coping('front',.40,'Concrete','Wood',False)
  # A partial corrugated repair breaks the outline of the weathered flat roof.
  x=-w*.29;patchWidth=w*.35;patchDepth=d*.66;z=d*.04
  box('Repaired roof underlay',(x,h+.29,z),(patchWidth,.14,patchDepth),'WoodDark')
  for i in range(round(patchWidth/.27)):box('Patched metal sheet',(x-patchWidth/2+(i+.5)*patchWidth/round(patchWidth/.27),h+.39,z),(.23,.065,patchDepth),'DarkOlive',.01)
  for zz in [-patchDepth/2+z,patchDepth/2+z]:box('Patch seam batten',(x,h+.43,zz),(patchWidth+.2,.075,.09),'WoodDark')
  fan(w*.25,d*.27,.68,.58);vent(w*.30,-d*.27,.80)
  for i in range(5):box('Spare timber',(-w*.27+i*.18,h+.49,d*.29),(.14,.08,1.5),'Wood',.008)
 elif n=='BlackMarket':
  for side in ['left','right','back','front']:coping(side,.29,'Graphite','Graphite')
  fan(-w*.29,d*.27,.78,.58)
  xx,zz=w*.29,d*.28
  box('Antenna junction',(xx,h+.41,zz),(.49,.36,.49),'Steel')
  cyl('Antenna mast',(xx,h+1.64,zz),.035,2.55,'Steel')
  beam('Aerial spine',(xx-1.1,h+2.93,zz),(xx+1.1,h+2.93,zz),.041,'Edge')
  for i in range(-4,5):beam('Aerial fin',(xx+i*.24,h+2.93,zz-.32),(xx+i*.24,h+2.93,zz+.32),.025,'Edge')
  for endpoint in [(-w*.29,d*.27),(w*.20,-d*.38),(-w*.31,-d*.12)]:
   pts=[]
   for i in range(19):
    t=i/18;pts.append((xx+(endpoint[0]-xx)*t+math.sin(t*8)*.18,h+.25,zz+(endpoint[1]-zz)*t+math.sin(t*6)*.35))
   pipepath('Loose roof cable',pts,.024)
  for i in range(7):box('Loose coping debris',(-w*.37+(i%3)*.29,h+.28,-d*.26+(i//3)*.34),(.22,.11,.19),'Graphite',.022)
  for x,z,sx,sz in [(-w*.24,-d*.20,2.2,1.5),(w*.24,d*.29,1.8,1.0),(w*.2,-d*.25,1.15,1.7)]:box('Tar repair',(x,h+.232,z),(sx,.012,sz),'Steel',.002)
 export(n+'_Roof')
