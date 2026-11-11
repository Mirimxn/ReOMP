// tiny runtime for the wasm build: bump allocator with a free list per size, mem* functions
#include <stddef.h>
#include <stdint.h>
extern unsigned char __heap_base;
static uintptr_t top=0;
typedef struct blk{size_t size;struct blk*next;}blk;
static blk*freel=0;
void *malloc(size_t n){n=(n+15)&~(size_t)15;
  for(blk**pp=&freel;*pp;pp=&(*pp)->next){if((*pp)->size>=n&&(*pp)->size<=n*2){blk*b=*pp;*pp=b->next;return (char*)b+16;}}
  if(!top)top=((uintptr_t)&__heap_base+15)&~(uintptr_t)15;
  uintptr_t p=top;top+=16+n;size_t have=__builtin_wasm_memory_size(0)*65536;
  if(top>have){size_t need=(top-have+65535)/65536;if(__builtin_wasm_memory_grow(0,need)==(size_t)-1)return 0;}
  ((blk*)p)->size=n;return (void*)(p+16);}
void free(void*q){if(!q)return;blk*b=(blk*)((char*)q-16);b->next=freel;freel=b;}
void *calloc(size_t a,size_t b){size_t n=a*b;unsigned char*p=malloc(n);if(p)for(size_t i=0;i<n;i++)p[i]=0;return p;}
void *memcpy(void*d,const void*s,size_t n){unsigned char*D=d;const unsigned char*S=s;while(n--)*D++=*S++;return d;}
void *memmove(void*d,const void*s,size_t n){unsigned char*D=d;const unsigned char*S=s;if(D<S)while(n--)*D++=*S++;else{D+=n;S+=n;while(n--)*--D=*--S;}return d;}
void *memset(void*d,int c,size_t n){unsigned char*D=d;while(n--)*D++=(unsigned char)c;return d;}
int memcmp(const void*a,const void*b,size_t n){const unsigned char*A=a,*B=b;for(;n;n--,A++,B++)if(*A!=*B)return *A-*B;return 0;}
