const W2=[0,21,43,64], W3=[0,9,18,27,37,46,55,64], W4=[0,4,9,13,17,21,26,30,34,38,43,47,51,55,60,64];
// mode: [subsets, partBits, rotBits, idxSelBits, colorBits, alphaBits, endpointPBits, sharedPBits, idxBits, idx2Bits]
const BC7M=[[3,4,0,0,4,0,1,0,3,0],[2,6,0,0,6,0,0,1,3,0],[3,6,0,0,5,0,0,0,2,0],[2,6,0,0,7,0,1,0,2,0],
            [1,0,2,1,5,6,0,0,2,3],[1,0,2,0,7,8,0,0,2,2],[1,0,0,0,7,7,1,0,4,0],[2,6,0,0,5,5,1,0,2,0]];
function interp(a,b,i,bits){const w=(bits==2?W2:bits==3?W3:W4)[i];return ((64-w)*a+w*b+32)>>6;}
function decodeBC7Block(src,o,out,stride,ox,oy,W,H){
  let bitpos=0;
  const bits=(n)=>{let v=0;for(let i=0;i<n;i++){const p=bitpos+i;v|=((src[o+(p>>3)]>>(p&7))&1)<<i;}bitpos+=n;return v;};
  let mode=0;while(mode<8&&!bits(1))mode++;
  const px=(x,y,r,g,b,a)=>{const X=ox+x,Y=oy+y;if(X>=W||Y>=H)return;const q=(Y*stride+X)*4;out[q]=r;out[q+1]=g;out[q+2]=b;out[q+3]=a;};
  if(mode>=8){for(let y=0;y<4;y++)for(let x=0;x<4;x++)px(x,y,0,0,0,0);return;}
  const [ns,pb,rb,isb,cb,ab,epb,spb,ib,ib2]=BC7M[mode];
  const part=bits(pb), rot=bits(rb), isel=bits(isb);
  const ne=ns*2; const ep=[];for(let i=0;i<ne;i++)ep.push([0,0,0,255]);
  for(let c=0;c<3;c++)for(let i=0;i<ne;i++)ep[i][c]=bits(cb);
  if(ab)for(let i=0;i<ne;i++)ep[i][3]=bits(ab);
  let cbits=cb,abits=ab;
  if(epb){cbits++;if(ab)abits++;for(let i=0;i<ne;i++){const p=bits(1);for(let c=0;c<3;c++)ep[i][c]=(ep[i][c]<<1)|p;if(ab)ep[i][3]=(ep[i][3]<<1)|p;}}
  if(spb){cbits++;const p0=bits(1),p1=bits(1);for(let i=0;i<ne;i++){const p=i<2?p0:p1;for(let c=0;c<3;c++)ep[i][c]=(ep[i][c]<<1)|p;}}
  for(let i=0;i<ne;i++){for(let c=0;c<3;c++)ep[i][c]=(ep[i][c]<<(8-cbits))|(ep[i][c]>>(2*cbits-8));
    if(ab)ep[i][3]=(ep[i][3]<<(8-abits))|(ep[i][3]>>(2*abits-8));}
  const subset=(i)=>ns==1?0:ns==2?(BC7_SI2[part]>>i)&1:(BC7_SI3[part]>>(2*i))&3;
  const anchor=(i)=>{if(i==0)return true;if(ns==2)return i==BC7_A2[part];if(ns==3)return i==BC7_A32[part]||i==BC7_A33[part];return false;};
  const idx1=[],idx2=[];
  for(let i=0;i<16;i++)idx1.push(bits(anchor(i)?ib-1:ib));
  if(ib2)for(let i=0;i<16;i++)idx2.push(bits(i==0?ib2-1:ib2));
  for(let i=0;i<16;i++){
    const s=subset(i),e0=ep[2*s],e1=ep[2*s+1];let r,g,b,a;
    if(ib2){const ci=isel?idx2[i]:idx1[i],ai=isel?idx1[i]:idx2[i],cbt=isel?ib2:ib,abt=isel?ib:ib2;
      r=interp(e0[0],e1[0],ci,cbt);g=interp(e0[1],e1[1],ci,cbt);b=interp(e0[2],e1[2],ci,cbt);a=interp(e0[3],e1[3],ai,abt);}
    else{r=interp(e0[0],e1[0],idx1[i],ib);g=interp(e0[1],e1[1],idx1[i],ib);b=interp(e0[2],e1[2],idx1[i],ib);a=ab?interp(e0[3],e1[3],idx1[i],ib):255;}
    if(rot==1)[a,r]=[r,a];else if(rot==2)[a,g]=[g,a];else if(rot==3)[a,b]=[b,a];
    px(i&3,i>>2,r,g,b,a);
  }
}
function c565(v){return [((v>>11)&31)*255/31|0,((v>>5)&63)*255/63|0,(v&31)*255/31|0];}
function decodeBC1Block(src,o,out,stride,ox,oy,W,H,noAlpha){
  const c0=src[o]|src[o+1]<<8,c1=src[o+2]|src[o+3]<<8;const a=c565(c0),b=c565(c1);
  const pal=[a.concat(255),b.concat(255)];
  if(c0>c1||noAlpha){pal.push([0,1,2].map(k=>(2*a[k]+b[k])/3|0).concat(255),[0,1,2].map(k=>(a[k]+2*b[k])/3|0).concat(255));}
  else{pal.push([0,1,2].map(k=>(a[k]+b[k])/2|0).concat(255),[0,0,0,0]);}
  for(let i=0;i<16;i++){const idx=(src[o+4+(i>>2)]>>((i&3)*2))&3;const X=ox+(i&3),Y=oy+(i>>2);if(X>=W||Y>=H)continue;
    const q=(Y*stride+X)*4,p=pal[idx];out[q]=p[0];out[q+1]=p[1];out[q+2]=p[2];out[q+3]=p[3];}
}
function decodeBC3Alpha(src,o,out,stride,ox,oy,W,H){
  const a0=src[o],a1=src[o+1];const pal=[a0,a1];
  if(a0>a1)for(let i=1;i<7;i++)pal.push(((7-i)*a0+i*a1)/7|0);else{for(let i=1;i<5;i++)pal.push(((5-i)*a0+i*a1)/5|0);pal.push(0,255);}
  let bitsv=0n;for(let i=0;i<6;i++)bitsv|=BigInt(src[o+2+i])<<BigInt(8*i);
  for(let i=0;i<16;i++){const X=ox+(i&3),Y=oy+(i>>2);if(X>=W||Y>=H)continue;out[(Y*stride+X)*4+3]=pal[Number((bitsv>>BigInt(3*i))&7n)];}
}
function decodeBC2Alpha(src,o,out,stride,ox,oy,W,H){
  for(let i=0;i<16;i++){const X=ox+(i&3),Y=oy+(i>>2);if(X>=W||Y>=H)continue;const v=(src[o+(i>>1)]>>((i&1)*4))&15;out[(Y*stride+X)*4+3]=v*17;}
}
// fmt: 'BC1' | 'BC2' | 'BC3' | 'BC7' | 'RGBA8' | 'BGRA8'
function decodeImage(fmt,src,o,W,H){
  const out=new Uint8ClampedArray(W*H*4);
  if(fmt=='RGBA8'||fmt=='BGRA8'){for(let i=0;i<W*H;i++){const s=o+i*4;if(fmt=='RGBA8'){out[i*4]=src[s];out[i*4+2]=src[s+2];}else{out[i*4]=src[s+2];out[i*4+2]=src[s];}out[i*4+1]=src[s+1];out[i*4+3]=src[s+3];}return out;}
  const bw=Math.max(1,(W+3)>>2),bh=Math.max(1,(H+3)>>2);const bs=fmt=='BC1'?8:16;
  for(let by=0;by<bh;by++)for(let bx=0;bx<bw;bx++){const p=o+(by*bw+bx)*bs;
    if(fmt=='BC7')decodeBC7Block(src,p,out,W,bx*4,by*4,W,H);
    else if(fmt=='BC1')decodeBC1Block(src,p,out,W,bx*4,by*4,W,H,false);
    else{decodeBC1Block(src,p+8,out,W,bx*4,by*4,W,H,true);(fmt=='BC3'?decodeBC3Alpha:decodeBC2Alpha)(src,p,out,W,bx*4,by*4,W,H);}
  }
  return out;
}
function blockBytes(fmt){return fmt=='BC1'?8:fmt=='RGBA8'||fmt=='BGRA8'?0:16;}
function mipSize(fmt,w,h){return fmt=='RGBA8'||fmt=='BGRA8'?w*h*4:Math.max(1,(w+3)>>2)*Math.max(1,(h+3)>>2)*blockBytes(fmt);}
// DDS file -> {w,h,rgba}
function parseDDS(buf){
  const d=new DataView(buf),u8=new Uint8Array(buf);
  if(d.getUint32(0,true)!=0x20534444)throw new Error('not a DDS file');
  const H=d.getUint32(12,true),W=d.getUint32(16,true);const pfFlags=d.getUint32(80,true);
  const four=String.fromCharCode(u8[84],u8[85],u8[86],u8[87]);let fmt,off=128;
  if(pfFlags&4){
    if(four=='DXT1')fmt='BC1';else if(four=='DXT3')fmt='BC2';else if(four=='DXT5')fmt='BC3';
    else if(four=='DX10'){const dx=d.getUint32(128,true);off=148;
      if(dx==98||dx==99||dx==97)fmt='BC7';else if(dx>=70&&dx<=72)fmt='BC1';else if(dx>=73&&dx<=75)fmt='BC2';else if(dx>=76&&dx<=78)fmt='BC3';
      else if(dx==28||dx==29||dx==27)fmt='RGBA8';else if(dx==87||dx==91||dx==88)fmt='BGRA8';else throw new Error('DDS format '+dx+' is not supported');}
    else throw new Error('DDS compression '+four+' is not supported');
  } else {const rmask=d.getUint32(92,true);fmt=rmask==0xff?'RGBA8':'BGRA8';}
  return {w:W,h:H,rgba:decodeImage(fmt,u8,off,W,H),fmt};
}
// Skywalker Saga .texture -> {w,h,rgba}: block data for every mip sits at the end of the file
function parseGameTexture(buf){
  const d=new DataView(buf),u8=new Uint8Array(buf);
  const c1=d.getUint32(0,false)+4;let rt=-1;
  for(let i=c1;i<Math.min(u8.length-4,c1+4096);i++)if(u8[i]==0x52&&u8[i+1]==0x54&&u8[i+2]==0x58&&u8[i+3]==0x54){rt=i;break;}
  if(rt<0)throw new Error('no RTXT block: not a game texture');
  const W=d.getUint32(rt+8,false),H=d.getUint32(rt+12,false),mips=d.getUint32(rt+20,false),code=d.getUint32(rt+28,false);
  const tot=(f)=>{let s=0;for(let m=0;m<mips;m++)s+=mipSize(f,Math.max(1,W>>m),Math.max(1,H>>m));return s;};
  let fmt=code==1?'BC1':code==3||code==0x1a?'BC3':'BC7';
  // trust the data size over the code when they disagree
  const room=u8.length-rt;
  if(tot(fmt)>room||(fmt!='BC1'&&tot('BC1')<=room&&tot(fmt)>room))fmt='BC1';
  const off=u8.length-tot(fmt);
  return {w:W,h:H,rgba:decodeImage(fmt,u8,off,W,H),fmt};
}
if(typeof module!='undefined')module.exports={parseDDS,parseGameTexture,decodeImage};
