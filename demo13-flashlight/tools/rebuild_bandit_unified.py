"""Stockier fixed-outfit bandit: one body renderer/material, separate held bat."""
import bpy,bmesh,numpy as np,json,hashlib,shutil,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'tools'))
from character_uv_atlas import author_atlas
OUT=ROOT/'Assets/ChibiSurvivor/Bandit/Bandit01';BS=OUT/'BlenderSource~';TEX=OUT/'Textures';CACHE=ROOT/'Library/CodexBlender/BanditUnified';CACHE.mkdir(parents=True,exist_ok=True)
src=BS/'Bandit01_WithBat.blend';backup=CACHE/'Bandit01_WithBat_Before.blend'
if not backup.exists():shutil.copy2(src,backup)
if not (CACHE/'Bandit01_Before.blend').exists():shutil.copy2(BS/'Bandit01.blend',CACHE/'Bandit01_Before.blend')
authoring=BS/'Bandit01_Authoring.blend'
if not authoring.exists():shutil.copy2(backup,authoring)
bpy.ops.wm.open_mainfile(filepath=str(authoring));bpy.context.preferences.filepaths.save_version=0
sc=bpy.context.scene;rig=bpy.data.objects['Bandit01_Rig'];parts=sorted([o for o in sc.objects if o.type=='MESH' and o.name.startswith('Bandit_') and o.name!='Bandit_Bat'],key=lambda o:o.name);bat=bpy.data.objects['Bandit_Bat']
def rig_motion_hash():
 return hashlib.sha256(repr(([(b.name,tuple(b.head_local),tuple(b.tail_local),b.parent.name if b.parent else None) for b in rig.data.bones],[(a.name,[(f.data_path,f.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right)) for k in f.keyframe_points]) for f in a.fcurves]) for a in bpy.data.actions])).encode()).hexdigest()
