<script>
const SLOT_INFO=[
  ['head','Head','#e3b48a',/HEAD|FACE/],['body_front','Torso front','#f2f2f0',/BODY.*FRONT|TORSO.*FRONT|FRONT/],['body_back','Torso back','#f2f2f0',/BODY.*BACK|TORSO.*BACK|BACK/],
  ['arm_l','Left arm','#f2f2f0',/ARM.*LEFT|LEFT.*ARM/],['arm_r','Right arm','#f2f2f0',/ARM.*RIGHT|RIGHT.*ARM/],
  ['hands','Hands','#1b1b1b',/HAND/],['hips','Hips','#f2f2f0',/HIP/],['leg_l','Left leg','#f2f2f0',/LEG.*LEFT|LEFT.*LEG/],
  ['leg_r','Right leg','#f2f2f0',/LEG.*RIGHT|RIGHT.*LEG/]];
const HIDDEN_SLOTS={waist:'hips',neck:'head'};   // small studs take the colour of their neighbour
// texture kinds a material can hold, and how a file name says which one it is
const MAPS=[['color','Colour',null],['normal','Normal',/(_|\b)(NRM|NORM|NORMAL|NML)(_|\.|\b|$)/i],['brdf','BRDF',/(_|\b)(BRDF|BDRF|ROUGH|SPEC)(_|\.|\b|$)/i],['emission','Emission',/(_|\b)(EMI|EMIS|EMISSIVE|EMISSION|GLOW)(_|\.|\b|$)/i]];
const WEATHER=/(_|\b)(SAND|SNOW|MUD|WET)(_|\.|\b|$)/i;
const $=(id)=>document.getElementById(id);
const statusEl=$('status');let statusTimer=0;
function say(msg,err){statusEl.textContent=msg;statusEl.hidden=false;statusEl.classList.toggle('err',!!err);clearTimeout(statusTimer);statusTimer=setTimeout(()=>statusEl.hidden=true,err?9000:4500);}

// ---------- renderer: plain WebGL; per material a colour map (base colour + print), normal, BRDF and emission maps, each on either UV set
const canvas=$('view');const gl=canvas.getContext('webgl',{antialias:true,alpha:true});
const VS=`attribute vec3 p;attribute vec3 n;attribute vec2 t0;attribute vec2 t1;attribute vec4 g0;attribute vec4 g1;
uniform mat4 mvp;uniform mat4 model;varying vec3 vn;varying vec2 vt0;varying vec2 vt1;varying vec4 vg0;varying vec4 vg1;varying vec3 vw;
void main(){vec4 w=model*vec4(p,1.);vw=w.xyz;vn=mat3(model)*n;vg0=vec4(mat3(model)*g0.xyz,g0.w);vg1=vec4(mat3(model)*g1.xyz,g1.w);vt0=t0;vt1=t1;gl_Position=mvp*vec4(p,1.);}`;
const FS=`precision mediump float;varying vec3 vn;varying vec2 vt0;varying vec2 vt1;varying vec4 vg0;varying vec4 vg1;varying vec3 vw;
uniform sampler2D mC,mN,mB,mE;uniform vec4 use;uniform vec4 uvs;uniform vec4 flp;uniform vec3 eye;uniform float flatc;uniform vec4 tint;uniform float dxn;
vec2 pick(float s,float f){vec2 u=s>0.5?vt1:vt0;return f>0.5?vec2(u.x,1.-u.y):u;}
void main(){
  vec4 c=flatc>0.5?tint:texture2D(mC,pick(uvs.x,flp.x));vec3 base=pow(c.rgb,vec3(2.2));
  vec3 N=normalize(vn);float side=gl_FrontFacing?1.:-1.;N*=side;
  if(use.y>0.5){vec4 g=uvs.y>0.5?vg1:vg0;vec3 T=normalize(g.xyz-N*dot(N,g.xyz));vec3 B=cross(N,T)*(g.w<0.?-1.:1.);
    vec3 tn=texture2D(mN,pick(uvs.y,flp.y)).xyz*2.-1.;if(dxn>0.5)tn.y=-tn.y;N=normalize(T*tn.x+B*tn.y+N*max(tn.z,.05));}
  float rough=.32,spec=1.;
  if(use.z>0.5){vec4 b=texture2D(mB,pick(uvs.z,flp.z));rough=clamp(b.g,.04,1.);spec=b.r;}
  vec3 V=normalize(eye-vw);vec3 L1=normalize(vec3(-.55,.8,-.9)),L2=normalize(vec3(.7,.5,.8));float hemi=.5+.5*N.y;
  vec3 col=base*(mix(vec3(.28,.30,.34),vec3(.58),hemi)+.95*max(dot(N,L1),0.)+.40*max(dot(N,L2),0.));
  float shin=clamp(2./(rough*rough*rough*rough+1e-3)-2.,2.,512.);vec3 H=normalize(L1+V);
  float fres=.04+.96*pow(1.-max(dot(N,V),0.),5.);
  col+=vec3(.9*spec)*pow(max(dot(N,H),0.),shin)*(shin+8.)/(8.*3.14159)*.25*max(dot(N,L1),0.);col+=vec3(.10*spec)*fres*(1.-rough);
  if(use.w>0.5)col+=pow(texture2D(mE,pick(uvs.w,flp.w)).rgb,vec3(2.2));
  gl_FragColor=vec4(pow(col,vec3(1./2.2)),flatc>0.5?tint.a:1.);}`;
