"""Actual Blender review of sword slash, dagger thrust and axe chop.
Only the review blend contains the socket-mounted weapon proxies and stage.
--poses checks all three weapon sweeps, then renders five axe-chop poses.
--movies renders all three motions at two camera angles and assembles a reel.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/ChibiSurvivor/Player'
TWO=(OUT/'AttackSet.json').exists()
PREFIX='AttackSet' if TWO else 'OneHandAttacks'
CACHE=ROOT/'Library/CodexBlender'/('AttackSet' if TWO else 'OneHandSet')
CACHE.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'CompactSurvivor_Combat.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig']
characters=[o for o in scene.objects if o.type=='MESH']
preview=bpy.data.collections.new('PREVIEW_ONLY_Weapons');scene.collection.children.link(preview)
grip=bpy.data.objects.new('Preview_Weapon_Grip',None);preview.objects.link(grip)
c=grip.constraints.new('COPY_TRANSFORMS');c.target=rig;c.subtarget='HandSocket.R'
grip['purpose']='Animation review silhouettes only; excluded from character FBX/GLB'
def material(name,color,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;bs=m.node_tree.nodes['Principled BSDF']
    bs.inputs['Base Color'].default_value=(*color,1)
    bs.inputs['Roughness'].default_value=.72;bs.inputs['Metallic'].default_value=metal
    return m
wood=material('Preview_Weapon_Wood',(.085,.043,.020))
steel=material('Preview_Weapon_Steel',(.28,.31,.30),.4)
edge=material('Preview_Weapon_Edge',(.43,.46,.44),.45)
def box(name,loc,scale,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    o=bpy.context.object;o.name=name;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for collection in list(o.users_collection):collection.objects.unlink(o)
    preview.objects.link(o);o.parent=grip;o.data.materials.append(mat)
    bevel=o.modifiers.new('Soft_edge','BEVEL');bevel.width=.002;bevel.segments=1
    return o
def extrude(name,outline,axis,thick):
    if axis=='Z':verts=[(a,b,t) for t in (-thick,thick) for a,b in outline]
    else:verts=[(t,a,b) for t in (-thick,thick) for a,b in outline]
    n=len(outline);faces=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    o=bpy.data.objects.new(name,data);preview.objects.link(o);o.parent=grip
    data.materials.append(steel);data.materials.append(edge)
    for p in data.polygons:
        if p.index>1:p.material_index=1
    return o
weapons={};collision={}
for name,length in [('Sword',.56 if TWO else .42),('Dagger',.282)]:
    h=box('Preview_'+name+'_Handle',(0,-.05 if TWO and name=='Sword' else 0,0),(.029,.20 if TWO and name=='Sword' else .088,.025),wood)
    g=box('Preview_'+name+'_Guard',(0,.051,0),(.075 if name=='Sword' else .067,.011,.016),steel)
    blade=extrude('Preview_'+name+'_Blade',[(-.025,.056),(.025,.056),(.023,length-.065),
        (0,length),(-.023,length-.065)],'Z',.003)
    weapons[name]=[h,g,blade];collision[name]=[g,blade]
h=box('Preview_Axe_Handle',(0,.135 if TWO else .095,0),(.034,.57 if TWO else .310,.030),wood)
outline=[(.233,.040),(.318,.040),(.317,-.020),(.375,-.102),
    (.373,-.139),(.265,-.153),(.201,-.115),(.239,-.026)]
if TWO:outline=[(y+.14,z*1.15) for y,z in outline]
head=extrude('Preview_Axe_Head',outline,'X',.018)
weapons['Axe']=[h,head];collision['Axe']=[head]
if TWO:
    for weapon in weapons:
        for side in ['R','L'] if weapon!='Dagger' else ['R']:
            anchor=bpy.data.objects.new(weapon+'_Grip.'+side,None);preview.objects.link(anchor)
            anchor.parent=grip;anchor.location=(0,-.11 if side=='L' else 0,0)
            anchor.empty_display_type='ARROWS';anchor.empty_display_size=.04
            anchor['purpose']='Editable weapon-local attachment point; keep the shared two-hand profile when replacing weapon mesh.'
specs=[('Sword','검 · 베기','Attack_OneHand',40,15),
       ('Dagger','단검 · 찌르기','Attack_OneHand_Thrust',28,11),
       ('Axe','도끼 · 내려찍기','Attack_OneHand_Chop',40,17)]
if TWO:specs=[('Sword','양손 검 · 베기','Attack_TwoHand_Slash',40,16),
    ('Dagger','한손 단검 · 찌르기','Attack_OneHand_Thrust',28,11),
    ('Axe','양손 도끼 · 내려찍기','Attack_TwoHand_Chop',43,18)]
def select(spec):
    name,label,clip,end,contact=spec
    for key,parts in weapons.items():
        for o in parts:o.hide_render=key!=name;o.hide_viewport=key!=name
    action=bpy.data.actions[clip];rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    scene.frame_start=1;scene.frame_end=end;scene.frame_set(1)
    bpy.context.view_layer.update()
    return action
floor_mat=material('Preview_Ground',(.048,.054,.050))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.002))
floor=bpy.context.object;floor.name='Preview_Ground';floor.data.materials.append(floor_mat)
target=Vector((0,-.055,.82))
views=[('Quarter',(-4.5,-6,2.7),2.16),('GameAngle',(-3,-5,5.6),2.12)]
def camera(view):
    name,location,scale=view;scene.camera.location=location;scene.camera.data.ortho_scale=scale
    scene.camera.rotation_euler=(target-scene.camera.location).to_track_quat('-Z','Y').to_euler()
select(specs[-1]);camera(views[0])
scene.render.fps=30;scene.render.resolution_x=640;scene.render.resolution_y=720
scene.render.resolution_percentage=100
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(PREFIX+'_Preview.blend')))
def tree(o,dg):
    ev=o.evaluated_get(dg);mesh=ev.to_mesh()
    bvh=BVHTree.FromPolygons([ev.matrix_world@v.co for v in mesh.vertices],[tuple(p.vertices) for p in mesh.polygons])
    ev.to_mesh_clear();return bvh
if '--poses' in sys.argv:
    hits=[]
    for spec in specs:
        select(spec)
        for frame in range(1,spec[3]+1):
            scene.frame_set(frame);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
            targets=[(o.name,tree(o,dg)) for o in characters]
            for weapon in collision[spec[0]]:
                wt=tree(weapon,dg)
                for name,ct in targets:
                    overlaps=wt.overlap(ct)
                    if overlaps:hits.append({'clip':spec[2],'frame':frame,'weapon':weapon.name,'part':name,'overlaps':len(overlaps)})
    report={'scope':'Evaluated preview weapon head/blade/guard surface intersections; not gameplay colliders',
            'clips':3,'sampled_frames':sum(s[3] for s in specs),'intersections':hits,'unity_checked':False}
    (OUT/('AttackSetWeaponClearance.json' if TWO else 'OneHandWeaponClearance.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
    assert not hits,hits
    select(specs[-1]);floor.hide_render=True
    labels=[('시작',1),('들어 올리기',10),('내리기',14),('타격 · 반동',19),('회수',28)]
    if TWO:labels=[('양손 준비',1),('들어 올리기',11),('중앙으로 내리기',16),('타격 · 반동',21),('회수',30)]
    parts=characters+weapons['Axe'];rot=Matrix.Rotation(math.atan(.75),4,'Z')
    if TWO and '--slash' in sys.argv:
        select(specs[0]);parts=characters+weapons['Sword']
        labels=[('양손 준비',1),('회전 · 준비',11),('몸 앞 베기',16),('반대쪽 마무리',20),('회수',30)]
    for col,(label,frame) in enumerate(labels):
        scene.frame_set(frame);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
        for o in parts:
            ev=o.evaluated_get(dg);mesh=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=dg)
            mesh.transform(rot@ev.matrix_world)
            snap=bpy.data.objects.new('Pose_'+str(frame),mesh);scene.collection.objects.link(snap)
            snap.location.x=(col-2)*1.20
    for o in parts:o.hide_render=True
    font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf')
    ink=material('PoseLabel',(.8,.82,.79));bs=ink.node_tree.nodes['Principled BSDF']
    bs.inputs['Emission Color'].default_value=(.8,.82,.79,1);bs.inputs['Emission Strength'].default_value=.8
    for col,(label,frame) in enumerate(labels):
        data=bpy.data.curves.new('Label','FONT');data.body=f'{label} · {frame}f';data.font=font
        data.size=.075;data.align_x='CENTER';data.materials.append(ink)
        o=bpy.data.objects.new('Label',data);scene.collection.objects.link(o)
        o.location=((col-2)*1.20,-.75,1.88);o.rotation_euler=(math.pi/2,0,0)
    scene.camera.location=(0,-10,1);scene.camera.rotation_euler=(math.pi/2,0,0);scene.camera.data.ortho_scale=6.35
    scene.cycles.samples=20;scene.render.resolution_x=1800;scene.render.resolution_y=690
    scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/('Attack_TwoHand_Chop_Poses.png' if TWO else 'Attack_OneHand_Chop_Poses.png'))
    if TWO and '--slash' in sys.argv:scene.render.filepath=str(OUT/'Attack_TwoHand_Slash_Poses.png')
    bpy.ops.render.render(write_still=True)
    print('ONEHAND_SET_POSES_AND_CLEARANCE_OK',json.dumps(report),flush=True)

def movie_settings(sc):
    sc.render.image_settings.file_format='FFMPEG';sc.render.ffmpeg.format='MPEG4';sc.render.ffmpeg.codec='H264'
    sc.render.ffmpeg.constant_rate_factor='HIGH';sc.render.ffmpeg.ffmpeg_preset='GOOD';sc.render.ffmpeg.audio_codec='NONE'
def exact_movie(path):
    alternatives=[p for p in path.parent.glob(path.stem+'*.mp4') if p!=path]
    if alternatives:
        assert len(alternatives)==1;alternatives[0].replace(path)
    assert path.exists() and path.stat().st_size>1000
if '--movies' in sys.argv:
    scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=32
    scene.render.fps=30;scene.frame_step=1
    movie_settings(scene)
    for view in views:
        camera(view)
        for spec in specs:
            select(spec);scene.frame_end=spec[3]+12
            path=CACHE/(spec[0]+'_'+view[0]+'.mp4');scene.render.filepath=str(path)
            bpy.ops.render.render(animation=True);exact_movie(path)
            print('ONEHAND_SET_VIEW_READY',spec[0],view[0],flush=True)
    bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene
    scene.render.resolution_x=640;scene.render.resolution_y=720;scene.render.resolution_percentage=100
    scene.render.fps=30;scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
    movie_settings(scene)
    ed=scene.sequence_editor_create();strips=ed.strips if hasattr(ed,'strips') else ed.sequences
    font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf');cursor=1;entries=[];contact_frames=[]
    for view in views:
        for spec in specs:
            for repeat in range(2):
                path=CACHE/(spec[0]+'_'+view[0]+'.mp4')
                seq=strips.new_movie(spec[0],str(path),channel=1,frame_start=cursor)
                assert seq.frame_final_duration==spec[3]+12
                label=strips.new_effect('Label',type='TEXT',channel=2,frame_start=cursor,frame_end=seq.frame_final_end)
                label.text=spec[1]+(' · 쿼터뷰' if view[0]=='GameAngle' else '')
                label.font=font;label.font_size=25;label.location=(.5,.95);label.color=(.94,.94,.90,1);label.use_shadow=True
                if repeat==0:contact_frames.append((spec[0]+'_'+view[0],cursor+spec[4]-1))
                entries.append({'weapon':spec[0],'clip':spec[2],'view':view[0],'start':cursor,'frames':spec[3]+12})
                cursor=seq.frame_final_end
    scene.frame_start=1;scene.frame_end=cursor-1
    path=OUT/(PREFIX+'_Preview.mp4');scene.render.filepath=str(path)
    bpy.ops.render.render(animation=True);exact_movie(path)
    scene.render.image_settings.file_format='PNG'
    for name,frame in contact_frames:
        scene.frame_set(frame);scene.render.filepath=str(CACHE/(name+'.png'));bpy.ops.render.render(write_still=True)
    # Deliver the axe contact as a compact current preview, without duplicating every source clip.
    import shutil
    shutil.copy2(CACHE/'Axe_Quarter.png',OUT/('Attack_TwoHand_Chop_Preview.png' if TWO else 'Attack_OneHand_Chop_Preview.png'))
    report={'fps':30,'frames':cursor-1,'seconds':(cursor-1)/30,'clips':entries,
            'source':'Actual Blender character and animations','weapons_are_preview_proxies':True,
            'display_transform':'Standard; no second AgX pass','unity_connected':False}
    (OUT/('AttackSetPreviewCheck.json' if TWO else 'OneHandPreviewCheck.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('ONEHAND_SET_MOVIE_READY',json.dumps(report),flush=True)
