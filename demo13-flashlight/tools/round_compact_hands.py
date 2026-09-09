"""Replace only the animated survivor's hands with round, fingerless meshes.
The existing rig, animation keys and all other geometry are retained unchanged.
"""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated'
OUT=BASE/'RoundHands';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'CompactSurvivor_Animated.blend'))
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig']
hands=bpy.data.objects['Compact_Hands']
def animation_signature():
    values=[]
    for action in sorted(bpy.data.actions,key=lambda a:a.name):
        values.append((action.name,[(fc.data_path,fc.array_index,[(tuple(k.co),k.interpolation) for k in fc.keyframe_points]) for fc in action.fcurves]))
    return hashlib.sha256(repr(values).encode()).hexdigest()
def shape_signature(obj):
    return hashlib.sha256(repr(([tuple(v.co) for v in obj.data.vertices],[tuple(p.vertices) for p in obj.data.polygons])).encode()).hexdigest()
motion_before=animation_signature()
others={o.name:shape_signature(o) for o in scene.objects if o.type=='MESH' and o!=hands}
skin=hands.data.materials[0]
vs=[];fs=[];side_indices={}
N=20;RINGS=12
for side,sign in [('L',1),('R',-1)]:
    center=Vector((sign*.296,-.022,.536))
    radii=Vector((.044,.040,.047));start=len(vs)
    vs.append(center+Vector((0,0,radii.z)))
    for row in range(1,RINGS):
        t=math.pi*row/RINGS
        for col in range(N):
            a=math.tau*col/N
            vs.append(center+Vector((radii.x*math.sin(t)*math.cos(a),radii.y*math.sin(t)*math.sin(a),radii.z*math.cos(t))))
    bottom=len(vs);vs.append(center-Vector((0,0,radii.z)))
    for i in range(N):fs.append((start,start+1+i,start+1+(i+1)%N))
    for row in range(RINGS-2):
        a=start+1+row*N;b=a+N
        for i in range(N):fs.append((a+i,b+i,b+(i+1)%N,a+(i+1)%N))
    a=start+1+(RINGS-2)*N
    for i in range(N):fs.append((a+i,bottom,a+(i+1)%N))
    side_indices[side]=list(range(start,len(vs)))
mesh=bpy.data.meshes.new('Compact_RoundHands')
mesh.from_pydata(vs,[],fs);mesh.update();mesh.materials.append(skin)
for poly in mesh.polygons:poly.use_smooth=True
hands.data=mesh
hands.vertex_groups.clear()
for side,ids in side_indices.items():
    hands.vertex_groups.new(name='Hand.'+side).add(ids,1,'REPLACE')
hands['hand_style']='Round hands: no fingers or thumbs; original wrist rig retained'
assert animation_signature()==motion_before
assert all(shape_signature(bpy.data.objects[n])==sig for n,sig in others.items())
assert len([o for o in scene.objects if o.type=='MESH'])==13
assert all(v.groups and abs(sum(g.weight for g in v.groups)-1)<1e-6 for v in hands.data.vertices)

checks=json.loads((BASE/'AnimationCheck.json').read_text(encoding='utf-8'))
checks['source']=str(BASE/'CompactSurvivor_Animated.blend')
checks['weights']['Hands']={'vertices':len(mesh.vertices),'max_influences':1}
checks['hand_revision']={'style':'Round, no fingers or thumbs','motion_curves_unchanged':True,
    'other_meshes_unchanged':True,'animation_signature':motion_before,
    'note':'Ground/loop checks inherited for unchanged rig/clips; hands attached rigidly to existing hand bones.'}
(OUT/'AnimationCheck.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_start=1;scene.frame_end=91;scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in scene.objects:
    if o.type=='MESH':o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'CompactSurvivor_Animated.fbx'),use_selection=True,
    object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
    bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0,mesh_smooth_type='FACE',apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS')
bpy.ops.export_scene.gltf(filepath=str(OUT/'CompactSurvivor_Animated.glb'),use_selection=True,
    export_format='GLB',export_animations=True,export_animation_mode='ACTIONS')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CompactSurvivor_Animated.blend'))
scene.cycles.samples=24;scene.render.resolution_x=600;scene.render.resolution_y=750
for clip,frame in [('Idle',1),('Walk',7),('Run',6)]:
    rig.animation_data.action=bpy.data.actions[clip];scene.frame_set(frame)
    scene.render.filepath=str(OUT/(clip+'_Pose.png'));bpy.ops.render.render(write_still=True)
print('ROUND_HANDS_COMPLETE',str(OUT),'MOTION_UNCHANGED',animation_signature()==motion_before)