let prog,loc,uni,whiteTex,flatNormalTex;
function initGL(){
  const sh=(type,src)=>{const s=gl.createShader(type);gl.shaderSource(s,src);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw new Error(gl.getShaderInfoLog(s));return s;};
  prog=gl.createProgram();gl.attachShader(prog,sh(gl.VERTEX_SHADER,VS));gl.attachShader(prog,sh(gl.FRAGMENT_SHADER,FS));gl.linkProgram(prog);
  if(!gl.getProgramParameter(prog,gl.LINK_STATUS))throw new Error(gl.getProgramInfoLog(prog));gl.useProgram(prog);
  loc={};for(const a of ['p','n','t0','t1','g0','g1'])loc[a]=gl.getAttribLocation(prog,a);
  uni={};for(const u of ['mvp','model','eye','flatc','tint','mC','mN','mB','mE','use','uvs','flp','dxn'])uni[u]=gl.getUniformLocation(prog,u);
  gl.uniform1i(uni.mC,0);gl.uniform1i(uni.mN,1);gl.uniform1i(uni.mB,2);gl.uniform1i(uni.mE,3);
  const one=(rgba)=>{const t=gl.createTexture();gl.bindTexture(gl.TEXTURE_2D,t);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,1,1,0,gl.RGBA,gl.UNSIGNED_BYTE,new Uint8Array(rgba));return t;};
  whiteTex=one([255,255,255,255]);flatNormalTex=one([128,128,255,255]);
  gl.enable(gl.DEPTH_TEST);gl.enable(gl.BLEND);gl.blendFunc(gl.SRC_ALPHA,gl.ONE_MINUS_SRC_ALPHA);}
try{if(gl)initGL();else say('This browser cannot show 3D: WebGL is turned off or unavailable.',true);}catch(e){say('3D setup failed: '+e.message,true);}
function normals(pos,idx){const n=new Float32Array(pos.length);for(let i=0;i<idx.length;i+=3){const a=idx[i]*3,b=idx[i+1]*3,c=idx[i+2]*3;
  const ux=pos[b]-pos[a],uy=pos[b+1]-pos[a+1],uz=pos[b+2]-pos[a+2],vx=pos[c]-pos[a],vy=pos[c+1]-pos[a+1],vz=pos[c+2]-pos[a+2];
  const x=uy*vz-uz*vy,y=uz*vx-ux*vz,z=ux*vy-uy*vx;for(const k of [a,b,c]){n[k]+=x;n[k+1]+=y;n[k+2]+=z;}}
  for(let i=0;i<n.length;i+=3){const l=Math.hypot(n[i],n[i+1],n[i+2])||1;n[i]/=l;n[i+1]/=l;n[i+2]/=l;}return n;}
