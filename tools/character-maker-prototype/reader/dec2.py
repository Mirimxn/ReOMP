import sys,struct,collections,re
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from schema import Schema,R
FIXED={'Char':1,'UChar':1,'Short':2,'Int':4,'Int64':8,'Float':4,'Half':2,'HalfVec3':6,'Vec3':12,'Vec4':16,'Colour3':12,'Colour4':16,'Colour32':4,'NuQuat':16,'NuGuid':16,'Mtx':64,'Ptr':8,'NuTransform':48,'NuHSpecial':16}
class Fail(Exception): pass
class Dec:
    def __init__(s,d):
        s.d=d; s.S=Schema(d); s.NT=len(s.S.types); s.ok=collections.Counter(); s.bad=collections.Counter(); s.why={}
        s.links=0; s.trace=None
    def flat(s,ci):
        out=[]
        for f in s.S.classes[ci]['fields']:
            if (f['flags']&0xC0000000)==0xC0000000 and f['ref']==0: out+=s.flat(f['type'])
            else: out.append(f)
        return out
    def is_comp(s,ci,depth=0):
        if not (0<=ci<len(s.S.classes)) or depth>30: return False
        nm=s.S.classes[ci]['name']
        if nm in ('apiComponent','apiEntity'): return True
        for f in s.S.classes[ci]['fields']:
            if (f['flags']&0xC0000000)==0xC0000000 and f['ref']==0: return s.is_comp(f['type'],depth+1)
        return False
    def kind(s,f):
        fl=f['flags']&0xffffffff; t=f['type']
        if fl & 0x80000000:
            if fl & 0x10000000: return 'link'
            if not (0<=t<len(s.S.classes)): return 'type'
            if f['ref']==-1 or s.is_comp(t): return 'link'
            cn=s.S.classes[t]['name']
            if ('Handle' in cn and cn.startswith('nttResource')) or 'ResourceHandle' in cn: return 'handle'
            return 'struct'
        if t<s.NT: return 'type'
        return 'struct'
    def field(s,r,f,depth):
        k=s.kind(f)
        if f['c']==0:
            n=r.u32()
            if n>100000: raise Fail(f"list {f['name']} count {n}")
            for _ in range(n): s.one(r,f,k,depth)
        else: s.one(r,f,k,depth)
    def one(s,r,f,k,depth):
        if k=='link': s.links+=1; return
        if k=='handle':
            r.u32(); h=r.u8()
            if h: r.p+=16; n=r.u32(); r.p+=n
            return
        if k=='object':
            if r.u8()==0: return
            ci=r.u32(); s.mobj(r,ci,depth+1); return
        if k=='struct':
            for g in s.flat(f['type']): s.field(r,g,depth+1)
            return
        tn=s.S.types[f['type']][0]
        if tn in FIXED: r.p+=FIXED[tn]; return
        if tn in ('String','Data'): n=r.u32(); r.p+=n; return
        if tn in ('ClassObject','ClassObjectRef'):
            if r.u8()==0: return
            ci=r.u32(); s.mobj(r,ci,depth+1); return
        if f['type']==23:
            r.u32(); h=r.u8()
            if h: r.p+=16; n=r.u32(); r.p+=n
            return
        raise Fail(f"type {f['type']} {tn!r} in {f['name']}")
    def mobj(s,r,ci,depth=0):
        o=r.p; sz=r.u32(); nm=r.str()
        if nm!='MOBJ': raise Fail(f'expected MOBJ got {nm[:20]!r}')
        end=o+sz; cname=s.S.classes[ci]['name']; l0=s.links
        try:
            r.u32()
            for f in s.flat(ci):
                s.field(r,f,depth)
                if r.p>end: raise Fail(f"overran in field {f['name']}")
            if r.p!=end: raise Fail(f'consumed {r.p-o} of {sz}')
            s.ok[cname]+=1
        except (Fail,struct.error,IndexError,RecursionError) as e:
            s.bad[cname]+=1; s.why.setdefault(cname,(o,str(e)[:150]))
        r.p=end
if __name__=='__main__':
    d=open(sys.argv[1],'rb').read(); D=Dec(d)
    ol=d.find(b'OLST\0')-8; olsz=struct.unpack_from('<I',d,ol)[0]
    # decode every MOBJ found by signature independently (class from its prefix)
    res=[]
    for m in re.finditer(rb'\x01(....)(....)\x05\x00\x00\x00MOBJ\x00',d[ol:ol+olsz],re.S):
        ci=struct.unpack('<I',m.group(1))[0]
        if ci>=len(D.S.classes): continue
        r=R(d,ol+m.start()+5); b0=sum(D.bad.values()); D.mobj(r,ci); res.append(sum(D.bad.values())==b0)
    print(f'objects decoded exactly (incl. everything nested inside): {sum(res)}/{len(res)}')
    for c,n in D.bad.most_common(int(sys.argv[2]) if len(sys.argv)>2 else 15): print(f'  {n:4d} {c}: {D.why[c]}')
