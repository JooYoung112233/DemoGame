"""Support palm may roll around the shaft; the weapon stays fixed to the right hand."""
def solve_left_grip(rig,roll_hint=None,previous_roll=None):
 upper=rig.pose.bones['UpperArm.L'];fore=rig.pose.bones['Forearm.L'];hand=rig.pose.bones['Hand.L']
 socket=rig.pose.bones['HandSocket.R'].matrix;grip=socket@Vector((0,-.16,0));shoulder=upper.head.copy()
 rel=rig.data.bones['Hand.L'].matrix_local.inverted()@rig.data.bones['HandSocket.L'].matrix_local
 a=upper.bone.length;b=fore.bone.length;oldelbow=fore.head.copy()
 bodyq=rig.pose.bones['Chest'].matrix.to_quaternion()@rig.data.bones['Chest'].matrix_local.to_quaternion().inverted()
 desiredpole=bodyq@Vector((1,-.25,-.65));best=None
 # A round mitten has no finger pose to preserve. Keep a stable palm roll,
 # then allow a small swing at the grip instead of twisting around the shaft.
 angles=[roll_hint if roll_hint is not None else math.radians(-100)]
 for theta in angles:
  q=socket.to_quaternion()@Quaternion((0,1,0),theta)@rel.to_quaternion().inverted()
  for iteration in range(12):
   wrist=grip-q@rel.translation;delta=wrist-shoulder;dist=delta.length
   if dist>=a+b-.0001:
    offset=q@rel.translation
    q=offset.rotation_difference(grip-shoulder)@q
    wrist=grip-q@rel.translation;delta=wrist-shoulder;dist=delta.length
   if dist>=a+b-.0001 or dist<=abs(a-b)+.0001:break
   direction=delta/dist;along=(a*a-b*b+dist*dist)/(2*dist);pole=desiredpole-direction*desiredpole.dot(direction)
   if pole.length<.001:break
   elbow=shoulder+direction*along+pole.normalized()*math.sqrt(max(0,a*a-along*along))
   handaxis=q@Vector((0,1,0));foreaxis=(wrist-elbow).normalized();angle=handaxis.angle(foreaxis)
   if angle<=math.radians(12.1) or iteration==11:break
   correction=handaxis.rotation_difference(foreaxis)
   q=Quaternion().slerp(correction,(angle-math.radians(12))/angle)@q
  if dist>=a+b-.0001 or dist<=abs(a-b)+.0001:continue
  bend=(wrist-elbow).angle(q@Vector((0,1,0)))
  cost=bend*bend*3+(elbow-oldelbow).length_squared*1.5
  if previous_roll is not None:
   difference=(theta-previous_roll+math.pi)%math.tau-math.pi;cost+=difference*difference*.18
  if best is None or cost<best[0]:best=(cost,theta,q,wrist,elbow,bend)
 if best is None:raise RuntimeError(f'Support hand has no reachable grip: distance={dist}, lengths={a},{b}, offset={rel.translation.length}, gripdistance={(grip-shoulder).length}')
 _,theta,q,wrist,elbow,bend=best
 # Both bones must share one hinge plane. A shortest-arc aim from each
 # imported quaternion keeps their unrelated axial rolls and twists the
 # vertices blended between upper arm and forearm (candy-wrapper elbow).
 rest_upper=(upper.bone.tail_local-upper.bone.head_local).normalized()
 rest_fore=(fore.bone.tail_local-fore.bone.head_local).normalized()
 rest_hinge=rest_upper.cross(rest_fore).normalized()
 posed_hinge=(elbow-shoulder).cross(wrist-elbow).normalized()
 def hinge_frame(direction,normal):
  y=direction.normalized();x=(normal-y*normal.dot(y)).normalized();z=x.cross(y).normalized()
  return Matrix((x,y,z)).transposed().to_quaternion()
 for pb,h,t in [(upper,shoulder,elbow),(fore,elbow,wrist)]:
  rest_frame=hinge_frame(pb.bone.tail_local-pb.bone.head_local,rest_hinge)
  posed_frame=hinge_frame(t-h,posed_hinge)
  newq=posed_frame@rest_frame.inverted()@pb.bone.matrix_local.to_quaternion()
  m=newq.to_matrix().to_4x4();m.translation=h;pb.matrix=m;bpy.context.view_layer.update()
 m=q.to_matrix().to_4x4();m.translation=wrist;hand.matrix=m;bpy.context.view_layer.update()
 if previous_roll is not None:theta=previous_roll+(theta-previous_roll+math.pi)%math.tau-math.pi
 return theta,math.degrees(bend),(rig.pose.bones['HandSocket.L'].head-grip).length
