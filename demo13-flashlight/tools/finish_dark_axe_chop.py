"""Continue after fitting shared hand grip; review-only animations and removable proxy."""
from mathutils import Matrix,Quaternion
# The accepted character, its five existing motions and game controller stay untouched.
old_hash=digest()
base=[]
for f in range(1,end+1):
 scene.frame_set(f);bpy.context.view_layer.update()
 # Refit the floor once per pose because the resized body has different boot proportions.
 floor=100
 for o in characters:
  if not o.name.startswith('Hero_Sole'):continue
  ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();floor=min(floor,min((ev.matrix_world@v.co).z for v in me.vertices));ev.to_mesh_clear()
 h=rig.pose.bones['Hips'];h.location+=h.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-floor));h.keyframe_insert('location',frame=f,group=h.name)
 bpy.context.view_layer.update();base.append({p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones})

def mix(a,b,t):return {n:(a[n][0].lerp(b[n][0],t),a[n][1].slerp(b[n][1],t),a[n][2].lerp(b[n][2],t)) for n in a}
def at(frames,t):
 t=max(0,min(len(frames)-1,t));i=int(t);return mix(frames[i],frames[min(i+1,len(frames)-1)],t-i)
# Reduce the acrobatic lunge while retaining the source strike's timing.
arm=rig.data;newrest=rest;bone_data={b.name:(b.head_local.copy(),b.tail_local.copy(),b.use_deform) for b in arm.bones};grip_errors=[]
fitcode=Path(__file__).with_name('build_dark_survivor_game.py').read_text(encoding='utf8')
exec(fitcode[fitcode.index('def fit_sword_grip(pose):'):fitcode.index('clips=[]')])
guard=base[0];compact=[];prev={}
for f,pose in enumerate(base,1):
 scene.frame_set(f)
 bpy.context.view_layer.update();source_chest=rig.pose.bones['Chest'].head.copy();source_sockets={s:rig.pose.bones['HandSocket.'+s].matrix.copy() for s in ['R','L']}
 for p in rig.pose.bones:
  n=p.name;loc,q,scale=pose[n];g=guard[n]
  amount=.60 if n in ['Root','Hips','Spine','Chest'] else .55 if n.startswith(('Thigh','Shin','Foot')) else .78 if n.startswith(('Clavicle','UpperArm','Forearm')) else .68 if n in ['Head','Neck'] else 1.0
  p.location=g[0].lerp(loc,.60 if n in ['Hips','Root'] else amount);p.rotation_quaternion=g[1].slerp(q,amount);p.scale=scale
 bpy.context.view_layer.update()
 fitpose={p.name:(p.matrix.copy(),p.location.copy()) for p in rig.pose.bones}
 # Reduce reach, but keep the original weapon swing arc. Damping the wrists
 # with the torso would turn the actual chop into a small sideways gesture.
 for side,m in source_sockets.items():
  m.translation=rig.pose.bones['Chest'].head+(m.translation-source_chest)*.85
  fitpose['HandSocket.'+side]=(m,rig.pose.bones['HandSocket.'+side].location.copy())
 fit_sword_grip(fitpose)
 floor=100
 for o in characters:
  if not o.name.startswith('Hero_Sole'):continue
  ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();floor=min(floor,min((ev.matrix_world@v.co).z for v in me.vertices));ev.to_mesh_clear()
 h=rig.pose.bones['Hips'];h.location+=h.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-floor));bpy.context.view_layer.update()
 for p in rig.pose.bones:
  q=p.rotation_quaternion.copy()
  if p.name in prev and prev[p.name].dot(q)<0:q.negate()
  p.rotation_quaternion=q;prev[p.name]=q
  for prop in ['location','rotation_quaternion','scale']:p.keyframe_insert(prop,frame=f,group=p.name)
 compact.append({p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in rig.pose.bones})
