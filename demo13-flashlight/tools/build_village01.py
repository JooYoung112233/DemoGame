"""Village props in metres. Reuse established safehouse construction helpers only."""
from pathlib import Path
helpers=Path(__file__).with_name('build_safehouse01.py').read_text(encoding='utf8').split('# Architecture:')[0]
exec(helpers.replace('Safehouse01','Village01').replace('Safehouse_','Village_'))
exec(Path(__file__).with_name('village01_geometry.py').read_text(encoding='utf8'))

stats={}
for name,a in assets.items():
 meshes=[o for o in a['col'].objects if o.type=='MESH']
 for o in meshes:
  bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
  bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(70),island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT');o['editable_part']=True
 bpy.ops.object.select_all(action='DESELECT')
 for o in a['col'].objects:o.select_set(True)
 bpy.context.view_layer.objects.active=a['root']
 bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/(name+'.fbx')),use_selection=True,object_types={'EMPTY','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 stats[name]={'meshes':len(meshes),'triangles':sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons),'colliders':a['colliders'],'uv_mapped':True}
 # Separate review copies, never bake the display arrangement into FBX.
 mapping={}
 for o in a['col'].objects:
  cp=o.copy();layout.objects.link(cp);mapping[o]=cp
 for o,cp in mapping.items():cp.parent=mapping.get(o.parent,None)
 idx=list(assets).index(name);r=mapping[a['root']];r.location=((idx%5-2)*4.6,(idx//5)*4.2,0)
 if name in ['Window01','Awning01','Shutter01','ShopSign01']:r.location.z=1.6
library.hide_render=True;library.hide_viewport=True
def aim(o,p):o.rotation_euler=(Vector(p)-o.location).to_track_quat('-Z','Y').to_euler()
for name,pos,power,color in [('Key',(0,-3,14),2400,(1,.87,.69)),('Fill',(-9,5,10),1700,(.72,.83,1)),('Rim',(8,14,12),2200,(1,.9,.74))]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=9
 o=bpy.data.objects.new(name,d);studio.objects.link(o);o.location=pos;aim(o,(0,6,0))
sc.world=bpy.data.worlds.new('VillageStudio');sc.world.use_nodes=True;sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.19,.21,.23,1);sc.world.node_tree.nodes['Background'].inputs[1].default_value=.45
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.07));bpy.context.object.data.materials.append(mats['Concrete'])
cd=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',cd);studio.objects.link(cam);cam.location=(21,-27,30);aim(cam,(0,6,0));cd.type='ORTHO';cd.ortho_scale=27;sc.camera=cam
sc.render.engine='CYCLES';sc.cycles.samples=24;sc.cycles.use_denoising=True;sc.render.resolution_x=1700;sc.render.resolution_y=1150;sc.render.resolution_percentage=100;sc.view_settings.view_transform='AgX'
sc.render.filepath=str(OUT/'Previews/PropLibrary.png')
(OUT/'KitManifest.json').write_text(json.dumps({'units':'metres','assets':stats,'palette':{('Village_'+n):{'color':list(c),'metallic':.4 if n in ['Steel','EdgeMetal'] else 0,'smoothness':.3 if n in ['Steel','EdgeMetal'] else .14,'emission':1.1 if n=='LampGlow' else 0} for n,c in palette.items()}},indent=2),encoding='utf8')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/Village01.blend'))
if '--no-render' not in sys.argv:bpy.ops.render.render(write_still=True)
print('VILLAGE_COMPLETE',json.dumps({'assets':len(assets),'triangles':sum(a['triangles'] for a in stats.values())}))
