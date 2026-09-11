"""Assemble offline render previews for inspection; no game assets are changed."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
OUT=Path(__file__).resolve().parents[2]/'ArtWork/TownReview'
names=[('Pawnshop','전당포 · 붉은 벽돌'),('Repair','수리점 · 작업장 셔터'),('Medical','의료소 · 밝은 타일'),('Furniture','가구점 · 목재 전면'),('BlackMarket','암시장 · 검은 벽돌'),('Container_Home','집 · 올리브 철판')]
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',21);small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',17)
canvas=Image.new('RGB',(1440,992),(30,34,34));d=ImageDraw.Draw(canvas)
d.text((24,16),'안전구역 건물 · 원화 대조 모델 1차 시안',font=font,fill=(231,225,207))
d.text((24,49),'Blender 스튜디오 미리보기 / Unity 최종 적용·검증 대기',font=small,fill=(168,177,171))
for i,(name,label) in enumerate(names):
 im=Image.open(OUT/f'Model_{name}.png').convert('RGB');im.thumbnail((476,397))
 x=(i%3)*480;y=88+(i//3)*450;canvas.paste(im,(x,y));d.text((x+16,y+407),label,font=font,fill=(231,225,207))
canvas.save(OUT/'ModelsOverview.jpg',quality=94)
print(OUT/'ModelsOverview.jpg')
