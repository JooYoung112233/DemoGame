"""Render actual Blender animation movies and a pose contact sheet.
Preview-only forward travel matches planted feet; delivered actions stay in-place.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector, Matrix
from bpy.app.handlers import persistent
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated'
if '--round-hands' in sys.argv:OUT=OUT/'RoundHands'
SOURCE=OUT/'CompactSurvivor_Animated.blend'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig']
stats=json.loads((OUT/'AnimationCheck.json').read_text(encoding='utf-8'))
meshes=[o for o in scene.objects if o.type=='MESH']
scene.cycles.samples=16

# Inspect several phases together using evaluated, baked mesh snapshots.
rotation=Matrix.Rotation(-math.atan(.5),4,'Z')
for row,(clip,frames) in enumerate([('Walk',[1,6,11,16,21,26]),('Run',[1,4,7,11,14,17])]):
    rig.animation_data.action=bpy.data.actions[clip]
    for col,frame in enumerate(frames):
        scene.frame_set(frame);bpy.context.view_layer.update()
        dg=bpy.context.evaluated_depsgraph_get()
        for obj in meshes:
            ev=obj.evaluated_get(dg)
            mesh=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=dg)
            mesh.transform(rotation)
            snap=bpy.data.objects.new(clip+'_Snapshot',mesh);scene.collection.objects.link(snap)
            snap.location=((col-2.5)*.85,0,1.8 if row==0 else 0)
for o in meshes:o.hide_render=True
font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
labelmat=bpy.data.materials.new('PoseSheet_Label');labelmat.use_nodes=True
n=labelmat.node_tree.nodes;n.clear();e=n.new('ShaderNodeEmission');e.inputs[0].default_value=(.75,.78,.8,1)
out=n.new('ShaderNodeOutputMaterial');labelmat.node_tree.links.new(e.outputs[0],out.inputs[0])
for body,z in [('걷기 · Walk',3.46),('뛰기 · Run',1.66)]:
    data=bpy.data.curves.new('Label','FONT');data.body=body;data.font=font;data.size=.10
    o=bpy.data.objects.new('Label',data);scene.collection.objects.link(o);o.location=(-2.4,-.7,z)
    o.rotation_euler=(math.pi/2,0,0);data.materials.append(labelmat)
scene.camera.location=(0,-10,1.7);scene.camera.rotation_euler=(math.pi/2,0,0)
scene.camera.data.ortho_scale=5.8
scene.render.resolution_x=1800;scene.render.resolution_y=1200
scene.render.filepath=str(OUT/'MotionPoses.png');bpy.ops.render.render(write_still=True)

# Movie stage starts from the clean delivered asset, not the pose sheet.
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig']
scene.render.engine='BLENDER_EEVEE_NEXT'
if hasattr(scene.eevee,'taa_render_samples'):scene.eevee.taa_render_samples=32
scene.render.use_persistent_data=True
scene.render.resolution_x=480;scene.render.resolution_y=600
scene.render.resolution_percentage=100
scene.render.fps=15;scene.frame_step=2 # sample 30 fps source keys, output 15 fps
scene.render.image_settings.file_format='FFMPEG'
scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264'
scene.render.ffmpeg.constant_rate_factor='HIGH';scene.render.ffmpeg.ffmpeg_preset='GOOD'
scene.render.ffmpeg.audio_codec='NONE'

floor_mat=bpy.data.materials.new('Preview_Ground');floor_mat.use_nodes=True
bs=floor_mat.node_tree.nodes['Principled BSDF'];bs.inputs['Roughness'].default_value=.95
tex=floor_mat.node_tree.nodes.new('ShaderNodeTexChecker')
tex.inputs['Color1'].default_value=(.05,.053,.045,1)
tex.inputs['Color2'].default_value=(.075,.078,.065,1);tex.inputs['Scale'].default_value=3
coord=floor_mat.node_tree.nodes.new('ShaderNodeTexCoord')
floor_mat.node_tree.links.new(coord.outputs['Object'],tex.inputs['Vector'])
floor_mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
bpy.ops.mesh.primitive_plane_add(size=40,location=(0,-6,-.002))
bpy.context.object.name='Preview_Ground_Not_Exported';bpy.context.object.data.materials.append(floor_mat)
cam=scene.camera
cam_base=cam.location.copy()
light_bases={o:o.location.copy() for o in scene.objects if o.type=='LIGHT'}
current_speed=0
@persistent
def follow_preview(sc,*args):
    shift=-current_speed*(sc.frame_current-1)/30
    rig.location.y=shift
    cam.location=cam_base+Vector((0,shift,0))
    for light,base in light_bases.items():light.location=base+Vector((0,shift,0))
bpy.app.handlers.frame_change_pre.append(follow_preview)
movies=[]
for clip,repeats in [('Idle',1),('Walk',3),('Run',5)]:
    action=bpy.data.actions[clip];rig.animation_data.action=action
    for fc in action.fcurves:
        if not any(m.type=='CYCLES' for m in fc.modifiers):fc.modifiers.new('CYCLES')
    current_speed=stats['clips'][clip]['nominal_forward_speed_mps']
    count=stats['clips'][clip]['end']-1
    scene.frame_start=1;scene.frame_end=count*repeats-1;scene.frame_set(1)
    # Render to a fixed path without Blender's automatic frame-range suffix.
    scene.render.filepath=str(OUT/(clip+'.mp4'))
    bpy.ops.render.render(animation=True)
    movie=OUT/(clip+'.mp4')
    matches=[p for p in OUT.glob(clip+'*.mp4') if p!=movie]
    if matches:
        assert len(matches)==1,(clip,matches)
        matches[0].replace(movie)
    assert movie.exists() and movie.stat().st_size>1000
    movies.append((clip,movie))
    print('PREVIEW_MOVIE_COMPLETE',clip,str(movie),flush=True)
bpy.app.handlers.frame_change_pre.remove(follow_preview)

# Assemble native renders with a single display transform and check video lengths.
import runpy
runpy.run_path(str(ROOT/'tools/finalize_compact_preview.py'),run_name='__main__')
