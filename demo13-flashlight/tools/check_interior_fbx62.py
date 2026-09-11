"""Reimport representative hard-surface, cloth, and reused FBX exports."""
import bpy,json,math,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT.parent/'ArtWork/TownInteriors62'
if '--partition' in sys.argv:OUT=ROOT.parent/'ArtWork/TownInteriorsPartition62'
m=json.loads((OUT/'KitManifest.json').read_text(encoding='utf8'));rows=[]
names=['PawnDisplay62','TreatmentBed62','PrivacyScreen62','MarketCounter62','Reuse_Workbench01','SalvageShelf62']
if '--partition' in sys.argv:names+=['Pawnshop_Floor62','Medical_RearLining62','Furniture_RearClosureRoof62','BlackMarket_LeftPartition62','StorageDoorLeaf62']
for name in names:
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(OUT/'Models'/f'{name}.fbx'))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(meshes)==1,name
 o=meshes[0];bpy.context.view_layer.update();me=o.data
 assert len(me.uv_layers)==1,(name,'UV0 reimport')
 assert all(math.isfinite(c) for v in me.vertices for c in v.co),name
 actual=list(o.dimensions);expected=m['assets'][name]['size'];expected=[expected[0],expected[2],expected[1]]
 err=max(abs(a-b) for a,b in zip(actual,expected));assert err<.005,(name,actual,expected)
 if name=='StorageDoorLeaf62':assert o.location.length<1e-5,(name,'hinge origin moved during FBX export')
 rows.append({'asset':name,'meshCount':1,'UV0':True,'dimensionErrorMetres':err})
(OUT/'FBXValidation.json').write_text(json.dumps(rows,indent=2),encoding='utf8');print('INTERIOR_FBX62_VALIDATED')
