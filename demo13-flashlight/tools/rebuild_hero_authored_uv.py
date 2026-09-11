"""Author grouped, non-mirrored UV charts and restrained painted surface maps.
No geometry, skin weights, skeleton or motion edits. Atlas layout is reproducible.
"""
import bpy, numpy as np, math, json, hashlib, shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor';OUT=BASE/'SurfaceReview'
CACHE=ROOT/'Library/CodexBlender/HeroAuthoredUV';CACHE.mkdir(parents=True,exist_ok=True)
TEX=OUT/'Textures';N=2048
source=BASE/'BlenderSource~/DarkSurvivor.blend'
backup=CACHE/'Before.blend'
if not backup.exists():shutil.copy2(source,backup)
for p in TEX.glob('Hero_*.png'):
 if not (CACHE/p.name).exists():shutil.copy2(p,CACHE/p.name)
bpy.ops.wm.open_mainfile(filepath=str(source));bpy.context.preferences.filepaths.save_version=0
sc=bpy.context.scene;rig=bpy.data.objects['DarkSurvivor_Rig']
parts=sorted([o for o in sc.objects if o.type=='MESH' and o.name.startswith('Hero_')],key=lambda o:o.name)
if bpy.data.objects.get('Hero_Nose'):
 parts=[o for o in parts if o.name!='Hero_Head'] # Dedicated expression atlas stays intact.
def invariant():
 return hashlib.sha256(repr(([(o.name,[tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons], [[(g.group,g.weight) for g in v.groups] for v in o.data.vertices]) for o in parts],[(b.name,tuple(b.head_local),tuple(b.tail_local),b.parent.name if b.parent else None) for b in rig.data.bones],[(a.name,[(f.data_path,f.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right)) for k in f.keyframe_points]) for f in a.fcurves]) for a in bpy.data.actions])).encode()).hexdigest()
before=invariant();rig.data.pose_position='REST';bpy.context.view_layer.update()
regions={'FACE_BODY':(.02,.62,.44,.36),'SHIRT':(.48,.62,.50,.36),'TROUSERS':(.02,.28,.44,.32),'CAP':(.48,.30,.30,.30),'BACKPACK':(.79,.30,.19,.30),'SMALL_PARTS':(.02,.02,.96,.24)}
def bucket(n):
 if n in ['Hero_Head','Hero_FaceDetails','Hero_Hair','Hero_Neck','Hero_Hands'] or 'Forearm' in n:return 'FACE_BODY'
 if any(k in n for k in ['Jacket','Sleeve','Lapel','Scarf']):return 'SHIRT'
 if any(k in n for k in ['Trouser','Hips']):return 'TROUSERS'
 if n=='Hero_Cap':return 'CAP'
 if n in ['Hero_Backpack','Hero_BackpackFlap']:return 'BACKPACK'
 return 'SMALL_PARTS'
profiles=json.loads((OUT/'SurfaceCheck.json').read_text())['profiles']
profile={r['material']:r for r in profiles}
charts=[];positions={}
for o in parts:
 m=o.data;vs=np.array([tuple(o.matrix_world@v.co) for v in m.vertices]);positions[o.name]=vs
 keys={};edges={}
 for p in m.polygons:
  normal=o.matrix_world.to_3x3()@p.normal;axis=max(range(3),key=lambda k:abs(normal[k]));key=(p.material_index,axis,normal[axis]>=0)
  keys[p.index]=key
  for e in p.edge_keys:edges.setdefault(tuple(sorted(e)),[]).append(p.index)
 adj={p.index:set() for p in m.polygons}
 for ids in edges.values():
  for a in ids:
   for b in ids:
    if keys[a]==keys[b]:adj[a].add(b)
 pending=set(adj)
 while pending:
  seed=min(pending);pending.remove(seed);group=[seed];queue=[seed]
  while queue:
   for b in sorted(adj[queue.pop()]&pending):pending.remove(b);queue.append(b);group.append(b)
  mat,axis,pos=keys[seed];axes=[i for i in range(3) if i!=axis]
  verts=sorted({v for f in group for v in m.polygons[f].vertices});xy=vs[verts][:,axes];lo=xy.min(0);span=np.maximum(xy.max(0)-lo,.002)
  charts.append(dict(obj=o,faces=group,axis=axis,axes=axes,positive=pos,lo=lo,span=span,region=bucket(o.name),material=mat))
 # Keep exactly one production UV channel; old unwrap retained in the backup.
 while len(m.uv_layers):m.uv_layers.remove(m.uv_layers[0])
 m.uv_layers.new(name='SurfaceUV')

