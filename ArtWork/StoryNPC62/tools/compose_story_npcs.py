"""Label real Blender renders and sample the actual encoded review videos."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import json
OUT=Path(__file__).resolve().parents[1]
FONT='C:/Windows/Fonts/malgun.ttf'
font=ImageFont.truetype(FONT,27);small=ImageFont.truetype(FONT,20);title=ImageFont.truetype(FONT,32)
bg=(28,32,29);white=(233,224,205);muted=(180,188,177)
card=Image.new('RGB',(1440,1180),bg);d=ImageDraw.Draw(card)
d.text((28,17),'1지역 스토리 NPC · 4명',font=title,fill=white)
d.text((28,62),'실제 3D 모델 / 62° 탑다운 / 기존 C 리그 / 각 대기 모션 1개 / Unity 적용 전',font=small,fill=muted)
top=Image.open(OUT/'Review/Top62.png').convert('RGB').crop((0,150,1440,580))
front=Image.open(OUT/'Review/Front.png').convert('RGB').crop((0,115,1440,635))
card.paste(top,(0,130));card.paste(front,(0,610))
d.text((28,102),'62° 게임 시점',font=small,fill=muted)
names=['전당포 주인','베테랑 회수꾼','구역 관리인','떠돌이 상인']
for x,name in zip([218,552,887,1222],names):
 w=d.textbbox((0,0),name,font=font)[2];d.text((x-w/2,565),name,font=font,fill=white)
d.text((28,1141),'아래: 형태 확인용 낮은 시점 · 회수꾼은 쪼그려 앉은 상태',font=small,fill=muted)
card.save(OUT/'NPCOverview.jpg',quality=94)
for label in ['Quarter','Rear62','Side']:
 im=Image.open(OUT/'Review'/(label+'.png')).convert('RGB');c=Image.new('RGB',(1440,870),bg);c.paste(im,(0,70));draw=ImageDraw.Draw(c)
 draw.text((28,16),{'Quarter':'사선 형태 검수','Rear62':'62° 후면 · 머리와 등짐','Side':'측면 · 앉은 자세와 소품 접촉'}[label],font=font,fill=white)
 for x,name in zip([218,552,887,1222],names):
  w=draw.textbbox((0,0),name,font=small)[2];draw.text((x-w/2,835),name,font=small,fill=muted)
 c.save(OUT/(label+'Review.jpg'),quality=93)
print('NPC_COMPARISON_READY')
