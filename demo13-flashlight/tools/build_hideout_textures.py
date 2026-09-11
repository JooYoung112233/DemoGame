"""Deterministic, seamless PBR surface maps. No photographed or generated art inputs.
UV unit = one metre. Base RGB is modulated by the kit's linear material palette.
Normal maps are tangent-space +Y; packed mask R=metallic, A=smoothness (URP).
"""
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

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
grain=np.sin(2*np.pi*(y*95+1.1*np.sin(x*2*np.pi)+.4*np.sin(x*6*np.pi)))
grain2=np.sin(2*np.pi*(y*207+.9*np.sin(x*4*np.pi)))
wood=.63+.07*grain+.035*grain2+.23*n1+.19*n2+fine
# Knots repeated at the metre boundary; grain flows around elliptical growth rings.
for cx,cy in [(.23,.37),(.76,.82)]:
    dx=(x-cx+.5)%1-.5;dy=(y-cy+.5)%1-.5
    d=np.sqrt((dx/.15)**2+(dy/.03)**2)
    wood-=.15*np.exp(-d*d*2)+.08*np.cos(d*13)*np.exp(-d*d*.35)
metal=.71+.22*n1+.18*n2+fine
scratches=np.zeros((N,N))
for i in range(110):
    sx=int(rng.integers(0,N));sy=int(rng.integers(0,N));length=int(rng.integers(6,95))
    for t in range(length):scratches[(sy+t//8)%N,(sx+t)%N]=rng.uniform(.15,.3)
metal+=scratches
weave=(np.sin(x*N*np.pi/2)*np.sin(y*N*np.pi/2))*.055
cloth=.72+weave+.2*n1+.1*n2+fine*.6
concrete=.67+.20*n1+.30*n2+.17*n3+fine

def save(name,base,height,smooth,metallic=0):
    Image.fromarray((np.clip(base,0,1)[...,None]*np.ones(3)*255).astype('uint8'),'RGB').save(OUT/f'{name}_Base.png')
    gy,gx=np.gradient(height)
    v=np.stack([-gx*N*.07,-gy*N*.07,np.ones_like(gx)],axis=-1)
    v/=np.linalg.norm(v,axis=-1,keepdims=True)
    Image.fromarray(((v*.5+.5)*255).astype('uint8'),'RGB').save(OUT/f'{name}_Normal.png')
    rgba=np.zeros((N,N,4),dtype='uint8');rgba[:,:,0]=int(metallic*255)
    rgba[:,:,1]=255;rgba[:,:,3]=(np.clip(smooth,0,1)*255).astype('uint8')
    Image.fromarray(rgba,'RGBA').save(OUT/f'{name}_Mask.png')

save('Wood',wood,wood*.035,.18+.10*wood)
save('PaintedMetal',metal,metal*.015,.24+.15*metal,.15)
save('Steel',metal,metal*.009,.34+.19*metal,.7)
save('Cloth',cloth,cloth*.055,.06+cloth*.05)
save('Plaster',concrete,concrete*.06,.10+concrete*.04)
print('PBR_TEXTURES',len(list(OUT.glob('*.png'))))
