"""Make a labelled sheet from the three actual Blender comparison renders."""
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtWork/TownCharacterFit'
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',26);small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',20)
canvas=Image.new('RGB',(1800,790),(29,32,29));d=ImageDraw.Draw(canvas)
d.text((24,17),'캐릭터와 배경 비례 비교',font=font,fill=(231,222,203))
d.text((24,60),'동일한 45° 카메라 · 동일한 조명 · Blender 비교 / 실제 게임 화면 아님',font=small,fill=(176,185,173))
for i,(name,title,lines) in enumerate([
 ('A_LargeTiles','A · 원본 체형 + 큰 바닥 패턴',['비교용 2m 타일 / 강한 줄눈','첨부 화면의 실제 타일 치수를 재현한 것은 아님']),
 ('B_HumanScaleGround','B · 원본 체형 + 작은 바닥 패턴',['체형은 A와 동일 / 0.8m 포장','줄눈의 굵기와 대비를 함께 낮춤']),
 ('C_ProportionStudy','C · 체형 미세 조정 제안',['B의 바닥 / 머리 −10% · 몸 높이 +8%','몸통 두께 +12% / 리그·게임에는 미적용'])]):
 x=i*600+12;im=Image.open(OUT/f'{name}.png').convert('RGB');im.thumbnail((576,510),Image.Resampling.LANCZOS);canvas.paste(im,(x,110))
 d.text((x,641),title,font=font,fill=(231,222,203))
 for j,line in enumerate(lines):d.text((x,685+j*32),line,font=small,fill=(176,185,173))
canvas.save(OUT/'Comparison.jpg',quality=94);print(OUT/'Comparison.jpg')
