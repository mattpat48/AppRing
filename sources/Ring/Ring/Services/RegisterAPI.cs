using Newtonsoft.Json;
using Ring.Utils;
using System.Text;

namespace Ring.Services;

public class RegisterAPI
{
    private readonly HttpClient _httpClient;
    private readonly string? _server;

    public RegisterAPI(HttpClient client)
    {
        _httpClient = client;
        _server = Environment.GetEnvironmentVariable("API_SERVER");
    }

    public async Task<string> GetServerKey()
    {
        var url = _server + "/api/v1/auth/publickey";

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
                    return "Server key not returned";
                }
            }
            else
            {
                SecureStorage.Remove("ServerKey");
                return responseContent;
            }
        }
        catch (Exception ex)
        {
            SecureStorage.Remove("ServerKey");
            return ex.Message;
        }
    }


    public async Task<string> SignInRequest()
    {
        var url = _server + "/api/v1/auth/signin";

        // recupero le informazioni da inviare al server
        var keyToSend = await SecureStorage.GetAsync("PublicDeviceKey");
        var phoneNumber = await SecureStorage.GetAsync("PhoneNumber");
        var idToSend = await SecureStorage.GetAsync("DeviceId");
        var rememberLogin = await SecureStorage.GetAsync("RememberLogin");

        // creo il contenuto da inviare al server
        var myContent = new
        {
            PKey = keyToSend,
            Number = phoneNumber,
            Id = idToSend,
            RememberLogin = rememberLogin
        };

        try
        {
            var geServerKeyResponse = await GetServerKey();

            if (geServerKeyResponse == "success")
            {
                // recupero la chiave pubblica del server salvata in precedenza
                string? serverKey = await SecureStorage.GetAsync("ServerKey");

                // cifro il contenuto con la chiave pubblica del server
                if (!string.IsNullOrEmpty(serverKey))
                {
                    bool outcome;
                    string stringJson;
                    (outcome, stringJson) = CryptographyTools.EncryptString(serverKey, JsonConvert.SerializeObject(myContent));
                    StringContent stringContent = new StringContent(stringJson, Encoding.UTF8, "application/json");

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
