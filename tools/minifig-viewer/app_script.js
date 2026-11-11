<script>
const SLOT_INFO=[
  ['head','Head','#e3b48a',/HEAD|FACE/],['body_front','Torso front','#f2f2f0',/BODY.*FRONT|TORSO.*FRONT|FRONT/],['body_back','Torso back','#f2f2f0',/BODY.*BACK|TORSO.*BACK|BACK/],
  ['arm_l','Left arm','#f2f2f0',/ARM.*LEFT|LEFT.*ARM/],['arm_r','Right arm','#f2f2f0',/ARM.*RIGHT|RIGHT.*ARM/],
  ['hands','Hands','#1b1b1b',/HAND/],['hips','Hips','#f2f2f0',/HIP/],['leg_l','Left leg','#f2f2f0',/LEG.*LEFT|LEFT.*LEG/],
  ['leg_r','Right leg','#f2f2f0',/LEG.*RIGHT|RIGHT.*LEG/]];
const HIDDEN_SLOTS={waist:'hips',neck:'head'};   // small studs take the colour of their neighbour
const $=(id)=>document.getElementById(id);
const statusEl=$('status');let statusTimer=0;
function say(msg,err){statusEl.textContent=msg;statusEl.hidden=false;statusEl.classList.toggle('err',!!err);clearTimeout(statusTimer);statusTimer=setTimeout(()=>statusEl.hidden=true,err?9000:4500);}

// ---------- renderer: plain WebGL, textured lit meshes and an orbit camera
const canvas=$('view');const gl=canvas.getContext('webgl',{antialias:true,alpha:true});
const VS=`attribute vec3 p;attribute vec3 n;attribute vec2 t;uniform mat4 mvp;uniform mat4 model;varying vec3 vn;varying vec2 vt;varying vec3 vw;
void main(){vec4 w=model*vec4(p,1.);vw=w.xyz;vn=mat3(model)*n;vt=t;gl_Position=mvp*vec4(p,1.);}`;
const FS=`precision mediump float;varying vec3 vn;varying vec2 vt;varying vec3 vw;uniform sampler2D tex;uniform vec3 eye;uniform float flatc;uniform vec4 tint;
void main(){vec4 c=flatc>0.5?tint:texture2D(tex,vt);vec3 base=pow(c.rgb,vec3(2.2));vec3 N=normalize(vn);if(!gl_FrontFacing)N=-N;vec3 V=normalize(eye-vw);
vec3 L1=normalize(vec3(-.55,.8,-.9)),L2=normalize(vec3(.7,.5,.8));float hemi=.5+.5*N.y;
vec3 col=base*(mix(vec3(.28,.30,.34),vec3(.58),hemi)+.95*max(dot(N,L1),0.)+.40*max(dot(N,L2),0.));
vec3 H=normalize(L1+V);col+=vec3(.20)*pow(max(dot(N,H),0.),48.);col+=vec3(.06)*pow(1.-max(dot(N,V),0.),3.);
gl_FragColor=vec4(pow(col,vec3(1./2.2)),flatc>0.5?tint.a:1.);}`;
let prog,loc,uni;
function initGL(){
  const sh=(type,src)=>{const s=gl.createShader(type);gl.shaderSource(s,src);gl.compileShader(s);if(!gl.getShaderParameter(s,gl.COMPILE_STATUS))throw new Error(gl.getShaderInfoLog(s));return s;};
  prog=gl.createProgram();gl.attachShader(prog,sh(gl.VERTEX_SHADER,VS));gl.attachShader(prog,sh(gl.FRAGMENT_SHADER,FS));gl.linkProgram(prog);gl.useProgram(prog);
  loc={p:gl.getAttribLocation(prog,'p'),n:gl.getAttribLocation(prog,'n'),t:gl.getAttribLocation(prog,'t')};
  const U=(k)=>gl.getUniformLocation(prog,k);uni={mvp:U('mvp'),model:U('model'),eye:U('eye'),flatc:U('flatc'),tint:U('tint'),tex:U('tex')};
  gl.enable(gl.DEPTH_TEST);gl.enable(gl.BLEND);gl.blendFunc(gl.SRC_ALPHA,gl.ONE_MINUS_SRC_ALPHA);}
