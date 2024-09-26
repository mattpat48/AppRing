using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Ring.Utils;

namespace Ring.Services;

public class VerificationAPI
{

    private readonly HttpClient _httpClient;
    private readonly string? _server;

    public VerificationAPI(HttpClient client)
    {
        _httpClient = client;
        _server = Environment.GetEnvironmentVariable("API_SERVER");
    }

    public async Task<string> SendSMS(string phoneNumber)
    {
        if (phoneNumber == null)
        {
            return "Phone number not found";
        }
        else
        {
            var url = _server + "/api/v1/auth/sendcode";

            string? number = await SecureStorage.GetAsync("PhoneNumber");
            string? id = await SecureStorage.GetAsync("DeviceId");
            if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(id))
            {
                return "Phone number or device id not found";
            }

            string? deviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
            string? publicKey = await SecureStorage.GetAsync("ServerKey");
            if (string.IsNullOrEmpty(deviceKey) || string.IsNullOrEmpty(publicKey))
            {
                return "Server key or device key not found";
            }

            bool outcome;
            StringContent stringContent;
            string devpub = JsonConvert.SerializeObject(phoneNumber);
            (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject("Send Code Request"), number, id);

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
    }

    public async Task<string> VerifyCode(string inserted)
    {
        var url = _server + "/api/v1/auth/verifycode";

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
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject(inserted), number, id);

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

    public async Task<(string, bool)> HasManyDevices()
    {
        var url = _server + "/api/v1/auth/hasmanydevices";

        string? number = await SecureStorage.GetAsync("PhoneNumber");
        string? id = await SecureStorage.GetAsync("DeviceId");
        if (string.IsNullOrEmpty(number) || string.IsNullOrEmpty(id))
        {
            return ("Phone number or device id not found", false);
        }

        string? publicKey = await SecureStorage.GetAsync("ServerKey");
        string? deviceKey = await SecureStorage.GetAsync("PrivateDeviceKey");
        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(deviceKey))
        {
            return ("Server key or device key not found", false);
        }

        bool outcome;
        StringContent stringContent;
        (outcome, stringContent) = CryptographyTools.TotalEncrypt(deviceKey, publicKey, JsonConvert.SerializeObject("Has Many Devices Request"), number, id);

        if (!outcome)
        {
            return ("Error encrypting data", false);
        }

        HttpResponseMessage response = await _httpClient.PostAsync(url, stringContent);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return (responseContent, false);
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
}
