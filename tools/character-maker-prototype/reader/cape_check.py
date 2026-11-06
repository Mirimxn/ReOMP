import sys,struct,re
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from dec2 import Dec
from schema import R
d=open(sys.argv[1],'rb').read(); D=Dec(d); a,b=int(sys.argv[2]),int(sys.argv[3])
for m in re.finditer(rb'\x01(....)(....)\x05\x00\x00\x00MOBJ\x00',d[a:b],re.S):
    ci=struct.unpack('<I',m.group(1))[0]; o=a+m.start()+5
    b0=sum(D.bad.values()); D.why.clear(); D.mobj(R(d,o),ci); ok=sum(D.bad.values())==b0
    print(('ok  ' if ok else 'FAIL'),o,D.S.classes[ci]['name'], '' if ok else list(D.why.items())[:2])
