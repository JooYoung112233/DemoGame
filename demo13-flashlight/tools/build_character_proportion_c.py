"""Approved C proportions, staged outside Unity Assets during combat testing.
Preserve rest skeleton, vertex weights, UVs and every animation key. The existing
model rig root carries vertical scale; no extra bone/path is introduced.
Weapons and their anchors must stay under the same visual root on integration.
"""
import bpy,json,hashlib,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT.parent/'ArtWork/CharacterProportionC'
SOURCES={
 'Player': 'Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_Stage5_Slash.blend',
 'PlayerBat': 'Assets/ChibiSurvivor/Player/SimpleHeroStudy/BlenderSource~/SimpleHero_TwoHandBat.blend',
 'Bandit': 'Assets/ChibiSurvivor/Bandit/SimpleBandit/BlenderSource~/SimpleBandit.blend',
 'BanditCombat': 'Assets/ChibiSurvivor/Bandit/SimpleBandit/BlenderSource~/SimpleBandit_Combat.blend'}
for p in [OUT/'BlenderSource~',OUT/'Models']:p.mkdir(parents=True,exist_ok=True)
results=[]
def digest(x):return hashlib.sha256(repr(x).encode()).hexdigest()
def invariants(rig,parts):
 return {'skeleton':digest([(b.name,b.parent.name if b.parent else None,[list(row) for row in b.matrix_local],list(b.head_local),list(b.tail_local)) for b in rig.data.bones]),
 'animation':digest([(a.name,[(c.data_path,c.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right),k.interpolation) for k in c.keyframe_points]) for c in a.fcurves]) for a in bpy.data.actions]),
 'surface':digest([(o.name,[tuple(p.vertices) for p in o.data.polygons],[[tuple(u.uv) for u in uv.data] for uv in o.data.uv_layers],[[ (g.group,g.weight) for g in v.groups] for v in o.data.vertices]) for o in parts])}
def activate(rig,action,frame):
 rig.animation_data_create();rig.animation_data.action=action
 if action.slots:rig.animation_data.action_slot=action.slots[0]
 bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
for label,path in SOURCES.items():
 src=ROOT/path;sourceHash=hashlib.sha256(src.read_bytes()).hexdigest();bpy.ops.wm.open_mainfile(filepath=str(src));bpy.context.preferences.filepaths.save_version=0
 rig=bpy.data.objects['SimpleHero_Rig'];assert tuple(rig.scale)==(1,1,1),label
 parts=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith(('Study_','Gear_','Bandit_','Hero_'))]
 body=[o for o in parts if o.name not in ['Hero_Bat','Bandit_Bat','Bandit_Club']]
 before=invariants(rig,parts);rig.data.pose_position='REST';bpy.context.view_layer.update()
 for o in parts:
  assert o.parent==rig,(label,o.name,'must inherit the same model root')
 if label.startswith('Player'):
  ho=bpy.data.objects['Study_Head'];hm=rig.matrix_world.inverted()@ho.matrix_world;head=[hm@v.co for v in ho.data.vertices]
 else:
  o=bpy.data.objects['Bandit_Body'];idx=o.vertex_groups['Head'].index
  hm=rig.matrix_world.inverted()@o.matrix_world;head=[hm@v.co for v in o.data.vertices if any(g.group==idx and g.weight>.99 for g in v.groups)]
 pivot=Vector([(min(v[i] for v in head)+max(v[i] for v in head))/2 for i in range(3)])
 oldHeadWidth=max(v.x for v in head)-min(v.x for v in head)
 for o in body:
  toRig=rig.matrix_world.inverted()@o.matrix_world;fromRig=toRig.inverted()
  groups={g.index:g.name for g in o.vertex_groups}
  for v in o.data.vertices:
   p=toRig@v.co
   weights={groups[g.group]:g.weight for g in v.groups}
   isHead=o.name in ['Study_Head','Study_Cap','Study_CapBrim'] or (o.name=='Bandit_Body' and weights.get('Head',0)>.99)
   if isHead:
    delta=p-pivot;p=pivot+Vector((delta.x*.90,delta.y*.90,delta.z*.90/1.08))
   elif o.name in ['Study_Torso','Study_Hips']:p.y*=1.12
   elif o.name.startswith('Gear_'):
    # Shoulder straps, scarf and charm must follow the thicker jacket surface.
    p.y*=1.12
   elif o.name=='Bandit_Body':
    torso=min(1,sum(weights.get(n,0) for n in ['Hips','Spine','Chest']))
    p.y*=1+.12*torso
   v.co=fromRig@p
  o.data.update()
 # Keep a copy of all rig-space poses, including hand/socket attachment matrices.
 rig.data.pose_position='POSE';samples=[]
 for action in bpy.data.actions:
  for frame in range(math.ceil(action.frame_range[0]),math.floor(action.frame_range[1])+1):
   activate(rig,action,frame)
   samples.append((action.name,frame,{n:rig.pose.bones[n].matrix.copy() for n in ['Hand.L','Hand.R','HandSocket.L','HandSocket.R','Foot.L','Foot.R']}))
 rig.scale.z=1.08;bpy.context.view_layer.update()
 after=invariants(rig,parts);assert before==after,(label,'source invariants changed')
 error=0
 for actionName,frame,matrices in samples:
  activate(rig,bpy.data.actions[actionName],frame)
  for name,old in matrices.items():
   current=rig.pose.bones[name].matrix
   error=max(error,max(abs(current[r][c]-old[r][c]) for r in range(4) for c in range(4)))
 assert error<1e-6,(label,error)
 for o in body:
  assert all(math.isfinite(n) for v in o.data.vertices for n in v.co),o.name
  assert o.data.uv_layers and all(p.area>1e-12 for p in o.data.polygons),o.name
 for action in bpy.data.actions:action.use_fake_user=True
 activate(rig,bpy.data.actions['Idle'],1)
 for o in parts:o.hide_render=o not in body
 # Pack existing surface images, never alter the character texture atlases.
 for im in bpy.data.images:
  if im.source=='FILE' and im.has_data and not im.packed_file:im.pack()
 blend=OUT/'BlenderSource~'/f'{label}_C.blend';bpy.ops.wm.save_as_mainfile(filepath=str(blend))
 bpy.ops.object.select_all(action='DESELECT');rig.hide_set(False);rig.select_set(True)
 for o in body:o.hide_set(False);o.select_set(True)
 bpy.context.view_layer.objects.active=rig
 bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/f'{label}_C.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,bake_anim_simplify_factor=0,use_armature_deform_only=False)
 assert hashlib.sha256(src.read_bytes()).hexdigest()==sourceHash,(label,'live source changed')
 results.append({'model':label,'source':path,'sourceSHA256':sourceHash,'sourceUnchanged':True,'preserved':after,'sampledFrames':len(samples),'maxRigLocalAttachmentMatrixError':error,'actions':[a.name for a in bpy.data.actions],'bones':len(rig.data.bones),'headWidthBefore':oldHeadWidth,'headWidthAfter':oldHeadWidth*.9,'blenderRigScale':[1,1,1.08],'unityVisualScale':[1,1.08,1]})
report={'decision':'C approved 2026-09-11: player and bandits','models':results,'integration':'Staged only. No live Unity assets replaced. Preserve exported scale exactly once; do not double-apply scale. Attach weapons/stow mounts under same visual root. Unity firearm/grip, feet and ragdoll checks remain pending.','firearmVariants':'Pistol and rifle share Bandit source; no new enemy archetypes created.'}
(OUT/'Validation.json').write_text(json.dumps(report,indent=2),encoding='utf8');print('PROPORTION_C_COMPLETE',json.dumps(report))
