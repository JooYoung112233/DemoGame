"""Soft stylized three-head survivor. Appearance model, independent equipment slots.
Source surfaces are authored at useful resolution, with no low-poly budget imposed.
"""
import bpy, bmesh, math, sys
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[1]
REFERENCE='--reference' in sys.argv
ASSET_NAME='ReferenceMatchSurvivor' if REFERENCE else 'StylizedSurvivor'
OUT=ROOT/'Assets/ChibiSurvivor'/ASSET_NAME
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/ChibiSurvivor/ThreeHeadSurvivor/ThreeHeadSurvivor.blend'))
# Keep only the detailed dial/case, replacing its block-shaped strap below.
watch=bpy.data.objects['ThreeHead_Watch']
for obj in list(bpy.data.objects):
    if obj!=watch:bpy.data.objects.remove(obj,do_unlink=True)
bm=bmesh.new(); bm.from_mesh(watch.data)
remove=[f for f in bm.faces if watch.data.materials[f.material_index].name=='ThreeHead_Leather']
bmesh.ops.delete(bm,geom=remove,context='FACES')
loose=[v for v in bm.verts if not v.link_faces]
if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
bm.to_mesh(watch.data); bm.free(); watch['part']='Watch'

COLORS={'Skin':(.58,.345,.19),'InnerEar':(.41,.203,.099),'Nose':(.40,.185,.083),
 'Cap':(.16,.135,.080),'CapSeam':(.13,.108,.064),'Bill':(.061,.054,.041),
 'Hair':(.033,.022,.014),'Eyebrow':(.032,.023,.015),'Sclera':(.55,.427,.279),
 'Iris':(.059,.039,.022),'Pupil':(.027,.019,.012),'Lid':(.24,.123,.06),
 'Patch':(.244,.177,.115),'Shirt':(.19,.161,.104),'ShirtEdge':(.158,.13,.08),
 'Seam':(.113,.09,.051),'Under':(.43,.36,.24),'Trousers':(.040,.042,.037),
 'Cuff':(.079,.074,.059),'Leather':(.049,.032,.019),'Boot':(.087,.052,.028),
 'Sole':(.025,.024,.020),'Bag':(.073,.067,.048),'Flap':(.093,.084,.062),
 'Metal':(.21,.188,.146),'Repair':(.125,.109,.079)}
if REFERENCE:
    COLORS.update(Skin=(.53,.339,.20),Cap=(.127,.117,.081),CapSeam=(.084,.075,.048),
        Bill=(.048,.045,.035),Patch=(.20,.149,.099),Shirt=(.178,.154,.105),
        ShirtEdge=(.149,.13,.084),Trousers=(.036,.038,.034),Cuff=(.066,.063,.050),
        Hair=(.038,.028,.016),Repair=(.137,.118,.085))
M={}
for name,c in COLORS.items():
    m=bpy.data.materials.new('Stylized_'+name); m.diffuse_color=(*c,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*c,1)
    bs.inputs['Roughness'].default_value=.91 if REFERENCE else .82; bs.inputs['Specular IOR Level'].default_value=.20 if REFERENCE else .25
    if name=='Skin':bs.inputs['Subsurface Weight'].default_value=.012 if REFERENCE else .035
    if name=='Metal':bs.inputs['Metallic'].default_value=.35
    M[name]=m

def active(obj):
    bpy.ops.object.select_all(action='DESELECT'); obj.select_set(True); bpy.context.view_layer.objects.active=obj
def apply(obj,modifier):
    active(obj); bpy.ops.object.modifier_apply(modifier=modifier.name)
def smooth(obj):
    for p in obj.data.polygons:p.use_smooth=True
    return obj
def finish(obj,mat,part):
    obj.data.materials.append(M[mat]); obj['part']=part; return obj
def planes(obj,amount=.45):
    """Blend broad face normals with vertex normals instead of all-flat/all-smooth."""
    smooth(obj); obj.data.update()
    normals=[]
    for poly in obj.data.polygons:
        for li in poly.loop_indices:
            vertex=obj.data.vertices[obj.data.loops[li].vertex_index]
            normals.append((poly.normal*amount+vertex.normal*(1-amount)).normalized())
    obj.data.normals_split_custom_set(normals)
    return obj

