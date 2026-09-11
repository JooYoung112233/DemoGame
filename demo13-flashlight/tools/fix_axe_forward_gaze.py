# Invoked by the authoring pipeline before export. Only neck/head rotations change.
gaze_paths={'pose.bones["Head"].rotation_quaternion','pose.bones["Neck"].rotation_quaternion'}
def body_digest():return hashlib.sha256(repr([(c.data_path,c.array_index,[tuple(k.co) for k in c.keyframe_points]) for c in action.fcurves if c.data_path not in gaze_paths]).encode()).hexdigest()
body_before=body_digest();gaze_samples=[]
for f in range(1,end+1):
 scene.frame_set(f);bpy.context.view_layer.update();head=rig.pose.bones['Head']
 forward=head.matrix.to_quaternion()@rest['Head'].to_quaternion().inverted()@Vector((0,-1,0))
 gaze_samples.append((head.matrix.to_quaternion().copy(),math.atan2(forward.x,-forward.y),math.asin(max(-1,min(1,forward.z)))))
gaze_after=[];previous_gaze={}
for f,(original,yaw,pitch) in enumerate(gaze_samples,1):
 scene.frame_set(f);bpy.context.view_layer.update()
 yaw=max(math.radians(-7),min(math.radians(7),yaw*.18))
 elevation=math.radians(-2)+pitch*.035
 desired=Quaternion((0,0,1),yaw)@Quaternion((1,0,0),-elevation)@rest['Head'].to_quaternion()
 neck=rig.pose.bones['Neck'];world_delta=desired@original.inverted()
 nq=Quaternion().slerp(world_delta,.35)@neck.matrix.to_quaternion();m=nq.to_matrix().to_4x4();m.translation=neck.head;neck.matrix=m
 bpy.context.view_layer.update();head=rig.pose.bones['Head'];m=desired.to_matrix().to_4x4();m.translation=head.head;head.matrix=m
 for p in [neck,head]:
  q=p.rotation_quaternion.copy()
  if p.name in previous_gaze and previous_gaze[p.name].dot(q)<0:q.negate()
  p.rotation_quaternion=q;previous_gaze[p.name]=q;p.keyframe_insert('rotation_quaternion',frame=f,group=p.name)
 bpy.context.view_layer.update();v=head.matrix.to_quaternion()@rest['Head'].to_quaternion().inverted()@Vector((0,-1,0));gaze_after.append(math.degrees(math.asin(max(-1,min(1,v.z)))))
assert body_digest()==body_before,'Gaze adjustment changed body/weapon channels'
assert min(gaze_after)>-6 and max(gaze_after)<2,gaze_after
gaze_report={'before_min_degrees':math.degrees(min(p[2] for p in gaze_samples)),'after_min_degrees':min(gaze_after),'after_max_degrees':max(gaze_after),'changed_channels':sorted(gaze_paths),'body_and_weapon_channels_preserved':True}
(OUT/'GazeCheck.json').write_text(json.dumps(gaze_report,indent=2))
print('GAZE_CORRECTED',gaze_report,flush=True)
