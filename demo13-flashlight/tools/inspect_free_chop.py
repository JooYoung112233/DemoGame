import bpy,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'Library/CodexBlender/AxeReview';OUT.mkdir(parents=True,exist_ok=True)
rows=[]
for i in range(1,12):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(ROOT/f'Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack FREE/Animations/2Hand-Sword/RPG-Character@2Hand-Sword-Attack{i}.FBX'))
 sc=bpy.context.scene;end=int(bpy.data.objects['Motion'].animation_data.action.frame_range[1]);poses=[]
 for f in range(1,end+1):
  sc.frame_set(f);bpy.context.view_layer.update()
  r=bpy.data.objects['B_R_Hand'].matrix_world.translation;l=bpy.data.objects['B_L_Hand'].matrix_world.translation;h=bpy.data.objects['B_Head'].matrix_world.translation;c=bpy.data.objects['B_Spine2'].matrix_world.translation
  poses.append({'frame':f,'right':list(r),'left':list(l),'axis':list((r-l).normalized()),'head':list(h),'chest':list(c)})
 peak=max(poses,key=lambda p:p['right'][2]);down=min(zip(poses,poses[1:]),key=lambda p:p[1]['right'][2]-p[0]['right'][2])
 rows.append({'attack':i,'end':end,'peak':peak,'down':down,'samples':[poses[round(j*(end-1)/8)] for j in range(9)]})
(OUT/'SourceAttacks.json').write_text(json.dumps(rows,indent=2))
for r in rows:print('ATTACK',r['attack'],'frames',r['end'],'peak',r['peak']['frame'],[round(v,2) for v in r['peak']['right']],'headZ',round(r['peak']['head'][2],2),'downFrame',r['down'][0]['frame'],'axis',[round(v,2) for v in r['down'][0]['axis']])
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Assets/ChibiSurvivor/Player/DarkSurvivor/BlenderSource~/DarkSurvivor.blend'))
print('HERO_MESHES',[(o.name,len(o.data.vertices)) for o in bpy.context.scene.objects if o.type=='MESH'])