def wear_regions(obj,mat):
    """A few broad fabric color regions; no noise maps or per-face random speckles."""
    base=M[mat]; color=Vector(COLORS[mat])
    for factor in [.89,.95,1.035,1.065]:
        m=base.copy(); m.name=base.name+'_Worn'+str(factor)
        rgb=tuple(color*factor); m.diffuse_color=(*rgb,1)
        m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*rgb,1)
        obj.data.materials.append(m)
    for p in obj.data.polygons:
        x,y,z=p.center
        field=math.sin(14*x+6*z)*math.cos(9*z-3*y)+.38*math.cos(18*y+4*z)
        p.material_index=1 if field<-.83 else 2 if field<-.30 else 4 if field>.95 else 3 if field>.5 else 0

def cloth_folds(obj,kind):
    for v in obj.data.vertices:
        x,y,z=v.co
        if kind=='Shirt':
            torso=max(0,min(1,(.205-abs(x))/.038))
            ridge=.009*math.exp(-((z-(.818+.37*abs(x)))/.014)**2)
            ridge-=.004*math.exp(-((z-(.838+.37*abs(x)))/.010)**2)
            ridge+=.007*math.exp(-((z-(.913-.40*abs(x)))/.014)**2)
            sleeve=.008*math.exp(-((z-(.983+.32*(abs(x)-.24)))/.014)**2)
            sleeve-=.004*math.exp(-((z-1.007)/.012)**2)
            displacement=torso*ridge+(1-torso)*sleeve
        elif kind=='Trousers':
            localx=x-math.copysign(.092,x)
            displacement=.010*math.exp(-((z-(.43+.7*localx))/.014)**2)
            displacement-=.005*math.exp(-((z-(.452+.7*localx))/.013)**2)
            displacement+=.008*math.exp(-((z-(.301-.45*localx))/.012)**2)
            displacement+=.008*math.exp(-((z-(.658-.7*abs(x)))/.013)**2)
        else:displacement=0
        v.co.y+=math.copysign(displacement,y)
    obj.data.update()
def mesh(name,vs,fs,mat,part,subd=0):
    d=bpy.data.meshes.new(name); d.from_pydata(vs,[],fs); d.update()
    bm=bmesh.new(); bm.from_mesh(d); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(d); bm.free()
    o=bpy.data.objects.new(name,d); bpy.context.collection.objects.link(o); finish(o,mat,part)
    if subd:
        mod=o.modifiers.new('SurfaceResolution','SUBSURF'); mod.levels=subd; apply(o,mod)
    return smooth(o)
def loft(name,rows,mat,part,n=32,p=.85,subd=1):
    if REFERENCE:
        if name=='Face':n,subd=32,0
        elif name=='CapCrown':n,subd=24,0
        elif name.startswith('Rolled'):n,subd=18,0
        elif name=='Sole':n,subd=24,0
    vs=[]
    for z,rx,ry,cy,cx in rows:
        for i in range(n):
            a=math.tau*i/n; x,y=math.sin(a),-math.cos(a)
            vs.append((cx+rx*math.copysign(abs(x)**p,x),cy+ry*math.copysign(abs(y)**p,y),z))
    fs=[tuple(reversed(range(n)))]+[(k*n+i,k*n+(i+1)%n,(k+1)*n+(i+1)%n,(k+1)*n+i)
        for k in range(len(rows)-1) for i in range(n)]+[tuple(range((len(rows)-1)*n,len(rows)*n))]
    o=mesh(name,vs,fs,mat,part,subd)
    if REFERENCE and name in ['Face','CapCrown','RolledSleeve','RolledTrouserCuff','Sole']:
        planes(o,.16 if name=='Face' else .52)
        if name=='CapCrown':wear_regions(o,mat)
    return o
def box(name,pos,size,mat,part,bevel=.009):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos); o=bpy.context.object; o.name=name; o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    b=o.modifiers.new('SoftEdges','BEVEL'); b.width=bevel*(.65 if REFERENCE else 1); b.segments=2 if REFERENCE else 5; apply(o,b)
    smooth(o); mod=o.modifiers.new('FaceNormals','WEIGHTED_NORMAL'); mod.keep_sharp=True; apply(o,mod)
    return finish(o,mat,part)
