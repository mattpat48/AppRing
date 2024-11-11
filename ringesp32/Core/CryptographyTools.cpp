#include <CryptographyTools.h>

void CryptographyTools::generate_rsa_keypair() {
  mbedtls_pk_init(&pk);
  mbedtls_ctr_drbg_init(&ctr_drbg);
  mbedtls_entropy_init(&entropy);

  mbedtls_ctr_drbg_seed(&ctr_drbg, mbedtls_entropy_func, &entropy, (const unsigned char *)pers, strlen(pers));

  mbedtls_pk_setup(&pk, mbedtls_pk_info_from_type(MBEDTLS_PK_RSA));

  int ret = mbedtls_rsa_gen_key(mbedtls_pk_rsa(pk), mbedtls_ctr_drbg_random, &ctr_drbg, 2048, 65537);
  if (ret != 0) {
    Serial.print("Errore nella generazione delle chiavi RSA: ");
    Serial.println(ret);
  } else {
    Serial.println("Chiave RSA generata con successo!");
  }

  size_t olen = 0;
  mbedtls_pk_write_pubkey_pem(&pk, (unsigned char *)public_key_pem, sizeof(public_key_pem));
  Serial.println("Chiave pubblica in PEM:");
  Serial.println(public_key_pem);
}

void CryptographyTools::decrypt_message(const unsigned char *input, size_t input_len, unsigned char *output, size_t *output_len) {
  
  if (mbedtls_pk_can_do(&pk, MBEDTLS_PK_RSA)) {
    mbedtls_rsa_context *rsa = mbedtls_pk_rsa(pk);
    int ret = mbedtls_rsa_pkcs1_decrypt(rsa, mbedtls_ctr_drbg_random, &ctr_drbg, MBEDTLS_RSA_PRIVATE, output_len, input, output, *output_len);
    if (ret != 0) {
      Serial.println("Errore nella decrittazione RSA");
    } else {
      Serial.println("Messaggio decriptato con successo!");
    }
  }
}