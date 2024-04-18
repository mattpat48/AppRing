using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace RingServer.Utils;
public class CryptographyTools
{

    // Metodo per criptare i dati da inviare al server
    // @param key: chiave con cui cifrare i dati
    // @param toEncrypt: stringa già serializzata da cifrare
    public static object EncryptString(string encryptKey, string toEncrypt)
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
        //string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
        //var stringContent = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

        return requestBody;
    }

    public static string DecryptString(string decryptKey, string requestBody)
    {
        CommonClasses.EncryptedRequest request = JsonConvert.DeserializeObject<CommonClasses.EncryptedRequest>(requestBody);

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
