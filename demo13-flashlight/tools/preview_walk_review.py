"""Actual walk comparison; movement of the stage matches each clip's contact speed."""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1];PLAYER=ROOT/'Assets/ChibiSurvivor/Player';OUT=PLAYER/'WalkReview'
CACHE=ROOT/'Library/CodexBlender/WalkReview';CACHE.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'CompactSurvivor_WalkReview.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];meshes=[o for o in scene.objects if o.type=='MESH']
walk=bpy.data.actions['Walk'];stats=json.loads((OUT/'WalkCheck.json').read_text())
def use(action):
    rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
use(walk)
def material(name,color):
    m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes['Principled BSDF']
    bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=.95
    return m
if '--poses' in sys.argv:
    rot=Matrix.Rotation(math.atan(.8),4,'Z');phases=[1,7,13,19,25,31]
    for col,f in enumerate(phases):
        scene.frame_set(f);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
        for o in meshes:
            ev=o.evaluated_get(dg);mesh=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=dg)
            mesh.transform(rot@ev.matrix_world);snap=bpy.data.objects.new('WalkPose',mesh)
            scene.collection.objects.link(snap);snap.location.x=(col-2.5)*.9
    for o in meshes:o.hide_render=True
    ink=material('Labels',(.8,.82,.8));bs=ink.node_tree.nodes['Principled BSDF']
    bs.inputs['Emission Color'].default_value=(.8,.82,.8,1);bs.inputs['Emission Strength'].default_value=.8
    font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
    for i,label in enumerate(['왼발 디딤','체중 이동','오른발 통과','오른발 디딤','체중 이동','왼발 통과']):
        data=bpy.data.curves.new('Label','FONT');data.body=label;data.size=.06;data.font=font;data.align_x='CENTER';data.materials.append(ink)
        obj=bpy.data.objects.new('Label',data);scene.collection.objects.link(obj);obj.location=((i-2.5)*.9,-.8,1.77);obj.rotation_euler=(math.pi/2,0,0)
    scene.camera.location=(0,-10,.85);scene.camera.rotation_euler=(math.pi/2,0,0);scene.camera.data.ortho_scale=5.9
    scene.render.resolution_x=1800;scene.render.resolution_y=700;scene.render.resolution_percentage=100;scene.cycles.samples=20
    scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'WalkPoses.png')
    bpy.ops.render.render(write_still=True);print('WALK_POSES_READY',flush=True)
else:
    with bpy.data.libraries.load(str(PLAYER/'CompactSurvivor_Combat.blend'),link=False) as (src,dst):dst.actions=['Walk']
    old=dst.actions[0];old.name='Preview_PreviousWalk'
    floor_mat=material('Preview_Ground',(.05,.053,.048));nodes=floor_mat.node_tree.nodes
    check=nodes.new('ShaderNodeTexChecker');coord=nodes.new('ShaderNodeTexCoord')
    check.inputs['Color1'].default_value=(.05,.055,.05,1);check.inputs['Color2'].default_value=(.08,.085,.075,1);check.inputs['Scale'].default_value=3
    floor_mat.node_tree.links.new(coord.outputs['Object'],check.inputs['Vector'])
    floor_mat.node_tree.links.new(check.outputs['Color'],nodes['Principled BSDF'].inputs['Base Color'])
    bpy.ops.mesh.primitive_plane_add(size=40,location=(0,0,-.002));floor=bpy.context.object;floor.name='PREVIEW_ONLY_Ground';floor.data.materials.append(floor_mat)
    carrier=bpy.data.objects.new('PREVIEW_ONLY_Travel',None);scene.collection.objects.link(carrier)
    for obj in [rig,scene.camera]+[o for o in scene.objects if o.type=='LIGHT']:obj.parent=carrier
    scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=32
    scene.render.resolution_x=576;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.fps=30;scene.frame_step=1
    def movie_settings():
        scene.render.image_settings.file_format='FFMPEG';scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264'
        scene.render.ffmpeg.constant_rate_factor='HIGH';scene.render.ffmpeg.ffmpeg_preset='GOOD';scene.render.ffmpeg.audio_codec='NONE'
    def exact(path):
        variants=[p for p in path.parent.glob(path.stem+'*.mp4') if p!=path]
        if variants:assert len(variants)==1;variants[0].replace(path)
        assert path.exists() and path.stat().st_size>1000
    def setup(action,count,speed,location):
        use(action)
        for fc in action.fcurves:
            if not any(m.type=='CYCLES' for m in fc.modifiers):fc.modifiers.new('CYCLES')
        carrier.animation_data_clear();carrier.location=(0,0,0);carrier.keyframe_insert('location',frame=1)
        carrier.location.y=-speed*count*2/30;carrier.keyframe_insert('location',frame=count*2+1)
        for fc in carrier.animation_data.action.fcurves:
            for k in fc.keyframe_points:k.interpolation='LINEAR'
        scene.camera.location=location;target=Vector((0,-.02,.79));scene.camera.rotation_euler=(target-scene.camera.location).to_track_quat('-Z','Y').to_euler()
        scene.camera.data.ortho_scale=1.95;scene.frame_start=1;scene.frame_end=count*2;scene.frame_set(1)
    setups=[('Previous','이전 걷기',old,30,.60,(-4.8,-6,2.5)),
      ('Revised','수정 걷기',walk,36,stats['nominal_forward_speed_mps'],(-4.8,-6,2.5)),
      ('Side','수정 걷기 · 측면',walk,36,stats['nominal_forward_speed_mps'],(-7,-.5,2.0))]
    movie_settings()
    for key,label,action,count,speed,location in setups:
        setup(action,count,speed,location)
        if key=='Revised':bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'WalkPreview.blend'))
        path=CACHE/(key+'.mp4');scene.render.filepath=str(path);bpy.ops.render.render(animation=True);exact(path)
        print('WALK_VIEW_READY',key,flush=True)
    bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene
    scene.render.resolution_x=576;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.fps=30
    scene.view_settings.view_transform='Standard';scene.view_settings.look='None';movie_settings()
    ed=scene.sequence_editor_create();strips=ed.strips if hasattr(ed,'strips') else ed.sequences
    font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf');cursor=1;entries=[];stills=[]
    for key,label,action,count,speed,location in setups:
        for repeat in range(2):
            seq=strips.new_movie(key,str(CACHE/(key+'.mp4')),channel=1,frame_start=cursor)
            assert seq.frame_final_duration==count*2
            text=strips.new_effect('Label',type='TEXT',channel=2,frame_start=cursor,frame_end=seq.frame_final_end)
            text.text=label;text.font=font;text.font_size=25;text.location=(.5,.95);text.color=(.95,.95,.90,1);text.use_shadow=True
            if repeat==0:stills.append((key,cursor+9))
            entries.append({'view':key,'start':cursor,'frames':count*2});cursor=seq.frame_final_end
    scene.frame_start=1;scene.frame_end=cursor-1;path=OUT/'WalkComparison.mp4';scene.render.filepath=str(path)
    bpy.ops.render.render(animation=True);exact(path)
    scene.render.image_settings.file_format='PNG'
    for name,f in stills:
        scene.frame_set(f);scene.render.filepath=str(CACHE/(name+'.png'));bpy.ops.render.render(write_still=True)
    report={'frames':cursor-1,'fps':30,'seconds':(cursor-1)/30,'sequence':entries,'source':'Actual Blender render',
      'world_travel_matches_each_walk_speed':True,'approval':'pending','display_transform':'Standard; rendered colors not transformed twice'}
    (OUT/'PreviewCheck.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('WALK_COMPARISON_READY',json.dumps(report),flush=True)
