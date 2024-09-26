using Newtonsoft.Json;
using Ring.Utils;

namespace Ring.Services;
public class MultipleAPI
{
    private readonly HttpClient _httpClient;
    private readonly string? _server;

    public MultipleAPI(HttpClient client)
    {
        _httpClient = client;
        _server = Environment.GetEnvironmentVariable("API_SERVER");
    }

    public async Task<(string, List<Utils.Device>?)> GetAllDevices()
    {
        // compongo l'url della richiesta
        var url = _server + "/api/v1/auth/getdevices";

        // prendo le info per identificarmi
        string? number = await SecureStorage.GetAsync("PhoneNumber");
        string? id = await SecureStorage.GetAsync("DeviceId");
        if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(id))
        {
            return ("Phone number or device id not found", null);
        }

        // prendo le chiavi
        string? publicKey = await SecureStorage.GetAsync("ServerKey");
        string? deviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(deviceKey))
        {
            return ("Server key or device key not found", null);
        }

        // cripto il package
        bool outcome;
        StringContent stringContent;
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject("Get all devices request"), number, id);

        if (!outcome)
        {
            return ("Error encrypting data", null);
        }

        // invio la richiesta e aspetto la risposta
        HttpResponseMessage response = await _httpClient.PostAsync(url, stringContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (responseContent, null);
        }
        else
        {
            try
            {
                string? devicekey = await SecureStorage.GetAsync("PrivateDeviceKey");
                string? serverkey = await SecureStorage.GetAsync("ServerKey");
                if (string.IsNullOrEmpty(devicekey) || string.IsNullOrEmpty(serverkey))
                {
                    return ("Device key not found", null);
                }
                string plainText;
                (outcome, plainText) = CryptographyTools.TotalDecrypt(devicekey, serverkey, responseContent);
                if (!outcome)
                {
                    return (plainText, null);
                }
                List<Utils.Device>? devices = JsonConvert.DeserializeObject<List<Utils.Device>>(plainText);
                return ("success", devices);
            }
            catch (Exception ex)
            {
                return (ex.Message, null);
            }
        }
    }

    public async Task<string> LogoutDeviceRequest(string toRemove)
    {
        // compongo l'url della richiesta
        var url = _server + "/api/v1/auth/logoutuser";

        // prendo le info per identificarmi
        string? number = await SecureStorage.GetAsync("PhoneNumber");
        string? id = await SecureStorage.GetAsync("DeviceId");
        if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(id))
        {
            return "Phone number or device id not found";
        }

        // prendo le chiavi
        string? publicKey = await SecureStorage.GetAsync("ServerKey");
        string? deviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(deviceKey))
        {
            return "Server key or device key not found";
        }

        // cripto il package
        bool outcome;
        StringContent stringContent;
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject(toRemove), number, id);

        if (!outcome)
        {
            return "Error encrypting data";
        }

        // invio la richiesta e aspetto la risposta
        HttpResponseMessage response = await _httpClient.PostAsync(url, stringContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return responseContent;
        }
        else
        {
            return "success";
        }
    }

}
