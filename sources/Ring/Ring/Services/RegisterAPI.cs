using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Services;

public class RegisterAPI
{
    private readonly HttpClient _httpClient;

    public RegisterAPI(HttpClient client)
    {
        _httpClient = client;
    }

    public async Task<string> GetServerKey()
    {
        var url = "https://localhost:7046/api/v1/auth/publickey";
        try
        {
            // richiedo la chiave pubblica del server
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var serverKey = responseContent;
                if (!string.IsNullOrEmpty(serverKey))
                {
                    await SecureStorage.SetAsync("ServerKey", serverKey);
                    return "success";
                }
                else
                {
                    SecureStorage.Remove("ServerKey");
                    SecureStorage.Remove("PhoneNumber");
                    return "Server key not returned";
                }
            }
            else
            {
                SecureStorage.Remove("ServerKey");
                SecureStorage.Remove("PhoneNumber");
                return responseContent;
            }
        }
        catch (Exception ex)
        {
            SecureStorage.Remove("ServerKey");
            SecureStorage.Remove("PhoneNumber");
            return ex.Message;
        }
    }


    public async Task<string> SignInRequest()
    {
        var url = "https://localhost:7046/api/v1/auth/signin";

        // recupero le informazioni da inviare al server
        var keyToSend = await SecureStorage.GetAsync("PublicDeviceKey");
        var phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
        var idToSend = await SecureStorage.GetAsync("DeviceId");

        // creo il contenuto da inviare al server
        var myContent = new
        {
            PKey = keyToSend,
            Number = phoneNumber,
            Id = idToSend
        };

        try
        {
            var geServerKeyResponse = await GetServerKey();

            if (geServerKeyResponse == "success")
            {
                // cifro il contenuto da inviare al server
                var json = JsonConvert.SerializeObject(myContent);

                // recupero la chiave pubblica del server salvata in precedenza
                var rsaPublicKey = await SecureStorage.GetAsync("ServerKey");

                byte[] encryptedData;
                byte[] encryptedAesKey;
                byte[] encryptedAesIV;

                // cifro il contenuto con la chiave pubblica del server
                if (!string.IsNullOrEmpty(rsaPublicKey))
                {
                    using RSA rsa = RSA.Create(4096);
                    rsa.ImportFromPem(rsaPublicKey.ToCharArray());

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
                        swEncrypt.Write(json);
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

                    // invio il contenuto cifrato al server
                    HttpResponseMessage response = await _httpClient.PostAsync(url, stringContent);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        // se ho successo apro la pagina di verifica
                        return "success";
                    }
                    else
                    {
                        SecureStorage.Remove("ServerKey");
                        SecureStorage.Remove("PhoneNumber");
                        return responseContent;
                    }
                }
                else
                {
                    SecureStorage.Remove("ServerKey");
                    SecureStorage.Remove("PhoneNumber");
                    return "Key not found in SecureStorage";
                }
            }
            else
            {
                SecureStorage.Remove("ServerKey");
                SecureStorage.Remove("PhoneNumber");
                return geServerKeyResponse;
            }
        }
        catch (Exception ex)
        {
            SecureStorage.Remove("ServerKey");
            SecureStorage.Remove("PhoneNumber");
            return ex.Message;
        }
    }
}
