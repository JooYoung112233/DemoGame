"""Render the actual slash, with a preview-only socket-mounted knife proxy.
--poses renders the contact sheet; --movies renders and assembles two camera views.
The production FBX/GLB do not include the knife, studio or floor.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
ARCHIVE = ROOT/'ArtSource/CharacterArchive/2026-09-08'
OUT = ARCHIVE/'Assets/ChibiSurvivor/CompactSurvivor/Animated/Combat'
PREVIOUS_THRUST = OUT/'Thrust'
FLUID = '--fluid' in sys.argv
THRUST = '--stab' in sys.argv or FLUID
if THRUST: OUT = OUT/'Thrust'
if FLUID: OUT = ROOT/'Assets/ChibiSurvivor/Player'
ACTION = 'Attack_OneHand_Thrust' if THRUST else 'Attack_OneHand'
END = 28 if FLUID else 34 if THRUST else 40
CONTACT = 11 if FLUID else 14 if THRUST else 15
MOVIE_FRAMES = END+15
assert not ('--poses' in sys.argv and '--movies' in sys.argv), 'Render poses and movies in separate invocations.'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'CompactSurvivor_Combat.blend'))
scene = bpy.context.scene
rig = bpy.data.objects['CompactSurvivor_Rig']
action = bpy.data.actions[ACTION]
rig.animation_data.action = action
if action.slots: rig.animation_data.action_slot = action.slots[0]
scene.frame_set(1)

def material(name, color, metal=0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    bs = m.node_tree.nodes['Principled BSDF']
    bs.inputs['Base Color'].default_value = (*color,1)
    bs.inputs['Roughness'].default_value = .72
    bs.inputs['Metallic'].default_value = metal
    return m

preview = bpy.data.collections.new('PREVIEW_ONLY_Knife_And_Stage')
scene.collection.children.link(preview)
grip = bpy.data.objects.new('Preview_Knife_Grip',None); preview.objects.link(grip)
constraint = grip.constraints.new('COPY_TRANSFORMS')
constraint.target = rig; constraint.subtarget = 'HandSocket.R'
grip['purpose'] = 'Temporary knife silhouette for animation review; excluded from production exports'
handlemat = material('Preview_Handle',(.075,.047,.027))
steel = material('Preview_Blade',(.28,.31,.30),.4)
edge = material('Preview_Edge',(.40,.43,.41),.4)

def box(name, loc, scale, mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o = bpy.context.object; o.name = name; o.scale = scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(o.users_collection): c.objects.unlink(o)
    preview.objects.link(o); o.parent = grip; o.data.materials.append(mat)
    bevel = o.modifiers.new('Small_edge','BEVEL'); bevel.width = .002; bevel.segments = 1
    return o

handle = box('Preview_Knife_Handle',(0,0,0),(.027,.088,.023),handlemat)
guard = box('Preview_Knife_Guard',(0,.051,0),(.067,.011,.016),steel)
outline = [(-.025,.056),(.025,.056),(.022,.222),(-.006,.282),(-.025,.217)]
verts = [(x,y,z) for z in [-.003,.003] for x,y in outline]
faces = [(4,3,2,1,0),(5,6,7,8,9)]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)]
data = bpy.data.meshes.new('Preview_Knife_Blade'); data.from_pydata(verts,[],faces); data.update()
blade = bpy.data.objects.new('Preview_Knife_Blade',data); preview.objects.link(blade)
blade.parent = grip; data.materials.append(steel); data.materials.append(edge)
for p in data.polygons:
    if p.index>1: p.material_index = 1

character = [o for o in scene.objects if o.type=='MESH']
floor_mat = material('Preview_Ground',(.048,.054,.050))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.002))
floor = bpy.context.object; floor.name = 'Preview_Ground'; floor.data.materials.append(floor_mat)
floor.hide_render = True
scene.render.fps = 30; scene.frame_start = 1; scene.frame_end = END
scene.render.resolution_x = 640; scene.render.resolution_y = 720; scene.render.resolution_percentage = 100
scene.camera.location = (-6,-4,2.15) if THRUST else (-3,-6,1.65)
target = Vector((0,-.035,.79))
scene.camera.rotation_euler = (target-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera.data.ortho_scale = 1.91
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); bpy.context.view_layer.objects.active = rig
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(ACTION+'_Preview.blend')))

if '--poses' in sys.argv:
    scene.cycles.samples = 20
    labels = [('시작',1),('준비',11),('베기',15),('마무리',18),('복귀',28)]
    if THRUST: labels = [('시작',1),('당기기',9),('찌르기',14),('회수',20),('복귀',29)]
    if FLUID: labels=[('시작',1),('이어지는 준비',6),('찌르기',11),('반동 · 회수',15),('잔동작',22)]
    rot = Matrix.Rotation(math.atan(1.5 if THRUST else .5),4,'Z')
    for col,(label,frame) in enumerate(labels):
        scene.frame_set(frame); bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get()
        for o in character:
            ev = o.evaluated_get(dg)
            mesh = bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=dg)
            mesh.transform(rot @ ev.matrix_world)
            snap = bpy.data.objects.new('Pose_'+str(frame),mesh); scene.collection.objects.link(snap)
            snap.location.x = (col-2)*1.13
    for o in character: o.hide_render = True
    font = bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
    labelmat = bpy.data.materials.new('PoseLabel'); labelmat.use_nodes = True
    bs = labelmat.node_tree.nodes['Principled BSDF']
    bs.inputs['Base Color'].default_value = (.75,.77,.75,1)
    bs.inputs['Emission Color'].default_value = (.75,.77,.75,1)
    bs.inputs['Emission Strength'].default_value = .8
    for col,(label,frame) in enumerate(labels):
        d = bpy.data.curves.new('PoseLabel','FONT'); d.body = f'{label} · {frame}f'
        d.font = font; d.size = .075; d.align_x = 'CENTER'
        o = bpy.data.objects.new('PoseLabel',d); scene.collection.objects.link(o)
        o.location = ((col-2)*1.13,-.65,1.70); o.rotation_euler = (math.pi/2,0,0)
        d.materials.append(labelmat)
    scene.camera.location = (0,-10,.88); scene.camera.rotation_euler = (math.pi/2,0,0)
    scene.camera.data.ortho_scale = 5.95
    scene.render.resolution_x = 1800; scene.render.resolution_y = 640
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(OUT/(ACTION+'_Poses.png'))
    bpy.ops.render.render(write_still=True)
    print('ONEHAND_POSES_READY',flush=True)

if '--movies' in sys.argv:
    floor.hide_render = False
    scene.render.engine = 'BLENDER_EEVEE_NEXT'
    if hasattr(scene.eevee,'taa_render_samples'): scene.eevee.taa_render_samples = 32
    scene.render.resolution_x = 640; scene.render.resolution_y = 720
    scene.render.fps = 30; scene.frame_step = 1
    scene.render.image_settings.file_format = 'FFMPEG'
    scene.render.ffmpeg.format = 'MPEG4'; scene.render.ffmpeg.codec = 'H264'
    scene.render.ffmpeg.constant_rate_factor = 'HIGH'; scene.render.ffmpeg.ffmpeg_preset = 'GOOD'
    scene.render.ffmpeg.audio_codec = 'NONE'
    views = [('Quarter',(-3,-6,1.65),1.91),('GameAngle',(-3,-5,5.6),1.93)]
    if THRUST: views[0] = ('Quarter',(-6,-4,2.15),1.98)
    for name,location,scale in views:
        scene.camera.location = location; scene.camera.data.ortho_scale = scale
        scene.camera.rotation_euler = (target-scene.camera.location).to_track_quat('-Z','Y').to_euler()
        scene.frame_start = 1; scene.frame_end = MOVIE_FRAMES # include a half-second recovery hold
        scene.render.filepath = str(OUT/(name+'.mp4'))
        bpy.ops.render.render(animation=True)
        targetpath = OUT/(name+'.mp4')
        alternates = [p for p in OUT.glob(name+'*.mp4') if p!=targetpath]
        if alternates:
            assert len(alternates)==1; alternates[0].replace(targetpath)
        assert targetpath.exists() and targetpath.stat().st_size>1000
        print('ONEHAND_VIEW_READY',name,flush=True)

    # Assemble two repetitions per angle; encoded colors are already display transformed.
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.resolution_x = 640; scene.render.resolution_y = 720; scene.render.resolution_percentage = 100
    scene.render.fps = 30; scene.view_settings.view_transform = 'Standard'; scene.view_settings.look = 'None'
    scene.render.image_settings.file_format = 'FFMPEG'
    scene.render.ffmpeg.format = 'MPEG4'; scene.render.ffmpeg.codec = 'H264'
    scene.render.ffmpeg.constant_rate_factor = 'HIGH'; scene.render.ffmpeg.audio_codec = 'NONE'
    ed = scene.sequence_editor_create(); strips = ed.strips if hasattr(ed,'strips') else ed.sequences
    font = bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
    cursor = 1; entries = []
    for name,label in [('Quarter','한손 찌르기' if THRUST else '한손 베기'),('GameAngle','쿼터뷰 각도')]:
        for repeat in range(2):
            seq = strips.new_movie(name,str(OUT/(name+'.mp4')),channel=1,frame_start=cursor)
            assert seq.frame_final_duration==MOVIE_FRAMES,(name,seq.frame_final_duration)
            text = strips.new_effect('Label',type='TEXT',channel=2,frame_start=cursor,frame_end=seq.frame_final_end)
            text.text = label+' · 임시 칼'; text.font = font; text.font_size = 24
            if FLUID: text.text = ('수정 찌르기' if name=='Quarter' else '수정 · 쿼터뷰')+' · 임시 칼'
            text.color = (.94,.94,.90,1); text.location = (.5,.95); text.use_shadow = True
            entries.append({'view':name,'start':cursor,'frames':MOVIE_FRAMES})
            cursor = seq.frame_final_end
    scene.frame_start = 1; scene.frame_end = cursor-1
    scene.render.filepath = str(OUT/(ACTION+'_Preview.mp4'))
    bpy.ops.render.render(animation=True)
    path = OUT/(ACTION+'_Preview.mp4')
    alternatives = [p for p in OUT.glob(ACTION+'_Preview*.mp4') if p!=path]
    if alternatives:
        assert len(alternatives)==1; alternatives[0].replace(path)
    scene.render.image_settings.file_format = 'PNG'
    for name,frame in [('Quarter',CONTACT),('GameAngle',2*MOVIE_FRAMES+CONTACT)]:
        scene.frame_set(frame); scene.render.filepath = str(OUT/('Preview_'+name+'.png'))
        bpy.ops.render.render(write_still=True)
    info = {'fps':30,'resolution':[640,720],'frames':cursor-1,'seconds':(cursor-1)/30,
            'views':entries,'source':'Actual Blender model and animation','preview_only_knife':True,
            'display_transform':'Standard; no second AgX pass'}
    (OUT/'PreviewCheck.json').write_text(json.dumps(info,indent=2),encoding='utf-8')
    print('ONEHAND_MOVIE_VERIFIED',json.dumps(info),flush=True)
    if FLUID:
        # Same camera and actual playback speed make the removal of pose holds visible.
        scene.sequence_editor_clear()
        ed=scene.sequence_editor_create(); strips=ed.strips if hasattr(ed,'strips') else ed.sequences
        cursor=1; entries=[]
        for path,label,expected in [(PREVIOUS_THRUST/'Quarter.mp4','이전 찌르기',49),
                                     (OUT/'Quarter.mp4','수정 · 끊김 없이 이어지는 찌르기',MOVIE_FRAMES)]:
            for repeat in range(2):
                seq=strips.new_movie(label,str(path),channel=1,frame_start=cursor)
                assert seq.frame_final_duration==expected
                text=strips.new_effect('Label',type='TEXT',channel=2,frame_start=cursor,frame_end=seq.frame_final_end)
                text.text=label; text.font=font; text.font_size=24
                text.color=(.94,.94,.90,1); text.location=(.5,.95); text.use_shadow=True
                entries.append({'source':str(path),'start':cursor,'frames':expected})
                cursor=seq.frame_final_end
        scene.render.image_settings.file_format='FFMPEG'
        scene.frame_start=1; scene.frame_end=cursor-1
        path=OUT/'Thrust_Comparison.mp4'; scene.render.filepath=str(path)
        bpy.ops.render.render(animation=True)
        alternatives=[p for p in OUT.glob('Thrust_Comparison*.mp4') if p!=path]
        if alternatives:
            assert len(alternatives)==1; alternatives[0].replace(path)
        assert path.exists() and path.stat().st_size>1000
        (OUT/'ComparisonCheck.json').write_text(json.dumps({'fps':30,'frames':cursor-1,
            'same_camera':True,'retimed':False,'clips':entries},indent=2),encoding='utf-8')
        print('THRUST_COMPARISON_READY',str(path),flush=True)
