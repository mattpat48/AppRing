using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

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

        public class Payload
        {
            public string Data { get; set; }
            public string Signature { get; set; }

            public Payload(string data, string signature)
            {
                Data = data;
                Signature = signature;
            }
        }

        public class ExtendedIdentifier
        {
            public string Number { get; set; }
            public string Id { get; set; }
            public Payload Payload { get; set; }

            public ExtendedIdentifier(string number, string id, Payload payload)
            {
                Number = number;
                Id = id;
                Payload = payload;
            }
        }

        // Metodo per criptare i dati da inviare al server
        // @param key: chiave con cui cifrare i dati
        // @param toEncrypt: stringa già serializzata da cifrare
        public static (bool, string) EncryptString(string encryptKey, object toEncrypt)
        {
            try
            {
                byte[] encryptedData;
                byte[] encryptedAesKey;
                byte[] encryptedAesIV;

                // Importo la chiave pubblica per cifrare i dati
                using RSA rsa = RSA.Create(2048);
                rsa.ImportFromPem(encryptKey.ToCharArray());

                // Genero una chiave simmetrica per AES
                using Aes aes = Aes.Create();
                aes.GenerateKey();
                aes.GenerateIV();

                // Cripto i dati usando AES
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using MemoryStream msEncrypt = new();
                using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using StreamWriter swEncrypt = new(csEncrypt);
                    // Scrivo tutti i dati da criptare nel flusso
                    swEncrypt.Write(toEncrypt);
                }

                // Ottengo i dati cifrati
                encryptedData = msEncrypt.ToArray();
                encryptedAesKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);
                encryptedAesIV = rsa.Encrypt(aes.IV, RSAEncryptionPadding.Pkcs1);

                // Converto i dati cifrati in base64
                string encryptedDataBase64 = Convert.ToBase64String(encryptedData);
                string encryptedAesKeyBase64 = Convert.ToBase64String(encryptedAesKey);
                string encryptedAesIVBase64 = Convert.ToBase64String(encryptedAesIV);

                // Creo il contenuto da inviare al server
                var requestBody = new
                {
                    EncryptedData = encryptedDataBase64,
                    EncryptedKey = encryptedAesKeyBase64,
                    EncryptedIV = encryptedAesIVBase64
                };
                string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
                //var stringContent = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

                return (true, jsonRequestBody);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static (bool, string) SignData(string data, string privateKeyPem)
        {
            try
            {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                using RSA rsa = RSA.Create(2048);
                rsa.ImportFromPem(privateKeyPem.ToCharArray());
                byte[] signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return (true, Convert.ToBase64String(signature));
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }


        public static (bool, StringContent) TotalEncrypt(string privateKey, string publicKey, string content, string phoneNumber, string id)
        {
            bool outcome1, outcome2;
            string signature, encryptedWithPublicKey;

            try
            {
                (outcome1, signature) = SignData(content, privateKey);

                if (!outcome1)
                {
                    return (false, new StringContent("Error signing data"));
                }

                var payload = new Payload(content, signature);
                var toSend = new ExtendedIdentifier(phoneNumber, id, payload);

                (outcome2, encryptedWithPublicKey) = EncryptString(publicKey, JsonConvert.SerializeObject(toSend));
                if (!outcome2)
                {
                    return (false, new StringContent("Error encrypting data"));
                }

                var stringContent = new StringContent(encryptedWithPublicKey, Encoding.UTF8, "application/json");

                return (true, stringContent);
            }
            catch (Exception ex)
            {
                return (false, new StringContent(ex.Message));
            }
        }


        public static (bool, string) DecryptString(string decryptKey, string requestBody)
        {
            try
            { 
                EncryptedRequest? request = JsonConvert.DeserializeObject<EncryptedRequest>(requestBody);
                if (request == null)
                {
                    throw new Exception("Empty Request");
                }

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
                            return (true, plaintext);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static bool VerifySignature(string data, string signature, string publicKeyPem)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] signatureBytes = Convert.FromBase64String(signature);

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportFromPem(publicKeyPem.ToCharArray());
                return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
        }

        public static (bool, string) TotalDecrypt(string privateKey, string publicKey, string content)
        {
            bool outcome1, outcome2;
            string decryptedWithPrivateKey;

            try
            {
                (outcome1, decryptedWithPrivateKey) = DecryptString(privateKey, content);

                if (!outcome1)
                {
                    return (false, decryptedWithPrivateKey);
                }

                var decryptedJson = JsonConvert.DeserializeObject<dynamic>(decryptedWithPrivateKey);

                if (decryptedJson == null)
                {
                    return (false, "Error deserializing json");
                }

                string data = decryptedJson.Data;
                string signature = decryptedJson.Signature;

                outcome2 = VerifySignature(data, signature, publicKey);

                if (!outcome2)
                {
                    return (false, "Data is not signed!");
                }

                return (true, data);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }

}