def pack(items,w,h,scale):
 boxes=sorted([(max(14,int(c['span'][0]*scale))+16,max(14,int(c['span'][1]*scale))+16,i) for i,c in enumerate(items)],key=lambda a:(-a[1],-a[0],a[2]))
 x=y=row=0;result={}
 for bw,bh,i in boxes:
  if x+bw>w:x=0;y+=row;row=0
  if y+bh>h:return None
  result[i]=(x+8,y+8,bw-16,bh-16);x+=bw;row=max(row,bh)
 return result
for region,(rx,ry,rw,rh) in regions.items():
 items=[c for c in charts if c['region']==region];lo=1;hi=12000
 for _ in range(24):
  mid=(lo+hi)/2
  if pack(items,int(rw*N),int(rh*N),mid) is None:hi=mid
  else:lo=mid
 packed=pack(items,int(rw*N),int(rh*N),lo)
 if packed is None:raise RuntimeError('UV region too crowded: '+region)
 for i,c in enumerate(items):
  x,y,w,h=packed[i];c['rect']=(int(rx*N)+x,int(ry*N)+y,w,h)
  for fi in c['faces']:
   for li in c['obj'].data.polygons[fi].loop_indices:
    vi=c['obj'].data.loops[li].vertex_index;xy=positions[c['obj'].name][vi,c['axes']]
    uv=(xy-c['lo'])/c['span'];c['obj'].data.uv_layers.active.data[li].uv=((c['rect'][0]+uv[0]*w)/N,(c['rect'][1]+uv[1]*h)/N)