if(gl)initGL();else say('This browser cannot show 3D: WebGL is turned off or unavailable.',true);
function normals(pos,idx){const n=new Float32Array(pos.length);for(let i=0;i<idx.length;i+=3){const a=idx[i]*3,b=idx[i+1]*3,c=idx[i+2]*3;
  const ux=pos[b]-pos[a],uy=pos[b+1]-pos[a+1],uz=pos[b+2]-pos[a+2],vx=pos[c]-pos[a],vy=pos[c+1]-pos[a+1],vz=pos[c+2]-pos[a+2];
  const x=uy*vz-uz*vy,y=uz*vx-ux*vz,z=ux*vy-uy*vx;for(const k of [a,b,c]){n[k]+=x;n[k+1]+=y;n[k+2]+=z;}}
  for(let i=0;i<n.length;i+=3){const l=Math.hypot(n[i],n[i+1],n[i+2])||1;n[i]/=l;n[i+1]/=l;n[i+2]/=l;}return n;}
function buf(data,target){const t=target||gl.ARRAY_BUFFER;const b=gl.createBuffer();gl.bindBuffer(t,b);gl.bufferData(t,data,gl.STATIC_DRAW);return b;}
function geom(pos,uv,idx){if(!gl)return null;return {p:buf(pos),n:buf(normals(pos,idx)),t:buf(uv),i:buf(idx,gl.ELEMENT_ARRAY_BUFFER),count:idx.length};}
function setUV(g,uv){if(!g)return;gl.deleteBuffer(g.t);g.t=buf(uv);}
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
function drawMesh(g,mvp,model,eye,surf,flatColor){if(!g)return;
  gl.uniformMatrix4fv(uni.mvp,false,mvp);gl.uniformMatrix4fv(uni.model,false,model);gl.uniform3fv(uni.eye,eye);
  if(surf){gl.uniform1f(uni.flatc,0);gl.activeTexture(gl.TEXTURE0);gl.bindTexture(gl.TEXTURE_2D,surfaceTex(surf));gl.uniform1i(uni.tex,0);}
  else{gl.uniform1f(uni.flatc,1);gl.uniform4fv(uni.tint,flatColor);}
  for(const [k,b,sz] of [['p',g.p,3],['n',g.n,3],['t',g.t,2]]){gl.bindBuffer(gl.ARRAY_BUFFER,b);gl.enableVertexAttribArray(loc[k]);gl.vertexAttribPointer(loc[k],sz,gl.FLOAT,false,0,0);}
  gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,g.i);gl.drawElements(gl.TRIANGLES,g.count,gl.UNSIGNED_SHORT,0);}
function frame(){requestAnimationFrame(frame);if(!gl)return;
  const dpr=Math.min(2,window.devicePixelRatio||1),w=Math.max(1,Math.round(canvas.clientWidth*dpr)),h=Math.max(1,Math.round(canvas.clientHeight*dpr));
  if(canvas.width!=w||canvas.height!=h){canvas.width=w;canvas.height=h;}
  gl.viewport(0,0,w,h);gl.clearColor(0,0,0,0);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);
  if(spinning)figRot+=0.006;
  const eye=[cam.tgt[0]+cam.dist*Math.cos(cam.el)*Math.sin(cam.az),cam.tgt[1]+cam.dist*Math.sin(cam.el),cam.tgt[2]+cam.dist*Math.cos(cam.el)*Math.cos(cam.az)];
  const vp=M.mul(M.persp(30*Math.PI/180,w/h,0.01,20),M.look(eye,cam.tgt,[0,1,0]));
  if(!disc){const k=48,pos=[0,-0.001,0],uv=[0,0],idx=[];for(let i=0;i<=k;i++){const a=i/k*Math.PI*2;pos.push(Math.cos(a)*0.3,-0.001,Math.sin(a)*0.3);uv.push(0,0);if(i)idx.push(0,i,i+1);}
    disc=geom(new Float32Array(pos),new Float32Array(uv),new Uint16Array(idx));}
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

