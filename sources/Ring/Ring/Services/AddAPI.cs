using Newtonsoft.Json;
using Ring.Utils;

namespace Ring.Services;
public class AddAPI
{
    private readonly HttpClient _httpClient;
    private readonly string? _server;

    public AddAPI(HttpClient client)
    {
        _httpClient = client;
        _server = Environment.GetEnvironmentVariable("API_SERVER");
    }

    public async Task<string> AddUserRequest(string gateId, string toAdd)
    {
        // compongo l'url della richiesta
        var url = _server + "/api/v1/gate/adduser";

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

        // compongo il package delle informazioni necessarie alla richiesta
        var toSend = new
        {
            GateId = gateId,
            ToAdd = toAdd
        };

        // cripto il package
        bool outcome;
        StringContent stringContent;
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject(toSend), number, id);

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
