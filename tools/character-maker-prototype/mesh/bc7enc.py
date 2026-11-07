# Small BC7 encoder (mode 6 only: one subset, RGBA 7.7.7.7 + p-bit, 4-bit indices), enough for flat colours and normal maps.
import numpy as np
from PIL import Image
def _encode_level(rgba):
    h,w,_=rgba.shape; H=max(4,(h+3)//4*4); W=max(4,(w+3)//4*4)
    a=np.zeros((H,W,4),np.float32); a[:h,:w]=rgba; a[h:,:]=a[h-1:h,:] if h<H else a[h:,:]; a[:,w:]=a[:,w-1:w] if w<W else a[:,w:]
    blk=a.reshape(H//4,4,W//4,4,4).transpose(0,2,1,3,4).reshape(-1,16,4)
    lo=blk.min(1); hi=blk.max(1)
    def q(c):   # 8-bit -> (7-bit value, p-bit) choosing per endpoint the p-bit with the smaller error
        c=np.clip(np.round(c),0,255)
        best=None
        for p in (0,1):
            v=np.clip(np.round((c-p)/2),0,127); r=(v*2+p); err=((r-c)**2).sum(1)
            if best is None: best=(v,np.full(len(c),p),err,r)
            else:
                m=err<best[2]; best=(np.where(m[:,None],v,best[0]),np.where(m,p,best[1]),np.where(m,err,best[2]),np.where(m[:,None],r,best[3]))
        return best[0].astype(np.uint64),best[1].astype(np.uint64),best[3]
    v0,p0,e0=q(lo); v1,p1,e1=q(hi)
    d=(e1-e0); dd=(d*d).sum(1); dd[dd==0]=1
    t=((blk-e0[:,None,:])*d[:,None,:]).sum(2)/dd[:,None]
    idx=np.clip(np.round(t*15),0,15).astype(np.uint64)
    # anchor index 0 must have its top bit clear: swap endpoints where needed
    sw=idx[:,0]>=8
    v0s=np.where(sw[:,None],v1,v0); v1s=np.where(sw[:,None],v0,v1); p0s=np.where(sw,p1,p0); p1s=np.where(sw,p0,p1)
    idx=np.where(sw[:,None],15-idx,idx)
    out=np.zeros((len(blk),2),np.uint64)   # 128 bits as two u64 (lo, hi)
    bits=[(np.full(len(blk),1<<6,np.uint64),7)]
    for ch in range(4): bits+=[(v0s[:,ch],7),(v1s[:,ch],7)]
    bits+=[(p0s,1),(p1s,1),(idx[:,0],3)]+[(idx[:,k],4) for k in range(1,16)]
    pos=0
    for val,n in bits:
        for b in range(n):
            bit=(val>>np.uint64(b))&np.uint64(1); word=(pos+b)//64; sh=np.uint64((pos+b)%64)
            out[:,word]|=bit<<sh
        pos+=n
    assert pos==128
    return out.astype('<u8').tobytes()
def encode(img,size=None):
    """PIL image -> (w,h,mips,bytes) BC7 with a full mip chain"""
    img=img.convert('RGBA')
    if size: img=img.resize(size,Image.LANCZOS)
    w,h=img.size; data=b''; mips=0; cur=img
    while True:
        data+=_encode_level(np.asarray(cur,np.float32)); mips+=1
        cw,ch=cur.size
        if cw==1 and ch==1: break
        cur=cur.resize((max(1,cw//2),max(1,ch//2)),Image.LANCZOS)
    return w,h,mips,data
def dds(img,size=None):
    import struct
    w,h,m,data=encode(img,size)
    hd=bytearray(148); hd[0:4]=b'DDS '; struct.pack_into('<II',hd,4,124,0x1|0x2|0x4|0x1000|0x20000)
    struct.pack_into('<II',hd,12,h,w); struct.pack_into('<I',hd,28,m); struct.pack_into('<II',hd,76,32,0x4); hd[84:88]=b'DX10'
    struct.pack_into('<I',hd,108,0x1000|0x400000|0x8); struct.pack_into('<IIIII',hd,128,98,3,0,1,0); return bytes(hd)+data
