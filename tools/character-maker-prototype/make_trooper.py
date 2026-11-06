# Builds a custom copy of a character prefab + cheap scene.
# usage: make_trooper.py SRC_DIR OUT_DIR OLD_TAG NEW_TAG ENTRY_NAME [--hat-gsc NEWNAME:IDHEX] [--tex OLD=NEW:IDHEX ...]
import sys,os,struct,argparse
ap=argparse.ArgumentParser()
ap.add_argument('src'); ap.add_argument('out'); ap.add_argument('old'); ap.add_argument('new'); ap.add_argument('entry')
ap.add_argument('--hat-gsc'); ap.add_argument('--tex',action='append',default=[])
a=ap.parse_args(); assert len(a.old)==len(a.new)
OLD_HAT='hat_clonetrooper_phase2'
def swaps():
    s=[]
    if a.hat_gsc:
        name,idh=a.hat_gsc.split(':'); assert len(name)==len(OLD_HAT)
        s+= [(OLD_HAT+'_dx11.gsc',name+'_dx11.gsc',bytes.fromhex(idh)),(OLD_HAT+'_nxg.gsc',name+'_nxg.gsc',None)]
    for t in a.tex:
        old,rest=t.split('='); new,idh=rest.split(':'); assert len(old)==len(new)
        s.append((old+'.texture',new+'.texture',bytes.fromhex(idh)))
    return s
def patch(srcname,dstname,is_prefab):
    d=bytearray(open(os.path.join(a.src,srcname),'rb').read())
    n=struct.unpack('>H',d[20:22])[0]
    d[22:22+n]=bytes(d[22:22+n]).replace(a.old.encode(),a.new.encode()); d[22+n:38+n]=os.urandom(16)
    h=struct.unpack('>I',d[0:4])[0]+4; log=[]
    for old,new,nid in swaps():
        ob,nb=old.encode(),new.encode(); o=h; c=0
        while (o:=d.find(b'/'+ob+b'\0',o))>=0:
            st=o
            while not (d[st-1]==0 and struct.unpack('<I',d[st-4:st])[0]==o+1+len(ob)+1-st): st-=1
            if nid is not None and d[st-21]==1:
                oid=bytes(d[st-20:st-4]); d[st-20:st-4]=nid; d[:h]=bytes(d[:h]).replace(oid,nid)
            d[o+1:o+1+len(ob)]=nb; c+=1; o+=len(ob)
        assert bytes(d[:h]).count(b'\0'+ob+b'\0')==1, old
        d[:h]=bytes(d[:h]).replace(b'\0'+ob+b'\0',b'\0'+nb+b'\0'); log.append(f'{new} (body x{c})')
    # inner names of the character (not the helmet's)
    base=a.old.split('_',1)[0]  # e.g. clonetrooper
    for suffix in ['.prefab\0','_character.prefab\0','_cheapminifig.scene\0']:
        old=f'/{a.old}{suffix}'.encode(); d[:]=bytes(d).replace(old,f'/{a.new}{suffix}'.encode())
    # own resource id (the id after the source path)
    src_suffix='.prefab\0' if is_prefab else '_cheapminifig.scene\0'
    pat=f'/{a.new}{src_suffix}'.encode(); o=d.find(pat); assert o>0
    oid=bytes(d[o+len(pat):o+len(pat)+16]); rid=os.urandom(16); d[:]=bytes(d).replace(oid,rid); log.append('resource id '+rid.hex())
    if is_prefab:   # link to its roster entry
        tmpl=None; o=d.find(b'Characters\0')
        while o>=0:
            ln=struct.unpack('<I',d[o+27:o+31])[0]; nm=bytes(d[o+31:o+31+ln-1])
            if ln-1==len(a.entry) and d[o+31+ln-1]==0: tmpl=(o+31,nm); break
            o=d.find(b'Characters\0',o+1)
        assert tmpl, 'roster link of the same length not found'
        d[tmpl[0]:tmpl[0]+len(a.entry)]=a.entry.encode(); log.append(f'roster link {tmpl[1].decode()} -> {a.entry}')
    os.makedirs(a.out,exist_ok=True); open(os.path.join(a.out,dstname),'wb').write(d); print(dstname); [print('  ',l) for l in log]
U,N=a.old.upper(),a.new.upper()
patch(f'{U}.PREFAB_BAKED',f'{N}.PREFAB_BAKED',True)
patch(f'{U}_CHEAPMINIFIG.SCENE_BAKED',f'{N}_CHEAPMINIFIG.SCENE_BAKED',False)
