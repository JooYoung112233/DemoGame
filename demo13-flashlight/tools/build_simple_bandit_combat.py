"""Dedicated bandit club and review poses; preserves the approved character source."""
import bpy,bmesh,math,json,sys,hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from character_uv_atlas import author_atlas
OUT=ROOT/'Assets/ChibiSurvivor/Bandit/SimpleBandit';WEAPON=OUT/'Weapons';REV=ROOT.parent/'ArtWork/SimpleBandit/CombatReview'
for p in [WEAPON/'Textures',REV]:p.mkdir(parents=True,exist_ok=True)
SRC=OUT/'BlenderSource~/SimpleBandit.blend';before=hashlib.sha256(SRC.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SRC));bpy.context.preferences.filepaths.save_version=0
sc=bpy.context.scene;rig=bpy.data.objects['SimpleHero_Rig'];rig.data.pose_position='REST';bpy.context.view_layer.update()
old=bpy.data.objects['Bandit_Bat'];bpy.data.objects.remove(old,do_unlink=True)
# Same approved palm axis; shortening the striking end does not alter either grip.
rows=[(-.197,.026),(-.183,.030),(-.17,.023),(.06,.023),(.12,.029),(.29,.047),(.55,.058),(.618,.056),(.636,.042)]
N=10;local=[(.10+math.cos(i*math.tau/N)*radius,y,-.025+math.sin(i*math.tau/N)*radius) for y,radius in rows for i in range(N)]
faces=[tuple(reversed(range(N)))]
for j in range(len(rows)-1):
 for i in range(N):faces.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
faces.append(tuple(range((len(rows)-1)*N,len(rows)*N)))
me=bpy.data.meshes.new('BanditClub');me.from_pydata(local,[],faces);me.update()
bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
club=bpy.data.objects.new('Bandit_Club',me);sc.collection.objects.link(club)
base=bpy.data.materials.new('BanditClub_Base');base.diffuse_color=(.20,.115,.056,1);me.materials.append(base)
def paint(name,mat,p):
 x,y,z=p.T;n=len(p);a=np.arctan2(z+.025,x-.10)
 grain=np.sin(a*7+y*2.6)*.006+np.sin(a*17-y*4)*.002
 c=np.tile((.20,.115,.056),(n,1));c+=grain[:,None]*np.array((1,.65,.3));rough=np.full(n,.88)
 wrap=(y>-.175)&(y<.064);c[wrap]=(.073,.068,.060)
 seam=wrap&(np.mod(y+a*.006,.036)<.004);c[seam]=(.045,.043,.038)
 edge=wrap&(np.mod(y+a*.006,.036)>.031);c[edge]=(.09,.083,.07)
 for yy,aa in [(.25,.2),(.45,2.3),(.51,-1.1)]:
  scratch=(abs(y-yy)<.052)&(abs(np.arctan2(np.sin(a-aa-(y-yy)*.35),np.cos(a-aa-(y-yy)*.35)))<.014)
  c[scratch]=(.265,.164,.082)
 end=y>.629;r=np.sqrt((x-.1)**2+(z+.025)**2);c[end]=(.233,.139,.065)
 c[end&(np.mod(r,.012)<.0017)]=(.183,.102,.046)
 return c,np.zeros(n),0,rough
images,atlas=author_atlas([club],{'CLUB':(.02,.02,.96,.96)},lambda n:'CLUB',paint,WEAPON/'Textures','BanditClub',512)
material=bpy.data.materials.new('BanditClub_Surface');material.use_nodes=True;p=material.node_tree.nodes['Principled BSDF'];p.inputs['Roughness'].default_value=.88;p.inputs['Specular IOR Level'].default_value=0
t=material.node_tree.nodes.new('ShaderNodeTexImage');t.image=images['BaseColor'];material.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color']);me.materials.clear();me.materials.append(material)
for v in me.vertices:v.co=rig.data.bones['HandSocket.R'].matrix_local@(v.co+Vector((-.10,0,.022)))
club.parent=rig;club.vertex_groups.new(name='HandSocket.R').add(list(range(len(me.vertices))),1,'REPLACE');club.modifiers.new('Grip','ARMATURE').object=rig
# Review actions mirror the local Unity clips; every descendant of Chest retains
# the two-hand ready pose, while the existing hips and legs keep the approved gait.
rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['BatSwing'];sc.frame_set(1);bpy.context.view_layer.update()
upper={'Chest'}|{b.name for b in rig.data.bones['Chest'].children_recursive}
pose={b.name:{'rotation_quaternion':tuple(b.rotation_quaternion),'location':tuple(b.location),'scale':tuple(b.scale)} for b in rig.pose.bones}
for source,target in [('Idle','Bandit_BatIdle'),('Walk','Bandit_BatWalk'),('Run','Bandit_BatRun')]:
 action=bpy.data.actions[source].copy();action.name=target
 for fc in action.fcurves:
  if 'pose.bones[' not in fc.data_path:continue
  name=fc.data_path.split('"')[1];channel=fc.data_path.rsplit('.',1)[1]
  if name not in upper or channel not in pose[name]:continue
  value=pose[name][channel][fc.array_index]
  for k in fc.keyframe_points:k.co.y=value;k.handle_left.y=value;k.handle_right.y=value
