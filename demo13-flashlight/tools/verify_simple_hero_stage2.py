import bpy,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(OUT/'SimpleHero_Stage2.fbx'))
parts=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(parts)==17
materials={m for o in parts for m in o.data.materials};assert len(materials)==2
assert all(o.data.uv_layers.active and len(o.data.uv_layers.active.data)==len(o.data.loops) for o in parts)
assert all(math.isfinite(c) for o in parts for v in o.data.vertices for c in v.co)
assert all(0<=u.uv.x<=1 and 0<=u.uv.y<=1 for o in parts for u in o.data.uv_layers.active.data)
for name in ['Gear_Scarf','Gear_Backpack','Gear_Charm']:assert bpy.data.objects.get(name)
images=[]
for m in materials:
 assert m.use_nodes
 nodes=[n for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
 assert nodes,'Missing texture '+m.name
 for n in nodes:
  path=Path(bpy.path.abspath(n.image.filepath));assert path.exists(),str(path);images.append(str(path))
report={'fbx_reimport_passed':True,'meshes':17,'materials':2,'accessories_separate':True,'uv_bounds_valid':True,'texture_paths_valid':True,'textures':images}
(OUT/'Stage2ExportCheck.json').write_text(json.dumps(report,indent=2));print('STAGE2_EXPORT_VERIFIED',report)
