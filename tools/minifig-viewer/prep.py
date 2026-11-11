import sys,struct,base64,io,json,numpy as np
sys.path.insert(0,'../pd'); import gsc
from PIL import Image
A='/mnt/user-data/attachments/5d780e1f-0614-5cb0-8420-7e2215cd16ae/'
B='/mnt/user-data/outputs/ct212kama25/'
def b64(a): return base64.b64encode(a.tobytes() if hasattr(a,'tobytes') else a).decode()
# ---- body: the standard minifig's highest-detail pieces
d=open(A+'b75357a0-SUPER_MINIFIG_DX11.GHG','rb').read(); p,r,e=gsc.parse(d); L=r[4]; I=r[5]
SLOTS=[(0,'head'),(3,'hands'),(4,'arm_l'),(5,'arm_r'),(6,'body_front'),(7,'body_back'),(10,'waist'),(12,'hips'),(11,'leg_r'),(13,'leg_l'),(15,'neck')]
parts=[]
for i,slot in SLOTS:
    P=p[i]; pos=[];uv=[];uv0=[]
    for j in range(P['voff'],P['voff']+P['vcnt']):
        o=L['data']+j*L['stride']; pos+=struct.unpack_from('<3e',d,o); q4=struct.unpack_from('<4e',d,o+20); uv+=q4[2:4]; uv0+=q4[0:2]
    ix=np.frombuffer(d,'<u2',P['icnt'],I['data']+2*P['ioff'])
    parts.append({'slot':slot,'pos':b64(np.array(pos,np.float32)),'uv':b64(np.array(uv,np.float32)),'uv0':b64(np.array(uv0,np.float32)),'idx':b64(ix.astype(np.uint16))})
# ---- sample textures (the 212th airborne trooper), shrunk to 512 px PNG
def game_tex(path):
    t=open(path,'rb').read(); c1=struct.unpack('>I',t[0:4])[0]+4; rt=t.find(b'RTXT',c1)
    w,h,_,mips=struct.unpack_from('>IIII',t,rt+8); code=struct.unpack_from('>I',t,rt+28)[0]
    bs=8 if code==1 else 16; sz=sum(max(1,(w>>m)//4)*max(1,(h>>m)//4)*bs for m in range(mips))
    mip0=t[len(t)-sz:len(t)-sz+max(1,w//4)*max(1,h//4)*bs]
    hdr=bytearray(148); hdr[0:4]=b'DDS '; struct.pack_into('<IIIII',hdr,4,124,0x1007,h,w,0); struct.pack_into('<I',hdr,76,32); struct.pack_into('<I',hdr,80,4)
    hdr[84:88]=b'DX10'; struct.pack_into('<IIIII',hdr,128,71 if code==1 else 98,3,0,1,0)
    return Image.open(io.BytesIO(bytes(hdr)+mip0)).convert('RGBA')
def png(im,size=512):
    im=im.resize((size,size),Image.LANCZOS); bio=io.BytesIO(); im.save(bio,'PNG',optimize=True); return 'data:image/png;base64,'+base64.b64encode(bio.getvalue()).decode()
T=B+'CHARS/SUPER_CHARACTER_TEXTURE/'
tex={'body_front':png(game_tex(T+'BODY/BODY_CT212KAMA_FRONT_DIFF_DX11.texture')),
     'body_back':png(game_tex(T+'BODY/BODY_CT212KAMA_BACK_DIFF_DX11.texture')),
     'arm_l':png(game_tex(T+'ARM/ARM_CT212KAMA_LEFT_DIFF_DX11.texture')),'arm_r':png(game_tex(T+'ARM/ARM_CT212KAMA_RIGHT_DIFF_DX11.texture')),
     'leg_l':png(game_tex(T+'LEG/LEG_CT212KAMA_LEFT_DIFF_DX11.texture')),'leg_r':png(game_tex(T+'LEG/LEG_CT212KAMA_RIGHT_DIFF_DX11.texture')),
     'hips':png(game_tex(T+'HIP/HIP_CT212KAMA_DIFF_DX11.texture')),
     'helmet':png(game_tex(T+'HAT/HAT_CT212KAMA_DIFF_DX11.texture')),
     'kama':png(Image.open(A+'36a78509-KAMA_AIRBORN.dds').convert('RGBA')),
     'helmet_nrm':png(game_tex(T+'HAT/HAT_CT212KAMA_NRM_DX11.texture')),'helmet_brdf':png(game_tex(T+'HAT/HAT_CT212KAMA_BRDF_DX11.texture')),
     'satchel_nrm':png(game_tex(A+'3d6e21d6-ADDITIONALMODEL_SATCHEL_NRM_DX11.TEXTURE')),'satchel_brdf':png(game_tex(A+'3dbf068b-ADDITIONALMODEL_SATCHEL_BRDF_DX11.TEXTURE'),256)}
models={'helmet':b64(open(B+'CHARS/SUPER_CHARACTER/HAT/HAT_CT212KAMA_DX11.GSC','rb').read()),
        'satchel':b64(open(A+'8ce82ed0-ADDITIONALMODEL_SATCHEL_DX11.GHG','rb').read()),
        'kama':b64(open(B+'CHARS/SUPER_CHARACTER/SKINNEDSKIRT/SKINNEDSKIRT_FABRIC_KAMACT212_DX11.GHG','rb').read())}
open('assets.js','w').write('const BODY='+json.dumps(parts)+';\nconst SAMPLE_TEX='+json.dumps(tex)+';\nconst SAMPLE_MODELS='+json.dumps(models)+';\n')
import os; print(os.path.getsize('assets.js')/1e6,'MB', {k:len(v)//1000 for k,v in tex.items()})
