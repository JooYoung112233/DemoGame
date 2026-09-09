import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];CACHE=ROOT/'Library/CodexBlender/ExplosiveReview';OUT=ROOT/'Assets/ChibiSurvivor/Player/ExplosiveRunReview'
bpy.ops.wm.open_mainfile(filepath=str(CACHE/'Render.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];action=rig.animation_data.action
original=[o for o in scene.objects if o.type=='MESH']
stats=json.loads((OUT/'RetargetCheck.json').read_text())
poses={};ranges={n:[] for n in ['Head','UpperArm.L','UpperArm.R','Thigh.L','Thigh.R']}
for f in range(1,26):
 scene.frame_set(f);dg=bpy.context.evaluated_depsgraph_get()
 if f in [1,25]:
  coords=[]
  for o in original:
   ev=o.evaluated_get(dg);m=ev.to_mesh();coords.extend([list(ev.matrix_world@v.co) for v in m.vertices]);ev.to_mesh_clear()
  poses[f]=coords
 for n in ranges:ranges[n].append(list(rig.pose.bones[n].matrix.to_quaternion()))
stats['loop_seam_max_vertex_m']=max((Vector(a)-Vector(b)).length for a,b in zip(poses[1],poses[25]))
assert stats['loop_seam_max_vertex_m']<.01,stats['loop_seam_max_vertex_m']
stats['review_findings']=['Large elbow flexion exposes existing cuff/upper-sleeve deformation.','Source head forward lean is retained; large head makes it more visible.','No Unity Humanoid Avatar validation performed.']
stats['preview']='Actual imported FBX joint animation retargeted in Blender at native clip timing; in-place.'
(OUT/'RetargetCheck.json').write_text(json.dumps(stats,indent=2))
def movie():
 scene.render.image_settings.file_format='FFMPEG';scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264';scene.render.ffmpeg.constant_rate_factor='HIGH';scene.render.ffmpeg.audio_codec='NONE'
def exact(path):
 for p in path.parent.glob(path.stem+'*.mp4'):
  if p!=path:p.replace(path)
 assert path.exists() and path.stat().st_size>1000
mat=bpy.data.materials.new('ReviewGround');mat.diffuse_color=(.05,.055,.05,1)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.002));bpy.context.object.data.materials.append(mat)
scene.frame_start=1;scene.frame_end=24
scene.render.resolution_x=480;scene.render.resolution_y=600;scene.render.resolution_percentage=100;scene.render.fps=30
for name,position in [('Front',(-4.8,-7,1.9)),('Side',(-7,-.4,1.65))]:
 scene.camera.location=position;scene.camera.rotation_euler=(Vector((0,0,.79))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=1.98
 scene.frame_set(1);movie();path=CACHE/(name+'.mp4');scene.render.filepath=str(path);bpy.ops.render.render(animation=True);exact(path)
 if name=='Front':
  scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'Preview.png');scene.frame_set(1);bpy.ops.render.render(write_still=True)
bpy.ops.wm.read_factory_settings(use_empty=True);scene=bpy.context.scene
scene.render.resolution_x=960;scene.render.resolution_y=600;scene.render.resolution_percentage=100;scene.render.fps=30
scene.view_settings.view_transform='Standard';scene.view_settings.look='None';movie()
strips=scene.sequence_editor_create().strips
for i,(name,label) in enumerate([('Front','패키지 달리기 · 정면 사선'),('Side','패키지 달리기 · 측면')]):
 for repeat in range(4):
  seq=strips.new_movie(name,str(CACHE/(name+'.mp4')),channel=i+1,frame_start=1+24*repeat);seq.blend_type='ALPHA_OVER';seq.transform.offset_x=(i-.5)*480
 t=strips.new_effect('Label',type='TEXT',channel=i+3,frame_start=1,frame_end=97);t.text=label;t.font=bpy.data.fonts.load('C:/Windows/Fonts/malgun.ttf');t.font_size=23;t.location=(.25+i*.5,.96);t.use_shadow=True
scene.frame_start=1;scene.frame_end=96;path=OUT/'ExplosiveRunPreview.mp4';scene.render.filepath=str(path)
bpy.ops.render.render(animation=True);exact(path)
scene.render.image_settings.file_format='PNG';scene.frame_set(7);scene.render.filepath=str(CACHE/'VideoCheck.png');bpy.ops.render.render(write_still=True)
print('EXPLOSIVE_PREVIEW_READY',str(path),flush=True)
