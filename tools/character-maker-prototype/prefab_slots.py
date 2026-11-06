# Lists the parts of a character prefab: model refs (+ part name) and texture refs, grouped by folder.
import sys,re,struct,collections
d=open(sys.argv[1],'rb').read(); h=struct.unpack('>I',d[0:4])[0]+4
n=struct.unpack('>H',d[20:22])[0]; print('##',d[22:22+n].rstrip(b'\0').decode(),'| header',h,'| size',len(d))
refs=[]
for m in re.finditer(rb'chars/[a-z0-9_/]+?\.(gsc|ghg|texture|prefab)\x00',d[h:],re.I):
    o=h+m.start(); p=m.group()[:-1].decode()
    ln=struct.unpack('<I',d[o-4:o])[0] if o>=4 else 0
    if ln!=len(p)+1 and not (ln>len(p) and d[o+len(p):o+ln-1].strip(b'\0')==b''): continue
    part=''
    if p.lower().endswith(('.gsc','.ghg')):
        q=o+ln
        for k in range(q,q+24):
            L=struct.unpack('<I',d[k:k+4])[0]
            if 2<L<64 and all(32<=c<127 or c==0 for c in d[k+4:k+4+L-1]) and d[k+4]>64:
                part=d[k+4:k+4+L].split(b'\0')[0].decode(); break
    refs.append((o,p,part))
groups=collections.OrderedDict()
for o,p,part in refs:
    seg=p.split('/'); key='/'.join(seg[1:-1])
    groups.setdefault(key,[]).append((p.split('/')[-1],part))
for k,v in groups.items():
    print(f'  [{k}]')
    for name,part in v: print('     ',name,('<part '+part+'>') if part else '')
