<script>
const SLOT_INFO=[
  ['head','Head','#e3b48a',/HEAD|FACE/],['body_front','Torso front','#f2f2f0',/BODY.*FRONT|TORSO.*FRONT|FRONT/],['body_back','Torso back','#f2f2f0',/BODY.*BACK|TORSO.*BACK|BACK/],
  ['arm_l','Left arm','#f2f2f0',/ARM.*LEFT|LEFT.*ARM/],['arm_r','Right arm','#f2f2f0',/ARM.*RIGHT|RIGHT.*ARM/],
  ['hand_r','Right hand','#1b1b1b',/HAND.*RIGHT|RIGHT.*HAND/],['hand_l','Left hand','#1b1b1b',/HAND/],['hips','Hips','#f2f2f0',/HIP/],['leg_l','Left leg','#f2f2f0',/LEG.*LEFT|LEFT.*LEG/],
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
  gl.enable(gl.DEPTH_TEST);gl.enable(gl.BLEND);gl.frontFace(gl.CW);   // the mirrored view turns the winding round
  gl.blendFunc(gl.SRC_ALPHA,gl.ONE_MINUS_SRC_ALPHA);}
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
  // the game's coordinates are left-handed: mirror the screen so left and right match the game
  const pr=M.persp(30*Math.PI/180,w/h,0.01,20);pr[0]=-pr[0];const vp=M.mul(pr,M.look(eye,cam.tgt,[0,1,0]));
  if(!disc){const k=48,pos=[0,-0.001,0],uv=[0,0],idx=[];for(let i=0;i<=k;i++){const a=i/k*Math.PI*2;pos.push(Math.cos(a)*0.3,-0.001,Math.sin(a)*0.3);uv.push(0,0);if(i)idx.push(0,i,i+1);}
    disc=geom(new Float32Array(pos),new Float32Array(uv),null,new Uint16Array(idx));}
  const m0=M.model(0,0,0,0);gl.depthMask(false);drawMesh(disc,M.mul(vp,m0),m0,eye,null,[0,0,0,0.10]);gl.depthMask(true);
  const mb=M.model(figRot,0,0,0),mvpb=M.mul(vp,mb);const dy=headMode=='up'?HEAD_UP:0;lastView={vp,dy,w,h};
  const hid=CH?CH.hidden:null;
  for(const m of bodyMeshes){if(!showBody())continue;const onHead=m.slot=='head'||m.slot=='neck';if(onHead&&headMode=='hidden')continue;if(hid&&hid.has(m.follows||m.slot))continue;
    const mm=M.model(figRot,0,(onHead?dy:0)+liftAt(SLOT_LEVEL[m.slot]||0),0),mvp=M.mul(vp,mm);
    if(m.follows){const c=hexRGB(slots[m.follows].surface.color);drawMesh(m.g,mvp,mm,eye,null,[c[0],c[1],c[2],1]);}
    else drawMesh(m.g,mvp,mm,eye,slots[slotFor(m.slot)].surface);}
  for(const p of parts){if(!p.visible)continue;const mp=partMatrix(p,dy),mvp=M.mul(vp,mp);p.meshes.forEach((g,i)=>{const m=p.pieceMat&&p.pieceMat[i];if(m==='hide'||!pieceShown(p,i))return;drawMesh(g,mvp,mp,eye,m?m.surface:p.surface);});}}
// droid bodies carry several tops: show the one the character uses (custom_astromech_hat_a … in its prefab)
function pieceShown(p,i){const pc=p.model&&p.model.pieces[i];if(!pc||!pc.variant)return true;const vs=p.model.variants;
  let want=(CH&&CH.droidTop)||'A';if(!vs.includes(want))want=vs[0];return pc.variant==want;}
function partMatrix(p,dy){const m=M.model(figRot,p.place.x,p.place.y+(followsHead(p)?dy:0)+liftAt(levelOfPart(p)),p.place.z);return p.fit?M.mul(m,p.fit.mat):m;}
// orbit: drag to turn, wheel or pinch to zoom, shift-drag (or right-drag) to move up and down
const ptrs=new Map();let lastPinch=0,downAt=null;
canvas.addEventListener('pointerdown',(e)=>{canvas.setPointerCapture(e.pointerId);ptrs.set(e.pointerId,[e.clientX,e.clientY,e.shiftKey||e.button==2]);downAt=ptrs.size==1&&e.button==0?[e.clientX,e.clientY]:null;});
const endPtr=(e)=>{if(e.type=='pointerup'&&downAt&&ptrs.size==1&&Math.hypot(e.clientX-downAt[0],e.clientY-downAt[1])<5){const hit=pickAt(e.clientX,e.clientY);if(hit)select(hit);}
  ptrs.delete(e.pointerId);lastPinch=0;downAt=null;};canvas.addEventListener('pointerup',endPtr);canvas.addEventListener('pointercancel',endPtr);
canvas.addEventListener('contextmenu',(e)=>e.preventDefault());
canvas.addEventListener('pointermove',(e)=>{if(!ptrs.has(e.pointerId))return;const [x,y,pan]=ptrs.get(e.pointerId);const dx=e.clientX-x,dy=e.clientY-y;ptrs.set(e.pointerId,[e.clientX,e.clientY,pan]);
  if(ptrs.size==2){const [a,b]=[...ptrs.values()];const d=Math.hypot(a[0]-b[0],a[1]-b[1]);if(lastPinch)cam.dist=Math.min(3,Math.max(0.15,cam.dist*lastPinch/d));lastPinch=d;return;}
  if(pan){cam.tgt[1]=Math.min(0.6,Math.max(-0.1,cam.tgt[1]+dy*0.001*cam.dist));return;}
  cam.az+=dx*0.008;cam.el=Math.min(1.45,Math.max(-1.2,cam.el+dy*0.006));});
canvas.addEventListener('wheel',(e)=>{e.preventDefault();cam.dist=Math.min(3,Math.max(0.15,cam.dist*Math.exp(e.deltaY*0.001)));},{passive:false});
// ---------- click a part or body piece in the view to select it
let lastView=null,pickProg=null,pickLoc=null,pickU=null,pickFB=null,pickSize=[0,0],pickRB=null,pickTx=null;
const showBody=()=>!CH||!!CH.C.body;
function pickAt(cx,cy){if(!gl||!lastView)return null;const {vp,dy,w,h}=lastView;
  if(!pickProg){const sh=(t,src)=>{const o=gl.createShader(t);gl.shaderSource(o,src);gl.compileShader(o);return o;};pickProg=gl.createProgram();
    gl.attachShader(pickProg,sh(gl.VERTEX_SHADER,'attribute vec3 p;uniform mat4 mvp;void main(){gl_Position=mvp*vec4(p,1.);}'));
    gl.attachShader(pickProg,sh(gl.FRAGMENT_SHADER,'precision mediump float;uniform vec4 c;void main(){gl_FragColor=c;}'));gl.linkProgram(pickProg);
    pickLoc=gl.getAttribLocation(pickProg,'p');pickU={mvp:gl.getUniformLocation(pickProg,'mvp'),c:gl.getUniformLocation(pickProg,'c')};}
  if(!pickFB||pickSize[0]!=w||pickSize[1]!=h){pickFB=pickFB||gl.createFramebuffer();pickTx=pickTx||gl.createTexture();pickRB=pickRB||gl.createRenderbuffer();
    gl.bindTexture(gl.TEXTURE_2D,pickTx);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,w,h,0,gl.RGBA,gl.UNSIGNED_BYTE,null);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.NEAREST);
    gl.bindRenderbuffer(gl.RENDERBUFFER,pickRB);gl.renderbufferStorage(gl.RENDERBUFFER,gl.DEPTH_COMPONENT16,w,h);
    gl.bindFramebuffer(gl.FRAMEBUFFER,pickFB);gl.framebufferTexture2D(gl.FRAMEBUFFER,gl.COLOR_ATTACHMENT0,gl.TEXTURE_2D,pickTx,0);gl.framebufferRenderbuffer(gl.FRAMEBUFFER,gl.DEPTH_ATTACHMENT,gl.RENDERBUFFER,pickRB);pickSize=[w,h];}
  gl.bindFramebuffer(gl.FRAMEBUFFER,pickFB);gl.viewport(0,0,w,h);gl.disable(gl.BLEND);gl.clearColor(0,0,0,0);gl.clear(gl.COLOR_BUFFER_BIT|gl.DEPTH_BUFFER_BIT);
  gl.useProgram(pickProg);for(let i=0;i<8;i++)gl.disableVertexAttribArray(i);gl.enableVertexAttribArray(pickLoc);
  const hits=[null];const draw=(g,model,ref)=>{if(!g)return;hits.push(ref);const id=hits.length-1;
    gl.uniformMatrix4fv(pickU.mvp,false,M.mul(vp,model));gl.uniform4f(pickU.c,(id&255)/255,(id>>8&255)/255,0,1);
    gl.bindBuffer(gl.ARRAY_BUFFER,g.p);gl.vertexAttribPointer(pickLoc,3,gl.FLOAT,false,0,0);gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER,g.i);gl.drawElements(gl.TRIANGLES,g.count,gl.UNSIGNED_SHORT,0);};
  if(showBody())for(const m of bodyMeshes){const onHead=m.slot=='head'||m.slot=='neck';if(onHead&&headMode=='hidden')continue;if(CH&&CH.hidden.has(m.follows||m.slot))continue;
    draw(m.g,M.model(figRot,0,(onHead?dy:0)+liftAt(SLOT_LEVEL[m.slot]||0),0),slots[m.follows||slotFor(m.slot)]);}
  for(const p of parts){if(!p.visible)continue;const mp=partMatrix(p,dy);p.meshes.forEach((g,i)=>{if(pieceShown(p,i))draw(g,mp,p);});}
  const r=canvas.getBoundingClientRect();const x=Math.round((cx-r.left)*w/r.width),y=Math.round(h-(cy-r.top)*h/r.height);const px=new Uint8Array(4);gl.readPixels(x,y,1,1,gl.RGBA,gl.UNSIGNED_BYTE,px);
  gl.bindFramebuffer(gl.FRAMEBUFFER,null);gl.useProgram(prog);gl.enable(gl.BLEND);gl.disableVertexAttribArray(pickLoc);
  return px[3]?hits[px[0]+(px[1]<<8)]||null:null;}
// select a body slot or a part: it becomes where textures go, and its row opens in the panel
function select(ref){const isPart=parts.includes(ref);const key=isPart?'p'+ref.id:ref.id;expanded.add(key);setTarget({ref,el:null});
  renderBodyRows();renderParts();if(target&&target.el){target.el.scrollIntoView({block:'nearest',behavior:'smooth'});}
  say((isPart?ref.name:ref.label)+' selected: textures you load now go to it.');}
