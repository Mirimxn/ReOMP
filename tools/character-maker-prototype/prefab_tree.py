# Generic walker for the body of *_baked files: records are [u32 size incl. itself][u32 name len][name\0][payload]
import sys,struct
def is_name(d,o,end):
    if o+8>end: return None
    sz,nl=struct.unpack('<II',d[o:o+8])
    if sz<8+nl or o+sz>end or not (2<=nl<=80): return None
    nm=d[o+8:o+8+nl]
    if nm[-1]!=0 or not all(32<=c<127 for c in nm[:-1]): return None
    return sz,nm[:-1].decode()
def walk(d,o,end,depth,out,maxdepth=12):
    while o<end:
        r=is_name(d,o,end)
        if not r:
            return o   # not a record: stop (opaque data)
        sz,nm=r; p=o+8+len(nm)+1
        out.append((depth,o,sz,nm,p,o+sz))
        if depth<maxdepth:
            walk(d,p,o+sz,depth+1,out,maxdepth)
        o+=sz
    return o
if __name__=='__main__':
    d=open(sys.argv[1],'rb').read(); h=struct.unpack('>I',d[0:4])[0]+4
    start=int(sys.argv[2]) if len(sys.argv)>2 else h
    out=[]; stop=walk(d,start,len(d),0,out)
    print('records',len(out),'stopped at',stop,'of',len(d))
    for depth,o,sz,nm,p,e in out[:int(sys.argv[3]) if len(sys.argv)>3 else 60]:
        print(f"{'  '*depth}{nm} @{o} size {sz}")
