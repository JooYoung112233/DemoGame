"""Repair existing links and review metadata after the character folder cleanup."""
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
ARCHIVE=ROOT/'ArtSource/CharacterArchive/2026-09-08'
plan=json.loads((ARCHIVE/'cleanup-manifest.json').read_text(encoding='utf-8'))
mapping={Path(o['source']):Path(o['destination']) for o in plan['operations']}
dirs={}
for src,dst in mapping.items():
    for parent in src.parents:
        if not parent.is_relative_to(ROOT/'Assets/ChibiSurvivor'):break
        candidate=ARCHIVE/parent.relative_to(ROOT)
        if not parent.exists() and candidate.is_dir():dirs[parent]=candidate
dirs[ROOT/'Assets/ChibiSurvivor/CompactSurvivor/Animated/Combat/Thrust/Fluid']=ROOT/'Assets/ChibiSurvivor/Player'
replacements={}
for src,dst in {**dirs,**mapping}.items():
    if src.suffix=='.meta':continue
    replacements[src.relative_to(ROOT).as_posix()]=dst.relative_to(ROOT).as_posix()
pattern=re.compile('|'.join(re.escape(k) for k in sorted(replacements,key=len,reverse=True)))
changed=[]; patches=[]
for path in (ROOT/'docs').glob('*.md'):
    old=path.read_text(encoding='utf-8')
    def replace(m):
        prefix=old[max(0,m.start()-60):m.start()]
        if prefix.endswith('ArtSource/CharacterArchive/2026-09-08/'):return m[0]
        return replacements[m[0]]
    new=pattern.sub(replace,old)
    if new!=old:
        changed.append(str(path.relative_to(ROOT)))
        patch='*** Begin Patch\n*** Update File: '+path.as_posix()+'\n'
        for before,after in zip(old.splitlines(),new.splitlines()):
            if before!=after:patch+='@@\n-'+before+'\n+'+after+'\n'
        patches.append(patch+'*** End Patch')
absolute={str(src):str(dst) for src,dst in mapping.items()}
def retarget(value):
    if isinstance(value,str):return absolute.get(value,value)
    if isinstance(value,list):return [retarget(v) for v in value]
    if isinstance(value,dict):return {k:retarget(v) for k,v in value.items()}
    return value
for path in (ROOT/'Assets/ChibiSurvivor/Player').glob('*.json'):
    old=json.loads(path.read_text(encoding='utf-8'));new=retarget(old)
    if new!=old:path.write_text(json.dumps(new,ensure_ascii=False,indent=2),encoding='utf-8');changed.append(str(path.relative_to(ROOT)))
(ROOT/'Library/CodexBlender/character-cleanup-reference-updates.json').write_text(json.dumps(changed,indent=2),encoding='utf-8')
print(json.dumps({'updated_existing_paths':changed,'patches':patches},ensure_ascii=True))