def oval(name,pos,size,mat,part,n=24,r=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=n,ring_count=r,radius=1,location=pos)
    o=bpy.context.object; o.name=name; o.scale=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return smooth(finish(o,mat,part))
def path(name,points,radius,mat,part):
    data=bpy.data.curves.new(name,'CURVE'); data.dimensions='3D'; data.resolution_u=10
    spline=data.splines.new('BEZIER'); spline.bezier_points.add(len(points)-1)
    for bp,p in zip(spline.bezier_points,points):
        bp.co=p; bp.handle_left_type='AUTO'; bp.handle_right_type='AUTO'
    data.bevel_depth=radius; data.bevel_resolution=3; data.use_fill_caps=True
    o=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(o); active(o)
    bpy.ops.object.convert(target='MESH'); o=bpy.context.object; finish(o,mat,part); return smooth(o)
def fuse(objects,name,mat,part,voxel=.006):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]; bpy.ops.object.join(); o=bpy.context.object
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    rem=o.modifiers.new('ContinuousVolume','REMESH'); rem.mode='VOXEL'; rem.voxel_size=voxel; rem.use_smooth_shade=True; apply(o,rem)
    sm=o.modifiers.new('RelaxSurface','SMOOTH'); sm.factor=.65; sm.iterations=2 if REFERENCE else 4; apply(o,sm)
    o.data.materials.clear(); o.data.materials.append(M[mat]); o.name=name; o['part']=part
    if REFERENCE:
        if mat in ['Shirt','Trousers']:cloth_folds(o,mat)
        targets={'Shirt':1150,'Trousers':1500,'Boot':450,'Skin':900}
        dec=o.modifiers.new('DesignedBroadPlanes','DECIMATE')
        dec.ratio=min(1,targets.get(mat,1000)/len(o.data.polygons)); apply(o,dec)
        planes(o,.19 if mat=='Skin' else .40)
        if mat in ['Shirt','Trousers','Boot']:wear_regions(o,mat)
        return o
    return smooth(o)
def front(target,x,z,offset=.001):
    hit,point,normal,_=target.ray_cast(Vector((x,-2,z)),Vector((0,1,0)))
    if not hit:raise RuntimeError('Surface projection missed '+target.name+str((x,z)))
    return point+Vector((0,-offset,0))
def patch(name,outline,target,mat,part,offset=.002,cuts=7):
    x=sum(p[0] for p in outline)/len(outline); z=sum(p[1] for p in outline)/len(outline)
    o=mesh(name,[(x,-1,z)]+[(x,-1,z) for x,z in outline],
        [(0,i+1,(i+1)%len(outline)+1) for i in range(len(outline))],mat,part)
    bm=bmesh.new(); bm.from_mesh(o.data); bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=cuts,use_grid_fill=True)
    bm.to_mesh(o.data); bm.free()
    for v in o.data.vertices:v.co=front(target,v.co.x,v.co.z,offset)
    o.data.update()
    bm=bmesh.new(); bm.from_mesh(o.data); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(o.data); bm.free()
    for p in o.data.polygons:
        if p.normal.y>0:p.flip()
    return smooth(o)
def sheet(name,outline,mat,part,thickness=.002):
    o=mesh(name,outline,[tuple(range(len(outline)))],mat,part)
    sol=o.modifiers.new('ClothThickness','SOLIDIFY'); sol.thickness=thickness; apply(o,sol)
    b=o.modifiers.new('FoldEdge','BEVEL'); b.width=.002; b.segments=3; apply(o,b); return smooth(o)

# A newly shaped rounded face: no stretched cube, and no separate eyeballs.
face=loft('Face',[(1.196,.040,.042,0,0),(1.205,.095,.089,0,0),(1.222,.164,.140,.003,0),
    (1.252,.215,.177,.008,0),(1.299,.249,.198,.012,0),(1.347,.260,.207,.018,0),
    (1.403,.263,.211,.023,0),(1.468,.253,.211,.026,0),(1.526,.241,.201,.030,0),
    (1.594,.21,.18,.035,0),(1.649,.13,.12,.035,0),(1.663,.035,.035,.035,0)],'Skin','Head',n=40,p=.94,subd=2)
