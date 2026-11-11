// Reading the game's .DAT archives in place (port of DATExtract's new-format reader).
// Chunks: DFLT = raw deflate with block types 0 and 2 swapped (0 = dynamic Huffman, 2 = stored); OODL = Oodle, unpacked by ooz built to WebAssembly.
const DAT=(()=>{
const latin=(u,a,b)=>{let s='';for(let i=a;i<b;i++)s+=String.fromCharCode(u[i]);return s;};
const dv=(u)=>new DataView(u.buffer,u.byteOffset,u.byteLength);

// ---- deflate with TT's block types
const LBASE=[3,4,5,6,7,8,9,10,11,13,15,17,19,23,27,31,35,43,51,59,67,83,99,115,131,163,195,227,258],LEXT=[0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,3,3,3,3,4,4,4,4,5,5,5,5,0];
const DBASE=[1,2,3,4,5,7,9,13,17,25,33,49,65,97,129,193,257,385,513,769,1025,1537,2049,3073,4097,6145,8193,12289,16385,24577,32769,49153],DEXT=[0,0,0,0,1,1,2,2,3,3,4,4,5,5,6,6,7,7,8,8,9,9,10,10,11,11,12,12,13,13,14,14];
const ORDER=[16,17,18,0,8,7,9,6,10,5,11,4,12,3,13,2,14,1,15];
function table(lens){let max=0;for(const l of lens)if(l>max)max=l;if(!max)max=1;const cnt=new Uint16Array(16);for(const l of lens)cnt[l]++;cnt[0]=0;
  const next=new Uint16Array(16);let code=0;for(let b=1;b<16;b++){code=(code+cnt[b-1])<<1;next[b]=code;}
  const tab=new Int32Array(1<<max).fill(-1);
  for(let s=0;s<lens.length;s++){const l=lens[s];if(!l)continue;let c=next[l]++,r=0;for(let i=0;i<l;i++){r=(r<<1)|(c&1);c>>=1;}
    for(let k=r;k<(1<<max);k+=1<<l)tab[k]=(s<<4)|l;}
  return {tab,max};}
let FIXED=null;
function inflateTT(src,size){const out=new Uint8Array(size);let op=0,pos=0,bb=0,bc=0;
  const need=(n)=>{while(bc<n){bb|=(pos<src.length?src[pos++]:0)<<bc;bc+=8;}};
  const bits=(n)=>{if(!n)return 0;need(n);const v=bb&((1<<n)-1);bb>>>=n;bc-=n;return v;};
  const dec=(t)=>{need(t.max);const e=t.tab[bb&((1<<t.max)-1)];if(e<0)throw new Error('bad code');const l=e&15;bb>>>=l;bc-=l;return e>>4;};
  let final=0;
  while(!final){final=bits(1);const type=bits(2);
    if(type==2){const drop=bc&7;bits(drop);const len=bits(16);bits(16);for(let i=0;i<len;i++)out[op++]=bits(8);continue;}
    let lt,dt;
    if(type==1){if(!FIXED){const l=new Array(288);for(let i=0;i<288;i++)l[i]=i<144?8:i<256?9:i<280?7:8;FIXED=[table(l),table(new Array(32).fill(5))];}[lt,dt]=FIXED;}
    else if(type==0){const hlit=bits(5)+257,hdist=bits(5)+1,hclen=bits(4)+4;const cl=new Array(19).fill(0);for(let i=0;i<hclen;i++)cl[ORDER[i]]=bits(3);
      const ct=table(cl);const lens=[];
      while(lens.length<hlit+hdist){const s=dec(ct);
        if(s<16)lens.push(s);else if(s==16){const p=lens[lens.length-1],n=3+bits(2);for(let i=0;i<n;i++)lens.push(p);}
        else{const n=s==17?3+bits(3):11+bits(7);for(let i=0;i<n;i++)lens.push(0);}}
      lt=table(lens.slice(0,hlit));dt=table(lens.slice(hlit,hlit+hdist));}
    else throw new Error('bad block type');
    for(;;){const s=dec(lt);
      if(s<256){out[op++]=s;continue;}if(s==256)break;
      const k=s-257;const len=LBASE[k]+bits(LEXT[k]);const d=dec(dt);const dist=DBASE[d]+bits(DEXT[d]);
      let from=op-dist;if(from<0)throw new Error('distance too far back');for(let i=0;i<len;i++)out[op++]=out[from++];}}
  if(op!==size)throw new Error('DFLT chunk gave '+op+' of '+size+' bytes');
  return out;}

// ---- Oodle
let ooz=null;
async function oodle(src,size){
  if(!ooz){const {instance}=await WebAssembly.instantiate(Uint8Array.from(atob(OOZ_WASM),c=>c.charCodeAt(0)),{});ooz=instance.exports;}
  const ip=ooz.malloc(src.length);new Uint8Array(ooz.memory.buffer).set(src,ip);const op=ooz.malloc(size+64);
  const r=ooz.Kraken_Decompress(ip,src.length,op,size);const out=new Uint8Array(ooz.memory.buffer,op,size).slice();ooz.free(ip);ooz.free(op);
  if(r!==size)throw new Error('Oodle chunk did not unpack');return out;}

// ---- FNV-1a 64 of the upper-case path, as hi:lo
function fnv64(s){let hi=0xcbf29ce4,lo=0x84222325;
  for(let i=0;i<s.length;i++){let c=s.charCodeAt(i);if(c>=97&&c<=122)c-=32;lo=(lo^c)>>>0;
    const m=lo*0x1b3,carry=Math.floor(m/4294967296);const nl=m>>>0;const nh=(hi*0x1b3+carry+((lo<<8)>>>0))%4294967296;lo=nl;hi=nh;}
  return hi.toString(16)+':'+lo.toString(16);}
function fnv32(s){let h=0x811c9dc5;for(let i=0;i<s.length;i++){let c=s.charCodeAt(i);if(c>=97&&c<=122)c-=32;h=Math.imul((h^c)>>>0,0x199933)>>>0;}return h;}

// ---- archive table
async function open(file){
  const rd=async(o,n)=>new Uint8Array(await file.slice(o,o+n).arrayBuffer());
  const h0=dv(await rd(0,8));let off=h0.getUint32(0,true);if(off&0x80000000)off=((off^0xffffffff)>>>0)*256+256;
  if(file.size>4294967295&&off<4294967296)off+=4294967296;
  const sz=h0.getUint32(4,true);const h=await rd(off,sz);const v=dv(h);
  if(latin(h,4,8)!='.CC4')throw new Error(file.name+' is not a supported archive');
  const archiveId=v.getInt32(12,false),namesCount=v.getUint32(24,false),namesSize=v.getUint32(28,false);
  let p=32+4+namesSize;const folders=new Array(namesCount),paths=[];
  for(let i=0;i<namesCount;i++){const nameOffset=v.getUint32(p,false),folderId=v.getUint16(p+4,false);let fileId=v.getUint16(p+10,false);p+=12;
    if(nameOffset===0xffffffff)continue;const o=32+nameOffset;let e=o;while(h[e])e++;let name=latin(h,o,e);
    if(i==namesCount-1)fileId=paths.length;name=(folders[folderId]||'')+'\\'+name;
    if(fileId!==0)paths.push(name);else folders[i]=name;}
  p+=4;const fc=v.getUint32(p,false);p+=4;const files=new Array(fc);
  for(let i=0;i<fc;i++){const hi=v.getUint32(p,false),lo=v.getUint32(p+4,false);let packed=0,offset,zsize=v.getUint32(p+8,false),size=v.getUint32(p+12,false);
    if(archiveId<=-13){packed=hi>>>24;offset=(hi&0xffffff)*4294967296+lo;}
    else{offset=hi*4294967296+lo;if(archiveId<=-10){packed=size>>>31;size&=0x7fffffff;}}
    files[i]={offset,zsize,size,packed,path:null};p+=16;}
  // names reach their entries through a hash table of the paths (64-bit big-endian here; 32-bit kept as a fallback)
  const map64=new Map(),map32=new Map();
  for(let i=0;i<fc;i++){const q=p+8*i;if(q+8<=h.length)map64.set(v.getUint32(q,false).toString(16)+':'+v.getUint32(q+4,false).toString(16),i);
    const q2=p+4*i;if(q2+4<=h.length)map32.set(v.getUint32(q2,false),i);}
  let use64=0,use32=0;for(const n of paths.slice(0,200)){const s=n.slice(1);if(map64.has(fnv64(s)))use64++;if(map32.has(fnv32(s)))use32++;}
  const pick=use64>=use32?(s)=>map64.get(fnv64(s)):(s)=>map32.get(fnv32(s));
  let named=0;for(const n of paths){const k=pick(n.slice(1));if(k==null)continue;files[k].path=n.slice(1).replace(/\\/g,'/').toLowerCase();named++;}
  return {file,name:file.name,files:files.filter(f=>f.path),count:named};}

async function read(file,f){
  if(f.size===f.zsize&&!f.packed)return new Uint8Array(await file.slice(f.offset,f.offset+f.size).arrayBuffer());
  const z=new Uint8Array(await file.slice(f.offset,f.offset+f.zsize).arrayBuffer());const v=dv(z);const out=new Uint8Array(f.size);let o=0,w=0;
  while(o<z.length&&w<f.size){const cc=latin(z,o,o+4),cs=v.getInt32(o+4,true),ds=v.getInt32(o+8,true);const data=z.subarray(o+12,o+12+cs);
    let r;if(cs===ds)r=data;else if(cc=='DFLT')r=inflateTT(data,ds);else if(cc=='OODL')r=await oodle(data,ds);
    else throw new Error('archive chunk type '+cc+' is not supported');
    out.set(r,w);w+=ds;o+=12+cs;}
  if(w!==f.size)throw new Error('archive entry unpacked to '+w+' of '+f.size+' bytes');
  return out;}
return {open,read,inflateTT,fnv64};
})();
if(typeof module!='undefined')module.exports=DAT;
