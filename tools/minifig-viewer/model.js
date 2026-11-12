// Skywalker Saga GSC / GHG mesh reader (HSEM chunk, version 0xC9)
const VAR_POS=0,VAR_UV01=5,VAR_BLEND=9;
function halfToFloat(h){const s=(h&0x8000)?-1:1,e=(h>>10)&31,f=h&1023;if(e==0)return s*f*Math.pow(2,-24);if(e==31)return f?NaN:s*Infinity;return s*(1+f/1024)*Math.pow(2,e-15);}
// skeleton (LOGH): bone names and parents, then a table of local bind matrices (row-vector, translation in the last row);
// world = local x parent world. Returned matrices have the same memory layout a WebGL model matrix uses.
function parseSkeleton(u8,dv){let o=-1;for(let i=0;i<u8.length-4;i++)if(u8[i]==0x4c&&u8[i+1]==0x4f&&u8[i+2]==0x47&&u8[i+3]==0x48){o=i;break;}
  if(o<0)return null;try{const n=dv.getUint32(o+12,false);if(!n||n>512)return null;let p=o+16;const bones=[];
    for(let i=0;i<n;i++){const L=dv.getUint16(p,false);let name='';for(let k=0;k<L-1;k++)name+=String.fromCharCode(u8[p+2+k]);p+=2+L+64+12;const par=u8[p];p+=2;bones.push({name,parent:par==0xff?-1:par});}
    if(dv.getUint32(p+4,false)!==n)return null;p+=8;
    for(let i=0;i<n;i++){const l=new Float32Array(16);for(let k=0;k<16;k++)l[k]=dv.getFloat32(p+64*i+4*k,false);const b=bones[i];
      b.world=b.parent>=0&&b.parent<i?mul4(l,bones[b.parent].world):l;}
    return bones;}catch(e){return null;}}
function mul4(a,b){const o=new Float32Array(16);for(let r=0;r<4;r++)for(let c=0;c<4;c++){let s=0;for(let k=0;k<4;k++)s+=a[r*4+k]*b[k*4+c];o[r*4+c]=s;}return o;}
const findTag=(u8,t,from)=>{for(let i=from||0;i<u8.length-4;i++)if(u8[i]==t.charCodeAt(0)&&u8[i+1]==t.charCodeAt(1)&&u8[i+2]==t.charCodeAt(2)&&u8[i+3]==t.charCodeAt(3))return i;return -1;};
// material names in LTMU order without the ":VARIANT_AUTO" copies: the draw list numbers materials in this order
function materialNames(u8,dv){const o=findTag(u8,'LTMU');if(o<0)return [];const out=[];const end=Math.min(u8.length,o+4000000);
  for(let i=o+16;i<end-4;i++){if(u8[i]!==1||u8[i+1]!==0||u8[i+2]!==0)continue;const L=u8[i+3];if(L<3||L>80)continue;let ok=true,s='';
    for(let k=0;k<L-1;k++){const c=u8[i+4+k];if(c<32||c>126){ok=false;break;}s+=String.fromCharCode(c);}
    if(!ok||u8[i+3+L]!==0||!/^[A-Za-z_]/.test(s))continue;if(!s.includes(':'))out.push(s);i+=3+L;
    if(findTag(u8,'HSEM',i)===i)break;}
  return out;}
// PSID: one entry per mesh part, "[01 00] values… key 0 key part" then a 4-byte marker (87 ?? b3 00);
// one value = the material, two values = (count, material); a number past the material list keeps the previous material
function partMaterials(u8,dv,nparts,nmats){const o=findTag(u8,'PSID');if(o<0||!nmats)return null;const res=new Array(nparts).fill(-1);
  let s=o+20,prev=-1;const lim=Math.min(u8.length-8,o+20+nparts*40+64);
  for(let i=s;i<lim;i++){if(!(u8[i]===0x87&&u8[i+2]===0xb3&&u8[i+3]===0))continue;
    const e=i;if(e-s<8){s=i+4;continue;}const part=dv.getUint16(e-2,false);let a=s;if(u8[a]===1&&u8[a+1]===0)a+=2;
    const vals=[];for(let k=a;k+1<e-8;k+=2)vals.push(dv.getUint16(k,false));
    let m=vals.length?vals[vals.length-1]:-1;if(m<0||m>=nmats)m=prev;if(part<nparts){res[part]=m;prev=m;}s=i+4;i+=3;}
  return res;}
