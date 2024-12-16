#include <Arduino.h>
#include <cstring>
#include <vector>
#include <string>
#include <ArduinoJson.h>
#include <HTTPClient.h>

#include <mbedtls/aes.h>
#include <mbedtls/rsa.h>
#include <mbedtls/pk.h>
#include <mbedtls/md.h>
#include <mbedtls/sha256.h>
#include <mbedtls/base64.h>
#include <mbedtls/ctr_drbg.h>
#include <mbedtls/entropy.h>

class Crypto
{
  public:
    String privateKeyPem;
    String publicKeyPem;

    int generateRSAKeys();
    int decryptRSA(const std::string& privateKeyPem, const std::vector<uint8_t>& encryptedData, std::vector<uint8_t>& decryptedData);
    int decryptAES(const std::vector<uint8_t>& key, const std::vector<uint8_t>& iv, const std::vector<uint8_t>& data, std::vector<uint8_t>& output);
    int verifySignature(const String data, const String signature, const String userPublicKeyPem);
    String totalDecrypt(String encryptedData, String encryptedKey, String encryptedIV, String API_ADDRESS);
};