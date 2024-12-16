#include "Crypto.h"


int Crypto::generateRSAKeys() {
  int ret;

  mbedtls_pk_context pk;
  mbedtls_ctr_drbg_context ctr_drbg;
  mbedtls_entropy_context entropy;
  mbedtls_pk_init(&pk);
  mbedtls_ctr_drbg_init(&ctr_drbg);
  mbedtls_entropy_init(&entropy);
  char* pers = "rsa_key";

  if (ret = mbedtls_ctr_drbg_seed(&ctr_drbg, mbedtls_entropy_func, &entropy, (const unsigned char *)pers, strlen(pers)) != 0) {
    Serial.println("mbedtls_ctr_drbg_seed FAIL");
    return ret;
  }

  if (ret = mbedtls_pk_setup(&pk, mbedtls_pk_info_from_type(MBEDTLS_PK_RSA)) != 0) {
    Serial.println("mbedtls_pk_setup FAIL");
    return ret;
  }

  mbedtls_rsa_context* rsa = mbedtls_pk_rsa(pk);
  if (ret = mbedtls_rsa_gen_key(rsa, mbedtls_ctr_drbg_random, &ctr_drbg, 1024, 65537) != 0) {
    Serial.println("mbedtls_rsa_gen_key FAIL");
    return ret;
  }

  unsigned char output_buf[1600];
  size_t olen = 0;

  if (ret = mbedtls_pk_write_key_pem(&pk, output_buf, sizeof(output_buf)) != 0) {
    Serial.println("mbedtls_pk_write_key_pem FAIL");
    return ret;
  }
  privateKeyPem = String((char*)output_buf);

  if (ret = mbedtls_pk_write_pubkey_pem(&pk, output_buf, sizeof(output_buf)) != 0) {
    Serial.println("mbedtls_pk_write_pubkey_pem FAIL");
    return ret;
  }
  publicKeyPem = String((char*)output_buf);

  mbedtls_pk_free(&pk);
  mbedtls_ctr_drbg_free(&ctr_drbg);
  mbedtls_entropy_free(&entropy);

  Serial.println("generateRsaKeys OK");
  return 0;
}





int Crypto::decryptRSA(const std::string& privateKeyPem, const std::vector<uint8_t>& encryptedData, std::vector<uint8_t>& decryptedData) {
  int ret;

  mbedtls_pk_context pk;
  mbedtls_ctr_drbg_context ctr_drbg;
  mbedtls_entropy_context entropy;
  mbedtls_pk_init(&pk);
  mbedtls_ctr_drbg_init(&ctr_drbg);
  mbedtls_entropy_init(&entropy);
  char* pers = "rsa_key";

  if (ret = mbedtls_ctr_drbg_seed(&ctr_drbg, mbedtls_entropy_func, &entropy, (const unsigned char *)pers, strlen(pers)) != 0) {
    Serial.println("mbedtls_ctr_drbg_seed FAIL");
    return ret;
  }

  if (ret = mbedtls_pk_parse_key(&pk, (const unsigned char*)privateKeyPem.c_str(), privateKeyPem.size() + 1, NULL, 0, mbedtls_ctr_drbg_random, &ctr_drbg) != 0) {
    Serial.println("mbedtls_pk_parse_key FAIL");
    return ret;
  }

  size_t olen = 0;
  decryptedData.resize(mbedtls_pk_get_len(&pk));
  if (ret = mbedtls_pk_decrypt(&pk, encryptedData.data(), encryptedData.size(), decryptedData.data(), &olen, decryptedData.size(), mbedtls_ctr_drbg_random, &ctr_drbg) != 0) {
    Serial.println("mbedtls_pk_decrypt FAIL");
    return ret;
  }
  decryptedData.resize(olen);
  mbedtls_pk_free(&pk);
  mbedtls_ctr_drbg_free(&ctr_drbg);
  mbedtls_entropy_free(&entropy);

  Serial.println("decryptRSA OK");
  return 0;
}




int Crypto::decryptAES(const std::vector<uint8_t>& key, const std::vector<uint8_t>& iv, const std::vector<uint8_t>& data, std::vector<uint8_t>& output) {
  mbedtls_aes_context aes;
  mbedtls_aes_init(&aes);
  mbedtls_aes_setkey_dec(&aes, key.data(), 128); // 128 bit

  size_t data_len = data.size();
  output.resize(data_len);

  uint8_t iv_copy[16];
  memcpy(iv_copy, iv.data(), iv.size());

  int ret = mbedtls_aes_crypt_cbc(&aes, MBEDTLS_AES_DECRYPT, data_len, iv_copy, data.data(), output.data());
  if (ret != 0) {
    Serial.println("mbedtls_aes_crypt_cbc FAIL");
    return ret;
  }
  mbedtls_aes_free(&aes);

  Serial.println("decryptAES OK");
  return 0;
}