function tangents(pos,nrm,uv,idx){const nv=pos.length/3,t=new Float32Array(nv*3),b=new Float32Array(nv*3),out=new Float32Array(nv*4);
  for(let i=0;i<idx.length;i+=3){const ia=idx[i],ib=idx[i+1],ic=idx[i+2];
    const e1=[pos[ib*3]-pos[ia*3],pos[ib*3+1]-pos[ia*3+1],pos[ib*3+2]-pos[ia*3+2]],e2=[pos[ic*3]-pos[ia*3],pos[ic*3+1]-pos[ia*3+1],pos[ic*3+2]-pos[ia*3+2]];
    const du1=uv[ib*2]-uv[ia*2],dv1=uv[ib*2+1]-uv[ia*2+1],du2=uv[ic*2]-uv[ia*2],dv2=uv[ic*2+1]-uv[ia*2+1];const det=du1*dv2-du2*dv1;if(Math.abs(det)<1e-12)continue;const r=1/det;
    for(const k of [ia,ib,ic])for(let c=0;c<3;c++){t[k*3+c]+=(e1[c]*dv2-e2[c]*dv1)*r;b[k*3+c]+=(e2[c]*du1-e1[c]*du2)*r;}}
  for(let k=0;k<nv;k++){const N=[nrm[k*3],nrm[k*3+1],nrm[k*3+2]];let T=[t[k*3],t[k*3+1],t[k*3+2]];const d=dot3(N,T);T=norm3([T[0]-N[0]*d,T[1]-N[1]*d,T[2]-N[2]*d]);
    const w=dot3(cross3(N,T),[b[k*3],b[k*3+1],b[k*3+2]])<0?-1:1;out.set([T[0],T[1],T[2],w],k*4);}return out;}
function buf(data,target){const t=target||gl.ARRAY_BUFFER;const b=gl.createBuffer();gl.bindBuffer(t,b);gl.bufferData(t,data,gl.STATIC_DRAW);return b;}
function geom(pos,uv0,uv1,idx){if(!gl||!prog)return null;const n=normals(pos,idx);
  return {p:buf(pos),n:buf(n),t0:buf(uv0),t1:buf(uv1||uv0),g0:buf(tangents(pos,n,uv0,idx)),g1:buf(tangents(pos,n,uv1||uv0,idx)),i:buf(idx,gl.ELEMENT_ARRAY_BUFFER),count:idx.length};}
const sub3=(a,b)=>[a[0]-b[0],a[1]-b[1],a[2]-b[2]],dot3=(a,b)=>a[0]*b[0]+a[1]*b[1]+a[2]*b[2],
  cross3=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]],norm3=(a)=>{const l=Math.hypot(a[0],a[1],a[2])||1;return [a[0]/l,a[1]/l,a[2]/l];};
const M={mul(a,b){const o=new Float32Array(16);for(let c=0;c<4;c++)for(let r=0;r<4;r++){let s=0;for(let k=0;k<4;k++)s+=a[k*4+r]*b[c*4+k];o[c*4+r]=s;}return o;},
  persp(f,asp,n,fr){const t=1/Math.tan(f/2),o=new Float32Array(16);o[0]=t/asp;o[5]=t;o[10]=(fr+n)/(n-fr);o[11]=-1;o[14]=2*fr*n/(n-fr);return o;},
  look(e,c,u){const z=norm3(sub3(e,c)),x=norm3(cross3(u,z)),y=cross3(z,x);return new Float32Array([x[0],y[0],z[0],0,x[1],y[1],z[1],0,x[2],y[2],z[2],0,-dot3(x,e),-dot3(y,e),-dot3(z,e),1]);},
  model(ry,tx,ty,tz){const c=Math.cos(ry),s=Math.sin(ry);return new Float32Array([c,0,-s,0,0,1,0,0,s,0,c,0,tx*c+tz*s,ty,-tx*s+tz*c,1]);}};
const cam={az:Math.PI+0.18,el:0.14,dist:1.2,tgt:[0,0.19,0]};let figRot=0,spinning=false;
function view(dir){cam.az=dir>0?Math.PI+0.18:0.18;cam.el=0.14;cam.dist=1.2;cam.tgt=[0,0.19,0];figRot=0;}
let disc=null;
function hexRGB(h){const v=parseInt(h.slice(1),16);return [(v>>16&255)/255,(v>>8&255)/255,(v&255)/255];}
function bindMaps(s){const use=[1,0,0,0],uvs=[0,0,0,0],flp=[0,0,0,0];
  MAPS.forEach(([k],i)=>{const m=s.maps[k];gl.activeTexture(gl.TEXTURE0+i);
    if(k=='color'){gl.bindTexture(gl.TEXTURE_2D,colorTex(s));}else if(m.img){gl.bindTexture(gl.TEXTURE_2D,mapTex(m));use[i]=1;}else gl.bindTexture(gl.TEXTURE_2D,k=='normal'?flatNormalTex:whiteTex);
    uvs[i]=m.uv;flp[i]=m.flip?1:0;});
  gl.uniform4fv(uni.use,use);gl.uniform4fv(uni.uvs,uvs);gl.uniform4fv(uni.flp,flp);gl.uniform1f(uni.dxn,s.maps.normal.dx?1:0);}
