# Copies an attachment entity (top-level child of the root) from a donor prefab into a target prefab.
# usage: add_attachment.py DONOR TARGET OUT ENTITY_NAME
import sys,struct,re
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from schema import Schema,R,find_rec
from objtree import objtable
from dec2 import Dec

OBJ_RE=re.compile(rb'\x01(....)(....)\x05\x00\x00\x00MOBJ\x00',re.S)

def class_records(d):
    """offsets of each Class record inside ClassList"""
    o=find_rec(d,'ClassList'); r=R(d,o); nm,p,e=r.rec(); r.p=p; n=r.u32(); out=[]
    for _ in range(n):
        st=r.p; sz=r.u32(); out.append((st,sz)); r.p=st+sz
    return o,out

def class_refs(S,ci):
    """class indices referenced by a class definition (bases, embedded/linked classes)"""
    refs=set()
    for f in S.classes[ci]['fields']:
        fl=f['flags']&0xffffffff; t=f['type']
        if ((fl&0xC0000000)==0xC0000000) or (fl&0x80000000) or t>=len(S.types):
            if 0<=t<len(S.classes): refs.add(t)
    return refs

def remap_class_record(rec,S,ci,mapc):
    """rewrite the type numbers that are class indices inside one Class record"""
    rec=bytearray(rec); r=R(bytes(rec),0); r.u32(); assert r.str()=='Class'; r.str()   # record size, 'Class', class name
    while r.p<len(rec):
        fn,p3,e3=r.rec()
        if fn=='Types':
            r.u8(); nf=r.u32()
            for k in range(nf):
                tpos=r.p; t=r.u32(); r.str(); vals=[r.i32() for _ in range(5)]
                fl=vals[2]&0xffffffff
                if ((fl&0xC0000000)==0xC0000000) or (fl&0x80000000) or t>=len(S.types):
                    if 0<=t<len(S.classes): struct.pack_into('<I',rec,tpos,mapc[t])
        r.p=e3
    return bytes(rec)

