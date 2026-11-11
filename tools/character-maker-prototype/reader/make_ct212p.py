# Builds the "CT212 Kama Trooper" mod with a pauldron made from the user's FBX in the face-texture slot.
# Earlier notes: a copy of CaribDiss with the airborne helmet, 212th body textures,
# the user's arm/hip textures, the kama kept, extra model and face texture disabled, helmet lowered.
import sys,os,struct,glob,json,re
import random as _random
# ids are drawn from a seeded generator: the shared files (textures, helmet, kama, pauldron) come out identical in
# every outfit's build, so the prefabs of different outfits can share them; each prefab then gets its own ids below.
_rng=_random.Random('ct-kama-shared'); os.urandom=lambda n: bytes(_rng.getrandbits(8) for _ in range(n))
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from schema import Schema,R,find_rec
from objtree import objtable
from dec2 import Dec
from deps import Deps
import tints as tintmod
import donorswap
DONOR_DIR='/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/ct212/212_trooper/CHARS/MINIFIG/CLONETROOPER'
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/pd')
import make_pauldron_gsc

ATT='/mnt/user-data/attachments/5d780e1f-0614-5cb0-8420-7e2215cd16ae'
T212='/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/ct212/212_trooper/CHARS'
AIR='/mnt/user-data/outputs/airborne8/CHARS'
OUT=sys.argv[1]
SLOT=sys.argv[2] if len(sys.argv)>2 else 'face'   # 'face' or 'torso' (pauldron slot), 'satchel', or 'satchel+face' (both)
HAS_SAT='satchel' in SLOT; HAS_PLD=SLOT in ('face','torso','satchel+face'); FACE_PLD=SLOT in ('face','satchel+face')
UNIT=os.environ.get('UNIT','212')   # which outfit this prefab is for: names the prefab and its roster link (OMP_CT<unit>)
PNAME=f'CT{UNIT}Kama'; assert len(PNAME)==9
def att(name): return glob.glob(f'{ATT}/*-{name}')[0]
def put(rel,data):
    p=os.path.join(OUT,rel); os.makedirs(os.path.dirname(p),exist_ok=True); open(p,'wb').write(data); return rel
def selfpath_len(d): return struct.unpack('>H',d[20:22])[0]
resources=[]

# ---------- textures ----------
TEX_TEMPLATE=open(f'{T212}/SUPER_CHARACTER_TEXTURE/LEG/LEG_CLONETROOPER_PHASE2_UTAPOA_LEFT_DIFF_DX11.texture','rb').read()
def retarget_texture(tex, folder, name):
    """new self path and new id for a game .texture; returns (bytes, id)"""
    n=selfpath_len(tex); hdr=struct.unpack('>I',tex[0:4])[0]
    sp=f'Chars/Super_Character_Texture/{folder.upper()}/{name.upper()}_DX11.texture'.encode()+b'\0'
    tid=os.urandom(16)
    out=bytearray(tex[:20]+struct.pack('>H',len(sp))+sp+tid+tex[22+n+16:])
    struct.pack_into('>I',out,0,hdr+len(sp)-n)
    c=struct.unpack('>I',out[0:4])[0]+4; r=bytes(out).find(b'RTXT',c); assert r>0
    out[r+46:r+62]=os.urandom(16)            # inner id must be unique too
    return bytes(out),tid
def texture_from_dds(dds):
    """wrap a BC7 DDS (DX10 header) into the game texture layout, using the 212 texture as the template"""
    h,w=struct.unpack_from('<II',dds,12); mips=struct.unpack_from('<I',dds,28)[0]
    assert dds[84:88]==b'DX10' and struct.unpack_from('<I',dds,128)[0]==98, 'expected BC7 (DXGI 98)'
    pix=dds[148:]
    t=bytearray(TEX_TEMPLATE); c1=struct.unpack('>I',t[0:4])[0]+4
    rt=t.find(b'RTXT',c1); assert rt>0
    tw,th,depth,tm=struct.unpack_from('>IIII',t,rt+8)
    tpl_pix_start=len(t)-(len(TEX_TEMPLATE)-0)  # placeholder
    # template pixel data = everything after the RTXT block header; its size is the BC7 size of the template mips
    def bc7size(w,h,m): return sum(max(1,(w>>i+2) if (w>>i)>=4 else 1)*max(1,(h>>i+2) if (h>>i)>=4 else 1)*16 for i in range(m))
    tsz=bc7size(tw,th,tm); start=len(t)-tsz
    assert bc7size(w,h,mips)==len(pix), (bc7size(w,h,mips),len(pix))
    out=bytearray(t[:start])+pix
    struct.pack_into('>IIII',out,rt+8,w,h,depth,mips)
    struct.pack_into('>I',out,c1,len(out)-c1-4)   # second chunk size
    return bytes(out)
def bc7check():
    t=TEX_TEMPLATE; c1=struct.unpack('>I',t[0:4])[0]+4; rt=t.find(b'RTXT',c1)
    print('template texture', struct.unpack_from('>IIII',t,rt+8), 'chunk2 size field', struct.unpack('>I',t[c1:c1+4])[0], 'expected', len(t)-c1-4)
bc7check()

NEW_TEX={}   # game path (lowercase, no _dx11) -> (file rel, id)
def add_texture(folder,name,data_bytes):
    data,tid=retarget_texture(data_bytes,folder,name)
    rel=put(f'CHARS/SUPER_CHARACTER_TEXTURE/{folder.upper()}/{name.upper()}_DX11.texture',data)
    resources.append(rel); NEW_TEX[f'chars/super_character_texture/{folder}/{name}.texture']=(rel,tid)
    return f'chars/super_character_texture/{folder}/{name}.texture'

