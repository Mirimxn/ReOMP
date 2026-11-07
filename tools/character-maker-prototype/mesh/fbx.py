# minimal binary FBX reader (7.x)
import struct,zlib
def read(path):
    d=open(path,'rb').read(); ver=struct.unpack_from('<I',d,23)[0]; big=ver>=7500
    def node(p):
        if big: end,np_,pl=struct.unpack_from('<QQQ',d,p); p+=24
        else: end,np_,pl=struct.unpack_from('<III',d,p); p+=12
        nl=d[p]; p+=1; name=d[p:p+nl].decode(); p+=nl
        if end==0: return None,p
        props=[]; q=p
        for _ in range(np_):
            t=chr(d[q]); q+=1
            if t in 'YCILFD':
                f={'Y':'<h','C':'<B','I':'<i','L':'<q','F':'<f','D':'<d'}[t]; props.append(struct.unpack_from(f,d,q)[0]); q+=struct.calcsize(f)
            elif t in 'SR':
                n=struct.unpack_from('<I',d,q)[0]; q+=4; v=d[q:q+n]; q+=n; props.append(v.decode('utf8','replace') if t=='S' else v)
            else:
                n,enc,cl=struct.unpack_from('<III',d,q); q+=12; raw=d[q:q+cl]; q+=cl
                if enc: raw=zlib.decompress(raw)
                f={'f':'f','d':'d','l':'q','i':'i','b':'?'}[t]; props.append(list(struct.unpack('<%d%s'%(n,f),raw)))
        p=q+pl if False else p+pl
        kids=[]; q=p+pl if False else p
        # children follow the property list
        q=p_after=struct_p=0
        return (name,props,end,p),p
    out=[]
    def parse(p,end):
        nodes=[]
        while p<end:
            if big: e,np_,pl=struct.unpack_from('<QQQ',d,p); hdr=24
            else: e,np_,pl=struct.unpack_from('<III',d,p); hdr=12
            if e==0: break
            nl=d[p+hdr]; name=d[p+hdr+1:p+hdr+1+nl].decode(); pp=p+hdr+1+nl
            props=[]; q=pp
            for _ in range(np_):
                t=chr(d[q]); q+=1
                if t in 'YCILFD':
                    f={'Y':'<h','C':'<B','I':'<i','L':'<q','F':'<f','D':'<d'}[t]; props.append(struct.unpack_from(f,d,q)[0]); q+=struct.calcsize(f)
                elif t in 'SR':
                    n=struct.unpack_from('<I',d,q)[0]; q+=4; v=d[q:q+n]; q+=n; props.append(v.decode('utf8','replace') if t=='S' else v)
                else:
                    n,enc,cl=struct.unpack_from('<III',d,q); q+=12; raw=d[q:q+cl]; q+=cl
                    if enc: raw=zlib.decompress(raw)
                    f={'f':'f','d':'d','l':'q','i':'i','b':'?'}[t]; props.append(list(struct.unpack('<%d%s'%(n,f),raw)))
            kids=parse(pp+pl,e)
            nodes.append({'name':name,'props':props,'kids':kids}); p=e
        return nodes
    return parse(27,len(d))
def find(nodes,name): return [n for n in nodes if n['name']==name]
def child(n,name):
    m=find(n['kids'],name); return m[0] if m else None