motion_before=rig_motion_hash();bat_before=hashlib.sha256(repr([tuple(v.co) for v in bat.data.vertices]).encode()).hexdigest()
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=48;sc.render.resolution_x=850;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
def render(name,action='Idle',frame=1,loc=(2.7,-6,2.5),show_bat=False):
 rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions[action];sc.frame_set(frame);bat.hide_render=not show_bat;sc.camera.location=loc;sc.camera.rotation_euler=(Vector((0,0,.88))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.type='ORTHO';sc.camera.data.ortho_scale=2.08;sc.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
render('Unified_Before')
rig.data.pose_position='REST';bpy.context.view_layer.update()
original_parts=len(parts);bounds_before=[min((o.matrix_world@v.co)[i] for o in parts for v in o.data.vertices) for i in range(3)]+[max((o.matrix_world@v.co)[i] for o in parts for v in o.data.vertices) for i in range(3)]
for o in parts:
 inv=o.matrix_world.inverted();n=o.name
 for v in o.data.vertices:
  p=o.matrix_world@v.co;x,y,z=p;sign=1 if x>=0 else -1
  if any(k in n for k in ['Head','Hair','FaceDetails','Fringe','Hood','FaceWrap']):x*=.87;y*=.92;z=1.2+(z-1.2)*.94
  elif 'NeckKerchief' in n:x*=1.02;y*=.94;z+=.023
  elif any(k in n for k in ['JacketBody','JacketOpening','Lapel','JacketPocket']):
   t=max(0,min(1,(z-.82)/.34));x*=1.18+.08*t;y*=1.17+.05*t
  elif 'Sleeve' in n:
   x=sign*.245+(x-sign*.245)*1.17+sign*.022;y*=1.16
  elif 'Forearm' in n or 'WristWrap' in n:
   t=max(0,min(1,(z-.74)/.29));cx=sign*(.315-.073*t);x=cx+(x-cx)*1.19+sign*.018*t;y=-.10+(.10*t)+(y-(-.10+.10*t))*1.12
  elif 'TrouserLeg' in n:
   scale=1.16+.17*np.exp(-((z-.49)/.21)**2);x=sign*.103+(x-sign*.103)*scale;y=.005+(y-.005)*1.15
  elif 'TrouserCuff' in n:x=sign*.103+(x-sign*.103)*1.12;y=.005+(y-.005)*1.10
  elif 'Hips' in n or n in ['Bandit_Belt','Bandit_BeltBuckle']:x*=1.14;y*=1.16
  elif any(k in n for k in ['LootPouch','PouchFlap','PouchTab']):x+=sign*.025;y*=1.08
  elif 'Boot' in n or 'Sole' in n:x=sign*.103+(x-sign*.103)*1.09;y*=1.06
  v.co=inv@Vector((float(x),float(y),float(z)))
 # Resolve non-skin modifiers before merging so shells retain thickness and weights.
 bpy.context.view_layer.objects.active=o;o.hide_set(False)
 for mod in list(o.modifiers):
  if mod.type!='ARMATURE':bpy.ops.object.modifier_apply(modifier=mod.name)
 # Closed sleeve caps must face outwards; inverted caps become black under hull outlines.
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
 # Retain an edit selection group per source part, without adding any deform bones.
 g=o.vertex_groups.new(name='Part_'+n.removeprefix('Bandit_'));g.add(list(range(len(o.data.vertices))),1,'REPLACE')
assert rig_motion_hash()==motion_before
regions={'HEAD_HOOD':(.02,.53,.48,.45),'JACKET_ARMS':(.52,.53,.46,.45),'TROUSERS':(.02,.02,.42,.49),'BOOTS_GEAR':(.46,.02,.52,.49)}
def bucket(n):
 if any(k in n for k in ['Head','Hood','Hair','Face','Fringe','NeckKerchief','Neck']):return 'HEAD_HOOD'
 if any(k in n for k in ['Jacket','Lapel','Sleeve','Forearm','Hands','WristWrap']):return 'JACKET_ARMS'
 if any(k in n for k in ['Trouser','Hips']):return 'TROUSERS'
 return 'BOOTS_GEAR'
def gauss(v,c,w):return np.exp(-((v-c)/w)**2)
def paint(name,mat,p):
 bs=mat.node_tree.nodes.get('Principled BSDF');rgb=np.array(bs.inputs['Base Color'].default_value[:3]);color=np.tile(rgb,(len(p),1));x,y,z=p.T;h=np.zeros(len(p));rough=float(bs.inputs['Roughness'].default_value);metal=float(bs.inputs['Metallic'].default_value);key=mat.name.lower();front=np.clip((-y-.025)/.07,0,1)
 def shade(a):
  nonlocal color
  color*=a[:,None]
 def tint(a,c):
  nonlocal color
  a=np.clip(a,0,1)[:,None];color=color*(1-a)+np.array(c)*a
 cloth=any(k in key for k in ['jacket','hood','mask','pants','cuff','wrap','under'])
 if cloth:
  shade(.99+.035*np.sin(z*11+x*9)+.015*np.cos(y*13-z*7))
 if 'JacketBody' in name:
  shade(1-.12*gauss(z,.84,.014)-.10*gauss(z,1.17,.018))
  seam=gauss(np.abs(x),.215,.003);shade(1-.20*seam);h-=seam*.15
 if 'JacketPocket' in name:
  seam=gauss(z,.917,.003);shade(1-.24*seam);h-=seam*.2
 if 'Sleeve' in name and 'Rolled' not in name:
  # One simple sewn repair on the left upper sleeve, painted rather than another object.
  repair=(x<-.26)*(z>1.054)*(z<1.091)*front
  tint(repair*.22,(.135,.10,.055))
  edge=gauss(z,1.054,.002)*(x<-.26)*front;shade(1-.22*edge);h-=edge*.2
 if 'TrouserLeg' in name:
  localx=x-np.sign(x)*.103;fold=z-(.46+.18*localx);shade(1-.14*gauss(fold,0,.009)*front+.08*gauss(fold,.015,.012)*front)
  tint(np.clip((.38-z)/.25,0,1)*.10,(.13,.09,.055))
 if 'Hood' in name and 'Opening' not in name:
  seam=gauss(x,0,.0025)*np.clip((z-1.50)/.12,0,1);shade(1-.16*seam);h-=seam*.13
 if 'FaceWrap' in name:
  seam=gauss(z,1.30-.075*np.abs(x),.0025)*front;shade(1-.18*seam);h-=seam*.15
 if 'WristWrap' in name:
  bands=gauss(np.mod(z+x*.2,.022),.005,.0018);shade(1-.17*bands);h-=bands*.12
 if 'Boot' in name:tint(np.clip((.12-z)/.13,0,1)*.14,(.20,.14,.085))
 return np.clip(color,0,1),h,metal,rough
images,uvreport=author_atlas(parts,regions,bucket,paint,TEX,'Bandit',2048)
print('BANDIT_UV_READY',uvreport['charts'],uvreport['overlapping_pixels'],flush=True)
# One production body mesh. UV charts and edit-selection groups keep authoring practical.
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=bpy.data.objects['Bandit_JacketBody'];bpy.ops.object.join();body=bpy.context.object;body.name='Bandit_Body';body.data.name='Bandit_Body';body['fixed_outfit']=True
mat=bpy.data.materials.new('Bandit_Surface');mat.use_nodes=True;nodes=mat.node_tree.nodes;links=mat.node_tree.links;bs=nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(1,1,1,1);bs.inputs['Roughness'].default_value=.88
tn={}
for label,im in images.items():
 t=nodes.new('ShaderNodeTexImage');t.name='Baked_'+label;t.image=im;t.location=(-600,200-len(tn)*250);tn[label]=t
links.new(tn['BaseColor'].outputs['Color'],bs.inputs['Base Color']);normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.6;links.new(tn['Normal'].outputs['Color'],normal.inputs['Color']);links.new(normal.outputs[0],bs.inputs['Normal'])
inv=nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;links.new(tn['Mask'].outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],bs.inputs['Roughness'])
body.data.materials.clear();body.data.materials.append(mat)
for p in body.data.polygons:p.material_index=0
bone_names=set(rig.data.bones.keys());bad=[]
for v in body.data.vertices:
 total=sum(g.weight for g in v.groups if body.vertex_groups[g.group].name in bone_names)
 if abs(total-1)>.001:bad.append((v.index,total))
