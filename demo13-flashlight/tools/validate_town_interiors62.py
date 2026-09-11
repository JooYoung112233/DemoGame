"""Independent geometry and conservative floor-plan reachability checks."""
import bpy,json,math,hashlib,sys
from pathlib import Path
from collections import deque
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT.parent/'ArtWork/TownInteriors62'
if '--partition' in sys.argv:OUT=ROOT.parent/'ArtWork/TownInteriorsPartition62'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/TownInteriors62.blend'))
m=json.loads((OUT/'KitManifest.json').read_text(encoding='utf8'));report=[]
for path,h in m['sourceHashes'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==h,path
for name,spec in m['assets'].items():
 assert (OUT/'Models'/f'{name}.fbx').stat().st_size>1000,name
 assert (ROOT/spec['reference']).exists(),spec['reference']
 meshes=[o.data for o in bpy.data.collections[name+'_Editable'].objects if o.type=='MESH']
 for me in meshes:
  assert len(me.uv_layers)==1 and me.uv_layers.active.name=='MetreUV',(name,'UV0')
  assert all(math.isfinite(n) for v in me.vertices for n in v.co),(name,'finite vertices')
  assert all(math.isfinite(n) for v in me.uv_layers.active.data for n in v.uv),(name,'finite UVs')
  assert all(p.area>1e-10 and p.normal.length>.9 for p in me.polygons),(name,'surface')
 count=sum(len(p.vertices)-2 for me in meshes for p in me.polygons)
 assert count==spec['triangles'],name
 report.append({'name':name,'triangles':count,'metricUV0':True,'validSurface':True})
# Floor plan uses a conservative .35m-radius pedestrian and actual rotated footprint boxes.
# This is not a NavMesh/playtest or a claim about current gameplay collider radius.
walk=[];radius=.35;step=.15
for name,b in m['buildings'].items():
 width=b.get('serviceWidth',b['width']);depth=b.get('serviceDepth',b['depth']);cz=b.get('serviceCenterZ',0)
 rects=[]
 for p in m['layouts'][name]:
  if p['kind']!='obstacle':continue
  x,_,z=p['position'];w,d=p['footprint'];w*=p['scale'];d*=p['scale'];a=math.radians(p['yaw'])
  ex=(abs(math.cos(a))*w+abs(math.sin(a))*d)/2;ez=(abs(math.cos(a))*d+abs(math.sin(a))*w)/2
  assert abs(x)+ex<=width/2-.16+.04,(name,p['asset'],'side wall overlap')
  assert abs(z-cz)+ez<=depth/2-.16+.04,(name,p['asset'],'front/rear wall overlap')
  rects.append((x-ex-radius,x+ex+radius,z-ez-radius,z+ez+radius))
 def free(x,z):
  return abs(x)<width/2-.25-radius and abs(z-cz)<depth/2-.25-radius and not any(x1<x<x2 and z1<z<z2 for x1,x2,z1,z2 in rects)
 nx=math.floor((width-1)/step);nz=math.floor((depth-1)/step)
 def pos(i,j):return ((i-nx/2)*step,(j-nz/2)*step+cz)
 valid={(i,j) for i in range(nx+1) for j in range(nz+1) if free(*pos(i,j))}
 start=min(valid,key=lambda v:pos(*v)[0]**2+(pos(*v)[1]+b['depth']/2-.8)**2)
 q=deque([start]);seen={start}
 while q:
  i,j=q.popleft()
  for nxt in [(i-1,j),(i+1,j),(i,j-1),(i,j+1)]:
   if nxt in valid and nxt not in seen:seen.add(nxt);q.append(nxt)
 # Approach customer-facing counter/desk, or reach room centre and rear in shops without a counter.
 targets={'Pawnshop':[(0,.3),(0,2.4)],'Repair':[(0,0),(0,2.5)],'Medical':[(0,-2.7),(1,.05)],'Furniture':[(0,-2),(0,4)],'BlackMarket':[(0,-3.2),(0,0)]}[name]
 targets=b.get('approachTargets',targets)
 for tx,tz in targets:
  nearest=min(valid,key=lambda v:(pos(*v)[0]-tx)**2+(pos(*v)[1]-tz)**2)
  assert nearest in seen,(name,'unreachable target',tx,tz)
  assert math.dist(pos(*nearest),(tx,tz))<.3,(name,'blocked target',tx,tz)
 walk.append({'building':name,'pedestrianRadius':radius,'gridStep':step,'entryWidth':b['doorWidth'],'approachTargetsReachable':targets,'connectedFreeCells':len(seen),'allFreeCells':len(valid)})
 if 'serviceWidth' in b:
  assert abs(cz-depth/2+b['depth']/2)<1e-6,(name,'front door alignment')
  assert b['width']>=width and b['depth']>=depth,(name,'service inside exterior')
  for p in m['layouts'][name]:
   if p['kind'] not in ['floorDressing','architecture','previewOnly','permanentClosure']:assert p['scale']==1,(name,'furniture resized')
result={'models':report,'totalTriangles':sum(r['triangles'] for r in report),'roomCount':len(walk),'floorPlanChecks':walk,'sourceHashesPreserved':True,'notValidated':['Unity GameLit','roof/wall occlusion','runtime NPC interaction and collision','NavMesh']}
(OUT/'ModelValidation.json').write_text(json.dumps(result,indent=2),encoding='utf8');print('TOWN_INTERIORS62_VALIDATED',len(report),'models')