function drawMesh(g,mvp,model,eye,surf,flatColor){if(!g)return;
  gl.uniformMatrix4fv(uni.mvp,false,mvp);gl.uniformMatrix4fv(uni.model,false,model);gl.uniform3fv(uni.eye,eye);
  if(surf){gl.uniform1f(uni.flatc,0);bindMaps(surf);}
  else{gl.uniform1f(uni.flatc,1);gl.uniform4fv(uni.tint,flatColor);gl.uniform4fv(uni.use,[1,0,0,0]);}
  for(const [k,sz] of [['p',3],['n',3],['t0',2],['t1',2],['g0',4],['g1',4]]){if(loc[k]<0)continue;gl.bindBuffer(gl.ARRAY_BUFFER,g[k]);gl.enableVertexAttribArray(loc[k]);gl.vertexAttribPointer(loc[k],sz,gl.FLOAT,false,0,0);}
  gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,g.i);gl.drawElements(gl.TRIANGLES,g.count,gl.UNSIGNED_SHORT,0);}
function frame(){requestAnimationFrame(frame);if(!gl||!prog)return;
  const dpr=Math.min(2,window.devicePixelRatio||1),w=Math.max(1,Math.round(canvas.clientWidth*dpr)),h=Math.max(1,Math.round(canvas.clientHeight*dpr));
  if(canvas.width!=w||canvas.height!=h){canvas.width=w;canvas.height=h;}
  gl.viewport(0,0,w,h);gl.clearColor(0,0,0,0);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);
  if(spinning)figRot+=0.006;
  const eye=[cam.tgt[0]+cam.dist*Math.cos(cam.el)*Math.sin(cam.az),cam.tgt[1]+cam.dist*Math.sin(cam.el),cam.tgt[2]+cam.dist*Math.cos(cam.el)*Math.cos(cam.az)];
  const vp=M.mul(M.persp(30*Math.PI/180,w/h,0.01,20),M.look(eye,cam.tgt,[0,1,0]));
  if(!disc){const k=48,pos=[0,-0.001,0],uv=[0,0],idx=[];for(let i=0;i<=k;i++){const a=i/k*Math.PI*2;pos.push(Math.cos(a)*0.3,-0.001,Math.sin(a)*0.3);uv.push(0,0);if(i)idx.push(0,i,i+1);}
    disc=geom(new Float32Array(pos),new Float32Array(uv),null,new Uint16Array(idx));}
  const m0=M.model(0,0,0,0);gl.depthMask(false);drawMesh(disc,M.mul(vp,m0),m0,eye,null,[0,0,0,0.10]);gl.depthMask(true);
  const mb=M.model(figRot,0,0,0),mvpb=M.mul(vp,mb);
  for(const m of bodyMeshes){if(m.follows){const c=hexRGB(slots[m.follows].surface.color);drawMesh(m.g,mvpb,mb,eye,null,[c[0],c[1],c[2],1]);}
    else drawMesh(m.g,mvpb,mb,eye,slots[slotFor(m.slot)].surface);}
  for(const p of parts){if(!p.visible)continue;const mp=M.model(figRot,p.place.x,p.place.y,p.place.z),mvp=M.mul(vp,mp);for(const g of p.meshes)drawMesh(g,mvp,mp,eye,p.surface);}}
// orbit: drag to turn, wheel or pinch to zoom, shift-drag (or right-drag) to move up and down
const ptrs=new Map();let lastPinch=0;
canvas.addEventListener('pointerdown',(e)=>{canvas.setPointerCapture(e.pointerId);ptrs.set(e.pointerId,[e.clientX,e.clientY,e.shiftKey||e.button==2]);});
const endPtr=(e)=>{ptrs.delete(e.pointerId);lastPinch=0;};canvas.addEventListener('pointerup',endPtr);canvas.addEventListener('pointercancel',endPtr);
canvas.addEventListener('contextmenu',(e)=>e.preventDefault());
canvas.addEventListener('pointermove',(e)=>{if(!ptrs.has(e.pointerId))return;const [x,y,pan]=ptrs.get(e.pointerId);const dx=e.clientX-x,dy=e.clientY-y;ptrs.set(e.pointerId,[e.clientX,e.clientY,pan]);
  if(ptrs.size==2){const [a,b]=[...ptrs.values()];const d=Math.hypot(a[0]-b[0],a[1]-b[1]);if(lastPinch)cam.dist=Math.min(3,Math.max(0.15,cam.dist*lastPinch/d));lastPinch=d;return;}
  if(pan){cam.tgt[1]=Math.min(0.6,Math.max(-0.1,cam.tgt[1]+dy*0.001*cam.dist));return;}
  cam.az-=dx*0.008;cam.el=Math.min(1.45,Math.max(-1.2,cam.el+dy*0.006));});
