def refine_hero_proportions():
 """Smaller head assembly and an understated athletic torso, keeping joints/UVs."""
 import math
 from mathutils import Vector
 changed=[]
 def torso_scale(z):
  # Fitted waist flowing into chest; broad enough to support the smaller head.
  return .965+.105*math.exp(-((z-1.09)/.115)**2)
 for o in bpy.context.scene.objects:
  if o.type!='MESH' or not o.name.startswith('Hero_') or o.get('natural_proportions_v1'):continue
  n=o.name
  head=n in {'Hero_Head','Hero_FaceDetails','Hero_Nose','Hero_Hair','Hero_Cap'}
  torso=n in {'Hero_JacketBody','Hero_JacketOpening','Hero_ScarfFront','Hero_ScarfWrap','Hero_Neck'} or 'Lapel' in n or 'PackStrap' in n
  sleeve=n in {'Hero_Sleeve_1','Hero_Sleeve_-1'}
  hands=n=='Hero_Hands'
  leg=n in {'Hero_TrouserLeg_1','Hero_TrouserLeg_-1'}
  if not(head or torso or sleeve or hands or leg):continue
  inv=o.matrix_world.inverted()
  for v in o.data.vertices:
   p=o.matrix_world@v.co;x,y,z=p;sign=1 if x>=0 else -1
   if head:
    x*=.90;y=.015+(y-.015)*.90;z=1.20+(z-1.20)*.90
   elif torso:
    x*=torso_scale(z)
    # Straps remain flush: same field as the jacket at matching height.
    y*=.985+.035*math.exp(-((z-1.075)/.12)**2)
   elif sleeve:
    x=sign*.24+(x-sign*.24)*.98
    z-=.009*math.exp(-((z-1.145)/.035)**2)
   elif hands:
    # Round hands retained, slightly less bulbous. Grip centers stay fixed.
    x=sign*.31+(x-sign*.31)*.96;y=-.104+(y+.104)*.96;z=.714+(z-.714)*.96
   elif leg:
    # Ease the balloon-like knee transition without returning to a skinny fit.
    scale=1-.025*math.exp(-((z-.41)/.11)**2);x=sign*.103+(x-sign*.103)*scale;y=.005+(y-.005)*scale
   v.co=inv@Vector((x,y,z))
  o.data.update();o['natural_proportions_v1']=True;changed.append(n)
 return changed
