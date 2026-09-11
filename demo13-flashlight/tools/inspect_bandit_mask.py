import bpy,bmesh
from pathlib import Path
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Assets/ChibiSurvivor/Bandit/SimpleBandit/BlenderSource~/SimpleBandit.blend'))
o=bpy.data.objects['Bandit_Body'];g=o.vertex_groups['Part_Bandit_Mask'].index
m=bpy.data.materials.new('DiagnosticPlainMask');m.use_nodes=True
p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(.235,.185,.131,1);p.inputs['Roughness'].default_value=1;p.inputs['Specular IOR Level'].default_value=0
o.data.materials.append(m)
for f in o.data.polygons:
 if all(any(w.group==g for w in o.data.vertices[i].groups) for i in f.vertices):f.material_index=len(o.data.materials)-1
sc=bpy.context.scene;sc.render.filepath=str(root.parent/'ArtWork/SimpleBandit/MaskDiagnostic.png');bpy.ops.render.render(write_still=True)