TX={}
TX['body_front']=add_texture('body','body_ct212kama_front_diff',open(f'{T212}/SUPER_CHARACTER_TEXTURE/BODY/BODY_CLONETROOPER_PHASE2_UTAPOA_FRONT_DIFF_DX11.texture','rb').read())
TX['body_back']=add_texture('body','body_ct212kama_back_diff',open(f'{T212}/SUPER_CHARACTER_TEXTURE/BODY/BODY_CLONETROOPER_PHASE2_UTAPOA_BACK_DIFF_DX11.texture','rb').read())
TX['leg_left']=add_texture('leg','leg_ct212kama_left_diff',open(f'{T212}/SUPER_CHARACTER_TEXTURE/LEG/LEG_CLONETROOPER_PHASE2_UTAPOA_LEFT_DIFF_DX11.texture','rb').read())
TX['leg_right']=add_texture('leg','leg_ct212kama_right_diff',open(f'{T212}/SUPER_CHARACTER_TEXTURE/LEG/LEG_CLONETROOPER_PHASE2_UTAPOA_RIGHT_DIFF_DX11.texture','rb').read())
TX['arm_left']=add_texture('arm','arm_ct212kama_left_diff',texture_from_dds(open(att('ARMS_CLONE_212_LEFT_DIFF_DX11.dds'),'rb').read()))
TX['arm_right']=add_texture('arm','arm_ct212kama_right_diff',texture_from_dds(open(att('ARMS_CLONE_212_RIGHT_DIFF_DX11.dds'),'rb').read()))
TX['hip']=add_texture('hip','hip_ct212kama_diff',texture_from_dds(open(att('HIP_CT_KAMA_DIFF_DX11.dds'),'rb').read()))
TX['hat_diff']=add_texture('hat','hat_ct212kama_diff',open(att('HAT_AIRBORN_212_DIFF_DX11.texture'),'rb').read())
# helmet normal, roughness and sand (weather) maps: the user's 4K BC7 set
TX['hat_nrm']=add_texture('hat','hat_ct212kama_nrm',texture_from_dds(open(att('HAT_AIRBORNE_NRM_DX11.dds'),'rb').read()))
TX['hat_brdf']=add_texture('hat','hat_ct212kama_brdf',texture_from_dds(open(att('HAT_AIRBORNE_BRDF_DX11.dds'),'rb').read()))
TX['hat_sand']=add_texture('weather/hat','hat_ct212kama_sand',texture_from_dds(open(att('HAT_AIRBORN_SAND_DX11.dds'),'rb').read()))

# ---------- helmet model: the airborne GSC under its own name, with texture/shader files named after it ----------
HAT='hat_ct212kama'
g=bytearray(open(f'{AIR}/SUPER_CHARACTER/HAT/HAT_CL2AB_OMP_AIRBORNE2_DX11.GSC','rb').read())
n=selfpath_len(g); sp=f'CHARS/SUPER_CHARACTER/HAT/{HAT.upper()}_DX11.GSC'.encode(); assert len(sp)<n
g[22:22+n]=sp+bytes(n-len(sp)); i=22+n
while g[i]==0: i+=1
g[i:i+16]=os.urandom(16)
h=struct.unpack('>I',g[0:4])[0]+4
for old,new in [(b'hat_cl2ab_omp_airborne2.shaders',f'{HAT}.shaders'.encode()),(b'hat_cl2ab_omp_airborne2_dx11.nxg_textures',f'{HAT}_dx11.nxg_textures'.encode())]:
    o=bytes(g[:h]).find(old); assert o>0 and len(new)<=len(old)
    # the name may already be zero-padded after it: blank the old name fully
    g[o:o+len(old)]=new+bytes(len(old)-len(new))
resources.append(put(f'CHARS/SUPER_CHARACTER/HAT/{HAT.upper()}_DX11.GSC',bytes(g)))
for ext in ('NXG_TEXTURES','PC_SHADERS'):
    x=bytearray(open(f'{AIR}/SUPER_CHARACTER/HAT/HAT_CL2AB_OMP_AIRBORNE2_DX11.{ext}','rb').read()); n=selfpath_len(x)
    sp=f'CHARS/SUPER_CHARACTER/HAT/{HAT.upper()}_DX11.{ext.lower()}'.encode(); assert len(sp)<n
    x[22:22+n]=sp+bytes(n-len(sp)); i=22+n
    while x[i]==0: i+=1
    x[i:i+16]=os.urandom(16)
    resources.append(put(f'CHARS/SUPER_CHARACTER/HAT/{HAT.upper()}_DX11.{ext}',bytes(x)))

