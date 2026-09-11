"""Resolve archived authoring sources without restoring them into Unity Assets."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def source_path(relative):
 p=ROOT/relative
 if p.exists():return p
 manifest=ROOT/'Library/character-archive-manifest.json'
 if manifest.exists():
  data=json.loads(manifest.read_text(encoding='utf-8-sig'))
  candidate=Path(data['archive'])/relative
  if candidate.exists():return candidate
 raise FileNotFoundError(relative)
def archive_folder():
 return Path(json.loads((ROOT/'Library/character-archive-manifest.json').read_text(encoding='utf-8-sig'))['archive'])
