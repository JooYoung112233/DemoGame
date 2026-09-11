exec(Path(__file__).with_name('axe_left_grip_solver.py').read_text(encoding='utf8'))
previous_roll=None;previous_quats={};left_report=[]
for f in range(1,end+1):
 scene.frame_set(f);bpy.context.view_layer.update()
 fore=rig.pose.bones['Forearm.L'];hand=rig.pose.bones['Hand.L'];before=math.degrees((fore.tail-fore.head).angle(hand.tail-hand.head))
 theta,bend,error=solve_left_grip(rig,previous_roll=previous_roll);previous_roll=theta
 for n in ['UpperArm.L','Forearm.L','Hand.L']:
  p=rig.pose.bones[n];q=p.rotation_quaternion.copy()
  if n in previous_quats and previous_quats[n].dot(q)<0:q.negate()
  p.rotation_quaternion=q;previous_quats[n]=q
  p.keyframe_insert('rotation_quaternion',frame=f,group=n);p.keyframe_insert('location',frame=f,group=n)
 left_report.append({'frame':f,'roll':theta,'wrist_before':before,'wrist_after':bend,'grip_error':error})
for fc in action.fcurves:
 for k in fc.keyframe_points:k.interpolation='LINEAR'
(OUT/'LeftHandFix.json').write_text(json.dumps({'frames':left_report,'max_wrist_before':max(p['wrist_before'] for p in left_report),'max_wrist_after':max(p['wrist_after'] for p in left_report),'max_grip_error':max(p['grip_error'] for p in left_report)},indent=2))
print('LEFT_HAND_FIXED',max(p['wrist_before'] for p in left_report),max(p['wrist_after'] for p in left_report),flush=True)
