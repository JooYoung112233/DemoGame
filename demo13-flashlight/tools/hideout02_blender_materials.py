"""Match the editable Blender source to the Unity PBR bindings."""
from pathlib import Path
import bpy

def bind_surfaces(out, manifest):
    for name,spec in manifest['palette'].items():
        mat=bpy.data.materials.get(name)
        if mat is None:continue
        nodes=mat.node_tree.nodes;links=mat.node_tree.links
        nodes.clear();bsdf=nodes.new('ShaderNodeBsdfPrincipled');output=nodes.new('ShaderNodeOutputMaterial')
        links.new(bsdf.outputs['BSDF'],output.inputs['Surface'])
        maps={}
        for kind in ['Base','Normal','Mask']:
            node=nodes.new('ShaderNodeTexImage')
            path=out/'Textures'/f"{spec['family']}_{kind}.png"
            node.image=bpy.data.images.load(str(path),check_existing=True)
            node.image.filepath='//../Textures/'+path.name
            node.image.colorspace_settings.name='sRGB' if kind=='Base' else 'Non-Color'
            maps[kind]=node
        tint=nodes.new('ShaderNodeMixRGB');tint.blend_type='MULTIPLY';tint.inputs[0].default_value=1;tint.inputs[2].default_value=(*spec['color'],1)
        links.new(maps['Base'].outputs['Color'],tint.inputs[1]);links.new(tint.outputs[0],bsdf.inputs['Base Color'])
        normal=nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.16 if spec['family']=='Plaster' else .65
        links.new(maps['Normal'].outputs['Color'],normal.inputs['Color']);links.new(normal.outputs[0],bsdf.inputs['Normal'])
        split=nodes.new('ShaderNodeSeparateColor');links.new(maps['Mask'].outputs['Color'],split.inputs[0]);links.new(split.outputs[0],bsdf.inputs['Metallic'])
        inv=nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1;links.new(maps['Mask'].outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],bsdf.inputs['Roughness'])
        if spec['emission']:
            bsdf.inputs['Emission Color'].default_value=(1,.61,.24,1);bsdf.inputs['Emission Strength'].default_value=.6

def append_approved_chest(out,manifest):
    if any(c.name.startswith('StorageChest01 -') for c in bpy.data.collections):return
    project=out.parents[3]
    source=project/'Assets/Art/Props/StorageChest01/BlenderSource~/StorageChest01.blend'
    with bpy.data.libraries.load(str(source),link=False) as (src,dst):dst.collections=[n for n in src.collections if n.startswith('StorageChest01 -')]
    col=dst.collections[0];bpy.data.collections['02_Assembled_Safehouse'].children.link(col)
    root=next(o for o in col.objects if o.name=='StorageChest01')
    spec=next(p for p in manifest['placements'] if p['asset']=='StorageChest01')
    import math
    root.location=spec['position'];root.rotation_euler.z=math.radians(spec['rotation_z'])
    for o in col.objects:o.animation_data_clear()
