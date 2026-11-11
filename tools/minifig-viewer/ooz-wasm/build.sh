#!/bin/sh
# Builds ooz.wasm (Oodle decompressor) with plain clang + wasm-ld, no Emscripten.
# Sources: lib/kraken.cpp, lib/bitknit.cpp, lib/lzna.cpp from https://github.com/SnosMe/ooz-wasm (ooz by powzix, GPL-3.0).
# shim/ holds freestanding stand-ins for the C headers and scalar versions of the SSE2 intrinsics ooz uses; rt.c is malloc/free and mem*.
# Then: python3 -c "import base64;open('../ooz_wasm.js','w').write(\"const OOZ_WASM='\"+base64.b64encode(open('ooz.wasm','rb').read()).decode()+\"';\\n\")"
set -e
F="--target=wasm32 -O2 -ffreestanding -nostdlib -fno-exceptions -fno-rtti -Ishim -w"
for s in kraken bitknit lzna; do clang++ $F -c lib/$s.cpp -o $s.o; done
clang --target=wasm32 -O2 -ffreestanding -nostdlib -fno-builtin -c rt.c -o rt.o
wasm-ld --no-entry --export=Kraken_Decompress --export=malloc --export=free -z stack-size=1048576 --initial-memory=4194304 kraken.o bitknit.o lzna.o rt.o -o ooz.wasm
