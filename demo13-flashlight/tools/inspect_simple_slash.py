import bpy,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Assets/ChibiSurvivor/Player/SimpleHeroStudy'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'BlenderSource~/SimpleHero_Stage5_Slash.blend'));sc=bpy.context.scene;r=bpy.data.objects['SimpleHero_Rig'];a=bpy.data.actions['SwordSlash'];r.animation_data.action=a
sc.frame_set(1);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
for n in ['Chest','Head','HandSocket.R','HandSocket.L']:
 p=r.pose.bones[n];print(n,'pos',list(p.head),'axis',list(p.matrix.to_quaternion()@Vector((0,1,0))),'forward',list((p.matrix.to_quaternion()@p.bone.matrix_local.to_quaternion().inverted())@Vector((0,-1,0))))
for n in ['Study_Head','Review_Sword_Blade']:
 o=bpy.data.objects[n];e=o.evaluated_get(dg);me=e.to_mesh();vs=[e.matrix_world@v.co for v in me.vertices];print(n,'matrix',o.matrix_world,'bounds',[(min(v[i] for v in vs),max(v[i] for v in vs)) for i in range(3)]);e.to_mesh_clear()
errs=[]
for k in range(577):
 f=1+k*.0625;sc.frame_set(int(f),subframe=f-int(f));bpy.context.view_layer.update();err=(r.pose.bones['HandSocket.L'].head-r.pose.bones['HandSocket.R'].matrix@Vector((0,-.15,0))).length;errs.append((err,f))
print('ERRORS',sorted(errs,reverse=True)[:10])
