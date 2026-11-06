# object list in pre-order with owning entity path
import sys,struct,re
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from schema import Schema,R
def objtable(d,S):
    ol=d.find(b'OLST\0')-8; olsz=struct.unpack_from('<I',d,ol)[0]
    root=d.find(b'MOBJ\0',ol)-8
    objs=[(root,-1,struct.unpack_from('<I',d,root)[0])]
    for m in re.finditer(rb'\x01(....)(....)\x05\x00\x00\x00MOBJ\x00',d[ol:ol+olsz],re.S):
        o=ol+m.start()+5; objs.append((o,struct.unpack('<I',m.group(1))[0],struct.unpack_from('<I',d,o)[0]))
    out=[]; stack=[]
    for i,(o,ci,sz) in enumerate(objs):
        while stack and not (stack[-1][0] < o < stack[-1][1]): stack.pop()
        cname='ROOT' if ci<0 else S.classes[ci]['name']; ename=None
        if cname=='apiEntity':
            r=R(d,o+13); r.u32(); r.u8(); r.p+=16; r.p+=2; ename=r.str()
        path='/'.join(x[2] for x in stack if x[2])
        out.append({'i':i,'o':o,'end':o+sz,'class':cname,'entity':ename,'path':path})
        stack.append((o,o+sz,ename))
    tailpos=ol+olsz
    return out,ol,olsz,tailpos
