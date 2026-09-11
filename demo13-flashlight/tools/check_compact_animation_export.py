"""Check exported clip presence, skinning, animation seams, and motion after FBX reimport."""
import bpy, json, struct, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ArtSource/CharacterArchive/2026-09-08/Assets/ChibiSurvivor/CompactSurvivor/Animated'
if '--round-hands' in sys.argv:OUT=OUT/'RoundHands'
fluid='--fluid' in sys.argv or '--current' in sys.argv
thrust='--stab' in sys.argv or fluid
combat='--onehand' in sys.argv or thrust
if combat:OUT=OUT/'Combat'
if thrust:OUT=OUT/'Thrust'
if fluid:OUT=ROOT/'Assets/ChibiSurvivor/Player'
basename='CompactSurvivor_Combat' if combat else 'CompactSurvivor_Animated'
durations={'Idle':90,'Walk':30,'Run':20}
if combat:durations['Attack_OneHand']=39
if thrust:durations['Attack_OneHand_Thrust']=27 if fluid else 33
if fluid and (OUT/'OneHandAttackSet.json').exists():durations['Attack_OneHand_Chop']=39
if fluid and (OUT/'AttackSet.json').exists():
    durations={'Idle':90,'Walk':30,'Run':20,'Attack_OneHand_Thrust':27,'Attack_TwoHand_Slash':39,'Attack_TwoHand_Chop':42}
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT/(basename+'.fbx')))
rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
assert len(rigs)==1 and len(meshes)==13
rig=rigs[0];scene=bpy.context.scene
actions=list(bpy.data.actions)
names={a.name.split('|')[-1]:a for a in actions}
assert set(names)==set(durations),[a.name for a in actions]
assert all(any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers) for o in meshes)
report={'fbx_meshes':len(meshes),'fbx_bones':len(rig.data.bones),'clips':{}}
for name,action in names.items():
    rig.animation_data.action=action
    if action.slots:
        rig.animation_data.action_slot=action.slots[0]
    start,end=map(int,action.frame_range)
    assert end-start==durations[name]
    snapshots=[]
    for frame in [start,start+(end-start)//4,end]:
        scene.frame_set(frame);bpy.context.view_layer.update()
        dg=bpy.context.evaluated_depsgraph_get();coords=[]
        for o in meshes:
            ev=o.evaluated_get(dg);em=ev.to_mesh()
            coords.extend(ev.matrix_world @ v.co for v in em.vertices)
            ev.to_mesh_clear()
        snapshots.append(coords)
    seam=max((a-b).length for a,b in zip(snapshots[0],snapshots[-1]))
    motion=max((a-b).length for a,b in zip(snapshots[0],snapshots[1]))
    assert seam<1e-4 and motion>.001,(name,seam,motion,[s.identifier for s in action.slots])
    report['clips'][name]={'frames':[start,end],'max_loop_seam_m':seam,'max_motion_m':motion}
data=(OUT/(basename+'.glb')).read_bytes()
magic,version,total=struct.unpack_from('<4sII',data,0);assert magic==b'glTF' and version==2 and total==len(data)
length,kind=struct.unpack_from('<II',data,12);assert kind==0x4e4f534a
gltf=json.loads(data[20:20+length])
report['glb_animations']=[a['name'] for a in gltf.get('animations',[])]
assert set(report['glb_animations'])==set(durations)
report['glb_skinned_mesh_nodes']=sum('mesh' in n and 'skin' in n for n in gltf['nodes'])
assert report['glb_skinned_mesh_nodes']==13
report['unity_checked']=False
(OUT/'ExportCheck.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('ANIMATED_EXPORT_CHECK_OK',json.dumps(report))