bpy.context.view_layer.update()
for s in [-1,1]:
    x=s*.109
    patch('EyeWhite',[(x+math.sin(i*math.tau/48)*.044,min(1.391,1.362+math.cos(i*math.tau/48)*.050)) for i in range(48)],face,'Sclera','Face',.001)
    patch('Iris',[(x+math.sin(i*math.tau/48)*.033,min(1.391,1.364+math.cos(i*math.tau/48)*.047)) for i in range(48)],face,'Iris','Face',.002)
    patch('Pupil',[(x+math.sin(i*math.tau/24)*.017,1.363+math.cos(i*math.tau/24)*.026) for i in range(24)],face,'Pupil','Face',.0027)
    eyelid=[front(face,x+dx,z,.003) for dx,z in [(-.046,1.390),(-.022,1.397),(.017,1.397),(.045,1.389)]]
    path('UpperLid',eyelid,.0033,'Lid','Face')
    path('UpperLash',[front(face,x+dx,z,.004) for dx,z in [(-.045,1.388),(-.014,1.392),(.018,1.391),(.044,1.387)]],.0017,'Eyebrow','Face')
    patch('Eyebrow',[(x-.049,1.428),(x-.044,1.440),(x+.045,1.436),(x+.050,1.425),(x+.02,1.428)],face,'Eyebrow','Face',.002)
    oval('Ear',(s*.262,-.002,1.35),(.055,.037,.064),'Skin','Head')
    oval('EarInset',(s*.269,-.034,1.352),(.030,.006,.039),'InnerEar','Head')
    path('EarFold',[(s*.248,-.037,1.365),(s*.261,-.041,1.378),(s*.282,-.035,1.37)],.005,'Skin','Head')
nose=mesh('Nose',[(0,-.227,1.334),(-.019,-.192,1.325),(0,-.208,1.357),(.019,-.192,1.325),(0,-.209,1.315)],
    [(0,1,2),(0,2,3),(0,3,4),(0,4,1),(1,4,3,2)],'Nose','Face')
be=nose.modifiers.new('SoftNoseTip','BEVEL'); be.width=.004; be.segments=3; apply(nose,be)
path('Mouth',[front(face,x,z,.0017) for x,z in [(-.032,1.256),(0,1.261),(.032,1.255)]],.0014,'Lid','Face')
patch('CheekBandage',[(.165,1.261),(.209,1.275),(.207,1.3),(.161,1.286)],face,'Patch','Face',.002)

# Continuous hair surface fitted to the actual head, with shaped side boundaries.
vs=[]; N=65; R=14
for row in range(R):
    t=row/(R-1)
    for col in range(N):
        u=col/(N-1); a=math.pi*.29+math.pi*1.42*u
        bottom=1.235+.106*abs(2*u-1)**1.6
        z=bottom+(1.565-bottom)*t
        d=Vector((math.sin(a),-math.cos(a),0))
        hit,p,_,_=face.ray_cast(d*2+Vector((0,0,z)),-d)
        if not hit:raise RuntimeError('Hair projection missed')
        vs.append(p+d*.0035)
mesh('HairShell',vs,[(j*N+i,j*N+i+1,(j+1)*N+i+1,(j+1)*N+i) for j in range(R-1) for i in range(N-1)],'Hair','Hair')

# Baseball cap: rounded panels, curved continuous bill and fine panel seams.
cap=loft('CapCrown',[(1.473,.270,.222,.027,0),(1.484,.28,.229,.027,0),(1.555,.283,.238,.03,0),
    (1.635,.263,.228,.038,0),(1.703,.217,.194,.044,0),(1.754,.14,.131,.047,0),
    (1.782,.069,.066,.048,0),(1.789,.013,.013,.048,0)],'Cap','Cap',n=48,p=1,subd=2)
bpy.context.view_layer.update()
vs=[]; N=35; R=5
for r in range(R):
    u=r/(R-1)
    for i in range(N):
        t=-1+2*i/(N-1)
        vs.append((t*(.251+.006*math.sin(u*math.pi)),
            (-.198+.077*t*t)*(1-u)+(-.392+.148*t*t)*u,
            1.532-.068*t*t-.039*u+.005*math.sin(u*math.pi)))
