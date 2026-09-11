"""Assemble offline render previews for inspection; no game assets are changed."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
OUT=Path(__file__).resolve().parents[2]/'ArtWork/TownReview'
names=[('Pawnshop','전당포 · 높은 벽돌 난간'),('Repair','수리점 · 물탱크와 노출 배관'),('Medical','의료소 · 낮은 공조 설비'),('Furniture','가구점 · 부분 판금 보수'),('BlackMarket','암시장 · 검은 난간과 배선'),('Container_Home','집 · 낮은 철골 테두리')]
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',21);small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',17)
canvas=Image.new('RGB',(1440,992),(30,34,34));d=ImageDraw.Draw(canvas)
d.text((24,16),'안전구역 지붕 · 건물별 형태 보완',font=font,fill=(231,225,207))
d.text((24,49),'Blender 스튜디오 미리보기 / Unity 최종 적용·검증 대기',font=small,fill=(168,177,171))
for i,(name,label) in enumerate(names):
 im=Image.open(OUT/f'Model_{name}.png').convert('RGB');im.thumbnail((476,397))
 x=(i%3)*480;y=88+(i//3)*450;canvas.paste(im,(x,y));d.text((x+16,y+407),label,font=font,fill=(231,225,207))
canvas.save(OUT/'ModelsOverview.jpg',quality=94)
canvas.save(OUT/'RoofsOverview.jpg',quality=94)
print(OUT/'ModelsOverview.jpg')
