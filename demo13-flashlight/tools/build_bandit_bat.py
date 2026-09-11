import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Bandit/Bandit01'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/Bandit01.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['Bandit01_Rig']
wood=bpy.data.materials.new('Bandit_BatWood');wood.use_nodes=True
wood.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.18,.095,.043,1)
wood.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.88
wrap=bpy.data.materials['Bandit_wrap']
vs=[];fs=[];mats=[];N=10
# Both hands hold along the -Y handle; positive Y points toward the barrel.
profile=[(-.25,.031),(-.23,.034),(-.215,.023),(-.03,.023),(.08,.029),(.23,.043),(.55,.061),(.65,.061),(.69,.044)]
for y,r in profile:
 for i in range(N):
  t=i*math.tau/N;vs.append((math.cos(t)*r,y,math.sin(t)*r))
for j in range(len(profile)-1):
 for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i));mats.append(1 if j in (2,3) else 0)
fs.extend([tuple(reversed(range(N))),tuple((len(profile)-1)*N+i for i in range(N))]);mats.extend([0,0])
d=bpy.data.meshes.new('WoodenBat');d.from_pydata(vs,[],fs);d.materials.append(wood);d.materials.append(wrap)
for p,m in zip(d.polygons,mats):p.material_index=m
bat=bpy.data.objects.new('Bandit_Bat',d);scene.collection.objects.link(bat)
for v in d.vertices:v.co=rig.data.bones['HandSocket.R'].matrix_local@v.co
bat.parent=rig;g=bat.vertex_groups.new(name='HandSocket.R');g.add(list(range(len(d.vertices))),1,'REPLACE');mod=bat.modifiers.new('BatGrip','ARMATURE');mod.object=rig
bat['module']='Removable wooden bat';rig.animation_data.action=bpy.data.actions['SwordWalk'];scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in scene.objects:
 if o.type=='MESH' and o.name.startswith('Bandit_'):o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/Bandit01_WithBat.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'Bandit01_WithBat.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
palette=json.loads((OUT/'Palette.json').read_text());palette['materials']=[e for e in palette['materials'] if e['name']!=wood.name]
palette['materials'].append({'name':wood.name,'color':list(wood.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value),'roughness':.88});(OUT/'PaletteBat.json').write_text(json.dumps(palette,indent=2))
for name,action,frame in [('BatHold','SwordWalk',1),('BatSwing','SwordSlash',18)]:
 rig.animation_data.action=bpy.data.actions[action];scene.frame_set(frame);scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
print('BAT_EXPORT_OK',flush=True)