bill=mesh('CurvedBill',vs,[(r*N+i,r*N+i+1,(r+1)*N+i+1,(r+1)*N+i) for r in range(R-1) for i in range(N-1)],'Bill','Cap',subd=2)
so=bill.modifiers.new('BillThickness','SOLIDIFY'); so.thickness=.008; apply(bill,so)
be=bill.modifiers.new('BillRoundedEdge','BEVEL'); be.width=.002; be.segments=3; apply(bill,be)
patch('CapLabel',[(-.078,1.574),(.078,1.574),(.075,1.673),(-.075,1.673)],cap,'Patch','Cap',.003)
oval('CapButton',(0,.048,1.792),(.021,.020,.007),'CapSeam','Cap')
for s in [-1,1]:
    pts=[front(cap,s*x,z,.0012) for x,z in [(.095,1.69),(.14,1.65),(.179,1.584),(.196,1.52)]]
    path('PanelSeam',pts,.001,'CapSeam','Cap')

# Shirt body and sleeves are fused before adding folds/collar/pockets.
upper=[loft('Torso',[(.743,.162,.101,0,0),(.763,.179,.113,0,0),(.80,.18,.113,0,0),(.88,.17,.111,0,0),
    (1.004,.179,.119,0,0),(1.098,.184,.114,0,0),(1.15,.144,.09,0,0),(1.182,.066,.055,0,0)],'Shirt','Shirt',n=32,subd=1)]
for s in [-1,1]:
    upper.append(loft('UpperSleeve',[(.913,.060,.064,-.003,s*.258),(.946,.065,.07,-.002,s*.253),
        (1.023,.070,.077,0,s*.224),(1.09,.077,.079,0,s*.192),(1.136,.060,.07,0,s*.164),(1.151,.034,.048,0,s*.145)],'Shirt','Shirt',n=28,subd=1))
shirt=fuse(upper,'ContinuousShirt','Shirt','Shirt',.006)
bpy.context.view_layer.update()
oval('Neck',(0,0,1.188),(.065,.060,.039),'Skin','Body')
patch('UnderShirt',[(-.041,1.163),(0,1.093),(.041,1.163)],shirt,'Under','Shirt',.002)
for s in [-1,1]:
    sheet('Collar',[(s*.017,-.061,1.183),(s*.063,-.069,1.177),(s*.093,-.108,1.126),
        (s*.059,-.121,1.135),(s*.032,-.127,1.111)],'Shirt','Shirt',.002)
    x=s*.077
    patch('ClothPocket',[(x-.036,.974),(x+.031,.972),(x+.038,.978),(x+.038,1.055),(x-.036,1.055)],shirt,'ShirtEdge','Shirt',.0028)
    patch('PocketFlap',[(x-.039,1.050),(x,1.042),(x+.040,1.050),(x+.04,1.066),(x-.039,1.066)],shirt,'Shirt','Shirt',.004)
    path('PocketSeam',[front(shirt,x+dx,z,.0042) for dx,z in [(-.034,1.040),(-.033,.98),(0,.977),(.033,.98),(.034,1.04)]],.00065,'Seam','Shirt')
    oval('PocketButton',front(shirt,x,1.054,.006),(.0045,.002,.0045),'Metal','Shirt',16,8)
    loft('RolledSleeve',[(.901,.061,.067,-.003,s*.259),(.909,.069,.075,-.003,s*.259),(.938,.069,.075,-.003,s*.259),(.946,.062,.068,-.003,s*.259)],'ShirtEdge','Shirt',n=28,p=.9,subd=1)
    loft('Forearm',[(.743,.033,.034,-.017,s*.286),(.776,.039,.04,-.016,s*.283),(.864,.048,.045,-.008,s*.267),(.934,.049,.046,-.004,s*.259)],'Skin','Body',n=28,p=1,subd=1)
    # Joined palm, thumb and curved fingers, preserving a relaxed natural hand.
    hand=[oval('Palm',(s*.291,-.017,.721),(.045,.030,.055),'Skin','Hands')]
    for j in range(4):
        fx=s*(.258+j*.021)
        length=[.055,.065,.060,.045][j]
        hand.append(path('Finger',[(fx,-.02,.71),(fx,-.029,.696),(fx,-.036,.71-length)],.0115,'Skin','Hands'))
    hand.append(path('Thumb',[(s*.265,-.019,.743),(s*.247,-.035,.725),(s*.245,-.045,.703)],.016,'Skin','Hands'))
    fuse(hand,'Hand.'+str(s),'Skin','Hands',.003)
