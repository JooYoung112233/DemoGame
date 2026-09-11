def refine_hero_workwear():
 from mathutils.bvhtree import BVHTree
 changed=[]
 profile=[(.18,1.07),(.265,1.13),(.32,1.25),(.395,1.30),(.455,1.26),(.485,1.29),(.57,1.24),(.66,1.15),(.735,1.05),(.785,1.0)]
 for sign in [1,-1]:
  o=bpy.data.objects['Hero_TrouserLeg_'+str(sign)]
  if not o.get('workwear_fit_v1'):
   rings={}
   for v in o.data.vertices:rings.setdefault(round(v.co.z,3),[]).append(v)
   for z,vs in rings.items():
    factor=min(profile,key=lambda p:abs(p[0]-z))[1]
    cx=(min(v.co.x for v in vs)+max(v.co.x for v in vs))*.5;cy=(min(v.co.y for v in vs)+max(v.co.y for v in vs))*.5
    minimum=min(abs(v.co.x) for v in vs) if z>=.66 else .012
    for v in vs:
     v.co.x=cx+(v.co.x-cx)*factor;v.co.y=cy+(v.co.y-cy)*(1+(factor-1)*.8)
     v.co.x=sign*max(minimum,sign*v.co.x)
   o.data.update();o['workwear_fit_v1']=True;changed.append(o.name)
  o=bpy.data.objects['Hero_TrouserCuff_'+str(sign)]
  if not o.get('workwear_fit_v1'):
   for v in o.data.vertices:v.co.x=sign*.103+(v.co.x-sign*.103)*1.06;v.co.y=.00564+(v.co.y-.00564)*1.06
   o.data.update();o['workwear_fit_v1']=True;changed.append(o.name)
 def bvh(name):
  d=bpy.data.objects[name].data;return BVHTree.FromPolygons([v.co.copy() for v in d.vertices],[tuple(p.vertices) for p in d.polygons])
 jacket=bvh('Hero_JacketBody');pack=bvh('Hero_Backpack');belt=bvh('Hero_Belt')
 for sign in [1,-1]:
  o=bpy.data.objects['Hero_PackStrap_'+str(sign)]
  if o.get('attachment_seated_v1'):continue
  for i in [0,1,12,13]:
   v=o.data.vertices[i];hit,_,_,_=jacket.ray_cast(Vector((v.co.x,-2,v.co.z)),Vector((0,1,0)))
   if hit is not None:v.co.y=hit.y-(.009 if i<12 else .001)
  for i in [10,11,22,23]:
   v=o.data.vertices[i];v.co.x-=sign*.021;v.co.y+=.028
   for g in o.vertex_groups:g.remove([i])
   group=o.vertex_groups.get('Backpack') or o.vertex_groups.new(name='Backpack');group.add([i],1,'REPLACE')
  o.data.update();o['attachment_seated_v1']=True;changed.append(o.name)
 o=bpy.data.objects['Hero_BackpackFlap']
 if not o.get('attachment_seated_v1'):
  rings={}
  for v in o.data.vertices:rings.setdefault(round(v.co.z,4),[]).append(v)
  for z,vs in rings.items():
   if z<1.10:continue
   low=min(v.co.y for v in vs);high=max(v.co.y for v in vs)
   for v in vs:
    t=(v.co.y-low)/(high-low) if high-low>.001 else .5
    hit,_,_,_=pack.ray_cast(Vector((v.co.x,2,v.co.z)),Vector((0,-1,0)))
    if hit is not None:v.co.y=hit.y-.002+.025*t
  o.data.update();o['attachment_seated_v1']=True;changed.append(o.name)
 o=bpy.data.objects['Hero_LanternLoop']
 if not o.get('attachment_seated_v1'):
  top=Vector((-.215,-.074,.805));hit,_,_,_=belt.find_nearest(top)
  delta=hit-top+Vector((.003,0,0))
  for v in o.data.vertices:
   if v.co.z>.80:v.co+=delta
   else:v.co.z-=.004
  o.data.update();o['attachment_seated_v1']=True;changed.append(o.name)
 return changed
