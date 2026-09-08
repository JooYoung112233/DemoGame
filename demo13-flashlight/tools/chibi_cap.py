"""Independent, head-weighted cap module. Edit this file to change headwear only."""
import bpy
import math


def build_cap(rig):
    pieces = []

    def mat(name, color):
        result = bpy.data.materials.get(name) or bpy.data.materials.new(name)
        result.diffuse_color = (*color, 1)
        result.use_nodes = True
        shader = result.node_tree.nodes.get('Principled BSDF')
        shader.inputs['Base Color'].default_value = (*color, 1)
        shader.inputs['Roughness'].default_value = .86
        return result

    fabric = mat('Cap_DeepTeal', (.065, .145, .18))
    dark = mat('Cap_Underbrim', (.03, .063, .073))
    cream = mat('Cap_Badge', (.75, .66, .44))
    orange = mat('Cap_BadgeMark', (.69, .20, .069))

    def mesh(name, vertices, faces, material):
        data = bpy.data.meshes.new(name)
        data.from_pydata(vertices, [], faces)
        data.update()
        obj = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(obj)
        obj.data.materials.append(material)
        pieces.append(obj)
        return obj

    # Rounded-square cross sections cover the existing rounded-square head cleanly.
    vertices, faces = [], []
    sides = 16
    for z, rx, ry in [(1.575, .375, .34), (1.68, .375, .34),
                      (1.79, .29, .26), (1.85, .13, .12)]:
        for i in range(sides):
            a = math.tau*i/sides
            x, y = math.cos(a), math.sin(a)
            vertices.append((rx*math.copysign(abs(x)**.65, x),
                             .008+ry*math.copysign(abs(y)**.65, y), z))
    for ring in range(3):
        for i in range(sides):
            j = (i+1)%sides
            faces.append((ring*sides+i, ring*sides+j, (ring+1)*sides+j, (ring+1)*sides+i))
    vertices.append((0, .008, 1.87))
    for i in range(sides):
        faces.append((48+i, 48+(i+1)%sides, 64))
    mesh('Cap_Crown', vertices, faces, fabric)

    # A short, curved bill, with separate dark underside and thickness at the edge.
    outline = [(-.285,-.22),(-.32,-.32),(-.29,-.43),(-.21,-.50),
               (-.10,-.535),(0,-.545),(.10,-.535),(.21,-.50),
               (.29,-.43),(.32,-.32),(.285,-.22)]
    n = len(outline)
    verts = [(x,-.22+(y+.22)*.8,1.645-.028*max(0,(-y-.22)/.325)) for x,y in outline]
    verts += [(x,y,z-.024) for x,y,z in verts[:]]
    faces = [tuple(range(n)), tuple(reversed(range(n,2*n)))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    bill = mesh('Cap_Bill', verts, faces, fabric)
    bill.data.materials.append(dark)
    bill.data.polygons[1].material_index = 1

    def detail(name, position, size, material, bevel):
        bpy.ops.mesh.primitive_cube_add(size=1, location=position)
        obj = bpy.context.object
        obj.name = name
        obj.dimensions = size
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        mod = obj.modifiers.new('SoftCorners', 'BEVEL')
        mod.width, mod.segments = bevel, 2
        bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.data.materials.append(material)
        pieces.append(obj)

    detail('Cap_FrontBadge', (0,-.331,1.697), (.14,.025,.08), cream, .013)
    detail('Cap_BadgeStripe', (0,-.347,1.697), (.080,.009,.018), orange, .003)
    detail('Cap_TopButton', (0,.008,1.872), (.069,.064,.023), fabric, .01)
    detail('Cap_BackStrap', (0,.315,1.612), (.17,.02,.041), dark, .008)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in pieces:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = pieces[0]
    bpy.ops.object.join()
    cap = bpy.context.object
    cap.name = 'ChibiSurvivor_Cap'
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    group = cap.vertex_groups.new(name='Head')
    group.add(list(range(len(cap.data.vertices))), 1, 'REPLACE')
    cap.parent = rig
    modifier = cap.modifiers.new('HeadSkin', 'ARMATURE')
    modifier.object = rig
    cap['part'] = 'headwear'
    return cap
