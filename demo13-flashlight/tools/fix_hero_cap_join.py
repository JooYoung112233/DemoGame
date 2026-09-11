"""Seat the rear edge of the brim inside the actual crown curve, preserving UVs."""
def fix_hero_cap_join(cap):
 from mathutils.bvhtree import BVHTree
 mesh=cap.data
 if cap.get('brim_seated_v1'):return {'already_fixed':True}
 crown_faces=[tuple(p.vertices) for p in mesh.polygons if mesh.materials[p.material_index].name.removeprefix('Surface_').split('.')[0]=='Hero_cap']
 crown_ids={v for p in crown_faces for v in p};bottom=min(mesh.vertices[i].co.z for i in crown_ids)
 surface=BVHTree.FromPolygons([v.co.copy() for v in mesh.vertices],crown_faces,all_triangles=False)
 # Three rows of 17 vertices per side are the established editable brim grid.
 ids=list(range(144,161))+list(range(195,212));rows=[]
 for i in ids:
  v=mesh.vertices[i];old=v.co.copy()
  assert abs(old.x)<=.264 and -.21<old.y<-.14 and 1.48<old.z<1.56,('Unexpected brim topology',i,list(old))
  z=max(old.z,bottom+.009)
  hit,_,_,_=surface.ray_cast(Vector((old.x,-2,z)),Vector((0,1,0)))
  if hit is None:raise RuntimeError('Crown join not found '+str(i))
  v.co=Vector((old.x,hit.y+.012,z))
  rows.append({'vertex':i,'before':list(old),'after':list(v.co),'inset':.012})
 mesh.update();cap['brim_seated_v1']=True
 return {'moved_vertices':len(rows),'crown_inset_m':.012,'vertices':rows}
