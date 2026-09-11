"""Deterministic, seamless PBR surface maps. No photographed or generated art inputs.
UV unit = one metre. Base RGB is modulated by the kit's linear material palette.
Normal maps are tangent-space +Y; packed mask R=metallic, A=smoothness (URP).
"""
from pathlib import Path
import numpy as np
from PIL import Image

OUT = Path(__file__).resolve().parents[1]/'Assets/Art/Environments/Hideout02/Textures'
OUT.mkdir(parents=True, exist_ok=True)
N=1024
rng=np.random.default_rng(91126)
y,x=np.mgrid[0:N,0:N]/N

def noise(size):
    a=rng.uniform(0,255,(size,size)).astype('uint8')
    # Wrap the coarse field so edges interpolate continuously.
    tiled=np.tile(a,(3,3))
    im=Image.fromarray(tiled).resize((N*3,N*3),Image.Resampling.BICUBIC)
    return np.asarray(im,dtype=float)[N:2*N,N:2*N]/255-.5

n1,n2,n3=noise(8),noise(48),noise(256)
fine=rng.normal(0,.035,(N,N))
grain=np.sin(2*np.pi*(y*23+.22*np.sin(x*2*np.pi)+.09*np.sin(x*6*np.pi)))
grain2=np.sin(2*np.pi*(y*61+.36*np.sin(x*4*np.pi)))
# Broad painted colour first. Fibres are intermittent, low contrast, not a striped overlay.
growth=(.35+np.clip(n1+.3,0,.7))*grain
wood=.64+.014*growth+.005*grain2+.035*n1+.025*n2+fine*.10
for cx,cy in [(.23,.37)]:
    dx=(x-cx+.5)%1-.5;dy=(y-cy+.5)%1-.5
    d=np.sqrt((dx/.15)**2+(dy/.03)**2)
    wood-=.022*np.exp(-d*d*2)+.008*np.cos(d*10)*np.exp(-d*d*.35)
# Coated metal has almost uniform pigment; no large cloudy patches in colour or normal.
metal=.71+.017*n1+.019*n2+fine*.10
scratches=np.zeros((N,N))
for i in range(24):
    sx=int(rng.integers(0,N));sy=int(rng.integers(0,N));length=int(rng.integers(6,95))
    for t in range(length):scratches[(sy+t//8)%N,(sx+t)%N]=rng.uniform(.15,.3)
metal+=scratches*.10
weave=(np.sin(x*N*np.pi/2)*np.sin(y*N*np.pi/2))*.055
cloth=.72+weave*.13+.028*n1+.014*n2+fine*.08
concrete=.67+.045*n1+.06*n2+.035*n3+fine*.12

def save(name,base,height,smooth,metallic=0):
    Image.fromarray((np.clip(base,0,1)[...,None]*np.ones(3)*255).astype('uint8'),'RGB').save(OUT/f'{name}_Base.png')
    # Central differences with wrap: normal-map derivatives are seamless as well.
    gx=(np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*.5
    gy=(np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*.5
    v=np.stack([-gx*N*.07,-gy*N*.07,np.ones_like(gx)],axis=-1)
    v/=np.linalg.norm(v,axis=-1,keepdims=True)
    Image.fromarray(((v*.5+.5)*255).astype('uint8'),'RGB').save(OUT/f'{name}_Normal.png')
    rgba=np.zeros((N,N,4),dtype='uint8');rgba[:,:,0]=int(metallic*255)
    rgba[:,:,1]=255;rgba[:,:,3]=(np.clip(smooth,0,1)*255).astype('uint8')
    Image.fromarray(rgba,'RGBA').save(OUT/f'{name}_Mask.png')

save('Wood',wood,growth*.0015+grain2*.0004,.19+n2*.025)
save('PaintedMetal',metal,scratches*.0005+fine*.00015,.17+n2*.015,.02)
save('Steel',metal,scratches*.0004+fine*.00012,.32+n2*.03,.65)
save('Cloth',cloth,weave*.004+n3*.0001,.065+n2*.006)
save('Plaster',concrete,n2*.0004+n3*.0002,.10+n2*.008)
print('PBR_TEXTURES',len(list(OUT.glob('*.png'))))