print('UV_CHARTS',len(charts),flush=True)
base=np.zeros((N,N,4),np.float32);base[:]=(.035,.03,.023,1)
mask=np.zeros_like(base);mask[:]= (0,1,0,.15)
height=np.zeros((N,N),np.float32);occupied=np.zeros((N,N),bool)
def gaussian(v,c,w):return np.exp(-((v-c)/w)**2)
def paint(name,mat,p):
 x,y,z=p.T;pr=profile[mat];col=np.tile(np.array(pr['base_color'][:3],np.float32),(len(p),1));h=np.zeros(len(p));ao=np.ones(len(p))
 front=np.clip((-y-.025)/.075,0,1);back=np.clip((y-.12)/.09,0,1)
 def tint(amount,c):
  nonlocal col
  a=np.clip(amount,0,1)[:,None];col=col*(1-a)+np.array(c)*a
 def shade(a):
  nonlocal col
  col*=np.asarray(a)[:,None]
 def line(v,c,w,strength=.18):
  nonlocal h
  a=gaussian(v,c,w);shade(1-strength*a);h-=a*.15
 cloth=pr['surface']=='cloth';leather=pr['surface']=='leather'
 if cloth or leather:
  # Broad value design, not random dirt or baked directional illumination.
  shade(.96+.045*np.sin(x*12+z*8)+.025*np.cos(y*17-z*13))
 if mat in ['Hero_skin','Hero_ear']:
  shade(.97+.035*np.tanh((z-1.30)*8))
  if name=='Hero_Head':
   tint((gaussian(x,.20,.067)+gaussian(x,-.20,.067))*gaussian(z,1.33,.050)*front*.19,(.40,.175,.09))
   shade(1-.08*gaussian(z,1.217,.032))
 if name=='Hero_JacketBody':
  shade(1-.09*gaussian(z,.834,.012)-.07*gaussian(z,1.154,.019))
  for cx in [-.085,.085]:
   dx=np.abs(x-cx);dz=np.abs(z-1.006);inside=(dx<.043)&(dz<.047)
   tint(front*inside*.12,(.12,.145,.105))
   edge=np.maximum(gaussian(dx,.043,.0025)*(dz<.05),gaussian(dz,.047,.0025)*(dx<.045))*front
   shade(1-.27*edge);h-=edge*.25
   flap=gaussian(z,1.036,.003)*(dx<.043)*front;shade(1-.28*flap);h-=flap*.3
   stitch=gaussian(dx,.037,.0013)*(dz<.039)*((np.sin(z*1100)>0).astype(float))*front
   tint(stitch*.22,(.32,.34,.23))
  # Mild sewn panel below arm; no large noisy patches.
  shade(1-.10*gaussian(np.abs(x),.173,.003))
 if 'TrouserLeg' in name:
  sign=1 if '_1' in name else -1;localx=(x-sign*.113)*sign
  # Shallow bent-knee fold and a diagonal hip fold, continuous across charts.
  fold=z-(.454+.21*localx)
  shade(1-.16*gaussian(fold,0,.009)*front+.10*gaussian(fold,.014,.012)*front)
  fold2=z-(.66-.45*localx);shade(1-.10*gaussian(fold2,0,.007)*front)
  side=gaussian(np.abs(y-.005),0,.016)*gaussian(localx,.105,.018);shade(1-.22*side);h-=side*.2
  tint(np.clip((.34-z)/.18,0,1)*.12,(.13,.10,.065))
 if mat in ['Hero_cuff','Hero_trouser_cuff']:
  zs=positions[name][:,2];edge=np.minimum(z-zs.min(),zs.max()-z)
  shade(1-.15*gaussian(edge,.006,.002));h-=gaussian(edge,.006,.002)*.15
 if name=='Hero_Cap':
  if mat=='Hero_cap':
   theta=np.arctan2(x,-y);seam=np.exp(-(np.sin(theta*3)/.035)**2)*np.clip((z-1.51)/.10,0,1)
   shade(1-.20*seam);h-=seam*.2
   shade(1-.13*gaussian(z,1.52,.009))
  elif mat=='Hero_brim':
   edge=np.sqrt((x/.32)**2+((y+.04)/.37)**2)
   seam=gaussian(edge,.88,.012);shade(1-.18*seam);h-=seam*.14
  elif mat=='Hero_patch':
   vs=positions[name];shade(.99+.025*np.cos(x*25))
 if name in ['Hero_Backpack','Hero_BackpackFlap']:
  xx=np.abs(x);edge=gaussian(xx,.12,.0028);shade(1-.22*edge);h-=edge*.2
  if name=='Hero_Backpack':
   shade(1-.12*gaussian(z,.97,.017)*back)
   border=np.maximum(gaussian(xx,.102,.0025)*(z<.978)*(z>.87),gaussian(z,.877,.0025)*(xx<.103))*back
   shade(1-.25*border);h-=border*.22
  else:
   line(z,1.005,.003,.18)
   tint(gaussian(x,-.055,.025)*gaussian(z,1.064,.03)*back*.08,(.28,.25,.16))
 if 'Boot' in name:
  tint(np.clip((.13-z)/.13,0,1)*.16,(.20,.14,.085))
  if 'Shaft' not in name:
   line(y,-.115,.003,.18)
   for cy in [-.06,-.03,0]:
    lace=gaussian(y,cy,.003)*(np.abs(x-np.sign(x)*.103)<.041)*(z>.13)
    tint(lace*.3,(.09,.058,.032))
 if 'PackStrap' in name or name=='Hero_Belt':
  # Thin edge wear instead of a new strip of geometry.
  if 'PackStrap' in name:
   cx=np.sign(x)*.139;edge=gaussian(np.abs(x-cx),.016,.002);tint(edge*.16,(.23,.17,.095))
  else:line(z,.770,.0015,.18);line(z,.795,.0015,.18)
 return np.clip(col,0,1),h,ao,pr