canvas.addEventListener('wheel',(e)=>{e.preventDefault();cam.dist=Math.min(3,Math.max(0.15,cam.dist*Math.exp(e.deltaY*0.001)));},{passive:false});
$('spin').onclick=()=>{spinning=!spinning;$('spin').setAttribute('aria-pressed',spinning);$('spin').classList.toggle('primary',spinning);};
$('front').onclick=()=>view(1);$('back').onclick=()=>view(-1);

// ---------- materials: base colour + print in the colour map, then optional normal, BRDF and emission maps
const pot=(v)=>{let p=4;while(p<v&&p<1024)p*=2;return p;};
function makeSurface(color,hasUV1){const maps={};for(const [k] of MAPS)maps[k]={img:null,name:'',uv:(k=='color'&&hasUV1)?1:0,flip:false,glTex:null,dirty:true,dx:true};
  const c=document.createElement('canvas');c.width=c.height=4;const s={color,maps,canvas:c,glTex:null,dirty:true};paintColor(s);return s;}
function paintColor(s){const m=s.maps.color,w=m.img?pot(m.img.width):4,h=m.img?pot(m.img.height):4;s.canvas.width=w;s.canvas.height=h;
  const g=s.canvas.getContext('2d');g.fillStyle=s.color;g.fillRect(0,0,w,h);if(m.img)g.drawImage(m.img,0,0,w,h);s.dirty=true;}
function upload(tex,src){gl.bindTexture(gl.TEXTURE_2D,tex);gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,false);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,src);gl.generateMipmap(gl.TEXTURE_2D);
  gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR_MIPMAP_LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);}
function colorTex(s){if(!s.glTex)s.glTex=gl.createTexture();if(s.dirty){upload(s.glTex,s.canvas);s.dirty=false;}else gl.bindTexture(gl.TEXTURE_2D,s.glTex);return s.glTex;}
function potCanvas(img){const w=pot(img.width),h=pot(img.height);if(img.width==w&&img.height==h)return img;const c=document.createElement('canvas');c.width=w;c.height=h;c.getContext('2d').drawImage(img,0,0,w,h);return c;}
function mapTex(m){if(!m.glTex)m.glTex=gl.createTexture();if(m.dirty){upload(m.glTex,potCanvas(m.img));m.dirty=false;}else gl.bindTexture(gl.TEXTURE_2D,m.glTex);return m.glTex;}
function setMap(s,kind,img,name){const m=s.maps[kind];m.img=img;m.name=name;m.dirty=true;if(kind=='color')paintColor(s);}
async function loadImage(file){const n=file.name.toLowerCase();const buf=await file.arrayBuffer();let r;
  if(n.endsWith('.dds'))r=parseDDS(buf);else if(n.endsWith('.texture'))r=parseGameTexture(buf);
  else{const bmp=await createImageBitmap(new Blob([buf]));const c=document.createElement('canvas');c.width=bmp.width;c.height=bmp.height;c.getContext('2d').drawImage(bmp,0,0);return c;}
  const c=document.createElement('canvas');c.width=r.w;c.height=r.h;c.getContext('2d').putImageData(new ImageData(r.rgba,r.w,r.h),0,0);return c;}
function imgFromURL(url){return new Promise((res,rej)=>{const i=new Image();i.onload=()=>{const c=document.createElement('canvas');c.width=i.width;c.height=i.height;c.getContext('2d').drawImage(i,0,0);res(c);};i.onerror=()=>rej(new Error('bad image'));i.src=url;});}

// ---------- body
const b64=(s)=>{const bin=atob(s);const u=new Uint8Array(bin.length);for(let i=0;i<bin.length;i++)u[i]=bin.charCodeAt(i);return u.buffer;};
const slots={};for(const [id,label,color] of SLOT_INFO)slots[id]={id,label,surface:makeSurface(color,true),defColor:color};
const bodyMeshes=BODY.map(p=>({g:geom(new Float32Array(b64(p.pos)),new Float32Array(b64(p.uv0)),new Float32Array(b64(p.uv)),new Uint16Array(b64(p.idx))),slot:p.slot,follows:HIDDEN_SLOTS[p.slot]||null}));
function slotFor(s){return $('swapLR').checked?({arm_l:'arm_r',arm_r:'arm_l',leg_l:'leg_r',leg_r:'leg_l'}[s]||s):s;}

