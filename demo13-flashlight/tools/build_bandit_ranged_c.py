"""Ranged bandit visual on the approved C body and unchanged 23-bone rig.
Staged outside Assets so ongoing Unity combat testing is not interrupted.
"""
import bpy,bmesh,json,hashlib,math
from pathlib import Path
from mathutils import Vector
from mathutils.kdtree import KDTree
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT.parent/'ArtWork/BanditRoles62'
for p in [OUT/'BlenderSource~',OUT/'Models']:p.mkdir(parents=True,exist_ok=True)
SOURCE=ROOT.parent/'ArtWork/CharacterProportionC/BlenderSource~/Bandit_C.blend';sourceHash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));bpy.context.preferences.filepaths.save_version=0
sc=bpy.context.scene;rig=bpy.data.objects['SimpleHero_Rig'];body=bpy.data.objects['Bandit_Body'];rig.data.pose_position='REST';bpy.context.view_layer.update()
def signature():
 return hashlib.sha256(repr(([(b.name,b.parent.name if b.parent else None,[tuple(row) for row in b.matrix_local]) for b in rig.data.bones],[(a.name,[(c.data_path,c.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right),k.interpolation) for k in c.keyframe_points]) for c in a.fcurves]) for a in bpy.data.actions])).encode()).hexdigest()
before=signature();materials={};parts=[body]
def material(name,color,roughness=.88,metallic=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;bs=m.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=(*color,1);bs.inputs['Roughness'].default_value=roughness;bs.inputs['Metallic'].default_value=metallic
 materials[name]={'baseColor':list(color),'roughness':roughness,'metallic':metallic};return m
cloth=material('Ranged_OliveCloth',(.125,.164,.079));armor=material('Ranged_CharcoalWebbing',(.048,.057,.044));helmet=material('Ranged_HelmetPaint',(.102,.127,.083),.70,.12);edge=material('Ranged_WornEdge',(.21,.215,.16),.63,.22);pouch=material('Ranged_CanvasPouch',(.19,.18,.105));skin=material('Ranged_Skin',(.55,.335,.18));rubber=material('Ranged_Gloves',(.043,.047,.04),.88)
# Retain all original atlas UVs and body weights. Part_* groups identify authored
# components, so no approximate spatial deletion can remove the face or neck.
hoodIndex=body.vertex_groups['Part_Bandit_Hood'].index
removed=[v.index for v in body.data.vertices if any(g.group==hoodIndex and g.weight>.99 for g in v.groups)]
bm=bmesh.new();bm.from_mesh(body.data);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.verts[i] for i in removed],context='VERTS');bm.to_mesh(body.data);bm.free();body.data.update()
def in_part(v,name):
 g=body.vertex_groups.get(name)
 return g is not None and any(w.group==g.index and w.weight>.99 for w in v.groups)
slots={}
for m in [cloth,armor,rubber]:slots[m.name]=len(body.data.materials);body.data.materials.append(m)
for p in body.data.polygons:
 verts=[body.data.vertices[i] for i in p.vertices];mid=sum((v.co for v in verts),Vector())/len(verts)
 if all(in_part(v,'Part_Study_Torso') for v in verts):p.material_index=slots[cloth.name]
 elif all(in_part(v,'Part_Bandit_Mask') for v in verts):p.material_index=slots[armor.name]
 elif any(all(in_part(v,'Part_Study_Sleeve_'+s) for v in verts) for s in ['-1','1']) and mid.z>.845:p.material_index=slots[cloth.name]
 elif any(all(in_part(v,'Part_Study_Mitten_'+s) for v in verts) for s in ['-1','1']):p.material_index=slots[rubber.name]
for v in body.data.vertices:
 if in_part(v,'Part_Bandit_Mask') and v.co.z<1.25:v.co.z=1.25+(v.co.z-1.25)*.25
# New vest follows existing torso weights; headgear uses Head only.
torsoVerts=[v for v in body.data.vertices if in_part(v,'Part_Study_Torso')];kd=KDTree(len(torsoVerts))
for i,v in enumerate(torsoVerts):kd.insert(v.co,i)
kd.balance()
boneNames={b.name for b in rig.data.bones}
def mesh(name,vs,fs,mat,bone='Chest',followTorso=False):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(name,me);sc.collection.objects.link(o);o.parent=rig;me.materials.append(mat)
 for v in me.vertices:
  weights={bone:1}
  if followTorso:
   _,i,_=kd.find(v.co);weights={body.vertex_groups[g.group].name:g.weight for g in torsoVerts[i].groups if body.vertex_groups[g.group].name in boneNames}
  for n,w in weights.items():
   vg=o.vertex_groups.get(n) or o.vertex_groups.new(name=n);vg.add([v.index],w,'REPLACE')
 o.modifiers.new('Existing rig','ARMATURE').object=rig
 uv=me.uv_layers.new(name=body.data.uv_layers[0].name)
 for f in me.polygons:
  axis=max(range(3),key=lambda k:abs(f.normal[k]));axes=[k for k in range(3) if k!=axis]
  for li in f.loop_indices:p=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(p[axes[0]],p[axes[1]])
 parts.append(o);return o
def box(name,p,s,mat,bone='Chest',followTorso=False):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);temp=bpy.context.object;temp.dimensions=s;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 mod=temp.modifiers.new('Worn edges','BEVEL');mod.width=min(.012,min(s)*.18);mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 vs=[tuple(v.co) for v in temp.data.vertices];fs=[tuple(f.vertices) for f in temp.data.polygons];bpy.data.objects.remove(temp,do_unlink=True)
 return mesh(name,vs,fs,mat,bone,followTorso)