attack=bpy.data.actions['BatSwing'].copy();attack.name='Bandit_BatAttack'
for fc in attack.fcurves:
 for k in fc.keyframe_points:
  k.co.x=1+(k.co.x-1)*.9;k.handle_left.x=1+(k.handle_left.x-1)*.9;k.handle_right.x=1+(k.handle_right.x-1)*.9
# Check striking barrel against every non-hand surface over the complete swing.
body=bpy.data.objects['Bandit_Body'];hand_groups={g.index for g in body.vertex_groups if g.name.startswith('Part_Study_Mitten')}
body_faces=[tuple(f.vertices) for f in body.data.polygons if not all(any(w.group in hand_groups for w in body.data.vertices[i].groups) for i in f.vertices)]
barrel_faces=[tuple(f.vertices) for f in me.polygons if all(local[i][1]>.10 for i in f.vertices)]
collisions=[];max_grip=0
for action_name in ['Bandit_BatWalk','Bandit_BatAttack']:
 rig.animation_data.action=bpy.data.actions[action_name];start,end=rig.animation_data.action.frame_range
 for i in range(97):
  f=start+(end-start)*i/96;sc.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
  bm=body.evaluated_get(dg).to_mesh();cm=club.evaluated_get(dg).to_mesh()
  bt=BVHTree.FromPolygons([v.co for v in bm.vertices],body_faces);ct=BVHTree.FromPolygons([v.co for v in cm.vertices],barrel_faces)
  if bt.overlap(ct):collisions.append([action_name,round(f,3)])
  body.evaluated_get(dg).to_mesh_clear();club.evaluated_get(dg).to_mesh_clear()
  r=rig.pose.bones['HandSocket.R'];l=rig.pose.bones['HandSocket.L'];max_grip=max(max_grip,(l.head-(r.matrix@Vector((0,-.15,0)))).length)
report={'club_length_m':.833,'barrel_diameter_m':.116,'texture_size':512,'one_hand_item':False,'source_character_unchanged':hashlib.sha256(SRC.read_bytes()).hexdigest()==before,'body_barrel_intersections':collisions,'sampled_frames':194,'max_socket_error_m':max_grip,'attack_seconds':1.44}
(REV/'BlenderCheck.json').write_text(json.dumps(report,indent=2));print('COMBAT_CHECK',report,flush=True)
rig.animation_data.action=bpy.data.actions['Bandit_BatIdle'];sc.frame_set(1);club.hide_render=False
sc.camera.location=(3,-6,2.1);sc.camera.rotation_euler=(Vector((0,0,.91))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=2.45
sc.render.resolution_x=800;sc.render.resolution_y=900;sc.render.image_settings.file_format='PNG';sc.render.filepath=str(REV/'Ready.png');bpy.ops.render.render(write_still=True)
rig.animation_data.action=attack;sc.frame_set(17);sc.render.filepath=str(REV/'Attack.png');bpy.ops.render.render(write_still=True)
rig.animation_data.action=bpy.data.actions['Bandit_BatIdle'];sc.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/SimpleBandit_Combat.blend'))
bpy.ops.object.select_all(action='DESELECT');club.select_set(True);rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(WEAPON/'BanditClub.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
assert not collisions,collisions[:10]
assert max_grip<.002,max_grip
print('BANDIT_COMBAT_READY',flush=True)
