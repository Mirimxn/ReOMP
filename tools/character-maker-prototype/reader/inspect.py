import sys,struct
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from dec2 import Dec
d=open(sys.argv[1],'rb').read(); D=Dec(d); S=D.S
cname=sys.argv[2]; o=int(sys.argv[3])
ci=[i for i,c in enumerate(S.classes) if c['name']==cname][0]
def tn(t): return S.types[t][0] if t<D.NT else '#'+S.classes[t]['name'] if t<len(S.classes) else f'?{t}'
for f in D.flat(ci): print(f"   {f['name'][:34]:34s} {tn(f['type'])[:40]:40s} flags {f['flags']&0xffffffff:08x} off {f['ref']} a {f['a']:#x} b {f['b']} c {f['c']}")
sz=struct.unpack_from('<I',d,o)[0]
for p in range(o,o+min(sz,int(sys.argv[4]) if len(sys.argv)>4 else 160),16): print(f"{p:7d}",d[p:p+16].hex(' '),bytes(x if 32<=x<127 else 46 for x in d[p:p+16]).decode())
