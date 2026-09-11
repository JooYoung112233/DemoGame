"""Label the actual Blender renders without altering model appearance."""
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtWork/TownPropsReview'
font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',25)
small=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',18)
canvas=Image.new('RGB',(1500,1660),(30,33,31));d=ImageDraw.Draw(canvas)
d.text((32,20),'마을 생활 프랍 · 18종',font=font,fill=(227,219,201))
d.text((32,61),'쿼터뷰 모델 미리보기 · Blender 스튜디오 · Unity 적용 전',font=small,fill=(170,177,165))
for group,xy,width,title,desc in [
 ('Living',(24,110),930,'생활 공간','빨랫줄 · 세척대 · 급수통 · 양동이 · 빨래 바구니'),
 ('Workshop',(24,940),718,'작업 공간','사다리 · 배전함 · 공구함 · 소화기 · 조리대 · 로프 · 방수포'),
 ('Alley',(758,940),718,'골목 소품','폐기물함 · 쓰레기봉투 · 상자 · 가림막 · 폐자재 · 자전거')]:
 im=Image.open(OUT/f'{group}.png').convert('RGB');im.resize((width,round(width*im.height/im.width)),Image.Resampling.LANCZOS).save(OUT/f'{group}_thumb.jpg',quality=93)
 im.thumbnail((width,round(width*850/1100)),Image.Resampling.LANCZOS);canvas.paste(im,xy)
 yy=xy[1]+im.height+12;d.text((xy[0],yy),title,font=font,fill=(227,219,201));d.text((xy[0],yy+40),desc,font=small,fill=(177,183,171))
d.text((992,198),'기존 마을 소품과 조합',font=font,fill=(227,219,201))
for i,t in enumerate(['벤치 · 가로등 · 게시판','드럼통 · 타이어 · 팔레트','마당 테이블 · 의자 · 수레','','18개 개별 FBX + 편집 원본','UV · 표면 노멀 · PBR 맵','전투 플레이 유지 / 모델 제작']):d.text((992,253+i*42),t,font=small,fill=(177,183,171))
canvas.save(OUT/'PropsOverview.jpg',quality=94)
print(OUT/'PropsOverview.jpg')
