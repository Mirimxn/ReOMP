# Header dependency list of *_baked files: name tree (BrickVault-style nodes) + 64-byte records
import struct
class Deps:
    def __init__(s,d):
        s.d=d; n=struct.unpack('>H',d[20:22])[0]; s.base=22+n+16
        s.one,s.two,s.nfiles,s.nnodes,s.bloblen=struct.unpack('>5I',d[s.base:s.base+20])
        s.blob_at=s.base+20; s.blob=d[s.blob_at:s.blob_at+s.bloblen]
        s.nodes_at=s.blob_at+s.bloblen
        s.nodes=[list(struct.unpack('>HHiHH',d[s.nodes_at+i*12:s.nodes_at+i*12+12])) for i in range(s.nnodes)]
        p=s.nodes_at+12*s.nnodes
        s.mid=d[p:p+8]; s.nrec_at=p+8; s.nrec=struct.unpack('>I',d[p+8:p+12])[0] if False else None
        # find record count: the u32 right before the first record
        s.pre=d[p:p+12]
        s.nrec=struct.unpack('>I',d[p+8:p+12])[0]; s.rec_at=p+12
        s.recs=[d[s.rec_at+64*i:s.rec_at+64*i+64] for i in range(s.nrec)]
        s.after=s.rec_at+64*s.nrec
        s.hdr_end=struct.unpack('>I',d[0:4])[0]+4
    def name(s,i):
        off=s.nodes[i][2]
        if off<0: return ''
        e=s.blob.find(b'\0',off); return s.blob[off:e].decode()
    def path(s,i):
        parts=[]; seen=set()
        while i and i not in seen:
            seen.add(i); parts.append(s.name(i)); i=s.nodes[i][3]
        return '/'.join(reversed(parts))
    def rec(s,k):
        r=s.recs[k]; f=struct.unpack('>8I',r[:32])
        return {'w':f,'node':f[1],'id':r[28:44],'tail':r[44:64].hex(),'raw':r}
if __name__=='__main__':
    import sys,collections
    d=open(sys.argv[1],'rb').read(); D=Deps(d)
    print('files',D.nfiles,'nodes',D.nnodes,'pre-records',D.pre.hex(' '),'records',D.nrec,'after records at',D.after,'header end',D.hdr_end)
    for i in (434,435,438): print(i,D.nodes[i],D.path(i))
    c=collections.Counter()
    for k in range(D.nrec):
        r=D.rec(k); p=D.path(r['node']); c[(p.rsplit('.',1)[-1],r['kind'],hex(r['flags']))]+=1
        if k<6: print('rec',k,r['kind'],hex(r['flags']),r['node'],p,r['id'].hex()[:8])
    print(c.most_common(20))

def add_dependencies(d, entries):
    """entries: list of (path, 64-byte record from the donor). Returns new file bytes."""
    D=Deps(d); blob=bytearray(D.blob); nodes=[list(n) for n in D.nodes]; nfiles=D.nfiles; recs=list(D.recs)
    def child(par,name):
        for i,n in enumerate(nodes):
            if i and n[3]==par:
                off=n[2]
                if off>=0 and blob[off:blob.find(b'\0',off)].decode()==name: return i
        return None
    added=[]
    for path,rec in entries:
        par=0; parts=path.split('/')
        for k,part in enumerate(parts):
            c=child(par,part)
            if c is None:
                is_file=(k==len(parts)-1)
                off=len(blob); blob+=part.encode()+b'\0\0'
                c=len(nodes); nodes.append([0,nodes[par][0],off,par,nfiles if is_file else 0])
                nodes[par][0]=c
                if is_file: nfiles+=1
                added.append(path if is_file else part+'/')
            par=c
        rec=bytearray(rec); struct.pack_into('>I',rec,4,par); recs.append(bytes(rec))
    out=bytearray(d[:D.base])
    out+=struct.pack('>5I',D.one,D.two,nfiles,len(nodes),len(blob))+blob
    out+=b''.join(struct.pack('>HHiHH',*n) for n in nodes)
    out+=D.pre[:8]+struct.pack('>I',len(recs))+b''.join(recs)
    out+=d[D.after:D.hdr_end]
    struct.pack_into('>I',out,0,len(out)-4)
    out+=d[D.hdr_end:]
    return bytes(out),added