assert not bad,('Invalid skin weights',bad[:10]);assert rig_motion_hash()==motion_before;assert hashlib.sha256(repr([tuple(v.co) for v in bat.data.vertices]).encode()).hexdigest()==bat_before
bounds_after=[min((body.matrix_world@v.co)[i] for v in body.data.vertices) for i in range(3)]+[max((body.matrix_world@v.co)[i] for v in body.data.vertices) for i in range(3)]
for args in [('Unified_Preview','Idle',1,(2.7,-6,2.5),False),('Unified_Front','Idle',1,(0,-6,1.7),False),('Unified_Back','Idle',1,(2.4,6,2.3),False),('Unified_Run','Run',7,(2.7,-6,2.5),True),('Unified_BatSwing','SwordSlash',18,(2.7,-6,2.5),True)]:render(*args)
rig.animation_data.action=bpy.data.actions['SwordWalk'];sc.frame_set(1);bat.hide_render=False
bpy.ops.wm.save_as_mainfile(filepath=str(src))
def export(path,with_bat):
 bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True);bat.select_set(with_bat);bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
export(OUT/'Bandit01_WithBat.fbx',True);export(OUT/'Bandit01.fbx',False)
bpy.data.objects.remove(bat,do_unlink=True);rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(BS/'Bandit01.blend'))
report={'original_body_renderers':original_parts,'body_renderers':1,'body_materials':1,'weapon_separate':True,'body_vertices':len(body.data.vertices),'triangles':sum(len(p.vertices)-2 for p in body.data.polygons),'unweighted_vertices':len(bad),'skeleton_and_motion_preserved':True,'bat_preserved':True,'body_bounds_before':bounds_before,'body_bounds_after':bounds_after,'uv':{k:v for k,v in uvreport.items() if k!='layout'}}
(OUT/'UnifiedBuildCheck.json').write_text(json.dumps(report,indent=2));print('BANDIT_UNIFIED_READY',flush=True)
