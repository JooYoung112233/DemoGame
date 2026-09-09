"""Proportion-only variant of the user's chosen ThreeHeadSurvivor render.
The head is translated intact. Keep equipment slots and the source asset.
"""
import bpy, math, json, shutil
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
ARCHIVE=ROOT/'ArtSource/CharacterArchive/2026-09-08'
SOURCE=ARCHIVE/'Assets/ChibiSurvivor/ThreeHeadSurvivor/ThreeHeadSurvivor.blend'
OUT=ARCHIVE/'Assets/ChibiSurvivor/CompactSurvivor'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene=bpy.context.scene
character=[o for o in scene.objects if o.type=='MESH']
assert len(character)==13, 'Unexpected source module count'
assert not any(o.type=='ARMATURE' for o in scene.objects), 'This pass requires the static source'

WAIST=.768
CHIN=1.200
LOWER_SCALE=.75
TORSO_SCALE=.90
NEW_WAIST=WAIST*LOWER_SCALE
NEW_CHIN=NEW_WAIST+(CHIN-WAIST)*TORSO_SCALE
HEAD_SHIFT=NEW_CHIN-CHIN
def remap(z):
    if z<=WAIST:return z*LOWER_SCALE
    if z<=CHIN:return NEW_WAIST+(z-WAIST)*TORSO_SCALE
    return z+HEAD_SHIFT

originals=[]
before={}
for o in character:
    assert all(abs(o.matrix_world[r][c]-(1 if r==c else 0))<1e-5 for r in range(4) for c in range(4)), 'Expected applied source transforms'
    originals.append((o.name,o.data.copy(),dict(o.items())))
    before[o.name]=[v.co.copy() for v in o.data.vertices]

rigid_parts={
    'Head':HEAD_SHIFT,'FaceDetails':HEAD_SHIFT,'Hair':HEAD_SHIFT,'Cap':HEAD_SHIFT,
    'Hands':remap(.752)-.752,
    'Watch':remap(.807)-.807,
    'Belt':NEW_WAIST-WAIST,
    'BeltPouch':NEW_WAIST-WAIST,
}
for o in character:
    part=o['equipment_slot']
    for v in o.data.vertices:
        v.co.z=v.co.z+rigid_parts[part] if part in rigid_parts else remap(v.co.z)
    o.data.update()
    o.name='Compact_'+part
    o['stage']='Static proportion revision; rig/animation and Unity hookup pending'
    o['source_asset']=str(SOURCE)
    o['proportion_edit']='Rigid vertical move' if part in rigid_parts else 'Vertical piecewise remap: lower .75, torso .90'

# Meaningful local checks: face dimensions, topology, modularity, contacts.
head_errors=[]
for o in character:
    part=o['equipment_slot'];old=before['ThreeHead_'+part]
    assert len(old)==len(o.data.vertices), 'Topology count changed'
    for i,v in enumerate(o.data.vertices):
        assert abs(v.co.x-old[i].x)<1e-6 and abs(v.co.y-old[i].y)<1e-6, 'Unrequested width/depth change'
        if part in {'Head','FaceDetails','Hair','Cap'}:
            head_errors.append(abs(v.co.z-old[i].z-HEAD_SHIFT))
assert max(head_errors)<1e-6, 'Head/face geometry was deformed'
zmin=min(v.co.z for o in character for v in o.data.vertices)
zmax=max(v.co.z for o in character for v in o.data.vertices)
assert abs(zmin)<1e-6 and abs(zmax-1.5648)<1e-4
ratio=(zmax-zmin)/.600
report={'source':str(SOURCE),'stage':'Static proportion review only',
    'height_before':1.8,'height_after':zmax-zmin,'cap_inclusive_head_height':.6,
    'head_units_before':3,'head_units_after':ratio,'torso_length_scale':TORSO_SCALE,
    'waist_to_floor_scale':LOWER_SCALE,'head_face_hair_cap_translation_only':True,
    'width_depth_unchanged':True,'equipment_slots_preserved':len(character),
    'hands_watch_belt_pouch_rigid':True,'animation_or_unity_integration':False}
(OUT/'ProportionCheck.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
reference=ROOT/'Assets/ChibiSurvivor/KakaoTalk_20260908_170010839.png'
if reference.exists() and not (OUT/'ChosenReference.png').exists():shutil.copy2(reference,OUT/'ChosenReference.png')
scene['proportion']='2.608 cap-inclusive head units; source head unmodified; torso -10%, waist-to-floor -25%'
scene['stage']='Static proportion approval, no rig/animation or Unity replacement'
scene['source_asset']=str(SOURCE)
scene.cycles.samples=48
scene.render.resolution_x=880;scene.render.resolution_y=1100
scene.render.resolution_percentage=100
cam=scene.camera
target=Vector((0,0,zmax/2))
cam.data.ortho_scale=2.04*(zmax/1.8)
def camera(offset):
    cam.location=target+Vector(offset)
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
camera((3,-6,.75))
bpy.ops.object.select_all(action='DESELECT')
for o in character:o.select_set(True)
bpy.context.view_layer.objects.active=character[0]
bpy.ops.export_scene.fbx(filepath=str(OUT/'CompactSurvivor.fbx'),use_selection=True,
    object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
bpy.ops.export_scene.gltf(filepath=str(OUT/'CompactSurvivor.glb'),use_selection=True,export_format='GLB')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CompactSurvivor.blend'))
for name,offset in [('Quarter',(3,-6,.75)),('Front',(0,-6,.3)),('Back',(-3,6,.75))]:
    camera(offset);scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)

# Native Blender front comparison: same scale and floor, no raster editing.
comparison=bpy.data.collections.new('Original_Comparison_Only');scene.collection.children.link(comparison)
for name,data,props in originals:
    o=bpy.data.objects.new('Original_'+name,data);comparison.objects.link(o);o.location.x=-.42
    for k,v in props.items():o[k]=v
for o in character:o.location.x+=.42
font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
labelmat=bpy.data.materials.new('Comparison_Label');labelmat.use_nodes=True
nodes=labelmat.node_tree.nodes;nodes.clear()
e=nodes.new('ShaderNodeEmission');e.inputs[0].default_value=(.75,.78,.8,1)
out=nodes.new('ShaderNodeOutputMaterial');labelmat.node_tree.links.new(e.outputs[0],out.inputs[0])
def label(body,x):
    data=bpy.data.curves.new('Label','FONT');data.body=body;data.font=font;data.size=.038
    obj=bpy.data.objects.new('Label',data);scene.collection.objects.link(obj)
    obj.location=(x,-.6,1.92);obj.rotation_euler=(math.pi/2,0,0);data.materials.append(labelmat)
label('원본 · 3등신',-.67);label('조정 · 약 2.6등신',.11)
cam.location=(0,-6,.95);cam.rotation_euler=(math.pi/2,0,0);cam.data.ortho_scale=2.75
scene.render.resolution_x=1500;scene.render.resolution_y=1200
scene.render.filepath=str(OUT/'Comparison.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ProportionComparison.blend'))
bpy.ops.render.render(write_still=True)
print('COMPACT_SURVIVOR_COMPLETE',str(OUT),'HEAD_UNITS',ratio,'HEAD_PRESERVED',max(head_errors)<1e-6)
