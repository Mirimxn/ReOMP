# Gives the kama (skinned, old-format GHG, LTMU v0x17e) the printable material of the game's skirt attachment
# (same old format, also skinned). The kama keeps its own mesh, bones and LODs:
#  - its two textured vertex lists get the uvSet2 field (float4 = uv0,uv0) the skirt material reads;
#  - its LTMU becomes the skirt's front material (+ variant + texture block) and shadow material (+ variant),
#    renumbered to the kama's object ids, so the material order (main, main variant, shadow, shadow variant) stays;
#  - ATEM (template texture list) becomes the skirt's.
# Ship it with the skirt's PC_SHADERS and NXG_TEXTURES under the kama's names.
import struct,re,os,sys
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
import gsc

def be(d,p): return struct.unpack_from('>I',d,p)[0]

def _add_uv2(d,L):
    """bytes of list L rebuilt with uvSet2 (var 7, float4) at offset 28, later fields moved by 16"""
    dc=len(L['defs']); hdr=L['data']-6-3*dc-4-8-8-4
    defs=[];
    for var,typ,off in L['defs']:
        defs.append((var,typ,off+16 if off>=28 else off))
        if var==5: defs.append((7,4,28))
    assert any(v==5 for v,_,_ in L['defs']) and L['defs'][4]==(5,6,20)
    out=bytearray(d[hdr:hdr+4+8])                                  # m, unk, count
    out+=b'DXTV'+struct.pack('>II',0xA9,len(defs))+b''.join(bytes(x) for x in defs)+d[L['data']-6:L['data']]
    st=L['stride']
    for i in range(L['count']):
        v=d[L['data']+i*st:L['data']+(i+1)*st]
        u0,v0=struct.unpack_from('<2e',v,20)
        out+=v[:28]+struct.pack('<4f',u0,v0,u0,v0)+v[28:]
    end=L['data']+st*L['count']
    return hdr,end,bytes(out)

def _ltmu(d):
    a=d.find(b'LTMU',1000); b=d.find(b'SUPC',a); return a,b

def _rec_starts(L):
    """record start offsets: the material name's u16 length sits 1266 bytes into each record (v0x17e)"""
    out=[]
    for m in re.finditer(rb'\x00\x00\x00\x01\x00\x00',L):
        pass
    return out

def build(kama,skirt):
    K=bytes(kama); S=bytes(skirt)
    kp,kr,_=gsc.parse(K)
    # 1) vertex lists 4 and 11 (the textured skinned and posed lists)
    segs=[_add_uv2(K,kr[4]),_add_uv2(K,kr[11])]
    # 2) materials
    ka,kb=_ltmu(K); sa,sb=_ltmu(S)
    KL=K[ka:kb]; SL=S[sa:sb]
    assert be(KL,4)==0x17e and be(SL,4)==0x17e and be(KL,8)==4 and be(SL,8)==6
    def names(L):
        return [m.start()-2 for m in re.finditer(rb'(?<=\x00\x00)[\x0b-\x40](AdditionalModel_KamaCloth:[A-Za-z_]+|Skirt_[A-Z]+|LEGO_ShadowImpostor)(:VARIANT_AUTO)?\x00',L)]
    P=1266   # bytes from record start to its name's u16 length (rec 0: name length at 12+1266)
    kn=[m.start()-1 for m in re.finditer(rb'(AdditionalModel_KamaCloth:KamaCloth_MAT|AdditionalModel_KamaCloth:LEGO_ShadowImpostor)(:VARIANT_AUTO)?\x00',KL)]
    sn=[m.start()-1 for m in re.finditer(rb'(Skirt_FRONT|Skirt_BACK|LEGO_ShadowImpostor)(:VARIANT_AUTO)?\x00',SL)]
    assert len(kn)==4 and len(sn)==6,(kn,sn)
    kst=[n-1-P for n in kn]; sst=[n-1-P for n in sn]   # n-1: the u16 length's first byte
    assert kst[0]==12 and sst[0]==12,(kst,sst)
    # tail: the list of variant DXTV refs at the end
    def tail(L,cnt):
        t=L.rfind(b'\x5a\x8d\xb5\x92'); return t-8*cnt
    kt=tail(KL,2); st_=tail(SL,3)
    assert KL[kt:kt+2]==b'\xc0\x00' and SL[st_:st_+2]==b'\xc0\x00'
    front=SL[sst[0]:sst[2]]          # Skirt_FRONT, its variant and their texture block
    shadow=SL[sst[4]:st_]            # shadow impostor and its variant (+ whatever precedes the tail)
    kshadow=KL[kst[2]:kt]
    # object ids: kama material DXTVs are kbase..kbase+3, skirt's sbase..sbase+5
    kref=sorted(set(struct.unpack('>H',KL[m.start()+2:m.start()+4])[0] for m in re.finditer(b'\xc0\x00',KL)))
    sref=sorted(set(struct.unpack('>H',SL[m.start()+2:m.start()+4])[0] for m in re.finditer(b'\xc0\x00',SL)))
    kbase,sbase=kref[0],sref[0]; assert kref==list(range(kbase,kbase+4)) and sref==list(range(sbase,sbase+6)),(kref,sref)
    def renum(blob,mp):
        b=bytearray(blob); n=0
        for m in re.finditer(b'\xc0\x00',blob):
            v=struct.unpack('>H',blob[m.start()+2:m.start()+4])[0]
            if v in mp: b[m.start()+2:m.start()+4]=struct.pack('>H',mp[v]); n+=1
        return bytes(b),n
    front,n1=renum(front,{sbase:kbase}); shadow,n2=renum(shadow,{sbase+4:kbase+2})
    assert n1==1 and n2==1,(n1,n2)
    NL=KL[:12]+front+shadow+KL[kt:]
    print('kama material: skirt front + shadow materials',len(KL),'->',len(NL),'bytes; refs',kbase,'..',kbase+3)
    segs.append((ka,kb,NL))
    # 3) ATEM
    ka2=K.rfind(b'ATEM'); sa2=S.rfind(b'ATEM'); segs.append((ka2,len(K),S[sa2:]))
    segs.sort(); out=bytearray(); p=0
    for s,e,data in segs: out+=K[p:s]+data; p=e
    out+=K[p:]
    h=be(out,0); assert be(K,h+4)==len(K)-h-8-128
    struct.pack_into('>I',out,h+4,len(out)-h-8-128)
    # check
    P2,R2,_=gsc.parse(bytes(out))
    assert [x['defs'] for x in (R2[4],R2[11])]==[gsc.parse(S)[1][4]['defs'],gsc.parse(S)[1][13]['defs']]
    for a_,b_ in zip(kp,P2): assert (a_['icnt'],a_['vcnt'],a_['ioff'],a_['voff'])==(b_['icnt'],b_['vcnt'],b_['ioff'],b_['voff'])
    return bytes(out)

def selfpath(d,new,fresh_id=True):
    h=be(d,0); n=struct.unpack('>H',d[20:22])[0]; sp=new.encode()+b'\0'
    rid=os.urandom(16) if fresh_id else d[22+n:22+n+16]
    body=d[4:20]+struct.pack('>H',len(sp))+sp+rid+d[22+n+16:4+h]
    return struct.pack('>I',len(body))+body+d[4+h:]
