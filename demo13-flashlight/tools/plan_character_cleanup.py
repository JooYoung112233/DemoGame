"""Read-only Unity GUID dependency audit and a reversible character archive plan."""
import json, re, hashlib, subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'Assets'; CHAR=ASSETS/'ChibiSurvivor'
ARCHIVE=ROOT/'ArtSource/CharacterArchive/2026-09-08'
CURRENT=CHAR/'Player'
FLUID=CHAR/'CompactSurvivor/Animated/Combat/Thrust/Fluid'
assert FLUID.is_dir() and not CURRENT.exists() and not ARCHIVE.exists()
allfiles=[ROOT/p for p in subprocess.check_output(['rg','--files','--hidden','Assets'],cwd=ROOT,text=True,encoding='utf-8').splitlines()]
charfiles=sorted(p for p in CHAR.rglob('*') if p.is_file())
guid_to_asset={}
for meta in CHAR.rglob('*.meta'):
    match=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(encoding='utf-8'),re.M)
    if match:guid_to_asset[match[1]]=Path(str(meta)[:-5])
guidpat=re.compile(r'guid:\s*([0-9a-f]{32})')
text_extensions={'.prefab','.unity','.asset','.controller','.overrideController','.mat','.anim','.playable','.meta','.json','.shader','.cs'}
def readrefs(path):
    if not path.is_file() or path.suffix not in text_extensions:return set()
    return set(guidpat.findall(path.read_text(encoding='utf-8',errors='ignore')))
roots={}
for p in allfiles:
    if p.is_relative_to(CHAR):continue
    for guid in readrefs(p):
        if guid in guid_to_asset:
            roots.setdefault(guid,[]).append(str(p.relative_to(ROOT)))
keep=set();todo=[guid_to_asset[g] for g in roots]
while todo:
    p=todo.pop()
    if p in keep:continue
    keep.add(p)
    for source in [p,Path(str(p)+'.meta')]:
        for guid in readrefs(source):
            if guid in guid_to_asset and guid_to_asset[guid] not in keep:todo.append(guid_to_asset[guid])
mapping={}
for p in FLUID.iterdir():
    if p.is_file() and '.blend1' not in p.name:mapping[p]=CURRENT/p.name
for src,name in [(CHAR/'CompactSurvivor/Animated/RoundHands/LocomotionPreview.mp4','LocomotionPreview.mp4'),
                 (CHAR/'CompactSurvivor/Animated/RoundHands/Idle_Pose.png','CharacterPreview.png'),
                 (CHAR/'CompactSurvivor/ChosenReference.png','ChosenReference.png')]:
    assert src.is_file()
    mapping[src]=CURRENT/name
    meta=Path(str(src)+'.meta')
    if meta.exists():mapping[meta]=Path(str(CURRENT/name)+'.meta')
keepfiles=set()
for p in keep:
    if p.is_file():keepfiles.add(p)
    meta=Path(str(p)+'.meta')
    if meta.exists():keepfiles.add(meta)
    for parent in p.parents:
        if not parent.is_relative_to(CHAR):break
        meta=Path(str(parent)+'.meta')
        if meta.exists():keepfiles.add(meta)
operations=[]
for p in charfiles:
    if p in mapping:dest=mapping[p];kind='current'
    elif p in keepfiles:continue
    else:dest=ARCHIVE/p.relative_to(ROOT);kind='archive'
    assert not dest.exists(),dest
    operations.append({'source':str(p),'destination':str(dest),'kind':kind,
        'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
# This obsolete menu/importer targets only the archived prototype FBX.
legacy=ASSETS/'Editor/ChibiSurvivorAssets.cs'
for p in allfiles:
    if p.suffix=='.cs' and p!=legacy:
        assert not re.search(r'\b(ChibiSurvivorImporter|ChibiSurvivorAssets)\b',p.read_text(encoding='utf-8',errors='ignore')),p
assert legacy not in keep
for p in [legacy,Path(str(legacy)+'.meta')]:
    if p.exists():operations.append({'source':str(p),'destination':str(ARCHIVE/p.relative_to(ROOT)),
        'kind':'archive','bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
report={'project':str(ROOT),'character_root':str(CHAR),'archive_root':str(ARCHIVE),'current_root':str(CURRENT),
    'external_roots':{str(guid_to_asset[g].relative_to(ROOT)):refs for g,refs in roots.items()},
    'preserved_assets':[str(p.relative_to(ROOT)) for p in sorted(keep) if p.is_file()],
    'kept_files':[str(p) for p in sorted(keepfiles)],'operations':operations,
    'summary':{kind:{'files':sum(o['kind']==kind for o in operations),'bytes':sum(o['bytes'] for o in operations if o['kind']==kind)} for kind in ['current','archive']}}
path=ROOT/'Library/CodexBlender/character-cleanup-plan.json'
path.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:report[k] for k in ['external_roots','preserved_assets','summary']},ensure_ascii=False,indent=2))
