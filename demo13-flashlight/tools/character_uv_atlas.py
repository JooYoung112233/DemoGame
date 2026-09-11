"""Directional, connected UV charts grouped by editable character regions."""
import bpy,numpy as np,json

def author_atlas(parts,regions,bucket,paint,folder,prefix,size=2048):
 n=size;charts=[];positions={}
 for o in parts:
  m=o.data;vs=np.array([tuple(o.matrix_world@v.co) for v in m.vertices]);positions[o.name]=vs;keys={};edges={}
  for p in m.polygons:
   normal=o.matrix_world.to_3x3()@p.normal;axis=max(range(3),key=lambda k:abs(normal[k]));keys[p.index]=(axis,normal[axis]>=0)
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
   axis,pos=keys[seed];axes=[i for i in range(3) if i!=axis];verts=sorted({v for f in group for v in m.polygons[f].vertices});xy=vs[verts][:,axes];lo=xy.min(0);span=np.maximum(xy.max(0)-lo,.002)
   charts.append(dict(obj=o,faces=group,axes=axes,lo=lo,span=span,region=bucket(o.name)))
  while len(m.uv_layers):m.uv_layers.remove(m.uv_layers[0])
  m.uv_layers.new(name='SurfaceUV')
 def pack(items,w,h,scale):
  boxes=sorted([(max(12,int(c['span'][0]*scale))+16,max(12,int(c['span'][1]*scale))+16,i) for i,c in enumerate(items)],key=lambda a:(-a[1],-a[0],a[2]));x=y=row=0;result={}
  for bw,bh,i in boxes:
   if x+bw>w:x=0;y+=row;row=0
   if y+bh>h:return None
   result[i]=(x+8,y+8,bw-16,bh-16);x+=bw;row=max(row,bh)
  return result
 for region,(rx,ry,rw,rh) in regions.items():
  items=[c for c in charts if c['region']==region];lo=1;hi=12000
  for _ in range(24):
   mid=(lo+hi)/2
   if pack(items,int(rw*n),int(rh*n),mid) is None:hi=mid
   else:lo=mid
  packed=pack(items,int(rw*n),int(rh*n),lo)
  if packed is None:raise RuntimeError('Too many UV charts: '+region)
  for i,c in enumerate(items):
   x,y,w,h=packed[i];c['rect']=(int(rx*n)+x,int(ry*n)+y,w,h)
   for fi in c['faces']:
    for li in c['obj'].data.polygons[fi].loop_indices:
     vi=c['obj'].data.loops[li].vertex_index;xy=positions[c['obj'].name][vi,c['axes']];uv=(xy-c['lo'])/c['span'];c['obj'].data.uv_layers.active.data[li].uv=((c['rect'][0]+uv[0]*w)/n,(c['rect'][1]+uv[1]*h)/n)
 base=np.zeros((n,n,4),np.float32);base[:]=(.035,.03,.023,1);mask=np.zeros_like(base);mask[:]=(0,1,0,.12);height=np.zeros((n,n),np.float32);occupied=np.zeros((n,n),bool);overlap=0
 svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{n}" height="{n}" viewBox="0 0 {n} {n}"><rect width="100%" height="100%" fill="#24272a"/>']
 for region,(x,y,w,h) in regions.items():svg.append(f'<rect x="{x*n}" y="{(1-y-h)*n}" width="{w*n}" height="{h*n}" fill="none" stroke="#bdac85"/><text x="{x*n+5}" y="{(1-y-h)*n+20}" font-size="20" fill="white">{region}</text>')
 for c in charts:
  o=c['obj'];m=o.data;m.calc_loop_triangles();faceids=set(c['faces'])
  for tri in m.loop_triangles:
   if tri.polygon_index not in faceids:continue
   uv=np.array([tuple(m.uv_layers.active.data[l].uv) for l in tri.loops])*n;a,b,d=uv;v0=b-a;v1=d-a;den=v0[0]*v1[1]-v1[0]*v0[1]
   if abs(den)<1e-7:continue
   lo=np.maximum(np.floor(uv.min(0)).astype(int),0);hi=np.minimum(np.ceil(uv.max(0)).astype(int),n-1);yy,xx=np.mgrid[lo[1]:hi[1]+1,lo[0]:hi[0]+1];dx=xx+.5-a[0];dy=yy+.5-a[1];u=(dx*v1[1]-v1[0]*dy)/den;v=(v0[0]*dy-dx*v0[1])/den;ok=(u>=0)&(v>=0)&(u+v<1)
   px=xx[ok];py=yy[ok];u=u[ok];v=v[ok]
   if not len(px):continue
   xyz=positions[o.name][list(tri.vertices)];p=xyz[0]+u[:,None]*(xyz[1]-xyz[0])+v[:,None]*(xyz[2]-xyz[0]);material=m.materials[tri.material_index]
   color,hh,metal,rough=paint(o.name,material,p);overlap+=int(occupied[py,px].sum());base[py,px,:3]=color;height[py,px]=hh;mask[py,px,0]=metal;mask[py,px,3]=1-rough;occupied[py,px]=True
  for fi in c['faces']:
   pts=[m.uv_layers.active.data[l].uv for l in m.polygons[fi].loop_indices];svg.append('<polygon points="'+' '.join(f'{p.x*n:.1f},{(1-p.y)*n:.1f}' for p in pts)+'" fill="none" stroke="#8aa8b0" stroke-width=".6"/>')
 svg.append('</svg>');folder.mkdir(parents=True,exist_ok=True);(folder/(prefix+'_UV_Layout.svg')).write_text('\n'.join(svg))
 filled=occupied.copy()
 for _ in range(7):
  old=filled.copy()
  for dy,dx in [(0,1),(0,-1),(1,0),(-1,0)]:
   available=np.roll(old,(dy,dx),(0,1))&~filled
   for array in [base,mask,height]:array[available]=np.roll(array,(dy,dx),(0,1))[available]
   filled|=available
 gy,gx=np.gradient(height);normal=np.empty_like(base);normal[:,:,0]=-.2*gx;normal[:,:,1]=-.2*gy;normal[:,:,2]=1;normal[:,:,:3]/=np.linalg.norm(normal[:,:,:3],axis=2)[:,:,None];normal[:,:,:3]=normal[:,:,:3]*.5+.5;normal[:,:,3]=1;images={}
 for label,arr in [('BaseColor',base),('Normal',normal),('Mask',mask)]:
  stored=arr.copy()
  if label=='BaseColor':stored[:,:,:3]=np.where(arr[:,:,:3]<=.0031308,arr[:,:,:3]*12.92,1.055*np.maximum(arr[:,:,:3],0)**(1/2.4)-.055)
  path=folder/(prefix+'_'+label+'.png');im=bpy.data.images.new(prefix+'_'+label,width=n,height=n,alpha=True);im.colorspace_settings.name='sRGB' if label=='BaseColor' else 'Non-Color';im.pixels.foreach_set(stored.reshape(-1));im.filepath_raw=str(path);im.file_format='PNG';im.save();im=bpy.data.images.load(str(path),check_existing=False);im.colorspace_settings.name='sRGB' if label=='BaseColor' else 'Non-Color';images[label]=im
 report={'resolution':n,'charts':len(charts),'coverage':float(occupied.mean()),'overlapping_pixels':overlap,'regions':regions,'layout':[{'part':c['obj'].name,'region':c['region'],'rect_pixels':c['rect']} for c in charts]}
 (folder/(prefix+'_UV_Check.json')).write_text(json.dumps(report,indent=2));return images,report
