# Builds the "CT212 Kama Trooper" mod: a copy of CaribDiss with the airborne helmet, 212th body textures,
# the user's arm/hip textures, the kama kept, extra model and face texture disabled, helmet lowered.
import sys,os,struct,glob,json,re
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from schema import Schema,R,find_rec
from objtree import objtable
from dec2 import Dec
from deps import Deps

ATT='/mnt/user-data/attachments/5d780e1f-0614-5cb0-8420-7e2215cd16ae'
T212='/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/ct212/212_trooper/CHARS'
AIR='/mnt/user-data/outputs/airborne8/CHARS'
OUT=sys.argv[1]
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
TX['hat_nrm']=add_texture('hat','hat_ct212kama_nrm',open(att('HAT_AIRBORN_NRM_DX11.texture'),'rb').read())
TX['hat_brdf']=add_texture('hat','hat_ct212kama_brdf',open(att('HAT_AIRBORN_BDRF_DX11.texture'),'rb').read())

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

# ---------- the prefab ----------
d=bytearray(open(glob.glob(f'{ATT}/*-CARIBDISS.PREFAB_BAKED')[0],'rb').read())
S=Schema(bytes(d)); O,olo,olsz,tail=objtable(bytes(d),S)
edits=[]   # (pos, old_len, new_bytes)

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
       'chars/super_character_texture/hat/hat_caribdiss_brdf.texture':TX['hat_brdf']}
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
for ent in ('AdditionalModel_CaribDiss','FaceTex_CaribDiss'):
    for x in O:
        if (x['class']=='apiEntity' and x['entity']==ent and x['path']=='') or x['path']==ent or x['path'].startswith(ent+'/'):
            if x['class'] in ('apiEntity',) or S.classes[struct.unpack_from('<I',d,x['o']-4)[0]]['fields'][:1] and D.is_comp(struct.unpack_from('<I',d,x['o']-4)[0]):
                assert d[x['o']+17] in (0,1); d[x['o']+17]=0
    print('disabled',ent)
# own name, ids and roster link
n=selfpath_len(d); spath=bytes(d[22:22+n]); assert spath.endswith(b'/CaribDiss.prefab_baked\0'), spath
d[22:22+n]=spath.replace(b'/CaribDiss.prefab_baked',b'/CT212Kama.prefab_baked'); d[22+n:38+n]=os.urandom(16)
for old,new in [(b'/caribdiss.prefab\0',b'/ct212kama.prefab\0'),(b'/caribdiss_character.prefab\0',b'/ct212kama_character.prefab\0'),(b'/caribdiss_cheapminifig.scene\0',b'/ct212kama_cheapminifig.scene\0')]:
    c=bytes(d).count(old); d[:]=bytes(d).replace(old,new); print('inner name',new.decode().strip('/\0'),'x',c)
src=b'/ct212kama.prefab\0'; o=bytes(d).find(src); assert o>0; oid=bytes(d[o+len(src):o+len(src)+16]); rid=os.urandom(16)
print('resource id occurrences',bytes(d).count(oid)); d[:]=bytes(d).replace(oid,rid)
o=bytes(d).find(b'Characters\0');
while o>=0:
    ln=struct.unpack_from('<I',d,o+27)[0]
    if bytes(d[o+31:o+31+ln-1])==b'CaribDiss': break
    o=bytes(d).find(b'Characters\0',o+1)
assert o>0; d[o+11:o+27]=bytes(16); d[o+31:o+31+9]=b'OMP_CT212'; print('roster link -> OMP_CT212')

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
dd,added=depsmod.add_dependencies(bytes(d),entries+[(f'chars/super_character/hat/{HAT}_dx11.gsc',None),(f'chars/super_character/hat/{HAT}_nxg.gsc',None)]) if False else (None,None)
# add tree nodes for the gsc paths via a dummy record, then repoint the original hat records to them
gsc_new=f'chars/super_character/hat/{HAT}_dx11.gsc'; nxg_new=f'chars/super_character/hat/{HAT}_nxg.gsc'
old_dx=[k for k in range(DH.nrec) if DH.path(DH.rec(k)['node'])=='chars/super_character/hat/hat_caribdiss_dx11.gsc']
old_nx=[k for k in range(DH.nrec) if DH.path(DH.rec(k)['node'])=='chars/super_character/hat/hat_caribdiss_nxg.gsc']
assert len(old_dx)==1 and len(old_nx)==1
entries+= [(gsc_new,DH.recs[old_dx[0]]),(nxg_new,DH.recs[old_nx[0]])]
out,added=depsmod.add_dependencies(bytes(d),entries)
# drop the two original helmet records now that copies point at the new paths
N=Deps(out); keep=[k for k in range(N.nrec) if k not in (old_dx[0],old_nx[0])]
recs=b''.join(N.recs[k] for k in keep)
out=bytearray(out[:N.rec_at-4])+struct.pack('>I',len(keep))+recs+out[N.after:]
struct.pack_into('>I',out,0,struct.unpack('>I',out[0:4])[0]-64*2)
print('header: tree entries added',len(added),'records',N.nrec,'->',len(keep))
put('CHARS/MINIFIG/CARIBDISS/CT212KAMA.PREFAB_BAKED',bytes(out)); resources.insert(0,'CHARS/MINIFIG/CARIBDISS/CT212KAMA.PREFAB_BAKED')
json.dump({'resources':resources},open(os.path.join(OUT,'resources.json'),'w'),indent=1)
print('files:',len(resources))