def main(donor_path,target_path,out_path,entity):
    dd=open(donor_path,'rb').read(); td=bytearray(open(target_path,'rb').read())
    SD=Schema(dd); ST=Schema(bytes(td))
    OD,_,_,_=objtable(dd,SD); OT,olT,olszT,tailT=objtable(bytes(td),ST)
    # ---- the attachment in the donor
    ent=[x for x in OD if x['class']=='apiEntity' and x['entity']==entity and x['path']=='']
    assert len(ent)==1, f'entity {entity!r} not found at the top level of the donor'
    ent=ent[0]; first=ent['i']; inside=[x for x in OD if ent['o']<=x['o']<ent['end']]; last=inside[-1]['i']
    block=bytearray(dd[ent['o']-5:ent['end']])          # 01, u32 class, MOBJ ...
    print(f'{entity}: {len(inside)} objects (donor #{first}..#{last}), {len(block)} bytes')
    # ---- classes: map donor class index -> target index, adding missing ones
    tnames={c['name']:i for i,c in enumerate(ST.classes)}
    needed=set(); todo=[struct.unpack('<I',m.group(1))[0] for m in OBJ_RE.finditer(block)]
    while todo:
        c=todo.pop()
        if c in needed: continue
        needed.add(c); todo+=list(class_refs(SD,c))
    mapc={}; added=[]
    for c in sorted(needed):
        nm=SD.classes[c]['name']
        if nm in tnames: mapc[c]=tnames[nm]
        else: mapc[c]=len(ST.classes)+len(added); added.append(c)
    # classes referenced by added classes but already present also need mapping
    for c in added:
        for r_ in class_refs(SD,c):
            if r_ not in mapc:
                nm=SD.classes[r_]['name']; mapc[r_]=tnames[nm] if nm in tnames else None
    print('classes added:',[SD.classes[c]['name'] for c in added])
    # ---- remap class numbers inside the block
    for m in OBJ_RE.finditer(bytes(block)):
        ci=struct.unpack('<I',m.group(1))[0]; struct.pack_into('<I',block,m.start()+1,mapc[ci])
    # ---- link slots owned by the attachment (decoder walks the donor's top-level objects in order)
    _,_,_,tailD=objtable(dd,SD)
    ntD=struct.unpack_from('<I',dd,find_rec(dd,'TypeList')-4)[0]
    tailvals=struct.unpack_from('<%di'%ntD,dd,tailD)
    D=Dec(dd); D.linklog=[]
    top=[x for x in OD if x['path']=='' and x['i']>0 and x['class']!='ROOT']
    stack_end=0
    for x in OD[1:last+1]:
        if x['o']<stack_end: continue
        ci=struct.unpack_from('<I',dd,x['o']-4)[0]; D.mobj(R(dd,x['o']),ci); stack_end=x['end']
    assert not D.bad.get('apiEntity') or True
    before=sum(1 for (o,c),n,sb in D.linklog if o<ent['o'])
    mine=sum(1 for (o,c),n,sb in D.linklog if ent['o']<=o<ent['end'])
    bad_inside=[c for (o,c) in [(x['o'],x['class']) for x in inside] if False]
    print(f'link slots: {mine} (donor slots {before}..{before+mine-1})')
    slots=list(tailvals[before:before+mine])
    # ---- map links: inside -> new numbers, outside -> same class at the same place in the target
    newbase=len(OT)
    def map_obj(v):
        if v<0: return v
        if first<=v<=last: return newbase+(v-first)
        x=OD[v]; m=[y['i'] for y in OT if y['class']==x['class'] and y['path']==x['path'] and (y['entity']==x['entity'])]
        if len(m)!=1: raise SystemExit(f'cannot map donor object #{v} {x["class"]} in {x["path"]!r}: candidates {m}')
        return m[0]
    newslots=[map_obj(v) for v in slots]
    print('slots donor  :',slots); print('slots target :',newslots)
    # ---- write the target
    # 1. link table: append
    ntT=struct.unpack_from('<I',td,find_rec(bytes(td),'TypeList')-4)[0]
    assert tailT+4*ntT==len(td), 'link table is not at the end of the file'
    td+=struct.pack('<%di'%len(newslots),*newslots)
    # 2. object list: append the block as the last root component
    # the root is an apiEntity named 'RootNode': prefix, Enabled, Guid, MBA, Locked, Name, Mobility, Platform, NameSearch, Components
    root=OT[0]; r=R(bytes(td),root['o']+13); r.u32(); r.u8(); r.p+=16; r.p+=2; nm=r.str(); assert nm=='RootNode',nm
    cnt_pos=r.p+4+2+1
    insert_at=root['end']
    assert max(x['end'] for x in OT)==insert_at
    td[insert_at:insert_at]=block
    for pos in (root['o'],olT):                  # root MOBJ, OLST sizes
        struct.pack_into('<I',td,pos,struct.unpack_from('<I',td,pos)[0]+len(block))
    struct.pack_into('<I',td,cnt_pos,struct.unpack_from('<I',td,cnt_pos)[0]+1)
    # 3. counters before TypeList
    tl=find_rec(bytes(td),'TypeList')
    nobj,nt=struct.unpack_from('<II',td,tl-8); struct.pack_into('<II',td,tl-8,nobj+len(inside),nt+len(newslots))
    # 4. class list: append definitions
    clo,recsD=class_records(dd)
    newrecs=b''.join(remap_class_record(dd[recsD[c][0]:recsD[c][0]+recsD[c][1]],SD,c,{k:(v if v is not None else 0) for k,v in mapc.items()}) for c in added)
    clT=find_rec(bytes(td),'ClassList'); szc=struct.unpack_from('<I',td,clT)[0]
    td[clT+szc:clT+szc]=newrecs
    struct.pack_into('<I',td,clT,szc+len(newrecs))
    cntpos=clT+4+4+len('ClassList')+1
    struct.pack_into('<I',td,cntpos,struct.unpack_from('<I',td,cntpos)[0]+len(added))
    open(out_path,'wb').write(td)
    print(f'written {out_path}: {len(td)} bytes (+{len(td)-len(open(target_path,"rb").read())})')

if __name__=='__main__':
    main(*sys.argv[1:5])
