#pragma once
#include <stddef.h>
#ifdef __cplusplus
extern "C" {
#endif
void *malloc(size_t n); void free(void *p); void *calloc(size_t a,size_t b);
#ifdef __cplusplus
}
#endif
