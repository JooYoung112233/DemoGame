"""Modular hollow steel chest from the approved concept; metre scale, editable pivots."""
import bpy,math,random,json,bmesh
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Art/Props/StorageChest01';OUT.mkdir(parents=True,exist_ok=True)
(OUT/'BlenderSource~').mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;scene.name='StorageChest_OpenClose';scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1;scene.render.fps=30
model=bpy.data.collections.new('StorageChest01 - editable parts');scene.collection.children.link(model)
studio=bpy.data.collections.new('Studio - not exported');scene.collection.children.link(studio)
parts=[];rng=random.Random(904)
def empty(name,parent=None,loc=(0,0,0)):
 o=bpy.data.objects.new(name,None);model.objects.link(o);o.empty_display_type='PLAIN_AXES';o.empty_display_size=.07;o.parent=parent;o.location=loc;return o
root=empty('StorageChest01');root['dimensions_metres']='0.82 x 0.48 x 0.455';root['front']='-Y in Blender, +Z after FBX export';root['purpose']='modular storage prop; game interaction not connected'
bodyroot=empty('Body',root)
lid=empty('Lid_Pivot',root,(0,.238,.355));lid['open_rotation_degrees']=-105
clasp=empty('Clasp_Pivot',lid);clasp.matrix_world.translation=Vector((0,-.247,.401))
# Evaluate hierarchy before assigning world-space mesh coordinates.
bpy.context.view_layer.update();clasp.location=Vector((0,-.247,.401))-lid.location
M={}
def material(name,color):
 m=bpy.data.materials.new('Chest_'+name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes['Principled BSDF'];p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.83;p.inputs['Specular IOR Level'].default_value=.23
 if name in ['Hardware','Pin']:p.inputs['Metallic'].default_value=.45;p.inputs['Roughness'].default_value=.67
 M[name]=m;return m
for n,c in {'Olive':(.135,.148,.102),'Lid':(.205,.215,.150),'Inside':(.086,.100,.063),'Base':(.036,.042,.040),'Hardware':(.083,.094,.090),'Pin':(.19,.20,.174),'EdgeWear':(.28,.235,.145),'Label':(.49,.424,.285),'LabelEdge':(.24,.25,.168)}.items():material(n,c)
variants={}
for n in ['Olive','Lid']:
 variants[n]=[M[n]]
 c=M[n].diffuse_color[:3]
 for i,s in enumerate([.965,1.035]):variants[n].append(material(n+'_facet'+str(i),tuple(x*s for x in c)))
def objmesh(name,vs,fs,mat,parent=bodyroot):
 d=bpy.data.meshes.new(name);d.from_pydata(vs,[],fs);d.update();bm=bmesh.new();bm.from_mesh(d);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(d);bm.free()
 o=bpy.data.objects.new(name,d);model.objects.link(o);o.data.materials.append(M[mat]);o.parent=parent;bpy.context.view_layer.update();o.matrix_parent_inverse=parent.matrix_world.inverted();parts.append(o);return o
def box(name,loc,size,mat,parent=bodyroot,bevel=.006):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  m=o.modifiers.new('Broad bevel','BEVEL');m.width=bevel;m.segments=1;bpy.ops.object.modifier_apply(modifier=m.name)
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 for c in list(o.users_collection):c.objects.unlink(o)
 model.objects.link(o);o.data.materials.append(M[mat]);o.parent=parent;bpy.context.view_layer.update();o.matrix_parent_inverse=parent.matrix_world.inverted();parts.append(o);return o
def ring(w,d,c,z):return [(-w+c,-d,z),(w-c,-d,z),(w,-d+c,z),(w,d-c,z),(w-c,d,z),(-w+c,d,z),(-w,d-c,z),(-w,-d+c,z)]
def shell(name,profiles,mat,parent):
 vs=[v for p in profiles for v in ring(*p)];fs=[]
 for j in range(len(profiles)-1):
  for i in range(8):fs.append((j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i))
 fs.extend([tuple(reversed(range(8))),tuple(range((len(profiles)-1)*8,len(profiles)*8))])
 return objmesh(name,vs,fs,mat,parent)
# Closed manifold shell: exterior, lip, inner walls and a raised interior floor.
body=shell('Body_SteelShell',[(.393,.222,.024,.038),(.400,.230,.028,.072),(.400,.230,.028,.337),(.395,.225,.025,.348),(.376,.206,.022,.348),(.376,.206,.022,.071)],'Olive',bodyroot)
body.data.materials.append(M['Inside'])
for p in body.data.polygons:
 if p.index>=32:p.material_index=1
body['wall_thickness_metres']=.019
lidmesh=shell('Lid_SteelShell',[(.398,.228,.032,.452),(.410,.240,.035,.428),(.410,.240,.035,.367),(.402,.232,.03,.355),(.380,.210,.028,.355),(.380,.210,.028,.431)],'Lid',lid)
lidmesh.data.materials.append(M['Inside'])
for p in lidmesh.data.polygons:
 if p.index>=32 and not all(lidmesh.data.vertices[i].co.z>.45 for i in p.vertices):p.material_index=1
# A few large material facets keep the flat top from feeling like glossy plastic.
for o,key in [(lidmesh,'Lid'),(body,'Olive')]:
 for m in variants[key][1:]:o.data.materials.append(m)
 bm=bmesh.new();bm.from_mesh(o.data);bm.faces.ensure_lookup_table()
 target=[f for f in bm.faces if (key=='Lid' and all(v.co.z>.451 for v in f.verts)) or (key=='Olive' and f.normal.y<-.99 and len(f.verts)==4 and max(v.co.z for v in f.verts)-min(v.co.z for v in f.verts)>.2)]
 if target:
  result=bmesh.ops.poke(bm,faces=target)
  for f in result['faces']:f.material_index=rng.choice([0,0,0,2,3])
 bm.to_mesh(o.data);bm.free();o.data.update()
base=shell('Base_ReinforcedBand',[(.400,.23,.028,.018),(.406,.236,.03,.029),(.406,.236,.03,.070),(.398,.228,.024,.077),(.375,.205,.022,.077),(.375,.205,.022,.042)],'Base',bodyroot)
for x in [-.348,.348]:
 for y in [-.181,.181]:box('Base_Foot', (x,y,.016),(.116,.106,.032),'Base',bevel=.008)
box('Seal_ShadowLip',(0,0,.351),(.782,.432,.009),'Base',bevel=.012)
# Seal is a ring, not a solid sheet across the hollow opening.
seal=parts[-1];bpy.data.objects.remove(seal,do_unlink=True);parts.remove(seal)
sealvs=ring(.397,.227,.027,.350)+ring(.375,.205,.023,.350)
objmesh('Seal_Perimeter',sealvs,[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)],'Base')
def cyl(name,loc,radius,depth,mat,axis,parent):
 bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=radius,depth=depth,location=loc);o=bpy.context.object;o.name=name;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 for c in list(o.users_collection):c.objects.unlink(o)
 model.objects.link(o);o.data.materials.append(M[mat]);o.parent=parent;bpy.context.view_layer.update();o.matrix_parent_inverse=parent.matrix_world.inverted();parts.append(o);return o