# ---------- pauldron: GSC built from the FBX, hung from the neck locator ----------
NECK=(0.0,0.2891,0.0)
PLD_DIR='chars/super_character/hat'   # the GSC's own header names this folder for its shader and texture lists
SIDE=os.environ.get('PAULDRON_SIDE','left')   # which shoulder this character wears it on
LIB='/mnt/user-data/outputs/parts/pauldron'   # both sides are kept here for other characters
def build_side(side):
    name=f'pauldron_{side[0]}'; iname='PauldronR' if side=='right' else 'PauldronL'
    files={}
    g,col=make_pauldron_gsc.build(f'{AIR}/SUPER_CHARACTER/HAT/HAT_CL2AB_OMP_AIRBORNE2_DX11.GSC',att('rigid_pauldron.fbx'),NECK,iname,
        f'{PLD_DIR.upper()}/{name.upper()}_DX11.GSC',f'{name}.shaders',f'{name}_dx11.nxg_textures','hat_cl2ab_omp_airborne2.shaders','hat_cl2ab_omp_airborne2_dx11.nxg_textures',
        mirror=(side=='left'))   # the FBX sits on the right shoulder (negative x, the RightHand side)
    files[f'{PLD_DIR.upper()}/{name.upper()}_DX11.GSC']=g
    for ext in ('NXG_TEXTURES','PC_SHADERS'):
        x=bytearray(open(f'{AIR}/SUPER_CHARACTER/HAT/HAT_CL2AB_OMP_AIRBORNE2_DX11.{ext}','rb').read()); n=selfpath_len(x)
        sp=f'{PLD_DIR.upper()}/{name.upper()}_DX11.{ext.lower()}'.encode(); assert len(sp)<n
        x[22:22+n]=sp+bytes(n-len(sp)); i=22+n
        while x[i]==0: i+=1
        x[i:i+16]=os.urandom(16)
        files[f'{PLD_DIR.upper()}/{name.upper()}_DX11.{ext}']=bytes(x)
    for rel,data in files.items():
        q=os.path.join(LIB,side,rel); os.makedirs(os.path.dirname(q),exist_ok=True); open(q,'wb').write(data)
    return name,iname,files,col
for sd in ('right','left'):
    r_=build_side(sd)
    if sd==SIDE: PLD,PLD_NAME,pfiles,col=r_
for rel,data in pfiles.items():
    if HAS_PLD: resources.append(put(rel,data))
    else: put(rel,data)   # kept in the build folder for other characters, not used by this one
# ---------- satchel: the game's shoulder bag (AdditionalModel_Satchel, skinned to the minifig skeleton) in the
# template's additional-model entity, instead of the pauldron. Shipped at its game path (same files as the game's).
SATCHEL='chars/super_character/additionalmodel/additionalmodel_satchel'
SATCHEL_RGB=(0.098,0.023,0.006)   # LEGO reddish brown, linear
if HAS_SAT:
    TX['sat_nrm']=add_texture('additionalmodel','additionalmodel_satchel_ct212_nrm',open(att('ADDITIONALMODEL_SATCHEL_NRM_DX11.TEXTURE'),'rb').read())
    TX['sat_brdf']=add_texture('additionalmodel','additionalmodel_satchel_ct212_brdf',open(att('ADDITIONALMODEL_SATCHEL_BRDF_DX11.TEXTURE'),'rb').read())
    TX['sat_sand']=add_texture('weather/additionalmodel','additionalmodel_satchel_ct212_sand',open(att('ADDITIONALMODEL_SATCHEL_SAND_DX11.TEXTURE'),'rb').read())
    for ext in ('GHG','PC_SHADERS','NXG_TEXTURES'):
        resources.append(put(f'{SATCHEL.upper()}_DX11.{ext}',open(att(f'ADDITIONALMODEL_SATCHEL_DX11.{ext}'),'rb').read()))
