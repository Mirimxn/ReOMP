import struct
class R:
    def __init__(s,d,p=0): s.d=d; s.p=p
    def u8(s): v=s.d[s.p]; s.p+=1; return v
    def u16(s): v=struct.unpack_from('<H',s.d,s.p)[0]; s.p+=2; return v
    def u32(s): v=struct.unpack_from('<I',s.d,s.p)[0]; s.p+=4; return v
    def i32(s): v=struct.unpack_from('<i',s.d,s.p)[0]; s.p+=4; return v
    def f32(s): v=struct.unpack_from('<f',s.d,s.p)[0]; s.p+=4; return v
    def str(s): n=s.u32(); v=s.d[s.p:s.p+n]; s.p+=n; return v.rstrip(b'\0').decode('latin1')
    def take(s,n): v=s.d[s.p:s.p+n]; s.p+=n; return v
    def rec(s):  # returns (name, payload_start, end)
        o=s.p; sz=s.u32(); nm=s.str(); return nm, s.p, o+sz
def find_rec(d,name,start=0):
    o=d.find(struct.pack('<I',len(name)+1)+name.encode()+b'\0',start)
    return o-4
class Schema:
    def __init__(s,d):
        o=find_rec(d,'TypeList'); r=R(d,o); nm,p,e=r.rec(); assert nm=='TypeList'
        s.types=[]; r.p=p; count=r.u32()
        for i in range(count):
            nm,p2,e2=r.rec(); assert nm=='Type'; tn=r.str(); size=r.i32(); s.types.append((tn,size)); r.p=e2
        o=find_rec(d,'ClassList',e); r=R(d,o); nm,p,e=r.rec(); assert nm=='ClassList'
        r.p=p; count=r.u32(); s.classes=[]
        for i in range(count):
            nm,p2,e2=r.rec(); assert nm=='Class',nm
            cname=r.str(); cls={'name':cname,'fields':[],'version':None}
            while r.p<e2:
                fn,p3,e3=r.rec()
                if fn=='Version': cls['version']=r.f32()
                elif fn=='Types':
                    r.u8(); nf=r.u32()
                    for k in range(nf):
                        t=r.u32(); n=r.str(); vals=[r.i32() for _ in range(5)]
                        cls['fields'].append({'type':t,'name':n,'ref':vals[0],'a':vals[1],'flags':vals[2],'b':vals[3],'c':vals[4]})
                    assert r.p==e3,(cname,r.p,e3)
                r.p=e3
            s.classes.append(cls); r.p=e2
        s.end=e
if __name__=='__main__':
    import sys
    d=open(sys.argv[1],'rb').read(); S=Schema(d)
    print('types',len(S.types),S.types)
    print('classes',len(S.classes))
    for c in S.classes[:3]+[x for x in S.classes if 'Hat' in x['name'] or 'Attach' in x['name']][:6]:
        print(c['name'],c['version'],[(f['name'],S.types[f['type']][0] if f['type']<len(S.types) else f['type'],f['ref'],hex(f['flags'])) for f in c['fields']])