// named meshes after the PSID draw list: name and bounds in the space of the bone the mesh hangs on
function meshTable(u8,dv){const o=findTag(u8,'PSID');if(o<0)return [];const end=Math.min(u8.length,o+400000);const out=[];
  for(let i=o;i<end-80;i++){const L=u8[i];if(L<5||L>48||u8[i-1]!==0)continue;let s='',ok=true;
    for(let k=0;k<L-1;k++){const c=u8[i+1+k];if(!((c>=48&&c<=57)||(c>=65&&c<=90)||(c>=97&&c<=122)||c==95)){ok=false;break;}s+=String.fromCharCode(c);}
    if(!ok||u8[i+L]!==0||!/^[A-Za-z]/.test(s)||!/(Mesh|BreakUp)$/.test(s))continue;
    let k=i+40;for(;k<i+140;k++)if(u8[k]==0x3f&&u8[k+1]==0x80&&u8[k+2]==0&&u8[k+3]==0)break;if(k>=i+140)continue;
    const f=(p)=>dv.getFloat32(p,false);const mn=[f(k-12),f(k-8),f(k-4)],mx=[f(k+4),f(k+8),f(k+12)];
    if(![...mn,...mx].every(v=>Math.abs(v)<10))continue;out.push({name:s,mn,mx});i=k+16;}
  return out;}
// models built from named rigid meshes on their own skeleton (astromechs): keep the pieces of the drawn meshes,
// leave out break-up copies and loose bricks, and tag the alternative tops (Top_A, Top_B…) so a character can pick one
function namedPieces(all,table,bones){if(!table.length||!bones)return null;
  const offs=bones.filter(b=>b.parent>=0).map(b=>[b.world[12],b.world[13],b.world[14]]).filter(o=>o.some(v=>Math.abs(v)>1e-4));const out=[];
  // a table entry also stands for its mirror image (left and right parts)
  const ents=[];for(const t of table){ents.push(t);ents.push({name:t.name,mn:[-t.mx[0],t.mn[1],t.mn[2]],mx:[-t.mn[0],t.mx[1],t.mx[2]]});}
  for(const pc of all){const mn=[1e9,1e9,1e9],mx=[-1e9,-1e9,-1e9];for(let j=0;j<pc.pos.length;j+=3)for(let c=0;c<3;c++){const v=pc.pos[j+c];if(v<mn[c])mn[c]=v;if(v>mx[c])mx[c]=v;}
    // skinned pieces are listed in model space; rigid ones relative to the bone they hang on
    const os=pc.skin?[[0,0,0]]:offs;let best=null,be=0.004;
    for(const t of ents)for(const o of os){let e=0;for(let c=0;c<3;c++)e=Math.max(e,Math.abs(t.mn[c]+o[c]-mn[c]),Math.abs(t.mx[c]+o[c]-mx[c]));
      if(/BreakUp/i.test(t.name))e+=0.003;   // a drawn mesh wins a tie with its break-up copy
      if(e<be){be=e;best=t;}}
    if(!best||/BreakUp|^Brick/i.test(best.name))continue;const v=/^Top_([A-Z])_/i.exec(best.name);
    const variant=v?v[1].toUpperCase():null;
    // one copy of each mesh (detail levels) and of each top
    const o=out.find(q=>variant?q.variant==variant:(q.mesh==best.name&&[0,1,2].every(c=>Math.abs(q.bmn[c]-mn[c])<0.003&&Math.abs(q.bmx[c]-mx[c])<0.003)));
    if(o){const better=(pc.mat>=0)-(o.mat>=0)||pc.pos.length-o.pos.length;if(better<=0)continue;out.splice(out.indexOf(o),1);}
    out.push(Object.assign(pc,{mesh:best.name,variant,bmn:mn,bmx:mx}));}
  return out.length?out:null;}
