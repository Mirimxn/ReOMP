import sys,struct; sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from schema import Schema,R; from objtree import objtable; from dec2 import Dec
def tints(d,O,x):
    D=Dec(d); D.trace=[]; D.mobj(R(d,x['o']),struct.unpack_from('<I',d,x['o']-4)[0]); T=D.trace; out=[]
    for j,(n,k,p) in enumerate(T):
        if n!='Material State Overrides': continue
        get=lambda name: [q for nn,kk,q in T[j:j+60] if nn==name][0]
        idp=get('ID'); ce=get('colour_tint_enabled_0'); ct=get('colour_tint_0')
        out.append((struct.unpack_from('<H',d,idp)[0],ce,ct,d[ce],struct.unpack_from('<3f',d,ct)))
    return out
if __name__=='__main__':
    for f in sys.argv[1:]:
        d=open(f,'rb').read(); S=Schema(d); O,_,_,_=objtable(d,S); print('==',f.split('/')[-1])
        for x in O:
            if x['class']=='nttRenderTTExportObject' and x['path'] and '/' not in x['path'] and not x['path'].startswith(('Items','Abilities')):
                print('  ',x['path'],[(i,e,tuple(round(v,3) for v in c)) for i,_,_,e,c in tints(d,O,x)])