$('spin').onclick=()=>{spinning=!spinning;$('spin').setAttribute('aria-pressed',spinning);$('spin').classList.toggle('primary',spinning);};
$('front').onclick=()=>view(1);$('back').onclick=()=>view(-1);

// ---------- materials: base colour + print in the colour map, then optional normal, BRDF and emission maps
const pot=(v)=>{let p=4;while(p<v&&p<1024)p*=2;return p;};
function makeSurface(color,hasUV1){const maps={};for(const [k] of MAPS)maps[k]={img:null,name:'',uv:(k!='color'&&hasUV1)?1:0,uvDef:(k!='color'&&hasUV1)?1:0,flip:false,glTex:null,dirty:true,dx:true,src:null,game:null,cleared:false};
  const c=document.createElement('canvas');c.width=c.height=4;const s={color,maps,canvas:c,glTex:null,dirty:true};paintColor(s);return s;}
function paintColor(s){const m=s.maps.color,w=m.img?pot(m.img.width):4,h=m.img?pot(m.img.height):4;s.canvas.width=w;s.canvas.height=h;
  const g=s.canvas.getContext('2d');g.fillStyle=s.color;g.fillRect(0,0,w,h);if(m.img)g.drawImage(m.img,0,0,w,h);s.dirty=true;}
function upload(tex,src){gl.bindTexture(gl.TEXTURE_2D,tex);gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL,false);gl.texImage2D(gl.TEXTURE_2D,0,gl.RGBA,gl.RGBA,gl.UNSIGNED_BYTE,src);gl.generateMipmap(gl.TEXTURE_2D);
  gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MIN_FILTER,gl.LINEAR_MIPMAP_LINEAR);gl.texParameteri(gl.TEXTURE_2D,gl.TEXTURE_MAG_FILTER,gl.LINEAR);}
function colorTex(s){if(!s.glTex)s.glTex=gl.createTexture();if(s.dirty){upload(s.glTex,s.canvas);s.dirty=false;}else gl.bindTexture(gl.TEXTURE_2D,s.glTex);return s.glTex;}
function potCanvas(img){const w=pot(img.width),h=pot(img.height);if(img.width==w&&img.height==h)return img;const c=document.createElement('canvas');c.width=w;c.height=h;c.getContext('2d').drawImage(img,0,0,w,h);return c;}
function mapTex(m){if(!m.glTex)m.glTex=gl.createTexture();if(m.dirty){upload(m.glTex,potCanvas(m.img));m.dirty=false;}else gl.bindTexture(gl.TEXTURE_2D,m.glTex);return m.glTex;}
// meta: src = the user's file (exported as a new texture), game = a texture the base character already references, cleared = removed on purpose
function setMap(s,kind,img,name,meta={}){const m=s.maps[kind];m.img=img;m.name=name;m.dirty=true;m.src=meta.src||null;m.game=meta.game||null;m.cleared=!!meta.cleared;if(kind=='color')paintColor(s);}
async function loadImage(file){const n=file.name.toLowerCase();const buf=await file.arrayBuffer();let r;
  if(n.endsWith('.dds'))r=parseDDS(buf);else if(n.endsWith('.texture'))r=parseGameTexture(buf);
  else{const bmp=await createImageBitmap(new Blob([buf]));const c=document.createElement('canvas');c.width=bmp.width;c.height=bmp.height;c.getContext('2d').drawImage(bmp,0,0);return c;}
  const c=document.createElement('canvas');c.width=r.w;c.height=r.h;c.getContext('2d').putImageData(new ImageData(r.rgba,r.w,r.h),0,0);return c;}
function imgFromURL(url){return new Promise((res,rej)=>{const i=new Image();i.onload=()=>{const c=document.createElement('canvas');c.width=i.width;c.height=i.height;c.getContext('2d').drawImage(i,0,0);res(c);};i.onerror=()=>rej(new Error('bad image'));i.src=url;});}

// ---------- head height: some characters lift the neck joint by 1 cm, raising the head and everything on it
const HEAD_UP=0.01;let headMode='normal';
const followsHead=(p)=>(p.fit?HEAD_BONES.test(p.fit.bone):false)||/^(hat|hair|facetex|head|helmet)/i.test(p.entityName||p.name)||/\/(hat|hair|facetex)\//i.test(p.newModel?p.newModel.path:(p.origModel||''));
function setHead(m){headMode=m;for(const b of document.querySelectorAll('#headSeg button'))b.setAttribute('aria-pressed',b.dataset.h==m);
  let note='';if(CH){if(m=='hidden')note='Hidden only here: the exported character keeps its head.';else if(!CH.C.neck&&m=='up')note=CH.C.baseName+' has no neck lift to change, so it exports at normal height.';
    else if(CH.C.neck)note='Exported with the head '+(m=='up'?'raised by 1 cm':'at normal height')+' ('+CH.C.neck.entity+' sets the neck lift).';}
  else if(m=='up')note='Up is the height of characters whose neck joint is lifted by 1 cm.';
  $('headNote').textContent=note;}
for(const b of document.querySelectorAll('#headSeg button'))b.onclick=()=>setHead(b.dataset.h);
// ---------- body
const b64=(s)=>{const bin=atob(s);const u=new Uint8Array(bin.length);for(let i=0;i<bin.length;i++)u[i]=bin.charCodeAt(i);return u.buffer;};
const slots={};for(const [id,label,color] of SLOT_INFO)slots[id]={id,label,surface:makeSurface(color,true),defColor:color};
let bodyMeshes=[];const STD_BODY=bodyMeshes;
for(const p of BODY){const pos=new Float32Array(b64(p.pos)),uv0=new Float32Array(b64(p.uv0)),uv=new Float32Array(b64(p.uv)),idx=new Uint16Array(b64(p.idx));
  if(p.slot!='hands'){bodyMeshes.push({g:geom(pos,uv0,uv,idx),slot:p.slot,follows:HIDDEN_SLOTS[p.slot]||null});continue;}
  // the hands are one piece in the model: split it by side so each hand has its own material (materials 7 and 8)
  for(const [slot,left] of [['hand_l',true],['hand_r',false]]){const keep=[];
    for(let i=0;i<idx.length;i+=3){const cx=pos[idx[i]*3]+pos[idx[i+1]*3]+pos[idx[i+2]*3];if((cx<0)==left)keep.push(idx[i],idx[i+1],idx[i+2]);}
    bodyMeshes.push({g:geom(pos,uv0,uv,new Uint16Array(keep)),slot,follows:null});}}
// a character's own minifig-shaped body (customiser, small): each piece goes to the body slot it sits in, judged against its skeleton
function customBody(model){const B={};for(const b of model.bones||[])B[b.name]=b.world;const y=(n,d)=>B[n]?B[n][13]:d;
  const neck=y('Neck',0.289),hips=y('Hips',0.113),lh=B.LeftHand,rh=B.RightHand;const out=[];
  for(const pc of model.pieces){const c=[0,1,2].map(i=>(pc.mn[i]+pc.mx[i])/2);let slot;
    const near=(h)=>h&&Math.hypot(c[0]-h[12],c[1]-h[13],c[2]-h[14])<0.035;
    if(c[1]>neck-0.004)slot='head';else if(near(lh)||near(rh))slot=c[0]<0?'hand_l':'hand_r';
    else if(Math.abs(c[0])>0.055&&c[1]>hips)slot=c[0]<0?'arm_l':'arm_r';
    else if(Math.abs(c[0])<0.03&&c[1]<hips+0.015&&pc.mx[1]-pc.mn[1]<0.06)slot='hips';
    else if(c[1]>hips)slot=c[2]<0?'body_front':'body_back';   // the face looks along -z
    else slot=c[0]<0?'leg_l':'leg_r';
    out.push({g:geom(pc.pos,pc.uv0,model.hasUV1?pc.uv1:pc.uv0,pc.idx),slot,follows:null});}
  // the hips (and waist) are rigid pieces in these bodies
  const mats=model.materials||[];
  for(const pc of model.rigid||[]){const cy=(pc.mn[1]+pc.mx[1])/2;if(/HIPS/i.test(mats[pc.mat]||'')&&cy>hips-0.04&&cy<neck-0.08&&!out.some(o=>o.slot=='hips'&&o.cy==cy)){
    out.push({g:geom(pc.pos,pc.uv0,model.hasUV1?pc.uv1:pc.uv0,pc.idx),slot:'hips',follows:null,cy});}}
  return out;}
function slotFor(s){return $('swapLR').checked?({arm_l:'arm_r',arm_r:'arm_l',leg_l:'leg_r',leg_r:'leg_l',hand_l:'hand_r',hand_r:'hand_l'}[s]||s):s;}

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
    const nm=document.createElement('span');nm.className='tex';const ref=!owner.newModel&&s.refs&&s.refs[k];
    nm.textContent=m.name||(m.cleared?'removed':ref?'game: '+ref.path.split('/').pop()+(CH?' (drop it to see it)':''):'none');nm.title=m.name||(ref?ref.path:'');
    const sel=document.createElement('select');sel.id='uv_'+ownerKey+'_'+k;sel.setAttribute('aria-label',label+' UV set');
    for(const [v,t] of [[0,'UV 1'],[1,'UV 2']]){const o=document.createElement('option');o.value=v;o.textContent=t;sel.append(o);}sel.value=m.uv;sel.onchange=()=>{m.uv=+sel.value;};
    const fl=document.createElement('label');fl.className='mini';const fc=document.createElement('input');fc.type='checkbox';fc.checked=m.flip;fc.id='flip_'+ownerKey+'_'+k;fc.onchange=()=>{m.flip=fc.checked;};
    fl.append(fc,document.createTextNode('Flip'));fl.title='Flip the texture vertically';
    r.append(l,nm,sel,fl,btn('Load','icon',()=>chooseFor(owner,k),'Load '+label.toLowerCase()+' texture'),
      btn('×','icon',()=>{setMap(s,k,null,'',{cleared:!!(s.refs&&s.refs[k])});refresh();},'Remove '+label.toLowerCase()+' texture'));
    box.append(r);
    if(k=='emission'&&CH){const r3=document.createElement('p');r3.className='hint sub2';r3.textContent='Shown here only: character materials have no emission slot.';box.append(r3);}
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

// ---------- skeleton: a skinned attachment carries bones named like the character's ("Head_match", "Spine2_match", "LeftArm");
// the game puts each on the character's bone, so the part moves by (its bone's bind pose)^-1 x (the character's bone)
function charBones(){if(CH&&CH.bodyModel&&CH.bodyModel.bones){const B={};for(const b of CH.bodyModel.bones)B[b.name]=b.world;return B;}return MINIFIG_BONES;}
function inv4(m){const r=new Float32Array(16);for(let i=0;i<3;i++)for(let j=0;j<3;j++)r[i*4+j]=m[j*4+i];
  for(let j=0;j<3;j++)r[12+j]=-(m[12]*r[j]+m[13]*r[4+j]+m[14]*r[8+j]);r[15]=1;return r;}
