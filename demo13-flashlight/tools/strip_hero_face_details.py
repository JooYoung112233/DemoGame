def strip_hero_face_details():
 """Keep only the sculpted nose; expression surfaces are carried by head UVs."""
 import bmesh
 obj=bpy.data.objects.get('Hero_FaceDetails') or bpy.data.objects.get('Hero_Nose')
 if obj is None:raise RuntimeError('Missing face source / nose')
 if obj.get('expression_in_texture_v1'):return obj
 bm=bmesh.new();bm.from_mesh(obj.data)
 remove=[f for f in bm.faces if 'nose' not in obj.data.materials[f.material_index].name.lower()]
 bmesh.ops.delete(bm,geom=remove,context='FACES')
 loose=[v for v in bm.verts if not v.link_faces]
 if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
 bm.to_mesh(obj.data);bm.free();obj.name='Hero_Nose';obj.data.name='Hero_Nose'
 nose_material=next(m for m in obj.data.materials if 'nose' in m.name.lower())
 obj.data.materials.clear();obj.data.materials.append(nose_material)
 for face in obj.data.polygons:face.material_index=0
 obj['expression_in_texture_v1']=True;obj.data.update();return obj
