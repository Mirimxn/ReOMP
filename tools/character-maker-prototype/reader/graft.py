# Copy one or more top-level entities from a donor prefab into a target, including their header dependencies.
# usage: graft.py DONOR TARGET OUT AFTER_ENTITY ENTITY [ENTITY...]
import sys,re,struct,glob
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
import add_attachment
from deps import Deps,add_dependencies
from schema import Schema
from objtree import objtable
donor,target,out,after=sys.argv[1:5]; ents=sys.argv[5:]
cur=target
dd=open(donor,'rb').read(); DD=Deps(dd); SD=Schema(dd); OD,_,_,_=objtable(dd,SD)
for i,e in enumerate(ents):
    tmp=out+f'.step{i}'; add_attachment.main(donor,cur,tmp,e,after); cur=tmp; after=e
td=open(cur,'rb').read(); DT=Deps(td)
have={DT.path(DT.rec(k)['node']) for k in range(DT.nrec)}
paths=set()
for e in ents:
    x=[y for y in OD if y['class']=='apiEntity' and y['entity']==e and y['path']==''][0]
    blk=dd[x['o']:x['end']]
    for m in re.finditer(rb'(?:chars|assets)/[A-Za-z0-9_/#.\-]+\.[a-z_0-9]+',blk): paths.add(m.group().decode().lower())
want=[]
for k in range(DD.nrec):
    p=DD.path(DD.rec(k)['node']); twin=p.replace('_nxg.','_dx11.')
    if (p in paths or twin in paths) and p not in have and p not in [w[0] for w in want]: want.append((p,DD.recs[k]))
missing=[p for p in paths if p not in have and p not in [w[0] for w in want]]
print('dependencies added:',[w[0] for w in want]); print('referenced but not in the donor list:',missing)
res,_=add_dependencies(td,want); open(out,'wb').write(res); print('written',out,len(res))
