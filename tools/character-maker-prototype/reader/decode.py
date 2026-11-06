import sys,struct,collections
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from schema import Schema,R
FIXED={'Char':1,'UChar':1,'Short':2,'Int':4,'Int64':8,'Float':4,'Half':2,'HalfVec3':6,'Vec3':12,'Vec4':16,'Colour3':12,'Colour4':16,'Colour32':4,'NuQuat':16,'NuGuid':16,'Mtx':64,'Ptr':8}
class Fail(Exception): pass
class Dec:
    def __init__(s,d):
        s.d=d; s.S=Schema(d); s.stats=collections.Counter(); s.fails=collections.Counter(); s.first_fail={}
    def fields(s,ci):
        out=[]
        for f in s.S.classes[ci]['fields']:
            if (f['flags']&0xC0000000)==0xC0000000 and f['ref']==0: out+=s.fields(f['type'])
            else: out.append(f)
        return out
    def value(s,r,f,end):
        T=s.S.types; t=f['type']
        tn=T[t][0] if t<len(T) else None
        if tn in FIXED: r.p+=FIXED[tn]; return
        if tn in ('String','Data'): n=r.u32(); r.p+=n; return
        if tn=='NuTransform': r.p+=48; return
        if tn=='NuHSpecial': r.p+=16; return
        if tn=='ClassObject' or tn=='ClassObjectRef':
            s.classobj(r,end); return
        if t==23:   # graph / resource reference
            r.u32(); has=r.u8()
            if has: r.p+=16
            n=r.u32(); r.p+=n; return
        if t>=len(T) and t<len(s.S.classes):   # embedded struct of another class
            for g in s.fields(t):
                if (g['flags'] & 0xF0000000)==0x90000000: continue
                if g['c']==0:
                    n=r.u32()
                    for _ in range(n): s.value(r,g,end)
                else: s.value(r,g,end)
            return
        raise Fail(f'type {t} {tn!r} field {f["name"]}')
    def classobj(s,r,end):
        present=r.u8()
        if present==0: return
        ci=r.u32(); s.mobj(r,ci)
    def mobj(s,r,ci):
        o=r.p; sz=r.u32(); nm=r.str()
        if nm!='MOBJ': raise Fail(f'expected MOBJ got {nm!r} at {o}')
        end=o+sz; name=s.S.classes[ci]['name'] if 0<=ci<len(s.S.classes) else str(ci)
        try:
            r.u32()
            for f in s.fields(ci):
                if (f['flags'] & 0xF0000000)==0x90000000: continue   # runtime-only field, not stored
                if f['c']==0:
                    n=r.u32()
                    for _ in range(n): s.value(r,f,end)
                else: s.value(r,f,end)
            if r.p!=end: raise Fail(f'{name}: consumed {r.p-o} of {sz}')
            s.stats[name]+=1
        except (Fail,struct.error,IndexError) as e:
            s.fails[name]+=1; s.first_fail.setdefault(name,(o,str(e)))
        r.p=end
if __name__=='__main__':
    d=open(sys.argv[1],'rb').read(); D=Dec(d)
    # objects directly under RootNode: [u8 1][u32 class][MOBJ]...
    o=d.find(b'RootNode\0')+9; r=R(d,o); print('RootNode head', d[o:o+11].hex(' '))
    r.p=o+11
    rootend=o-17+struct.unpack_from('<I',d,o-17)[0]
    n=0
    while r.p<rootend-5 and d[r.p]==1:
        r.u8(); ci=r.u32(); D.mobj(r,ci); n+=1
    print('top objects',n,'stopped at',r.p,'root end',rootend)
    print('ok',sum(D.stats.values()),'fail',sum(D.fails.values()))
    print('fails',D.fails.most_common(12))
    for k,v in list(D.first_fail.items())[:14]: print('  ',k,v[0],v[1][:160])
