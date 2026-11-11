#pragma once
// scalar stand-ins for the SSE2 intrinsics ooz uses (wasm32 has no SSE)
#include <stdint.h>
typedef union { uint8_t b[16]; int8_t sb[16]; uint16_t w[8]; int16_t sw[8]; uint32_t d[4]; uint64_t q[2]; } __m128i;
typedef __m128i __m128;
typedef struct { uint8_t b[8]; } __m64;
#define _MM_HINT_T0 0
#define _MM_HINT_T1 0
#define _MM_HINT_T2 0
#define _MM_HINT_NTA 0
#define SI static inline
SI void _mm_prefetch(const void*, int) {}
SI __m128i _mm_loadu_si128(const __m128i *p){__m128i r; __builtin_memcpy(&r,p,16); return r;}
SI void _mm_storeu_si128(__m128i *p,__m128i a){__builtin_memcpy(p,&a,16);}
SI __m128i _mm_loadl_epi64(const __m128i *p){__m128i r; r.q[1]=0; __builtin_memcpy(&r.q[0],p,8); return r;}
SI void _mm_storel_epi64(__m128i *p,__m128i a){__builtin_memcpy(p,&a.q[0],8);}
SI void _mm_storeh_pi(__m64 *p,__m128 a){__builtin_memcpy(p,&a.q[1],8);}
SI __m128 _mm_castsi128_ps(__m128i a){return a;}
SI __m128i _mm_add_epi8(__m128i a,__m128i b){for(int i=0;i<16;i++)a.b[i]+=b.b[i];return a;}
SI __m128i _mm_sub_epi8(__m128i a,__m128i b){for(int i=0;i<16;i++)a.b[i]-=b.b[i];return a;}
SI __m128i _mm_add_epi16(__m128i a,__m128i b){for(int i=0;i<8;i++)a.w[i]+=b.w[i];return a;}
SI __m128i _mm_sub_epi16(__m128i a,__m128i b){for(int i=0;i<8;i++)a.w[i]-=b.w[i];return a;}
SI __m128i _mm_and_si128(__m128i a,__m128i b){a.q[0]&=b.q[0];a.q[1]&=b.q[1];return a;}
SI __m128i _mm_xor_si128(__m128i a,__m128i b){a.q[0]^=b.q[0];a.q[1]^=b.q[1];return a;}
SI __m128i _mm_cmpeq_epi8(__m128i a,__m128i b){for(int i=0;i<16;i++)a.b[i]=a.b[i]==b.b[i]?0xff:0;return a;}
SI __m128i _mm_cmpgt_epi16(__m128i a,__m128i b){for(int i=0;i<8;i++)a.w[i]=a.sw[i]>b.sw[i]?0xffff:0;return a;}
SI __m128i _mm_max_epu8(__m128i a,__m128i b){for(int i=0;i<16;i++)if(b.b[i]>a.b[i])a.b[i]=b.b[i];return a;}
SI int _mm_movemask_epi8(__m128i a){int m=0;for(int i=0;i<16;i++)m|=(a.b[i]>>7)<<i;return m;}
SI __m128i _mm_set1_epi8(char v){__m128i r;for(int i=0;i<16;i++)r.b[i]=(uint8_t)v;return r;}
SI __m128i _mm_set1_epi16(short v){__m128i r;for(int i=0;i<8;i++)r.w[i]=(uint16_t)v;return r;}
SI __m128i _mm_set1_epi32(int v){__m128i r;for(int i=0;i<4;i++)r.d[i]=(uint32_t)v;return r;}
SI __m128i _mm_set_epi16(short e7,short e6,short e5,short e4,short e3,short e2,short e1,short e0){__m128i r;r.w[0]=e0;r.w[1]=e1;r.w[2]=e2;r.w[3]=e3;r.w[4]=e4;r.w[5]=e5;r.w[6]=e6;r.w[7]=e7;return r;}
SI __m128i _mm_cvtsi32_si128(int v){__m128i r;r.q[0]=0;r.q[1]=0;r.d[0]=(uint32_t)v;return r;}
SI __m128i _mm_shuffle_epi32(__m128i a,int imm){__m128i r;for(int i=0;i<4;i++)r.d[i]=a.d[(imm>>(2*i))&3];return r;}
SI __m128i _mm_slli_si128(__m128i a,int n){__m128i r;for(int i=0;i<16;i++)r.b[i]=i>=n?a.b[i-n]:0;return r;}
SI __m128i _mm_srli_epi16(__m128i a,int n){for(int i=0;i<8;i++)a.w[i]=n>15?0:(uint16_t)(a.w[i]>>n);return a;}
SI __m128i _mm_srai_epi16(__m128i a,int n){if(n>15)n=15;for(int i=0;i<8;i++)a.sw[i]=(int16_t)(a.sw[i]>>n);return a;}
SI __m128i _mm_unpacklo_epi8(__m128i a,__m128i b){__m128i r;for(int i=0;i<8;i++){r.b[2*i]=a.b[i];r.b[2*i+1]=b.b[i];}return r;}
SI __m128i _mm_unpackhi_epi8(__m128i a,__m128i b){__m128i r;for(int i=0;i<8;i++){r.b[2*i]=a.b[8+i];r.b[2*i+1]=b.b[8+i];}return r;}
SI __m128i _mm_unpacklo_epi16(__m128i a,__m128i b){__m128i r;for(int i=0;i<4;i++){r.w[2*i]=a.w[i];r.w[2*i+1]=b.w[i];}return r;}
SI int8_t _sat8(int16_t v){return v>127?127:v<-128?-128:(int8_t)v;}
SI __m128i _mm_packs_epi16(__m128i a,__m128i b){__m128i r;for(int i=0;i<8;i++){r.sb[i]=_sat8(a.sw[i]);r.sb[8+i]=_sat8(b.sw[i]);}return r;}
SI int _mm_cvtsi128_si32(__m128i a){return (int)a.d[0];}