svg=['<svg xmlns="http://www.w3.org/2000/svg" width="2048" height="2048" viewBox="0 0 2048 2048"><rect width="2048" height="2048" fill="#22282b"/>']
for region,(x,y,w,h) in regions.items():
 svg.append(f'<rect x="{x*N}" y="{(1-y-h)*N}" width="{w*N}" height="{h*N}" fill="none" stroke="#d7bd81"/><text x="{x*N+5}" y="{(1-y-h)*N+24}" fill="#ffffff" font-size="22">{region}</text>')
for ci,c in enumerate(charts):
 o=c['obj'];m=o.data;mat=m.materials[c['material']].name;mat=mat.removeprefix('Surface_').split('.')[0]
 m.calc_loop_triangles();faceids=set(c['faces'])
 for tri in m.loop_triangles:
  if tri.polygon_index not in faceids:continue
  uv=np.array([tuple(m.uv_layers.active.data[l].uv) for l in tri.loops])*N
  a,b,d=uv;v0=b-a;v1=d-a;den=v0[0]*v1[1]-v1[0]*v0[1]
  if abs(den)<1e-7:continue
  lo=np.maximum(np.floor(uv.min(0)).astype(int),0);hi=np.minimum(np.ceil(uv.max(0)).astype(int),N-1)
  yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];dx=xx+.5-a[0];dy=yy+.5-a[1]
  u=(dx*v1[1]-v1[0]*dy)/den;v=(v0[0]*dy-dx*v0[1])/den;ok=(u>=-1e-5)&(v>=-1e-5)&(u+v<=1+1e-5)
  px=xx[ok];py=yy[ok];u=u[ok];v=v[ok]
  if len(px)==0:continue
  xyz=positions[o.name][list(tri.vertices)];p=xyz[0]+u[:,None]*(xyz[1]-xyz[0])+v[:,None]*(xyz[2]-xyz[0])
  color,hh,ao,pr=paint(o.name,mat,p);base[py,px,:3]=color;height[py,px]=hh;mask[py,px,0]=pr['metallic'];mask[py,px,1]=ao;mask[py,px,3]=1-pr['roughness'];occupied[py,px]=True
 for fi in c['faces']:
  pts=[m.uv_layers.active.data[l].uv for l in m.polygons[fi].loop_indices]
  svg.append('<polygon points="'+' '.join(f'{p.x*N:.1f},{(1-p.y)*N:.1f}' for p in pts)+'" fill="none" stroke="#8aa8b0" stroke-width=".6"/>')
svg.append('</svg>');(TEX/'Hero_UV_Layout.svg').write_text('\n'.join(svg))
# Eight pixels of color dilation around every chart for filtered mip sampling.
filled=occupied.copy()
for _ in range(7):
 old=filled.copy()
 for dy,dx in [(0,1),(0,-1),(1,0),(-1,0)]:
  available=np.roll(old,(dy,dx),(0,1))&~filled
  for array in [base,mask,height]:array[available]=np.roll(array,(dy,dx),(0,1))[available]
  filled|=available
