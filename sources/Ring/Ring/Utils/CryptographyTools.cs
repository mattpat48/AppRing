using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Utils
{
    public static class CryptographyTools
    {
        public class EncryptedRequest
        {
            public string EncryptedData { get; set; }
            public string EncryptedKey { get; set; }
            public string EncryptedIV { get; set; }

            public EncryptedRequest(string encryptedData, string encryptedKey, string encryptedIV)
            {
                EncryptedData = encryptedData;
                EncryptedKey = encryptedKey;
                EncryptedIV = encryptedIV;
            }
        }

        // Metodo per criptare i dati da inviare al server
        // @param key: chiave con cui cifrare i dati
        // @param toEncrypt: stringa già serializzata da cifrare
        public static StringContent EncryptString(string key, string toEncrypt)
        {
            byte[] encryptedData;
            byte[] encryptedAesKey;
            byte[] encryptedAesIV;

            using RSA rsa = RSA.Create(2048);
            rsa.ImportFromPem(key.ToCharArray());

            // genero una chiave simmetrica per AES
            using Aes aes = Aes.Create();
            aes.GenerateKey();
            aes.GenerateIV();

            // cripto i dati usando AES
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using MemoryStream msEncrypt = new();
            using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                using StreamWriter swEncrypt = new(csEncrypt);
                // Scrivi tutti i dati da criptare nel flusso.
                swEncrypt.Write(toEncrypt);
            }
            encryptedData = msEncrypt.ToArray();
            encryptedAesKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);
            encryptedAesIV = rsa.Encrypt(aes.IV, RSAEncryptionPadding.Pkcs1);

            // converto i dati cifrati in base64
            string encryptedDataBase64 = Convert.ToBase64String(encryptedData);
            string encryptedAesKeyBase64 = Convert.ToBase64String(encryptedAesKey);
            string encryptedAesIVBase64 = Convert.ToBase64String(encryptedAesIV);

            // creo il contenuto da inviare al server
            var requestBody = new
            {
                EncryptedData = encryptedDataBase64,
                EncryptedKey = encryptedAesKeyBase64,
                EncryptedIV = encryptedAesIVBase64
            };
            string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
            var stringContent = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

            return stringContent;
        }

        public static string DecryptString(string decryptKey, string requestBody)
        {
            EncryptedRequest request = JsonConvert.DeserializeObject<EncryptedRequest>(requestBody);

            // Importo la chiave privata per decifrare i dati
            using RSA rsa = RSA.Create(2048);
            rsa.ImportFromPem(decryptKey.ToCharArray());

            // Decifro la chiave e l'IV
            byte[] key = rsa.Decrypt(Convert.FromBase64String(request.EncryptedKey), RSAEncryptionPadding.Pkcs1);
            byte[] iv = rsa.Decrypt(Convert.FromBase64String(request.EncryptedIV), RSAEncryptionPadding.Pkcs1);

            byte[] data = Convert.FromBase64String(request.EncryptedData);

            // Decifro i dati usando AES
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream msDecrypt = new MemoryStream(data))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        string plaintext = srDecrypt.ReadToEnd();
                        return plaintext;
                    }
                }
            }
        }
    }
}