// ---------- side panel
let target=null;   // where dropped textures go when the name does not say: {ref (a slot or part), el}
const expanded=new Set();   // which material editors are expanded
function setTarget(t){target=t;document.querySelectorAll('.target').forEach(e=>e.classList.remove('target'));if(t&&t.el)t.el.classList.add('target');}
function btn(label,cls,fn,aria){const b=document.createElement('button');b.className='btn '+(cls||'');b.textContent=label;b.onclick=fn;if(aria)b.setAttribute('aria-label',aria);return b;}
function mapCount(s){return MAPS.filter(([k])=>s.maps[k].img).length;}
// the four texture rows of a material: name, UV set, flip, load, clear
function mapEditor(owner,ownerKey,refresh){const s=owner.surface;const box=document.createElement('div');box.className='maps';
  for(const [k,label] of MAPS){const m=s.maps[k];const r=document.createElement('div');r.className='maprow';
    const l=document.createElement('span');l.className='mlabel';l.textContent=label;
    const nm=document.createElement('span');nm.className='tex';nm.textContent=m.name||'none';nm.title=m.name;
    const sel=document.createElement('select');sel.id='uv_'+ownerKey+'_'+k;sel.setAttribute('aria-label',label+' UV set');
    for(const [v,t] of [[0,'UV 1'],[1,'UV 2']]){const o=document.createElement('option');o.value=v;o.textContent=t;sel.append(o);}sel.value=m.uv;sel.onchange=()=>{m.uv=+sel.value;};
    const fl=document.createElement('label');fl.className='mini';const fc=document.createElement('input');fc.type='checkbox';fc.checked=m.flip;fc.id='flip_'+ownerKey+'_'+k;fc.onchange=()=>{m.flip=fc.checked;};
    fl.append(fc,document.createTextNode('Flip'));fl.title='Flip the texture vertically';
    r.append(l,nm,sel,fl,btn('Load','icon',()=>chooseFor(owner,k),'Load '+label.toLowerCase()+' texture'),
      btn('×','icon',()=>{setMap(s,k,null,'');refresh();},'Remove '+label.toLowerCase()+' texture'));
    box.append(r);
    if(k=='normal'){const r2=document.createElement('label');r2.className='mini sub2';const c2=document.createElement('input');c2.type='checkbox';c2.checked=m.dx;c2.id='dx_'+ownerKey;
      c2.onchange=()=>{m.dx=c2.checked;};r2.append(c2,document.createTextNode('Green channel points down (game and DirectX normal maps)'));box.append(r2);}}
  return box;}
function renderBodyRows(){const box=$('bodyRows');box.textContent='';
  for(const [id] of SLOT_INFO){const s=slots[id];const wrap=document.createElement('div');wrap.className='slot';
    const row=document.createElement('div');row.className='row';
    const name=document.createElement('button');name.className='name';name.textContent=s.label;name.setAttribute('aria-expanded',expanded.has(id));name.title='Show this material\'s textures; dropped textures with no clear name go here';
    const col=document.createElement('input');col.type='color';col.value=s.surface.color;col.id='col_'+id;col.setAttribute('aria-label',s.label+' colour');
    col.oninput=()=>{s.surface.color=col.value;paintColor(s.surface);};
    const tex=document.createElement('span');tex.className='tex';const n=mapCount(s.surface);
    tex.textContent=s.surface.maps.color.name||(n?'':'no texture');if(n>(s.surface.maps.color.img?1:0))tex.textContent+=(tex.textContent?' +':'')+(n-(s.surface.maps.color.img?1:0))+' map'+(n-(s.surface.maps.color.img?1:0)>1?'s':'');
    tex.title=MAPS.map(([k,l])=>l+': '+(s.surface.maps[k].name||'none')).join('\n');
    row.append(name,col,tex,btn('Colour…','icon',()=>chooseFor(s,'color'),'Load '+s.label+' colour texture'));wrap.append(row);
    if(expanded.has(id))wrap.append(mapEditor(s,id,renderBodyRows));
    name.onclick=()=>{if(expanded.has(id))expanded.delete(id);else expanded.add(id);setTarget({ref:s,el:row});renderBodyRows();};
    box.append(wrap);if(target&&target.ref===s){row.classList.add('target');target.el=row;}}}

