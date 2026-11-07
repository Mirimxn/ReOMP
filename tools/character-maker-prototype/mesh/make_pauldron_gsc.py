# Builds a TSS GSC from a rigid FBX mesh, using an existing hat GSC as the container (8 parts = 4 LODs x 2 lists).
# Every part gets the new mesh. The mesh is converted from the user's Blender scene units to game units and
# moved into the space of the locator it will hang from.
import sys,struct,math,os
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
import fbx,gsc

def load_fbx_mesh(path):
    N=fbx.read(path); obj=fbx.find(N,'Objects')[0]
    g=[o for o in obj['kids'] if o['name']=='Geometry'][0]
    m=[o for o in obj['kids'] if o['name']=='Model'][0]
    props={p['props'][0]:p['props'][4:] for p in fbx.child(m,'Properties70')['kids']}
    V=fbx.child(g,'Vertices')['props'][0]; PVI=fbx.child(g,'PolygonVertexIndex')['props'][0]
    ln=fbx.child(g,'LayerElementNormal'); NR=fbx.child(ln,'Normals')['props'][0]; NI=fbx.child(ln,'NormalsIndex')['props'][0]
    luv=[k for k in g['kids'] if k['name']=='LayerElementUV' and k['props'][0]==0][0]
    UV=fbx.child(luv,'UV')['props'][0]; UI=fbx.child(luv,'UVIndex')['props'][0]
    mat=[o for o in obj['kids'] if o['name']=='Material']
    col=None
    if mat:
        mp={p['props'][0]:p['props'][4:] for p in fbx.child(mat[0],'Properties70')['kids']}
        col=mp.get('DiffuseColor')
    # polygons -> triangles (fan), corners = polygon-vertex indices
    tris=[]; poly=[]
    for c,i in enumerate(PVI):
        poly.append(c)
        if i<0:
            for k in range(1,len(poly)-1): tris.append((poly[0],poly[k],poly[k+1]))
            poly=[]
    def pos(c):
        i=PVI[c]; i=~i if i<0 else i; return tuple(V[3*i:3*i+3]),i
    corners={}
    for c in range(len(PVI)):
        p,pi=pos(c); n=tuple(NR[3*NI[c]:3*NI[c]+3]); uv=tuple(UV[2*UI[c]:2*UI[c]+2])
        corners[c]=(pi,NI[c],UI[c],p,n,uv)
    return tris,corners,props,col