// ---------- surfaces: a base colour with the texture printed on top (alpha lets the base colour through)
function makeSurface(color){const c=document.createElement('canvas');c.width=c.height=4;const s={color,img:null,name:'',canvas:c,flip:false,glTex:null,dirty:true};paint(s);return s;}
const pot=(v)=>{let p=4;while(p<v&&p<1024)p*=2;return p;};
function paint(s){const w=s.img?pot(s.img.width):4,h=s.img?pot(s.img.height):4;s.canvas.width=w;s.canvas.height=h;
  const g=s.canvas.getContext('2d');g.save();g.fillStyle=s.color;g.fillRect(0,0,w,h);
  if(s.img){if(s.flip){g.translate(0,h);g.scale(1,-1);}g.drawImage(s.img,0,0,w,h);}g.restore();s.dirty=true;}
function surfaceTex(s){if(!s.glTex)s.glTex=gl.createTexture();gl.bindTexture(gl.TEXTURE_2D,s.glTex);
  if(s.dirty){gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,false);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,s.canvas);gl.generateMipmap(gl.TEXTURE_2D);
    gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR_MIPMAP_LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);s.dirty=false;}
  return s.glTex;}
async function loadImage(file){const n=file.name.toLowerCase();const buf=await file.arrayBuffer();let r;
  if(n.endsWith('.dds'))r=parseDDS(buf);else if(n.endsWith('.texture'))r=parseGameTexture(buf);
  else{const bmp=await createImageBitmap(new Blob([buf]));const c=document.createElement('canvas');c.width=bmp.width;c.height=bmp.height;c.getContext('2d').drawImage(bmp,0,0);return c;}
  const c=document.createElement('canvas');c.width=r.w;c.height=r.h;c.getContext('2d').putImageData(new ImageData(r.rgba,r.w,r.h),0,0);return c;}
function imgFromURL(url){return new Promise((res,rej)=>{const i=new Image();i.onload=()=>{const c=document.createElement('canvas');c.width=i.width;c.height=i.height;c.getContext('2d').drawImage(i,0,0);res(c);};i.onerror=()=>rej(new Error('bad image'));i.src=url;});}

// ---------- body
const b64=(s)=>{const bin=atob(s);const u=new Uint8Array(bin.length);for(let i=0;i<bin.length;i++)u[i]=bin.charCodeAt(i);return u.buffer;};
const slots={};for(const [id,label,color] of SLOT_INFO)slots[id]={id,label,surface:makeSurface(color),defColor:color};
const bodyMeshes=BODY.map(p=>({g:geom(new Float32Array(b64(p.pos)),new Float32Array(b64(p.uv)),new Uint16Array(b64(p.idx))),slot:p.slot,follows:HIDDEN_SLOTS[p.slot]||null}));
function slotFor(s){return $('swapLR').checked?({arm_l:'arm_r',arm_r:'arm_l',leg_l:'leg_r',leg_r:'leg_l'}[s]||s):s;}

let target=null;   // where dropped textures go when the name does not say: {kind:'slot'|'part', ref, el}
function setTarget(t){target=t;document.querySelectorAll('.target').forEach(e=>e.classList.remove('target'));if(t&&t.el)t.el.classList.add('target');}
function btn(label,cls,fn,aria){const b=document.createElement('button');b.className='btn '+(cls||'');b.textContent=label;b.onclick=fn;if(aria)b.setAttribute('aria-label',aria);return b;}
function renderBodyRows(){const box=$('bodyRows');box.textContent='';
  for(const [id] of SLOT_INFO){const s=slots[id];const row=document.createElement('div');row.className='row';
    const name=document.createElement('span');name.className='name';name.textContent=s.label;name.tabIndex=0;name.title='Send dropped textures here';
    const col=document.createElement('input');col.type='color';col.value=s.surface.color;col.id='col_'+id;col.setAttribute('aria-label',s.label+' colour');
    col.oninput=()=>{s.surface.color=col.value;paint(s.surface);};
    const tex=document.createElement('span');tex.className='tex';tex.textContent=s.surface.name||'no texture';tex.title=s.surface.name;
    const acts=document.createElement('span');acts.style.cssText='display:flex;gap:4px';
    acts.append(btn('Texture','icon',()=>chooseFor({kind:'slot',ref:s,el:row})),
      btn('×','icon',()=>{s.surface.img=null;s.surface.name='';paint(s.surface);renderBodyRows();},'Remove '+s.label+' texture'));
    row.append(name,col,tex,acts);box.append(row);
    const sel=()=>setTarget({kind:'slot',ref:s,el:row});name.onclick=sel;name.onkeydown=(e)=>{if(e.key=='Enter'||e.key==' '){e.preventDefault();sel();}};
    if(target&&target.ref===s){row.classList.add('target');target.el=row;}}}