path('ShirtPlacket',[front(shirt,0,z,.002) for z in [.766,.85,.96,1.065]],.002,'ShirtEdge','Shirt')
for z in [.805,.914,1.025]:oval('Button',front(shirt,0,z,.004),(.0055,.0025,.0055),'Metal','Shirt',16,8)

# Soft trouser volumes with a bent knee profile, continuous crotch and shaped cuffs.
pants=[loft('Pelvis',[(.651,.144,.093,0,0),(.70,.166,.102,0,0),(.783,.173,.109,0,0)],'Trousers','Trousers',n=28,subd=1)]
for s in [-1,1]:
    x=s*.092
    pants.append(loft('Leg',[(.213,.066,.067,.010,x),(.275,.074,.074,.01,x),(.342,.071,.075,.009,x),
        (.407,.079,.083,-.005,x),(.449,.084,.09,-.01,x),(.484,.079,.083,-.005,x),
        (.556,.083,.085,.009,x),(.67,.087,.10,.004,x),(.745,.084,.099,.004,x)],'Trousers','Trousers',n=28,subd=1))
trousers=fuse(pants,'ContinuousTrousers','Trousers','Trousers',.006)
bpy.context.view_layer.update()
for s in [-1,1]:
    x=s*.092
    loft('RolledTrouserCuff',[(.222,.073,.076,.007,x),(.232,.080,.083,.007,x),(.268,.080,.083,.007,x),(.277,.073,.076,.007,x)],'Cuff','Trousers',n=28,p=.9,subd=1)
    box('CargoPocket',(s*.163,-.013,.564),(.031,.104,.114),'Trousers','Trousers',.011)
    box('CargoFlap',(s*.166,-.014,.615),(.033,.11,.028),'Cuff','Trousers',.008)
    foot=loft('Foot',[(.019,.076,.116,-.039,x),(.036,.082,.126,-.043,x),(.082,.080,.125,-.044,x),
        (.122,.071,.104,-.029,x),(.166,.059,.070,.007,x)],'Boot','Boots',n=32,p=.77,subd=1)
    ankle=loft('BootUpper',[(.09,.064,.07,.004,x),(.17,.063,.069,.005,x),(.235,.066,.071,.004,x)],'Boot','Boots',n=28,p=.9,subd=1)
    boot=fuse([foot,ankle],'Boot.'+str(s),'Boot','Boots',.0045)
    loft('Sole',[(.005,.074,.115,-.042,x),(.009,.082,.126,-.043,x),(.032,.083,.127,-.043,x),(.041,.078,.12,-.043,x)],'Sole','Boots',n=36,p=.75,subd=1)
    bpy.context.view_layer.update()
    for z in [.128,.151,.177]:
        path('BootLace',[front(boot,x+dx,z,.002) for dx in [-.027,0,.027]],.0018,'Leather','Boots')
patch('KneePatch',[(.053,.402),(.124,.407),(.122,.474),(.055,.473)],trousers,'Repair','Trousers',.003)

