#ifndef CRYPTOGRAPHYTOOLS_H
#define CRYPTOGRAPHYTOOLS_H

#include "mbedtls/rsa.h"
#include "mbedtls/pk.h"
#include "mbedtls/ctr_drbg.h"
#include "mbedtls/entropy.h"

class CryptographyTools {

  private:
    mbedtls_pk_context pk;
    mbedtls_ctr_drbg_context ctr_drbg;
    mbedtls_entropy_context entropy;
    char *pers = "rsa_keygen";

  public:
    char public_key_pem[1600];
    void generate_rsa_keypair();
    void decrypt_message(const unsigned char *input, size_t input_len, unsigned char *output, size_t *output_len);

  CryptographyTools() {
    generate_rsa_keypair();
  }

}
#endif //CRYPTOGRAPHYTOOLS_H