print('this character wears the',SIDE,'pauldron:',PLD)
def srgb(c): return round(255*(12.92*c if c<=0.0031308 else 1.055*c**(1/2.4)-0.055))
RGB=tuple(srgb(c) for c in col); print('pauldron colour',RGB)
def solid_bc7_dds(rgb,size=256):
    # BC7 mode 6 block: both endpoints = colour, every index 0 (7-bit endpoints + p-bit 1 -> odd values)
    bits=1<<6; pos=7
    for ch in (rgb[0],rgb[0],rgb[1],rgb[1],rgb[2],rgb[2],255,255):
        bits|=(ch>>1)<<pos; pos+=7
    bits|=1<<pos; bits|=1<<(pos+1)
    blk=bits.to_bytes(16,'little'); mips=size.bit_length(); pix=b''
    for m in range(mips): s_=max(1,size>>m); pix+=blk*(max(1,s_//4)**2)
    h=bytearray(148); h[0:4]=b'DDS '; struct.pack_into('<II',h,12,size,size); struct.pack_into('<I',h,28,mips)
    h[84:88]=b'DX10'; struct.pack_into('<I',h,128,98); return bytes(h)+pix
# ---------- kama: its own material has only the plain plastic layer. The game's skirt attachment is the same
# old file format, also skinned, and its front material has a printable second layer (fed by diffuse_1 in the
# prefab, like the helmet). The kama keeps its mesh, bones and LODs and gets the skirt's materials and shaders.
import nxg as nxgmod, kama_skirtmat
KAMA_OLD='skinnedskirt_fabric_kamacloth'; KAMA='skinnedskirt_fabric_kamact212'; KDIR='CHARS/SUPER_CHARACTER/SKINNEDSKIRT'
SKIRT='/mnt/user-data/uploads/LEGO Star Wars - The Skywalker Saga/CHARS/SUPER_CHARACTER/SKIRT_ATTACHMENT/SKIRT_ATTACHMENT_DX11.'
kg,_=nxgmod.rename(open(att('SKINNEDSKIRT_FABRIC_KAMACLOTH_DX11.GHG'),'rb').read(),KAMA_OLD,KAMA)
kg=kama_skirtmat.build(kg,open(SKIRT+'GHG','rb').read())
ks=kama_skirtmat.selfpath(open(SKIRT+'PC_SHADERS','rb').read(),f'{KDIR}/{KAMA.upper()}_DX11.pc_shaders')
kn=kama_skirtmat.selfpath(open(SKIRT+'NXG_TEXTURES','rb').read(),f'{KDIR}/{KAMA.upper()}_DX11.nxg_textures')
for ext,data in (('GHG',kg),('PC_SHADERS',ks),('NXG_TEXTURES',kn)): resources.append(put(f'{KDIR}/{KAMA.upper()}_DX11.{ext}',data))
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/pd'); import bc7enc
from PIL import Image
# the skirt material reads uvSet2 upside down compared with the kama's own UVs: flip the print vertically
_kimg=Image.open(att('KAMA_AIRBORN.dds')).convert('RGBA').transpose(Image.FLIP_TOP_BOTTOM)
TX['kama_diff']=add_texture('skinnedskirt','kama_ct212kama_diff',texture_from_dds(bc7enc.dds(_kimg)))
pdiff=Image.open(glob.glob(f'{ATT}/bc98a4b7-image.png')[0]).convert('RGBA')
_bg=Image.new('RGBA',pdiff.size,(0,0,0,255)); _bg.alpha_composite(pdiff); pdiff=_bg   # the gaps between the brown panels are the black lines
pnrm=Image.open(glob.glob(f'{ATT}/30904468-image.png')[0]).convert('RGBA')
TX['pld_diff']=add_texture('torsoattachment','pauldron_diff',texture_from_dds(bc7enc.dds(pdiff)))
TX['pld_nrm']=add_texture('torsoattachment','pauldron_nrm',texture_from_dds(bc7enc.dds(pnrm)))
for k in ('pld_diff','pld_nrm'):
    rel=NEW_TEX[TX[k]][0]; q=os.path.join(LIB,'textures',rel); os.makedirs(os.path.dirname(q),exist_ok=True); open(q,'wb').write(open(os.path.join(OUT,rel),'rb').read())

def patch(src,is_prefab):
    # ---------- the prefab ----------
    d=bytearray(open(src,'rb').read())
    S=Schema(bytes(d)); O,olo,olsz,tail=objtable(bytes(d),S)
    edits=[]   # (pos, old_len, new_bytes)
    donor_paths=[]; DONOR_FILE=None

    def handle_at_path(path):
        """positions of resource handles whose path is `path` (u32 2, u8 1, id16, u32 len, path\\0)"""
        out=[]; b=path.encode()+b'\0'; o=0; body=bytes(d)
        hdr_end=struct.unpack('>I',body[0:4])[0]+4
        while (o:=body.find(b,o+1))>0:
            if o<hdr_end: continue
            st=o-4-16-1-4
            if body[st:st+5]==b'\x02\x00\x00\x00\x01' and struct.unpack_from('<I',body,o-4)[0]==len(b): out.append(st)
        return out
    def handle_bytes(path,rid): b=path.encode()+b'\0'; return b'\x02\x00\x00\x00\x01'+rid+struct.pack('<I',len(b))+b
    def handle_len(pos):
        if d[pos+4]==0: return 5
        return 5+16+4+struct.unpack_from('<I',d,pos+21)[0]

    swaps={'chars/super_character_texture/body/body_caribdiss_front_diff.texture':TX['body_front'],
           'chars/super_character_texture/body/body_caribdiss_back_diff.texture':TX['body_back'],
           'chars/super_character_texture/leg/leg_caribdiss_left_diff.texture':TX['leg_left'],
           'chars/super_character_texture/leg/leg_caribdiss_right_diff.texture':TX['leg_right'],
           'chars/super_character_texture/arm/arm_caribdiss_left_diff.texture':TX['arm_left'],
           'chars/super_character_texture/arm/arm_caribdiss_right_diff.texture':TX['arm_right'],
           'chars/super_character_texture/hip/hip_caribdiss_diff.texture':TX['hip'],
           'chars/super_character_texture/hat/hat_caribdiss_norm.texture':TX['hat_nrm'],
           'chars/super_character_texture/hat/hat_caribdiss_brdf.texture':TX['hat_brdf'],
           'chars/super_character_texture/weather/hat/hat_caribdiss_sand.texture':TX['hat_sand']}
    if HAS_SAT: swaps['chars/super_character_texture/weather/torso_attachment/torsoattachment_caribdiss_sand.texture']=TX['sat_sand']
    for old,new in swaps.items():
        ps=handle_at_path(old); assert ps, old
        for p in ps: edits.append((p,handle_len(p),handle_bytes(new,NEW_TEX[new][1])))
    # helmet model: our GSC path, keep the original model id
    hat_model='chars/super_character/hat/hat_caribdiss_dx11.gsc'
    p=handle_at_path(hat_model); assert len(p)==1; p=p[0]; hat_id=bytes(d[p+5:p+21])
    edits.append((p,handle_len(p),handle_bytes(f'chars/super_character/hat/{HAT}_dx11.gsc',hat_id)))
    # helmet material: fill the empty diffuse_1 slot of the second material override, set the part name
    hatent='HAT_CaribDiss'
    tt=[x for x in O if x['path']==hatent and x['class']=='nttRenderTTExportObject'][0]
    D=Dec(bytes(d)); D.trace=[]; D.mobj(R(bytes(d),tt['o']),struct.unpack_from('<I',d,tt['o']-4)[0]); assert not D.bad
    dif=[p for n_,k,p in D.trace if n_=='diffuse_1']; assert len(dif)==2 and d[dif[1]:dif[1]+5]==b'\x02\x00\x00\x00\x00'
    edits.append((dif[1],5,handle_bytes(TX['hat_diff'],NEW_TEX[TX['hat_diff']][1])))
    sn=[p for n_,k,p in D.trace if n_=='Special Name'][0]; ln=struct.unpack_from('<I',d,sn)[0]
    print('helmet part name was',bytes(d[sn+4:sn+4+ln]))
    edits.append((sn,4+ln,struct.pack('<I',len(b'Hat_Cl2ab\0'))+b'Hat_Cl2ab\0'))

    # same-size edits first (they do not move anything)
    # lower the helmet to the Utapau trooper's height (local and world transforms of the helmet entity)
    tr=[x for x in O if x['path']==hatent and x['class']=='apiTransformComponent'][0]
    p=tr['o']+17+19
    for k in (9,9+12):   # pos.y in local NuTransform (floats 8..11) and world NuTransform
        y=struct.unpack_from('<f',d,p+4*k)[0]; struct.pack_into('<f',d,p+4*k,0.3742397129535675); print('helmet y',y,'->',struct.unpack_from('<f',d,p+4*k)[0])
    # disable the extra model and the face texture (entity and its components)
    for ent in {'face':('AdditionalModel_CaribDiss',),'torso':('FaceTex_CaribDiss',),'satchel':(),'satchel+face':()}[SLOT]:
        for x in O:
            if (x['class']=='apiEntity' and x['entity']==ent and x['path']=='') or x['path']==ent or x['path'].startswith(ent+'/'):
                if x['class'] in ('apiEntity',) or S.classes[struct.unpack_from('<I',d,x['o']-4)[0]]['fields'][:1] and D.is_comp(struct.unpack_from('<I',d,x['o']-4)[0]):
                    assert d[x['o']+17] in (0,1); d[x['o']+17]=0
        print('disabled',ent)
    def copy_handle(path):
        q=handle_at_path(path)[0]; return bytes(d[q:q+handle_len(q)])
    def set_material(rt):
        D2=Dec(bytes(d)); D2.trace=[]; D2.mobj(R(bytes(d),rt['o']),struct.unpack_from('<I',d,rt['o']-4)[0]); assert not D2.bad
        def slot(name): s_=[p for n_,k_,p in D2.trace if n_==name]; assert len(s_)==2; return s_[1]
        for sl in ('diffuse_0','diffuse_1'):   # the helmet material this GSC was built from takes its colour from diffuse_1
            edits.append((slot(sl),handle_len(slot(sl)),handle_bytes(TX['pld_diff'],NEW_TEX[TX['pld_diff']][1])))
        edits.append((slot('normal_0'),handle_len(slot('normal_0')),handle_bytes(TX['pld_nrm'],NEW_TEX[TX['pld_nrm']][1])))
        edits.append((slot('specular_0'),handle_len(slot('specular_0')),copy_handle('chars/super_character_texture/minifig/body_front_dx11_brdf.texture')))
        return D2
    fm='chars/super_character/facetex/facetex_head_dx11.gsc'
    p=handle_at_path(fm); assert len(p)==1,p; fid=bytes(d[p[0]+5:p[0]+21])
    if SLOT=='torso':
        # ---- the torso-attachment slot (skinned GHG on the Neck joint) gets the rigid pauldron GSC
        tent='AdditionalModel_CaribDiss'
        for x in O:
            if x['path']==tent and x['class']=='SkeletonComponent':
                assert d[x['o']+17]==1; d[x['o']+17]=0; print('torso slot skeleton disabled (the GSC has no skin)')
        ao=[x for x in O if x['path']==tent and x['class']=='AttachmentOffsetComponent'][0]
        DA=Dec(bytes(d)); DA.trace=[]; DA.mobj(R(bytes(d),ao['o']),struct.unpack_from('<I',d,ao['o']-4)[0])
        op=[p for n_,k_,p in DA.trace if n_=='Offset'][0]; print('torso offset',struct.unpack_from('<3f',d,op),'-> 0'); struct.pack_into('<3f',d,op,0,0,0)
        rt=[x for x in O if x['path']==tent and x['class']=='nttRenderTTExportObject'][0]
        gm='chars/super_character/additionalmodel/additionalmodel_caribdiss_dx11.ghg'
        p=[q for q in handle_at_path(gm) if rt['o']<=q<rt['end']]; assert len(p)==1; p=p[0]
        edits.append((p,handle_len(p),handle_bytes(f'{PLD_DIR}/{PLD}_dx11.gsc',fid)))
        D2=set_material(rt)
        sn=[p for n_,k_,p in D2.trace if n_=='Special Name'][0]; ln=struct.unpack_from('<I',d,sn)[0]; print('torso special name',bytes(d[sn+4:sn+4+ln]))
    if HAS_SAT:
        aent=[x for x in O if x['class']=='apiEntity' and x['entity']=='AdditionalModel_CaribDiss' and x['path']==''][0]
        gm='chars/super_character/additionalmodel/additionalmodel_caribdiss_dx11.ghg'
        ps=handle_at_path(gm); assert ps and all(aent['o']<=q<aent['end'] for q in ps),ps
        for q in ps: edits.append((q,handle_len(q),handle_bytes(f'{SATCHEL}_dx11.ghg',bytes(d[q+5:q+21]))))
        print('additional model -> satchel (render and skeleton):',len(ps))
        rt=[x for x in O if x['path']=='AdditionalModel_CaribDiss' and x['class']=='nttRenderTTExportObject'][0]
        DS=Dec(bytes(d)); DS.trace=[]; DS.mobj(R(bytes(d),rt['o']),struct.unpack_from('<I',d,rt['o']-4)[0]); assert not DS.bad
        # CaribDiss's own torso textures do not fit the bag's UVs: no diffuse (the colour comes from the tint),
        # the satchel's own normal and roughness maps
        for sl,new in (('diffuse_0',None),('normal_0',TX['sat_nrm']),('specular_0',TX['sat_brdf'])):
            q=[q for n_,k_,q in DS.trace if n_==sl][1]
            edits.append((q,handle_len(q),b'\x02\x00\x00\x00\x00' if new is None else handle_bytes(new,NEW_TEX[new][1])))
        # CaribDiss's extra model lifts the Neck joint by 1 cm (AttachmentOffsetComponent): with the satchel in that
        # entity the whole head sat too high, showing the neck. No lift.
        ao=[x for x in O if x['path']=='AdditionalModel_CaribDiss' and x['class']=='AttachmentOffsetComponent'][0]
        DA=Dec(bytes(d)); DA.trace=[]; DA.mobj(R(bytes(d),ao['o']),struct.unpack_from('<I',d,ao['o']-4)[0])
        op=[p for n_,k_,p in DA.trace if n_=='Offset'][0]; print('neck lift',struct.unpack_from('<3f',d,op),'-> 0'); struct.pack_into('<3f',d,op,0,0,0)
        satchel_rt=rt
    if SLOT=='satchel':
        # the template's face sits higher than a clone's (the helmet was lowered by the same amount above)
        fent='FaceTex_CaribDiss'
        tr=[x for x in O if x['path']==fent and x['class']=='apiTransformComponent'][0]
        p=tr['o']+17+19
        for k in (9,9+12):
            y=struct.unpack_from('<f',d,p+4*k)[0]; struct.pack_into('<f',d,p+4*k,y-(0.3844897449016571-0.3742397129535675)); print('face y',y,'->',struct.unpack_from('<f',d,p+4*k)[0])
    if FACE_PLD:
      fent='FaceTex_CaribDiss'
      for x in O:
          if x['path']==fent and x['class']=='AnimBlendTreeComponent':
              assert d[x['o']+17]==1; d[x['o']+17]=0; print('face animation disabled')
      at=[x for x in O if x['path']==fent and x['class']=='AttachToLocatorComponent'][0]
      k=bytes(d).find(b'\x0d\x00\x00\x00Face_Locator\x00',at['o'],at['end']); assert k>0
      d[k+4:k+16]=b'Neck_Locator'; print('pauldron hangs from Neck_Locator')
      tr=[x for x in O if x['path']==fent and x['class']=='apiTransformComponent'][0]
      p=tr['o']+17+19
      for k in (8,8+12):
          struct.pack_into('<3f',d,p+4*k,*NECK)
      print('pauldron entity at',NECK)
      p=handle_at_path(fm)[0]
      edits.append((p,handle_len(p),handle_bytes(f'{PLD_DIR}/{PLD}_dx11.gsc',fid)))
      rt=[x for x in O if x['path']==fent and x['class']=='nttRenderTTExportObject'][0]
      D2=Dec(bytes(d)); D2.trace=[]; D2.mobj(R(bytes(d),rt['o']),struct.unpack_from('<I',d,rt['o']-4)[0]); assert not D2.bad
      sn=[p for n_,k_,p in D2.trace if n_=='Special Name'][0]; ln=struct.unpack_from('<I',d,sn)[0]
      assert bytes(d[sn+4:sn+4+ln])==b'FaceTex_Head\0'
      edits.append((sn,4+ln,struct.pack('<I',10)+PLD_NAME.encode()+b'\0'))
      set_material(rt)

    # ---- clone trooper colours: white plastic, black hands, tan head (values copied from CloneTrooper)
    WHITE=(0.957,0.957,0.957); SKIN=(0.776,0.435,0.29); BLACK=(0.067,0.067,0.067)
    def render_of(pred):
        r=[x for x in O if x['class']=='nttRenderTTExportObject' and pred(x)]; assert len(r)==1,len(r); return r[0]
    def set_tints(rt,colours):
        for i,ce,ct,en,old in tintmod.tints(bytes(d),O,rt):
            if i in colours:
                d[ce]=1; struct.pack_into('<3f',d,ct,*colours[i])
    def model_of(x):
        D3=Dec(bytes(d)); D3.trace=[]; D3.mobj(R(bytes(d),x['o']),struct.unpack_from('<I',d,x['o']-4)[0])
        q=[q for n_,k_,q in D3.trace if n_=='Model Resource' and d[q+4]]; return bytes(d[q[0]+25:q[0]+25+struct.unpack_from('<I',d,q[0]+21)[0]-1]).decode() if q else ''
    body=render_of(lambda x: x['path']=='' and model_of(x).endswith('super_minifig_dx11.ghg'))
    set_tints(body,{1:SKIN,2:SKIN,3:WHITE,4:WHITE,5:WHITE,6:WHITE,7:BLACK,8:BLACK,9:WHITE,10:WHITE,11:WHITE})
    set_tints(render_of(lambda x: x['path']==hatent),{1:WHITE})
    if HAS_SAT: set_tints(satchel_rt,{0:SATCHEL_RGB,1:SATCHEL_RGB}); print('satchel reddish brown')
    kama=render_of(lambda x: x['path']=='SkinnedSkirt_Fabric')
    kama_ent=[x for x in O if x['class']=='apiEntity' and x['entity']=='SkinnedSkirt_Fabric' and x['path']==''][0]
    set_tints(kama,{0:(1.0,1.0,1.0),1:(1.0,1.0,1.0)})   # untinted, so the kama texture shows its own colours
    print('colours: white body and helmet, black hands, tan head; kama untinted')
    for q in handle_at_path(f'chars/super_character/skinnedskirt/{KAMA_OLD}_dx11.ghg'):
        assert kama_ent['o']<=q<kama_ent['end'], 'kama model used outside the kama entity'
        edits.append((q,handle_len(q),handle_bytes(f'chars/super_character/skinnedskirt/{KAMA}_dx11.ghg',bytes(d[q+5:q+21]))))
    print('kama model -> own copy')
    DK=Dec(bytes(d)); DK.trace=[]; DK.mobj(R(bytes(d),kama['o']),struct.unpack_from('<I',d,kama['o']-4)[0]); assert not DK.bad
    q=[q for n_,k_,q in DK.trace if n_=='Special Name'][0]; ln=struct.unpack_from('<I',d,q)[0]
    edits.append((q,4+ln,struct.pack('<I',15)+b'[All Specials]\0'))
    for q in [q for n_,k_,q in DK.trace if n_=='diffuse_1']:
        edits.append((q,handle_len(q),handle_bytes(TX['kama_diff'],NEW_TEX[TX['kama_diff']][1])))
    print('kama print -> diffuse_1, Special Name [All Specials]')
    # ---- the rifle painted black
    for x in O:
        if x['class']=='nttRenderTTExportObject' and x['path'] in ('Items/GRS1_SnareRifle','Items/GRS1_SnareRifle/Entity'):
            for i,ce,ct,en,old in tintmod.tints(bytes(d),O,x):
                if en: struct.pack_into('<3f',d,ct,*BLACK)
    print('rifle painted black')
    # ---- clone trooper voice, foley and body animations, taken from the Utapau clone
    U=open(f'{DONOR_DIR}/CLONETROOPER_PHASE2_UTAPAU'+('.PREFAB_BAKED' if is_prefab else '_CHEAPMINIFIG.SCENE_BAKED'),'rb').read()
    OU=objtable(U,Schema(U))[0]
    swaps_=donorswap.sound_pairs(bytes(d),O,U,OU,skip=('Items',))+donorswap.anim_pairs(bytes(d),O,U,OU)
    for p_,p2,a_,b_ in swaps_:
        l2=5+16+4+struct.unpack_from('<I',U,p2+21)[0]
        edits.append((p_,handle_len(p_),U[p2:p2+l2])); donor_paths.append(b_.lower())
    print('clone sounds/animations swapped:',len(swaps_))

    # own name, ids and roster link
    n=selfpath_len(d); spath=bytes(d[22:22+n])
    if is_prefab:
        assert spath.endswith(b'/CaribDiss.prefab_baked\0'), spath; spath=spath.replace(b'/CaribDiss.prefab_baked',f'/{PNAME}.prefab_baked'.encode())
    else:
        assert spath.endswith(b'/caribdiss_cheapminifig.scene_baked\0'), spath; spath=spath.replace(b'/caribdiss_cheapminifig',f'/{PNAME.lower()}_cheapminifig'.encode())
    d[22:22+n]=spath; d[22+n:38+n]=os.urandom(16)
    for old,new in [(b'/caribdiss.prefab\0',f'/{PNAME.lower()}.prefab\0'.encode()),(b'/caribdiss_character.prefab\0',f'/{PNAME.lower()}_character.prefab\0'.encode()),(b'/caribdiss_cheapminifig.scene\0',f'/{PNAME.lower()}_cheapminifig.scene\0'.encode())]:
        c=bytes(d).count(old); d[:]=bytes(d).replace(old,new); print('inner name',new.decode().strip('/\0'),'x',c)
    srcname=f'/{PNAME.lower()}.prefab\0'.encode() if is_prefab else f'/{PNAME.lower()}_cheapminifig.scene\0'.encode(); o=bytes(d).find(srcname); assert o>0; oid=bytes(d[o+len(srcname):o+len(srcname)+16]); rid=os.urandom(16)
    print('resource id occurrences',bytes(d).count(oid)); d[:]=bytes(d).replace(oid,rid)
    o=bytes(d).find(b'Characters\0');
    while o>=0:
        ln=struct.unpack_from('<I',d,o+27)[0]
        if bytes(d[o+31:o+31+ln-1])==b'CaribDiss': break
        o=bytes(d).find(b'Characters\0',o+1)
    if o>0: d[o+11:o+27]=bytes(16); d[o+31:o+31+9]=f'OMP_CT{UNIT}'.encode(); print(f'roster link -> OMP_CT{UNIT}')
    else: print('no roster link in this file')

    # size-changing edits, highest position first, growing every enclosing object and OLST
    O2,olo,olsz,_=objtable(bytes(d),Schema(bytes(d)))
    for pos,oldlen,newb in sorted(edits,key=lambda e:-e[0]):
        delta=len(newb)-oldlen
        d[pos:pos+oldlen]=newb
        if delta:
            for x in O2:
                if x['o']<pos<x['end']: struct.pack_into('<I',d,x['o'],struct.unpack_from('<I',d,x['o'])[0]+delta)
            struct.pack_into('<I',d,olo,struct.unpack_from('<I',d,olo)[0]+delta)
    print('edits applied:',len(edits))

    # header: repoint the helmet model records to the new path, add records for the new textures
    DH=Deps(bytes(d))
    import deps as depsmod
    def rec_bytes(kind,flags,rid,reftype=2):
        return struct.pack('>7I',kind,0,0,flags,0,0,0xffffffff)+rid+struct.pack('>I',reftype)+bytes(16)
    entries=[(p,rec_bytes(36,0xffffffff,NEW_TEX[p][1])) for p in NEW_TEX]
    #dd,added=depsmod.add_dependencies(bytes(d),entries+[(f'chars/super_character/hat/{HAT}_dx11.gsc',None),(f'chars/super_character/hat/{HAT}_nxg.gsc',None)]) if False else (None,None)
    # add tree nodes for the gsc paths via a dummy record, then repoint the original hat records to them
    gsc_new=f'chars/super_character/hat/{HAT}_dx11.gsc'; nxg_new=f'chars/super_character/hat/{HAT}_nxg.gsc'
    old_dx=[k for k in range(DH.nrec) if DH.path(DH.rec(k)['node'])=='chars/super_character/hat/hat_caribdiss_dx11.gsc']
    old_nx=[k for k in range(DH.nrec) if DH.path(DH.rec(k)['node'])=='chars/super_character/hat/hat_caribdiss_nxg.gsc']
    assert len(old_dx)==1 and len(old_nx)==1
    entries+= [(gsc_new,DH.recs[old_dx[0]]),(nxg_new,DH.recs[old_nx[0]])]
    f_dx=[k for k in range(DH.nrec) if DH.path(DH.rec(k)['node'])=='chars/super_character/facetex/facetex_head_dx11.gsc']
    f_nx=[k for k in range(DH.nrec) if DH.path(DH.rec(k)['node'])=='chars/super_character/facetex/facetex_head_nxg.gsc']
    assert len(f_dx)==1 and len(f_nx)==1
    if HAS_PLD: entries+= [(f'{PLD_DIR}/{PLD}_dx11.gsc',DH.recs[f_dx[0]]),(f'{PLD_DIR}/{PLD}_nxg.gsc',DH.recs[f_nx[0]])]
    if HAS_SAT:
        a_dx=[k for k in range(DH.nrec) if DH.path(DH.rec(k)['node'])=='chars/super_character/additionalmodel/additionalmodel_caribdiss_dx11.ghg']
        a_nx=[k for k in range(DH.nrec) if DH.path(DH.rec(k)['node'])=='chars/super_character/additionalmodel/additionalmodel_caribdiss_nxg.ghg']
        assert len(a_dx)==1 and len(a_nx)==1
        entries+= [(f'{SATCHEL}_dx11.ghg',DH.recs[a_dx[0]]),(f'{SATCHEL}_nxg.ghg',DH.recs[a_nx[0]])]
        old_dx=old_dx+a_dx; old_nx=old_nx+a_nx
    if FACE_PLD: old_dx=old_dx+f_dx; old_nx=old_nx+f_nx
    k_dx=[k for k in range(DH.nrec) if DH.path(DH.rec(k)['node'])==f'chars/super_character/skinnedskirt/{KAMA_OLD}_dx11.ghg']
    k_nx=[k for k in range(DH.nrec) if DH.path(DH.rec(k)['node'])==f'chars/super_character/skinnedskirt/{KAMA_OLD}_nxg.ghg']
    assert len(k_dx)==1 and len(k_nx)==1
    entries+= [(f'chars/super_character/skinnedskirt/{KAMA}_dx11.ghg',DH.recs[k_dx[0]]),(f'chars/super_character/skinnedskirt/{KAMA}_nxg.ghg',DH.recs[k_nx[0]])]
    old_dx=old_dx+k_dx; old_nx=old_nx+k_nx
    if donor_paths:
        DU=Deps(U); have={DH.path(DH.rec(k)['node']).lower() for k in range(DH.nrec)}
        byp={}
        for k in range(DU.nrec): byp.setdefault(DU.path(DU.rec(k)['node']).lower(),[]).append(DU.recs[k])
        added_=0
        for pth in sorted(set(donor_paths)):
            if pth in have: continue
            for rec in byp.get(pth,[]): entries.append((pth,rec)); added_+=1
        print('load list: donor records added',added_)   # the face model is no longer used; in torso mode the disabled face keeps its records
    out,added=depsmod.add_dependencies(bytes(d),entries)
    # drop the two original helmet records now that copies point at the new paths
    N=Deps(out); drop=set(old_dx+old_nx); keep=[k for k in range(N.nrec) if k not in drop]
    recs=b''.join(N.recs[k] for k in keep)
    out=bytearray(out[:N.rec_at-4])+struct.pack('>I',len(keep))+recs+out[N.after:]
    struct.pack_into('>I',out,0,struct.unpack('>I',out[0:4])[0]-64*len(drop))
    print('header: tree entries added',len(added),'records',N.nrec,'->',len(keep))
    return bytes(out)

_rng.seed('ct-kama-prefab-'+UNIT)
out=patch(glob.glob(f'{ATT}/*-CARIBDISS.PREFAB_BAKED')[0],True)
# clone troopers' voices go through a radio filter: the game applies it when the helmet entity has a
# SpeechModifier component (CaribDiss has none). Copy the Utapau clone's onto our helmet entity.
import add_component
out=add_component.add(open(f'{DONOR_DIR}/CLONETROOPER_PHASE2_UTAPAU.PREFAB_BAKED','rb').read(),out,'SpeechModifier','HAT_CaribDiss','AttachToLocatorComponent',fresh_guid_at=23)
put(f'CHARS/MINIFIG/CARIBDISS/{PNAME.upper()}.PREFAB_BAKED',out)
scene=patch(glob.glob(f'{ATT}/*-CARIBDISS_CHEAPMINIFIG.SCENE_BAKED')[0],False)
put(f'CHARS/MINIFIG/CARIBDISS/{PNAME.upper()}_CHEAPMINIFIG.SCENE_BAKED',scene)
resources[0:0]=[f'CHARS/MINIFIG/CARIBDISS/{PNAME.upper()}.PREFAB_BAKED']   # the preview scene is served by v1, not registered
json.dump({'resources':resources},open(os.path.join(OUT,'resources.json'),'w'),indent=1)
print('files:',len(resources))
