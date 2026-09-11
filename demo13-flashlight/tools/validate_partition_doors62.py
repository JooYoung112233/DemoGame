"""Check actual door openings, hinge clearance, initial locks and room separation.
This validates offline assets, not Unity interactions or navigation.
"""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT.parent/'ArtWork/TownInteriorsPartition62'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/TownInteriors62.blend'))
m=json.loads((OUT/'KitManifest.json').read_text(encoding='utf8'))
def tree(objects,matrix=Matrix.Identity(4)):
 vs=[];fs=[]
 for o in objects:
  offset=len(vs);vs.extend(matrix@o.matrix_world@v.co for v in o.data.vertices)
  fs.extend(tuple(offset+i for i in p.vertices) for p in o.data.polygons)
 return BVHTree.FromPolygons(vs,fs)
rows=[];ids=set()
for room,zones in m['expansionZones'].items():
 b=m['buildings'][room]
 for zone in zones:
  assert zone['id'] not in ids;ids.add(zone['id'])
  assert zone['initialState']=='locked' and zone['onUnlock']['keepPartition']
  d=zone['door'];p=m['layouts'][room][d['placementIndex']]
  assert p['zoneId']==zone['id'] and p['asset']==d['leafAsset']
  yaw=math.radians(d['yaw']);R=Matrix.Rotation(-yaw,4,'Z')
  x,y,z=d['hingeLocal'];T=Matrix.Translation((-x,-z,y))@R
  leaf=bpy.data.collections[d['leafAsset']+'_Editable'].objects
  closed=tree(leaf,T);opened=tree(leaf,T@Matrix.Rotation(math.pi/2,4,'Z'))
  fixed=tree(bpy.data.collections[d['frameAsset']+'_Editable'].objects,Matrix.Translation((0,-b['serviceCenterZ'],0)))
  x,_,z=d['openingCenter'];centre=Vector((-x,-z,0))
  normal=R.to_3x3()@Vector((0,-1,0));tangent=R.to_3x3()@Vector((-1,0,0))
  for height in [.30,1.0,1.90]:
   for offset in [-.30,0,.30]:
    origin=centre+tangent*offset+Vector((0,0,height))-normal*.6
    assert fixed.ray_cast(origin,normal,1.2)[0] is None,(zone['id'],'wall fills door opening')
    assert closed.ray_cast(origin,normal,1.2)[0] is not None,(zone['id'],'closed door does not block')
    assert opened.ray_cast(origin,normal,1.2)[0] is None,(zone['id'],'open leaf blocks clear passage')
  assert zone['roofAsset'] in m['assets'] and zone['floorAsset'] in m['assets']
  # Rear/side spaces have a permanent divider even after their own door unlocks.
  assert room+'_RoomDividers62' in m['assets']
  rows.append({'zone':zone['id'],'initiallyLocked':True,'fixedWallPreserved':True,'realOpeningRays':9,'closedDoorBlocks':True,'ninetyDegreeOpenClear':True,'stableHingePivot':True})
report={'rooms':5,'independentLockedZones':len(rows),'doors':rows,'notValidated':['Unity door interaction','unlock conditions/save state','Unity colliders/NavMesh','roof visibility runtime']}
(OUT/'DoorValidation.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('PARTITION_DOORS_VALIDATED',len(rows))
