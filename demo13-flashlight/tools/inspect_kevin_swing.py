import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Library/KevinMeleeSource/HumanM_Melee.blend'))
r=bpy.data.objects['Rig'];a=bpy.data.actions['HumanM@Attack2H01'];r.animation_data.action=a
for t in list(r.animation_data.nla_tracks):r.animation_data.nla_tracks.remove(t)
if a.slots:r.animation_data.action_slot=a.slots[0]
rows=[]
for f in [1,9,17,21,25,29,33,41,49]:
 bpy.context.scene.frame_set(f);bpy.context.view_layer.update()
 row={'f':f}
 for n in ['B-chest','B-head','B-handProp.R','B-handProp.L','B-upperArm.R','B-forearm.R','B-hand.R']:
  m=r.pose.bones[n].matrix;row[n]={'pos':list(m.translation),'axis':list(m.to_quaternion()@Vector((0,1,0)))}
 o=bpy.data.objects.get('WeaponGS')
 if o:
  dg=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(dg);me=ev.to_mesh();pts=[ev.matrix_world@v.co for v in me.vertices];ev.to_mesh_clear()
  row['weapon']={'parent':str(o.parent),'bone':o.parent_bone,'constraints':[(c.name,c.type) for c in o.constraints],'bounds':[[min(p[i] for p in pts),max(p[i] for p in pts)] for i in range(3)]}
 rows.append(row)
(root/'Library/kevin-source-inspect.json').write_text(json.dumps(rows,indent=2))
print(json.dumps(rows))
