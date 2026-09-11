"""Project the approved modeled expression onto a dedicated head texture."""
import bpy,numpy as np,json,hashlib,shutil,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from character_uv_atlas import author_atlas
BASE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor';OUT=BASE/'SurfaceReview';TEX=OUT/'Textures';CACHE=ROOT/'Library/CodexBlender/HeroFaceTexture';CACHE.mkdir(parents=True,exist_ok=True)
source=BASE/'BlenderSource~/DarkSurvivor.blend';backup=CACHE/'DarkSurvivor_Before.blend'
if not backup.exists():shutil.copy2(source,backup)
bpy.ops.wm.open_mainfile(filepath=str(source));bpy.context.preferences.filepaths.save_version=0
if bpy.data.objects.get('Hero_Nose'):
 raise RuntimeError('Face already migrated. Edit Hero_Face_Neutral.png directly; do not restore an older character for rebaking.')
sc=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig'];head=bpy.data.objects['Hero_Head'];features=bpy.data.objects['Hero_FaceDetails']
def motion_hash():return hashlib.sha256(repr(([(b.name,tuple(b.head_local),tuple(b.tail_local)) for b in rig.data.bones],[(a.name,[(f.data_path,f.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right)) for k in f.keyframe_points]) for f in a.fcurves]) for a in bpy.data.actions])).encode()).hexdigest()
unchanged=motion_hash();head_before=hashlib.sha256(repr([tuple(v.co) for v in head.data.vertices]).encode()).hexdigest();before_vertices=len(features.data.vertices)
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=64;sc.render.resolution_x=850;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
def render(name,target=(0,0,.88),loc=(2,-6,2.2),scale=2.12):
 sc.camera.location=loc;sc.camera.rotation_euler=(Vector(target)-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=scale;sc.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
render('FaceTexture_Before');rig.data.pose_position='REST';bpy.context.view_layer.update()
profile={r['material']:r for r in json.loads((OUT/'SurfaceCheck.json').read_text())['profiles']}
# High-resolution orthographic expression projection, depth sorted per sample.
S=2048;x0,x1=-.33,.33;z0,z1=1.18,1.66
layer=np.zeros((S,S,4),np.float32);depth=np.full((S,S),np.inf,np.float32)
m=features.data;m.calc_loop_triangles();vs=np.array([tuple(features.matrix_world@v.co) for v in m.vertices]);count=0
for tri in m.loop_triangles:
 mat=m.materials[tri.material_index];name=mat.name.removeprefix('Surface_').split('.')[0]
 if name=='Hero_nose':continue
 xyz=vs[list(tri.vertices)];uv=np.column_stack(((xyz[:,0]-x0)/(x1-x0)*S,(xyz[:,2]-z0)/(z1-z0)*S));a,b,c=uv;v0=b-a;v1=c-a;den=v0[0]*v1[1]-v1[0]*v0[1]
 if abs(den)<1e-8:continue
 lo=np.maximum(np.floor(uv.min(0)).astype(int),0);hi=np.minimum(np.ceil(uv.max(0)).astype(int),S-1)
 yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];dx=xx+.5-a[0];dy=yy+.5-a[1];u=(dx*v1[1]-v1[0]*dy)/den;v=(v0[0]*dy-dx*v0[1])/den;inside=(u>=0)&(v>=0)&(u+v<=1);yd=xyz[0,1]+u*(xyz[1,1]-xyz[0,1])+v*(xyz[2,1]-xyz[0,1]);ok=inside&(yd<depth[yy,xx]);px=xx[ok];py=yy[ok]
 layer[py,px,:3]=profile[name]['base_color'][:3];layer[py,px,3]=1;depth[py,px]=yd[ok];count+=1