function boneFit(model){if(!model||!model.skinned||!model.bones)return null;const C=charBones();
  for(const b of model.bones){const base=b.name.replace(/_match$/i,'').replace(/^HipsBase$/i,'Hips');if(b.parent<0&&base==b.name)continue;
    const key=Object.keys(C).find(k=>k.toLowerCase()==base.toLowerCase());if(!key)continue;
    return {bone:key,own:b.name,mat:mul4(inv4(b.world),new Float32Array(C[key]))};}
  return null;}
const JOINT_LEVEL={hips:0,hipsbase:0,spinebase:1,spine:2,spine1:3,spine2:4,neck:5,neckdriven:5,head:6,hat:6,hair:6,glasses:6};
const SLOT_LEVEL={hips:0,leg_l:0,leg_r:0,body_front:4,body_back:4,arm_l:4,arm_r:4,hand_l:4,hand_r:4,neck:5,head:6};
const levelOfBone=(n)=>{n=n.toLowerCase();if(n in JOINT_LEVEL)return JOINT_LEVEL[n];if(/arm|hand|shoulder|finger|clav/.test(n))return 4;if(/leg|foot|knee|toe/.test(n))return 0;return 4;};
function levelOfPart(p){if(p.fit){const l=levelOfBone(p.fit.bone);if(l>0||!p.model)return l;
    // a part fitted to the hips whose geometry sits on the torso (Fode and Beed's front arms) moves with the torso
    const ps=p.model.pieces;if(!ps.length)return l;const cy=ps.reduce((a,pc)=>a+(pc.mn[1]+pc.mx[1])/2,0)/ps.length;const B=p.model.bones||[];const hb=B.find(b=>/^hips(base)?$/i.test(b.name));
    return cy-(hb?hb.world[13]:0)>0.03?4:l;}if(followsHead(p))return 6;const y=p.place.y;return y>0.27?5:y>0.12?4:0;}
// the joint lifts of visible attachments that reach this level
function liftAt(level){if(!CH)return 0;let y=0;for(const o of CH.C.offsets){const owner=parts.find(q=>q.entityName==o.entity);if(owner&&!owner.visible)continue;
  if(level>=levelOfBone(o.joint))y+=o.off[1];}return y;}
const HEAD_BONES=/^(head|neck|neckdriven|hat|hair|glasses)$/i;
// parts that replace pieces of the body: a custom head (face models with their own head) or whole arms
function hiddenSlots(){const h=new Set();for(const p of parts){if(!p.visible||!p.model||!p.model.bones)continue;const names=p.model.bones.map(b=>b.name);
    if(names.some(n=>/^(head|neck)_match$/i.test(n))&&!/super_face|facetex/i.test(p.origModel||p.name))h.add('head');
    for(const [bone,slots] of [['LeftArm',['arm_r']],['RightArm',['arm_l']]])if(p.fit&&p.fit.own==bone)slots.forEach(x=>h.add(x));}
  return h;}
// ---------- several materials in one model: the draw list says which material each piece uses; the prefab's overrides are
// matched to the materials by name (head_fode_diff -> Fode_MAT); shadow impostors and occluders are not drawn
const HIDE_MAT=/shadow|occ?ulder|occluder/i;
const nameToks=(s)=>s.replace(/([a-z])([A-Z])/g,'$1_$2').toLowerCase().split(/[^a-z0-9]+/).map(t=>t.replace(/\d+$/,'')).filter(t=>t.length>=3&&!/^(mat|dx11|variant|auto)$/.test(t));
function matchMaterials(names,ovs){const res=new Array(names.length).fill(null);const used=new Set();const pairs=[];
  names.forEach((n,i)=>{if(HIDE_MAT.test(n))return;const tk=nameToks(n);for(const o of ovs){const txt=Object.values(o.refs).map(r=>r.path.split('/').pop()).join(' ');const sc=tk.filter(t=>txt.includes(t)).length;if(sc)pairs.push([sc,i,o]);}});
  pairs.sort((a,b)=>b[0]-a[0]||a[1]-b[1]||a[2].id-b[2].id);
  for(const [sc,i,o] of pairs){if(res[i]||used.has(o))continue;res[i]=o;used.add(o);}
  const free=ovs.filter(o=>!used.has(o));let k=0;names.forEach((n,i)=>{if(!res[i]&&!HIDE_MAT.test(n)&&k<free.length)res[i]=free[k++];});
  return res;}
function setupMats(p){p.mats=null;p.pieceMat=null;const m=p.model;if(!m||!m.materials||m.materials.length<2)return;
  const hasUV1=m.hasUV1;let ovs;
  if(p.ovs&&p.ovs.length>1)ovs=p.ovs;else if(!p.ovs)ovs=m.materials.filter(n=>!HIDE_MAT.test(n)).map((n,i)=>({id:null,label:n,refs:{},tint:null}));else return;
  const mats=ovs.map(o=>{const surface=makeSurface(o.tint||p.surface.color,hasUV1);surface.refs=o.refs;surface.origColor=surface.color;uvDefaults(surface,m);
    return {id:o.id,label:o.label||'',surface,ov:o};});
  const byOv=new Map(mats.map(x=>[x.ov,x]));
  const match=p.ovs?matchMaterials(m.materials,ovs):m.materials.map(n=>ovs.find(o=>o.label==n)||null);
  match.forEach((o,i)=>{if(o){const x=byOv.get(o);if(!x.label)x.label=m.materials[i];else if(!x.label.includes(m.materials[i]))x.label+=', '+m.materials[i];}});
  p.mats=mats;p.pieceMat=m.pieces.map(pc=>pc.mat<0?null:HIDE_MAT.test(m.materials[pc.mat]||'')?'hide':(match[pc.mat]?byOv.get(match[pc.mat]):null));}
function allOwners(){const o=[...Object.values(slots),...parts];for(const p of parts)if(p.mats)o.push(...p.mats);return o;}
// ---------- parts
const parts=[];let partSeq=0;
function defaultPlacement(name,model){const n=name.toUpperCase();if(!model)return {y:0};
  if(/HAT|HELMET|HAIR/.test(n))return {y:0.3742};      // helmet height used by the clone trooper prefabs
  if(/FACE/.test(n))return {y:0.2999};                 // face models (super_face) sit on the head
  if(/PAULDRON/.test(n))return {y:0.2891};             // neck locator
  if(/SKIRT|KAMA/.test(n))return {y:0.104};
  return {y:model.skinned?0:0.3742};}
function addPart(name,model,opts={}){
  const p={id:++partSeq,name,model,surface:makeSurface(opts.color||'#f2f2f0',model?model.hasUV1:true),meshes:[],visible:true,
    place:Object.assign({x:0,y:0,z:0},opts.place||defaultPlacement(name,model))};
  if(model){p.meshes=meshesOf(model);uvDefaults(p.surface,model);setupMats(p);p.fit=boneFit(model);if(p.fit)p.place={x:0,y:0,z:0};}parts.push(p);if(!opts.collapsed)expanded.add('p'+p.id);return p;}
// parts whose colour sits in the material's base layer (diffuse_0), which the game maps with the second UV pair
const baseLayer=(n)=>/PAULDRON|TORSOATTACHMENT|ADDITIONALMODEL|FACETEX/i.test(n);
function meshesOf(model){return model.pieces.map(pc=>geom(pc.pos,pc.uv0,model.hasUV1?pc.uv1:pc.uv0,pc.idx));}
// default UV sets: rigid models (.gsc, helmets) print on UV 2 and take their maps from UV 1; skinned ones the other way round
function uvDefaults(surface,model){if(!model)return;const rigid=!model.skinned;for(const [k] of MAPS){const m=surface.maps[k];const d=model.hasUV1?(k=='color'?(rigid?1:0):(rigid?0:1)):0;
  if(m.uv===m.uvDef)m.uv=d;m.uvDef=d;}}