function parseModel(buf){
  const u8=new Uint8Array(buf),dv=new DataView(buf);const be=(p)=>dv.getUint32(p,false);
  const find=(s,from)=>{const c=[...s].map(ch=>ch.charCodeAt(0));outer:for(let i=from||0;i<u8.length-c.length;i++){for(let k=0;k<c.length;k++)if(u8[i+k]!=c[k])continue outer;return i;}return -1;};
  const SS=[0,0,8,12,16,4,8,4,4,4];
  const h=find('HSEM');if(h<0||be(h+4)!=0xC9)throw new Error('no mesh data (HSEM) found: is this a .gsc or .ghg model?');
  let q=h+8;const n=be(q);q+=4;let ref=4;const lists={},idxs={},parts=[];
  for(let i=0;i<n;i++){
    q+=4;if(String.fromCharCode(u8[q],u8[q+1],u8[q+2],u8[q+3])!='SMNR')throw new Error('unexpected mesh layout at part '+i);q+=8;
    const lc=be(q);q+=4;const pl=[];
    for(let l=0;l<lc;l++){const m=be(q);q+=4;
      if(m&0xc0000000){pl.push(m&0xffff);q+=8;continue;}
      const cnt=be(q+4);q+=8;q+=8;const dc=be(q);q+=4;const defs=[];
      for(let k=0;k<dc;k++){defs.push([u8[q],u8[q+1],u8[q+2]]);q+=3;}
      q+=6;const last=defs[defs.length-1];const stride=last[2]+SS[last[1]];
      lists[ref]={defs,stride,count:cnt,data:q};pl.push(ref);ref++;q+=stride*cnt+4;}
    q+=4;const m=be(q);q+=4;let ix;
    if(m&0xc0000000){ix=m&0xffff;q+=4;}else{const ic=be(q+4);q+=12;idxs[ref]={count:ic,data:q};ix=ref;ref++;q+=2*ic;}
    const info=q;const ioff=be(q),icnt=be(q+4),voff=be(q+8),vcnt=be(q+14);
    q=info+22;const num=dv.getInt32(q,false);q+=4;if(num>0)q+=num;const num2=dv.getInt32(q,false);q+=4;if(num2>0)throw new Error('unsupported mesh (relative positions)');
    q+=44;ref+=2;parts.push({lists:pl,idx:ix,ioff,icnt,voff,vcnt});
  }
  // a part's vertex data can be split over several streams (positions in one list, UVs and skin weights in another)
  const listWith=(P,v)=>{for(const k of P.lists){const L=lists[k];if(L&&L.defs.some(d=>d[0]==v))return L;}return null;};
  const has=(P,v)=>!!listWith(P,v);
  // which material each part draws with: LTMU names (variants and shadow impostors aside) and the PSID draw list
  const mats=materialNames(u8,dv);const pm=partMaterials(u8,dv,parts.length,mats.length);parts.forEach((P,i)=>P.mat=pm?pm[i]:-1);
  let use=parts.filter(P=>has(P,VAR_POS));const textured=use.filter(P=>has(P,VAR_UV01));if(textured.length)use=textured;
  let rigidUse=[];
  if(use.some(P=>has(P,VAR_BLEND))){rigidUse=use.filter(P=>!has(P,VAR_BLEND));use=use.filter(P=>has(P,VAR_BLEND));}   // skinned model: the rest are posed copies and break-up pieces
  const readPos=(P,j)=>{const L=listWith(P,VAR_POS);const d=L.defs.find(x=>x[0]==VAR_POS);const o=L.data+j*L.stride+d[2];
    if(d[1]==6||d[1]==5)return [halfToFloat(dv.getUint16(o,true)),halfToFloat(dv.getUint16(o+2,true)),d[1]==6?halfToFloat(dv.getUint16(o+4,true)):0];
    return [dv.getFloat32(o,true),dv.getFloat32(o+4,true),dv.getFloat32(o+8,true)];};
  const readUV=(P,j,set)=>{const L=listWith(P,VAR_UV01);if(!L)return [0,0];const d=L.defs.find(x=>x[0]==VAR_UV01);const o=L.data+j*L.stride+d[2];
    const k=(set==1&&d[1]==6)?4:0;return [halfToFloat(dv.getUint16(o+k,true)),halfToFloat(dv.getUint16(o+k+2,true))];};
  // one copy of each piece: detail levels repeat a piece with the same bounds, keep the most detailed
  const build=(list)=>{const groups=[];
    for(const P of list){if(!idxs[P.idx]||P.icnt<3)continue;const key=P.lists.join(',')+'|'+P.mat;
      const mn=[1e9,1e9,1e9],mx=[-1e9,-1e9,-1e9];for(let j=P.voff;j<P.voff+P.vcnt;j+=Math.max(1,P.vcnt>>6)){const p=readPos(P,j);for(let c=0;c<3;c++){mn[c]=Math.min(mn[c],p[c]);mx[c]=Math.max(mx[c],p[c]);}}
      const same=(g)=>[0,1,2].every(c=>{const ca=(g.mn[c]+g.mx[c])/2,cb=(mn[c]+mx[c])/2,ea=g.mx[c]-g.mn[c],eb=mx[c]-mn[c];
        return Math.abs(ca-cb)<0.012&&Math.abs(ea-eb)<=0.2*Math.max(ea,eb)+0.004;});
      const g=groups.find(g=>(g.key==key||(P.mat<0&&g.key.split('|')[0]==key.split('|')[0]))&&same(g));
      if(!g)groups.push({key,mn,mx,part:P});else if(P.icnt>g.part.icnt)g.part=P;}
    return groups.map(g=>{const P=g.part,I=idxs[P.idx];
      const pos=new Float32Array(P.vcnt*3),uv0=new Float32Array(P.vcnt*2),uv1=new Float32Array(P.vcnt*2);
      for(let j=0;j<P.vcnt;j++){const p=readPos(P,P.voff+j);pos.set(p,j*3);uv0.set(readUV(P,P.voff+j,0),j*2);uv1.set(readUV(P,P.voff+j,1),j*2);}
      const idx=new Uint16Array(P.icnt);for(let k=0;k<P.icnt;k++)idx[k]=dv.getUint16(I.data+2*(P.ioff+k),true);
      return {pos,uv0,uv1,idx,mn:g.mn,mx:g.mx,mat:P.mat};});};
  const hasUV1=use.some(P=>{const L=listWith(P,VAR_UV01);const d=L&&L.defs.find(x=>x[0]==VAR_UV01);return d&&d[1]==6;});
  let pieces=build(use),rigid=build(rigidUse);
  const skinned=use.some(P=>has(P,VAR_BLEND));const bones=parseSkeleton(u8,dv);let variants=null;
  // a skeleton without a minifig's hips: the body is made of named rigid meshes (droids)
  if(bones&&!bones.some(b=>/^hips$/i.test(b.name))&&bones.some(b=>/^body$/i.test(b.name))){const np=namedPieces([...pieces.map(p=>Object.assign(p,{skin:true})),...rigid],meshTable(u8,dv),bones);
    if(np){pieces=np;rigid=[];variants=[...new Set(np.map(p=>p.variant).filter(Boolean))].sort();}}
  return {pieces,rigid,skinned,hasUV1,bones,materials:mats,variants};
}
if(typeof module!='undefined')module.exports={parseModel,mul4};