base=compact
exec(Path(__file__).with_name('fix_axe_forward_gaze.py').read_text(encoding='utf8'))
exec(Path(__file__).with_name('fix_axe_grip_roll.py').read_text(encoding='utf8'))
exec(Path(__file__).with_name('fix_axe_left_hand.py').read_text(encoding='utf8'))
actions={'AxeChop01':action}
assert digest()==old_hash,'Existing locomotion changed'
# Simple scale/grip proxy, not the final axe artwork.
steel=bpy.data.materials.get('Hero_WeaponSteel');wood=bpy.data.materials.get('Hero_leather')
red=bpy.data.materials.new('AxeProxy_Red');red.use_nodes=True;red.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.24,.064,.038,1);red.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.82
proxy=[]
for label,pos,size,mat in [('Handle',(0,.16,0),(.047,.80,.047),wood),('Head',(0,.57,0),(.28,.15,.065),red),('Edge',(.16,.57,0),(.065,.19,.04),steel)]:
 bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.name='Review_Axe_'+label;o.location=pos;o.dimensions=size
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mat)
 bevel=o.modifiers.new('Broad_Edge','BEVEL');bevel.width=.008;bevel.segments=1;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=bevel.name)
 # Blade edge follows the downstroke: rotate the head around the shaft, not the hands.
 for v in o.data.vertices:v.co=rest['HandSocket.R']@Matrix.Rotation(math.pi/2,4,'Y')@v.co
 g=o.vertex_groups.new(name='HandSocket.R');g.add(list(range(len(o.data.vertices))),1,'REPLACE');o.parent=rig;mod=o.modifiers.new('HandSocket','ARMATURE');mod.object=rig;proxy.append(o)
bpy.data.objects['Hero_SwordProxy'].hide_render=True;bpy.data.objects['Hero_SwordProxy'].hide_set(True)
exec(Path(__file__).with_name('apply_hero_surface_to_scene.py').read_text(encoding='utf8'))
print('SURFACE_APPLIED',apply_hero_surface_to_scene(ROOT),flush=True)
exec(Path(__file__).with_name('clear_hero_elbow_cuffs.py').read_text(encoding='utf8'))
print('ELBOW_CLEARANCE',clear_hero_elbow_cuffs(rig),flush=True)
exec(Path(__file__).with_name('fix_hero_cap_join.py').read_text(encoding='utf8'))
fix_hero_cap_join(bpy.data.objects['Hero_Cap'])
exec(Path(__file__).with_name('refine_hero_workwear.py').read_text(encoding='utf8'))
refine_hero_workwear()
exec(Path(__file__).with_name('refine_hero_proportions.py').read_text(encoding='utf8'))
refine_hero_proportions()
checks={}
for name,a in actions.items():
 rig.animation_data.action=a;errors=[];floors=[]
 for f in range(1,int(a.frame_range[1])+1):
  scene.frame_set(f);bpy.context.view_layer.update()
  socket=rig.pose.bones['HandSocket.R'].matrix
  errors.append((rig.pose.bones['HandSocket.L'].head-socket@Vector((0,-.16,0))).length)
 checks[name]={'frames':int(a.frame_range[1]),'seconds':(a.frame_range[1]-1)/30,'max_support_hand_error_m':max(errors)}
assert max(c['max_support_hand_error_m'] for c in checks.values())<.012,checks
rig.animation_data.action=actions['AxeChop01'];scene.frame_start=1;scene.frame_end=37;scene.frame_set(1);scene.render.fps=30
sourceDir=OUT/'BlenderSource~';sourceDir.mkdir(exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(sourceDir/'DarkSurvivor_AxeChop.blend'))
# Export the three review actions only on the current skeleton; existing source stays unchanged.
for a in list(bpy.data.actions):
 if a.name not in actions:bpy.data.actions.remove(a)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in characters+proxy:
 if o.name!='Hero_SwordProxy':o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'DarkSurvivor_AxeChop.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
(OUT/'ChopReview.json').write_text(json.dumps({'source':'ExplosiveLLC/2Hand-Sword-Attack5','clips':checks,'amplitude_from_guard':{'torso':.60,'legs':.55,'arms':.78,'head_neck':.68},'axe_head_roll_degrees':90,'grip_spacing_m':.16,'gameplay_connected':False,'existing_actions_unchanged':True,'status':'Awaiting visual approval'},indent=2))
scene.camera.location=(-3.7,-6,2.8);scene.camera.rotation_euler=(Vector((0,-.15,1.05))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=2.75
scene.render.engine='BLENDER_EEVEE_NEXT';scene.eevee.taa_render_samples=16;scene.render.resolution_x=600;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
for f in [9,13,16,23]:
 rig.animation_data.action=actions['AxeChop01'];scene.frame_set(f);scene.render.filepath=str(CACHE/f'ChopPose_{f}.png');bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(CACHE/'FittedReview.blend'))
print('AXE_REVIEW_READY',json.dumps(checks),flush=True)