// ---------- parts
const parts=[];let partSeq=0;
function defaultPlacement(name,model){const n=name.toUpperCase();
  if(/HAT|HELMET|HAIR/.test(n))return {y:0.3742};      // helmet height used by the clone trooper prefabs
  if(/PAULDRON/.test(n))return {y:0.2891};             // neck locator
  if(/SKIRT|KAMA/.test(n))return {y:0.104};
  return {y:model.skinned?0:0.3742};}
function addPart(name,model,opts={}){
  const p={id:++partSeq,name,model,surface:makeSurface(opts.color||'#f2f2f0'),meshes:[],uvSet:model.hasUV1?1:0,visible:true,
    place:Object.assign({x:0,y:0,z:0},defaultPlacement(name,model))};
  buildPartMeshes(p);parts.push(p);return p;}
function buildPartMeshes(p){if(p.meshes.length){p.model.pieces.forEach((pc,i)=>setUV(p.meshes[i],p.uvSet?pc.uv1:pc.uv0));return;}
  p.meshes=p.model.pieces.map(pc=>geom(pc.pos,p.uvSet?pc.uv1:pc.uv0,pc.idx));}
function renderParts(){const box=$('partList');box.textContent='';$('noParts').hidden=parts.length>0;
  for(const p of parts){const el=document.createElement('div');el.className='part';
    const head=document.createElement('div');head.className='part-head';
    const vis=document.createElement('input');vis.type='checkbox';vis.checked=p.visible;vis.id='vis_'+p.id;vis.setAttribute('aria-label','Show '+p.name);vis.onchange=()=>{p.visible=vis.checked;};
    const nm=document.createElement('span');nm.className='pname';nm.textContent=p.name;nm.title=p.name+' (click to send dropped textures here)';nm.style.cursor='pointer';nm.tabIndex=0;
    const col=document.createElement('input');col.type='color';col.value=p.surface.color;col.id='pcol_'+p.id;col.setAttribute('aria-label',p.name+' colour');
    col.oninput=()=>{p.surface.color=col.value;paint(p.surface);};
    head.append(vis,nm,col,btn('Remove','icon',()=>{parts.splice(parts.indexOf(p),1);if(target&&target.ref===p)target=null;renderParts();}));
    const texrow=document.createElement('div');texrow.style.cssText='display:flex;gap:6px;align-items:center;min-width:0';
    const tn=document.createElement('span');tn.className='tex';tn.style.flex='1';tn.textContent=p.surface.name||'no texture';tn.title=p.surface.name;
    texrow.append(tn,btn('Texture','icon',()=>chooseFor({kind:'part',ref:p,el})),btn('×','icon',()=>{p.surface.img=null;p.surface.name='';paint(p.surface);renderParts();},'Remove texture'));
    const checks=document.createElement('div');checks.className='checks';
    const mk=(label,checked,fn,id)=>{const l=document.createElement('label');const c=document.createElement('input');c.type='checkbox';c.checked=checked;c.id=id;c.onchange=()=>fn(c.checked);l.append(c,document.createTextNode(label));return l;};
    if(p.model.hasUV1)checks.append(mk('Second UV set',p.uvSet==1,(v)=>{p.uvSet=v?1:0;buildPartMeshes(p);},'uv_'+p.id));
    checks.append(mk('Flip texture',p.surface.flip,(v)=>{p.surface.flip=v;paint(p.surface);},'flip_'+p.id));
    const grid=document.createElement('div');grid.className='part-grid';
    for(const [ax,lab,min,max] of [['y','Height',-0.1,0.5],['x','Side',-0.2,0.2],['z','Depth',-0.2,0.2]]){
      const l=document.createElement('span');l.textContent=lab;const r=document.createElement('input');r.type='range';r.min=min;r.max=max;r.step=0.0005;r.value=p.place[ax];r.id='pl_'+ax+'_'+p.id;r.setAttribute('aria-label',p.name+' '+lab);
      const o=document.createElement('output');o.textContent=(+p.place[ax]).toFixed(4);r.oninput=()=>{p.place[ax]=+r.value;o.textContent=(+r.value).toFixed(4);};grid.append(l,r,o);}
    el.append(head,texrow,checks,grid);box.append(el);
    const sel=()=>setTarget({kind:'part',ref:p,el});nm.onclick=sel;nm.onkeydown=(e)=>{if(e.key=='Enter'||e.key==' '){e.preventDefault();sel();}};
    if(target&&target.ref===p){el.classList.add('target');target.el=el;}}}

