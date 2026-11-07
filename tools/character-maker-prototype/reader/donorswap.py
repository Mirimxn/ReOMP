# Pairs a target prefab's sound-event and animation-set handles with a donor prefab's, component by component.
import sys,struct
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from schema import Schema,R; from objtree import objtable; from dec2 import Dec
from collections import defaultdict
EXT=('.audio_event','.pod_baked')
def handles(d,x):
    D=Dec(d); D.trace=[]
    try: D.mobj(R(d,x['o']),struct.unpack_from('<I',d,x['o']-4)[0])
    except Exception: return None
    out=[]; nxt=[y for y in [x] ]
    for n,k,p in D.trace:
        if k=='handle' and d[p+4]:
            ln=struct.unpack_from('<I',d,p+21)[0]; path=d[p+25:p+25+ln-1].decode('latin1')
            if path.lower().endswith(EXT): out.append((n,p,path))
    return out
def pairs(t,OT,u,OU,skip=()):
    """[(target pos, donor pos, old path, new path)] for components whose sound/animation fields line up exactly"""
    idx=defaultdict(list)
    for y in OU: idx[(y['class'],y['path'])].append(y)
    # only innermost objects own their fields: skip any object that contains another object with handles
    out=[]; seen=set()
    comps=[x for x in OT if x['class']!='apiEntity' and not x['path'].startswith(skip)]
    for x in comps:
        ht=handles(t,x)
        if not ht: continue
        ys=idx.get((x['class'],x['path']),[])
        if len(ys)!=1: continue
        hu=handles(u,ys[0]) or []
        if len(ht)!=len(hu) or [n for n,_,_ in ht]!=[n for n,_,_ in hu]: continue
        for (n,p,a),(n2,p2,b) in zip(ht,hu):
            if p in seen or a.lower()==b.lower(): continue
            seen.add(p); out.append((p,p2,a,b))
    return out

def emitter_entries(d,x):
    """{entry ID: (event handle pos, event path)} of an nttAudio::ComponentEmitter"""
    D=Dec(d); D.trace=[]
    try: D.mobj(R(d,x['o']),struct.unpack_from('<I',d,x['o']-4)[0])
    except Exception: return {}
    out={}; cur=None
    for n,k,p in D.trace:
        if n=='ID' and k=='type':
            ln=struct.unpack_from('<I',d,p)[0]; cur=d[p+4:p+4+ln-1].decode('latin1') if 0<ln<100 else None
        elif n=='Event' and k=='handle' and cur is not None:
            if d[p+4]: out.setdefault(cur,(p,d[p+25:p+25+struct.unpack_from('<I',d,p+21)[0]-1].decode('latin1')))
            cur=None
    return out
def sound_pairs(t,OT,u,OU,skip=()):
    """emitters matched by path and shared entry IDs; events swapped entry by entry"""
    out=[]
    te=[x for x in OT if x['class']=='nttAudio::ComponentEmitter' and not x['path'].startswith(skip)]
    ue=[y for y in OU if y['class']=='nttAudio::ComponentEmitter']
    for x in te:
        a=emitter_entries(t,x)
        if not a: continue
        best=max(((len(set(a)&set(emitter_entries(u,y))),y) for y in ue if y['path']==x['path']),default=(0,None),key=lambda z:z[0])
        if best[0]==0: continue
        b=emitter_entries(u,best[1])
        for k,(p,pa) in a.items():
            if k in b and b[k][1].lower()!=pa.lower(): out.append((p,b[k][0],pa,b[k][1]))
    return out
def anim_pairs(t,OT,u,OU):
    """the body's animation set (root AnimBlendTreeComponent 'Action Graph')"""
    def root_graph(d,O):
        for x in O:
            if x['class']=='AnimBlendTreeComponent' and x['path']=='':
                for n,p,pa in handles(d,x) or []:
                    if pa.lower().endswith('_animation.pod_baked'): return p,pa
        return None
    a=root_graph(t,OT); b=root_graph(u,OU)
    return [(a[0],b[0],a[1],b[1])] if a and b and a[1].lower()!=b[1].lower() else []
