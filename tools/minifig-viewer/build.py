# assembles minifig-part-viewer.html from its pieces
h=open('head.html').read(); d=open('data_block.html').read(); s=open('app_script.js').read()
def strip(t):
    import re; return re.sub(r"if\(typeof module!='undefined'\)module\.exports=[^;]*;","",t)
d=d.replace('/*__DECODERS__*/',strip(open('tables.js').read()+open('decode.js').read())).replace('/*__MODEL__*/',strip(open('model.js').read()+open('prefab.js').read()+open('tex_template.js').read()+open('gamedat.js').read()+open('ooz_wasm.js').read())).replace('/*__ASSETS__*/',open('assets.js').read())
open('minifig-part-viewer.html','w').write(h+d+s)
