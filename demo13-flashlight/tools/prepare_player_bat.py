import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=Path(__file__).resolve().parents[1];out=root.parent/'ArtWork/SimpleHeroMelee'
bpy.ops.wm.open_mainfile(filepath=str(root/'Assets/ChibiSurvivor/Bandit/Bandit01/BlenderSource~/Bandit01_WithBat.blend'))
old=bpy.data.objects['Bandit_Bat'];r=old.parent;inv=r.data.bones['HandSocket.R'].matrix_local.inverted()
verts=[list(inv@v.co) for v in old.data.vertices];faces=[list(p.vertices) for p in old.data.polygons];indices=[p.material_index for p in old.data.polygons]
verts=[[v[0],v[1]*.8 if v[1]<0 else v[1],v[2]] for v in verts]
materials=[]
for m in old.data.materials:
 bs=m.node_tree.nodes.get('Principled BSDF');materials.append({'name':m.name,'color':list(bs.inputs['Base Color'].default_value),'roughness':bs.inputs['Roughness'].default_value})
(root/'Library/player-bat-mesh.json').write_text(json.dumps({'vertices':verts,'faces':faces,'materialIndices':indices,'materials':materials}))
bpy.ops.wm.open_mainfile(filepath=str(out/'BlenderSource~/SimpleHero_Stage5_Slash.blend'))
s=bpy.context.scene;rig=bpy.data.objects['SimpleHero_Rig'];action=bpy.data.actions['SwordSlash'];action.name='BatSwing';rig.animation_data.action=action
for o in list(bpy.data.objects):
 if o.name.startswith('Review_Sword'):bpy.data.objects.remove(o,do_unlink=True)
me=bpy.data.meshes.new('Player_Bat');me.from_pydata(verts,[],faces)
for info in materials:
 m=bpy.data.materials.new(info['name']);m.use_nodes=True;bs=m.node_tree.nodes['Principled BSDF'];bs.inputs['Base Color'].default_value=info['color'];bs.inputs['Roughness'].default_value=info['roughness'];me.materials.append(m)
for p,i in zip(me.polygons,indices):p.material_index=i
bat=bpy.data.objects.new('Hero_Bat',me);s.collection.objects.link(bat)
for v in me.vertices:v.co=rig.data.bones['HandSocket.R'].matrix_local@v.co
bat.parent=rig;group=bat.vertex_groups.new(name='HandSocket.R');group.add(list(range(len(me.vertices))),1,'REPLACE');mod=bat.modifiers.new('BatGrip','ARMATURE');mod.object=rig
parts=[o for o in s.objects if o.type=='MESH' and o.name.startswith(('Study_','Gear_'))]
def tree(o,dg):
 ev=o.evaluated_get(dg);mesh=ev.to_mesh();t=BVHTree.FromPolygons([ev.matrix_world@v.co for v in mesh.vertices],[tuple(p.vertices) for p in mesh.polygons]);ev.to_mesh_clear();return t
# Fit the reused bat inside the round palms; keep all approved bone keys intact.
frames=[]
for k in range(97):
 f=1+k*.5;s.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
 frames.append((rig.pose.bones['HandSocket.R'].matrix.copy(),[(o.name,tree(o,dg)) for o in parts]))
best=None
for dx in [-.10,-.075,-.05,-.025,0,.025,.05,.075,.10]:
 for dz in [-.10,-.075,-.05,-.025,0,.025,.05,.075,.10]:
  if dx*dx+dz*dz>.013:continue
  vv=[Vector((v[0]+dx,v[1],v[2]+dz)) for v in verts];count=0;misses=0
  for matrix,body in frames:
   bt=BVHTree.FromPolygons([matrix@v for v in vv],faces)
   for name,tr in body:
    touching=bool(bt.overlap(tr))
    if name.startswith('Study_Mitten'):misses+=not touching
    else:count+=touching
  score=(misses,count,dx*dx+dz*dz)
  if best is None or score<best[0]:best=(score,dx,dz)
print('BAT_PALM_FIT',best,flush=True)
dx,dz=best[1:];verts=[[v[0]+dx,v[1],v[2]+dz] for v in verts]
for v,local in zip(me.vertices,verts):v.co=rig.data.bones['HandSocket.R'].matrix_local@Vector(local)
(root/'Library/player-bat-mesh.json').write_text(json.dumps({'vertices':verts,'faces':faces,'materialIndices':indices,'materials':materials,'palm_offset':[dx,0,dz]}))
hits=[];grip_contacts=0
for k in range(769):
 f=1+k/16;s.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();bt=tree(bat,dg)
 for o in parts:
  if o.name.startswith('Study_Mitten'):continue
  if bt.overlap(tree(o,dg)):hits.append([f,o.name])
(out/'BatCollisionCheck.json').write_text(json.dumps({'samples':769,'barrel_or_body_intersections':hits,'grip_cuff_contacts_inside_hands':grip_contacts},indent=2));print('BAT_COLLISIONS',len(hits),hits[:8],flush=True)
if hits:
 s.frame_set(1);s.render.image_settings.file_format='PNG';s.render.filepath=str(out/'BatDebug.png');bpy.ops.render.render(write_still=True)
 dg=bpy.context.evaluated_depsgraph_get();bt=tree(bat,dg)
 for o in parts:
  if o.name.startswith('Study_Mitten'):continue
  pairs=bt.overlap(tree(o,dg))
  if pairs:print('HIT_FACES',o.name,[(a,[round(verts[v][1],3) for v in faces[a]]) for a,b in pairs[:8]],flush=True)
 raise SystemExit(1)
s.frame_set(1);s.render.image_settings.file_format='PNG';s.render.filepath=str(out/'BatFinalHold.png');bpy.ops.render.render(write_still=True)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_TwoHandBat.blend'))
s.camera.location=(4.8,-7,2.7);s.camera.data.ortho_scale=3.1;s.camera.rotation_euler=(Vector((0,-.1,.9))-s.camera.location).to_track_quat('-Z','Y').to_euler()
s.render.resolution_x=800;s.render.resolution_y=800;s.render.resolution_percentage=100;s.eevee.taa_render_samples=64
s.render.image_settings.file_format='FFMPEG';s.render.ffmpeg.format='MPEG4';s.render.ffmpeg.codec='H264';s.render.ffmpeg.constant_rate_factor='HIGH';s.render.filepath=str(out/'BatSwing.mp4');s.frame_start=1;s.frame_end=49;bpy.ops.render.render(animation=True)
print('BAT_READY',flush=True)
