"""Editable soft bedding geometry for the hideout. Metres, Blender Z-up.
The wood frame, facility origin and collider stay unchanged.
"""
import bpy,math

def refine_bedding(col,root,mats):
    prefixes=('Padded_Mattress','Soft_Pillow','Olive_Quilt','Quilt_Edge_Seam',
              'Mended_Patch','Repair_Stitch','Quilt_Fold','Quilt_Hem')
    for o in list(col.objects):
        if o.type=='MESH' and o.name.startswith(prefixes):bpy.data.objects.remove(o,do_unlink=True)

    def finish(o,name,mat):
        o.name=name
        for c in list(o.users_collection):c.objects.unlink(o)
        col.objects.link(o);o.parent=root;o.data.materials.append(mats[mat])
        for f in o.data.polygons:f.use_smooth=True
        return o

    def surface(name,vs,fs,mat):
        mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],fs);mesh.update()
        o=bpy.data.objects.new(name,mesh);col.objects.link(o);o.parent=root;o.data.materials.append(mats[mat])
        for f in mesh.polygons:f.use_smooth=True
        return o

    bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,.445))
    o=bpy.context.object;o.dimensions=(1.17,1.91,.21)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    b=o.modifiers.new('Rounded upholstered edge','BEVEL');b.width=.085;b.segments=5
    bpy.ops.object.modifier_apply(modifier=b.name)
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    finish(o,'Padded_Mattress','Linen')

    # Superellipse cross-section: soft rectangular pillow, not an eight-corner slab.
    def sp(v,p):return math.copysign(abs(v)**p,v)
    vs=[];fs=[];lon=48;lat=24
    for j in range(lat+1):
        phi=-math.pi/2+math.pi*j/lat
        for i in range(lon):
            theta=2*math.pi*i/lon
            x=.435*sp(math.cos(phi),.65)*sp(math.cos(theta),.48)
            y=.225*sp(math.cos(phi),.65)*sp(math.sin(theta),.48)
            z=.645+.098*sp(math.sin(phi),.9)
            z+=.007*math.sin(x*17+y*9)*max(0,math.sin(phi))
            vs.append((x,y+.66,z))
    for j in range(lat):
        for i in range(lon):
            a=j*lon+i;b=j*lon+(i+1)%lon
            fs.append((a,b,b+lon,a+lon))
    surface('Soft_Pillow',vs,fs,'Linen')

    def smooth(v):v=max(0,min(1,v));return v*v*(3-2*v)
    def height(x,y):
        side=smooth((abs(x)-.52)/.092)
        foot=smooth((-.83-y)/.11)
        z=.589-.105*side-.050*foot
        z+=.025*math.sin(17*y+5*x)*math.exp(-((abs(x)-.49)/.17)**2)
        z+=.027*math.exp(-((y+.10+x*.32)/.11)**2)*math.exp(-((x-.18)/.40)**2)
        z-=.011*math.exp(-((y+.21+x*.32)/.08)**2)*math.exp(-((x-.18)/.40)**2)
        # Keep the underside above the rounded mattress, including low fold troughs.
        if abs(x)<.585 and abs(y)<.955:
            dx=max(0,abs(x)-.5);dy=max(0,abs(y)-.87)
            mattress=.465+math.sqrt(max(0,.085**2-dx*dx-dy*dy))
            z=max(z,mattress+.018)
        return z

    nx=48;ny=60;vs=[];fs=[]
    for j in range(ny+1):
        y=-.945+j*1.35/ny
        for i in range(nx+1):
            x=-.615+i*1.23/nx
            vs.append((x,y,height(x,y)))
    for j in range(ny):
        for i in range(nx):
            a=j*(nx+1)+i;fs.append((a,a+1,a+nx+2,a+nx+1))
    quilt=surface('Olive_Quilt',vs,fs,'Blanket')
    bpy.ops.object.select_all(action='DESELECT');quilt.select_set(True);bpy.context.view_layer.objects.active=quilt
    mod=quilt.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.012
    bpy.ops.object.modifier_apply(modifier=mod.name)

    # Turned-down head edge is geometry; the normal texture only describes fine weave.
    vs=[];fs=[]
    for j in range(7):
        y=.25+j*.15/6
        for i in range(41):
            x=-.59+i*1.18/40
            vs.append((x,y,height(x,y)+.014+.026*math.sin(j*math.pi/6)))
    for j in range(6):
        for i in range(40):
            a=j*41+i;fs.append((a,a+1,a+42,a+41))
    surface('Quilt_Fold',vs,fs,'Blanket')

    def seam(name,points,mat,radius):
        curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=radius;curve.bevel_resolution=1
        spline=curve.splines.new('POLY');spline.points.add(len(points)-1)
        for p,v in zip(spline.points,points):p.co=(*v,1)
        o=bpy.data.objects.new(name,curve);col.objects.link(o)
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
        bpy.ops.object.convert(target='MESH');finish(bpy.context.object,name,mat)

    for x in [-.589,.589]:
        seam('Quilt_Hem',[(x,y,height(x,y)+.008) for y in [-.92+i*1.14/128 for i in range(129)]],'Blanket',.0018)
    vs=[];fs=[]
    for j in range(5):
        for i in range(5):
            u=-.09+i*.18/4;v=-.10+j*.20/4
            x=.29+u*math.cos(.17)-v*math.sin(.17);y=-.53+u*math.sin(.17)+v*math.cos(.17)
            vs.append((x,y,height(x,y)+.006))
    for j in range(4):
        for i in range(4):
            a=j*5+i;fs.append((a,a+1,a+6,a+5))
    surface('Mended_Patch',vs,fs,'OliveDark')
    for j in range(6):
        for xx in [-.10,.10]:
            yy=-.085+j*.034
            x=.29+xx*math.cos(.17)-yy*math.sin(.17);y=-.53+xx*math.sin(.17)+yy*math.cos(.17)
            seam('Repair_Stitch',[(x-.013,y,height(x-.013,y)+.010),(x+.013,y+.004,height(x+.013,y+.004)+.010)],'Canvas',.0018)
