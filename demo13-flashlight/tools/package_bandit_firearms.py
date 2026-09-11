"""Encode the actual Unity capture as a labeled, looping confirmation GIF."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

out=Path(__file__).resolve().parents[2]/'ArtWork/SimpleBandit/Firearms'
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',23)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
frames=[]
for i,p in enumerate(sorted((out/'Frames').glob('*.png'))):
    im=Image.open(p).convert('RGB'); d=ImageDraw.Draw(im)
    d.text((225,22),'돌격소총 · 양손',font=font,fill='#e8e5df',anchor='mt')
    d.text((675,22),'권총 · 한손',font=font,fill='#e8e5df',anchor='mt')
    t=i/10
    phase='비전투 · 수납하고 걷기' if t<2 or t>=11.2 else '적 발견 · 꺼내기' if t<3.2 else '조준' if t<5 else '총 들고 걷기' if t<8 else '사격 · 무료 에셋 반동 모션' if t<10 else '경계 해제 · 수납'
    d.text((450,567),phase,font=small,fill='#e8e5df',anchor='mt')
    frames.append(im.quantize(colors=128))
frames[0].save(out/'BanditFirearmsReview.gif',save_all=True,append_images=frames[1:],duration=100,loop=0,optimize=False)
print(out/'BanditFirearmsReview.gif')