// ---------- parts
const parts=[];let partSeq=0;
function defaultPlacement(name,model){const n=name.toUpperCase();
  if(/HAT|HELMET|HAIR/.test(n))return {y:0.3742};      // helmet height used by the clone trooper prefabs
  if(/PAULDRON/.test(n))return {y:0.2891};             // neck locator
  if(/SKIRT|KAMA/.test(n))return {y:0.104};
  return {y:model.skinned?0:0.3742};}
function addPart(name,model,opts={}){
  const p={id:++partSeq,name,model,surface:makeSurface(opts.color||'#f2f2f0',model.hasUV1),meshes:[],visible:true,
    place:Object.assign({x:0,y:0,z:0},defaultPlacement(name,model))};
  p.meshes=model.pieces.map(pc=>geom(pc.pos,pc.uv0,model.hasUV1?pc.uv1:pc.uv0,pc.idx));parts.push(p);expanded.add('p'+p.id);return p;}
function renderParts(){const box=$('partList');box.textContent='';$('noParts').hidden=parts.length>0;
  for(const p of parts){const el=document.createElement('div');el.className='part';const key='p'+p.id;
    const head=document.createElement('div');head.className='part-head';
    const vis=document.createElement('input');vis.type='checkbox';vis.checked=p.visible;vis.id='vis_'+p.id;vis.setAttribute('aria-label','Show '+p.name);vis.onchange=()=>{p.visible=vis.checked;};
    const nm=document.createElement('button');nm.className='pname';nm.textContent=p.name;nm.title='Show or hide this part\'s textures; dropped textures with no clear name go here';nm.setAttribute('aria-expanded',expanded.has(key));
    nm.onclick=()=>{if(expanded.has(key))expanded.delete(key);else expanded.add(key);setTarget({ref:p,el});renderParts();};
    const col=document.createElement('input');col.type='color';col.value=p.surface.color;col.id='pcol_'+p.id;col.setAttribute('aria-label',p.name+' colour');
    col.oninput=()=>{p.surface.color=col.value;paintColor(p.surface);};
    head.append(vis,nm,col,btn('Remove','icon',()=>{parts.splice(parts.indexOf(p),1);if(target&&target.ref===p)target=null;renderParts();}));
    el.append(head);
    if(expanded.has(key)){el.append(mapEditor(p,key,renderParts));
      const grid=document.createElement('div');grid.className='part-grid';
      for(const [ax,lab,min,max] of [['y','Height',-0.1,0.5],['x','Side',-0.2,0.2],['z','Depth',-0.2,0.2]]){
        const l=document.createElement('span');l.textContent=lab;const r=document.createElement('input');r.type='range';r.min=min;r.max=max;r.step=0.0005;r.value=p.place[ax];r.id='pl_'+ax+'_'+p.id;r.setAttribute('aria-label',p.name+' '+lab);
        const o=document.createElement('output');o.textContent=(+p.place[ax]).toFixed(4);r.oninput=()=>{p.place[ax]=+r.value;o.textContent=(+r.value).toFixed(4);};grid.append(l,r,o);}
      el.append(grid);}
    box.append(el);if(target&&target.ref===p){el.classList.add('target');target.el=el;}}}

// ---------- files
let pending=null;
function chooseFor(owner,kind){pending={ref:owner,kind};$('fileInput').click();}
$('addFiles').onclick=()=>{pending=null;$('fileInput').click();};
$('fileInput').onchange=async(e)=>{const files=[...e.target.files];e.target.value='';await handleFiles(files,pending);pending=null;};
function kindOf(name){for(const [k,,re] of MAPS)if(re&&re.test(name))return k;return 'color';}
function ownerOf(name){const n=name.toUpperCase().replace(/\.[A-Z]+$/,'');
  for(const p of parts){const toks=p.name.toUpperCase().replace(/\.[A-Z]+$/,'').split(/[_\-\s.]+/).filter(t=>t.length>=4&&!/^(DX11|ADDITIONALMODEL|SUPER|CHARACTER)$/.test(t));
    if(toks.some(t=>n.includes(t)))return p;}
  if(/HAT|HELMET/.test(n)){const h=parts.find(p=>/HAT|HELMET/i.test(p.name));if(h)return h;}
  for(const [id,,,re] of SLOT_INFO)if(re.test(n))return slots[id];
  return null;}