// ---------- files
let pendingTarget=null;
function chooseFor(t){pendingTarget=t;setTarget(t);$('fileInput').click();}
$('addFiles').onclick=()=>{pendingTarget=null;$('fileInput').click();};
$('fileInput').onchange=async(e)=>{const files=[...e.target.files];e.target.value='';await handleFiles(files,pendingTarget);pendingTarget=null;};
const IGNORE=/_(NRM|NORM|NORMAL|NML|BRDF|BDRF|SAND|SNOW|MUD|ROUGH|SPEC)(_|\.|$)/i;
function autoTarget(name){const n=name.toUpperCase().replace(/\.[A-Z]+$/,'');
  for(const p of parts){const toks=p.name.toUpperCase().replace(/\.[A-Z]+$/,'').split(/[_\-\s.]+/).filter(t=>t.length>=4&&!/^(DX11|ADDITIONALMODEL|SUPER|CHARACTER)$/.test(t));
    if(toks.some(t=>n.includes(t)))return {kind:'part',ref:p};}
  if(/HAT|HELMET/.test(n)){const h=parts.find(p=>/HAT|HELMET/i.test(p.name));if(h)return {kind:'part',ref:h};}
  for(const [id,,,re] of SLOT_INFO)if(re.test(n))return {kind:'slot',ref:slots[id]};
  return null;}
async function handleFiles(files,forced){let placed=0;const skipped=[];
  for(const f of files){const n=f.name;try{
    if(/\.(gsc|ghg)$/i.test(n)){const model=parseModel(await f.arrayBuffer());if(!model.pieces.length)throw new Error('no visible mesh in this model');
      const p=addPart(n,model);renderParts();setTarget({kind:'part',ref:p,el:null});placed++;continue;}
    if(!/\.(dds|texture|png|jpe?g|webp)$/i.test(n)){skipped.push(n+' (not a texture or model)');continue;}
    if(!forced&&IGNORE.test(n)){skipped.push(n+' (normal, roughness or weather map: only colour textures show here)');continue;}
    const t=forced||autoTarget(n)||target;if(!t){skipped.push(n+' (name does not say where it goes: click a row name, then load it again)');continue;}
    const img=await loadImage(f);const s=t.ref.surface;s.img=img;s.name=n;paint(s);placed++;
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
  for(const [id,url] of Object.entries(SAMPLE_TEX)){if(!slots[id])continue;slots[id].surface.img=await imgFromURL(url);slots[id].surface.name=id.toUpperCase()+'_CT212 (example)';paint(slots[id].surface);}
  const helm=addPart('HAT_CT212KAMA_DX11.GSC',parseModel(b64(SAMPLE_MODELS.helmet)));helm.surface.img=await imgFromURL(SAMPLE_TEX.helmet);helm.surface.name='HAT_CT212KAMA_DIFF (example)';paint(helm.surface);
  const kama=addPart('SKINNEDSKIRT_FABRIC_KAMACT212_DX11.GHG',parseModel(b64(SAMPLE_MODELS.kama)));kama.uvSet=0;buildPartMeshes(kama);
  kama.surface.img=await imgFromURL(SAMPLE_TEX.kama);kama.surface.name='KAMA_AIRBORN (example)';paint(kama.surface);
  addPart('ADDITIONALMODEL_SATCHEL_DX11.GHG',parseModel(b64(SAMPLE_MODELS.satchel)),{color:'#582a12'});
  renderBodyRows();renderParts();}
$('clearAll').onclick=()=>{parts.length=0;target=null;
  for(const [id] of SLOT_INFO){const s=slots[id].surface;s.img=null;s.name='';s.color=slots[id].defColor;paint(s);}
  renderBodyRows();renderParts();say('Plain minifig. Load textures or parts to dress it.');};
renderBodyRows();renderParts();requestAnimationFrame(frame);
loadExample().catch(e=>say('The example could not load: '+e.message,true));
</script>