def build(template_gsc,fbx_path,locator_pos,internal_name,self_path,shader_name,nxg_name,old_shader,old_nxg):
    tris,corners,props,col=load_fbx_mesh(fbx_path)
    T=props['Lcl Translation']; S=props['Lcl Scaling']; Rd=props['Lcl Rotation']
    assert abs(abs(Rd[0])-180)<1e-3 and abs(Rd[1])<1e-3 and abs(Rd[2])<1e-3, Rd
    UNIT=10000.0   # FBX cm of the user's Blender scene (scene at 100x game size) -> game units
    sx,sy,sz=S[0]/UNIT,-S[1]/UNIT,-S[2]/UNIT          # scale, then 180 deg about X flips y and z
    tx,ty,tz=T[0]/UNIT,T[1]/UNIT,T[2]/UNIT
    lx,ly,lz=locator_pos
    def xp(p): return (sx*p[0]+tx-lx, sy*p[1]+ty-ly, sz*p[2]+tz-lz)
    def xn(n):
        v=(n[0]/sx,n[1]/sy,n[2]/sz); l=math.sqrt(sum(a*a for a in v)) or 1; return tuple(a/l for a in v)
    flip=(sx*sy*sz)<0
    # dedupe vertices
    key2v={}; verts=[]; idx=[]
    for t in tris:
        tri=[]
        for c in t:
            pi,ni,ui,p,n,uv=corners[c]; k=(pi,ni,ui)
            if k not in key2v: key2v[k]=len(verts); verts.append((xp(p),xn(n),uv))
            tri.append(key2v[k])
        if flip: tri=[tri[0],tri[2],tri[1]]
        idx+=tri
    # sanity: winding agrees with the normals
    agree=0
    for i in range(0,len(idx),3):
        a,b,c=[verts[j][0] for j in idx[i:i+3]]; n=verts[idx[i]][1]
        e1=[b[k]-a[k] for k in range(3)]; e2=[c[k]-a[k] for k in range(3)]
        g=[e1[1]*e2[2]-e1[2]*e2[1],e1[2]*e2[0]-e1[0]*e2[2],e1[0]*e2[1]-e1[1]*e2[0]]
        agree+=1 if sum(g[k]*n[k] for k in range(3))>0 else -1
    if agree<0: idx=[x for i in range(0,len(idx),3) for x in (idx[i],idx[i+2],idx[i+1])]
    nv=len(verts); assert nv<65536
    mn=[min(v[0][k] for v in verts) for k in range(3)]; mx=[max(v[0][k] for v in verts) for k in range(3)]
    print(f'mesh: {len(tris)} triangles, {nv} vertices, winding agree score {agree}, flip {flip}')
    print('bbox (locator space):',[round(a,4) for a in mn],[round(a,4) for a in mx])
    def mini(f): return max(0,min(255,int(round((f+1)*128))))
    d=open(template_gsc,'rb').read()
    parts,refs,end=gsc.parse(d)
    lists=[L for L in refs.values() if 'defs' in L]; I=[L for L in refs.values() if 'defs' not in L]
    assert len(lists)==2 and len(I)==1, 'expected a hat-style GSC (one full and one position/normal vertex list, one index list)'
    def vdata(L):
        out=bytearray()
        for p,n,uv in verts:
            b=bytearray(L['stride'])
            for var,typ,off in L['defs']:
                name=gsc.VAR[var]
                if name=='position': assert typ==6; struct.pack_into('<4e',b,off,p[0],p[1],p[2],1.0)
                elif name=='normal': assert typ==8; b[off:off+4]=bytes([mini(n[0]),mini(n[1]),mini(n[2]),255])
                elif name=='colorSet0': b[off:off+4]=b'\xff\xff\xff\xff'
                elif name=='tangent': b[off:off+4]=bytes(4)
                elif name=='uvSet01': assert typ==6; struct.pack_into('<4e',b,off,uv[0],1-uv[1],uv[0],1-uv[1])
                elif name=='uvSet2': pass
                else: raise SystemExit('unhandled vertex field '+name)
            out+=b
        return bytes(out)
    I=I[0]
    segs=sorted([(L['countpos'],L['data'],L['data']+L['stride']*L['count'],nv,vdata(L)) for L in lists]+
                [(I['countpos'],I['data'],I['data']+2*I['count'],len(idx),struct.pack('<%dH'%len(idx),*idx))])
    out=bytearray(); p=0
    for cp,ds,de,cnt,data in segs:
        out+=d[p:cp]+struct.pack('>I',cnt)+d[cp+4:ds]+data; p=de
    out+=d[p:]
    # part infos and bounding boxes
    parts2,_,_=gsc.parse(bytes(out))
    cen=[(mn[k]+mx[k])/2 for k in range(3)]; half=[(mx[k]-mn[k])/2 for k in range(3)]
    for P in parts2:
        q=P['info']; struct.pack_into('>III',out,q,0,len(idx),0); struct.pack_into('>I',out,q+14,nv)
        blk=bytes(out[q:P['end']]); k=blk.find(b'\x3f\x80\x00\x00'); assert k>=12
        struct.pack_into('>3f',out,q+k-12,*cen); struct.pack_into('>3f',out,q+k+4,*half)
    h=struct.unpack('>I',out[:4])[0]; c=h+4; struct.pack_into('>I',out,c,len(out)-c-4)
    # internal model name (same length, it sits in the fixed-size name table)
    i=out.find(b'default_string\0'); j=i+15; e=out.index(b'\0',j); old=bytes(out[j:e])
    assert len(internal_name)==len(old), (old,internal_name); out[j:e]=internal_name.encode()
    # own path and id, shader/texture-list names
    n=struct.unpack('>H',out[20:22])[0]; sp=self_path.encode(); assert len(sp)<n
    out[22:22+n]=sp+bytes(n-len(sp)); k=22+n
    while out[k]==0: k+=1
    out[k:k+16]=os.urandom(16)
    for o_,n_ in ((old_shader,shader_name),(old_nxg,nxg_name)):
        o=bytes(out[:h]).find(o_.encode()); assert o>0 and len(n_)<=len(o_),(o_,n_)
        out[o:o+len(o_)]=n_.encode()+bytes(len(o_)-len(n_))
    # check it parses back
    P3,_,_=gsc.parse(bytes(out)); assert all(p['vcnt']==nv and p['icnt']==len(idx) for p in P3)
    print(f'GSC {len(d)} -> {len(out)} bytes, internal name {old.decode()} -> {internal_name}')
    return bytes(out),col
