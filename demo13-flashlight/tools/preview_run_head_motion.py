"""Previous/new run comparison at each clip's natural cadence; preview scene only."""
import bpy,math,json,sys
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/RunReview/HeadMotionReview'
OLD=ROOT/'Assets/ChibiSurvivor/Player/RunReview/CompactSurvivor_RunReview_v3.blend'
CACHE=ROOT/'Library/CodexBlender/RunHeadReview';CACHE.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'CompactSurvivor_RunHeadReview.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];new=bpy.data.actions['Run']
meshes=[o for o in scene.objects if o.type=='MESH'];stats=json.loads((OUT/'HeadMotionCheck.json').read_text())
with bpy.data.libraries.load(str(OLD),link=False) as (src,dst):dst.actions=['Run']
old=dst.actions[0];old.name='PREVIEW_PreviousRun'
def use(a):
    rig.animation_data.action=a
    if a.slots:rig.animation_data.action_slot=a.slots[0]
def mat(name,color):
    m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes['Principled BSDF']
    bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=.9
    return m
font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
def label(body,x,z,size=.09):
    d=bpy.data.curves.new(body,'FONT');d.body=body;d.font=font;d.size=size;d.align_x='CENTER'
    ink=mat(body,(.8,.82,.8));bs=ink.node_tree.nodes['Principled BSDF']
    bs.inputs['Emission Color'].default_value=(.8,.82,.8,1);bs.inputs['Emission Strength'].default_value=.8
    d.materials.append(ink);o=bpy.data.objects.new(body,d);scene.collection.objects.link(o)
    o.location=(x,-.7,z);o.rotation_euler=(math.pi/2,0,0)
def tree(o,dg):
    ev=o.evaluated_get(dg);m=ev.to_mesh();b=BVHTree.FromPolygons([ev.matrix_world@v.co for v in m.vertices],[tuple(p.vertices) for p in m.polygons]);ev.to_mesh_clear();return b
if '--poses' in sys.argv:
    measures={};hits=[]
    for title,action in [('previous',old),('revised',new)]:
        use(action);ys=[];head=[];wrists=[]
        for f in range(1,25):
            scene.frame_set(f);bpy.context.view_layer.update()
            chest=rig.pose.bones['Chest'].matrix;forward=chest.to_3x3()@Vector((0,0,1))
            # Bone Z is the chest's forward/back reference; record global yaw range.
            ys.append(math.degrees(math.atan2(forward.x,forward.y)))
            head.append(rig.pose.bones['Head'].matrix.translation.x)
            wrists.append(rig.pose.bones['Hand.R'].matrix.translation.y)
            if title=='revised':
                dg=bpy.context.evaluated_depsgraph_get();hands=tree(bpy.data.objects['Compact_Hands'],dg)
                for name in ['Compact_Shirt','Compact_Trousers','Compact_BeltPouch','Compact_Backpack']:
                    if hands.overlap(tree(bpy.data.objects[name],dg)):hits.append([f,name])
        # Unwrap near +/-180 if the imported chest axis points backward.
        ys=[v+360 if v<0 else v for v in ys] if max(ys)-min(ys)>180 else ys
        measures[title]={'chest_yaw_range_degrees':max(ys)-min(ys),'head_lateral_range_m':max(head)-min(head),
          'right_hand_fore_aft_range_m':max(wrists)-min(wrists)}
    (OUT/'BodyComparisonCheck.json').write_text(json.dumps({'ranges':measures,'hand_surface_intersections':hits},indent=2),encoding='utf-8')
    assert not hits,hits
    rot=Matrix.Rotation(math.atan(.30),4,'Z')
    for row,f in enumerate([1,7]):
        for col,action in enumerate([old,new]):
            use(action);scene.frame_set(f);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
            for o in meshes:
                ev=o.evaluated_get(dg);m=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=dg)
                m.transform(rot@ev.matrix_world);snap=bpy.data.objects.new('ComparisonPose',m);scene.collection.objects.link(snap)
                snap.location=((col-.5)*1.2,0,(1-row)*1.85)
    for o in meshes:o.hide_render=True
    label('기존 머리 움직임',-.6,3.59);label('머리 반동·시간차',.6,3.59)
    scene.camera.location=(0,-10,1.74);scene.camera.rotation_euler=(math.pi/2,0,0);scene.camera.data.ortho_scale=3.95
    scene.render.resolution_x=900;scene.render.resolution_y=1300;scene.render.resolution_percentage=100
    scene.cycles.samples=16;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'HeadPoses.png')
    bpy.ops.render.render(write_still=True);print('BODY_POSES_READY',json.dumps(measures),flush=True)
