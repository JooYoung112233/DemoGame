def clear_hero_elbow_cuffs(rig):
 """Roll sleeves above the hinge and extend the covered upper-arm skin.
 Topology/UVs/bone weights stay compatible with the surface atlas and clips.
 """
 changed=[]
 for side,sign in [('L',1),('R',-1)]:
  bone=rig.data.bones['UpperArm.'+side];up=(bone.head_local-bone.tail_local).normalized();shift=up*.075
  for name in ['Hero_RolledSleeve_'+str(sign),'Hero_Sleeve_'+str(sign),'Hero_Forearm_'+str(sign)]:
   o=bpy.data.objects.get(name)
   if o is None or o.get('elbow_clearance_v1'):continue
   for v in o.data.vertices:
    z=v.co.z
    if 'RolledSleeve' in name:t=1
    elif 'Forearm' in name:t=max(0,min(1,(z-.957)/(.981-.957)))*.91
    else:t=max(0,min(1,(1.10-z)/(1.10-.947)))
    v.co+=shift*t
   o['elbow_clearance_v1']=True;changed.append(name)
 return changed
