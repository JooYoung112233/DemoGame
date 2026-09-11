from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtWork/BanditRoles62'
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',26);small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',19)
for source,target in [('Roles62.png','Comparison.jpg'),('Rear62.png','RearComparison.jpg'),('Movement62.png','MovementComparison.jpg'),('Side62.png','SideComparison.jpg'),('Run62.png','RunComparison.jpg')]:
 im=Image.open(OUT/source).convert('RGB');canvas=Image.new('RGB',(1200,930),(29,32,29));canvas.paste(im,(0,90));d=ImageDraw.Draw(canvas)
 d.text((24,17),'밴딧 2종 · 탑다운 62°',font=font,fill=(231,222,204))
 d.text((24,55),'동일한 기존 리그 / C 비례 / Blender 미리보기 · Unity 적용 전',font=small,fill=(177,185,173))
 for x,title,desc in [(380,'근접 · 기존 후드','갈색 상의 · 후드 · 붕대'),(820,'원거리 · 신규 본체','군용 헬멧 · 조끼 · 탄창 파우치')]:
  width=d.textbbox((0,0),title,font=font)[2];d.text((x-width/2,848),title,font=font,fill=(231,222,204));width=d.textbbox((0,0),desc,font=small)[2];d.text((x-width/2,887),desc,font=small,fill=(177,185,173))
 canvas.save(OUT/target,quality=94)
old=Image.open(OUT/'BeforeRevision.jpg').convert('RGB').crop((600,90,1200,850))
new=Image.open(OUT/'Roles62.png').convert('RGB').crop((600,0,1200,760))
card=Image.new('RGB',(1200,920),(29,32,29));card.paste(old,(0,80));card.paste(new,(600,80));d=ImageDraw.Draw(card)
d.text((24,14),'원거리 밴딧 · 형태 수정 전 / 후',font=font,fill=(231,222,204))
d.text((24,49),'동일한 62° 카메라 · 기존 23본 리그 · Blender 모델 검수',font=small,fill=(177,185,173))
d.text((130,859),'이전 · 넓고 납작한 헬멧',font=small,fill=(177,185,173))
d.text((718,859),'수정 · 둥근 헬멧 / 작은 파우치',font=small,fill=(231,222,204))
card.save(OUT/'RangedRevisionComparison.jpg',quality=94)
print('ROLES62_COMPARISON_READY')
