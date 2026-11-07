# TSS GSC mesh reader (port of Hologram MESHC9/DXTVA9 layout)
import struct
SS=[0,0,8,12,16,4,8,4,4,4]
VAR=['position','normal','colorSet0','tangent','colorSet1','uvSet01','diffuse','uvSet2','albedo','blendIndices0','blendWeight0','tangent2','lightDirSet','lightColSet','blendPos2','random']
def be(d,p): return struct.unpack_from('>I',d,p)[0]
def parse(d):
    p=d.find(b'HSEM'); assert p>0 and be(d,p+4)==0xC9
    q=p+8; n=be(d,q); q+=4; parts=[]; ref=4; refs={}
    for i in range(n):
        part={'start':q}; one=be(d,q); q+=4
        assert d[q:q+4]==b'SMNR' and be(d,q+4)==0xC9,(q,d[q:q+8]); q+=8
        lc=be(d,q); q+=4; part['lists']=[]
        for l in range(lc):
            m=be(d,q); q+=4
            if m&0xc0000000: part['lists'].append(('ref',m&0xffff,q-4)); q+=8; continue
            unk=be(d,q); cnt=be(d,q+4); cntpos=q+4; q+=8
            assert d[q:q+4]==b'DXTV' and be(d,q+4)==0xA9; q+=8
            dc=be(d,q); q+=4; defs=[]
            for k in range(dc): defs.append((d[q],d[q+1],d[q+2])); q+=3
            q+=6; stride=defs[-1][2]+SS[defs[-1][1]]
            L={'id':ref,'unk':unk,'count':cnt,'countpos':cntpos,'defs':defs,'stride':stride,'data':q}
            refs[ref]=L; part['lists'].append(('new',ref,L)); ref+=1
            q+=stride*cnt; q+=4
        q+=4
        m=be(d,q); q+=4
        if m&0xc0000000: part['idx']=('ref',m&0xffff); q+=4
        else:
            two=be(d,q); ic=be(d,q+4); icpos=q+4; q+=12
            part['idx']=('new',ref,{'count':ic,'countpos':icpos,'data':q}); refs[ref]=part['idx'][2]; ref+=1; q+=2*ic
        part['info']=q
        part['ioff'],part['icnt'],part['voff']=struct.unpack_from('>III',d,q); part['vcnt']=be(d,q+14)
        q+=4  # Hologram TSS reader: offs(12)+2+vcnt(4)+4, then num/num2 blocks, then 44
        q=part['info']+12+2+4+4
        num=struct.unpack_from('>i',d,q)[0]; q+=4
        if num>0: q+=num
        num2=struct.unpack_from('>i',d,q)[0]; q+=4
        part['num2']=num2
        if num2>0: raise Exception('relative position list not handled')
        q+=44; ref+=2; part['end']=q; parts.append(part)
    return parts,refs,q
def verts(d,L):
    out=[]
    for i in range(L['count']):
        b=L['data']+i*L['stride']; v={}
        for var,typ,off in L['defs']:
            o=b+off
            if typ==3: v[VAR[var]]=struct.unpack_from('<3f',d,o)
            elif typ==2: v[VAR[var]]=struct.unpack_from('<2f',d,o)
            elif typ==4: v[VAR[var]]=struct.unpack_from('<4f',d,o)
            elif typ==5: v[VAR[var]]=struct.unpack_from('<2e',d,o)
            elif typ==6: v[VAR[var]]=struct.unpack_from('<4e',d,o)
            else: v[VAR[var]]=d[o:o+4]
        out.append(v)
    return out