# Separate padded pack, curved straps and belt accessories.
loft('Belt',[(.755,.178,.114,0,0),(.793,.178,.114,0,0)],'Leather','Belt',n=48,p=.83,subd=0)
box('Buckle',(0,-.118,.774),(.044,.012,.037),'Metal','Belt',.004)
box('BuckleInset',(0,-.126,.774),(.028,.004,.022),'Leather','Belt',.002)
box('BeltPouch',(-.126,-.132,.729),(.09,.065,.111),'Leather','BeltPouch',.018)
box('PouchFlap',(-.126,-.169,.768),(.096,.014,.049),'Boot','BeltPouch',.008)
box('PouchLatch',(-.126,-.18,.742),(.016,.007,.02),'Metal','BeltPouch',.002)
box('PaddedPack',(0,.161,1.005),(.29,.164,.328),'Bag','Backpack',.047)
box('PackFlap',(0,.253,1.081),(.276,.027,.173),'Flap','Backpack',.014)
box('PackPocket',(0,.247,.905),(.232,.049,.094),'Bag','Backpack',.018)
box('PackRepair',(.061,.27,1.077),(.057,.005,.056),'Repair','Backpack',.004)
box('PackFastener',(0,.273,.991),(.028,.010,.11),'Leather','Backpack',.004)
box('PackBuckle',(0,.281,1.02),(.036,.007,.028),'Metal','Backpack',.003)
path('PackHandle',[(-.047,.153,1.167),(-.033,.153,1.195),(.033,.153,1.195),(.047,.153,1.167)],.006,'Leather','Backpack')
for s in [-1,1]:
    box('PackSidePocket',(s*.146,.162,.938),(.042,.119,.101),'Flap','Backpack',.014)
    centers=[front(shirt,s*x,z,.004) for x,z in [(.121,.963),(.132,1.025),(.131,1.086),(.114,1.14)]]
    centers += [Vector((s*.102,-.028,1.168)),Vector((s*.103,.04,1.168)),Vector((s*.109,.107,1.133)),Vector((s*.114,.154,1.08))]
    vs=[]
    for c in centers:vs.extend([c+Vector((-.014,0,0)),c+Vector((.014,0,0))])
    strap=mesh('PaddedStrap',vs,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(centers)-1)],'Leather','Backpack',subd=2)
    mod=strap.modifiers.new('StrapThickness','SOLIDIFY'); mod.thickness=.004; apply(strap,mod)
    box('StrapBuckle',front(shirt,s*.132,1.035,.009),(.032,.008,.023),'Metal','Backpack',.003)
    box('StrapInset',front(shirt,s*.132,1.035,.014),(.02,.003,.012),'Leather','Backpack',.001)
loft('WatchBand',[(.793,.045,.045,-.014,.279),(.819,.045,.045,-.014,.279)],'Leather','Watch',n=40,p=1,subd=0)

# Partitioned assets; export only character, never studio cameras/lights.
parts=sorted({o.get('part') for o in bpy.context.scene.objects if o.type=='MESH'})
for part in parts:
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.get('part')==part]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    if len(objects)>1:bpy.ops.object.join()
    o=bpy.context.object; o.name='Stylized_'+part; o['equipment_slot']=part
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    o['stage']='Appearance mesh, no skeleton or animations'
character=[o for o in bpy.context.scene.objects if o.type=='MESH']
scene=bpy.context.scene; scene.render.engine='CYCLES'; scene.cycles.samples=48; scene.cycles.use_denoising=True
scene.world.use_nodes=True; bg=scene.world.node_tree.nodes['Background']
bg.inputs[0].default_value=(.072,.078,.085,1); bg.inputs[1].default_value=.42
scene.view_settings.view_transform='AgX'; scene.view_settings.look='AgX - Medium High Contrast'; scene.view_settings.exposure=0
def light(pos,power,size):
    bpy.ops.object.light_add(type='AREA',location=pos); o=bpy.context.object
    o.data.energy=power; o.data.shape='DISK'; o.data.size=size
    o.rotation_euler=(Vector((0,0,.95))-o.location).to_track_quat('-Z','Y').to_euler()
light((-3,-4,4.7),440,4); light((3,-4,2.2),160,3); light((1,3,4),250,3)
bpy.ops.object.camera_add(); scene.camera=bpy.context.object; scene.camera.data.type='ORTHO'; scene.camera.data.ortho_scale=2.04
def camera(pos):
    scene.camera.location=pos; scene.camera.rotation_euler=(Vector((0,0,.90))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
camera((0,-6,1.2))
scene.render.resolution_x=960; scene.render.resolution_y=1200; scene.render.resolution_percentage=100
scene['stage']='Soft stylized character appearance approval. No rig/animations/Unity hookup.'
bpy.ops.object.select_all(action='DESELECT')
for o in character:o.select_set(True)
bpy.context.view_layer.objects.active=character[0]
bpy.ops.export_scene.fbx(filepath=str(OUT/'StylizedSurvivor.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
bpy.ops.export_scene.gltf(filepath=str(OUT/'StylizedSurvivor.glb'),use_selection=True,export_format='GLB')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'StylizedSurvivor.blend'))
for name,pos in [('Quarter',(3,-6,1.65)),('Front',(0,-6,1.2)),('Back',(-3,6,1.65)),('Side',(6,-.15,1.2))]:
    camera(pos); scene.render.filepath=str(OUT/(name+'.png')); bpy.ops.render.render(write_still=True)
print('STYLIZED_SURVIVOR_CREATED',str(OUT),'parts',len(character),'vertices',sum(len(o.data.vertices) for o in character))
