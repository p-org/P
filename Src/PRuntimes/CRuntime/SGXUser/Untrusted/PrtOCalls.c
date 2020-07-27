#include "ext_compat.h"
#include <stdio.h>

sgx_status_t SGX_CDECL ocall_print(const char* str) {
    printf("%s", str);
}

