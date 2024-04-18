using Newtonsoft.Json;
using Ring.Utils;

namespace Ring.Services;
public class MainAPI
{
    private readonly HttpClient _httpClient;
    private readonly string _server;

    public MainAPI(HttpClient client)
    {
        _httpClient = client;
        _server = Environment.GetEnvironmentVariable("API_SERVER");
    }

    public async Task<string> CheckLogout()
    {
        var url = _server + "/api/v1/auth/checklogout";

        var toSend = new
        {
            Number = await SecureStorage.GetAsync("PhoneNumber"),
            Id = await SecureStorage.GetAsync("DeviceId")
        };

        string key = await SecureStorage.GetAsync("ServerKey");
        if (key == null)
        {
            return "Server key not found";
        }
        var stringContent = CryptographyTools.EncryptString(key, JsonConvert.SerializeObject(toSend));

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

        var toSend = new
        {
            Number = await SecureStorage.GetAsync("PhoneNumber"),
            Id = await SecureStorage.GetAsync("DeviceId"),
        };

        string key = await SecureStorage.GetAsync("ServerKey");
        if (key == null)
        {
            return ("Server key not found", null);
        }
        var stringContent = CryptographyTools.EncryptString(key, JsonConvert.SerializeObject(toSend));

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
                string devicekey = await SecureStorage.GetAsync("PrivateDeviceKey");
                string plainText = CryptographyTools.DecryptString(devicekey, responseContent);
                List<Gate> gates = JsonConvert.DeserializeObject<List<Gate>>(plainText);
                return ("success", gates);
            }
            catch (Exception ex)
            {
                return (ex.Message, null);  
            }
        }
    }
}
