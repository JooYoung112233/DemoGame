"""Reference-approved survivor; separable face, clothing, cap and accessories."""
from pathlib import Path
import sys
import bmesh
BASE = Path(__file__).with_name('build_reclaimer.py')
# Reuse geometry helpers and unchanged animation skeleton, not the old appearance.
exec(compile(BASE.read_text(encoding='utf-8').split("if part == 'all':")[0], str(BASE), 'exec'))
OUT = ASSETS/'ReferenceSurvivor'
OUT.mkdir(parents=True, exist_ok=True)
LOOK.update(skin=(.52,.355,.22), coat=(.225,.205,.145), bag=(.105,.10,.075), cap=(.17,.155,.105))
for k,v in LOOK.items():
    if k != 'head_scale': M[k] = material('Recovery_'+k,v)
M['white'] = material('Reference_EyeIvory',(.57,.49,.35))
M['iris'] = material('Reference_Iris',(.09,.065,.037))
M['hair'] = material('Reference_Hair',(.055,.042,.026))
M['bandage'] = material('Reference_Bandage',(.34,.285,.205))

def oval(name,center,scale,mat,bone='Head',segments=16,rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=center)
    o=bpy.context.object
    o.name=name
    o.scale=scale
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    o.data.materials.append(M[mat])
    return skin(o,bone)

remodel_body()
# Keep neck skin and backpack straps independent from replaceable clothing.
def extract_materials(source, name, material_names):
    duplicate=source.copy()
    duplicate.data=source.data.copy()
    bpy.context.collection.objects.link(duplicate)
    duplicate.name=name
    for obj,keep_matches in [(duplicate,True),(source,False)]:
        bm=bmesh.new()
        bm.from_mesh(obj.data)
        remove=[f for f in bm.faces if (obj.data.materials[f.material_index].name in material_names) != keep_matches]
        bmesh.ops.delete(bm,geom=remove,context='FACES')
        loose=[v for v in bm.verts if not v.link_faces]
        if loose: bmesh.ops.delete(bm,geom=loose,context='VERTS')
        bm.to_mesh(obj.data)
        bm.free()
    return duplicate

shirt=bpy.data.objects['ChibiSurvivor_Mesh']
extract_materials(shirt,'Reference_Neck',{'Skin_WarmPeach'})
extract_materials(shirt,'Reference_BackpackStraps',{'Straps_DarkLeather'})
clear_part('Recovery_Head')
clear_part('Recovery_Backpack')
clear_part('ChibiSurvivor_Cap')
# Broad cheek volume and a tapered chin instead of the original cube head.
oval('Face_Base',(0,-.004,1.52),(.405,.325,.405),'skin',segments=20,rings=12)
for s in [-1,1]:
    oval('Face_Ear',(s*.398,-.004,1.49),(.074,.063,.105),'skin')
    oval('Face_EarInset',(s*.416,-.053,1.49),(.040,.012,.060),'bandage')
    x=s*.145
    oval('Face_EyeSocket',(x,-.306,1.505),(.091,.020,.107),'bandage')
    oval('Face_EyeWhite',(x,-.326,1.501),(.078,.012,.088),'white')
    oval('Face_Iris',(x,-.338,1.505),(.052,.010,.074),'iris')
    oval('Face_Pupil',(x,-.347,1.515),(.027,.005,.052),'ink')
    box('Face_UpperLid',(x,-.345,1.566),(.158,.023,.025),'hair','Head',.007,rot=(0,s*.05,0))
    box('Face_Brow',(x,-.302,1.639),(.143,.027,.024),'hair','Head',.004,rot=(0,s*.07,0))
oval('Face_Nose',(0,-.341,1.437),(.027,.033,.029),'skin',segments=6,rings=4)
rod('Face_Mouth',(-.043,-.304,1.365),(0,-.312,1.37),.005,'leather','Head',6)
rod('Face_Mouth',(0,-.312,1.37),(.043,-.304,1.365),.005,'leather','Head',6)
box('Face_Bandage',(.262,-.269,1.409),(.076,.017,.046),'bandage','Head',.008,rot=(0,-.25,.25))
combine('Face_','Reference_Face')
# Hair is only a restrained rim beneath the cap.
for s in [-1,1]:
    oval('Hair_Side',(s*.333,.026,1.644),(.063,.244,.169),'hair',segments=10,rings=6)