def rings(name,rows,mat,bone='Head',followTorso=False,cap=True):
 vs=[];profile=[(-1,-.60),(-.65,-1),(.65,-1),(1,-.60),(1,.60),(.65,1),(-.65,1),(-1,.60)] if followTorso else None;N=len(profile) if profile else 16
 for z,rx,ry,cy in rows:
  for i in range(N):
   if profile:xx,yy=profile[i];vs.append((xx*rx,cy+yy*ry,z))
   else:a=i*math.tau/N;vs.append((math.sin(a)*rx,cy-math.cos(a)*ry,z))
 fs=[]
 for k in range(len(rows)-1):
  for i in range(N):fs.append((k*N+i,k*N+(i+1)%N,(k+1)*N+(i+1)%N,(k+1)*N+i))
 if cap:fs.extend([tuple(reversed(range(N))),tuple(range((len(rows)-1)*N,len(rows)*N))])
 return mesh(name,vs,fs,mat,bone,followTorso)
# The source hood concealed an open rear skull region. Fill it under the helmet.
box('Skull under helmet',(0,.02,1.417),(.475,.24,.185),skin,'Head')
rings('Low military helmet',[(1.413,.282,.222,.008),(1.447,.302,.244,.009),(1.548,.274,.225,.014),(1.613,.208,.180,.018),(1.637,.118,.115,.018)],helmet)
rings('Helmet lower band',[(1.416,.286,.225,.008),(1.443,.306,.246,.009)],armor,cap=False)
box('Short front brim',(0,-.218,1.446),(.44,.055,.022),helmet,'Head')
for s in [-1,1]:
 box('Helmet side fixing',(s*.283,-.01,1.455),(.019,.07,.033),edge,'Head')
box('Crown field patch',(.045,.009,1.641),(.092,.17,.009),pouch,'Head')
for o in parts:
 if o.name.startswith(('Low military helmet','Helmet lower band','Short front brim','Helmet side fixing','Crown field patch')):
  # At pitch 62, a low projecting brim hid the entire eye line. Lift the rim
  # and flatten the crown while keeping the short military helmet silhouette.
  for v in o.data.vertices:v.co.z=1.49+(v.co.z-1.413)*.82
  o.data.update()
# Sleeveless soft vest, not bulky shoulder armour; keep rifle stow lane clear.
rings('Soft tactical vest',[(.744,.25,.172,0),(.864,.238,.171,0),(.982,.227,.164,0),(1.065,.192,.151,0)],armor,'Chest',True,False)
for s in [-1,1]:
 box('Shoulder webbing',(s*.126,0,1.067),(.056,.305,.025),armor,'Chest')
 box('Chest magazine pouch',(s*.094,-.198,.902),(.139,.079,.207),pouch,'Chest',True)
 box('Magazine pouch lid',(s*.094,-.247,.981),(.143,.022,.054),cloth,'Chest',True)
 box('Magazine lid catch',(s*.094,-.261,.965),(.023,.008,.028),edge,'Chest',True)
 box('Belt side pouch',(s*.224,.058,.727),(.090,.116,.12),armor,'Hips')
box('Vest lower strap',(0,-.174,.786),(.389,.023,.035),cloth,'Spine',True)
box('Vest buckle',(.025,-.194,.784),(.057,.018,.032),edge,'Spine',True)
# Short local seam/repair strips; no large background noise or new atlas bake.
for s in [-1,1]:box('Shoulder seam',(s*.129,-.094,1.082),(.039,.067,.004),pouch,'Chest')
assert signature()==before,'Skeleton or animation keys changed'
for o in parts:
 assert o.data.uv_layers and all(math.isfinite(n) for v in o.data.vertices for n in v.co),o.name
 for v in o.data.vertices:
  total=sum(g.weight for g in v.groups if o.vertex_groups[g.group].name in boneNames);assert abs(total-1)<.001,(o.name,v.index,total)
# Retain renderer name and one skinned body for the existing visual integration.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=body;bpy.ops.object.join();body.name='Bandit_Body';body.hide_render=False
rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1);bpy.context.view_layer.update()
for o in sc.objects:
 if o.type=='MESH' and o!=body and o.name!='Preview_Ground':o.hide_render=True
for action in bpy.data.actions:action.use_fake_user=True
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/Bandit_Ranged_C.blend'))
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/Bandit_Ranged_C.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0)
assert signature()==before and hashlib.sha256(SOURCE.read_bytes()).hexdigest()==sourceHash
report={'source':str(SOURCE),'sourceSHA256':sourceHash,'sourceUnchanged':True,'skeletonAndAnimationHash':before,'bones':len(rig.data.bones),'actions':[a.name for a in bpy.data.actions],'triangles':sum(len(f.vertices)-2 for f in body.data.polygons),'materialSlots':len(body.data.materials),'newMaterials':materials,'renderer':'Bandit_Body','rootScale':list(rig.scale),'viewPitchYaw':[62,0],'roles':{'melee':'existing CharacterProportionC/Bandit_C','ranged':'Bandit_Ranged_C; shared pistol/rifle body'},'unityStatus':'staged only; runtime firearm aim, grip and stow pending'}
(OUT/'BuildValidation.json').write_text(json.dumps(report,indent=2),encoding='utf8');print('RANGED_C_BUILT',json.dumps(report))
