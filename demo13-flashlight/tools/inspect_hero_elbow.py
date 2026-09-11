import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Library/CodexBlender/AxeReview/FittedReview.blend'))
rig=bpy.data.objects['DarkSurvivor_Rig']
for n in ['Hero_Sleeve_1','Hero_RolledSleeve_1','Hero_Forearm_1']:
 o=bpy.data.objects[n];print('ELBOW_MESH',n,'bounds',[(min(v.co[i] for v in o.data.vertices),max(v.co[i] for v in o.data.vertices)) for i in range(3)])
 print('RINGS',json.dumps([{'center':[sum(v.co[i] for v in o.data.vertices[j:j+12])/len(o.data.vertices[j:j+12]) for i in range(3)],'weight':[(o.vertex_groups[g.group].name,g.weight) for g in o.data.vertices[j].groups]} for j in range(0,len(o.data.vertices),12)]))
