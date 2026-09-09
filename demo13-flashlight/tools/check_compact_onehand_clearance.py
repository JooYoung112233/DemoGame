"""Check the preview blade against evaluated character surfaces over the slash."""
import bpy, json, sys
from pathlib import Path
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated/Combat'
fluid='--fluid' in sys.argv
thrust='--stab' in sys.argv or fluid
if thrust:OUT=OUT/'Thrust'
if fluid:OUT=ROOT/'Assets/ChibiSurvivor/Player'
clip='Attack_OneHand_Thrust' if thrust else 'Attack_OneHand'
end=28 if fluid else 34 if thrust else 40
bpy.ops.wm.open_mainfile(filepath=str(OUT/(clip+'_Preview.blend')))
scene=bpy.context.scene
blade=bpy.data.objects['Preview_Knife_Blade']
parts=[o for o in scene.objects if o.type=='MESH' and o.name.startswith('Compact_')]
def tree(o,dg):
    ev=o.evaluated_get(dg);mesh=ev.to_mesh()
    result=BVHTree.FromPolygons([ev.matrix_world @ v.co for v in mesh.vertices],
        [tuple(p.vertices) for p in mesh.polygons])
    ev.to_mesh_clear()
    return result
hits=[]
for frame in range(1,end+1):
    scene.frame_set(frame);bpy.context.view_layer.update()
    dg=bpy.context.evaluated_depsgraph_get()
    weapon=tree(blade,dg)
    for part in parts:
        overlap=weapon.overlap(tree(part,dg))
        if overlap:hits.append({'frame':frame,'part':part.name,'overlaps':len(overlap)})
report={'scope':'Preview blade triangle-surface intersections; not a gameplay collider check',
        'frames':end,'parts':len(parts),'intersections':hits,'unity_checked':False}
(OUT/'BladeClearanceCheck.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
assert not hits,hits
print('BLADE_CLEARANCE_OK',json.dumps(report))
