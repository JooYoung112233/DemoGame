from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtWork/CharacterProportionC'
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',26);small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',19)
for source,target in [('BeforeAfter.png','FrontComparison.jpg'),('QuarterBeforeAfter.png','Comparison.jpg')]:
 im=Image.open(OUT/source).convert('RGB');canvas=Image.new('RGB',(1400,880),(29,32,29));canvas.paste(im,(0,80));d=ImageDraw.Draw(canvas)
 d.text((24,13),'C 비례 · 플레이어와 밴딧 공통 적용본',font=font,fill=(230,222,204))
 d.text((24,51),'Blender 모델 비교 / 전투 플레이 유지 / Unity 적용 전',font=small,fill=(176,184,172))
 for x,label in [(280,'플레이어 · 이전'),(560,'플레이어 · C'),(840,'밴딧 · 이전'),(1120,'밴딧 · C')]:
  box=d.textbbox((0,0),label,font=font);d.text((x-(box[2]-box[0])/2,819),label,font=font,fill=(220,230,191) if label.endswith('C') else (194,196,188))
 canvas.save(OUT/target,quality=94)
print('C_COMPARISON_READY')
