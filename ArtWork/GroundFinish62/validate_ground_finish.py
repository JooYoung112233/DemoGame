"""Independent .blend/FBX checks; no Unity process or asset import."""
import bpy, json, math
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent
expected=json.loads((OUT/'ModelValidation.json').read_text())
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/GroundFinish62.blend'))
scene=bpy.context.scene
camera=scene.camera
pitch=math.degrees(math.asin(abs((camera.rotation_euler.to_quaternion()@Vector((0,0,-1))).z)))
assert abs(pitch-62)<.001, pitch
images=[im for im in bpy.data.images if im.source=='FILE']
assert all(im.packed_file for im in images), 'Unpacked source texture'
sources={name:bpy.data.collections[name+'_Editable'] for name in expected['newAssets']}
for name,col in sources.items():
    own=bpy.data.scenes['Asset_'+name]
    assert col.name in own.collection.children
    for obj in col.objects:
        assert len(obj.data.uv_layers)==1
        assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
        assert all(p.area>1e-12 for p in obj.data.polygons)
        if any(m.name.endswith(('Soil','DryDust')) for m in obj.data.materials):
            assert obj.data.color_attributes.get('GroundFade') is not None
        assert not obj.modifiers, (name,obj.name,'Unapplied modifier')
results={}
for name,spec in expected['newAssets'].items():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(OUT/'Models'/f'{name}.fbx'))
    objs=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert len(objs)==1,(name,len(objs))
    obj=objs[0];me=obj.data;me.calc_loop_triangles()
    assert len(me.loop_triangles)==spec['triangles'],name
    verts=[obj.matrix_world@v.co for v in me.vertices]
    bounds=[(min(p[a] for p in verts),max(p[a] for p in verts)) for a in range(3)]
    delta=max(abs(bounds[a][i]-spec['boundsXYZ'][a][i]) for a in range(3) for i in range(2))
    assert delta<1e-4,(name,delta)
    assert len(me.uv_layers)==1
    assert all(t.area>1e-11 for t in me.loop_triangles)
    has_fade=me.color_attributes.get('GroundFade') is not None
    if name in ('BrokenPaving_A','BrokenPaving_B','DirtGravel_Transition','WallWeeds_Dust'):
        assert has_fade, (name,'FBX lost vertex alpha')
        layer=me.color_attributes['GroundFade'];alphas=[d.color[3] for d in layer.data]
        assert min(alphas)<.01 and max(alphas)>.2
    results[name]={'meshCount':1,'triangles':len(me.loop_triangles),'boundsMaxDeltaMetres':delta,
                   'UV0':True,'GroundFadeExported':has_fade,'passed':True}
(OUT/'FBXValidation.json').write_text(json.dumps({'cameraPitch':pitch,'packedImageCount':len(images),
    'editableAssetScenes':6,'exports':results,'unityImported':False},indent=2))
print('GROUND_FBX_VALIDATION_PASSED')
