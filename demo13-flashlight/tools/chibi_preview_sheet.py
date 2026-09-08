"""Compose rendered model views and motion previews (Pillow, no asset edits)."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
root = Path(__file__).resolve().parents[1] / 'ArtSource/ChibiSurvivor'
font_path = 'C:/Windows/Fonts/segoeui.ttf'
bold_path = 'C:/Windows/Fonts/segoeuib.ttf'
font = lambda size: ImageFont.truetype(font_path, size)
bold = lambda size: ImageFont.truetype(bold_path, size)
sheet = Image.new('RGB', (1500, 720), '#17272c')
draw = ImageDraw.Draw(sheet)
draw.text((40, 22), 'BRB / LITTLE SURVIVOR', font=bold(31), fill='#efdebe')
draw.text((40, 66), 'CHARACTER 01     /     LOW POLY     /     2.5 HEADS     /     RIGGED', font=font(16), fill='#a5b6b2')
for i, view in enumerate(['front', 'quarter', 'back']):
    panel = Image.open(root/(view+'.png')).convert('RGB').resize((490, 560))
    sheet.paste(panel, (5+i*500, 109))
    draw.text((27+i*500, 678), ['01  FRONT', '02  QUARTER', '03  BACK / PACK'][i], font=bold(17), fill='#efdebe')
sheet.save(root/'CharacterSheet.png')
# Twelve seconds is the common loop period of all three authored motions.
periods = {'Idle': 2.4, 'Walk': 1, 'Run': 2/3}
frames = {name: [Image.open(root/'frames'/name/('%02d.png'%i)).convert('RGB') for i in range(12)] for name in periods}
for name, period in periods.items():
    frames[name][0].save(root/(name+'.gif'), save_all=True, append_images=frames[name][1:], duration=round(period*1000/12), loop=0)
timeline = []
for frame in range(180):
    canvas = Image.new('RGB', (1050, 452), '#17272c')
    d = ImageDraw.Draw(canvas)
    for i, (name, period) in enumerate(periods.items()):
        index = int(((frame/15)%period)/period*12)%12
        canvas.paste(frames[name][index], (i*350, 0))
        d.text((i*350+22, 414), name.upper()+'  /  '+str(round(period,2))+'s', font=bold(18), fill='#efdebe')
    timeline.append(canvas)
timeline[0].save(root/'Animations.gif', save_all=True, append_images=timeline[1:], duration=67, loop=0)
print('Preview sheet and four GIFs created.')
