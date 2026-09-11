"""Compose labelled, unmodified Blender renders for review."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT.parent/'ArtWork/TownInteriorsPartition62';OLD=ROOT.parent/'ArtWork/TownInteriors62'
m=json.loads((OUT/'KitManifest.json').read_text(encoding='utf8'))
font='C:/Windows/Fonts/malgun.ttf';title=ImageFont.truetype(font,29);body=ImageFont.truetype(font,20)
rooms=[('Pawnshop','전당포'),('Repair','수리점'),('Medical','의료소'),('Furniture','가구점'),('BlackMarket','암시장')]
sheet=Image.new('RGB',(1800,1650),(29,32,30));d=ImageDraw.Draw(sheet)
d.text((26,17),'마을 내부 수정 · 작은 영업 공간 + 잠긴 내부 문',font=title,fill=(232,227,206))
d.text((26,60),'건물 외곽 유지 / 가구 실제 크기 유지 / 62° Blender 미리보기 / Unity 적용 전',font=body,fill=(166,184,169))
for i,(n,label) in enumerate(rooms):
 x=i%2*900;y=105+i//2*510;b=m['buildings'][n]
 im=Image.open(OUT/f'{n}Service62.png').convert('RGB');im.thumbnail((880,420));sheet.paste(im,(x+(900-im.width)//2,y))
 d.text((x+22,y+424),f"{label} · 영업 공간 {b['serviceWidth']} × {b['serviceDepth']}m",font=title,fill=(232,227,206))
 d.text((x+22,y+469),f"외곽 {b['width']} × {b['depth']}m 그대로 · 나머지 구역은 잠금",font=body,fill=(166,184,169))
for j,line in enumerate(['지금은 닫힌 문','뒤 · 왼쪽 · 오른쪽 방을 독립 분리','해금 후에는 벽을 유지하고 문을 개방','문짝 피벗 · 실제 문 구멍 · 구역 덮개 분리','','사진에서는 영업 공간을 확대해 표시','건물 전체는 별도 전당포 비교 이미지 참고','','게임 내 해금 / 세이브 / 문 동작은 연결 전']):
 d.text((930,1160+j*43),line,font=title if j==0 else body,fill=(226,224,205) if j==0 else (166,184,169))
sheet.save(OUT/'InteriorsOverview.jpg',quality=94)
card=Image.new('RGB',(1800,900),(29,32,30));d=ImageDraw.Draw(card)
d.text((26,17),'전당포 · 외곽은 그대로, 내부 이용 공간만 축소',font=title,fill=(232,227,206))
d.text((26,60),'192m² 전체 매장 → 54m² 영업 공간 + 138m² 잠긴 추가 방',font=body,fill=(166,184,169))
for i,(path,label) in enumerate([(OLD/'Pawnshop62.png','이전 · 넓은 전체 매장'),(OUT/'Pawnshop62.png','현재 · 폐쇄 구역의 덮개 유지'),(OUT/'PawnshopService62.png','영업 공간 확대 · 내부 문으로 확장')]):
 im=Image.open(path).convert('RGB');im.thumbnail((590,610));x=i*600
 card.paste(im,(x+(600-im.width)//2,130+(610-im.height)//2))
 d.text((x+18,754),label,font=body,fill=(232,227,206))
d.text((26,824),'각 사진은 개별 프레이밍입니다. 오른쪽은 공간을 확대한 보기이며 가구·캐릭터 크기를 키운 것이 아닙니다.',font=body,fill=(166,184,169))
card.save(OUT/'PawnshopPartitionReview.jpg',quality=94)
print('PARTITION_REVIEW_COMPOSED')
