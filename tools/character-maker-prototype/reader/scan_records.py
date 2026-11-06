import sys,struct,collections
def valid(d,o,end):
    if o+9>end: return None
    sz,nl=struct.unpack_from('<II',d,o)
    if not (2<=nl<=64) or sz<8+nl or o+sz>end: return None
    nm=d[o+8:o+8+nl]
    if nm[-1]!=0 or not all(48<=c<123 or c in (32,95) for c in nm[:-1]) or not (65<=nm[0]<=90 or 97<=nm[0]<=122): return None
    return sz,nm[:-1].decode()
def scan(d,o,end,depth,out,maxdepth):
    p=o
    while p<end-8:
        r=valid(d,p,end)
        if r:
            sz,nm=r; out.append((depth,p,sz,nm))
            if depth<maxdepth: scan(d,p+8+len(nm)+1,p+sz,depth+1,out,maxdepth)
            p+=sz
        else: p+=1
if __name__=='__main__':
    d=open(sys.argv[1],'rb').read(); a,b=int(sys.argv[2]),int(sys.argv[3]); md=int(sys.argv[4]); lim=int(sys.argv[5])
    out=[]; scan(d,a,b,0,out,md)
    for depth,p,sz,nm in out[:lim]: print('  '*depth+f'{nm} @{p} +{sz}')
    print('total',len(out),collections.Counter(x[3] for x in out).most_common(25))
