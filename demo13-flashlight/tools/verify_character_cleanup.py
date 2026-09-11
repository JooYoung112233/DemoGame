"""Verify archived bytes, kept Unity references, and repaired character links."""
import json,hashlib,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
ARCHIVE=ROOT/'ArtSource/CharacterArchive/2026-09-08'
plan=json.loads((ARCHIVE/'cleanup-manifest.json').read_text(encoding='utf-8'))
retargeted={'AnimationCheck.json','ComparisonCheck.json'}
checked=0
for op in plan['operations']:
    dest=Path(op['destination']);assert dest.is_file(),str(dest)
    if not (op['kind']=='current' and dest.name in retargeted):
        assert hashlib.sha256(dest.read_bytes()).hexdigest()==op['sha256'],str(dest)
        checked+=1
for path in plan['kept_files']:assert Path(path).is_file(),path
for asset,referrers in plan['external_roots'].items():
    meta=Path(str(ROOT/asset)+'.meta')
    guid=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(encoding='utf-8'),re.M)[1]
    for referrer in referrers:assert guid in (ROOT/referrer).read_text(encoding='utf-8'),referrer
broken=[];links=0
for name in ['char-art.md','character-modeling-workflow.md','chibi-survivor-3d.md']:
    path=ROOT/'docs'/name
    for target in re.findall(r'\]\(([^)]+)\)',path.read_text(encoding='utf-8')):
        if target.startswith(('../Assets/ChibiSurvivor/','../ArtSource/CharacterArchive/')):
            links+=1
            if not (path.parent/target).resolve().exists():broken.append([name,target])
assert not broken,broken
report={'status':'passed','exact_hash_checks':checked,'retargeted_metadata':sorted(retargeted),
    'retained_runtime_assets':plan['preserved_assets'],'character_links_checked':links,
    'current':plan['current_root'],'archive':plan['archive_root'],'summary':plan['summary']}
(ROOT/'Library/CodexBlender/character-cleanup-verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=True))
