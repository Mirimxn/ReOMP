# Lists the entity tree of a character prefab: entities (apiEntity) and their component classes.
import sys,struct
sys.path.insert(0,'/tmp/claude-0/-home-claude/5d780e1f-0614-5cb0-8420-7e2215cd16ae/scratchpad/cm')
from schema import Schema,R
def entities(d,S):
    o=d.find(b'RootNode\0')+9; r=R(d,o)
    mob=r.u32(); plat=r.u16(); ns=r.u8(); n=r.u32()
    out=[]
    def comps(r,count,depth,owner):
        for _ in range(count):
            if r.u8()!=1: continue
            ci=r.u32(); o=r.p; sz=r.u32(); nm=r.str(); end=o+sz
            cname=S.classes[ci]['name']
            if cname=='apiEntity':
                r.u32(); r.u8(); r.p+=16; r.p+=2; name=r.str(); r.u32(); r.u16(); r.u8(); k=r.u32()
                out.append((depth,'entity',name,o,sz,k)); comps(r,k,depth+1,name)
            else:
                out.append((depth,'comp',cname,o,sz,0))
            r.p=end
    comps(r,n,0,'root')
    return out
if __name__=='__main__':
    d=open(sys.argv[1],'rb').read(); S=Schema(d); E=entities(d,S)
    ents=[e for e in E if e[1]=='entity']; print(sys.argv[1].split('-',1)[-1],'| top components',sum(1 for e in E if e[0]==0),'| entities',len(ents))
    for depth,kind,name,o,sz,k in E:
        if kind=='entity': print('  '*depth+f'[{name}] {k} components, {sz} bytes')
