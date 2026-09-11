"""Label actual Blender renders after visual review. No generated model images."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT.parent/'ArtWork/TownInteriors62'
font='C:/Windows/Fonts/malgun.ttf';title=ImageFont.truetype(font,31);body=ImageFont.truetype(font,21)
rooms=[('Pawnshop','전당포','진열장 · 금고 · 중고품 선반'),('Repair','수리점','공구대 · 용접 장비 · 정비 공간'),('Medical','의료소','접수대 · 진료대 · 약장'),('Furniture','가구점','식탁 · 의자 · 수납 가구 전시'),('BlackMarket','암시장','천 덮개 판매대 · 물자 창고')]
sheet=Image.new('RGB',(1980,1820),(29,32,30));d=ImageDraw.Draw(sheet)
d.text((30,20),'마을 상점 내부 5종 · 탑다운 62°',font=title,fill=(232,227,206))
d.text((30,67),'Blender 모델 미리보기 · 자체 검수 완료 · Unity 연결/통행 검증 대기',font=body,fill=(165,181,166))
for i,(name,label,detail) in enumerate(rooms):
 im=Image.open(OUT/f'{name}62.png').convert('RGB')
 x=i%2*990;y=115+i//2*560
 # Fit without cropping, preserve source camera/proportions.
 im.thumbnail((970,475));sheet.paste(im,(x+(990-im.width)//2,y))
 d.text((x+30,y+478),label,font=title,fill=(232,227,206));d.text((x+30,y+520),detail,font=body,fill=(169,182,170))
for j,line in enumerate(['검수 기준','GPT 건물별 원화 대조','62°에서 가구와 캐릭터 비례 확인','진열장 반사 · 선반 소품 · 바닥 대비 보완','UV/표면 · 대표 FBX 재임포트 확인','입구와 주요 접근 지점의 평면 통행 확인','','실제 Unity 조명·충돌·상호작용은 연결 후 확인']):
 d.text((1020,1300+j*44),line,font=title if j==0 else body,fill=(220,218,199) if j==0 else (160,178,164))
sheet.save(OUT/'InteriorsOverview.jpg',quality=94)
print('INTERIORS_OVERVIEW_READY')
