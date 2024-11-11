using CommunityToolkit.Maui.Alerts;
using ExCSS;
using Newtonsoft.Json;
using Ring.Utils;
using System.Net.Http;

namespace Ring.Services;
public class UsersAPI
{
    private readonly HttpClient _httpClient;
    private readonly string? _server;

    public UsersAPI(HttpClient client)
    {
        _httpClient = client;
        _server = Environment.GetEnvironmentVariable("API_SERVER");
    }

    public async Task<string> AddUserRequest(string gateId, string toAdd, bool makeAdmin)
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
            ToAdd = toAdd,
            MakeAdmin = makeAdmin
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

    public async Task<(string, List<string>?)> GetUsersPerGate(string gateId)
    {
        // compongo l'url della richiesta
        var url = _server + "/api/v1/gate/getuserspergate";

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
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject(gateId), number, id);

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
                List<string>? users = JsonConvert.DeserializeObject<List<string>>(plainText);
                return ("success", users);
            }
            catch (Exception ex)
            {
                return (ex.Message, null);
            }
        }
    }

    public async Task<string> RemoveUserRequest(string gateId, string toRemove)
    {
        // compongo l'url della richiesta
        var url = _server + "/api/v1/gate/removeuserfromgate";

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
            ToRemove = toRemove
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
