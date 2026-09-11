import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT.parent/'ArtWork/CharacterProportionC/BlenderSource~/Bandit_C.blend'))
o=bpy.data.objects['Bandit_Body'];me=o.data;links=[set() for v in me.vertices]
for e in me.edges:a,b=e.vertices;links[a].add(b);links[b].add(a)
seen=set();rows=[]
for v in me.vertices:
 if v.index in seen:continue
 stack=[v.index];seen.add(v.index);ids=[]
 while stack:
  i=stack.pop();ids.append(i)
  for j in links[i]:
   if j not in seen:seen.add(j);stack.append(j)
 ps=[me.vertices[i].co for i in ids];weights={}
 for i in ids:
  for g in me.vertices[i].groups:weights[o.vertex_groups[g.group].name]=weights.get(o.vertex_groups[g.group].name,0)+g.weight
 rows.append({'count':len(ids),'first':ids[0],'min':[min(p[k] for p in ps) for k in range(3)],'max':[max(p[k] for p in ps) for k in range(3)],'weights':{n:round(w/len(ids),3) for n,w in weights.items()}})
print('COMPONENTS',json.dumps(rows))
