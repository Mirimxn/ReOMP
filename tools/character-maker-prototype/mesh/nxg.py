# NXG_TEXTURES: header chunk (BE size), a TSXT chunk listing the textures, then the embedded textures as plain DDS files back to back.
import struct,io,os
from PIL import Image
def dds_blobs(d):
    h=struct.unpack('>I',d[:4])[0]; c=h+4; p=c+4+struct.unpack('>I',d[c:c+4])[0]; out=[]
    while p<len(d):
        assert d[p:p+4]==b'DDS ',p
        hh,w=struct.unpack_from('<II',d,p+12); mips=max(1,struct.unpack_from('<I',d,p+28)[0]); fcc=d[p+84:p+88]
        bs=8 if fcc==b'DXT1' else 16; size=0
        for m in range(mips): size+=max(1,(max(1,w>>m)+3)//4)*max(1,(max(1,hh>>m)+3)//4)*bs
        out.append((p,128+size,w,hh,mips,fcc)); p+=128+size
    assert p==len(d)
    return out
def dxt1_dds(img,template_header):
    """img -> DDS (DXT1, full mip chain) reusing a game DDS header (keeps its NVTT/OPAQNU2T markers)"""
    img=img.convert('RGB'); w,h=img.size; data=b''; mips=0; cur=img
    while True:
        b=io.BytesIO(); cur.save(b,format='DDS',pixel_format='DXT1'); data+=b.getvalue()[128:]; mips+=1
        if cur.size==(1,1): break
        cur=cur.resize((max(1,cur.size[0]//2),max(1,cur.size[1]//2)),Image.LANCZOS)
    hd=bytearray(template_header[:128])
    struct.pack_into('<IIIII',hd,12,h,w,max(1,(w+3)//4)*8,0,mips)
    return bytes(hd)+data
def rename(d,old,new,newid=True):
    """copy of a TT resource file under a new name (same-length names: self path, header names)"""
    d=bytearray(d); h=struct.unpack('>I',d[:4])[0]
    n=struct.unpack('>H',d[20:22])[0]; sp=bytes(d[22:22+n])
    lo,ln=sp.lower(),new.lower(); assert old.lower().encode() in lo,(old,sp)
    i=lo.find(old.lower().encode()); d[22+i:22+i+len(old)]=new.upper().encode() if sp[i:i+1].isupper() else new.encode()
    k=22+n
    while d[k]==0: k+=1
    if newid: d[k:k+16]=os.urandom(16)
    body=bytes(d[:h+4]); o=0; cnt=0
    while (o:=body.find(old.encode(),o+1))>0:
        if o<22+n: continue
        d[o:o+len(old)]=new.encode(); cnt+=1
    return bytes(d),cnt

def entry_ids(d):
    """positions of the 16-byte id of each listed texture (just before its u16 0x0001 + u16 name length + name)"""
    import re
    out=[]
    for m in re.finditer(rb'\.nut\0',d):
        e=m.end(); start=d.rfind(b'\x00\x01\x00',0,e-100)
        out.append(m)
    return out