for x in [-.247,.247]:
 box('Hinge_BodyLeaf',(x,.234,.321),(.098,.014,.055),'Hardware',bevel=.003)
 box('Hinge_LidLeaf',(x,.242,.390),(.098,.014,.056),'Hardware',lid,.003)
 cyl('Hinge_Pin',(x,.238,.355),.013,.11,'Hardware',(1,0,0),bodyroot)
 cyl('Hinge_EndCap',(x+.057,.238,.355),.014,.008,'Pin',(1,0,0),bodyroot)
box('Latch_StrikePlate',(0,-.240,.287),(.084,.014,.090),'Hardware',bevel=.008)
box('Latch_Catch',(0,-.255,.301),(.039,.022,.025),'Pin',bevel=.003)
box('Latch_LidMount',(0,-.242,.403),(.09,.015,.063),'Hardware',lid,.004)
box('Latch_Flap',(0,-.266,.350),(.055,.021,.111),'Hardware',clasp,.007)
box('Latch_FlapInset',(0,-.278,.355),(.037,.004,.078),'Pin',clasp,.003)
cyl('Latch_PivotPin',(0,-.25,.402),.009,.069,'Pin',(1,0,0),lid)
handles=[]
for sign in [-1,1]:
 hp=empty('Handle_'+('L' if sign<0 else 'R')+'_Pivot',root,(sign*.425,0,.273));handles.append(hp)
 for y in [-.085,.085]:
  box('Handle_Mount',(sign*.406,y,.270),(.022,.039,.067),'Hardware',bevel=.005)
  cyl('Handle_Pin',(sign*.422,y,.277),.010,.018,'Pin',(1,0,0),bodyroot)
  box('Handle_Arm',(sign*.437,y,.242),(.022,.022,.068),'Hardware',hp,.006)
 box('Handle_Grip',(sign*.437,0,.211),(.026,.184,.026),'Hardware',hp,.007)
# A faded blank inventory patch and sparse broad paint chips, separate editable meshes.
label=[(.196,-.2312,.186),(.322,-.2312,.19),(.329,-.2312,.251),(.318,-.2312,.259),(.195,-.2312,.254),(.191,-.2312,.225)]
objmesh('Inventory_Label',label,[(0,1,2,3,4,5)],'Label')
chipvs=[];chipfs=[]
def chipfront(x,z,w,h,y=-.2303):
 i=len(chipvs);chipvs.extend([(x-w/2,y,z-h/2),(x+w*.13,y,z-h*.32),(x+w*.48,y,z+h*.07),(x+w*.25,y,z+h*.49),(x-w*.31,y,z+h*.30),(x-w*.48,y,z)]);chipfs.append(tuple(range(i,i+6)))