gy,gx=np.gradient(height);normal=np.empty_like(base);normal[:,:,0]=-.20*gx;normal[:,:,1]=-.20*gy;normal[:,:,2]=1
normal[:,:,:3]/=np.linalg.norm(normal[:,:,:3],axis=2)[:,:,None];normal[:,:,:3]=normal[:,:,:3]*.5+.5;normal[:,:,3]=1
images={}
for label,arr in [('BaseColor',base),('Normal',normal),('Mask',mask)]:
 # Generated byte-image pixels are stored in the assigned color space. Encode
 # linear material colors explicitly, then reload the written production image.
 stored=arr.copy()
 if label=='BaseColor':stored[:,:,:3]=np.where(arr[:,:,:3]<=.0031308,arr[:,:,:3]*12.92,1.055*np.maximum(arr[:,:,:3],0)**(1/2.4)-.055)
 path=TEX/('Hero_'+label+'.png');im=bpy.data.images.new('Authored_'+label,width=N,height=N,alpha=True);im.colorspace_settings.name='sRGB' if label=='BaseColor' else 'Non-Color';im.pixels.foreach_set(stored.reshape(-1));im.filepath_raw=str(path);im.file_format='PNG';im.save();im=bpy.data.images.load(str(path),check_existing=False);im.colorspace_settings.name='sRGB' if label=='BaseColor' else 'Non-Color';images[label]=im
for mat in {m for o in parts for m in o.data.materials}:
 pr=profile[mat.name];nodes=mat.node_tree.nodes;links=mat.node_tree.links
 # Rebuild the material cleanly, keeping its original name and base parameters.
 nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');bs=nodes.new('ShaderNodeBsdfPrincipled');links.new(bs.outputs['BSDF'],out.inputs[0]);bs.inputs['Base Color'].default_value=tuple(pr['base_color']);bs.inputs['Roughness'].default_value=pr['roughness'];bs.inputs['Metallic'].default_value=pr['metallic']
 ts={}
 for label,im in images.items():
  t=nodes.new('ShaderNodeTexImage');t.name='Baked_'+label;t.image=im;t.location=(-650,300-len(ts)*300);ts[label]=t
 links.new(ts['BaseColor'].outputs['Color'],bs.inputs['Base Color'])
 if pr['surface']!='plain':
  norm=nodes.new('ShaderNodeNormalMap');norm.inputs['Strength'].default_value=.7;links.new(ts['Normal'].outputs['Color'],norm.inputs['Color']);links.new(norm.outputs['Normal'],bs.inputs['Normal'])
 if mat.name=='Hero_AnomalyAccent':bs.inputs['Emission Color'].default_value=(.30,.035,.65,1);bs.inputs['Emission Strength'].default_value=.45
assert invariant()==before,'Model/weights/rig/motion changed'
rig.data.pose_position='POSE';rig.animation_data.action=bpy.data.actions['Idle'];sc.frame_set(1)
sc.render.engine='BLENDER_EEVEE_NEXT';sc.eevee.taa_render_samples=48;sc.render.resolution_x=850;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
for o in parts:o.hide_render=o.name=='Hero_SwordProxy'
for name,loc in [('Authored_Front',(2,-6,2.2)),('Authored_Back',(2,6,2.2))]:
 sc.camera.location=loc;sc.camera.rotation_euler=(Vector((0,0,.91))-sc.camera.location).to_track_quat('-Z','Y').to_euler();sc.camera.data.ortho_scale=2.15;sc.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(source))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BlenderSource~/DarkSurvivor_Textured.blend'))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in parts:o.hide_set(False);o.select_set(True)
if bpy.data.objects.get('Hero_Nose'):
 bpy.data.objects['Hero_Head'].hide_set(False);bpy.data.objects['Hero_Head'].select_set(True)
bpy.context.view_layer.objects.active=rig
for path in [BASE/'DarkSurvivor.fbx',OUT/'DarkSurvivor_Textured.fbx']:
 bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
report={'geometry_weights_rig_animation_preserved':True,'invariant':before,'resolution':N,'charts':len(charts),'regions':regions,'coverage':float(occupied.mean()),'layout':[{'part':c['obj'].name,'region':c['region'],'rect_pixels':c['rect'],'faces':len(c['faces'])} for c in charts]}
(OUT/'AuthoredUVCheck.json').write_text(json.dumps(report,indent=2));print('AUTHORED_UV_READY',flush=True)
