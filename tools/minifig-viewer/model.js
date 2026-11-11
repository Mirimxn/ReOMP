// Skywalker Saga GSC / GHG mesh reader (HSEM chunk, version 0xC9)
const VAR_POS=0,VAR_UV01=5,VAR_BLEND=9;
function halfToFloat(h){const s=(h&0x8000)?-1:1,e=(h>>10)&31,f=h&1023;if(e==0)return s*f*Math.pow(2,-24);if(e==31)return f?NaN:s*Infinity;return s*(1+f/1024)*Math.pow(2,e-15);}
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
    q+=44;ref+=2;parts.push({list:pl[0],idx:ix,ioff,icnt,voff,vcnt});
  }
  const has=(L,v)=>L.defs.some(d=>d[0]==v);
  const textured=Object.keys(lists).map(Number).filter(k=>has(lists[k],VAR_UV01));
  let use=textured.length?textured:Object.keys(lists).map(Number);
  if(use.some(k=>has(lists[k],VAR_BLEND)))use=use.filter(k=>has(lists[k],VAR_BLEND));   // skinned model: skip posed copies
  const readPos=(L,j)=>{const d=L.defs.find(x=>x[0]==VAR_POS);const o=L.data+j*L.stride+d[2];
    if(d[1]==6||d[1]==5)return [halfToFloat(dv.getUint16(o,true)),halfToFloat(dv.getUint16(o+2,true)),d[1]==6?halfToFloat(dv.getUint16(o+4,true)):0];
    return [dv.getFloat32(o,true),dv.getFloat32(o+4,true),dv.getFloat32(o+8,true)];};
  const readUV=(L,j,set)=>{const d=L.defs.find(x=>x[0]==VAR_UV01);if(!d)return [0,0];const o=L.data+j*L.stride+d[2];
    const k=(set==1&&d[1]==6)?4:0;return [halfToFloat(dv.getUint16(o+k,true)),halfToFloat(dv.getUint16(o+k+2,true))];};
  // one copy of each piece: detail levels repeat a piece with the same bounds, keep the most detailed
  const groups=[];
  for(const P of parts){if(!use.includes(P.list)||!idxs[P.idx]||P.icnt<3)continue;const L=lists[P.list];
    const mn=[1e9,1e9,1e9],mx=[-1e9,-1e9,-1e9];for(let j=P.voff;j<P.voff+P.vcnt;j+=Math.max(1,P.vcnt>>6)){const p=readPos(L,j);for(let c=0;c<3;c++){mn[c]=Math.min(mn[c],p[c]);mx[c]=Math.max(mx[c],p[c]);}}
    const same=(g)=>[0,1,2].every(c=>{const ca=(g.mn[c]+g.mx[c])/2,cb=(mn[c]+mx[c])/2,ea=g.mx[c]-g.mn[c],eb=mx[c]-mn[c];
      return Math.abs(ca-cb)<0.012&&Math.abs(ea-eb)<=0.2*Math.max(ea,eb)+0.004;});
    const g=groups.find(g=>g.list==P.list&&same(g));
    if(!g)groups.push({list:P.list,mn,mx,part:P});else if(P.icnt>g.part.icnt)g.part=P;}
  const hasUV1=use.some(k=>{const d=lists[k].defs.find(x=>x[0]==VAR_UV01);return d&&d[1]==6;});
  const pieces=groups.map(g=>{const P=g.part,L=lists[P.list],I=idxs[P.idx];
    const pos=new Float32Array(P.vcnt*3),uv0=new Float32Array(P.vcnt*2),uv1=new Float32Array(P.vcnt*2);
    for(let j=0;j<P.vcnt;j++){const p=readPos(L,P.voff+j);pos.set(p,j*3);uv0.set(readUV(L,P.voff+j,0),j*2);uv1.set(readUV(L,P.voff+j,1),j*2);}
    const idx=new Uint16Array(P.icnt);for(let k=0;k<P.icnt;k++)idx[k]=dv.getUint16(I.data+2*(P.ioff+k),true);
    return {pos,uv0,uv1,idx,mn:g.mn,mx:g.mx};});
  const skinned=use.some(k=>has(lists[k],VAR_BLEND));
  return {pieces,skinned,hasUV1};
}
if(typeof module!='undefined')module.exports={parseModel};