def paint(name,mat,p):
 x,y,z=p.T;key=mat.name.removeprefix('Surface_').split('.')[0];base=np.tile(np.array(profile[key]['base_color'][:3]),(len(p),1));base*=.985
 # Preserve simple, muted skin shading; no directional shadows baked into eyes.
 cheek=(np.exp(-((x-.18)/.06)**2)+np.exp(-((x+.18)/.06)**2))*np.exp(-((z-1.316)/.042)**2)*np.clip((-y-.025)/.08,0,1)*.09
 base=base*(1-cheek[:,None])+np.array((.4,.175,.09))*cheek[:,None]
 u=np.clip((x-x0)/(x1-x0)*S-.5,0,S-1.001);v=np.clip((z-z0)/(z1-z0)*S-.5,0,S-1.001);ix=u.astype(int);iy=v.astype(int);fx=u-ix;fy=v-iy
 # Premultiplied interpolation keeps borders soft without dark fringes.
 sample=layer[iy,ix]*((1-fx)*(1-fy))[:,None]+layer[iy,ix+1]*(fx*(1-fy))[:,None]+layer[iy+1,ix]*((1-fx)*fy)[:,None]+layer[iy+1,ix+1]*(fx*fy)[:,None]
 face=np.clip((-y-.035)/.045,0,1);alpha=sample[:,3]*face;rgb=base*(1-alpha[:,None])+sample[:,:3]*face[:,None]
 return np.clip(rgb,0,1),np.zeros(len(p)),0,.9
images,uvreport=author_atlas([head],{'HEAD_FACE':(.015,.015,.97,.97)},lambda n:'HEAD_FACE',paint,TEX,'Hero_Face',1024)
# Give the neutral expression a stable editable name for later expression swaps.
neutral=TEX/'Hero_Face_Neutral.png';shutil.copy2(TEX/'Hero_Face_BaseColor.png',neutral)
image=bpy.data.images.load(str(neutral),check_existing=False);image.colorspace_settings.name='sRGB'
face_mat=bpy.data.materials.get('Hero_Face') or bpy.data.materials.new('Hero_Face');face_mat.use_nodes=True;nodes=face_mat.node_tree.nodes;nodes.clear();bs=nodes.new('ShaderNodeBsdfPrincipled');bs.inputs['Roughness'].default_value=.9;bs.inputs['Specular IOR Level'].default_value=.15;tex=nodes.new('ShaderNodeTexImage');tex.image=image;tex.name='Face_Neutral';out=nodes.new('ShaderNodeOutputMaterial');face_mat.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color']);face_mat.node_tree.links.new(bs.outputs[0],out.inputs['Surface'])
head.data.materials.clear();head.data.materials.append(face_mat)
for poly in head.data.polygons:poly.material_index=0
head['expression_in_texture_v1']=True
exec(Path(__file__).with_name('strip_hero_face_details.py').read_text());nose=strip_hero_face_details()
assert motion_hash()==unchanged;assert hashlib.sha256(repr([tuple(v.co) for v in head.data.vertices]).encode()).hexdigest()==head_before
rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
render('FaceTexture_After');render('FaceTexture_Close',(0,-.07,1.405),(1.3,-5,1.7),.78);render('FaceTexture_Side',(0,0,1.39),(5,-1,1.6),.82)
rig.animation_data.action=bpy.data.actions['SwordSlash'];sc.frame_set(16);render('FaceTexture_Slash');rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(source));bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/DarkSurvivor_Textured.blend'))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in sc.objects:
 if o.type=='MESH' and o.name.startswith('Hero_'):o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
for fbx in [BASE/'DarkSurvivor.fbx',OUT/'DarkSurvivor_Textured.fbx']:
 bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
report={'expression':'neutral','face_texture':str(neutral),'expression_geometry_removed':True,'nose_retained':True,'head_shape_rig_motion_preserved':True,'feature_vertices_before':before_vertices,'nose_vertices_after':len(nose.data.vertices),'head_texture_size':1024,'uv_charts':uvreport['charts']};(OUT/'FaceTextureCheck.json').write_text(json.dumps(report,indent=2));print('HERO_FACE_TEXTURE_READY',flush=True)
