// Bridge used when the page runs inside the ReOMP Mod Manager (its Characters tab).
// The manager supplies the game files from its own index of the archives, the minifig
// body comes from the player's game instead of a built-in copy, and finished mods go
// through the manager's import (or a save dialog) instead of a browser download.
window.MM=(()=>{
  const tauri=(window.parent&&window.parent.__TAURI__)||window.__TAURI__;
  if(!tauri)return null;
  const invoke=(cmd,args,opts)=>tauri.core.invoke(cmd,args,opts);
  const LOOSE=4294967295;
  const BODY_PATH='chars/super_character/super_minifig/super_minifig_dx11.ghg';
  let dest='library';
  const failure=(o)=>new Error([o.title,...(o.details||[])].filter(Boolean).join(' '));
  return {
    async start(){
      this.listen();
      // the folder controls are the browser's; the manager's game folder is used instead
      for(const id of ['gameConnect','gameReconnect','gameForget'])if($(id))$(id).hidden=true;
      const h1=document.querySelector('h1');if(h1)h1.textContent='Character maker';
      const sub=document.querySelector('p.sub');
      if(sub)sub.textContent='Start from a game character: search for it below (for example clonetrooper) and open its .prefab_baked. Change its parts, colours and textures (Ctrl+Z undoes, Ctrl+Y redoes), then add it to your mods. Textures you drop or load are yours and go into the mod; textures opened from the search are used from the game.';
      const btn=$('exportBtn');
      if(btn){const sel=document.createElement('select');sel.id='mmDest';
        sel.innerHTML='<option value="library">Add to my mods</option><option value="file">Save the ZIP…</option>';
        sel.onchange=()=>{dest=sel.value;btn.textContent=dest=='library'?'Add to my mods':'Save mod ZIP…';};
        btn.textContent='Add to my mods';btn.before(sel);sel.style.marginRight='8px';}
      try{await this.connect();}
      catch(e){say('The game files could not be read: '+(e&&e.message||e)+'. Choose the game folder in the Mods tab, then open this tab again.',true);console.error(e);}
    },
    async connect(){
      say('Reading the game file list…');
      const text=new TextDecoder().decode(await invoke('assets_paths'));
      const lines=text.split('\n');const archives=lines[0]?lines[0].split('\t'):[];
      resetGame();GAME.name='Game folder';GAME.dats=archives;
      for(let i=1;i<lines.length;i++){const line=lines[i];if(!line)continue;const tab=line.indexOf('\t');
        const path=line.slice(tab+1);if(!WANT.test(path))continue;
        const src=line.slice(0,tab);const loose=src.startsWith('L:');const owner=loose?src.slice(2):null;
        addEntry(path,{mm:{path,source:loose?LOOSE:Number(src),owner},src:loose?'mod '+owner:archives[Number(src)]||'archive'});
        if(loose)GAME.loose++;}
      const body=findGame(BODY_PATH);
      if(!body)throw new Error(BODY_PATH+' is not in the game files');
      const bytes=await readGame(body);
      addStdBody(minifigBody(bytes.buffer.slice(bytes.byteOffset,bytes.byteOffset+bytes.byteLength)));
      finishScan();
    },
    async read(m){return new Uint8Array(await invoke('assets_read',{path:m.path,source:m.source}));},
    // The Mod Manager's window receives dropped files; on this tab it passes them here with
    // the cursor position, and the page reads the ones just dropped.
    async dropped(msg){
      $('drop').hidden=true;
      say('Reading '+msg.paths.length+' dropped file'+(msg.paths.length>1?'s':'')+'…');
      const files=[];
      for(const path of msg.paths){const name=path.split(/[\\/]/).pop();
        try{files.push(new File([await invoke('read_dropped',{path})],name));}
        catch(e){say('Could not read '+name+': '+(e&&e.message||e),true);return;}}
      await dropFiles(files,msg.x,msg.y);
    },
    async save(filename,data){
      const o=await invoke('package_bytes',data,{headers:{'file-name':filename,'to-library':dest=='library'?'1':'0'}});
      if(!o.ok)throw failure(o);
      this.cancelled=dest=='file'&&!(o.details||[]).length;   // the save dialog was closed
    },
    listen(){window.addEventListener('message',(e)=>{if(e.source!==window.parent||!e.data)return;
      if(e.data.type=='reomp-drag')$('drop').hidden=!e.data.on;
      if(e.data.type=='reomp-drop')this.dropped(e.data);
      if(e.data.type=='reomp-undo')undo();
      if(e.data.type=='reomp-redo')redo();});},
    // BC7 with every mip level, compressed by the manager: {mips, data}
    async encodeBC7(px,w,h){
      const out=new Uint8Array(await invoke('encode_bc7',new Uint8Array(px.buffer,px.byteOffset,px.byteLength),{headers:{width:String(w),height:String(h)}}));
      return {mips:new DataView(out.buffer).getUint32(0,true),data:out.subarray(4)};
    },
    // the installed version's mod.json, and a new version of the mod made from it with the changed files
    async manifest(id){return invoke('mod_manifest',{id});},
    async update(id,zip){
      const o=await invoke('package_update',zip,{headers:{'mod-id':id}});
      if(!o.ok)throw failure(o);
      return (o.details||[]).join(' ');
    },
    done(mod){return dest=='library'?'Added '+mod+' to your mods':this.cancelled?'Not saved ('+mod+'.zip)':'Saved '+mod+'.zip';},
  };
})();
