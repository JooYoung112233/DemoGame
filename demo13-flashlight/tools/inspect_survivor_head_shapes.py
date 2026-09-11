import bpy,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Assets/ChibiSurvivor/Player/DarkSurvivorReview/Stage01_Silhouette/Revision02/DarkSurvivor_Stage01_Rev02.blend'))
for name in ['Study_Head','Study_Hair','Study_Cap','Study_ScarfWrap','Study_ScarfFront']:
 o=bpy.data.objects[name];adj=[[] for v in o.data.vertices]
 for e in o.data.edges:
  a,b=e.vertices;adj[a].append(b);adj[b].append(a)
 seen=set();groups=[]
 for i in range(len(adj)):
  if i in seen:continue
  stack=[i];seen.add(i);ids=[]
  while stack:
   j=stack.pop();ids.append(j)
   for k in adj[j]:
    if k not in seen:seen.add(k);stack.append(k)
  ps=[o.matrix_world@o.data.vertices[k].co for k in ids]
  groups.append({'n':len(ids),'min':[min(p[j] for p in ps) for j in range(3)],'max':[max(p[j] for p in ps) for j in range(3)]})
 print(name,json.dumps(groups),flush=True)