async function handleFiles(files,forced){let placed=0;const skipped=[];
  for(const f of files){const n=f.name;try{
    if(/\.(gsc|ghg)$/i.test(n)){const model=parseModel(await f.arrayBuffer());if(!model.pieces.length)throw new Error('no visible mesh in this model');
      const p=addPart(n,model);renderParts();setTarget({ref:p,el:null});placed++;continue;}
    if(!/\.(dds|texture|png|jpe?g|webp)$/i.test(n)){skipped.push(n+' (not a texture or model)');continue;}
    if(!forced&&WEATHER.test(n)){skipped.push(n+' (weather overlay: not shown here)');continue;}
    const owner=forced?forced.ref:(ownerOf(n)||(target&&target.ref));
    if(!owner){skipped.push(n+' (the name does not say which part it is for: click a part or body row name, then load it again)');continue;}
    const kind=forced&&forced.kind?forced.kind:kindOf(n);
    setMap(owner.surface,kind,await loadImage(f),n);placed++;
  }catch(err){skipped.push(n+' ('+err.message+')');}}
  renderBodyRows();renderParts();
  if(skipped.length)say((placed?'Loaded '+placed+'. ':'')+'Skipped: '+skipped.join('; '),placed==0);else if(placed)say('Loaded '+placed+' file'+(placed>1?'s':'')+'.');}
let dragDepth=0;
window.addEventListener('dragenter',(e)=>{e.preventDefault();dragDepth++;$('drop').hidden=false;});
window.addEventListener('dragleave',()=>{dragDepth=Math.max(0,dragDepth-1);if(!dragDepth)$('drop').hidden=true;});
window.addEventListener('dragover',(e)=>e.preventDefault());
window.addEventListener('drop',(e)=>{e.preventDefault();dragDepth=0;$('drop').hidden=true;handleFiles([...e.dataTransfer.files],null);});

// ---------- example: the 212th airborne clone trooper
async function loadExample(){
  for(const id of Object.keys(slots))if(SAMPLE_TEX[id])setMap(slots[id].surface,'color',await imgFromURL(SAMPLE_TEX[id]),id.toUpperCase()+'_CT212 (example)');
  const helm=addPart('HAT_CT212KAMA_DX11.GSC',parseModel(b64(SAMPLE_MODELS.helmet)));
  setMap(helm.surface,'color',await imgFromURL(SAMPLE_TEX.helmet),'HAT_CT212KAMA_DIFF (example)');
  setMap(helm.surface,'normal',await imgFromURL(SAMPLE_TEX.helmet_nrm),'HAT_AIRBORNE_NRM (example)');
  setMap(helm.surface,'brdf',await imgFromURL(SAMPLE_TEX.helmet_brdf),'HAT_AIRBORNE_BRDF (example)');
  const kama=addPart('SKINNEDSKIRT_FABRIC_KAMACT212_DX11.GHG',parseModel(b64(SAMPLE_MODELS.kama)));kama.surface.maps.color.uv=0;
  setMap(kama.surface,'color',await imgFromURL(SAMPLE_TEX.kama),'KAMA_AIRBORN (example)');
  const sat=addPart('ADDITIONALMODEL_SATCHEL_DX11.GHG',parseModel(b64(SAMPLE_MODELS.satchel)),{color:'#582a12'});paintColor(sat.surface);
  setMap(sat.surface,'normal',await imgFromURL(SAMPLE_TEX.satchel_nrm),'ADDITIONALMODEL_SATCHEL_NRM (example)');
  setMap(sat.surface,'brdf',await imgFromURL(SAMPLE_TEX.satchel_brdf),'ADDITIONALMODEL_SATCHEL_BRDF (example)');
  expanded.clear();renderBodyRows();renderParts();}
$('clearAll').onclick=()=>{parts.length=0;target=null;expanded.clear();
  for(const [id] of SLOT_INFO){const s=slots[id].surface;for(const [k] of MAPS){s.maps[k].img=null;s.maps[k].name='';s.maps[k].dirty=true;}s.color=slots[id].defColor;paintColor(s);}
  renderBodyRows();renderParts();say('Plain minifig. Load textures or parts to dress it.');};
renderBodyRows();renderParts();requestAnimationFrame(frame);
loadExample().catch(e=>say('The example could not load: '+e.message,true));
</script>
