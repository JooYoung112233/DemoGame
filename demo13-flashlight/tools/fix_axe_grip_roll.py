# Preserve a continuous grip frame instead of rebuilding roll from world-up.
# World-up look-at has a pole singularity when the shaft passes through vertical.
fitcode=Path(__file__).with_name('build_dark_survivor_game.py').read_text(encoding='utf8')
fitcode=fitcode[fitcode.index('def fit_sword_grip(pose):'):fitcode.index('clips=[]')]
fitcode=fitcode.replace("socketq=axis.to_track_quat('Y','Z')","socketq=pose['HandSocket.R'][0].to_quaternion()")
exec(fitcode)
poses=[];axes=[]
for f in range(1,end+1):
 scene.frame_set(f);bpy.context.view_layer.update();poses.append({p.name:(p.matrix.copy(),p.location.copy()) for p in rig.pose.bones})
 axes.append((rig.pose.bones['HandSocket.R'].matrix.to_quaternion()@Vector((0,1,0))).normalized())
strike=15;y=axes[strike];edge=Vector((0,0,-1));edge=(edge-y*edge.dot(y)).normalized();z=-edge;x=y.cross(z).normalized()
seed=Matrix((x,y,z)).transposed().to_quaternion();qs=[None]*end;qs[strike]=seed
for i in range(strike+1,end):qs[i]=axes[i-1].rotation_difference(axes[i])@qs[i-1]
for i in range(strike-1,-1,-1):qs[i]=axes[i+1].rotation_difference(axes[i])@qs[i+1]
# Close the stance during recovery, distributing any accumulated roll smoothly.
closure=qs[-1].inverted()@qs[0]
for i in range(23,end):
 t=(i-23)/(end-1-23);t=t*t*(3-2*t);qs[i]=qs[i]@Quaternion().slerp(closure,t)
prev={};arm_names=['UpperArm.R','Forearm.R','Hand.R','UpperArm.L','Forearm.L','Hand.L'];errors=[]
for i,(pose,q) in enumerate(zip(poses,qs)):
 scene.frame_set(i+1);bpy.context.view_layer.update()
 for side in ['R','L']:
  n='HandSocket.'+side;m=q.to_matrix().to_4x4();m.translation=pose[n][0].translation;pose[n]=(m,pose[n][1])
 fit_sword_grip(pose);bpy.context.view_layer.update()
 for n in arm_names:
  p=rig.pose.bones[n];local=p.rotation_quaternion.copy()
  if n in prev and prev[n].dot(local)<0:local.negate()
  p.rotation_quaternion=local;prev[n]=local
  p.keyframe_insert('rotation_quaternion',frame=i+1,group=n);p.keyframe_insert('location',frame=i+1,group=n)
 actual=rig.pose.bones['HandSocket.R'].matrix.to_quaternion().normalized();target=q.normalized()
 errors.append(math.degrees(2*math.acos(min(1,abs(actual.dot(target))))))
for fc in action.fcurves:
 for k in fc.keyframe_points:k.interpolation='LINEAR'
report={'method':'Parallel transport of shared grip; continuous recovery to initial stance','strike_edge_direction':'Downward projected perpendicular to shaft','max_grip_orientation_error_degrees':max(errors),'weapon_bound_to_right_hand_socket':True}
(OUT/'GripRollCheck.json').write_text(json.dumps(report,indent=2));print('GRIP_ROLL_FIXED',report,flush=True)