int Crypto::verifySignature(const String data, const String signature, const String userPublicKeyPem) {
    // Initialize contexts
    mbedtls_pk_context pk;
    mbedtls_pk_init(&pk);
    mbedtls_sha256_context sha_ctx;
    mbedtls_sha256_init(&sha_ctx);

    // Outcome tracking
    int ret = 0;
    bool verified = false;

    // 1. Parse the public key
    ret = mbedtls_pk_parse_public_key(&pk, reinterpret_cast<const unsigned char*>(userPublicKeyPem.c_str()), userPublicKeyPem.length() + 1);
    if (ret != 0) {
        Serial.print("mbedtls_pk_parse_public_key FAIL: ");
        return ret;
    }

    // 2. Compute SHA256 hash of the data
    unsigned char hash[32]; // SHA256 produces 32-byte hash
    mbedtls_sha256_starts(&sha_ctx, 0); // 0 for SHA-256
    mbedtls_sha256_update(&sha_ctx, reinterpret_cast<const unsigned char*>(data.c_str()), data.length());
    mbedtls_sha256_finish(&sha_ctx, hash);

    // 3. Decode base64 signature
    size_t signature_len = 0;
    unsigned char sig_decoded[128]; // Adjust size for your key length
    
    ret = mbedtls_base64_decode(sig_decoded, sizeof(sig_decoded), &signature_len, reinterpret_cast<const unsigned char*>(signature.c_str()), signature.length());
    if (ret != 0) {
        Serial.print("mbedtls_base64_decode: ");
        return ret;
    }

    // 4. Verify the signature
    ret = mbedtls_pk_verify(&pk, MBEDTLS_MD_SHA256, hash, sizeof(hash), sig_decoded, signature_len);
    if (ret != 0) {
      Serial.println("mbedtls_pk_verify: ");
      Serial.println(ret);
    }

    return ret;
}




String getUserPublic(String API_ADDRESS, String phoneNumber, String id) {

  HTTPClient http;

  String keyPath = API_ADDRESS + "/api/v1/auth/getuserpublic";
  http.begin(keyPath.c_str());
  http.addHeader("Content-Type", "application/json");

  String payload = "{\"phoneNumber\":\"" + phoneNumber + "\",""\"id\":\"" + id + "\"}";
  int httpResponseCode = http.POST(payload);
  
  String returned = "";
  if (httpResponseCode == 200) {
    Serial.print("GET USER KEY Response code: ");
    Serial.println(httpResponseCode);
    returned = http.getString();
  }
  else {
    Serial.print("GET USER KEY Error code: ");
    Serial.println(httpResponseCode);
    Serial.println(http.getString());
    return "";
  }

  http.end();
  return returned;
}




String Crypto::totalDecrypt(String encryptedData, String encryptedKey, String encryptedIV, String API_ADDRESS) {
  int ret;

  std::vector<uint8_t> decodedData(encryptedData.length());
  size_t decodedDataLen;
  mbedtls_base64_decode(decodedData.data(), decodedData.size(), &decodedDataLen, (const unsigned char*)encryptedData.c_str(), encryptedData.length());
  decodedData.resize(decodedDataLen);

  std::vector<uint8_t> decodedKey(encryptedKey.length());
  size_t decodedKeyLen;
  mbedtls_base64_decode(decodedKey.data(), decodedKey.size(), &decodedKeyLen, (const unsigned char*)encryptedKey.c_str(), encryptedKey.length());
  decodedKey.resize(decodedKeyLen);

  std::vector<uint8_t> decodedIV(encryptedIV.length());
  size_t decodedIVLen;
  mbedtls_base64_decode(decodedIV.data(), decodedIV.size(), &decodedIVLen, (const unsigned char*)encryptedIV.c_str(), encryptedIV.length());
  decodedIV.resize(decodedIVLen);

  std::vector<uint8_t> aesKey;
  std::vector<uint8_t> iv;
  ret = decryptRSA(privateKeyPem.c_str(), decodedKey, aesKey);
  if (ret != 0) {
    Serial.print("Error code: ");
    Serial.print(ret);
    return "";
  }
  ret = decryptRSA(privateKeyPem.c_str(), decodedIV, iv);
  if (ret != 0) {
    Serial.print("Error code: ");
    Serial.print(ret);
    return "";
  }

  std::vector<uint8_t> decryptedData;
  ret = decryptAES(aesKey, iv, decodedData, decryptedData);
  if (ret != 0) {
    Serial.print("Error code: ");
    Serial.print(ret);
    return "";
  }

  String decryptedString = "";
  for (size_t i = 0; i < decryptedData.size(); i++) {
    decryptedString += (char)decryptedData[i];
  }

  JsonDocument decryptedDoc;
  DeserializationError error = deserializeJson(decryptedDoc, decryptedString);
  if (error) {
    Serial.print(F("crypto deserializeJson() 1 failed: "));
    Serial.println(error.f_str());
    return "";
  }

  String phoneNumber = decryptedDoc["Number"];
  String id = decryptedDoc["Id"];
  String payload = decryptedDoc["Payload"];

  JsonDocument dataPayload;
  error = deserializeJson(dataPayload, payload);
  if (error) {
    Serial.print(F("crypto deserializeJson() 2 failed: "));
    Serial.println(error.f_str());
    return "";
  }

  String data = dataPayload["Data"];
  String signature = dataPayload["Signature"];

  String userPublicKeyPem = getUserPublic(API_ADDRESS, phoneNumber, id);
  if (userPublicKeyPem == "") {
    Serial.println("Error while getting user key");
    return "";
  }

  ret = verifySignature(data, signature, userPublicKeyPem);
  if (ret != 0) {
    Serial.print("Error code: ");
    Serial.print(ret);
    //return "";
  }

  Serial.println("totalDecrypt OK");
  return data;
}