oval('Hair_Back',(0,.221,1.59),(.331,.112,.224),'hair')
combine('Hair_','Reference_Hair')
build_headwear()
material('Cap_DeepTeal',LOOK['cap'])
material('Cap_Underbrim',(.06,.057,.045))
material('Cap_Badge',(.26,.21,.15))
material('Cap_BadgeMark',(.26,.21,.15))
# Stitched cap patch, separately editable from the face.
for x in [-.073,.073]:
    for z in [1.817,1.883]:
        box('CapDetail_Stitch',(x,-.398,z),(.009,.009,.023),'leather','Head',.001)
combine('CapDetail_','Reference_CapStitches')
# Open folded shirt collar, chest pockets, belt and visible repairs.
for s in [-1,1]:
    box('Shirt_Collar',(s*.085,-.165,1.10),(.113,.047,.126),'coat','Chest',.009,rot=(0,s*.42,0))
    box('Shirt_Pocket',(s*.117,-.185,.965),(.142,.028,.116),'coat','Chest',.009)
    box('Shirt_Flap',(s*.117,-.205,1.012),(.15,.018,.037),'patch','Chest',.005)
    oval('Shirt_Button',(s*.117,-.22,1.009),(.009,.004,.009),'leather','Chest',8,4)
box('Shirt_Undershirt',(0,-.15,1.112),(.08,.018,.075),'dial','Chest',.008)
box('Shirt_ArmPatch',(-.38,-.065,.875),(.065,.14,.074),'cloth','Forearm.R',.008)
box('Trouser_KneePatch',(.132,-.108,.40),(.095,.015,.112),'bandage','Shin.L',.006)
combine('Shirt_','Reference_ShirtDetails')
box('Belt_Band',(0,-.008,.695),(.472,.35,.071),'leather','Hips',.026)
box('Belt_Buckle',(0,-.192,.697),(.078,.023,.060),'metal','Hips',.008)
box('Belt_Inset',(0,-.206,.697),(.045,.004,.034),'leather','Hips',.002)
combine('Belt_','Reference_Belt')
box('Pouch_Body',(-.205,-.215,.665),(.165,.105,.166),'leather','Hips',.018)
box('Pouch_Flap',(-.205,-.276,.718),(.175,.025,.080),'leather','Hips',.012)
box('Pouch_Clasp',(-.205,-.294,.686),(.037,.014,.041),'metal','Hips',.004)
combine('Pouch_','Reference_BeltPouch')
# Simple patched flap backpack matching the reference silhouette.
box('Bag_Body',(0,.27,.919),(.425,.235,.454),'bag','Backpack',.048)
box('Bag_Flap',(0,.405,1.01),(.405,.055,.257),'cloth','Backpack',.031)
box('Bag_Patch',(.087,.440,.965),(.095,.014,.085),'patch','Backpack',.006,rot=(0,.25,0))
box('Bag_Fastener',(0,.421,.819),(.05,.031,.178),'leather','Backpack',.008)
box('Bag_Buckle',(0,.444,.867),(.068,.012,.048),'metal','Backpack',.005)
for s in [-1,1]:
    box('Bag_Side',(s*.222,.281,.825),(.075,.19,.167),'bag','Backpack',.019)
    rod('Bag_Handle',(s*.069,.25,1.143),(s*.055,.25,1.196),.012,'leather','Backpack')
rod('Bag_Handle',(-.055,.25,1.196),(.055,.25,1.196),.012,'leather','Backpack')
for x in [.05,.12]:
    for z in [.932,.999]: box('Bag_Stitch',(x,.452,z),(.007,.007,.025),'leather','Backpack',.001)
combine('Bag_','Reference_Backpack')
build_watch()
material('Jacket_Sage',LOOK['coat'])
material('Jacket_Shadow',(.16,.15,.105))
material('Trousers_Slate',(.055,.059,.054))
material('Backpack_Ochre',LOOK['bag'])
material('Hardware_Brass',(.22,.20,.155))

def join_slot(names,name,slot):
    objects=[bpy.data.objects[n] for n in names]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects: o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.join()
    o=bpy.context.object
    o.name=name
    o['equipment_slot']=slot
    o['shared_skeleton']='ChibiSurvivor'
    return o

