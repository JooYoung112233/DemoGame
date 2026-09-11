"""Preserve unrelated concurrent MASTER edits: produce our single index-row change."""
from pathlib import Path
import subprocess
ROOT=Path(__file__).resolve().parents[1]
path='demo13-flashlight/docs/MASTER.md'
base=subprocess.check_output(['git','show','codex/hideout-reference-art-pass:'+path],cwd=ROOT.parent).decode('utf8')
current=(ROOT/'docs/MASTER.md').read_text(encoding='utf8')
tag='| [`building-interior.md`](building-interior.md) |'
replacement=next(line for line in current.splitlines() if line.startswith(tag))
lines=base.splitlines();matches=[i for i,line in enumerate(lines) if line.startswith(tag)];assert len(matches)==1
lines[matches[0]]=replacement
target=ROOT/'Library/TownProps02/interiors-master-commit.md';target.write_bytes(('\n'.join(lines)+'\n').encode('utf8'))
print(target)
