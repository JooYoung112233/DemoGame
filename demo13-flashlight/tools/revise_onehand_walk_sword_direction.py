"""Turn the sword slightly inward using only the right wrist rotation."""
import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/ChibiSurvivor/Player/OneHandWalkReview'
OUT=BASE/'ForwardGrip';OUT.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(BASE/'CompactSurvivor_OneHandWalk.blend'))
bpy.context.preferences.filepaths.save_version=0
scene=bpy.context.scene;rig=bpy.data.objects['CompactSurvivor_Rig'];action=bpy.data.actions['Walk_OneHand_Sword']
rig.animation_data.action=action
if action.slots:rig.animation_data.action_slot=action.slots[0]
path='pose.bones["Hand.R"].rotation_quaternion'
def digest():
 return hashlib.sha256(repr([(a.name,[(c.data_path,c.array_index,[(tuple(k.co),k.interpolation) for k in c.keyframe_points]) for c in a.fcurves if not(a==action and c.data_path==path)]) for a in bpy.data.actions]).encode()).hexdigest()
before=digest();rest={b.name:b.matrix_local.copy() for b in rig.data.bones};p=rig.pose.bones['Hand.R']
end=round(action.frame_range[1]);samples=[]
for f in range(1,end+1):
 scene.frame_set(f);bpy.context.view_layer.update()
 socket=rig.pose.bones['HandSocket.R'].matrix.to_quaternion();direction=socket@Vector((0,1,0))
 chest=rig.pose.bones['Chest'].matrix.to_quaternion()@rest['Chest'].to_quaternion().inverted()
 forward=chest@Vector((0,-1,0));yaw=math.atan2(forward.x,-forward.y)
 # Keep a small torso response, with the tip 12 degrees toward the centerline.
 heading=math.radians(12)+yaw*.25;elevation=math.asin(max(-1,min(1,direction.z)))
 desired=Vector((math.sin(heading)*math.cos(elevation),-math.cos(heading)*math.cos(elevation),math.sin(elevation)))
 socket_q=direction.rotation_difference(desired)@socket
 rel=p.matrix.to_quaternion().inverted()@socket
 samples.append((socket_q@rel.inverted(),p.parent.matrix.to_quaternion().copy(),math.degrees(math.atan2(direction.x,-direction.y)),math.degrees(heading)))
prev=None
for f,(world,parent,old,new) in enumerate(samples,1):
 q=rest[p.name].to_quaternion().inverted()@rest[p.parent.name].to_quaternion()@parent.inverted()@world
 if prev is not None and prev.dot(q)<0:q.negate()
 prev=q.copy();p.rotation_quaternion=q;p.keyframe_insert('rotation_quaternion',frame=f,group=p.name)
for c in action.fcurves:
 if c.data_path==path:
  for k in c.keyframe_points:k.interpolation='LINEAR'
assert before==digest(),'A channel outside the right wrist changed'
def tree(o,dg):
 ev=o.evaluated_get(dg);m=ev.to_mesh();t=BVHTree.FromPolygons([ev.matrix_world@v.co for v in m.vertices],[tuple(p.vertices) for p in m.polygons]);ev.to_mesh_clear();return t
hits=[];angles=[]
characters=[o for o in scene.objects if o.type=='MESH' and o.name.startswith('Compact_')]
for f in range(1,end+1):
 scene.frame_set(f);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
 blade=tree(bpy.data.objects['Preview_Sword_Blade'],dg);guard=tree(bpy.data.objects['Preview_Sword_Guard'],dg)
 direction=rig.pose.bones['HandSocket.R'].matrix.to_quaternion()@Vector((0,1,0));angles.append(math.degrees(math.atan2(direction.x,-direction.y)))
 assert abs(angles[-1]-samples[f-1][3])<.01,('Unexpected sword heading',f,angles[-1],samples[f-1][3])
 for o in characters:
  t=tree(o,dg)
  if blade.overlap(t) or (o.name!='Compact_Hands' and guard.overlap(t)):hits.append([f,o.name])
assert not hits,hits
stats=json.loads((BASE/'RetargetCheck.json').read_text())
stats.update({'changed_bones':['Hand.R'],'all_other_animation_channels_unchanged':True,'weapon_surface_intersections':hits,'sword_heading_before_degrees':[min(s[2] for s in samples),max(s[2] for s in samples)],'sword_heading_after_degrees':[min(angles),max(angles)],'heading_sign':'Positive is inward toward the character center from the right hand','approval':'pending','preview_description':'One-handed guard walk with right-wrist-only sword heading correction; gait and arm joint positions preserved.'})
(OUT/'RetargetCheck.json').write_text(json.dumps(stats,indent=2))
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'CompactSurvivor_OneHandWalk_ForwardGrip.blend'))
print('SWORD_DIRECTION_READY',json.dumps({'before':stats['sword_heading_before_degrees'],'after':stats['sword_heading_after_degrees'],'intersections':hits}),flush=True)