else:
    groundmat=mat('Floor',(.055,.06,.052));nodes=groundmat.node_tree.nodes
    check=nodes.new('ShaderNodeTexChecker');coord=nodes.new('ShaderNodeTexCoord');check.inputs['Scale'].default_value=3
    check.inputs['Color1'].default_value=(.05,.055,.05,1);check.inputs['Color2'].default_value=(.075,.080,.070,1)
    groundmat.node_tree.links.new(coord.outputs['Object'],check.inputs['Vector']);groundmat.node_tree.links.new(check.outputs['Color'],nodes['Principled BSDF'].inputs['Base Color'])
    bpy.ops.mesh.primitive_plane_add(size=40,location=(0,0,-.002));bpy.context.object.data.materials.append(groundmat)
    carrier=bpy.data.objects.new('PREVIEW_ONLY_Travel',None);scene.collection.objects.link(carrier)
    for o in [rig,scene.camera]+[o for o in scene.objects if o.type=='LIGHT']:o.parent=carrier
    scene.camera.location=(-4.8,-6.2,2.3);target=Vector((0,0,.79));scene.camera.rotation_euler=(target-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=1.98
    scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=24
    scene.render.resolution_x=512;scene.render.resolution_y=640;scene.render.resolution_percentage=100;scene.render.fps=30
    def movie_settings():
        scene.render.image_settings.file_format='FFMPEG';scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264'
        scene.render.ffmpeg.constant_rate_factor='HIGH';scene.render.ffmpeg.ffmpeg_preset='GOOD';scene.render.ffmpeg.audio_codec='NONE'
    def exact(path):
        variants=[p for p in path.parent.glob(path.stem+'*.mp4') if p!=path]
        if variants:assert len(variants)==1;variants[0].replace(path)
        assert path.exists() and path.stat().st_size>1000
    movie_settings()
    views=[('Revised',new,stats['nominal_forward_speed_mps'])] if '--render-only' in sys.argv else [('Previous',old,stats['nominal_forward_speed_mps']),('Revised',new,stats['nominal_forward_speed_mps'])]
    for title,action,speed in views:
        use(action)
        for fc in action.fcurves:
            if not any(m.type=='CYCLES' for m in fc.modifiers):fc.modifiers.new('CYCLES')
        carrier.animation_data_clear();carrier.location=(0,0,0);carrier.keyframe_insert('location',frame=1)
        carrier.location.y=-speed*120/30;carrier.keyframe_insert('location',frame=121)
        for fc in carrier.animation_data.action.fcurves:
            for k in fc.keyframe_points:k.interpolation='LINEAR'
        scene.frame_start=1;scene.frame_end=120;scene.frame_set(1)
        if title=='Revised':bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'HeadPreview.blend'))
        path=CACHE/(title+'.mp4');scene.render.filepath=str(path);bpy.ops.render.render(animation=True);exact(path)
        print('BODY_MOVIE_VIEW_READY',title,flush=True)
    if '--render-only' in sys.argv:
        print('RUN_RENDER_READY',flush=True)
        raise SystemExit(0)
    bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene
    scene.render.resolution_x=1024;scene.render.resolution_y=640;scene.render.resolution_percentage=100;scene.render.fps=30
    scene.view_settings.view_transform='Standard';scene.view_settings.look='None';movie_settings()
    ed=scene.sequence_editor_create();strips=ed.strips if hasattr(ed,'strips') else ed.sequences
    for col,(title,body) in enumerate([('Previous','기존 머리 움직임'),('Revised','머리 반동·시간차')]):
        for rep in range(2):
            start=1+120*rep;seq=strips.new_movie(title,str(CACHE/(title+'.mp4')),channel=1+col,frame_start=start)
            assert seq.frame_final_duration==120
            seq.blend_type='ALPHA_OVER'
            seq.transform.scale_x=1;seq.transform.scale_y=1;seq.transform.offset_x=(col-.5)*512
            text=strips.new_effect('Label',type='TEXT',channel=3+col,frame_start=start,frame_end=start+120)
            text.text=body;text.font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf');text.font_size=24;text.location=(.25+.5*col,.95);text.use_shadow=True
    scene.frame_start=1;scene.frame_end=240;path=OUT/'HeadMotionComparison.mp4';scene.render.filepath=str(path)
    bpy.ops.render.render(animation=True);exact(path)
    scene.render.image_settings.file_format='PNG'
    for f in [1,7,12]:
        scene.frame_set(f);scene.render.filepath=str(CACHE/f'Pair_{f}.png');bpy.ops.render.render(write_still=True)
    (OUT/'BodyPreviewCheck.json').write_text(json.dumps({'frames':240,'fps':30,'seconds':8.0,
      'left':'Run v3 original head','right':'Head-only revision','same_camera':True,'each_clip_natural_cadence':True,
      'source':'Actual Blender animation','production_player_unchanged':True,'approval':'pending'},indent=2),encoding='utf-8')
    print('BODY_COMPARISON_READY',str(path),flush=True)
