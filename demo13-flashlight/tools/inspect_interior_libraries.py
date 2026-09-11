import bpy
from pathlib import Path
R=Path(__file__).resolve().parents[1]
for kit in ['Hideout02','TownProps02']:
 with bpy.data.libraries.load(str(R/f'Assets/Art/Environments/{kit}/BlenderSource~/{kit}.blend')) as (src,dst):
  print(kit,src.collections)
