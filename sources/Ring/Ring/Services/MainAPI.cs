using Newtonsoft.Json;
using Ring.Utils;

namespace Ring.Services;
public class MainAPI
{
    private readonly HttpClient _httpClient;
    private readonly string? _server;

    public MainAPI(HttpClient client)
    {
        _httpClient = client;
        _server = Environment.GetEnvironmentVariable("API_SERVER");
    }

    public async Task<string> CheckLogout()
    {
        var url = _server + "/api/v1/auth/checklogout";

        string? number = await SecureStorage.GetAsync("PhoneNumber");
        string? id = await SecureStorage.GetAsync("DeviceId");
        if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(id))
        {
            return "Phone number or device id not found";
        }

        string? publicKey = await SecureStorage.GetAsync("ServerKey");
        string? deviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(deviceKey))
        {
            return "Server key or device key not found";
        }
        
        bool outcome;
        StringContent stringContent;
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject("Check Logout Request"), number, id);

        if (!outcome)
        {
            return "Error encrypting data";
        }

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

    public async Task<(string, List<Gate>?)> GetAllGates()
    {
        var url = _server + "/api/v1/gate/getallgates";

        string? number = await SecureStorage.GetAsync("PhoneNumber");
        string? id = await SecureStorage.GetAsync("DeviceId");
        if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(id))
        {
            return ("Phone number or device id not found", null);
        }

        string? publicKey = await SecureStorage.GetAsync("ServerKey");
        string? deviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(deviceKey))
        {
            return ("Server key or device key not found", null);
        }

        bool outcome;
        StringContent stringContent;
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject("Get All Gates Request"), number, id);

        if (!outcome)
        {
            return ("Error encrypting data", null);
        }

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
                List<Gate>? gates = JsonConvert.DeserializeObject<List<Gate>>(plainText);
                return ("success", gates);
            }
            catch (Exception ex)
            {
                return (ex.Message, null);  
            }
        }
    }

    public async Task<(string, bool?)> IsAdmin(string gateId)
    {
        var url = _server + "/api/v1/gate/isadmin";

        string? number = await SecureStorage.GetAsync("PhoneNumber");
        string? id = await SecureStorage.GetAsync("DeviceId");
        if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(id))
        {
            return ("Phone number or device id not found", null);
        }

        string? publicKey = await SecureStorage.GetAsync("ServerKey");
        string? deviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(deviceKey))
        {
            return ("Server key or device key not found", null);
        }

        bool outcome;
        StringContent stringContent;
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject(gateId), number, id);

        if (!outcome)
        {
            return ("Error encrypting data", null);
        }

        HttpResponseMessage response = await _httpClient.PostAsync(url, stringContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (responseContent, null);
        }
        else
        {
            if (responseContent == "true")
            {
                return ("success", true);
            }
            else
            {
                return ("success", false);
            }
        }
    }

    public async Task<string> GenerateLog(string gateId)
    {
        var url = _server + "/api/v1/gate/generatelog";

        string? number = await SecureStorage.GetAsync("PhoneNumber");
        string? id = await SecureStorage.GetAsync("DeviceId");
        if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(id))
        {
            return "Phone number or device id not found";
        }

        string? publicKey = await SecureStorage.GetAsync("ServerKey");
        string? deviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(deviceKey))
        {
            return "Server key or device key not found";
        }

        bool outcome;
        StringContent stringContent;
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject(gateId), number, id);

        if (!outcome)
        {
            return "Error encrypting data";
        }

        HttpResponseMessage response = await _httpClient.PostAsync(url, stringContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return responseContent;
        }
        else
        {
            if (responseContent == "true")
            {
                return "success";
            }
            else
            {
                return "success";
            }
        }
    }

    public async Task<string> ManuallyAddLogs(List<Log> logs)
    {
        var url = _server + "/api/v1/gate/manuallyaddlogs";

        string? number = await SecureStorage.GetAsync("PhoneNumber");
        string? id = await SecureStorage.GetAsync("DeviceId");
        if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(id))
        {
            return "Phone number or device id not found";
        }

        string? publicKey = await SecureStorage.GetAsync("ServerKey");
        string? deviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(deviceKey))
        {
            return "Server key or device key not found";
        }

        bool outcome;
        StringContent stringContent;
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject(logs), number, id);

        if (!outcome)
        {
            return "Error encrypting data";
        }

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

    public async Task<(string, List<Log>?)> GetLogs(string gateId)
    {
        var url = _server + "/api/v1/gate/getlogs";

        string? number = await SecureStorage.GetAsync("PhoneNumber");
        string? id = await SecureStorage.GetAsync("DeviceId");
        if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(id))
        {
            return ("Phone number or device id not found", null);
        }

        string? publicKey = await SecureStorage.GetAsync("ServerKey");
        string? deviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(deviceKey))
        {
            return ("Server key or device key not found", null);
        }

        bool outcome;
        StringContent stringContent;
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject(gateId), number, id);

        if (!outcome)
        {
            return ("Error encrypting data", null);
        }

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
                List<Log>? logs = JsonConvert.DeserializeObject<List<Log>>(plainText);
                return ("success", logs);
            }
            catch (Exception ex)
            {
                return (ex.Message, null);
            }
        }
    }
}
