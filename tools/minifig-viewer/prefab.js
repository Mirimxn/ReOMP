// Reader and editor for baked character prefabs (.prefab_baked) and their roster preview scenes (.scene_baked).
// Port of tools/character-maker-prototype/reader (schema.py, objtree.py, dec2.py, deps.py, tints.py, make_ct212p.py).
const PF=(()=>{
const TE=new TextEncoder();
const enc=(s)=>TE.encode(s);
const latin=(u,a,b)=>{let s='';for(let i=a;i<b;i++)s+=String.fromCharCode(u[i]);return s;};
const dv=(d)=>new DataView(d.buffer,d.byteOffset,d.byteLength);
function find(d,pat,start){if(typeof pat=='string')pat=enc(pat);const f=pat[0],n=d.length-pat.length;let i=start||0;
  while(i<=n){i=d.indexOf(f,i);if(i<0||i>n)return -1;let k=1;while(k<pat.length&&d[i+k]===pat[k])k++;if(k===pat.length)return i;i++;}return -1;}
function findAll(d,pat,start,end){const out=[];let o=(start||0)-1;while((o=find(d,pat,o+1))>=0&&(end==null||o<end))out.push(o);return out;}
function cat(parts){let n=0;for(const p of parts)n+=p.length;const o=new Uint8Array(n);let q=0;for(const p of parts){o.set(p,q);q+=p.length;}return o;}
const u32le=(v)=>{const b=new Uint8Array(4);dv(b).setUint32(0,v>>>0,true);return b;};
const u32be=(v)=>{const b=new Uint8Array(4);dv(b).setUint32(0,v>>>0,false);return b;};
function rand16(){const b=new Uint8Array(16);crypto.getRandomValues(b);return b;}
const eq=(a,b)=>a.length==b.length&&a.every((v,i)=>v===b[i]);

class R{constructor(d,p){this.d=d;this.v=dv(d);this.p=p||0;}
  u8(){return this.d[this.p++];} u16(){const v=this.v.getUint16(this.p,true);this.p+=2;return v;}
  u32(){const v=this.v.getUint32(this.p,true);this.p+=4;return v;} i32(){const v=this.v.getInt32(this.p,true);this.p+=4;return v;}
  f32(){const v=this.v.getFloat32(this.p,true);this.p+=4;return v;}
  str(){const n=this.u32();let e=this.p+n;const s0=this.p;this.p=e;while(e>s0&&this.d[e-1]===0)e--;return latin(this.d,s0,e);}
  rec(){const o=this.p;const sz=this.u32();const nm=this.str();return [nm,this.p,o+sz];}}
function findRec(d,name,start){const o=find(d,cat([u32le(name.length+1),enc(name+'\0')]),start||0);return o<0?-1:o-4;}

// ---------- schema: TypeList and ClassList
function Schema(d){const S={types:[],classes:[]};
  let o=findRec(d,'TypeList');if(o<0)throw new Error('not a baked prefab (no TypeList)');
  let r=new R(d,o);let [nm,p,e]=r.rec();r.p=p;let count=r.u32();
  for(let i=0;i<count;i++){const [n2,p2,e2]=r.rec();const tn=r.str();const size=r.i32();S.types.push([tn,size]);r.p=e2;}
  o=findRec(d,'ClassList',e);r=new R(d,o);[nm,p,e]=r.rec();r.p=p;count=r.u32();
  for(let i=0;i<count;i++){const [n2,p2,e2]=r.rec();const cname=r.str();const cls={name:cname,fields:[],version:null};
    while(r.p<e2){const [fn,p3,e3]=r.rec();
      if(fn=='Version')cls.version=r.f32();
      else if(fn=='Types'){r.u8();const nf=r.u32();
        for(let k=0;k<nf;k++){const t=r.u32(),n=r.str();const v=[r.i32(),r.i32(),r.i32(),r.i32(),r.i32()];
          cls.fields.push({type:t,name:n,ref:v[0],a:v[1],flags:v[2]>>>0,b:v[3],c:v[4]});}}
      r.p=e3;}
    S.classes.push(cls);r.p=e2;}
  S.end=e;return S;}

// ---------- object table: every MOBJ in pre-order with its owning entity path
function objtable(d,S){const v=dv(d);const ol=find(d,'OLST\0')-8;const olsz=v.getUint32(ol,true);
  const root=find(d,'MOBJ\0',ol)-8;const objs=[[root,-1,v.getUint32(root,true)]];
  const M=enc('\x05\x00\x00\x00MOBJ\x00');let k=ol;const lim=ol+olsz;
  while((k=find(d,M,k+1))>=0&&k<lim){const st=k-9;if(st<ol||d[st]!==1)continue;if(st+5===root)continue;
    objs.push([st+5,v.getUint32(st+1,true),v.getUint32(st+5,true)]);}
  const out=[],stack=[];
  objs.forEach(([o,ci,sz],i)=>{while(stack.length&&!(stack[stack.length-1][0]<o&&o<stack[stack.length-1][1]))stack.pop();
    const cname=ci<0?'ROOT':(S.classes[ci]||{name:'?'}).name;let ename=null;
    if(cname=='apiEntity'){const r=new R(d,o+13);r.u32();r.u8();r.p+=16;r.p+=2;ename=r.str();}
    const path=stack.map(x=>x[2]).filter(Boolean).join('/');
    out.push({i,o,end:o+sz,ci,class:cname,entity:ename,path});stack.push([o,o+sz,ename]);});
  return {O:out,olo:ol,olsz,tail:ol+olsz};}

// ---------- field decoder (dec2.py)
const FIXED={Char:1,UChar:1,Short:2,Int:4,Int64:8,Float:4,Half:2,HalfVec3:6,Vec3:12,Vec4:16,Colour3:12,Colour4:16,Colour32:4,NuQuat:16,NuGuid:16,Mtx:64,Ptr:8,NuTransform:48,NuHSpecial:16};
class Dec{constructor(d,S){this.d=d;this.S=S||Schema(d);this.NT=this.S.types.length;this.bad=0;this.why=null;this.trace=null;this._flat={};this._comp={};this._root={};}
  flat(ci){if(this._flat[ci])return this._flat[ci];let out=[];
    for(const f of this.S.classes[ci].fields){if((f.flags&0xC0000000)>>>0==0xC0000000&&f.ref==0)out=out.concat(this.flat(f.type));else out.push(f);}
    return this._flat[ci]=out;}
  isComp(ci,depth=0){if(!(ci>=0&&ci<this.S.classes.length)||depth>30)return false;if(depth==0&&ci in this._comp)return this._comp[ci];
    const nm=this.S.classes[ci].name;let r=false;
    if(nm=='apiComponent'||nm=='apiEntity')r=true;
    else for(const f of this.S.classes[ci].fields){if((f.flags&0xC0000000)>>>0==0xC0000000&&f.ref==0){r=this.isComp(f.type,depth+1);break;}}
    if(depth==0)this._comp[ci]=r;return r;}
  root(ci,depth=0){if(ci in this._root)return this._root[ci];let r=this.S.classes[ci].name;
    for(const f of this.S.classes[ci].fields){if((f.flags&0xC0000000)>>>0==0xC0000000&&f.ref==0&&depth<30){r=this.root(f.type,depth+1);break;}}
    return this._root[ci]=r;}
  kind(f){const fl=f.flags,t=f.type,C=this.S.classes;
    if(fl&0x80000000){
      if(fl&0x10000000){if(t>=0&&t<C.length&&this.root(t)=='AnimNode')return 'noderef';return 'link';}
      if(!(t>=0&&t<C.length))return 'type';
      if(f.ref==-1||this.isComp(t))return 'link';
      const cn=C[t].name;if((cn.includes('Handle')&&cn.startsWith('nttResource'))||cn.includes('ResourceHandle'))return 'handle';
      return 'struct';}
    if(t<this.NT)return 'type';return 'struct';}
  field(r,f,depth){const k=f._k||(f._k=this.kind(f));
    if(f.c==0){const n=r.u32();if(n>100000)throw new Error('list '+f.name+' count '+n);for(let i=0;i<n;i++)this.one(r,f,k,depth);}
    else this.one(r,f,k,depth);}
  one(r,f,k,depth){if(this.trace)this.trace.push([f.name,k,r.p]);
    if(k=='link')return;
    if(k=='noderef'){r.p+=16;const n=r.u32();r.p+=n;r.p+=8;return;}
    if(k=='handle'){r.u32();const h=r.u8();if(h){r.p+=16;const n=r.u32();r.p+=n;}return;}
    if(k=='struct'){for(const g of this.flat(f.type))this.field(r,g,depth+1);return;}
    const tn=this.S.types[f.type][0];
    if(tn in FIXED){r.p+=FIXED[tn];return;}
    if(tn=='String'||tn=='Data'){const n=r.u32();r.p+=n;return;}
    if(tn=='ClassObject'||tn=='ClassObjectRef'){if(r.u8()==0)return;const ci=r.u32();this.mobj(r,ci,depth+1);return;}
    if(f.type==23){r.u32();const h=r.u8();if(h){r.p+=16;const n=r.u32();r.p+=n;}return;}
    throw new Error('type '+f.type+' '+tn+' in '+f.name);}
  mobj(r,ci,depth=0){const o=r.p;const sz=r.u32();const nm=r.str();if(nm!='MOBJ')throw new Error('expected MOBJ');const end=o+sz;
    try{r.u32();for(const f of this.flat(ci)){this.field(r,f,depth);if(r.p>end)throw new Error('overran in field '+f.name);}
      if(r.p!=end)throw new Error('consumed '+(r.p-o)+' of '+sz);}
    catch(e){this.bad++;if(!this.why)this.why=this.S.classes[ci].name+': '+e.message;}
    r.p=end;}
  traceOf(x){this.trace=[];this.bad=0;this.mobj(new R(this.d,x.o),dv(this.d).getUint32(x.o-4,true));const t=this.trace;this.trace=null;return t;}}

// ---------- header dependency list (deps.py)
function Deps(d){const v=dv(d);const D={};const n=v.getUint16(20,false);D.base=22+n+16;
  [D.one,D.two,D.nfiles,D.nnodes,D.bloblen]=[0,1,2,3,4].map(i=>v.getUint32(D.base+4*i,false));
  D.blob_at=D.base+20;D.blob=d.slice(D.blob_at,D.blob_at+D.bloblen);D.nodes_at=D.blob_at+D.bloblen;D.nodes=[];
  for(let i=0;i<D.nnodes;i++){const q=D.nodes_at+i*12;D.nodes.push([v.getUint16(q,false),v.getUint16(q+2,false),v.getInt32(q+4,false),v.getUint16(q+8,false),v.getUint16(q+10,false)]);}
  const p=D.nodes_at+12*D.nnodes;D.pre=d.slice(p,p+12);D.nrec=v.getUint32(p+8,false);D.rec_at=p+12;D.recs=[];
  for(let i=0;i<D.nrec;i++)D.recs.push(d.slice(D.rec_at+64*i,D.rec_at+64*i+64));
  D.after=D.rec_at+64*D.nrec;D.hdr_end=v.getUint32(0,false)+4;
  D.name=(i)=>{const off=D.nodes[i][2];if(off<0)return '';const e=D.blob.indexOf(0,off);return latin(D.blob,off,e);};
  D.path=(i)=>{const parts=[],seen=new Set();while(i&&!seen.has(i)){seen.add(i);parts.push(D.name(i));i=D.nodes[i][3];}return parts.reverse().join('/');};
  D.recNode=(k)=>dv(D.recs[k]).getUint32(4,false);
  return D;}
function addDependencies(d,entries){const D=Deps(d);let blob=[...D.blob];const nodes=D.nodes.map(n=>n.slice());let nfiles=D.nfiles;const recs=D.recs.slice();
  const nameAt=(off)=>{let e=off;while(blob[e]!==0)e++;return String.fromCharCode(...blob.slice(off,e));};
  const child=(par,name)=>{for(let i=1;i<nodes.length;i++){const n=nodes[i];if(n[3]==par&&n[2]>=0&&nameAt(n[2])==name)return i;}return null;};
  for(const [path,rec] of entries){let par=0;const parts=path.split('/');
    parts.forEach((part,k)=>{let c=child(par,part);
      if(c==null){const isFile=k==parts.length-1;const off=blob.length;blob.push(...enc(part),0,0);
        c=nodes.length;nodes.push([0,nodes[par][0],off,par,isFile?nfiles:0]);nodes[par][0]=c;if(isFile)nfiles++;}
      par=c;});
    const r=rec.slice();dv(r).setUint32(4,par,false);recs.push(r);}
  const head=new Uint8Array(20);const hv=dv(head);[D.one,D.two,nfiles,nodes.length,blob.length].forEach((x,i)=>hv.setUint32(4*i,x,false));
  const nb=new Uint8Array(12*nodes.length);const nv=dv(nb);
  nodes.forEach((n,i)=>{nv.setUint16(i*12,n[0],false);nv.setUint16(i*12+2,n[1],false);nv.setInt32(i*12+4,n[2],false);nv.setUint16(i*12+8,n[3],false);nv.setUint16(i*12+10,n[4],false);});
  const out=cat([d.slice(0,D.base),head,new Uint8Array(blob),nb,D.pre.slice(0,8),u32be(recs.length),...recs,d.slice(D.after,D.hdr_end)]);
  dv(out).setUint32(0,out.length-4,false);
  return cat([out,d.slice(D.hdr_end)]);}
function dropRecords(d,keepFn){const N=Deps(d);const keep=[];for(let k=0;k<N.nrec;k++)if(keepFn(N.path(N.recNode(k))))keep.push(k);
  if(keep.length==N.nrec)return d;
  const out=cat([d.slice(0,N.rec_at-4),u32be(keep.length),...keep.map(k=>N.recs[k]),d.slice(N.after)]);
  dv(out).setUint32(0,dv(d).getUint32(0,false)-64*(N.nrec-keep.length),false);return out;}

// ---------- reading a character
const handleLen=(d,p)=>d[p+4]?5+16+4+dv(d).getUint32(p+21,true):5;
const handlePath=(d,p)=>{if(!d[p+4])return null;const L=dv(d).getUint32(p+21,true);return latin(d,p+25,p+25+L-1).replace(/\0+$/,'');};
const handleBytes=(path,id)=>{const b=enc(path+'\0');return cat([new Uint8Array([2,0,0,0,1]),id,u32le(b.length),b]);};
const EMPTY_HANDLE=new Uint8Array([2,0,0,0,0]);
const SLOT_FIELDS=['diffuse_0','diffuse_1','normal_0','specular_0'];
// body material ids of the standard minifig (super_minifig_dx11.ghg)
const BODY_TEX_IDS={head:[1]};   // the head print sits on material 1 only
const BODY_IDS={head:[1,2],body_front:[3],body_back:[4],arm_l:[5],arm_r:[6],hand_l:[7],hand_r:[8],leg_l:[9],leg_r:[10],hips:[11]};
function selfPath(d){const n=dv(d).getUint16(20,false);let e=22+n;while(e>22&&d[e-1]===0)e--;return latin(d,22,e);}
function srgb(c){c=Math.max(0,Math.min(1,c));return Math.round(255*(c<=0.0031308?12.92*c:1.055*Math.pow(c,1/2.4)-0.055));}
function lin(v){v/=255;return v<=0.04045?v/12.92:Math.pow((v+0.055)/1.055,2.4);}

function readCharacter(d){
  const S=Schema(d);const {O,olo}=objtable(d,S);const D=new Dec(d,S);const v=dv(d);const hdrEnd=v.getUint32(0,false)+4;
  const sp=selfPath(d);const file=sp.split('/').pop();const isScene=/\.scene_baked$/i.test(file);
  const baseName=file.replace(/_cheapminifig\.scene_baked$/i,'').replace(/\.(prefab|scene)_baked$/i,'');
  const renders=[];
  // every drawn model of the character: the body, its attachments (nested ones too) and extra models on the root; not items, abilities, damage stages or locators
  for(const x of O){if(x.class!='nttRenderTTExportObject')continue;if(/^(Items|Abilities|Locators)|Damage|IngameAnimation/i.test(x.path))continue;
    const T=D.traceOf(x);if(D.bad)continue;
    const r={entity:x.path,key:x.path||'#'+x.i,obj:x,model:null,special:null,overrides:[]};let cur=null,inOv=false;
    for(const [n,k,p] of T){
      if(n=='Model Resource'&&r.model==null)r.model={pos:p,path:handlePath(d,p)};
      else if(n=='Special Name'&&r.special==null){const L=v.getUint32(p,true);r.special={pos:p,len:4+L,value:latin(d,p+4,p+4+Math.max(0,L-1))};}
      else if(n=='Material State Overrides')inOv=true;
      else if(n=='ID'&&inOv){inOv=false;cur={id:v.getUint16(p,true),slots:{}};r.overrides.push(cur);}
      else if(cur&&n=='colour_tint_enabled_0')cur.tintEn=p;
      else if(cur&&n=='alpha_tint_enabled_0')cur.alphaEn=p;
      else if(cur&&n=='alpha_tint_0')cur.alpha=p;
      else if(cur&&n=='colour_tint_0'){cur.tint=p;cur.colour=[0,1,2].map(i=>v.getFloat32(p+4*i,true));}
      else if(cur&&k=='handle'&&SLOT_FIELDS.includes(n))cur.slots[n]={pos:p,path:handlePath(d,p)};
      else if(n=='Normal Strength')cur=null;}
    if(!r.model||!r.model.path)continue;
    const ent=x.path?entityOf(O,x.path):null;
    r.enabled=ent?d[ent.o+17]===1:true;
    const tr=x.path?O.find(y=>y.path==x.path&&y.class=='apiTransformComponent'):null;
    if(tr){const p=tr.o+17+19;r.transform={local:[8,9,10].map(k=>v.getFloat32(p+4*k,true)),world:[20,21,22].map(k=>v.getFloat32(p+4*k,true)),pos:p};}
    if(renders.some(q=>q.key==r.key))r.key+='#'+x.i;
    renders.push(r);}
  // the neck lift some characters carry (an attachment's AttachmentOffsetComponent on the Neck joint raises the head)
  // joint lifts: an attachment's AttachmentOffsetComponent moves a joint of the body (Neck raises the head, SpineBase everything above the hips)
  const offsets=[];
  for(const x of O){if(x.class!='AttachmentOffsetComponent'||/^(Items|Abilities)/.test(x.path))continue;
    const T=D.traceOf(x);if(D.bad)continue;const j=T.find(t=>t[0]=='Joint'),of=T.find(t=>t[0]=='Offset');if(!j||!of)continue;
    const L=v.getUint32(j[2],true);const joint=latin(d,j[2]+4,j[2]+4+Math.max(0,L-1)).replace(/\0+$/,'');
    offsets.push({entity:x.path,joint,pos:of[2],off:[0,1,2].map(i=>v.getFloat32(of[2]+4*i,true))});}
  const n0=offsets.find(o=>o.joint=='Neck');const neck=n0?{entity:n0.entity,pos:n0.pos,y:n0.off[1]}:null;
  // roster link: "Characters" + 16-byte id + the row name
  let roster=null;for(const o of findAll(d,'Characters\0',hdrEnd)){if(v.getUint32(o-4,true)!==11)continue;const L=v.getUint32(o+27,true);
    if(L>1&&L<80){roster={pos:o,lenPos:o+27,name:latin(d,o+31,o+31+L-1)};break;}}
  const body=renders.find(r=>r.entity==''&&/super_(minifig|minifig_customiser|small)_dx11/i.test(r.model.path))||null;   // minifig-shaped bodies (standard, customiser, small); others (droids…) are shown as parts
  return {S,O,D,olo,selfPath:sp,file,isScene,baseName,renders,roster,hdrEnd,neck,offsets,
    body,attachments:renders.filter(r=>r!==body)};}
// the apiEntity object of a full entity path ("Parent/Child")
function entityOf(O,full){return O.find(y=>y.class=='apiEntity'&&(y.path?y.path+'/':'')+y.entity==full);}

// ---------- texture files
function bc7size(w,h,m){let s=0;for(let i=0;i<m;i++)s+=Math.max(1,Math.ceil(Math.max(1,w>>i)/4))*Math.max(1,Math.ceil(Math.max(1,h>>i)/4))*16;return s;}
function retargetTexture(tex,gamePath){   // gamePath like Chars/Super_Character_Texture/BODY/BODY_X_DX11.texture
  const v=dv(tex);const n=v.getUint16(20,false);const hdr=v.getUint32(0,false);const sp=enc(gamePath+'\0');const tid=rand16();
  const out=cat([tex.slice(0,20),new Uint8Array([sp.length>>8,sp.length&255]),sp,tid,tex.slice(22+n+16)]);
  dv(out).setUint32(0,hdr+sp.length-n,false);
  const c=dv(out).getUint32(0,false)+4;const r=find(out,'RTXT',c);if(r<0)throw new Error('not a game texture (no RTXT block)');
  out.set(rand16(),r+46);return {data:out,id:tid};}
function wrapBC7(template,w,h,mips,pix){   // template: a game BC7 texture header (everything before the pixel data)
  const t=template;const c1=dv(t).getUint32(0,false)+4;const rt=find(t,'RTXT',c1);
  const out=cat([t,pix]);const ov=dv(out);ov.setUint32(rt+8,w,false);ov.setUint32(rt+12,h,false);ov.setUint32(rt+20,mips,false);
  ov.setUint32(c1,out.length-c1-4,false);return out;}
// BC7 mode 6 encoder (one subset, RGBA 7.7.7.7 + p-bit, 4-bit indices): port of bc7enc.py
function encodeBC7Level(px,w,h){const bw=Math.max(1,Math.ceil(w/4)),bh=Math.max(1,Math.ceil(h/4));const out=new Uint8Array(bw*bh*16);const blk=new Float32Array(64);
  const q=(c)=>{let best=null;for(const pb of [0,1]){const vv=[],rr=[];let err=0;for(let k=0;k<4;k++){const cc=Math.max(0,Math.min(255,Math.round(c[k])));const x=Math.max(0,Math.min(127,Math.round((cc-pb)/2)));vv.push(x);rr.push(x*2+pb);err+=(x*2+pb-cc)**2;}
      if(!best||err<best.err)best={v:vv,p:pb,r:rr,err};}return best;};
  for(let by=0;by<bh;by++)for(let bx=0;bx<bw;bx++){
    const lo=[255,255,255,255],hi=[0,0,0,0];
    for(let i=0;i<16;i++){const X=Math.min(w-1,bx*4+(i&3)),Y=Math.min(h-1,by*4+(i>>2));const s=(Y*w+X)*4;
      for(let k=0;k<4;k++){const c=px[s+k];blk[i*4+k]=c;if(c<lo[k])lo[k]=c;if(c>hi[k])hi[k]=c;}}
    let e0=q(lo),e1=q(hi);const dd=[0,1,2,3].map(k=>e1.r[k]-e0.r[k]);let den=dd.reduce((s,x)=>s+x*x,0)||1;
    const idx=[];for(let i=0;i<16;i++){let t=0;for(let k=0;k<4;k++)t+=(blk[i*4+k]-e0.r[k])*dd[k];idx.push(Math.max(0,Math.min(15,Math.round(t/den*15))));}
    if(idx[0]>=8){[e0,e1]=[e1,e0];for(let i=0;i<16;i++)idx[i]=15-idx[i];}
    const bits=[[1<<6,7]];for(let k=0;k<4;k++)bits.push([e0.v[k],7],[e1.v[k],7]);bits.push([e0.p,1],[e1.p,1],[idx[0],3]);for(let i=1;i<16;i++)bits.push([idx[i],4]);
    const o=(by*bw+bx)*16;let pos=0;for(const [val,n] of bits){for(let b=0;b<n;b++){if((val>>b)&1){const P=pos+b;out[o+(P>>3)]|=1<<(P&7);}}pos+=n;}}
  return out;}
function halve(px,w,h){const W=Math.max(1,w>>1),H=Math.max(1,h>>1);const o=new Uint8ClampedArray(W*H*4);
  for(let y=0;y<H;y++)for(let x=0;x<W;x++)for(let k=0;k<4;k++){let s=0,n=0;for(const [dx,dy] of [[0,0],[1,0],[0,1],[1,1]]){const X=Math.min(w-1,2*x+dx),Y=Math.min(h-1,2*y+dy);s+=px[(Y*w+X)*4+k];n++;}o[(y*W+x)*4+k]=Math.round(s/n);}
  return [o,W,H];}
function encodeBC7(px,w,h){const levels=[];let cur=px,cw=w,ch=h,m=0;
  while(true){levels.push(encodeBC7Level(cur,cw,ch));m++;if(cw==1&&ch==1)break;[cur,cw,ch]=halve(cur,cw,ch);}
  return {mips:m,data:cat(levels)};}

// ---------- applying edits to a prefab or scene
function apply(d0,plan,ctx){
  const C=readCharacter(d0);const d=d0.slice();const v=dv(d);const edits=[];const log=[];
  const same=[];   // same-size writes
  const putHandle=(p,path,id)=>edits.push([p,handleLen(d,p),path==null?EMPTY_HANDLE:handleBytes(path,id)]);
  const setTint=(ov,rgb)=>{if(ov.tintEn==null)return;d[ov.tintEn]=1;rgb.forEach((c,i)=>v.setFloat32(ov.tint+4*i,c,true));};
  // body
  if(C.body&&plan.body){for(const [slot,ch] of Object.entries(plan.body)){
    for(const ov of C.body.overrides.filter(o=>(BODY_IDS[slot]||[]).includes(o.id))){
      if(ch.tint)setTint(ov,ch.tint);
      if(BODY_TEX_IDS[slot]&&!BODY_TEX_IDS[slot].includes(ov.id))continue;
      for(const [field,tex] of Object.entries(ch.tex||{})){const s=ov.slots[field];if(!s)continue;putHandle(s.pos,tex&&tex.path,tex&&tex.id);}}}}
  // attachments
  const modelSwaps=[];
  for(const pc of plan.parts||[]){const r=C.renders.find(a=>a.key==pc.entity);if(!r){log.push('no '+pc.entity+' in '+C.file);continue;}
    const ent=r.entity?entityOf(C.O,r.entity):null;
    if(pc.enabled===false&&!ent){if(d[r.obj.o+17]<=1)d[r.obj.o+17]=0;continue;}
    if(pc.enabled===false){for(const x of C.O){const inside=x===ent||x.path==r.entity||x.path.startsWith(r.entity+'/');
        if(inside&&(x.class=='apiEntity'||C.D.isComp(x.ci))&&(d[x.o+17]===0||d[x.o+17]===1))d[x.o+17]=0;}continue;}
    if(pc.model){const old=r.model.path;const rg=ent||r.obj;
      for(const p of findAll(d0,enc(old+'\0'),rg.o,rg.end)){const st=p-4-16-1-4;if(d0[st]==2&&d0[st+1]==0&&d0[st+4]==1&&v.getUint32(p-4,true)>=old.length+1)
        edits.push([st,handleLen(d0,st),handleBytes(pc.model.path,d0.slice(st+5,st+21))]);}
      modelSwaps.push([old,pc.model.path]);}
    if(pc.special!=null&&r.special){const b=enc(pc.special+'\0');edits.push([r.special.pos,r.special.len,cat([u32le(b.length),b])]);}
    // material 0 is the model's unused default when the model has more: write the real materials
    const real=r.overrides.filter(o=>o.id>=1);
    for(const e of pc.mats||[]){const ov=r.overrides.find(o=>o.id===e.id);if(!ov)continue;if(e.tint)setTint(ov,e.tint);
      for(const [field,tex] of Object.entries(e.tex||{})){const sl=ov.slots[field];if(!sl||(!tex&&!sl.path))continue;putHandle(sl.pos,tex&&tex.path,tex&&tex.id);}}
    if(!pc.mats)for(const ov of (real.length?real:r.overrides)){if(pc.tint)setTint(ov,pc.tint);
      for(const [field,tex] of Object.entries(pc.tex||{})){const s=ov.slots[field];if(!s)continue;
        if(!tex&&!s.path)continue;putHandle(s.pos,tex&&tex.path,tex&&tex.id);}}
    if(pc.place&&r.transform){const p=r.transform.pos;pc.place.forEach((c,i)=>{const dl=c-r.transform.local[i];v.setFloat32(p+4*(8+i),c,true);v.setFloat32(p+4*(20+i),r.transform.world[i]+dl,true);});}}
  if(plan.neckLift!=null&&C.neck)v.setFloat32(C.neck.pos+4,plan.neckLift,true);
  // own name and roster link (names keep the base's length, so the header name tree is untouched)
  const B=C.baseName.toLowerCase(),N=plan.internalName.toLowerCase();if(N.length!=B.length)throw new Error('internal name must have '+B.length+' letters');
  const lower=d.map(c=>c>=65&&c<=90?c+32:c);
  for(const suf of ['.prefab\0','_character.prefab\0','_cheapminifig.scene\0']){const pat=enc('/'+B+suf);
    for(const o of findAll(lower,pat))d.set(enc('/'+N),o);}
  const srcName=enc('/'+N+(C.isScene?'_cheapminifig.scene\0':'.prefab\0'));const so=find(d,srcName);
  if(so>0){const oid=d.slice(so+srcName.length,so+srcName.length+16);const rid=rand16();for(const o of findAll(d,oid))d.set(rid,o);}
  if(C.roster&&plan.rosterRow){const b=enc(plan.rosterRow+'\0');d.fill(0,C.roster.pos+11,C.roster.pos+27);edits.push([C.roster.lenPos,4+v.getUint32(C.roster.lenPos,true),cat([u32le(b.length),b])]);}
  // size-changing edits, highest position first, growing every enclosing object and the object list
  edits.sort((a,b)=>b[0]-a[0]);
  for(let i=1;i<edits.length;i++)if(edits[i][0]+edits[i][1]>edits[i-1][0])throw new Error('overlapping edits');
  let out=d;
  const sizes=new Map();let olsz=v.getUint32(C.olo,true);
  for(const [pos,oldLen,nb] of edits){const delta=nb.length-oldLen;
    if(delta){for(const x of C.O)if(x.o<pos&&pos<x.end)sizes.set(x.o,(sizes.has(x.o)?sizes.get(x.o):v.getUint32(x.o,true))+delta);olsz+=delta;}}
  const chunks=[];let at=d.length;
  for(const [pos,oldLen,nb] of edits){chunks.push(d.slice(pos+oldLen,at),nb);at=pos;}
  chunks.push(d.slice(0,at));out=cat(chunks.reverse());
  // an object's size field moves by the growth of the edits before it
  const shift=(o)=>{let s=0;for(const [pos,oldLen,nb] of edits)if(pos<o)s+=nb.length-oldLen;return s;};
  const ov=dv(out);for(const [o,s] of sizes)ov.setUint32(o+shift(o),s,true);ov.setUint32(C.olo,olsz,true);
  // self path and id
  {const n=ov.getUint16(20,false);const sp=C.selfPath;const nsp=sp.slice(0,sp.length-C.file.length)+C.file.replace(new RegExp('^'+C.baseName,'i'),plan.internalName);
    const b=enc(nsp+'\0');const hdr=ov.getUint32(0,false);
    out=cat([out.slice(0,20),new Uint8Array([b.length>>8,b.length&255]),b,rand16(),out.slice(22+n+16)]);dv(out).setUint32(0,hdr+b.length-n,false);
    C.newSelfPath=nsp;}
  // header load list: new textures, swapped models
  const H=Deps(out);const paths=[];for(let k=0;k<H.nrec;k++)paths.push(H.path(H.recNode(k)));
  const entries=[];
  for(const t of ctx.textures){if(!paths.includes(t.path)){const r=new Uint8Array(64);const rv=dv(r);[36,0,0,0xffffffff,0,0,0xffffffff].forEach((x,i)=>rv.setUint32(4*i,x,false));r.set(t.id,28);rv.setUint32(44,2,false);entries.push([t.path,r]);}}
  const dropOld=new Set();
  for(const [oldp,newp] of modelSwaps){for(const [a,b] of [[oldp,newp],[oldp.replace(/_dx11\./,'_nxg.'),newp.replace(/_dx11\./,'_nxg.')]]){
      const k=paths.indexOf(a);if(k<0||paths.includes(b))continue;entries.push([b,H.recs[k]]);
      if(find(out,enc(oldp+'\0'),ov.getUint32(0,false)+4)<0)dropOld.add(a);}}
  if(entries.length)out=addDependencies(out,entries);
  if(dropOld.size)out=dropRecords(out,(p)=>!dropOld.has(p));
  return {data:out,selfPath:C.newSelfPath,log};}

// ---------- check: every top-level object decodes to its exact size
function verify(d){const C=readCharacter(d);const D=new Dec(d,C.S);let bad=0;
  for(const x of C.O){if(x.i==0||x.class=='ROOT')continue;if(x.path.includes('/'))continue;D.traceOf(x);if(D.bad)bad++;}
  return {objects:C.O.length,bad,renders:C.renders.length};}

// ---------- ZIP (stored)
const CRC=(()=>{const t=new Uint32Array(256);for(let n=0;n<256;n++){let c=n;for(let k=0;k<8;k++)c=c&1?0xEDB88320^(c>>>1):c>>>1;t[n]=c>>>0;}return t;})();
function crc32(b){let c=0xffffffff;for(let i=0;i<b.length;i++)c=CRC[(c^b[i])&255]^(c>>>8);return (c^0xffffffff)>>>0;}
function zip(files){const local=[],central=[];let off=0;
  for(const [name,data] of files){const nb=enc(name);const crc=crc32(data);
    const h=new Uint8Array(30);const hv=dv(h);hv.setUint32(0,0x04034b50,true);hv.setUint16(4,20,true);hv.setUint16(6,0x800,true);hv.setUint32(14,crc,true);hv.setUint32(18,data.length,true);hv.setUint32(22,data.length,true);hv.setUint16(26,nb.length,true);
    local.push(h,nb,data);
    const c=new Uint8Array(46);const cv=dv(c);cv.setUint32(0,0x02014b50,true);cv.setUint16(4,20,true);cv.setUint16(6,20,true);cv.setUint16(8,0x800,true);cv.setUint32(16,crc,true);cv.setUint32(20,data.length,true);cv.setUint32(24,data.length,true);cv.setUint16(28,nb.length,true);cv.setUint32(42,off,true);
    central.push(c,nb);off+=30+nb.length+data.length;}
  const cd=cat(central);const e=new Uint8Array(22);const ev=dv(e);ev.setUint32(0,0x06054b50,true);ev.setUint16(8,files.length,true);ev.setUint16(10,files.length,true);ev.setUint32(12,cd.length,true);ev.setUint32(16,off,true);
  return cat([...local,cd,e]);}

// part names a model offers ("$Name" entries), for the render object's Special Name
function specialNames(m){const out=[];for(let i=0;i<m.length-2;i++){if(m[i]===36&&(i==0||m[i-1]===0)){let e=i+1;while(e<m.length&&e-i<64&&((m[e]>=48&&m[e]<=57)||(m[e]>=65&&m[e]<=90)||(m[e]>=97&&m[e]<=122)||m[e]===95))e++;
  if(m[e]===0&&e-i>3){const s=latin(m,i+1,e);if(!out.includes(s))out.push(s);}}}return out;}

return {Schema,objtable,Dec,Deps,readCharacter,apply,verify,retargetTexture,wrapBC7,encodeBC7,bc7size,zip,crc32,selfPath,srgb,lin,BODY_IDS,BODY_TEX_IDS,specialNames,enc,find,cat,rand16};
})();
if(typeof module!='undefined')module.exports=PF;
