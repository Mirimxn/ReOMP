# Puts one model's mesh into another model's file (same vertex layouts, same skeleton), keeping the host's
# materials, shaders, texture list and skeleton. Host lists/parts are filled from donor lists/parts by mapping.
import struct,sys,os
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
import gsc
def swap(host,donor,list_map,idx_from,part_map):
    """list_map {host list id: donor list id}, idx_from: donor index-list id, part_map [donor part index per host part]"""
    H=bytes(host); hp,hr,_=gsc.parse(H); dp,dr,_=gsc.parse(donor)
    hidx=[r for r,L in hr.items() if 'defs' not in L]; assert len(hidx)==1
    segs=[]
    for hl,dl in list_map.items():
        A=hr[hl]; B=dr[dl]; assert A['defs']==B['defs'] and A['stride']==B['stride'], (A['defs'],B['defs'])
        segs.append((A['countpos'],A['data'],A['data']+A['stride']*A['count'],B['count'],donor[B['data']:B['data']+B['stride']*B['count']]))
    A=hr[hidx[0]]; B=dr[idx_from]
    segs.append((A['countpos'],A['data'],A['data']+2*A['count'],B['count'],donor[B['data']:B['data']+2*B['count']]))
    for k,P in enumerate(hp):   # part infos (offsets, counts, bounds, bone palette)
        D=dp[part_map[k]]; segs.append((None,P['info'],P['end'],None,donor[D['info']:D['end']]))
    segs.sort(key=lambda s:s[1]); out=bytearray(); p=0
    for cp,ds,de,cnt,data in segs:
        if cp is None: out+=H[p:ds]+data
        else: out+=H[p:cp]+struct.pack('>I',cnt)+H[cp+4:ds]+data
        p=de
    out+=H[p:]
    h=struct.unpack('>I',out[:4])[0]; struct.pack_into('>I',out,h+4,len(out)-h-8)
    P2,_,_=gsc.parse(bytes(out))
    for k,P in enumerate(P2): assert (P['icnt'],P['vcnt'])==(dp[part_map[k]]['icnt'],dp[part_map[k]]['vcnt'])
    return bytes(out)