join_slot(['ChibiSurvivor_Mesh','Reference_ShirtDetails'],'Reference_Shirt','upper_body')
join_slot(['Recovery_Trousers','Trouser_KneePatch'],'Reference_Trousers','lower_body')
join_slot(['ChibiSurvivor_Cap','Reference_CapStitches'],'Reference_Cap','headwear')
join_slot(['Reference_Backpack','Reference_BackpackStraps'],'Reference_Backpack','backpack')
for name,slot in [('Reference_Belt','belt'),('Reference_BeltPouch','belt_pouch'),('Recovery_Wristwatch','wrist'),('Recovery_Boots','footwear')]:
    bpy.data.objects[name]['equipment_slot']=slot

# Add a matching left-hand grip without changing any existing animation tracks.
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.object.mode_set(mode='EDIT')
if not rig.data.edit_bones.get('HandSocket.L'):
    socket=rig.data.edit_bones.new('HandSocket.L')
    socket.head=(.39,-.035,.65)
    socket.tail=(.39,-.035,.75)
    socket.parent=rig.data.edit_bones['Hand.L']
bpy.ops.object.mode_set(mode='OBJECT')
rig['animation_scope']='Idle, Walk, Run; combat clips pending'
rig['equipment_setup']='Separate skinned slots; runtime equipment switching pending'

scene=bpy.context.scene
rig.animation_data.action=bpy.data.actions['Idle']
scene.frame_set(1)
meshes=[o for o in rig.children if o.type=='MESH']
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active=rig
# ── 앰비언트 오클루전을 버텍스 컬러에 굽는다 ─────────────────────────────
# 왜 버텍스 AO인가: 실시간 SSAO는 게임플레이 거리에서 캐릭터가 화면에 작게 잡혀
# 틈새(모자챙 아래·턱밑·가방과 등 사이)가 몇 픽셀밖에 안 되므로 사실상 안 잡힌다.
# 구워두면 런타임 비용이 0이고 거리·해상도와 무관하게 일정하다.
# 도메인은 CORNER — 면마다 값을 따로 가져 각진 로우폴리에서 경계가 뭉개지지 않는다.
scene=bpy.context.scene
scene.render.engine='CYCLES'
try:
    scene.cycles.device='CPU'
    scene.cycles.samples=128
    scene.cycles.use_denoising=False
except Exception as exc:
    print('AO_BAKE_CYCLES_SETUP_SKIPPED',exc)
scene.render.bake.target='VERTEX_COLORS'
scene.render.bake.use_selected_to_active=False
scene.render.bake.margin=0

ao_meshes=[o for o in rig.children if o.type=='MESH']
for o in ao_meshes:
    me=o.data
    for existing in list(me.color_attributes):
        if existing.name=='AO':
            me.color_attributes.remove(existing)
    attr=me.color_attributes.new(name='AO',type='BYTE_COLOR',domain='CORNER')
    me.color_attributes.active_color=attr
    me.color_attributes.render_color_index=me.color_attributes.find('AO')

bpy.ops.object.select_all(action='DESELECT')
for o in ao_meshes: o.select_set(True)
bpy.context.view_layer.objects.active=ao_meshes[0]
try:
    bpy.ops.object.bake(type='AO')
    print('AO_BAKE_OK',len(ao_meshes))
except Exception as exc:
    print('AO_BAKE_FAILED',exc)

# ⚠️ 베이크가 선택 상태를 덮어썼다. 익스포트는 use_selection=True이므로 여기서
#    아마추어+메시 선택을 되돌리지 않으면 **리그가 빠져 스키닝이 통째로 사라진다.**
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True)
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active=rig

bpy.ops.export_scene.fbx(filepath=str(OUT/'ReferenceSurvivor.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,mesh_smooth_type='FACE',colors_type='SRGB')
bpy.ops.export_scene.gltf(filepath=str(OUT/'ReferenceSurvivor.glb'),use_selection=True,export_format='GLB',export_animations=True,export_animation_mode='ACTIONS')
material('Backdrop',(.026,.028,.03))
material('Stage',(.075,.072,.063))
scene.camera.location=(2.6,-5,2.6)
target=Vector((0,0,1.02))
scene.camera.rotation_euler=(target-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera.data.ortho_scale=2.55
scene.render.resolution_x=900
scene.render.resolution_y=1000
scene.render.resolution_percentage=100
scene.cycles.samples=32
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ReferenceSurvivor.blend'))
scene.render.filepath=str(OUT/'Preview.png')
bpy.ops.render.render(write_still=True)
scene.camera.location=(-2.6,5,2.6)
scene.camera.rotation_euler=(target-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(OUT/'PreviewBack.png')
bpy.ops.render.render(write_still=True)
print('REFERENCE_MODEL_GENERATED',str(OUT))
