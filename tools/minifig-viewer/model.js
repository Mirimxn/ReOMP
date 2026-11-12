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
  let use=parts.filter(P=>has(P,VAR_POS));const textured=use.filter(P=>has(P,VAR_UV01));if(textured.length)use=textured;
  if(use.some(P=>has(P,VAR_BLEND)))use=use.filter(P=>has(P,VAR_BLEND));   // skinned model: skip posed copies
  const readPos=(P,j)=>{const L=listWith(P,VAR_POS);const d=L.defs.find(x=>x[0]==VAR_POS);const o=L.data+j*L.stride+d[2];
    if(d[1]==6||d[1]==5)return [halfToFloat(dv.getUint16(o,true)),halfToFloat(dv.getUint16(o+2,true)),d[1]==6?halfToFloat(dv.getUint16(o+4,true)):0];
    return [dv.getFloat32(o,true),dv.getFloat32(o+4,true),dv.getFloat32(o+8,true)];};
  const readUV=(P,j,set)=>{const L=listWith(P,VAR_UV01);if(!L)return [0,0];const d=L.defs.find(x=>x[0]==VAR_UV01);const o=L.data+j*L.stride+d[2];
    const k=(set==1&&d[1]==6)?4:0;return [halfToFloat(dv.getUint16(o+k,true)),halfToFloat(dv.getUint16(o+k+2,true))];};
  // one copy of each piece: detail levels repeat a piece with the same bounds, keep the most detailed
  const groups=[];
  for(const P of use){if(!idxs[P.idx]||P.icnt<3)continue;const key=P.lists.join(',');
    const mn=[1e9,1e9,1e9],mx=[-1e9,-1e9,-1e9];for(let j=P.voff;j<P.voff+P.vcnt;j+=Math.max(1,P.vcnt>>6)){const p=readPos(P,j);for(let c=0;c<3;c++){mn[c]=Math.min(mn[c],p[c]);mx[c]=Math.max(mx[c],p[c]);}}
    const same=(g)=>[0,1,2].every(c=>{const ca=(g.mn[c]+g.mx[c])/2,cb=(mn[c]+mx[c])/2,ea=g.mx[c]-g.mn[c],eb=mx[c]-mn[c];
      return Math.abs(ca-cb)<0.012&&Math.abs(ea-eb)<=0.2*Math.max(ea,eb)+0.004;});
    const g=groups.find(g=>g.key==key&&same(g));
    if(!g)groups.push({key,mn,mx,part:P});else if(P.icnt>g.part.icnt)g.part=P;}
  const hasUV1=use.some(P=>{const L=listWith(P,VAR_UV01);const d=L&&L.defs.find(x=>x[0]==VAR_UV01);return d&&d[1]==6;});
  const pieces=groups.map(g=>{const P=g.part,I=idxs[P.idx];
    const pos=new Float32Array(P.vcnt*3),uv0=new Float32Array(P.vcnt*2),uv1=new Float32Array(P.vcnt*2);
    for(let j=0;j<P.vcnt;j++){const p=readPos(P,P.voff+j);pos.set(p,j*3);uv0.set(readUV(P,P.voff+j,0),j*2);uv1.set(readUV(P,P.voff+j,1),j*2);}
    const idx=new Uint16Array(P.icnt);for(let k=0;k<P.icnt;k++)idx[k]=dv.getUint16(I.data+2*(P.ioff+k),true);
    return {pos,uv0,uv1,idx,mn:g.mn,mx:g.mx};});
  const skinned=use.some(P=>has(P,VAR_BLEND));
  return {pieces,skinned,hasUV1,bones:parseSkeleton(u8,dv)};
}
if(typeof module!='undefined')module.exports={parseModel,mul4};
