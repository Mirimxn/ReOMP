import sys,struct,re,collections
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from schema import Schema
def flat(S,ci,seen=None):
    out=[]
    for f in S.classes[ci]['fields']:
        if (f['flags']&0xC0000000)==0xC0000000 and f['ref']==0: out+=flat(S,f['type'])
        else: out.append(f)
    return out
def slots(S,ci,depth=0):
    """runtime link fields (0x9...) of a class incl. embedded structs"""
    n=0
    for f in flat(S,ci):
        if (f['flags']&0xF0000000)==0x90000000: n+=1
        elif f['type']>=len(S.types) and f['type']<len(S.classes) and depth<6 and f['c']!=0: n+=0
        elif f['type']>=len(S.types) and f['type']<len(S.classes) and depth<6: n+=slots(S,f['type'],depth+1)
    return n
def objects(d):
    ol=d.find(b'OLST\0')-8; olsz=struct.unpack_from('<I',d,ol)[0]
    objs=[('root',-1,d.find(b'MOBJ\0',ol)-8)]
    for m in re.finditer(rb'\x01(....)(....)\x05\x00\x00\x00MOBJ\x00',d[ol:ol+olsz],re.S):
        objs.append((None,struct.unpack('<I',m.group(1))[0],ol+m.start()+5))
    tail=struct.unpack('<%di'%((len(d)-ol-olsz)//4),d[ol+olsz:])
    return objs,tail
if __name__=='__main__':
    d=open(sys.argv[1],'rb').read(); S=Schema(d); objs,tail=objects(d)
    tot=sum(slots(S,ci) for _,ci,_ in objs if ci>=0)
    print('objects',len(objs),'predicted slots',tot,'table',len(tail))
    c=collections.Counter()
    for _,ci,_ in objs:
        if ci>=0 and slots(S,ci): c[S.classes[ci]['name']]+=slots(S,ci)
    print(c.most_common(12))