function renderParts(){if(CH)CH.hidden=hiddenSlots();const box=$('partList');box.textContent='';$('noParts').hidden=parts.length>0;
  for(const p of parts){const el=document.createElement('div');el.className='part';const key='p'+p.id;
    const head=document.createElement('div');head.className='part-head';
    const vis=document.createElement('input');vis.type='checkbox';vis.checked=p.visible;vis.id='vis_'+p.id;vis.setAttribute('aria-label','Show '+p.name);vis.onchange=()=>{p.visible=vis.checked;if(CH)CH.hidden=hiddenSlots();};
    const nm=document.createElement('button');nm.className='pname';nm.textContent=p.name;nm.title='Show or hide this part\'s textures; dropped textures with no clear name go here';nm.setAttribute('aria-expanded',expanded.has(key));
    nm.onclick=()=>{if(expanded.has(key))expanded.delete(key);else expanded.add(key);setTarget({ref:p,el});renderParts();};
    const col=document.createElement('input');col.type='color';col.value=p.surface.color;col.id='pcol_'+p.id;col.setAttribute('aria-label',p.name+' colour');
    col.oninput=()=>{p.surface.color=col.value;paintColor(p.surface);};
    if(p.ownBody){vis.disabled=true;head.append(vis,nm,col);}
    else if(p.entity){vis.setAttribute('aria-label','Include '+p.entityName+' in the character');vis.title='Part of the character: untick to leave it out of the exported character';
      head.append(vis,nm,col,btn('Model…','icon',()=>{pending={replace:p};$('fileInput').click();},'Replace the model of '+p.entityName));}
    else head.append(vis,nm,col,btn('Remove','icon',()=>{parts.splice(parts.indexOf(p),1);if(target&&target.ref===p)target=null;renderParts();}));
    el.append(head);
    if(CH)el.append(partSlotLine(p));
    if(expanded.has(key)){if(p.entity&&p.newModel)el.append(specialEditor(p));
      if(p.mats){const h=document.createElement('p');h.className='hint sub2';h.textContent=p.mats.length+' materials: each has its own colour and textures.';el.append(h);
        for(const m of p.mats){const mk=key+'_m'+(m.id??m.label);const row=document.createElement('div');row.className='part-head';row.style.paddingLeft='14px';
          const c=document.createElement('input');c.type='color';c.value=m.surface.color;c.setAttribute('aria-label','Colour of '+m.label);c.oninput=()=>{m.surface.color=c.value;paintColor(m.surface);};
          const b=document.createElement('button');b.className='part-name';b.textContent=(m.id!=null?'Material '+m.id+' · ':'')+m.label;b.setAttribute('aria-expanded',expanded.has(mk));
          b.onclick=()=>{expanded.has(mk)?expanded.delete(mk):expanded.add(mk);setTarget({ref:m,el:null});renderParts();};
          row.append(c,b);el.append(row);if(expanded.has(mk))el.append(mapEditor(m,mk,renderParts));}}
      else el.append(mapEditor(p,key,renderParts));
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
  for(const p of parts){const toks=p.name.toUpperCase().replace(/\.[A-Z]+$/,'').split(/[_\-\s.]+/).filter(t=>t.length>=4&&!/\d/.test(t)&&!/^(DX11|ADDITIONALMODEL|SUPER|CHARACTER|MINIFIG|FACE|HAIR|SKINNEDSKIRT|FABRIC|CAPE)$/.test(t));
    if(toks.some(t=>n.includes(t)))return p;}
  if(/HAT|HELMET/.test(n)){const h=parts.find(p=>/HAT|HELMET/i.test(p.name));if(h)return h;}
  for(const [id,,,re] of SLOT_INFO)if(re.test(n))return slots[id];
  return null;}
async function handleFiles(files,forced){let placed=0;const skipped=[];
  // a character first, then its preview scene, then everything else (so game files can match its references)
  const rank=(n)=>/\.prefab_baked$/i.test(n)?0:/\.scene_baked$/i.test(n)?1:/\.(pc_shaders|nxg_textures)$/i.test(n)?2:3;
  files=[...files].sort((a,b)=>rank(a.name)-rank(b.name));
  for(const f of files){const n=f.name;try{
    if(/\.(prefab|scene)_baked$/i.test(n)){say('Reading '+n+'…');await new Promise(r=>setTimeout(r,20));
      loadCharacter(n,new Uint8Array(await f.arrayBuffer()));placed++;continue;}
    if(/\.(pc_shaders|nxg_textures)$/i.test(n)){extraFiles.set(n.toLowerCase(),{name:n,data:new Uint8Array(await f.arrayBuffer())});placed++;continue;}
    if(/\.(gsc|ghg)$/i.test(n)){const buf=await f.arrayBuffer();const model=parseModel(buf);if(!model.pieces.length)throw new Error('no visible mesh in this model');
      const bytes=new Uint8Array(buf);
      if(forced&&forced.replace){const no=fits(forced.replace,model,n);if(no){skipped.push(no);continue;}setPartModel(forced.replace,n,model,bytes);renderParts();placed++;continue;}
      if(CH&&CH.bodyModelPath&&keyOf(CH.bodyModelPath)==keyOf(n)){CH.bodyModel=model;bodyMeshes=customBody(model);
        for(const q of parts)if(q.model){q.fit=boneFit(q.model);if(q.fit)q.place={x:0,y:0,z:0};}placed++;continue;}
      const own=CH&&parts.find(p=>p.entity&&keyOf(p.origModel)==keyOf(n));
      if(own){setPartModel(own,n,model,null);renderParts();placed++;continue;}
      const p=addPart(n,model);p.bytes=bytes;renderParts();select(p);placed++;continue;}
    if(!/\.(dds|texture|png|jpe?g|webp)$/i.test(n)){skipped.push(n+' (not a texture or model)');continue;}
    if(!forced&&WEATHER.test(n)){skipped.push(n+' (weather overlay: not shown here)');continue;}
    // a texture the loaded character already uses: show it wherever it is used
    const game=CH&&CH.refIndex.get(keyOf(n));
    if(game&&!forced){const img=await loadImage(f);let hit=0;
      for(const o of allOwners())if(o.surface.refs)for(const [k,r] of Object.entries(o.surface.refs))if(r.path==game.path){setMap(o.surface,k,img,n,{game});hit++;}
      if(hit){placed++;continue;}}
    const owner=forced?forced.ref:((target&&target.ref)||ownerOf(n));   // the part you selected wins over a guess from the name
    if(!owner){skipped.push(n+' (the name does not say which part it is for: click a part or body row name, then load it again)');continue;}
    const kind=forced&&forced.kind?forced.kind:kindOf(n);
    setMap(owner.surface,kind,await loadImage(f),n,game?{game}:{src:f});placed++;
  }catch(err){skipped.push(n+' ('+err.message+')');}}
  renderBodyRows();renderParts();renderCharacter();
  if(skipped.length)say((placed?'Loaded '+placed+'. ':'')+'Skipped: '+skipped.join('; '),placed==0);else if(placed)say('Loaded '+placed+' file'+(placed>1?'s':'')+'.');}
let dragDepth=0;
window.addEventListener('dragenter',(e)=>{e.preventDefault();dragDepth++;$('drop').hidden=false;});
window.addEventListener('dragleave',()=>{dragDepth=Math.max(0,dragDepth-1);if(!dragDepth)$('drop').hidden=true;});
window.addEventListener('dragover',(e)=>e.preventDefault());
window.addEventListener('drop',(e)=>{e.preventDefault();dragDepth=0;$('drop').hidden=true;handleFiles([...e.dataTransfer.files],null);});

// ---------- character prefab: load a game character, dress it, export it as a mod
// The prefab carries the character's abilities, voice and behaviour; its parts and materials are what can change.
const TEX_FOLDER={head:'head',body_front:'body',body_back:'body',arm_l:'arm',arm_r:'arm',hand_l:'hand',hand_r:'hand',hips:'hip',leg_l:'leg',leg_r:'leg'};
const TEX_PART={head:'',body_front:'front',body_back:'back',arm_l:'left',arm_r:'right',hand_l:'left',hand_r:'right',hips:'',leg_l:'left',leg_r:'right'};
const KIND_TAG={color:'diff',normal:'nrm',brdf:'brdf'};
const FACTIONS=[['','Same as the base character'],['0','Jedi'],['1','Sith'],['2','Hero'],['3','Bounty Hunter'],['4','Astromech Droid'],['5','Protocol Droid'],['6','Scoundrel'],['7','Villain'],['8','Scavenger'],['9','Civilian']];
let CH=null;
const extraFiles=new Map();   // .pc_shaders / .nxg_textures that go with a replacement model
const keyOf=(n)=>n.toLowerCase().split('/').pop().replace(/\.[a-z0-9_]+$/,'').replace(/_dx11$/,'');
const hexOf=(rgb)=>'#'+rgb.map(c=>PF.srgb(c).toString(16).padStart(2,'0')).join('');
const linOf=(hex)=>hexRGB(hex).map(c=>PF.lin(c*255));
const baseFile=(p)=>p.split('/').pop();
function refsOf(C,r,ids){const out={};
  for(const ov of r.overrides){if(ids&&!ids.includes(ov.id))continue;
    for(const [f,s] of Object.entries(ov.slots)){if(!s.path)continue;
      const k=f.startsWith('diffuse')?'color':f=='normal_0'?'normal':f=='specular_0'?'brdf':null;
      if(k&&!out[k])out[k]={field:f,path:s.path,id:C.d.slice(s.pos+5,s.pos+21)};}}
  return out;}
function tintOf(C,r,ids){const ov=r.overrides.find(o=>(!ids||ids.includes(o.id))&&o.tintEn!=null&&C.d[o.tintEn]===1);return ov?hexOf(ov.colour):null;}

function loadCharacter(name,bytes){
  const C=PF.readCharacter(bytes);C.d=bytes;
  const hat=/custom_astromech_hat_([a-z])\.prefab/i.exec(new TextDecoder('latin1').decode(bytes));
  if(C.isScene){
    if(!CH)throw new Error('load the character .prefab_baked first, then its preview scene');
    if(C.baseName.toLowerCase()!=CH.C.baseName.toLowerCase())throw new Error('this preview scene is for '+C.baseName+', not '+CH.C.baseName);
    CH.scene={name,bytes,C};renderCharacter();say('Preview scene added: the roster will show the new look.');return;}
  resetAll();
  CH={name,bytes,C,donors:[],copy:{anim:-1,sounds:-1,items:{}},items:PF.itemsOf(C),droidTop:hat?hat[1].toUpperCase():null,scene:null,refIndex:new Map(),hidden:new Set(),bodyModel:null,
    bodyModelPath:C.body&&!/super_minifig_dx11/i.test(C.body.model.path)?C.body.model.path:null};
  const index=(refs)=>{for(const r of Object.values(refs))CH.refIndex.set(keyOf(r.path),r);};
  if(C.body)for(const [id] of SLOT_INFO){const s=slots[id].surface;const ids=PF.BODY_IDS[id];
    s.refs=refsOf(C,C.body,PF.BODY_TEX_IDS[id]||ids);index(s.refs);
    s.color=tintOf(C,C.body,ids)||slots[id].defColor;s.origColor=s.color;paintColor(s);}
  for(const r of C.attachments){const skinned=/\.ghg$/i.test(r.model.path);
    // every attachment sits where its entity puts it (faces on the head, skirts at the hips)
    const place=r.transform?{x:r.transform.world[0],y:r.transform.world[1],z:r.transform.world[2]}:(r.entity?null:{x:0,y:0,z:0});
    const p=addPart(baseFile(r.model.path),null,{color:tintOf(C,r)||'#f2f2f0',place,collapsed:true});
    if(!place)Object.assign(p.place,defaultPlacement(r.model.path,{skinned}));
    Object.assign(p,{entity:r.key,entityName:r.entity||'(character root)',ownBody:!r.entity,slotKind:slotKind(r),origModel:r.model.path,origSpecial:r.special&&r.special.value,special:r.special&&r.special.value,
      visible:r.enabled,origEnabled:r.enabled,rigid:!skinned,origPlace:{...p.place},origColor:p.surface.color});
    p.surface.refs=refsOf(C,r);index(p.surface.refs);
    p.ovs=r.overrides.filter(o=>o.id>=1).map(o=>({id:o.id,refs:refsOf(C,r,[o.id]),tint:(o.tintEn!=null&&C.d[o.tintEn]===1)?hexOf(o.colour):null}));
    for(const o of p.ovs)index(o.refs);
}
  setHead(C.neck&&Math.abs(C.neck.y-HEAD_UP)<0.002?'up':'normal');CH.origHead=headMode;
  $('bodySec').hidden=!C.body;$('ownBodyNote').hidden=!!C.body;
  // the game's own minifig maps are useful to see but are not what a mod changes
  renderBodyRows();renderParts();renderCharacter();
  $('expName').value='';$('expId').value='';$('expMod').value='';$('expDesc').value='';
  if(GAME.entries.size)setTimeout(autoLoadCharacter,0);
  say('Loaded '+C.baseName+(GAME.entries.size?'.':'. Drop its game textures and models to see it (or connect the game folder), then change what you like and export.'));}

// what a character slot holds: rigid models (.gsc) attach to a locator, skinned ones (.ghg) follow the skeleton
function slotKind(r){const m=r.model.path.toLowerCase();const folder=m.split('/').slice(-2,-1)[0]||'';
  return {skinned:/\.ghg$/.test(m),label:(!r.entity?'body model':folder.replace(/^super_/,'').replace(/_/g,' '))+(/\.ghg$/.test(m)?' (skinned)':' (rigid)')};}
function fits(p,model,name){if(!p.slotKind)return null;const sk=/\.ghg$/i.test(name);
  if(p.ownBody)return 'the character\'s own body model cannot be swapped here';
  if(sk!==p.slotKind.skinned)return name+' is '+(sk?'a skinned model (.ghg)':'a rigid model (.gsc)')+' but '+p.entityName+' holds '+(p.slotKind.skinned?'skinned':'rigid')+' models: the game would not show it there';
  return null;}
function setPartModel(p,name,model,bytes){p.model=model;p.meshes=meshesOf(model);uvDefaults(p.surface,model);setupMats(p);p.fit=p.ownBody?null:boneFit(model);if(p.fit){p.place={x:0,y:0,z:0};p.origPlace={x:0,y:0,z:0};}
  if(p.entity&&bytes){const path=PF.selfPath(bytes).toLowerCase();
    if(keyOf(path)==keyOf(p.origModel)){p.newModel=null;p.name=name;return;}
    const names=PF.specialNames(bytes);
    p.newModel={name,bytes,path,selfPath:PF.selfPath(bytes),names};p.name=name;
    p.special=/\.ghg$/i.test(path)?'[All Specials]':(names[0]||'[All Specials]');
    p.rigid=!/\.ghg$/i.test(path);if(!p.rigid&&!p.origPlace)Object.assign(p.place,defaultPlacement(name,model));
    expanded.add('p'+p.id);}
  else p.name=name;}

// one line under a part: which character slot it fills
function partSlotLine(p){const l=document.createElement('div');l.className='hint slotline';
  if(p.entity){l.textContent=(p.ownBody?'Body model':'Slot '+p.entityName+' · '+p.slotKind.label)+(p.newModel?' · new model (game had '+baseFile(p.origModel)+')':p.model?'':' · '+baseFile(p.origModel)+' not loaded');
    if(p.newModel){const u=btn('Undo','icon',()=>{p.newModel=null;p.model=null;p.meshes=[];p.name=baseFile(p.origModel);p.special=p.origSpecial;p.rigid=!/\.ghg$/i.test(p.origModel);renderParts();},'Go back to the game model');u.style.marginLeft='6px';l.append(u);}
    return l;}
  l.textContent='Shown here only: the game won\'t have it unless it fills a slot. Use it as: ';const sel=document.createElement('select');sel.setAttribute('aria-label','Character slot for '+p.name);
  const o0=document.createElement('option');o0.value='';o0.textContent='nothing (view only)';sel.append(o0);
  for(const q of parts.filter(q=>q.entity&&!q.ownBody)){const o=document.createElement('option');o.value=q.entity;const no=fits(q,p.model,p.name);
    o.textContent=q.entityName+' · '+q.slotKind.label+(no?' (won\'t fit)':'');o.disabled=!!no;sel.append(o);}
  sel.onchange=()=>{const q=parts.find(x=>x.entity==sel.value);if(!q)return;
    setPartModel(q,p.name,p.model,p.bytes);for(const [k] of MAPS){const a=p.surface.maps[k];if(a.img)setMap(q.surface,k,a.img,a.name,{src:a.src,game:a.game});}
    q.surface.color=p.surface.color;paintColor(q.surface);q.visible=true;
    parts.splice(parts.indexOf(p),1);renderParts();say(p.name+' now fills '+q.entity+'.');};
  l.append(sel);return l;}
function specialEditor(p){const w=document.createElement('div');w.className='part-grid';
  const l=document.createElement('label');l.textContent='Part name';l.htmlFor='sp_'+p.id;
  const i=document.createElement('input');i.id='sp_'+p.id;i.value=p.special||'';i.setAttribute('list','spl_'+p.id);i.className='txt';
  i.title='The model part the game draws. Helmets name one (e.g. Hat_Cl2ab); [All Specials] draws everything.';
  const dl=document.createElement('datalist');dl.id='spl_'+p.id;for(const n of ['[All Specials]',...p.newModel.names]){const o=document.createElement('option');o.value=n;dl.append(o);}
  i.oninput=()=>{p.special=i.value;};w.append(l,i,dl);return w;}


// ---------- copying animations, voice and sounds, and item looks from other characters
const cleanName=(s)=>s.replace(/\0[\s\S]*$/,'');
const itemLabel=(it)=>cleanName(it.name)+' ('+it.model.split('/').pop().replace(/_dx11\.gsc$/i,'').toLowerCase()+')';
function addDonor(name,bytes){if(!CH)return;let C;try{C=PF.readCharacter(bytes);}catch(e){say('Could not read '+name+': '+e.message,true);return;}
  if(C.baseName.toLowerCase()==CH.C.baseName.toLowerCase()){say(C.baseName+' is the base already.',true);return;}
  if(CH.donors.some(d=>d.C.baseName.toLowerCase()==C.baseName.toLowerCase())){say(C.baseName+' is in the list already.');return;}
  CH.donors.push({name:C.baseName,C,items:PF.itemsOf(C),anim:PF.animSet(C),sounds:PF.soundPairs(CH.C,C)});
  $('donorSearch').value='';$('donorHits').textContent='';renderCopy();say(C.baseName+' added: choose below what to copy from it.');}
function removeDonor(i){CH.donors.splice(i,1);const fix=(v)=>v==i?-1:v>i?v-1:v;CH.copy.anim=fix(CH.copy.anim);CH.copy.sounds=fix(CH.copy.sounds);
  for(const k of Object.keys(CH.copy.items)){const c=CH.copy.items[k];if(!c)continue;if(c[0]==i)CH.copy.items[k]=null;else if(c[0]>i)c[0]--;}renderCopy();}
function selectRow(label,opts,value,onchange){const l=document.createElement('label');l.textContent=label;const s=document.createElement('select');
  for(const [v,t,dis] of opts){const o=document.createElement('option');o.value=v;o.textContent=t;if(dis)o.disabled=true;s.append(o);}
  s.value=value;s.onchange=()=>{onchange(s.value);renderCopy();};const id='cp'+Math.random().toString(36).slice(2);s.id=id;l.htmlFor=id;$('copyRows').append(l,s);}
function renderCopy(){const box=$('copyRows'),chips=$('donorChips');box.textContent='';chips.textContent='';if(!CH)return;
  CH.donors.forEach((d,i)=>{const c=document.createElement('span');c.className='chip';c.textContent=d.name;const b=document.createElement('button');b.textContent='×';b.title='Remove '+d.name;b.onclick=()=>removeDonor(i);c.append(b);chips.append(c);});
  if(!CH.donors.length)return;const base=CH.C.baseName+' (base)';const D=CH.donors;
  // everything from one character
  const all=D.findIndex((d,i)=>CH.copy.anim==i&&CH.copy.sounds==i&&CH.items.every((it,k)=>{const c=CH.copy.items[it.path];return d.items[k]?c&&c[0]==i&&c[1]==d.items[k].path:!c;}));
  const none=CH.copy.anim<0&&CH.copy.sounds<0&&CH.items.every(it=>!CH.copy.items[it.path]);
  selectRow('Everything',[['mix',none?'—':'Mixed (set below)',true],['-1',base],...D.map((d,i)=>[String(i),d.name])],none?'-1':all>=0?String(all):'mix',(v)=>{const i=+v;
    CH.copy.anim=i;CH.copy.sounds=i;for(const [k,it] of CH.items.entries()){const u=i>=0&&D[i].items[k];CH.copy.items[it.path]=u?[i,u.path]:null;}});
  const sep=document.createElement('div');sep.className='sep';box.append(sep);
  selectRow('Animations',[['-1',base],...D.map((d,i)=>[String(i),d.name+(d.anim?'':' (none found)'),!d.anim])],String(CH.copy.anim),(v)=>CH.copy.anim=+v);
  selectRow('Voice and sounds',[['-1',base],...D.map((d,i)=>[String(i),d.name+' ('+d.sounds.pairs.length+' of '+d.sounds.total+' match)',!d.sounds.pairs.length])],String(CH.copy.sounds),(v)=>CH.copy.sounds=+v);
  if(!CH.items.length){const l=document.createElement('label');l.textContent='Items';const t=document.createElement('span');t.className='hint';t.textContent=CH.C.baseName+' carries no items.';box.append(l,t);}
  for(const it of CH.items){const c=CH.copy.items[it.path];const opts=[['',base+': '+itemLabel(it)]];
    D.forEach((d,i)=>d.items.forEach(u=>opts.push([i+'|'+u.path,d.name+': '+itemLabel(u)])));
    selectRow('Item: '+cleanName(it.name),opts,c?c[0]+'|'+c[1]:'',(v)=>{if(!v){CH.copy.items[it.path]=null;return;}const j=v.indexOf('|');CH.copy.items[it.path]=[+v.slice(0,j),v.slice(j+1)];});}
  issueKey='';renderIssues();}
// what export hands to PF.apply: one entry per donor with what it gives
function donorPlan(){if(!CH)return [];const out=[];
  CH.donors.forEach((d,i)=>{const items=CH.items.filter(it=>{const c=CH.copy.items[it.path];return c&&c[0]==i;}).map(it=>[it.path,CH.copy.items[it.path][1]]);
    const e={C:d.C,label:d.name,anim:CH.copy.anim==i,sounds:CH.copy.sounds==i,items};if(e.anim||e.sounds||items.length)out.push(e);});
  return out;}
function copyIssues(){if(!CH||!CH.donors.length)return [];const out=[];const D=CH.donors;
  const a=D[CH.copy.anim];if(a){if(!a.C.body!==!CH.C.body)out.push('Animations from '+a.name+' are made for a different body than '+CH.C.baseName+"'s, so they may not play right.");}
  const s=D[CH.copy.sounds];if(s&&s.sounds.pairs.length<s.sounds.total)out.push((s.sounds.total-s.sounds.pairs.length)+' of '+CH.C.baseName+"'s "+s.sounds.total+' sounds have no match in '+s.name+' and stay the base\'s.');
  for(const it of CH.items){const c=CH.copy.items[it.path];if(!c)continue;const d=D[c[0]],u=d.items.find(x=>x.path==c[1]);if(!u)continue;
    out.push('Item '+cleanName(it.name)+' takes the look of '+d.name+"'s "+cleanName(u.name)+' only: it is still used like '+CH.C.baseName+"'s item"+(u.renders.length!=it.renders.length?' and only '+Math.min(u.renders.length,it.renders.length)+' of its models are swapped':'')+'.');}
  return out;}
let donorTimer=0;$('donorSearch').oninput=()=>{clearTimeout(donorTimer);donorTimer=setTimeout(searchDonors,120);};
function searchDonors(){const box=$('donorHits');box.textContent='';const q=$('donorSearch').value.trim().toLowerCase();if(!q)return;
  if(!GAME.entries.size){box.textContent='';say('Connect the game folder to search its characters, or add a .prefab_baked file.',true);return;}
  const out=[];const seen=new Set();for(const [p,e] of GAME.entries){if(p!==e.path||seen.has(e))continue;if(!/\.prefab_baked$/i.test(p)||!/(^|\/)chars\/minifig\//.test(p))continue;
    const f=p.split('/').pop();if(!f.includes(q))continue;seen.add(e);out.push(e);if(out.length>=40)break;}
  for(const e of out){const b=document.createElement('button');b.className='hit';const n=e.path.split('/');const a=document.createElement('span');a.className='hit-name';a.textContent=n.pop();
    const d=document.createElement('span');d.className='hit-dir';d.textContent=n.join('/')+' · '+e.src;b.append(a,d);b.title='Copy from '+e.path;
    b.onclick=async()=>{try{say('Reading '+e.path.split('/').pop()+'…');addDonor(e.path.split('/').pop(),await readGame(e));}catch(err){say('Could not read it: '+err.message,true);}};box.append(b);}
  if(!out.length){const t=document.createElement('div');t.className='hint';t.style.padding='4px 6px';t.textContent='No character prefab matches.';box.append(t);}}
$('donorFileBtn').onclick=()=>$('donorFile').click();
$('donorFile').onchange=async(e)=>{const f=e.target.files[0];e.target.value='';if(f)addDonor(f.name,new Uint8Array(await f.arrayBuffer()));};

// what the character panel shows: base, abilities, preview, and the game files still to drop
function renderCharacter(){const sec=$('charSec'),ex=$('exportSec');sec.hidden=ex.hidden=!CH;if(!CH)return;const C=CH.C;
  $('charBase').textContent=C.baseName;$('charRow').textContent=C.roster?C.roster.name:'(none found)';
  $('charScene').textContent=CH.scene?CH.scene.name:'not loaded: drop '+C.baseName.toLowerCase()+'_cheapminifig.scene_baked so the roster shows the new look';
  const need=[];
  for(const p of parts)if(p.entity&&!p.model&&!p.newModel)need.push(baseFile(p.origModel));
  for(const o of allOwners())if(o.surface.refs&&o.surface.refs.color&&!o.surface.maps.color.img&&!o.surface.maps.color.cleared)need.push(baseFile(o.surface.refs.color.path));
  const u=[...new Set(need)];$('charNeed').textContent=u.length?'To see it as in the game, drop: '+u.join(', '):'All its models and prints are loaded.';
  renderSlots();renderCopy();issueKey='';renderIssues();
  if(!$('expId').value)$('internalName').textContent='';else $('internalName').textContent=internalName($('expId').value,C.baseName);}

function internalName(id,base){const L=base.length;const s=id.replace(/[^A-Za-z0-9]/g,'');const h=PF.crc32(PF.enc(id.toUpperCase())).toString(36).toUpperCase().padStart(7,'0');
  let n=s.length>L?s.slice(0,Math.max(1,L-3))+h:s+h+h+h;return n.slice(0,L);}
const slug=(s)=>s.toLowerCase().normalize('NFKD').replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'').slice(0,48);
$('expName').oninput=()=>{const v=$('expName').value;if(!$('expId').dataset.touched)$('expId').value=v.toUpperCase().replace(/[^A-Z0-9]+/g,'').slice(0,12);
  if(!$('expMod').dataset.touched)$('expMod').value=slug(v);renderCharacter();};
$('expId').oninput=()=>{$('expId').dataset.touched=1;renderCharacter();};$('expMod').oninput=()=>{$('expMod').dataset.touched=1;};
{const sel=$('expFaction');for(const [v,t] of FACTIONS){const o=document.createElement('option');o.value=v;o.textContent=t;sel.append(o);}}
let iconFile=null;$('expIconBtn').onclick=()=>$('iconInput').click();
$('iconInput').onchange=(e)=>{iconFile=e.target.files[0]||null;e.target.value='';$('expIconName').textContent=iconFile?iconFile.name:'none';};

// ---------- what the game won't get: listed beside the export button
function gameIssues(){if(!CH)return [];const out=[];const C=CH.C;const all=[...(C.body?Object.values(slots).map(x=>[x.label,x.surface]):[]),...parts.map(p=>[p.name,p.surface])];
  for(const p of parts)if(!p.entity&&p.visible)out.push(p.name+' is shown here only: it fills no slot of '+C.baseName+'.');
  for(const p of parts)if(p.entity&&!p.rigid&&p.origPlace&&['x','y','z'].some(a=>Math.abs(p.place[a]-p.origPlace[a])>1e-5))out.push('Moving '+p.name+' shows here only: skinned parts follow the skeleton.');
  for(const p of parts)if(p.newModel&&!p.newModel.orig){const st=p.newModel.name.replace(/\.[^.]+$/,'').toLowerCase();if(!(extraFiles.has(st+'.pc_shaders')&&extraFiles.has(st+'.nxg_textures')))out.push(p.newModel.name+' has no .pc_shaders/.nxg_textures loaded: fine for a game model, otherwise it shows untextured.');}
  for(const [n,s] of all){if(s.maps.emission.img)out.push('The emission map on '+n+' is shown here only (no emission slot in the game).');
    for(const [k,lab] of MAPS){const m=s.maps[k];if(m.img&&m.uv!==m.uvDef)out.push(lab+' on '+n+' uses '+(m.uv?'UV 2':'UV 1')+' here: the game uses the model\'s own UV.');
      if(m.img&&m.flip&&!m.src)out.push('Flip on '+n+' '+lab.toLowerCase()+' changes this view only (it is a game texture).');}}
  if(C.body){const ids=new Set(C.body.overrides.map(o=>o.id));for(const [id,label] of SLOT_INFO){const s=slots[id].surface;if((s.maps.color.src||s.color!=s.origColor)&&!(PF.BODY_IDS[id]||[]).some(i=>ids.has(i)))out.push(C.baseName+' has no material for '+label.toLowerCase()+', so its changes there won\'t show.');}}
  if(headMode=='hidden')out.push('The hidden head is only hidden here.');
  if(!C.neck&&headMode=='up')out.push(C.baseName+' has no neck lift, so the head stays at normal height.');
  if($('swapLR').checked)out.push('Swapping left and right textures changes this view only.');
  out.push(...copyIssues());
  return out;}
let issueKey='';
function renderIssues(){const l=gameIssues();const k=l.join('\n');if(k===issueKey)return;issueKey=k;$('issueBox').hidden=!l.length;const ul=$('issues');ul.textContent='';
  for(const t of l){const li=document.createElement('li');li.textContent=t;ul.append(li);}}
setInterval(renderIssues,700);
// slots of the base, and saving the character's own files
function renderSlots(){if(!CH)return;const l=parts.filter(p=>p.entity&&!p.ownBody).map(p=>p.entityName+' · '+p.slotKind.label);
  $('charSlots').textContent=l.length?l.join('; '):'none: this character has no attachment slots';}
$('charFiles').onclick=async()=>{if(!CH)return;const files=[[CH.C.selfPath,CH.bytes]];if(CH.scene)files.push([CH.scene.C.selfPath,CH.scene.bytes]);
  const paths=new Set();for(const p of parts)if(p.origModel)paths.add(p.origModel);
  for(const o of allOwners())if(o.surface.refs)for(const r of Object.values(o.surface.refs))paths.add(r.path);
  let missing=0;for(const ref of paths){const e=GAME.entries.size?findGame(ref):null;if(!e){missing++;continue;}try{files.push([e.path,await readGame(e)]);}catch(err){missing++;}}
  await saveFile(CH.C.baseName+'-files.zip',PF.zip(files));say('Saved '+files.length+' files'+(missing?' ('+missing+' not found in the game folder)':'')+'.');};
// ---------- export
const potX=(v)=>{let p=4;while(p<v&&p<2048)p*=2;return p;};
async function buildTexture(m,gamePath){const f=m.src,n=f.name.toLowerCase();const buf=new Uint8Array(await f.arrayBuffer());
  if(n.endsWith('.texture')&&!m.flip)return PF.retargetTexture(buf,gamePath);
  const v=new DataView(buf.buffer);
  if(n.endsWith('.dds')&&!m.flip&&buf.length>148&&String.fromCharCode(...buf.slice(84,88))=='DX10'&&v.getUint32(128,true)==98){
    const h=v.getUint32(12,true),w=v.getUint32(16,true),mips=Math.max(1,v.getUint32(28,true));
    if(PF.bc7size(w,h,mips)==buf.length-148)return PF.retargetTexture(PF.wrapBC7(b64u(TEX_TEMPLATE),w,h,mips,buf.slice(148)),gamePath);}
  const img=m.img;const W=potX(img.width),H=potX(img.height);const c=document.createElement('canvas');c.width=W;c.height=H;const g=c.getContext('2d');
  if(m.flip){g.translate(0,H);g.scale(1,-1);}g.drawImage(img,0,0,W,H);
  const px=g.getImageData(0,0,W,H).data;const e=PF.encodeBC7(px,W,H);
  return PF.retargetTexture(PF.wrapBC7(b64u(TEX_TEMPLATE),W,H,e.mips,e.data),gamePath);}
const b64u=(s)=>new Uint8Array(b64(s));
async function exportMod(){if(!CH)return;const C=CH.C;
  const name=$('expName').value.trim(),id=$('expId').value.trim().toUpperCase(),mod=$('expMod').value.trim(),desc=$('expDesc').value.trim();
  if(!name)return say('Give the character a name first.',true);
  if(!/^[A-Z0-9_]{2,24}$/.test(id))return say('The character ID needs 2–24 letters, digits or _.',true);
  if(!/^[a-z0-9][a-z0-9_.-]{0,63}$/.test(mod))return say('The mod folder name may use lower-case letters, digits, - and _.',true);
  const btnE=$('exportBtn');btnE.disabled=true;
  try{
  const pname=internalName(id,C.baseName);const files=[],resources=[],texList=[],alone=[];const cache=new Map();
  const tick=()=>new Promise(r=>setTimeout(r,0));
  async function texFor(m,folder,stem,ref){
    if(m.src){const key=m.src.name+'|'+m.src.size+'|'+m.flip;if(cache.has(key))return cache.get(key);
      say('Encoding '+m.src.name+'…');await tick();
      const gp=`Chars/Super_Character_Texture/${folder.toUpperCase()}/${stem.toUpperCase()}_DX11.texture`;
      const t=await buildTexture(m,gp);const rel=`CHARS/SUPER_CHARACTER_TEXTURE/${folder.toUpperCase()}/${stem.toUpperCase()}_DX11.texture`;
      files.push([rel,t.data]);resources.push(rel);const r={path:`chars/super_character_texture/${folder}/${stem}.texture`.toLowerCase(),id:t.id};texList.push(r);cache.set(key,r);return r;}
    if(m.game&&(!ref||m.game.path!=ref.path))return m.game;
    if(m.cleared)return null;
    return undefined;}   // unchanged
  const P=pname.toLowerCase();const plan={internalName:pname,rosterRow:'OMP_'+id,body:{},parts:[],donors:donorPlan()};
  if(C.neck&&headMode!='hidden'&&headMode!=CH.origHead)plan.neckLift=headMode=='up'?HEAD_UP:0;
  for(const [sid] of SLOT_INFO){const s=slots[sid].surface,ch={};
    if(s.color.toLowerCase()!=(s.origColor||'').toLowerCase())ch.tint=linOf(s.color);
    for(const k of ['color','normal','brdf']){const ref=s.refs&&s.refs[k];const field=k=='color'?(ref?ref.field:'diffuse_1'):k=='normal'?'normal_0':'specular_0';
      const stem=[TEX_FOLDER[sid],P,TEX_PART[sid],KIND_TAG[k]].filter(Boolean).join('_');
      const t=await texFor(s.maps[k],TEX_FOLDER[sid],stem,ref);if(t!==undefined)(ch.tex=ch.tex||{})[field]=t;}
    if(ch.tint||ch.tex)plan.body[sid]=ch;}
  for(const p of parts.filter(q=>q.entity)){const pc={entity:p.entity};const s=p.surface;
    if(!p.visible){pc.enabled=false;plan.parts.push(pc);continue;}
    const folder=(p.newModel?p.newModel.path:p.origModel).split('/').slice(-2,-1)[0]||'additionalmodel';
    const same=parts.filter(q=>q.entity&&((q.newModel?q.newModel.path:q.origModel).split('/').slice(-2,-1)[0]||'additionalmodel')==folder);
    const tag=same.length>1?String(same.indexOf(p)+1):'';
    if(p.newModel){pc.model={path:p.newModel.path};pc.special=p.special||'[All Specials]';pc.tex={};
      const cf=(s.refs&&s.refs.color)?s.refs.color.field:'diffuse_1';
      for(const k of ['color','normal','brdf']){const m=s.maps[k];let t=await texFor(m,folder,[folder,P,tag,KIND_TAG[k]].filter(Boolean).join('_'),null);if(t===undefined)t=null;
        if(k=='color'){pc.tex.diffuse_0=cf=='diffuse_0'?t:null;pc.tex.diffuse_1=cf=='diffuse_1'?t:null;}else pc.tex[k=='normal'?'normal_0':'specular_0']=t;}
      if(!p.newModel.orig){const sp=p.newModel.selfPath;files.push([sp,p.newModel.bytes]);resources.push(sp);
        const stem=p.newModel.name.replace(/\.[^.]+$/,'').toLowerCase();
        let found=0;for(const ext of ['pc_shaders','nxg_textures']){const x=extraFiles.get(stem+'.'+ext);if(x){found++;const rel=sp.replace(/\.[^./]+$/,'.'+ext.toUpperCase());files.push([rel,x.data]);resources.push(rel);}}
        if(found<2)alone.push(p.newModel.name);}}
    else if(p.mats&&p.mats[0].id!=null){pc.mats=[];   // one entry per material override
      for(const m of p.mats){const ms=m.surface,e={id:m.id};
        for(const k of ['color','normal','brdf']){const ref=ms.refs&&ms.refs[k];const field=k=='color'?(ref?ref.field:'diffuse_1'):k=='normal'?'normal_0':'specular_0';
          const t=await texFor(ms.maps[k],folder,[folder,P,tag,'m'+m.id,KIND_TAG[k]].filter(Boolean).join('_'),ref);if(t!==undefined)(e.tex=e.tex||{})[field]=t;}
        if(ms.color.toLowerCase()!=(ms.origColor||'').toLowerCase())e.tint=linOf(ms.color);
        if(e.tex||e.tint)pc.mats.push(e);}}
    else{for(const k of ['color','normal','brdf']){const ref=s.refs&&s.refs[k];const field=k=='color'?(ref?ref.field:'diffuse_1'):k=='normal'?'normal_0':'specular_0';
        const t=await texFor(s.maps[k],folder,[folder,P,tag,KIND_TAG[k]].filter(Boolean).join('_'),ref);if(t!==undefined)(pc.tex=pc.tex||{})[field]=t;}}
    if(!pc.mats&&(s.color.toLowerCase()!=(p.origColor||'').toLowerCase()||p.newModel))pc.tint=linOf(s.color);
    if(p.rigid&&p.origPlace&&['x','y','z'].some(a=>Math.abs(p.place[a]-p.origPlace[a])>1e-5))pc.place=[p.place.x,p.place.y,p.place.z];
    plan.parts.push(pc);}
  say('Writing the prefab…');await tick();
  const pre=PF.apply(CH.bytes,plan,{textures:texList});const dir=pre.selfPath.slice(0,pre.selfPath.lastIndexOf('/')+1).toUpperCase();
  const prefabRel=dir+pname.toUpperCase()+'.PREFAB_BAKED';files.unshift([prefabRel,pre.data]);resources.unshift(prefabRel);
  let sceneRel=null;
  if(CH.scene){const sc=PF.apply(CH.scene.bytes,plan,{textures:texList});sceneRel=dir+pname.toUpperCase()+'_CHEAPMINIFIG.SCENE_BAKED';files.push([sceneRel,sc.data]);}
  let icon=null;if(iconFile){const bmp=await createImageBitmap(iconFile);const c=document.createElement('canvas');c.width=bmp.width;c.height=bmp.height;c.getContext('2d').drawImage(bmp,0,0);
    const blob=await new Promise(r=>c.toBlob(r,'image/png'));files.push(['icon.png',new Uint8Array(await blob.arrayBuffer())]);icon='icon.png';}
  const payload={id,tableName:'Characters',templateMatch:C.roster?C.roster.name:C.baseName,prefabResourcePath:pre.selfPath.toLowerCase(),name,description:desc||name};
  if(icon)payload.icon=icon;
  if(sceneRel){payload.previewScene=sceneRel;const pb=$('expPreview').value.trim();if(pb)payload.previewBase=pb;}
  const fac=$('expFaction').value;if(fac!=='')payload.columnOverrides={CharacterClass:fac};
  const manifest={name,description:desc||name,dependencies:[{id:'v1'}],
    actions:[...resources.map(r=>({typeId:'v1/register-custom-resource-action',payload:{path:r,srcPath:r}})),{typeId:'v1/register-custom-vehicle-action',payload}]};
  files.unshift(['mod.json',PF.enc(JSON.stringify(manifest,null,1))]);
  const zipped=PF.zip(files.map(([n,d])=>[mod+'/'+n,d]));
  const kept=parts.filter(p=>!p.entity).length;
  await saveFile(mod+'.zip',zipped);
  say('Exported '+mod+'.zip: '+(files.length-1)+' files.'+(kept?' View-only parts were left out.':'')+
    (alone.length?' '+alone.join(', ')+' went without its .pc_shaders and .nxg_textures: fine for a game model, otherwise drop them and export again.':''));}
  catch(e){say('Export failed: '+e.message,true);console.error(e);}
  finally{btnE.disabled=false;}}
$('exportBtn').onclick=exportMod;
async function saveFile(filename,data){
  const dl=window.claude&&window.claude.use?await window.claude.use('downloads').catch(()=>null):null;
  if(dl){try{await dl.save({filename,data:new Blob([data])});return;}catch(e){if(e&&e.code=='declined')throw new Error('saving was cancelled');if(e&&e.code&&!['unavailable','not_granted','capability_disabled','capability_removed'].includes(e.code))throw new Error(e.message||e.code);}}
  const a=document.createElement('a');a.href=URL.createObjectURL(new Blob([data],{type:'application/zip'}));a.download=filename;document.body.append(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(a.href),30000);}

// ---------- game folder: read textures, models and characters straight from the install (archives and loose files)
const GAME={name:'',dats:[],entries:new Map(),byName:new Map(),loose:0,handle:null,count:0};
const WANT=/\.(texture|dds|png|gsc|ghg|prefab_baked|scene_baked|pc_shaders|nxg_textures)$/i;
const SKIP_DIRS=/^(omp|omp-.*|\.git|_?aamods|audio|cut|levels|ui)$/i;   // runtime folders and areas with no character files
const normPath=(p)=>p.replace(/\\/g,'/').replace(/^\/+/,'').toLowerCase();
function addEntry(path,e){path=normPath(path);e.path=path;if(!GAME.entries.has(path))GAME.count++;GAME.entries.set(path,e);
  const alias=path.replace(/^additionalcontent\/[^/]+\//,'');if(alias!==path&&!GAME.entries.has(alias))GAME.entries.set(alias,e);
  // installed mods replace the game's files at the same path, as they do in the game
  const modAlias=path.replace(/^mods\/[^/]+\//,'');if(modAlias!==path)GAME.entries.set(modAlias,e);
  const k=path.split('/').pop();const l=GAME.byName.get(k);if(l)l.push(e);else GAME.byName.set(k,[e]);}
// a reference from a prefab (no _dx11, lower case) to the file that holds it
function findGame(ref){ref=normPath(ref);const cands=[ref,ref.replace(/(\.[a-z_]+)$/,'_dx11$1'),ref.replace(/_dx11(\.[a-z_]+)$/,'$1')];
  for(const c of cands){const e=GAME.entries.get(c);if(e)return e;}
  for(const c of cands){const l=GAME.byName.get(c.split('/').pop());if(l&&l.length)return l[l.length-1];}
  return null;}
async function readGame(e){if(e.dat)return DAT.read(e.dat.file,e.f);const f=e.handle?await e.handle.getFile():e.file;return new Uint8Array(await f.arrayBuffer());}
async function gameFile(e){const name=e.path.split('/').pop().toUpperCase();return new File([await readGame(e)],name);}

// remembered folder (only with the folder picker; the fallback file list cannot be kept)
const idb=()=>new Promise((res,rej)=>{const r=indexedDB.open('minifig-viewer',1);r.onupgradeneeded=()=>r.result.createObjectStore('kv');r.onsuccess=()=>res(r.result);r.onerror=()=>rej(r.error);});
async function kvGet(k){try{const db=await idb();return await new Promise((res)=>{const q=db.transaction('kv').objectStore('kv').get(k);q.onsuccess=()=>res(q.result);q.onerror=()=>res(null);});}catch(e){return null;}}
async function kvSet(k,v){try{const db=await idb();await new Promise((res)=>{const t=db.transaction('kv','readwrite');t.objectStore('kv').put(v,k);t.oncomplete=res;t.onerror=res;});}catch(e){}}
async function kvDel(k){try{const db=await idb();await new Promise((res)=>{const t=db.transaction('kv','readwrite');t.objectStore('kv').delete(k);t.oncomplete=res;t.onerror=res;});}catch(e){}}

function resetGame(){GAME.count=0;GAME.name='';GAME.dats=[];GAME.entries=new Map();GAME.byName=new Map();GAME.loose=0;GAME.handle=null;}
async function addDats(files){files.sort((a,b)=>a.name.localeCompare(b.name));   // later archives win, like patches
  for(const f of files){say('Reading '+f.name+'…');try{const d=await DAT.open(f);GAME.dats.push(d);for(const x of d.files)addEntry(x.path,{dat:d,f:x,src:d.name});}
    catch(e){console.warn(f.name,e);}}}
async function scanHandle(dir){resetGame();GAME.name=dir.name;GAME.handle=dir;const dats=[],loose=[];
  async function walk(h,rel,depth){for await(const [name,x] of h.entries()){
    if(x.kind=='file'){if(depth==0&&/\.dat$/i.test(name))dats.push(await x.getFile());else if(WANT.test(name))loose.push([rel+name,x]);}
    else if(depth<8&&!(depth==0&&SKIP_DIRS.test(name)))await walk(x,rel+name+'/',depth+1);}}
  say('Looking through '+dir.name+'…');await walk(dir,'',0);
  await addDats(dats);for(const [p,x] of loose)addEntry(p,{handle:x,src:'folder'});GAME.loose=loose.length;finishScan();}
async function scanList(files){resetGame();const dats=[],loose=[];
  for(const f of files){const rel=(f.webkitRelativePath||f.name).split('/');GAME.name=GAME.name||rel[0];const p=rel.slice(1).join('/')||f.name;
    if(rel.length<=2&&/\.dat$/i.test(f.name))dats.push(f);else if(WANT.test(f.name)&&!SKIP_DIRS.test(rel[1]||''))loose.push([p,f]);}
  await addDats(dats);for(const [p,f] of loose)addEntry(p,{file:f,src:'folder'});GAME.loose=loose.length;finishScan();}
function finishScan(){renderGame();
  if(!GAME.entries.size){say('No game archives or files found in '+GAME.name+'. Choose the game\'s install folder.',true);return;}
  say('Game files ready: '+GAME.dats.length+' archives, '+GAME.count.toLocaleString()+' files.');
  if(CH)autoLoadCharacter();else loadStdMaps();}
async function connectGame(){
  if(window.showDirectoryPicker){try{const h=await window.showDirectoryPicker({id:'lswtss',mode:'read'});await kvSet('gameDir',h);await scanHandle(h);return;}
    catch(e){if(e&&e.name=='AbortError')return;console.warn('folder picker unavailable here',e);}}
  $('dirInput').click();}
$('dirInput').onchange=async(e)=>{const files=[...e.target.files];e.target.value='';if(files.length)await scanList(files);};
$('gameConnect').onclick=connectGame;
$('gameReconnect').onclick=async()=>{const h=GAME.saved;if(!h)return;try{if(await h.requestPermission({mode:'read'})=='granted')await scanHandle(h);}catch(e){say('Could not open '+h.name+' again: choose the folder.',true);connectGame();}};
$('gameForget').onclick=async()=>{await kvDel('gameDir');GAME.saved=null;resetGame();renderGame();say('Game folder disconnected.');};
(async()=>{const h=await kvGet('gameDir');if(!h||!h.queryPermission)return;GAME.saved=h;
  try{if(await h.queryPermission({mode:'read'})=='granted'){await scanHandle(h);return;}}catch(e){}
  renderGame();})();

// what the panel shows
function renderGame(){const on=GAME.entries.size>0;$('gameOff').hidden=on;$('gameOn').hidden=!on;
  $('gameReconnect').hidden=on||!GAME.saved;if(GAME.saved)$('gameReconnect').textContent='Reopen '+GAME.saved.name;
  if(on)$('gameInfo').textContent=GAME.name+' · '+GAME.dats.length+' archives · '+GAME.count.toLocaleString()+' files'+(GAME.loose?' ('+GAME.loose.toLocaleString()+' loose)':'')+(GAME.handle?'':'. This browser could not keep the folder, so choose it again next time.');
  searchGame();}
let searchTimer=0;$('gameSearch').oninput=()=>{clearTimeout(searchTimer);searchTimer=setTimeout(searchGame,120);};
$('gameKind').onchange=searchGame;
function searchGame(){const box=$('gameResults');box.textContent='';if(!GAME.entries.size)return;
  const terms=$('gameSearch').value.toLowerCase().split(/\s+/).filter(Boolean);const kind=$('gameKind').value;
  if(!terms.length){$('gameCount').textContent='Type part of a name: a character (cloneTrooper), a part (hat_), a texture…';return;}
  const out=[];const seen=new Set();
  for(const [p,e] of GAME.entries){if(p!==e.path||seen.has(e))continue;if(kind&&!new RegExp(kind,'i').test(p))continue;
    if(terms.every(t=>p.includes(t))){seen.add(e);out.push(e);if(out.length>=150)break;}}
  $('gameCount').textContent=out.length>=150?'First 150 matches, type more to narrow.':out.length+' match'+(out.length==1?'':'es')+'.';
  for(const e of out){const b=document.createElement('button');b.className='hit';const n=e.path.split('/');
    const a=document.createElement('span');a.className='hit-name';a.textContent=n.pop();const d=document.createElement('span');d.className='hit-dir';d.textContent=n.join('/')+' · '+e.src;
    b.append(a,d);b.title='Load '+e.path;b.onclick=()=>loadFromGame([e]);box.append(b);}}
async function loadFromGame(list){try{say('Opening '+list.map(e=>e.path.split('/').pop()).join(', ')+'…');const files=[];
    for(const e of list)files.push(await gameFile(e));await handleFiles(files,null);}
  catch(e){say('Could not read it: '+e.message,true);console.error(e);}}

// a character from anywhere gets its models, prints, normal and BRDF maps and preview scene from the game
async function autoLoadCharacter(){if(!CH||!GAME.entries.size)return;const me=CH;const tried=new Set();let total=0;
  // models can bring more materials with their own textures, so look again after each batch
  for(let pass=0;pass<4;pass++){const want=new Map();const add=(e)=>{if(e&&!tried.has(e.path)){want.set(e.path,e);}};
    for(const p of parts)if(p.entity&&!p.model&&!p.newModel)add(findGame(p.origModel));
    for(const o of allOwners())if(o.surface.refs)for(const [k,r] of Object.entries(o.surface.refs)){if(o.surface.maps[k].img)continue;add(findGame(r.path));}
    if(CH.bodyModelPath&&!CH.bodyModel)add(findGame(CH.bodyModelPath));
    if(!CH.scene)add(findGame(CH.C.selfPath.replace(/\.prefab_baked$/i,'_cheapminifig.scene_baked')));
    if(!want.size)break;for(const k of want.keys())tried.add(k);total+=want.size;
    say('Loading '+total+' game files for '+CH.C.baseName+'…');
    const files=[];for(const e of want.values()){if(CH!==me)return;try{files.push(await gameFile(e));}catch(err){console.warn(e.path,err);}}
    if(CH!==me)return;   // another character was opened meanwhile
    await handleFiles(files,null);if(CH!==me)return;}
  if(total)say(CH.C.baseName+' loaded from the game files.');}
// the standard minifig's own normal and BRDF maps for the plain body and the example
const STD_MAPS={head:['head_front','head_front'],body_front:['body_front','body_front'],body_back:['body_front','body_front'],arm_l:['left_arm','left_arm'],arm_r:['right_arm','right_arm'],
  hand_l:['left_hand','right_hand'],hand_r:['left_hand','right_hand'],hips:['hips','hips'],leg_l:['left_leg','left_leg'],leg_r:['right_leg','right_leg']};
async function loadStdMaps(){if(!GAME.entries.size||CH)return;const cache=new Map();let n=0;
  for(const [id,[nrm,brdf]] of Object.entries(STD_MAPS)){const s=slots[id].surface;
    for(const [kind,stem] of [['normal',nrm+'_dx11_nrm'],['brdf',brdf+'_dx11_brdf']]){if(s.maps[kind].img)continue;
      const ref='chars/super_character_texture/minifig/'+stem+'.texture';const e=findGame(ref);if(!e)continue;
      try{if(!cache.has(e.path))cache.set(e.path,await loadImage(await gameFile(e)));setMap(s,kind,cache.get(e.path),e.path.split('/').pop().toUpperCase(),{game:{path:ref}});n++;}catch(err){console.warn(err);}}}
  if(n){renderBodyRows();say('Minifig normal and BRDF maps loaded from the game.');}}

// ---------- example: the 212th airborne clone trooper
async function loadExample(){
  for(const id of Object.keys(slots))if(SAMPLE_TEX[id])setMap(slots[id].surface,'color',await imgFromURL(SAMPLE_TEX[id]),id.toUpperCase()+'_CT212 (example)');
  const helm=addPart('HAT_CT212KAMA_DX11.GSC',parseModel(b64(SAMPLE_MODELS.helmet)));
  setMap(helm.surface,'color',await imgFromURL(SAMPLE_TEX.helmet),'HAT_CT212KAMA_DIFF (example)');
  setMap(helm.surface,'normal',await imgFromURL(SAMPLE_TEX.helmet_nrm),'HAT_AIRBORNE_NRM (example)');
  setMap(helm.surface,'brdf',await imgFromURL(SAMPLE_TEX.helmet_brdf),'HAT_AIRBORNE_BRDF (example)');
  const kama=addPart('SKINNEDSKIRT_FABRIC_KAMACT212_DX11.GHG',parseModel(b64(SAMPLE_MODELS.kama)));
  setMap(kama.surface,'color',await imgFromURL(SAMPLE_TEX.kama),'KAMA_AIRBORN (example)');
  const sat=addPart('ADDITIONALMODEL_SATCHEL_DX11.GHG',parseModel(b64(SAMPLE_MODELS.satchel)),{color:'#582a12'});paintColor(sat.surface);
  setMap(sat.surface,'normal',await imgFromURL(SAMPLE_TEX.satchel_nrm),'ADDITIONALMODEL_SATCHEL_NRM (example)');
  setMap(sat.surface,'brdf',await imgFromURL(SAMPLE_TEX.satchel_brdf),'ADDITIONALMODEL_SATCHEL_BRDF (example)');
  expanded.clear();renderBodyRows();renderParts();}
function resetAll(){parts.length=0;target=null;expanded.clear();CH=null;bodyMeshes=STD_BODY;extraFiles.clear();$('bodySec').hidden=false;$('ownBodyNote').hidden=true;
  for(const [id] of SLOT_INFO){const s=slots[id].surface;s.refs=null;for(const [k] of MAPS){Object.assign(s.maps[k],{img:null,name:'',dirty:true,src:null,game:null,cleared:false});}s.color=slots[id].defColor;paintColor(s);}
  renderCharacter();}
$('clearAll').onclick=()=>{resetAll();setHead('normal');renderBodyRows();renderParts();say('Plain minifig. Load textures, parts or a character prefab.');loadStdMaps();};
renderBodyRows();renderParts();requestAnimationFrame(frame);
loadExample().catch(e=>say('The example could not load: '+e.message,true));
</script>
