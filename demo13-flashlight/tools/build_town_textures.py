"""Numerical seamless PBR maps: muted mortar and plaster, low-contrast asphalt."""
from pathlib import Path
import numpy as np
from PIL import Image
OUT=Path(__file__).resolve().parents[1]/'Assets/Art/Environments/Town02/Textures'
OUT.mkdir(parents=True,exist_ok=True)
N=1024;rng=np.random.default_rng(911);y,x=np.mgrid[:N,:N]/N
def noise(s):
 a=rng.uniform(0,255,(s,s)).astype('uint8')
 return np.asarray(Image.fromarray(np.tile(a,(3,3))).resize((3*N,3*N),Image.Resampling.BICUBIC),float)[N:2*N,N:2*N]/255-.5
a,b,c=noise(8),noise(64),noise(256)
def save(name,base,h,smooth):
 Image.fromarray((np.clip(base,0,1)[...,None]*np.ones(3)*255).astype('uint8')).save(OUT/f'{name}_Base.png')
 dx=(np.roll(h,-1,1)-np.roll(h,1,1))*N*.5;dy=(np.roll(h,-1,0)-np.roll(h,1,0))*N*.5
 v=np.stack([-dx,-dy,np.ones_like(dx)],-1);v/=np.linalg.norm(v,axis=-1,keepdims=True)
 Image.fromarray(((v*.5+.5)*255).astype('uint8')).save(OUT/f'{name}_Normal.png')
 rgba=np.zeros((N,N,4),dtype='uint8');rgba[:,:,1]=255;rgba[:,:,3]=(np.clip(smooth,0,1)*255).astype('uint8')
 Image.fromarray(rgba).save(OUT/f'{name}_Mask.png')
# Broad stylized masonry is legible in the quarter-view rather than sub-pixel mortar.
row=np.floor(y*4);u=(x*2+(row%2)*.5)%1;v=(y*4)%1
edge=np.minimum(np.minimum(u,1-u)*.5,np.minimum(v,1-v)*.25)
joint=np.clip((.014-edge)/.006,0,1)
variation=rng.uniform(-.05,.05,(4,3))[row.astype(int)%4,np.floor(x*2+(row%2)*.5).astype(int)%3]
save('Brick',.73+.022*a+.02*b+variation+joint*.15,(1-joint)*.012+c*.00045,.10+b*.018)
save('Plaster',.79+.035*a+.04*b+.018*c,b*.00065+c*.00025,.095+b*.012)
sites=rng.uniform(0,1,(8,2));dist=[]
for sx,sy in sites:
 dx=(x-sx+.5)%1-.5;dy=(y-sy+.5)%1-.5;dist.append(np.sqrt(dx*dx+dy*dy))
dist=np.sort(np.stack(dist),axis=0);crack=np.clip((.0035-(dist[1]-dist[0]))/.0035,0,1)
save('Roof',.78+.01*a+.02*b-crack*.11,b*.00025-crack*.002,.08+b*.009)
tileedge=np.minimum(np.minimum((x*2)%1,1-(x*2)%1),np.minimum((y*2)%1,1-(y*2)%1))*.5
tilejoint=np.clip((.009-tileedge)/.005,0,1)
save('Tile',.82+.017*a+.012*b-tilejoint*.075,-tilejoint*.003+b*.00012,.20+b*.01)
aggregate=np.maximum(c-.20,0)
save('Asphalt',.72+.045*a+.055*b+aggregate*.2,b*.0009+c*.00055,.065+b*.013)
print('TOWN_TEXTURE_MAPS',len(list(OUT.glob('*.png'))))
