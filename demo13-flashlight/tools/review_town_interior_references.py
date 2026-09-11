"""Read-only contact sheet of source references; originals remain untouched."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT.parent/'ArtWork/TownInteriors62'
OUT.mkdir(parents=True,exist_ok=True)
base=ROOT/'Assets/GPT/안전구역'
prefix='ChatGPT Image 2026년 6월 11일 오후 '
refs=[('전당포 내부',base/'전당포/완전체.png'),('전당포 프랍',base/'전당포'/f'{prefix}06_41_32.png'),('수리점',base/f'{prefix}07_37_36.png'),('의료소',base/f'{prefix}07_40_15.png'),('암시장',base/f'{prefix}07_40_37.png'),('가구점',base/f'{prefix}07_42_47.png')]
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',20)
im=Image.new('RGB',(1500,1050),(40,41,38));d=ImageDraw.Draw(im)
for i,(label,path) in enumerate(refs):
 p=Image.open(path).convert('RGB');p.thumbnail((490,470));x=i%3*500;y=i//3*525
 im.paste(p,(x+(500-p.width)//2,y+30));d.text((x+10,y+5),label,font=font,fill='white')
im.save(OUT/'ReferenceIndex.jpg',quality=92)
ext=[('전당포','06_38_18'),('수리점','06_59_29'),('의료소','06_53_58'),('가구점','06_56_33'),('암시장','06_57_39')]
im=Image.new('RGB',(1500,1050),(40,41,38));d=ImageDraw.Draw(im)
for i,(label,t) in enumerate(ext):
 p=Image.open(base/f'{prefix}{t}.png').convert('RGB');p.thumbnail((490,470));x=i%3*500;y=i//3*525
 im.paste(p,(x+(500-p.width)//2,y+30));d.text((x+10,y+5),label,font=font,fill='white')
im.save(OUT/'ExteriorReferences.jpg',quality=92)
print('\n'.join(str(p) for _,p in refs))