for x,z,w,h in [(-.362,.325,.035,.019),(.353,.33,.027,.02),(-.369,.11,.02,.038),(.36,.081,.035,.012),(-.19,.338,.026,.006),(.27,.338,.018,.009),(.196,.25,.018,.009),(.322,.199,.014,.014)]:chipfront(x,z,w,h)
objmesh('Body_PaintWear',chipvs,chipfs,'EdgeWear')
lv=[];lf=[]
for x,y,w in [(-.355,-.192,.04),(.355,-.188,.032),(-.32,.192,.026),(.26,.199,.019)]:
 i=len(lv);lv.extend([(x-w/2,y-.008,.4523),(x+w*.4,y-.004,.4523),(x+w*.12,y+.01,.4523),(x-w*.4,y+.006,.4523)]);lf.append(tuple(range(i,i+4)))
objmesh('Lid_EdgeWear',lv,lf,'EdgeWear',lid)
# Model-space UVs remain editable and avoid external texture dependencies.
for o in parts:
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(70),island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
 o['editable_part']=True
# A single 100-frame inspection clip, with latch release before the lid opens.
scene.frame_start=1;scene.frame_end=100
for frame,angle in [(1,0),(14,0),(42,-105),(58,-105),(86,0),(100,0)]:
 lid.rotation_euler.x=math.radians(angle);lid.keyframe_insert(data_path='rotation_euler',frame=frame)
for frame,angle in [(1,0),(12,-70),(86,-70),(100,0)]:
 clasp.rotation_euler.x=math.radians(angle);clasp.keyframe_insert(data_path='rotation_euler',frame=frame)
scene.frame_set(1)
# Export only the model. Cameras, lights and floor never enter the FBX.
bpy.ops.object.select_all(action='DESELECT')
for o in model.objects:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(OUT/'StorageChest01.fbx'),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,add_leaf_bones=False,path_mode='AUTO')
# Neutral studio views.
def tostudio(o):
 for c in list(o.users_collection):c.objects.unlink(o)
 studio.objects.link(o)
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.mesh.primitive_plane_add(size=200);floor=bpy.context.object;floor.name='StudioFloor';floor.location.z=-.001;tostudio(floor);floor.data.materials.append(material('Studio',(.036,.041,.040)))
for n,loc,power,size in [('Key',(-2,-3,4),380,3),('Fill',(3,-1,2),110,3),('Rim',(1,3,3),260,2.5)]:
 d=bpy.data.lights.new(n,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(n,d);studio.objects.link(o);o.location=loc;aim(o,(0,0,.25))
scene.world=bpy.data.worlds.new('ChestStudio');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.16,.17,.18,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.35
cd=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',cd);studio.objects.link(cam);scene.camera=cam;cd.type='ORTHO';cd.ortho_scale=1.18;cam.location=(1.4,-2.4,1.35);aim(cam,(0,0,.235))
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.render.resolution_x=1100;scene.render.resolution_y=850;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX'
for filename,frame,position,target,scale in [('Preview',1,(1.4,-2.4,1.35),(0,0,.235),1.18),('Open',50,(1.4,-2.4,1.6),(0,0,.40),1.65),('Back',1,(-1.4,2.4,1.35),(0,0,.24),1.18)]:
 scene.frame_set(frame);cam.location=position;aim(cam,target);cd.ortho_scale=scale;scene.render.filepath=str(OUT/(filename+'.png'));bpy.ops.render.render(write_still=True)
scene.frame_set(1);cam.location=(1.4,-2.4,1.35);aim(cam,(0,0,.235));cd.ortho_scale=1.18
stats={'mesh_parts':len(parts),'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in parts),'lid_pivot':list(lid.location),'frames':100,'fps':30,'uv_mapped':all(bool(o.data.uv_layers) for o in parts),'non_manifold_shell_edges':{}}
for o in [body,lidmesh,base]:
 bm=bmesh.new();bm.from_mesh(o.data);stats['non_manifold_shell_edges'][o.name]=sum(not e.is_manifold for e in bm.edges);bm.free()
(OUT/'ModelInfo.json').write_text(json.dumps(stats,indent=2),encoding='utf8')
(OUT/'Palette.json').write_text(json.dumps({k:list(v.diffuse_color) for k,v in M.items() if k!='Studio'},indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/StorageChest01.blend'))
print('CHEST_COMPLETE',json.dumps(stats))
