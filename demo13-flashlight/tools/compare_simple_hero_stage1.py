import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/SimpleHero_Stage1.blend'))
sc=bpy.context.scene;root=bpy.data.objects['SimpleHero_Stage1']
right=Vector((.946,.324,0));root.location=right*.58
source=ROOT/'Library/CodexBlender/SimpleHeroFaceRatioBefore/SimpleHero_Stage1.blend'
with bpy.data.libraries.load(str(source),link=False) as (src,dst):dst.objects=[n for n in src.objects if n.startswith('Study_')]
for o in dst.objects:
 if o is not None and o.type=='MESH':
  world=o.matrix_basis.copy();o.parent=None;o.matrix_world=world;o.location-=right*.58;sc.collection.objects.link(o)
  for m in o.data.materials:
   if m and m.use_nodes:
    for node in m.node_tree.nodes:
     if node.type=='TEX_IMAGE' and node.image:
      filename=Path(node.image.filepath).name
      resolved=OUT/'Textures'/filename
      if not resolved.exists():resolved=OUT/filename
      if not resolved.exists():raise RuntimeError('Missing comparison texture '+filename)
      node.image.filepath=str(resolved);node.image.reload()
cam=sc.camera;cam.location=(2.3,-6,3.1);cam.rotation_euler=(Vector((0,0,.92))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=3.25
sc.render.resolution_x=1500;sc.render.resolution_y=1050;sc.render.resolution_percentage=100;sc.render.filepath=str(OUT/'Stage1_Comparison.png');bpy.ops.render.render(write_still=True)
# Reimport the actual FBX into a fresh scene, checking exported geometry is valid.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT/'SimpleHero_Stage1.fbx'))
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
assert len(objects)==14,len(objects)
assert all(all(math.isfinite(x) for x in v.co) for o in objects for v in o.data.vertices)
assert all(o.data.uv_layers.active for o in objects)
assert all(o.data.materials and all(m is not None for m in o.data.materials) for o in objects)
head=next(o for o in objects if o.name=='Study_Head')
assert any(m and m.name.startswith('SimpleHero_Surface') for m in head.data.materials)
assert all(0<=uv.uv.x<=1 and 0<=uv.uv.y<=1 for o in objects for uv in o.data.uv_layers.active.data)
assert len({m for o in objects for m in o.data.materials})==1
report={'fbx_reimport_passed':True,'mesh_count':len(objects),'finite_vertices':True,'uv_and_materials_present':True,'uv_bounds_valid':True,'materials':1,'stage':1,'revision':4}
(OUT/'Stage1ExportCheck.json').write_text(json.dumps(report,indent=2));print('STAGE1_EXPORT_VERIFIED',report)
