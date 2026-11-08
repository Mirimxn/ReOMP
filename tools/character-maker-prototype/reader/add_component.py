# Copies one link-free component object (e.g. SpeechModifier) from a donor prefab into an entity of a target prefab.
import sys,struct,os
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
from schema import Schema,R,find_rec
from objtree import objtable
from dec2 import Dec
import add_attachment as AA

def _links(d,S,O):
    D=Dec(d); D.linklog=[]; e_=0
    for x in O[1:]:
        if x['o']<e_: continue
        D.mobj(R(d,x['o']),struct.unpack_from('<I',d,x['o']-4)[0]); e_=x['end']
    return D.linklog

def _cnt_pos(d,o):
    r=R(d,o+13); r.u32(); r.u8(); r.p+=16; r.p+=2; r.str(); return r.p+4+2+1

def add(donor,target,donor_class,entity,after_class,fresh_guid_at=None):
    dd=bytes(donor); td=bytearray(target)
    SD=Schema(dd); ST=Schema(bytes(td))
    OD,_,_,_=objtable(dd,SD); OT,olT,_,tailT=objtable(bytes(td),ST)
    src=[x for x in OD if x['class']==donor_class]; assert len(src)==1,src; src=src[0]
    assert not [l for l in _links(dd,SD,OD) if src['o']<=l[0][0]<src['end']], 'component has links'
    block=bytearray(dd[src['o']-5:src['end']])
    if fresh_guid_at is not None: block[fresh_guid_at:fresh_guid_at+16]=os.urandom(16)
    # class
    tnames={c['name']:i for i,c in enumerate(ST.classes)}
    ci=struct.unpack_from('<I',block,1)[0]; nm=SD.classes[ci]['name']
    mapc={r:tnames[SD.classes[r]['name']] for r in AA.class_refs(SD,ci)}   # base classes must exist in the target
    newclass=nm not in tnames
    tci=tnames.get(nm,len(ST.classes)); struct.pack_into('<I',block,1,tci)
    # where
    ent=[x for x in OT if x['class']=='apiEntity' and x['entity']==entity and x['path']=='']; assert len(ent)==1; ent=ent[0]
    comps=[x for x in OT if x['path']==entity and ent['o']<x['o']<ent['end']]
    aft=[x for x in comps if x['class']==after_class]; assert len(aft)==1,(after_class,[c['class'] for c in comps]); aft=aft[0]
    assert not [x for x in comps if x['class']==nm], f'{entity} already has a {nm}'
    P=aft['i']+1; at=aft['end']
    # links: objects numbered P and above move up by one
    nt=struct.unpack_from('<I',td,find_rec(bytes(td),'TypeList')-4)[0]
    vals=list(struct.unpack_from('<%di'%nt,td,tailT)); vals=[v+1 if v>=P else v for v in vals]
    struct.pack_into('<%di'%nt,td,tailT,*vals)
    # insert, then grow the sizes of the entity, the root and the object list, and the entity's component count
    cp=_cnt_pos(bytes(td),ent['o'])
    td[at:at]=block
    for pos in (ent['o'],OT[0]['o'],olT): struct.pack_into('<I',td,pos,struct.unpack_from('<I',td,pos)[0]+len(block))
    struct.pack_into('<I',td,cp,struct.unpack_from('<I',td,cp)[0]+1)
    tl=find_rec(bytes(td),'TypeList'); nobj,nt2=struct.unpack_from('<II',td,tl-8); struct.pack_into('<II',td,tl-8,nobj+1,nt2)
    if newclass:
        clo,recsD=AA.class_records(dd); rec=AA.remap_class_record(dd[recsD[ci][0]:recsD[ci][0]+recsD[ci][1]],SD,ci,mapc)
        clT=find_rec(bytes(td),'ClassList'); szc=struct.unpack_from('<I',td,clT)[0]
        td[clT+szc:clT+szc]=rec; struct.pack_into('<I',td,clT,szc+len(rec))
        cntpos=clT+4+4+len('ClassList')+1; struct.pack_into('<I',td,cntpos,struct.unpack_from('<I',td,cntpos)[0]+1)
    # check
    T=bytes(td); S2=Schema(T); O2,*_=objtable(T,S2)
    x=[y for y in O2 if y['class']==nm and y['path']==entity]; assert len(x)==1
    assert len(O2)==len(OT)+1
    print(f'{nm} added to {entity} (object {P}, class {"new" if newclass else "existing"} #{tci})')
    return T
