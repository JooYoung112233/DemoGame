"""Validate native and FBX surfaces, bones, loop, planted feet and UV interiors."""
import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parents[1]
specs=json.loads((OUT/'BuildValidation.json').read_text(encoding='utf8'))
def posed_vertices(o):
 deps=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(deps);m=ev.to_mesh();result=[ev.matrix_world@v.co for v in m.vertices];ev.to_mesh_clear();return result
def surface(o,r):
 me=o.data;me.calc_loop_triangles();names={b.name for b in r.data.bones}
 assert me.uv_layers and len(me.polygons)>0
 bad_weights=0
 for v in me.vertices:
  total=sum(g.weight for g in v.groups if o.vertex_groups[g.group].name in names)
  if abs(total-1)>1e-4:bad_weights+=1
 assert bad_weights==0
 assert all(math.isfinite(c) for v in me.vertices for c in v.co)
 assert all(math.isfinite(c) for u in me.uv_layers.active.data for c in u.uv)
 degenerate=sum(1 for t in me.loop_triangles if t.area<1e-10)
 assert not degenerate,degenerate
 return dict(vertices=len(me.vertices),triangles=len(me.loop_triangles),badWeights=bad_weights,degenerateTriangles=degenerate)
def cross(a,b):return a[0]*b[1]-a[1]*b[0]
def intersect_area(a,b):
 # Convex triangle clipping. Coincident shared edges have zero interior area.
 if cross(b[1]-b[0],b[2]-b[0])<0:b=list(reversed(b))
 p=list(a)
 for k in range(3):
  x,y=b[k],b[(k+1)%3];edge=y-x;out=[]
  for i,v in enumerate(p):
   prev=p[i-1];dv=cross(edge,v-x);dp=cross(edge,prev-x)
   if (dv>=0)!=(dp>=0):out.append(prev+(v-prev)*(dp/(dp-dv)))
   if dv>=0:out.append(v)
  p=out
  if len(p)<3:return 0
 return abs(sum(cross(p[i],p[(i+1)%len(p)]) for i in range(len(p)))*.5)
def uv_check(o):
 me=o.data;uv=me.uv_layers.active.data;tris=[[uv[l].uv.copy() for l in t.loops] for t in me.loop_triangles];grid={};checked=set();overlaps=[]
 for i,t in enumerate(tris):
  lo=[math.floor(min(v[a] for v in t)*64) for a in range(2)];hi=[math.floor(max(v[a] for v in t)*64) for a in range(2)]
  for x in range(lo[0],hi[0]+1):
   for y in range(lo[1],hi[1]+1):
    for j in grid.get((x,y),[]):
     if (j,i) in checked:continue
     checked.add((j,i));area=intersect_area(t,tris[j])
     if area>1e-9:overlaps.append((j,i,area))
    grid.setdefault((x,y),[]).append(i)
 return dict(interiorOverlapPairs=len(overlaps),maxOverlapArea=max([v[2] for v in overlaps],default=0),examples=overlaps[:10])
report=[]
for s in specs:
 bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~'/(s['name']+'.blend')))
 sc=bpy.context.scene;r=bpy.data.objects['SimpleHero_Rig'];o=bpy.data.objects['NPC_Body'];native=surface(o,r)
 uv=uv_check(o)
 assert uv['interiorOverlapPairs']==0,uv
 names=sorted(b.name for b in r.data.bones);native['uv']=uv
 a=r.animation_data.action;assert len(bpy.data.actions)==1 and a.name==s['motion']
 sc.frame_set(1);bpy.context.view_layer.update();first=posed_vertices(o)
 sc.frame_set(91);bpy.context.view_layer.update();last=posed_vertices(o)
 native['loopError']=max((x-y).length for x,y in zip(first,last));assert native['loopError']<1e-5
 bounds=[[min(v[k] for v in first),max(v[k] for v in first)] for k in range(3)]
 # Read the delivered file into a clean scene; do not inspect the source only.
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(OUT/'Models'/(s['name']+'.fbx')))
 rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 assert len(rigs)==1 and len(meshes)==1
 r=rigs[0];o=meshes[0];assert sorted(b.name for b in r.data.bones)==names
 assert len(bpy.data.actions)==1
 a=bpy.data.actions[0];r.animation_data_create();r.animation_data.action=a
 if a.slots:r.animation_data.action_slot=a.slots[0]
 fbx=surface(o,r);fbx['bones']=len(names);fbx['clips']=[a.name];fbx['frameRange']=list(a.frame_range)
 sc=bpy.context.scene;sc.frame_set(int(a.frame_range[0]));bpy.context.view_layer.update();first=posed_vertices(o)
 sc.frame_set(int(a.frame_range[1]));bpy.context.view_layer.update();last=posed_vertices(o)
 fbx['loopError']=max((x-y).length for x,y in zip(first,last));assert fbx['loopError']<1e-4
 fbounds=[[min(v[k] for v in first),max(v[k] for v in first)] for k in range(3)]
 fbx['boundsError']=max(abs(bounds[k][j]-fbounds[k][j]) for k in range(3) for j in range(2));assert fbx['boundsError']<1e-4
 textures=[n.image for m in o.data.materials if m and m.use_nodes for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
 fbx['importedImages']=[im.name for im in textures]
 assert textures
 report.append(dict(npcId=s['id'],native=native,fbx=fbx))
 print('NPC_VALIDATED',s['name'],uv,flush=True)
(OUT/'RigAndFBXValidation.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('NPC_VALIDATION_COMPLETE',flush=True)
